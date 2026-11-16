#if UNITY_EDITOR
using System.IO;
using ChiecLopCuoi.Database;
using ChiecLopCuoi.UI;
using UnityEditor;
using UnityEngine;

namespace ChiecLopCuoi.Editor
{
    public static class TutorialDebugMenu
    {
        private const string RobotSpritePath = "Assets/Resources/UI/Robot_Companion.png";

        [MenuItem("Tools/Tutorial/Reset First-Time Tutorial")]
        public static void ResetTutorial()
        {
            SimpleTutorialController.ResetTutorialProgress();
            StoryBookManager.HasReadLoreThisSession = false;
            Debug.Log("[Tutorial] Đã đặt lại hướng dẫn lần đầu (ChiecLopCuoi_TutorialDone = 0, Stage = 0).");
        }

        [MenuItem("Tools/Game/Reset All Game State (Test From Start)")]
        public static void ResetAllGameStateForFreshTest()
        {
            DatabaseManager.Initialize();
            GameRepository.ResetProgressConfigAndSaveData(preserveAudioSettings: true);

            string persistentDb = Path.Combine(Application.persistentDataPath, "game_runtime.db");
            if (File.Exists(persistentDb))
            {
                File.Delete(persistentDb);
            }

            SimpleTutorialController.ResetTutorialProgress();
            StoryBookManager.HasReadLoreThisSession = false;

            Debug.Log("[GameReset] Đã reset toàn bộ trạng thái game (Tutorial 3 chặng, Tiểu sử, 6 màn chơi, Sao, Lốp, Nâng cấp & Thưởng 30 lốp về 0; giữ nguyên âm lượng).");
        }

        [MenuItem("Tools/UI/Optimize New UI Sprites (Crisp)")]
        public static void ConfigureRobotSpriteImporter()
        {
            string[] spritePaths =
            {
                RobotSpritePath,
                "Assets/Resources/UI/White_Card_Bg.png",
                "Assets/Resources/UI/Btn_Map_Blue.png",
                "Assets/Resources/UI/Loading_Bg.jpg",
                "Assets/Resources/UI/Pink_Tire.png"
            };

            foreach (string assetPath in spritePaths)
            {
                if (!File.Exists(assetPath))
                {
                    Debug.LogWarning($"[TutorialDebugMenu] Không tìm thấy {assetPath}");
                    continue;
                }

                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue;

                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }
                if (importer.filterMode != FilterMode.Bilinear)
                {
                    importer.filterMode = FilterMode.Bilinear;
                    changed = true;
                }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }
                if (importer.anisoLevel < 4)
                {
                    importer.anisoLevel = 4;
                    changed = true;
                }
                if (importer.maxTextureSize < 2048)
                {
                    importer.maxTextureSize = 2048;
                    changed = true;
                }
                if (changed)
                {
                    importer.SaveAndReimport();
                }
                Debug.Log($"[TutorialDebugMenu] Đã tối ưu độ nét Sprite (Bilinear + Uncompressed + Aniso 4 + 2048): {assetPath}");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
#endif
