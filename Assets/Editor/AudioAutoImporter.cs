#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ChiecLopCuoi.Audio;

namespace ChiecLopCuoi.Editor
{
    /// <summary>
    /// Tự động cấu hình thông số tối ưu khi import âm thanh vào Assets/Audio/:
    /// - BGM: Streaming + Vorbis (tiết kiệm RAM cho nhạc dài)
    /// - SFX: DecompressOnLoad + PCM (độ trễ bằng 0 cho hiệu ứng ngắn)
    /// - UI: DecompressOnLoad + PCM (phản hồi ngay lập tức khi click)
    /// </summary>
    public class AudioAutoImporter : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Audio/")) return;
            ApplyAudioImportSettings((AudioImporter)assetImporter, assetPath);
        }

        private static AudioImporterSampleSettings ApplyAudioImportSettings(AudioImporter audioImporter, string path)
        {
            AudioImporterSampleSettings settings = audioImporter.defaultSampleSettings;

            if (path.Contains("/BGM/"))
            {
                // WebGL dùng CompressedInMemory + Vorbis; Standalone giữ Streaming để tối ưu RAM.
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                settings.preloadAudioData = false;
                audioImporter.forceToMono = false; // BGM giữ Stereo

                AudioImporterSampleSettings standaloneSettings = settings;
                standaloneSettings.loadType = AudioClipLoadType.Streaming;
                audioImporter.SetOverrideSampleSettings(BuildTargetGroup.Standalone, standaloneSettings);
            }
            else if (path.Contains("/SFX/"))
            {
                long fileBytes = System.IO.File.Exists(path) ? new System.IO.FileInfo(path).Length : 0;
                settings.preloadAudioData = true;
                if (fileBytes >= 500 * 1024)
                {
                    settings.loadType = AudioClipLoadType.CompressedInMemory;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.7f;
                }
                else
                {
                    // SFX ngắn: Decompress sẵn trong bộ nhớ để phát ngay lập tức
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = AudioCompressionFormat.PCM;
                }
                audioImporter.forceToMono = true; // SFX mono tiết kiệm 50% dung lượng
            }
            else if (path.Contains("/UI/"))
            {
                // UI: DecompressOnLoad + preload sẵn để click phản hồi tức thì, không lỗi WebAudio chưa tải
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.preloadAudioData = true;
                audioImporter.forceToMono = true;
            }

            audioImporter.defaultSampleSettings = settings;
            return settings;
        }

        [MenuItem("Tools/Audio/Optimize and Reimport All Audio")]
        public static void OptimizeAndReimportAllAudio()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer != null)
                {
                    var settings = ApplyAudioImportSettings(importer, path);
                    importer.SaveAndReimport();
                    count++;
                    Debug.Log($"[AudioAutoImporter] Optimized: {path} -> {settings.loadType} | {settings.compressionFormat}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AudioAutoImporter] Successfully optimized and reimported {count} audio clips in Assets/Audio/.");
        }

        [MenuItem("Tools/Audio/Setup AudioManager in Active Scene")]
        public static void SetupAudioManagerInScene()
        {
            var existingManager = Object.FindFirstObjectByType<AudioManager>();
            GameObject go;
            if (existingManager != null)
            {
                go = existingManager.gameObject;
            }
            else
            {
                go = new GameObject("[AudioManager]");
                existingManager = go.AddComponent<AudioManager>();
                Undo.RegisterCreatedObjectUndo(go, "Create AudioManager");
            }

            // Assign Default Clips
            existingManager.bgmLoginClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/bgm_login_epic.wav");
            existingManager.bgmBattleClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/bgm_battle_epic.wav");

            existingManager.sfxRunClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_run.wav");
            existingManager.sfxJumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_jump.wav");
            existingManager.sfxHitImpactClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_hit_impact.wav");
            existingManager.sfxPickupClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_pickup.wav");

            existingManager.uiBtnAttackClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ui_btn_attack.wav");
            existingManager.uiBtnDefendClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ui_btn_defend.wav");
            existingManager.uiBtnRetreatClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ui_btn_retreat.wav");
            existingManager.uiClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ui_click.wav");

            EditorUtility.SetDirty(existingManager);
            Debug.Log("[AudioAutoImporter] AudioManager setup successfully with all 10 audio clips linked!");
        }
    }
}
#endif
