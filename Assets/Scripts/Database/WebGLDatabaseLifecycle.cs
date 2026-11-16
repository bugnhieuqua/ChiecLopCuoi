using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ChiecLopCuoi.Database
{
    /// <summary>
    /// Quản lý vòng đời CSDL đa nền tảng (đặc biệt WebGL):
    /// 1. Trên WebGL, tải game_runtime.db từ StreamingAssets qua UnityWebRequest để bổ sung dữ liệu mẫu lần đầu vào idbvfs/IndexedDB.
    /// 2. Khi Pause/FocusOut/Destroy, đóng kết nối SQLite an toàn để giải phóng khóa và đẩy toàn bộ thay đổi xuống lưu trữ bền vững.
    /// </summary>
    public sealed class WebGLDatabaseLifecycle : MonoBehaviour
    {
        private static WebGLDatabaseLifecycle _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRegistered()
        {
            if (_instance != null) return;

            var go = new GameObject("[DatabaseLifecycle]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<WebGLDatabaseLifecycle>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DatabaseManager.Initialize();
        }

        private IEnumerator Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string streamingUrl = Path.Combine(Application.streamingAssetsPath, "game_runtime.db");
            using (var request = UnityWebRequest.Get(streamingUrl))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success && request.downloadHandler != null)
                {
                    DatabaseManager.ApplySeedBytesFromStreamingAssets(request.downloadHandler.data);
                }
                else
                {
                    Debug.LogWarning($"[WebGLDatabaseLifecycle] Không thể tải StreamingAssets DB ({request.error}); tiếp tục dùng schema khởi tạo mặc định.");
                }
            }
#else
            yield break;
#endif
        }

        private void OnApplicationPause(bool pauseStatus)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (pauseStatus)
            {
                DatabaseManager.Close();
            }
            else if (!DatabaseManager.IsInitialized)
            {
                DatabaseManager.Initialize();
            }
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!hasFocus)
            {
                DatabaseManager.Close();
            }
            else if (!DatabaseManager.IsInitialized)
            {
                DatabaseManager.Initialize();
            }
#endif
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                DatabaseManager.Close();
                _instance = null;
            }
        }
    }
}
