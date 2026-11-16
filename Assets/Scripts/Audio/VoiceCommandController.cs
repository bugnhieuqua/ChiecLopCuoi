using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChiecLopCuoi.Gameplay;
using ChiecLopCuoi.UI;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

namespace ChiecLopCuoi.Audio
{
    public enum VoiceCommandKind
    {
        None = 0,
        Block = 1,
        Skill1_Attack = 2,
        Skill2_TireThrow = 3,
        Skill3_TireSpin = 4,
        Skill4_JumpSlam = 5,
        Jump = 6,
        TogglePause = 7
    }

    /// <summary>
    /// Điều khiển nhân vật bằng giọng nói Tiếng Việt Offline (Silero VAD + Sherpa-ONNX Zipformer-vi, dự phòng Vosk/KeywordRecognizer):
    /// - Mặc định LUÔN TẮT (IsVoiceEnabled == false) khi vào màn chơi để người chơi chủ động bật.
    /// - Nút Bật/Tắt nằm gọn ngay dưới Btn_Pause góc trên phải, kèm nhãn phản hồi trạng thái.
    /// - Mỗi lần phát âm qua Silero VAD chỉ kích hoạt TỐI ĐA 1 lệnh duy nhất, tránh nhiễu kích hoạt trùng:
    ///   1: Đánh thường ("một", "chiêu một")
    ///   2: Ném lốp ("hai", "chiêu hai")
    ///   3: Xoay lốp ("ba", "chiêu ba")
    ///   4: Đập lốp ("bốn", "tư", "chiêu bốn")
    ///   5: Nhảy ("năm", "nhảy", "chiêu năm")
    ///   6: Phòng thủ ("sáu", "phòng thủ", "đỡ đòn", "chiêu sáu")
    /// </summary>
    public class VoiceCommandController : MonoBehaviour
    {
        private const int SampleRate = 16000;
        private const int MicLoopSeconds = 4;
        private const float CommandCooldownSeconds = 0.15f;
        private const float TimedBlockDuration = 1.6f;

        private const string GrammarJson =
            "[\"phòng thủ\", \"phòng\", \"thủ\", \"đỡ đòn\", " +
            "\"chiêu một\", \"chiêu hai\", \"chiêu ba\", \"chiêu bốn\", \"chiêu tư\", \"chiêu năm\", \"chiêu sáu\", " +
            "\"một\", \"hai\", \"ba\", \"bốn\", \"tư\", \"năm\", \"sáu\", " +
            "\"nhảy\", \"tạm dừng\", \"[unk]\"]";

        public static VoiceCommandController Instance { get; private set; }

        [Header("UI References (Optional - Auto Built if Null)")]
        [SerializeField] private Button toggleButton;
        [SerializeField] private Image toggleImage;
        [SerializeField] private TMP_Text statusPillText;
        [SerializeField] private GameObject statusPillRoot;

        public bool IsVoiceEnabled => _isVoiceEnabled;

        private bool _isVoiceEnabled;
        private Sprite _offSprite;
        private Sprite _onSprite;

        private CowLopPlayerController _player;
        private HUDMenuController _menuController;

        // Mic & Worker Thread State
        private string _micDevice;
        private AudioClip _micClip;
        private int _lastMicSamplePos;
        private float[] _micFloatBuffer;

        private readonly ConcurrentQueue<float[]> _floatPcmQueue = new ConcurrentQueue<float[]>();
        private readonly ConcurrentQueue<VoiceCommandKind> _commandQueue = new ConcurrentQueue<VoiceCommandKind>();
        private readonly ConcurrentQueue<string> _statusQueue = new ConcurrentQueue<string>();
        private readonly AutoResetEvent _audioSignal = new AutoResetEvent(false);

        private Thread _workerThread;
#pragma warning disable 0414
        private volatile bool _workerRunning;
#pragma warning restore 0414
        private volatile bool _isInitializingModel;
        private float _lastDispatchTime = -10f;
        private float _statusHideAt;
        private float _pulseTimer;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private WasapiMicCapture _wasapiCapture;
        private KeywordRecognizer _fallbackRecognizer;
        private bool _usingFallback;
#endif

        public static VoiceCommandController EnsureMounted(GameObject hudCanvas)
        {
            if (hudCanvas == null) hudCanvas = GameObject.Find("HUD_Canvas");
            if (hudCanvas == null) return null;

            var existing = FindFirstObjectByType<VoiceCommandController>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.EnsureUI(hudCanvas);
                return existing;
            }

            var ctrl = hudCanvas.AddComponent<VoiceCommandController>();
            ctrl.EnsureUI(hudCanvas);
            return ctrl;
        }

        private void Awake()
        {
            Instance = this;
            // Yêu cầu bắt buộc: Mặc định LUÔN TẮT khi khởi tạo màn chơi
            _isVoiceEnabled = false;
            LoadToggleSprites();
        }

        private void Start()
        {
            EnsureUI(GameObject.Find("HUD_Canvas"));
            RefreshToggleVisual();
            if (statusPillRoot != null) statusPillRoot.SetActive(false);
        }

        private void OnDisable()
        {
            StopListening();
        }

        private void OnDestroy()
        {
            StopListening();
            if (Instance == this) Instance = null;
        }

        private void LoadToggleSprites()
        {
            Sprite[] loaded = Resources.LoadAll<Sprite>("UI/ui_voice_toggle");
            if (loaded != null)
            {
                for (int i = 0; i < loaded.Length; i++)
                {
                    if (loaded[i] == null) continue;
                    string n = loaded[i].name.ToLowerInvariant();
                    if (n.Contains("off")) _offSprite = loaded[i];
                    else if (n.Contains("on")) _onSprite = loaded[i];
                }
                if (_offSprite == null && loaded.Length >= 1) _offSprite = loaded[0];
                if (_onSprite == null && loaded.Length >= 2) _onSprite = loaded[1];
            }

            if (_offSprite == null)
            {
                _offSprite = Resources.Load<Sprite>("UI/ui_voice_toggle");
            }
        }

        public void EnsureUI(GameObject hudCanvas)
        {
            if (hudCanvas == null) return;
            LoadToggleSprites();

            Transform existingBtn = hudCanvas.transform.Find("Btn_VoiceToggle");
            bool isNewBtn = existingBtn == null;
            GameObject btnGo = isNewBtn
                ? new GameObject("Btn_VoiceToggle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button))
                : existingBtn.gameObject;

            if (isNewBtn)
            {
                btnGo.transform.SetParent(hudCanvas.transform, false);
                // Đặt ngay trên HUD_Top/HUD_Notebook nhưng bên dưới các Panel lớn (Menu/Result/Guide)
                int sibling = hudCanvas.transform.childCount - 1;
                for (int i = 0; i < hudCanvas.transform.childCount - 1; i++)
                {
                    string cn = hudCanvas.transform.GetChild(i).name;
                    if (cn.Contains("Result_KO_Panel") || cn.Contains("HUD_Menu") || cn.Contains("Tutorial_Guide_Panel"))
                    {
                        sibling = i;
                        break;
                    }
                }
                btnGo.transform.SetSiblingIndex(sibling);
            }

            var rt = btnGo.GetComponent<RectTransform>();
            if (isNewBtn)
            {
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(-50f, -132f);
                rt.sizeDelta = new Vector2(64f, 64f);
            }

            toggleImage = btnGo.GetComponent<Image>();
            toggleImage.preserveAspect = true;
            toggleImage.raycastTarget = true;

            toggleButton = btnGo.GetComponent<Button>();
            toggleButton.onClick.RemoveListener(ToggleVoiceControl);
            toggleButton.onClick.AddListener(ToggleVoiceControl);

            // Tạo Status Pill gọn bên trái nút Mic (ẩn mặc định)
            Transform existingPill = hudCanvas.transform.Find("Voice_Status_Pill");
            bool isNewPill = existingPill == null;
            GameObject pillGo = isNewPill
                ? new GameObject("Voice_Status_Pill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                : existingPill.gameObject;
            if (isNewPill)
            {
                pillGo.transform.SetParent(hudCanvas.transform, false);
                pillGo.transform.SetSiblingIndex(btnGo.transform.GetSiblingIndex());
            }

            statusPillRoot = pillGo;
            var prt = pillGo.GetComponent<RectTransform>();
            if (isNewPill)
            {
                prt.anchorMin = new Vector2(1f, 1f);
                prt.anchorMax = new Vector2(1f, 1f);
                prt.pivot = new Vector2(1f, 0.5f);
                prt.anchoredPosition = new Vector2(-92f, -132f);
                prt.sizeDelta = new Vector2(220f, 36f);
            }

            var pImg = pillGo.GetComponent<Image>();
            Sprite pillSp = Resources.Load<Sprite>("UI/UI_Capsule_Dark");
            if (pillSp != null)
            {
                pImg.sprite = pillSp;
                pImg.type = Image.Type.Sliced;
            }
            pImg.color = new Color(0.08f, 0.11f, 0.14f, 0.86f);
            pImg.raycastTarget = false;

            Transform txtTf = pillGo.transform.Find("Text");
            GameObject txtGo = txtTf != null
                ? txtTf.gameObject
                : new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            if (txtTf == null) txtGo.transform.SetParent(pillGo.transform, false);

            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10f, 2f);
            trt.offsetMax = new Vector2(-10f, -2f);

            statusPillText = txtGo.GetComponent<TextMeshProUGUI>();
            statusPillText.fontSize = 18f;
            statusPillText.alignment = TextAlignmentOptions.Center;
            statusPillText.color = new Color(1f, 0.92f, 0.55f, 1f);
            statusPillText.textWrappingMode = TextWrappingModes.NoWrap;
            statusPillText.raycastTarget = false;
            TMP_FontAsset storyFont = ThemeFontManager.GetStoryFont();
            if (storyFont != null) statusPillText.font = storyFont;

            if (isNewPill) pillGo.SetActive(false);
            RefreshToggleVisual();
        }

        public void ToggleVoiceControl()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            SetVoiceEnabled(!_isVoiceEnabled);
        }

        public void SetVoiceEnabled(bool enabled)
        {
            if (_isVoiceEnabled == enabled) return;
            _isVoiceEnabled = enabled;

            if (_isVoiceEnabled)
            {
                StartListening();
            }
            else
            {
                StopListening();
                ShowStatus("Mic: Đã tắt", 1.1f);
            }
            RefreshToggleVisual();
        }

        private void RefreshToggleVisual()
        {
            if (toggleImage != null)
            {
                Sprite target = _isVoiceEnabled ? (_onSprite != null ? _onSprite : _offSprite) : _offSprite;
                if (target != null) toggleImage.sprite = target;
                toggleImage.color = _isVoiceEnabled ? Color.white : new Color(0.92f, 0.92f, 0.95f, 0.96f);
            }
            if (toggleButton != null && !_isVoiceEnabled)
            {
                toggleButton.transform.localScale = Vector3.one;
            }
        }

        private void StartListening()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            _isVoiceEnabled = false;
            RefreshToggleVisual();
            ShowStatus("WebGL chưa hỗ trợ Mic", 2.2f);
            Debug.LogWarning("[VoiceCommandController] Nền tảng WebGL chưa hỗ trợ Voice Offline / UnityEngine.Microphone.");
            return;
#else
            string[] devices = Microphone.devices;
            if (devices == null || devices.Length == 0)
            {
                _isVoiceEnabled = false;
                RefreshToggleVisual();
                ShowStatus("Không thấy Micro!", 2.2f);
                Debug.LogWarning("[VoiceCommandController] Không tìm thấy thiết bị Micro.");
                return;
            }

            _micDevice = devices[0];
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            string streamingAssetsPath = Application.streamingAssetsPath;
            string dataPath = Application.dataPath;
            string pluginsDir = SherpaVoiceSession.ResolvePluginsDir(dataPath);

            if (SherpaVoiceSession.HasModelFiles(streamingAssetsPath) && !string.IsNullOrEmpty(pluginsDir))
            {
                StartSherpaPipeline(pluginsDir, streamingAssetsPath);
                return;
            }

            string voskPath = Path.Combine(streamingAssetsPath, "Models", "vosk-vn");
            bool hasVosk = Directory.Exists(voskPath) && File.Exists(Path.Combine(voskPath, "am", "final.mdl"));
            if (hasVosk)
            {
                StartVoskPipeline(voskPath);
            }
            else
            {
                StartFallbackKeywordRecognizer();
            }
#else
            StartFallbackKeywordRecognizer();
#endif
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private void BeginMicrophoneWorker(Action workerEntry, string threadName, string initStatus)
        {
            StopListeningInternal();
            _isVoiceEnabled = true;

            while (_floatPcmQueue.TryDequeue(out _)) { }
            while (_commandQueue.TryDequeue(out _)) { }
            _audioSignal.Reset();

            bool startedWasapi = false;
            try
            {
                _wasapiCapture = new WasapiMicCapture(_floatPcmQueue, _audioSignal);
                _wasapiCapture.Start();
                startedWasapi = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VoiceCommandController] WASAPI thu âm thất bại ({ex.Message}), dùng Unity Microphone fallback.");
                try { _wasapiCapture?.Dispose(); } catch { }
                _wasapiCapture = null;
            }
            if (!startedWasapi)
            {
                _micClip = Microphone.Start(_micDevice, true, MicLoopSeconds, SampleRate);
                _lastMicSamplePos = 0;
            }

            _workerRunning = true;
            _isInitializingModel = true;

            ShowStatus(initStatus, 2.5f);

            _workerThread = new Thread(() => workerEntry())
            {
                IsBackground = true,
                Name = threadName
            };
            try { _workerThread.Priority = System.Threading.ThreadPriority.AboveNormal; } catch { }
            _workerThread.Start();
        }

        private void StartSherpaPipeline(string pluginsDir, string streamingAssetsPath)
        {
            BeginMicrophoneWorker(
                () => SherpaWorkerLoop(pluginsDir, streamingAssetsPath),
                "SherpaVoiceWorker",
                "Đang bật Mic Real-time...");
        }

        private void SherpaWorkerLoop(string pluginsDir, string streamingAssetsPath)
        {
            SherpaVoiceSession session = null;
            try
            {
                session = new SherpaVoiceSession(pluginsDir, streamingAssetsPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VoiceCommandController] Không thể nạp Sherpa-ONNX ({ex.Message}). Chuyển sang Vosk/fallback.");
                _isInitializingModel = false;
                _statusQueue.Enqueue("__VOSK_OR_FALLBACK__");
                return;
            }

            _isInitializingModel = false;
            _statusQueue.Enqueue("Mic Real-time: Sẵn sàng (1-6)!");

            try
            {
                while (_workerRunning)
                {
                    if (_floatPcmQueue.TryDequeue(out float[] chunk))
                    {
                        session.AcceptSamplesRealtime(chunk, chunk.Length, (candidateText, isFinalSegment) =>
                        {
                            if (!isFinalSegment && ShouldDeferEarlyCommit(candidateText)) return false;

                            VoiceCommandKind cmd = ParseCommand(candidateText);
                            if (cmd == VoiceCommandKind.None) return false;

                            _commandQueue.Enqueue(cmd);
                            return true;
                        });
                    }
                    else
                    {
                        _audioSignal.WaitOne(40);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VoiceCommandController] Lỗi luồng Sherpa ({ex.Message}).");
            }
            finally
            {
                session.Dispose();
            }
        }

        private void StartVoskPipeline(string modelPath)
        {
            BeginMicrophoneWorker(
                () => VoskWorkerLoop(modelPath),
                "VoskVoiceWorker",
                "Đang bật Mic...");
        }

        private void VoskWorkerLoop(string modelPath)
        {
            IntPtr model = IntPtr.Zero;
            IntPtr recognizer = IntPtr.Zero;
            try
            {
                VoskNative.PrepareDllSearchPath();
                VoskNative.vosk_set_log_level(-1);
                model = VoskNative.CreateModel(modelPath);
                if (model != IntPtr.Zero)
                {
                    recognizer = VoskNative.CreateGrammarRecognizer(model, SampleRate, GrammarJson);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VoiceCommandController] Không thể nạp libvosk ({ex.Message}). Chuyển sang fallback.");
                _isInitializingModel = false;
                _statusQueue.Enqueue("__FALLBACK__");
                return;
            }

            _isInitializingModel = false;
            if (model == IntPtr.Zero || recognizer == IntPtr.Zero)
            {
                if (recognizer != IntPtr.Zero) VoskNative.vosk_recognizer_free(recognizer);
                if (model != IntPtr.Zero) VoskNative.vosk_model_free(model);
                _statusQueue.Enqueue("__FALLBACK__");
                return;
            }

            _statusQueue.Enqueue("Mic: Sẵn sàng (1-6)!");

            try
            {
                while (_workerRunning)
                {
                    if (_floatPcmQueue.TryDequeue(out float[] fchunk))
                    {
                        short[] chunk = FloatToInt16Pcm(fchunk);
                        // Chỉ xử lý khi câu nói kết thúc (không dùng partial để tránh bắn trùng nhiều lệnh)
                        int accepted = VoskNative.vosk_recognizer_accept_waveform_s(recognizer, chunk, chunk.Length);
                        if (accepted != 0)
                        {
                            string json = VoskNative.GetResult(recognizer);
                            string text = ExtractJsonField(json, "text");
                            VoiceCommandKind cmd = ParseCommand(text);
                            if (cmd != VoiceCommandKind.None)
                            {
                                _commandQueue.Enqueue(cmd);
                                VoskNative.vosk_recognizer_reset(recognizer);
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(15);
                    }
                }
            }
            finally
            {
                if (recognizer != IntPtr.Zero) VoskNative.vosk_recognizer_free(recognizer);
                if (model != IntPtr.Zero) VoskNative.vosk_model_free(model);
            }
        }
#endif

        private static short[] FloatToInt16Pcm(float[] samples)
        {
            short[] pcm = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                float clamped = samples[i] < -1f ? -1f : (samples[i] > 1f ? 1f : samples[i]);
                pcm[i] = (short)Math.Round(clamped * 32767f);
            }
            return pcm;
        }

        private void StartFallbackKeywordRecognizer()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                _usingFallback = true;
                string[] keywords = { "phong thu", "chieu mot", "chieu hai", "chieu ba", "chieu bon", "chieu nam", "chieu sau", "nhay", "block", "one", "two", "three", "four", "five", "six", "jump" };
                _fallbackRecognizer = new KeywordRecognizer(keywords, ConfidenceLevel.Low);
                _fallbackRecognizer.OnPhraseRecognized += args =>
                {
                    VoiceCommandKind cmd = ParseCommand(args.text);
                    if (cmd != VoiceCommandKind.None) _commandQueue.Enqueue(cmd);
                };
                _fallbackRecognizer.Start();
                ShowStatus("Mic (EN): Sẵn sàng!", 1.8f);
            }
            catch (Exception ex)
            {
                _isVoiceEnabled = false;
                RefreshToggleVisual();
                ShowStatus("Lỗi Mic Windows", 2f);
                Debug.LogWarning($"[VoiceCommandController] Fallback KeywordRecognizer error: {ex.Message}");
            }
#else
            _isVoiceEnabled = false;
            RefreshToggleVisual();
            ShowStatus("Thiếu model Vosk", 2f);
#endif
        }

        private void StopListening()
        {
            StopListeningInternal();
        }

        private void StopListeningInternal()
        {
            _workerRunning = false;
            _isInitializingModel = false;
            try { _audioSignal.Set(); } catch { }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_wasapiCapture != null)
            {
                try { _wasapiCapture.Dispose(); } catch { }
                _wasapiCapture = null;
            }
#endif

#if !UNITY_WEBGL || UNITY_EDITOR
            if (_micClip != null)
            {
                try
                {
                    if (Microphone.IsRecording(_micDevice)) Microphone.End(_micDevice);
                }
                catch { }
                _micClip = null;
            }
#endif

            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Join(250);
            }
            _workerThread = null;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_fallbackRecognizer != null)
            {
                try
                {
                    if (_fallbackRecognizer.IsRunning) _fallbackRecognizer.Stop();
                    _fallbackRecognizer.Dispose();
                }
                catch { }
                _fallbackRecognizer = null;
            }
            _usingFallback = false;
#endif
        }

        private void Update()
        {
            if (statusPillRoot != null && statusPillRoot.activeSelf && Time.unscaledTime > _statusHideAt && !_isInitializingModel)
            {
                statusPillRoot.SetActive(false);
            }

            while (_statusQueue.TryDequeue(out string msg))
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                if (msg == "__VOSK_OR_FALLBACK__")
                {
                    StopListeningInternal();
                    if (_isVoiceEnabled)
                    {
                        string voskPath = Path.Combine(Application.streamingAssetsPath, "Models", "vosk-vn");
                        if (Directory.Exists(voskPath) && File.Exists(Path.Combine(voskPath, "am", "final.mdl")))
                            StartVoskPipeline(voskPath);
                        else
                            StartFallbackKeywordRecognizer();
                    }
                }
                else
#endif
                if (msg == "__FALLBACK__")
                {
                    StopListeningInternal();
                    if (_isVoiceEnabled) StartFallbackKeywordRecognizer();
                }
                else
                {
                    ShowStatus(msg, 1.6f);
                }
            }

            if (_isVoiceEnabled)
            {
                _pulseTimer += Time.unscaledDeltaTime * 5f;
                if (toggleButton != null)
                {
                    float s = 1f + 0.045f * Mathf.Sin(_pulseTimer);
                    toggleButton.transform.localScale = new Vector3(s, s, 1f);
                }

                PollMicrophoneAudio();
            }

            while (_commandQueue.TryDequeue(out VoiceCommandKind cmd))
            {
                DispatchCommand(cmd);
            }
        }

        private void PollMicrophoneAudio()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return;
#else
            if (_micClip == null || !_workerRunning || _isInitializingModel) return;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_usingFallback) return;
#endif
            int currentPos = Microphone.GetPosition(_micDevice);
            if (currentPos < 0 || currentPos == _lastMicSamplePos) return;

            int totalSamples = _micClip.samples;
            int sampleCount = currentPos >= _lastMicSamplePos
                ? (currentPos - _lastMicSamplePos)
                : (totalSamples - _lastMicSamplePos + currentPos);

            if (sampleCount <= 0) return;

            if (_micFloatBuffer == null || _micFloatBuffer.Length < sampleCount)
            {
                _micFloatBuffer = new float[sampleCount];
            }

            int channels = _micClip.channels;
            float[] raw = channels == 1 ? _micFloatBuffer : new float[sampleCount * channels];
            _micClip.GetData(raw, _lastMicSamplePos);
            _lastMicSamplePos = currentPos;

            float[] mono = new float[sampleCount];
            if (channels == 1)
            {
                Array.Copy(raw, 0, mono, 0, sampleCount);
            }
            else
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    mono[i] = raw[i * channels];
                }
            }

            while (_floatPcmQueue.Count > 12 && _floatPcmQueue.TryDequeue(out _)) { }
            _floatPcmQueue.Enqueue(mono);
            _audioSignal.Set();
#endif
        }

        /// <summary>
        /// Thực thi lệnh thoại lên nhân vật CowLop hoặc Pause Menu.
        /// Có thể gọi trực tiếp để kiểm thử bằng code hoặc từ ASR worker.
        /// </summary>
        public bool DispatchCommand(VoiceCommandKind cmd)
        {
            if (cmd == VoiceCommandKind.None) return false;
            if (Time.unscaledTime - _lastDispatchTime < CommandCooldownSeconds) return false;
            _lastDispatchTime = Time.unscaledTime;

            if (cmd == VoiceCommandKind.TogglePause)
            {
                if (_menuController == null) _menuController = FindFirstObjectByType<HUDMenuController>(FindObjectsInactive.Include);
                if (_menuController != null)
                {
                    if (_menuController.menuPanel != null && _menuController.menuPanel.activeSelf)
                        _menuController.OnPlayClicked();
                    else
                        _menuController.OpenPauseMenu();
                    ShowStatus("Mic: Tạm dừng!", 1.4f);
                    return true;
                }
                return false;
            }

            if (_player == null) _player = FindFirstObjectByType<CowLopPlayerController>();
            if (_player == null) return false;

            switch (cmd)
            {
                case VoiceCommandKind.Block:
                    _player.TriggerTimedBlock(TimedBlockDuration);
                    ShowStatus("Mic: 6 - Phòng Thủ!", 1.5f);
                    return true;
                case VoiceCommandKind.Skill1_Attack:
                    _player.TriggerAttack();
                    ShowStatus("Mic: 1 - Đánh!", 1.3f);
                    return true;
                case VoiceCommandKind.Skill2_TireThrow:
                    _player.TriggerTireThrow();
                    ShowStatus("Mic: 2 - Ném Lốp!", 1.3f);
                    return true;
                case VoiceCommandKind.Skill3_TireSpin:
                    _player.TriggerTireSpin();
                    ShowStatus("Mic: 3 - Xoay Lốp!", 1.3f);
                    return true;
                case VoiceCommandKind.Skill4_JumpSlam:
                    _player.TriggerJumpSlam();
                    ShowStatus("Mic: 4 - Đập Lốp!", 1.3f);
                    return true;
                case VoiceCommandKind.Jump:
                    _player.TriggerJump();
                    ShowStatus("Mic: 5 - Nhảy!", 1.2f);
                    return true;
                default:
                    return false;
            }
        }

        public void ShowStatus(string text, float durationSeconds)
        {
            if (statusPillRoot == null || statusPillText == null) return;
            statusPillText.text = text;
            statusPillRoot.SetActive(true);
            _statusHideAt = Time.unscaledTime + durationSeconds;
        }

        private static readonly (VoiceCommandKind cmd, string[] phrases, string[] tokens)[] CommandRules =
        {
            (VoiceCommandKind.Block,
                new[] { "phong thu", "do don", "chieu sau", "chieu 6" },
                new[] { "sau", "6", "six", "thu", "phong", "block" }),
            (VoiceCommandKind.Jump,
                new[] { "chieu nam", "chieu lam", "chieu 5" },
                new[] { "nhay", "nam", "5", "five", "jump" }),
            (VoiceCommandKind.Skill4_JumpSlam,
                new[] { "chieu bon", "chieu tu", "chieu 4", "dap lop" },
                new[] { "bon", "tu", "4", "four" }),
            (VoiceCommandKind.Skill3_TireSpin,
                new[] { "chieu ba", "chieu 3", "yeu ba", "xoay lop" },
                new[] { "ba", "3", "three" }),
            (VoiceCommandKind.Skill2_TireThrow,
                new[] { "chieu hai", "chieu 2", "tieu hai", "nem lop" },
                new[] { "hai", "2", "two" }),
            (VoiceCommandKind.Skill1_Attack,
                new[] { "chieu mot", "chieu 1", "danh thuong" },
                new[] { "mot", "1", "one", "danh" }),
            (VoiceCommandKind.TogglePause,
                new[] { "tam dung" },
                new[] { "pause" })
        };

        private static string NormalizeVoiceText(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;
            return RemoveVietnameseDiacritics(rawText)
                .ToLowerInvariant()
                .Replace(",", " ")
                .Replace(".", " ")
                .Replace("!", " ")
                .Replace("?", " ")
                .Trim();
        }

        /// <summary>
        /// Khi giải mã sớm giữa câu nói, tránh chốt vội các tiền tố 2 âm tiết (ví dụ mới nói "chiêu" hoặc "phòng").
        /// </summary>
        public static bool ShouldDeferEarlyCommit(string rawText)
        {
            string norm = NormalizeVoiceText(rawText);
            return norm == "chieu" || norm == "tieu" || norm == "yeu" || norm == "phong" || norm == "tam";
        }

        /// <summary>
        /// Phân giải câu thoại tiếng Việt (1..6, tên chiêu, nhảy, phòng thủ) thành DUY NHẤT 1 VoiceCommandKind.
        /// Ưu tiên cụm từ đầy đủ trước khi so token đơn để tránh khớp nhầm đa nghĩa.
        /// </summary>
        public static VoiceCommandKind ParseCommand(string rawText)
        {
            string norm = NormalizeVoiceText(rawText);
            if (string.IsNullOrEmpty(norm) || norm == "[unk]") return VoiceCommandKind.None;

            string[] tokens = norm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Bước 1: So khớp cụm đa âm tiết ("chiêu một".."chiêu sáu", "phòng thủ", "tạm dừng")
            for (int i = 0; i < CommandRules.Length; i++)
            {
                string[] phrases = CommandRules[i].phrases;
                for (int j = 0; j < phrases.Length; j++)
                {
                    if (norm.Contains(phrases[j])) return CommandRules[i].cmd;
                }
            }

            // Bước 2: Biến thể phát âm ngắn thường gặp từ Zipformer cho câu 1 âm tiết ("hai"->"hay", "nhảy"->"nhỉ")
            if (norm == "hay") return VoiceCommandKind.Skill2_TireThrow;
            if (norm == "nhi") return VoiceCommandKind.Jump;

            // Bước 3: So khớp từng token độc lập (tối đa 1 lệnh)
            for (int i = 0; i < CommandRules.Length; i++)
            {
                string[] allowed = CommandRules[i].tokens;
                for (int j = 0; j < allowed.Length; j++)
                {
                    if (ContainsToken(tokens, allowed[j])) return CommandRules[i].cmd;
                }
            }

            return VoiceCommandKind.None;
        }

        private static bool ContainsToken(string[] tokens, string target)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i] == target) return true;
            }
            return false;
        }

        private static string RemoveVietnameseDiacritics(string input)
        {
            string decomposed = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            for (int i = 0; i < decomposed.Length; i++)
            {
                char c = decomposed[i];
                if (c == 'đ') c = 'd';
                else if (c == 'Đ') c = 'D';
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string ExtractJsonField(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return string.Empty;
            string needle = $"\"{key}\"";
            int idx = json.IndexOf(needle, StringComparison.Ordinal);
            if (idx < 0) return string.Empty;
            int colon = json.IndexOf(':', idx + needle.Length);
            if (colon < 0) return string.Empty;
            int q1 = json.IndexOf('"', colon + 1);
            if (q1 < 0) return string.Empty;
            int q2 = json.IndexOf('"', q1 + 1);
            if (q2 <= q1) return string.Empty;
            return json.Substring(q1 + 1, q2 - q1 - 1);
        }
    }
}
