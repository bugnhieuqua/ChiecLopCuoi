#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.EditorTools
{
    /// <summary>
    /// Chuẩn hóa kiến trúc dùng chung từ Màn 1 (SampleScene.unity):
    /// - Xuất HUD_Canvas và [AudioManager] thành Prefab chuẩn trong Assets/Resources/.
    /// - Dọn bỏ bản sao trùng lặp khỏi Màn 2–6 (chỉ giữ phần riêng của từng màn), để runtime code tự kích hoạt.
    /// </summary>
    public static class SharedLevelArchitectureTool
    {
        public const string Level1ScenePath = "Assets/Scenes/SampleScene.unity";
        public const string HudPrefabAssetPath = "Assets/Resources/Prefabs/HUD_Canvas.prefab";
        public const string AudioPrefabAssetPath = "Assets/Resources/AudioManager.prefab";

        private static readonly string[] SharedManagedLevelPaths =
        {
            "Assets/Scenes/level2.unity",
            "Assets/Scenes/level3.unity",
            "Assets/Scenes/level4.unity",
            "Assets/Scenes/level5.unity",
            "Assets/Scenes/level6.unity"
        };

        private static readonly string[] SharedRootNamesToClean =
        {
            "HUD_Canvas",
            "[AudioManager]",
            "EventSystem"
        };

        [MenuItem("Tools/HUD Architecture/Export Level 1 Prefabs & Clean Level 2-6 Scenes")]
        public static string ExportAndCleanAllLevels()
        {
            ExportLevel1SharedPrefabs();

            int cleanedCount = 0;
            foreach (string scenePath in SharedManagedLevelPaths)
            {
                if (!File.Exists(scenePath)) continue;
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                CleanDuplicateSharedObjectsFromScene(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                cleanedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            string summary = $"[SharedLevelArchitecture] Exported Level 1 Prefabs ({HudPrefabAssetPath}, {AudioPrefabAssetPath}) and cleaned {cleanedCount} scenes (level2..level6).";
            Debug.Log("<color=green>" + summary + "</color>");
            return summary;
        }

        public static void ExportLevel1SharedPrefabs()
        {
            var level1 = EditorSceneManager.OpenScene(Level1ScenePath, OpenSceneMode.Single);
            ExportLevel1SharedPrefabsFromOpenScene(level1, saveScene: true);
        }

        public static void ExportLevel1SharedPrefabsFromOpenScene(Scene level1, bool saveScene)
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Prefabs");

            GameObject hudGo = null;
            GameObject audioGo = null;

            foreach (var root in level1.GetRootGameObjects())
            {
                if (root.name == "HUD_Canvas") hudGo = root;
                else if (root.name == "[AudioManager]") audioGo = root;
            }

            if (hudGo != null)
            {
                var menuCtrl = hudGo.GetComponentInChildren<HUDMenuController>(true);
                if (menuCtrl != null)
                {
                    menuCtrl.LoadSpritesIfMissing();
                    menuCtrl.AutoBindReferences();
                    menuCtrl.EnsurePauseHomeButton();
                    menuCtrl.EnsureGuidePanelExists();
                    if (menuCtrl.menuPanel != null) menuCtrl.menuPanel.SetActive(false);
                    if (menuCtrl.guidePanel != null) menuCtrl.guidePanel.SetActive(false);
                    EditorUtility.SetDirty(menuCtrl);
                }

                // Clone để lưu thành Prefab sạch, đảm bảo ẩn sẵn các panel modal/P2 mặc định
                GameObject tempHud = Object.Instantiate(hudGo);
                tempHud.name = "HUD_Canvas";
                ResetHudDefaults(tempHud);
                PrefabUtility.SaveAsPrefabAsset(tempHud, HudPrefabAssetPath);
                Object.DestroyImmediate(tempHud);
            }

            if (audioGo != null)
            {
                GameObject tempAudio = Object.Instantiate(audioGo);
                tempAudio.name = "[AudioManager]";
                PrefabUtility.SaveAsPrefabAsset(tempAudio, AudioPrefabAssetPath);
                Object.DestroyImmediate(tempAudio);
            }
            else if (!File.Exists(AudioPrefabAssetPath))
            {
                var fallback = new GameObject("[AudioManager]", typeof(AudioManager));
                PrefabUtility.SaveAsPrefabAsset(fallback, AudioPrefabAssetPath);
                Object.DestroyImmediate(fallback);
            }

            if (saveScene)
            {
                EditorSceneManager.MarkSceneDirty(level1);
                EditorSceneManager.SaveScene(level1);
            }
        }

        public static void CleanDuplicateSharedObjectsFromScene(Scene targetScene)
        {
            if (!targetScene.IsValid() || targetScene.path == Level1ScenePath) return;

            var roots = targetScene.GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root == null) continue;
                foreach (string sharedName in SharedRootNamesToClean)
                {
                    if (root.name == sharedName)
                    {
                        Object.DestroyImmediate(root);
                        break;
                    }
                }
            }
        }

        private static void ResetHudDefaults(GameObject hudRoot)
        {
            string[] hidePaths =
            {
                "HUD_Menu",
                "Result_KO_Panel",
                "Tutorial_Guide_Panel",
                "KO_Splash_Overlay",
                "Duel_Encounter_Banner"
            };
            foreach (string path in hidePaths)
            {
                Transform tf = hudRoot.transform.Find(path);
                if (tf != null) tf.gameObject.SetActive(false);
            }

            Transform nb = hudRoot.transform.Find("HUD_Notebook");
            if (nb != null)
            {
                foreach (Transform child in nb)
                {
                    if (child.name.StartsWith("P2_")) child.gameObject.SetActive(false);
                }
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            string parent = Path.GetDirectoryName(folderPath).Replace("\\", "/");
            string name = Path.GetFileName(folderPath);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
