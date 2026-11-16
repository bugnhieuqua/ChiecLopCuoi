#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections.Concurrent;
using System.Threading;
using NAudio.Wave;

namespace ChiecLopCuoi.Audio
{
    /// <summary>
    /// Bộ thu âm thanh độ trễ cực thấp (20ms/khung) qua thư viện mã nguồn mở NAudio (NAudio.WinMM WaveInEvent, MIT).
    /// Thu trực tiếp 16kHz 16-bit Mono PCM ổn định trên Unity Mono (tránh lỗi COM marshaling của WasapiCapture)
    /// và đánh thức luồng nhận diện giọng nói tức thì qua AutoResetEvent (0ms polling delay).
    /// </summary>
    public sealed class WasapiMicCapture : IDisposable
    {
        public const int TargetSampleRate = 16000;
        private const int RequestedBufferMs = 20;
        private const int RequestedBuffers = 4;

        private WaveInEvent _capture;
        private bool _disposed;

        private readonly ConcurrentQueue<float[]> _outQueue;
        private readonly AutoResetEvent _signalEvent;
        private readonly int _maxBufferedChunks;

        public WasapiMicCapture(ConcurrentQueue<float[]> outQueue, AutoResetEvent signalEvent, int maxBufferedChunks = 24)
        {
            _outQueue = outQueue ?? throw new ArgumentNullException(nameof(outQueue));
            _signalEvent = signalEvent ?? throw new ArgumentNullException(nameof(signalEvent));
            _maxBufferedChunks = maxBufferedChunks;

            if (WaveInEvent.DeviceCount <= 0)
            {
                throw new InvalidOperationException("Không tìm thấy thiết bị microphone đầu vào.");
            }

            _capture = new WaveInEvent
            {
                DeviceNumber = 0,
                WaveFormat = new WaveFormat(TargetSampleRate, 16, 1),
                BufferMilliseconds = RequestedBufferMs,
                NumberOfBuffers = RequestedBuffers
            };
            _capture.DataAvailable += OnDataAvailable;
        }

        public void Start()
        {
            if (_disposed || _capture == null) return;
            _capture.StartRecording();
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (_disposed || e == null || e.BytesRecorded < 2) return;

            int sampleCount = e.BytesRecorded / 2;
            float[] chunk = new float[sampleCount];
            byte[] buf = e.Buffer;

            for (int i = 0; i < sampleCount; i++)
            {
                int pos = i * 2;
                short s = (short)(buf[pos] | (buf[pos + 1] << 8));
                chunk[i] = s / 32768f;
            }

            while (_outQueue.Count >= _maxBufferedChunks && _outQueue.TryDequeue(out _)) { }
            _outQueue.Enqueue(chunk);
            _signalEvent.Set();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_capture != null)
            {
                try { _capture.DataAvailable -= OnDataAvailable; } catch { }
                try { _capture.StopRecording(); } catch { }
                try { _capture.Dispose(); } catch { }
                _capture = null;
            }
        }
    }
}
#endif
