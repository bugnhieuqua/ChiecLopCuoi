#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ChiecLopCuoi.Editor
{
    /// <summary>
    /// Áp dụng các tối ưu WebGL an toàn (Chặng 1) mà KHÔNG làm thay đổi cấu trúc game, kích thước world,
    /// pivot/9-slice sprite, font Tiếng Việt hay chất lượng bản chạy Editor/Windows Standalone:
    /// 1. Chỉ thêm Platform Override cho "WebGL" trên Audio nặng (Vorbis 0.65-0.70).
    /// 2. Chỉ thêm Platform Override cho "WebGL" trên Texture Environments/UI (Crunch Compression 75, giữ nguyên MaxSize).
    ///    Không chạm vào Characters/Enemies để giữ nguyên độ sắc nét nhân vật.
    /// 3. Cấu hình WebGL PlayerSettings theo Bước 1 (Brotli, OptimizeSize, Master/LTO, High Stripping + link.xml).
    /// </summary>
    public static class WebGLAssetOptimizationUtility
    {
        private const string WebGLPlatformName = "WebGL";
        private const long MinAudioBytesForWebGLOverride = 100 * 1024;
        private const long LargeAudioBytesThreshold = 500 * 1024;
        private const long MinTextureBytesForWebGLCrunch = 200 * 1024;

        private static readonly string[] TextureTargetFolders =
        {
            "Assets/Art/Environments",
            "Assets/Art/UI",
            "Assets/Resources/UI"
        };

        private static readonly string[] AudioTargetFolders =
        {
            "Assets/Audio",
            "Assets/Resources/Audio"
        };

        [MenuItem("Tools/WebGL/Apply Safe Chặng 1 Optimizations")]
        public static string ApplyAllSafeOptimizations()
        {
            ApplyWebGLPlayerSettings();
            int audioUpdated = ApplyWebGLAudioOverrides();
            int textureUpdated = ApplyWebGLTextureCrunchOverrides();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary = $"[WebGLOpt] Hoàn tất Chặng 1 an toàn: PlayerSettings=OK, AudioOverrides={audioUpdated}, TextureCrunchOverrides={textureUpdated}";
            Debug.Log(summary);
            return summary;
        }

        [MenuItem("Tools/WebGL/Revert WebGL Asset Overrides")]
        public static void RevertWebGLOverrides()
        {
            int audioCleared = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", AudioTargetFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as AudioImporter;
                if (imp != null && imp.ContainsSampleSettingsOverride(WebGLPlatformName))
                {
                    if (imp.ClearSampleSettingOverride(WebGLPlatformName))
                    {
                        imp.SaveAndReimport();
                        audioCleared++;
                    }
                }
            }

            int texCleared = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", TextureTargetFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp != null)
                {
                    var settings = imp.GetPlatformTextureSettings(WebGLPlatformName);
                    if (settings.overridden)
                    {
                        imp.ClearPlatformTextureSettings(WebGLPlatformName);
                        imp.SaveAndReimport();
                        texCleared++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[WebGLOpt] Đã gỡ override WebGL: Audio={audioCleared}, Texture={texCleared}");
        }

        public static void ApplyWebGLPlayerSettings()
        {
            var webglTarget = NamedBuildTarget.WebGL;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(webglTarget, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(webglTarget, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCompilerConfiguration(webglTarget, Il2CppCompilerConfiguration.Master);
            PlayerSettings.SetApiCompatibilityLevel(webglTarget, ApiCompatibilityLevel.NET_Standard_2_0);
        }

        public static int ApplyWebGLAudioOverrides()
        {
            int updated = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", AudioTargetFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!File.Exists(path)) continue;

                long len = new FileInfo(path).Length;
                if (len < MinAudioBytesForWebGLOverride) continue;

                var imp = AssetImporter.GetAtPath(path) as AudioImporter;
                if (imp == null) continue;

                bool isLong = path.Contains("/BGM/") || len >= LargeAudioBytesThreshold;
                AudioClipLoadType targetLoad = isLong
                    ? AudioClipLoadType.CompressedInMemory
                    : AudioClipLoadType.DecompressOnLoad;
                float targetQuality = isLong ? 0.65f : 0.70f;

                var current = imp.ContainsSampleSettingsOverride(WebGLPlatformName)
                    ? imp.GetOverrideSampleSettings(WebGLPlatformName)
                    : imp.defaultSampleSettings;

                if (imp.ContainsSampleSettingsOverride(WebGLPlatformName) &&
                    current.compressionFormat == AudioCompressionFormat.Vorbis &&
                    current.loadType == targetLoad &&
                    Mathf.Abs(current.quality - targetQuality) < 0.01f)
                {
                    continue;
                }

                var wgl = imp.defaultSampleSettings;
                wgl.compressionFormat = AudioCompressionFormat.Vorbis;
                wgl.loadType = targetLoad;
                wgl.quality = targetQuality;

                if (imp.SetOverrideSampleSettings(WebGLPlatformName, wgl))
                {
                    imp.SaveAndReimport();
                    updated++;
                }
            }
            return updated;
        }

        public static int ApplyWebGLTextureCrunchOverrides()
        {
            int updated = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", TextureTargetFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!File.Exists(path)) continue;

                long len = new FileInfo(path).Length;
                if (len < MinTextureBytesForWebGLCrunch) continue;

                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;

                imp.GetSourceTextureWidthAndHeight(out int w, out int h);
                if (w <= 0 || h <= 0 || (w % 4 != 0) || (h % 4 != 0))
                {
                    // Crunch đòi hỏi kích thước chia hết cho 4; bỏ qua an toàn nếu ảnh không chia hết cho 4.
                    continue;
                }

                var existing = imp.GetPlatformTextureSettings(WebGLPlatformName);
                if (existing.overridden &&
                    existing.crunchedCompression &&
                    existing.compressionQuality == 75 &&
                    existing.textureCompression == TextureImporterCompression.Compressed &&
                    existing.maxTextureSize == imp.maxTextureSize)
                {
                    continue;
                }

                var wgl = imp.GetPlatformTextureSettings(WebGLPlatformName);
                wgl.name = WebGLPlatformName;
                wgl.overridden = true;
                wgl.maxTextureSize = imp.maxTextureSize; // Giữ nguyên maxTextureSize để không đổi world scale sprite
                wgl.format = TextureImporterFormat.Automatic;
                wgl.textureCompression = TextureImporterCompression.Compressed;
                wgl.crunchedCompression = true;
                wgl.compressionQuality = 75;

                imp.SetPlatformTextureSettings(wgl);
                imp.SaveAndReimport();
                updated++;
            }
            return updated;
        }

        [MenuItem("Tools/WebGL/Build WebGL Baseline (Measure Size)")]
        public static string BuildWebGLBaseline()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                string skipped = "[WebGLOpt] Skipped BuildWebGLBaseline: Editor is compiling or updating.";
                Debug.LogWarning(skipped);
                return skipped;
            }

            ApplyWebGLPlayerSettings();

            var enabledScenes = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s != null && s.enabled && !string.IsNullOrEmpty(s.path))
                {
                    enabledScenes.Add(s.path);
                }
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildDir = Path.Combine(projectRoot, "Builds", "WebGLBaseline");
            if (Directory.Exists(buildDir))
            {
                Directory.Delete(buildDir, true);
            }
            Directory.CreateDirectory(buildDir);

            var options = new BuildPlayerOptions
            {
                scenes = enabledScenes.ToArray(),
                locationPathName = buildDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"BuildResult={report.summary.result}");
            sb.AppendLine($"TotalErrors={report.summary.totalErrors}, TotalWarnings={report.summary.totalWarnings}");
            sb.AppendLine($"TotalTime={report.summary.totalTime.TotalSeconds:F1}s");
            sb.AppendLine($"SummaryTotalSizeMB={report.summary.totalSize / (1024.0 * 1024.0):F2}");

            if (Directory.Exists(buildDir))
            {
                var allFiles = Directory.GetFiles(buildDir, "*", SearchOption.AllDirectories);
                long diskBytes = 0;
                foreach (var f in allFiles) diskBytes += new FileInfo(f).Length;
                sb.AppendLine($"OutputFilesCount={allFiles.Length}, OutputDiskMB={diskBytes / (1024.0 * 1024.0):F2}");
                sb.AppendLine("--- Output Files ---");
                System.Array.Sort(allFiles, (a, b) => new FileInfo(b).Length.CompareTo(new FileInfo(a).Length));
                int shown = 0;
                foreach (var f in allFiles)
                {
                    if (shown++ >= 30) break;
                    string rel = f.Substring(buildDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    sb.AppendLine($"  {new FileInfo(f).Length / 1024.0:F1} KB | {rel}");
                }
            }

            if (report.packedAssets != null && report.packedAssets.Length > 0)
            {
                sb.AppendLine("--- Top Packed Assets (Uncompressed Estimate in Report) ---");
                var entries = new System.Collections.Generic.List<UnityEditor.Build.Reporting.PackedAssetInfo>();
                foreach (var pa in report.packedAssets)
                {
                    if (pa.contents != null)
                    {
                        entries.AddRange(pa.contents);
                    }
                }
                entries.Sort((a, b) => b.packedSize.CompareTo(a.packedSize));
                for (int i = 0; i < Mathf.Min(35, entries.Count); i++)
                {
                    var e = entries[i];
                    sb.AppendLine($"  {e.packedSize / 1024.0:F1} KB | {e.type} | {e.sourceAssetPath}");
                }
            }

            string reportFile = Path.Combine(projectRoot, "Builds", "WebGLBaseline_report.txt");
            File.WriteAllText(reportFile, sb.ToString(), System.Text.Encoding.UTF8);
            Debug.Log($"[WebGLOpt] WebGL Baseline Report saved to: {reportFile}\n{sb}");
            return sb.ToString();
        }

        [MenuItem("Tools/WebGL/Build WebGL for itch.io")]
        public static string BuildWebGLForItchIo()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                string skipped = "[WebGLOpt] Skipped BuildWebGLForItchIo: Editor is compiling or updating.";
                Debug.LogWarning(skipped);
                return skipped;
            }

            ApplyWebGLPlayerSettings();
            // itch.io tự nén & giải nén file .zip; dùng Disabled (.data/.wasm chuẩn) để tránh lỗi header Brotli/Gzip trên CDN.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.template = "APPLICATION:Default";

            var enabledScenes = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s != null && s.enabled && !string.IsNullOrEmpty(s.path))
                {
                    enabledScenes.Add(s.path);
                }
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildDir = Path.Combine(projectRoot, "Builds", "itchio");
            if (Directory.Exists(buildDir))
            {
                Directory.Delete(buildDir, true);
            }
            Directory.CreateDirectory(buildDir);

            var options = new BuildPlayerOptions
            {
                scenes = enabledScenes.ToArray(),
                locationPathName = buildDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"BuildResult={report.summary.result}");
            sb.AppendLine($"TotalErrors={report.summary.totalErrors}, TotalWarnings={report.summary.totalWarnings}");
            sb.AppendLine($"TotalTime={report.summary.totalTime.TotalSeconds:F1}s");
            sb.AppendLine($"SummaryTotalSizeMB={report.summary.totalSize / (1024.0 * 1024.0):F2}");

            if (Directory.Exists(buildDir))
            {
                var allFiles = Directory.GetFiles(buildDir, "*", SearchOption.AllDirectories);
                long diskBytes = 0;
                foreach (var f in allFiles) diskBytes += new FileInfo(f).Length;
                sb.AppendLine($"OutputFilesCount={allFiles.Length}, OutputDiskMB={diskBytes / (1024.0 * 1024.0):F2}");
                sb.AppendLine("--- Output Files ---");
                System.Array.Sort(allFiles, (a, b) => new FileInfo(b).Length.CompareTo(new FileInfo(a).Length));
                foreach (var f in allFiles)
                {
                    string rel = f.Substring(buildDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    sb.AppendLine($"  {new FileInfo(f).Length / 1024.0:F1} KB | {rel}");
                }
            }

            string reportFile = Path.Combine(projectRoot, "Builds", "itchio_build_report.txt");
            File.WriteAllText(reportFile, sb.ToString(), System.Text.Encoding.UTF8);
            Debug.Log($"[WebGLOpt] itch.io WebGL Build Report saved to: {reportFile}\n{sb}");
            return sb.ToString();
        }
    }
}
#endif
