#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace ChiecLopCuoi.Audio
{
    /// <summary>
    /// Lớp P/Invoke thuần giao tiếp trực tiếp với thư viện C libvosk.dll (x86_64).
    /// Toàn bộ chuỗi truyền nhận được mã hóa UTF-8 để giữ nguyên dấu tiếng Việt.
    /// </summary>
    internal static class VoskNative
    {
        private const string DllName = "libvosk";
        private static bool _dllPathPrepared;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void vosk_set_log_level(int logLevel);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "vosk_model_new")]
        private static extern IntPtr vosk_model_new_raw(byte[] modelPathUtf8);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void vosk_model_free(IntPtr model);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "vosk_recognizer_new_grm")]
        private static extern IntPtr vosk_recognizer_new_grm_raw(IntPtr model, float sampleRate, byte[] grammarJsonUtf8);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void vosk_recognizer_free(IntPtr recognizer);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void vosk_recognizer_reset(IntPtr recognizer);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int vosk_recognizer_accept_waveform_s(IntPtr recognizer, short[] data, int length);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "vosk_recognizer_result")]
        private static extern IntPtr vosk_recognizer_result_raw(IntPtr recognizer);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "vosk_recognizer_partial_result")]
        private static extern IntPtr vosk_recognizer_partial_result_raw(IntPtr recognizer);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "vosk_recognizer_final_result")]
        private static extern IntPtr vosk_recognizer_final_result_raw(IntPtr recognizer);

        internal static void PrepareDllSearchPath()
        {
            if (_dllPathPrepared) return;
            _dllPathPrepared = true;

#if UNITY_EDITOR_WIN
            string pluginsPath = Path.Combine(Application.dataPath, "Plugins", "x86_64");
            if (Directory.Exists(pluginsPath))
            {
                try { SetDllDirectory(pluginsPath); } catch { }
            }
#elif UNITY_STANDALONE_WIN
            string pluginsPath = Path.Combine(Application.dataPath, "Plugins", "x86_64");
            if (Directory.Exists(pluginsPath))
            {
                try { SetDllDirectory(pluginsPath); } catch { }
            }
#endif
        }

        private static byte[] ToUtf8Z(string text)
        {
            if (string.IsNullOrEmpty(text)) return new byte[] { 0 };
            int len = Encoding.UTF8.GetByteCount(text);
            byte[] buf = new byte[len + 1];
            Encoding.UTF8.GetBytes(text, 0, text.Length, buf, 0);
            buf[len] = 0;
            return buf;
        }

        private static string PtrToUtf8String(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return string.Empty;
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0) len++;
            if (len == 0) return string.Empty;
            byte[] buf = new byte[len];
            Marshal.Copy(ptr, buf, 0, len);
            return Encoding.UTF8.GetString(buf);
        }

        internal static IntPtr CreateModel(string modelPath)
        {
            PrepareDllSearchPath();
            return vosk_model_new_raw(ToUtf8Z(modelPath));
        }

        internal static IntPtr CreateGrammarRecognizer(IntPtr model, float sampleRate, string grammarJson)
        {
            if (model == IntPtr.Zero) return IntPtr.Zero;
            return vosk_recognizer_new_grm_raw(model, sampleRate, ToUtf8Z(grammarJson));
        }

        internal static string GetPartialResult(IntPtr recognizer)
        {
            return recognizer == IntPtr.Zero ? string.Empty : PtrToUtf8String(vosk_recognizer_partial_result_raw(recognizer));
        }

        internal static string GetResult(IntPtr recognizer)
        {
            return recognizer == IntPtr.Zero ? string.Empty : PtrToUtf8String(vosk_recognizer_result_raw(recognizer));
        }

        internal static string GetFinalResult(IntPtr recognizer)
        {
            return recognizer == IntPtr.Zero ? string.Empty : PtrToUtf8String(vosk_recognizer_final_result_raw(recognizer));
        }
    }
}
#endif
