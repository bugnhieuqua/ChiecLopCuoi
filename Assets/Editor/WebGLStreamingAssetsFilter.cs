#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ChiecLopCuoi.Editor
{
    /// <summary>
    /// Tự động tạm ẩn thư mục Assets/StreamingAssets/Models (chứa ~84 MB model Voice Offline chỉ chạy trên Windows)
    /// khi build target WebGL, giữ nguyên game_runtime.db, và khôi phục lại ngay sau khi build xong hoặc khi Editor reload.
    /// Không ảnh hưởng tới Play Mode trên Editor hay bản build Standalone Windows.
    /// </summary>
    [InitializeOnLoad]
    public sealed class WebGLStreamingAssetsFilter : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string ModelsAssetPath = "Assets/StreamingAssets/Models";
        private static readonly string BackupRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "WebGLStreamingAssetsBackup"));
        private static readonly string BackupModelsDir = Path.Combine(BackupRoot, "Models");
        private static readonly string BackupModelsMeta = Path.Combine(BackupRoot, "Models.meta");

        public int callbackOrder => 0;

        static WebGLStreamingAssetsFilter()
        {
            RestoreModelsIfBackedUp();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.WebGL)
            {
                return;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string modelsAbs = Path.Combine(projectRoot, "Assets", "StreamingAssets", "Models");
            string metaAbs = modelsAbs + ".meta";

            if (!Directory.Exists(modelsAbs))
            {
                return;
            }

            if (Directory.Exists(BackupModelsDir))
            {
                Debug.LogWarning("[WebGLStreamingAssetsFilter] Phát hiện bản backup cũ chưa khôi phục, bỏ qua bước di chuyển để tránh mất dữ liệu.");
                return;
            }

            Directory.CreateDirectory(BackupRoot);
            Directory.Move(modelsAbs, BackupModelsDir);
            if (File.Exists(metaAbs))
            {
                File.Move(metaAbs, BackupModelsMeta);
            }

            AssetDatabase.Refresh();
            Debug.Log("[WebGLStreamingAssetsFilter] Đã tạm ẩn Assets/StreamingAssets/Models (~84 MB) khỏi bản build WebGL.");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report == null || report.summary.platform != BuildTarget.WebGL)
            {
                return;
            }

            RestoreModelsIfBackedUp(refreshNow: false);
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
                {
                    AssetDatabase.Refresh();
                }
            };
        }

        public static void RestoreModelsIfBackedUp()
        {
            RestoreModelsIfBackedUp(refreshNow: true);
        }

        private static void RestoreModelsIfBackedUp(bool refreshNow)
        {
            if (!Directory.Exists(BackupModelsDir))
            {
                return;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string modelsAbs = Path.Combine(projectRoot, "Assets", "StreamingAssets", "Models");
            string metaAbs = modelsAbs + ".meta";

            if (!Directory.Exists(modelsAbs))
            {
                string streamingDir = Path.GetDirectoryName(modelsAbs);
                if (!string.IsNullOrEmpty(streamingDir) && !Directory.Exists(streamingDir))
                {
                    Directory.CreateDirectory(streamingDir);
                }

                Directory.Move(BackupModelsDir, modelsAbs);
                if (File.Exists(BackupModelsMeta) && !File.Exists(metaAbs))
                {
                    File.Move(BackupModelsMeta, metaAbs);
                }

                if (Directory.Exists(BackupRoot) && Directory.GetFileSystemEntries(BackupRoot).Length == 0)
                {
                    Directory.Delete(BackupRoot, false);
                }

                if (refreshNow)
                {
                    AssetDatabase.Refresh();
                }

                Debug.Log("[WebGLStreamingAssetsFilter] Đã khôi phục Assets/StreamingAssets/Models.");
            }
        }
    }
}
#endif
