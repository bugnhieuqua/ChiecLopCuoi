#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SherpaOnnx;
using UnityEngine;

namespace ChiecLopCuoi.Audio
{
    /// <summary>
    /// Nạp thư viện native của k2-fsa/sherpa-onnx và vận hành pipeline
    /// Silero VAD (silero_vad.onnx) + Zipformer Vietnamese (sherpa-onnx-vi)
    /// để tách rành mạch từng phát âm và triệt tiêu lỗi kích hoạt nhiều lần.
    /// </summary>
    public sealed class SherpaVoiceSession : IDisposable
    {
        public const int ExpectedSampleRate = 16000;
        public const int VadWindowSize = 512;

        private const float VadThreshold = 0.50f;
        private const float VadMinSilenceDuration = 0.06f; // Kết thúc câu nhanh hơn (trước: 0.09)
        private const float VadMinSpeechDuration = 0.06f;  // Nhận âm tiết ngắn tốt hơn (trước: 0.09)
        private const float VadMaxSpeechDuration = 2.6f;
        private const float MinSegmentRms = 0.008f;
        private const float MinSegmentPeak = 0.02f;

        private const int PreRollWindows = 3;          // 96ms giữ lại phụ âm đầu từ
        private const int MinSpeculativeWindows = 9;   // 288ms buffer = 192ms thoại thực (đã gồm 96ms pre-roll)
        private const int SpeculativeStepWindows = 3;  // Thử giải mã sớm mỗi 96ms khi đang nói
        private const int MaxSpeculativeWindows = 32;  // ~1s thoại: dừng decode sớm để không đốt CPU
        private const int MaxSnapshotSamples = ExpectedSampleRate * 8 / 10; // Chỉ decode 0.8s gần nhất
        private const int MaxSpeculativeSamples = ExpectedSampleRate * 2;

        // Hotwords (contextual biasing) chỉ hoạt động với modified_beam_search.
        // Đo thực tế trên i5-7300U: beam chậm hơn greedy ~20-60ms/câu ngắn,
        // nên mặc định TẮT để ưu tiên realtime. Đổi thành true nếu cần độ chính xác.
        private const bool EnableHotwords = false;

        private static readonly object NativeInitLock = new object();
        private static bool _nativeLibrariesLoaded;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        private VoiceActivityDetector _vad;
        private OfflineRecognizer _recognizer;
        private readonly float[] _windowBuffer = new float[VadWindowSize];
        private int _windowFill;
        private readonly Queue<float[]> _preRollQueue = new Queue<float[]>(PreRollWindows + 1);
        private readonly List<float> _liveSpeech = new List<float>(ExpectedSampleRate * 2);
        private bool _wasInSpeech;
        private bool _utteranceCommitted;
        private int _windowsSinceLastSpeculative;
        private int _speechWindows;
        private int _adaptiveStep = SpeculativeStepWindows;
        private bool _disposed;

        /// <summary>
        /// Preload onnxruntime.dll + sherpa-onnx-c-api.dll từ thư mục plugin của dự án
        /// trước System32\onnxruntime.dll (Windows inbox cũ gây BadImageFormatException).
        /// </summary>
        public static bool EnsureNativeLibrariesLoaded(string pluginsDir)
        {
            lock (NativeInitLock)
            {
                if (_nativeLibrariesLoaded) return true;
                if (string.IsNullOrEmpty(pluginsDir) || !Directory.Exists(pluginsDir)) return false;

                try
                {
                    SetDllDirectory(pluginsDir);
                }
                catch { }

                string ortPath = Path.Combine(pluginsDir, "onnxruntime.dll");
                string sherpaPath = Path.Combine(pluginsDir, "sherpa-onnx-c-api.dll");

                if (File.Exists(ortPath))
                {
                    IntPtr hOrt = LoadLibrary(ortPath);
                    if (hOrt == IntPtr.Zero) return false;
                }

                if (File.Exists(sherpaPath))
                {
                    IntPtr hSherpa = LoadLibrary(sherpaPath);
                    if (hSherpa == IntPtr.Zero) return false;
                }

                _nativeLibrariesLoaded = true;
                return true;
            }
        }

        public static string ResolvePluginsDir(string unityDataPath)
        {
            if (string.IsNullOrEmpty(unityDataPath)) return string.Empty;
            string x64 = Path.Combine(unityDataPath, "Plugins", "x86_64");
            if (Directory.Exists(x64)) return x64;
            string flat = Path.Combine(unityDataPath, "Plugins");
            return Directory.Exists(flat) ? flat : string.Empty;
        }

        public static bool HasModelFiles(string streamingAssetsPath)
        {
            if (string.IsNullOrEmpty(streamingAssetsPath)) return false;
            string vadPath = Path.Combine(streamingAssetsPath, "Models", "silero_vad.onnx");
            string viDir = Path.Combine(streamingAssetsPath, "Models", "sherpa-onnx-vi");
            return File.Exists(vadPath)
                && File.Exists(Path.Combine(viDir, "encoder.int8.onnx"))
                && File.Exists(Path.Combine(viDir, "decoder.onnx"))
                && File.Exists(Path.Combine(viDir, "joiner.int8.onnx"))
                && File.Exists(Path.Combine(viDir, "tokens.txt"));
        }

        public SherpaVoiceSession(string pluginsDir, string streamingAssetsPath)
        {
            if (!EnsureNativeLibrariesLoaded(pluginsDir))
            {
                throw new InvalidOperationException("Không thể nạp thư viện native onnxruntime/sherpa-onnx-c-api.");
            }

            string vadPath = Path.Combine(streamingAssetsPath, "Models", "silero_vad.onnx");
            string viDir = Path.Combine(streamingAssetsPath, "Models", "sherpa-onnx-vi");

            var vadCfg = new VadModelConfig();
            vadCfg.SileroVad.Model = vadPath;
            vadCfg.SileroVad.Threshold = VadThreshold;
            vadCfg.SileroVad.MinSilenceDuration = VadMinSilenceDuration;
            vadCfg.SileroVad.MinSpeechDuration = VadMinSpeechDuration;
            vadCfg.SileroVad.WindowSize = VadWindowSize;
            vadCfg.SileroVad.MaxSpeechDuration = VadMaxSpeechDuration;
            vadCfg.SampleRate = ExpectedSampleRate;
            vadCfg.NumThreads = 1;
            vadCfg.Debug = 0;

            var recCfg = new OfflineRecognizerConfig();
            recCfg.ModelConfig.Transducer.Encoder = Path.Combine(viDir, "encoder.int8.onnx");
            recCfg.ModelConfig.Transducer.Decoder = Path.Combine(viDir, "decoder.onnx");
            recCfg.ModelConfig.Transducer.Joiner = Path.Combine(viDir, "joiner.int8.onnx");
            recCfg.ModelConfig.Tokens = Path.Combine(viDir, "tokens.txt");
            // 2 thread là điểm tối ưu trên CPU 2 nhân/4 luồng (i5-7300U):
            // đo thực tế 3 thread chậm hơn 1.5-2x do tranh chấp với Unity/OS.
            recCfg.ModelConfig.NumThreads = 2;
            recCfg.ModelConfig.Debug = 0;

            string hotwordsPath = Path.Combine(viDir, "hotwords.txt");
            string bpeVocabPath = Path.Combine(viDir, "bpe.vocab");
            if (EnableHotwords && File.Exists(hotwordsPath) && File.Exists(bpeVocabPath))
            {
                // Hotwords bắt buộc modified_beam_search; beam chậm hơn greedy trên CPU yếu.
                recCfg.DecodingMethod = "modified_beam_search";
                recCfg.MaxActivePaths = 2;
                recCfg.ModelConfig.ModelingUnit = "bpe";
                recCfg.ModelConfig.BpeVocab = bpeVocabPath;
                recCfg.HotwordsFile = hotwordsPath;
                recCfg.HotwordsScore = 1.5f;
            }
            else
            {
                recCfg.DecodingMethod = "greedy_search";
            }

            _vad = new VoiceActivityDetector(vadCfg, 15f);
            _recognizer = new OfflineRecognizer(recCfg);

            // Warm-up: nạp graph ORT + cấp phát bộ nhớ trước lệnh thật đầu tiên.
            // Không có bước này, lệnh đầu sau khi bật mic có thể trễ thêm 200-500ms.
            try { DecodeAudio(new float[ExpectedSampleRate / 2]); } catch { }
        }

        /// <summary>
        /// Đẩy mảng PCM float [-1..1] (16kHz mono) vào Silero VAD và giải mã mỗi câu nói hoàn chỉnh thành chuỗi.
        /// </summary>
        public void AcceptSamples(float[] samples, int count, List<string> decodedOut)
        {
            AcceptSamplesRealtime(samples, count, (text, isFinal) =>
            {
                if (!string.IsNullOrWhiteSpace(text) && decodedOut != null)
                {
                    decodedOut.Add(text);
                }
                return true;
            }, enableSpeculative: false);
        }

        /// <summary>
        /// Luồng nhận diện gần real-time (~250-350ms trên CPU 2 nhân; decode sớm bắt đầu sau ~192ms thoại):
        /// - Thử giải mã sớm mỗi ~96ms ngay trong khi đang nói qua Silero VAD IsSpeechDetected().
        /// - Chỉ decode tối đa 0.8s gần nhất và dừng thử sau ~1s thoại để giữ CPU cho game.
        /// - callback nhận (decodedText, isFinalSegment) -> trả true khi đã chốt lệnh hợp lệ,
        ///   từ đó khóa _utteranceCommitted để phần đuôi câu nói không bao giờ bắn trùng.
        /// </summary>
        public void AcceptSamplesRealtime(float[] samples, int count, Func<string, bool, bool> onCandidateDecoded, bool enableSpeculative = true)
        {
            if (_disposed || samples == null || count <= 0) return;

            int offset = 0;
            while (offset < count)
            {
                int needed = VadWindowSize - _windowFill;
                int copy = Math.Min(needed, count - offset);
                Array.Copy(samples, offset, _windowBuffer, _windowFill, copy);
                _windowFill += copy;
                offset += copy;

                if (_windowFill == VadWindowSize)
                {
                    ProcessVadWindow(onCandidateDecoded, enableSpeculative);
                    _windowFill = 0;
                }
            }
        }

        private void ProcessVadWindow(Func<string, bool, bool> onCandidateDecoded, bool enableSpeculative)
        {
            _vad.AcceptWaveform(_windowBuffer);
            bool inSpeech = _vad.IsSpeechDetected();

            if (inSpeech)
            {
                if (!_wasInSpeech)
                {
                    _wasInSpeech = true;
                    _utteranceCommitted = false;
                    _windowsSinceLastSpeculative = 0;
                    _speechWindows = 0;
                    _adaptiveStep = SpeculativeStepWindows;
                    _liveSpeech.Clear();
                    foreach (float[] pre in _preRollQueue)
                    {
                        _liveSpeech.AddRange(pre);
                    }
                }

                if (_liveSpeech.Count + VadWindowSize <= MaxSpeculativeSamples)
                {
                    _liveSpeech.AddRange(_windowBuffer);
                }

                _windowsSinceLastSpeculative++;
                _speechWindows++;
                if (enableSpeculative
                    && !_utteranceCommitted
                    && _speechWindows <= MaxSpeculativeWindows
                    && _liveSpeech.Count >= VadWindowSize * MinSpeculativeWindows
                    && _windowsSinceLastSpeculative >= _adaptiveStep)
                {
                    _windowsSinceLastSpeculative = 0;

                    // Chỉ decode 0.8s gần nhất: câu càng dài thì chi phí decode không tăng vô hạn.
                    int snapStart = Math.Max(0, _liveSpeech.Count - MaxSnapshotSamples);
                    int snapCount = _liveSpeech.Count - snapStart;
                    float[] snap = new float[snapCount];
                    _liveSpeech.CopyTo(snapStart, snap, 0, snapCount);

                    if (PassesEnergyGate(snap))
                    {
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        string earlyText = DecodeAudio(snap);
                        sw.Stop();

                        // CPU yếu: nếu decode gần bằng bước thử thì giãn bước để không trễ dây chuyền.
                        if (sw.ElapsedMilliseconds > 80) _adaptiveStep = Math.Min(4, _adaptiveStep + 1);
                        else if (sw.ElapsedMilliseconds < 45) _adaptiveStep = Math.Max(SpeculativeStepWindows, _adaptiveStep - 1);

                        if (!string.IsNullOrEmpty(earlyText) && onCandidateDecoded != null && onCandidateDecoded(earlyText, false))
                        {
                            _utteranceCommitted = true;
                        }
                    }
                }
            }
            else
            {
                _wasInSpeech = false;
                var frameCopy = new float[VadWindowSize];
                Array.Copy(_windowBuffer, 0, frameCopy, 0, VadWindowSize);
                if (_preRollQueue.Count >= PreRollWindows) _preRollQueue.Dequeue();
                _preRollQueue.Enqueue(frameCopy);
            }

            DrainReadySegmentsRealtime(onCandidateDecoded);

            if (!inSpeech && _vad.IsEmpty())
            {
                _utteranceCommitted = false;
                _liveSpeech.Clear();
                _speechWindows = 0;
                _adaptiveStep = SpeculativeStepWindows;
            }
        }

        public void Flush(List<string> decodedOut)
        {
            FlushRealtime((text, isFinal) =>
            {
                if (!string.IsNullOrWhiteSpace(text) && decodedOut != null)
                {
                    decodedOut.Add(text);
                    return true;
                }
                return false;
            });
        }

        public void FlushRealtime(Func<string, bool, bool> onCandidateDecoded)
        {
            if (_disposed) return;

            if (_windowFill > 0)
            {
                Array.Clear(_windowBuffer, _windowFill, VadWindowSize - _windowFill);
                ProcessVadWindow(onCandidateDecoded, enableSpeculative: false);
                _windowFill = 0;
            }

            _vad.Flush();
            DrainReadySegmentsRealtime(onCandidateDecoded);
            _wasInSpeech = false;
            _utteranceCommitted = false;
            _liveSpeech.Clear();
            _speechWindows = 0;
            _adaptiveStep = SpeculativeStepWindows;
        }

        public void Reset()
        {
            if (_disposed) return;
            _windowFill = 0;
            _wasInSpeech = false;
            _utteranceCommitted = false;
            _windowsSinceLastSpeculative = 0;
            _speechWindows = 0;
            _adaptiveStep = SpeculativeStepWindows;
            _liveSpeech.Clear();
            _preRollQueue.Clear();
            _vad.Reset();
        }

        private void DrainReadySegmentsRealtime(Func<string, bool, bool> onCandidateDecoded)
        {
            while (!_vad.IsEmpty())
            {
                SpeechSegment seg = _vad.Front();
                _vad.Pop();

                bool alreadyCommitted = _utteranceCommitted;
                _utteranceCommitted = false;
                _liveSpeech.Clear();
                _speechWindows = 0;
                _adaptiveStep = SpeculativeStepWindows;

                if (alreadyCommitted) continue;

                float[] audio = seg.Samples;
                if (audio == null || audio.Length < ExpectedSampleRate / 12) continue;
                if (!PassesEnergyGate(audio)) continue;

                string text = DecodeAudio(audio);
                if (!string.IsNullOrEmpty(text) && onCandidateDecoded != null)
                {
                    onCandidateDecoded(text, true);
                }
            }
        }

        private string DecodeAudio(float[] audio)
        {
            using (OfflineStream stream = _recognizer.CreateStream())
            {
                stream.AcceptWaveform(ExpectedSampleRate, audio);
                _recognizer.Decode(stream);
                string text = stream.Result.Text;
                return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
            }
        }

        private static bool PassesEnergyGate(float[] samples)
        {
            double sumSq = 0;
            float peak = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float s = samples[i];
                float abs = s < 0f ? -s : s;
                if (abs > peak) peak = abs;
                sumSq += s * s;
            }
            double rms = Math.Sqrt(sumSq / samples.Length);
            return rms >= MinSegmentRms && peak >= MinSegmentPeak;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try { _vad?.Dispose(); } catch { }
            try { _recognizer?.Dispose(); } catch { }
            _vad = null;
            _recognizer = null;
        }
    }
}
#endif
