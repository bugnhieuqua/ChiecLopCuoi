#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.Editor
{
    /// <summary>
    /// Tạo Scene LoadingScene.unity với giao diện HUB_load và đăng ký vào EditorBuildSettings.
    /// </summary>
    public static class LoadingSceneBuilder
    {
        private const string LoadingScenePath = "Assets/Scenes/LoadingScene.unity";
        private const string LoadingBgPath = "Assets/Resources/UI/Loading_Bg.jpg";
        private const string PinkTirePath = "Assets/Resources/UI/Pink_Tire.png";

        [MenuItem("Tools/UI/Build LoadingScene")]
        public static void BuildLoadingScene()
        {
            ConfigureSpriteAsset(LoadingBgPath, maxTextureSize: 1024);
            ConfigureSpriteAsset(PinkTirePath, maxTextureSize: 512);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.backgroundColor = Color.black;
            cam.clearFlags = CameraClearFlags.SolidColor;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var controllerGo = new GameObject("LoadingSceneController");
            var controller = controllerGo.AddComponent<LoadingSceneController>();
            controller.EnsureHubLoadHierarchy();

            EditorSceneManager.SaveScene(scene, LoadingScenePath);
            EnsureSceneInBuildSettings(LoadingScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LoadingSceneBuilder] Đã tạo và đăng ký thành công {LoadingScenePath}");
        }

        private static void ConfigureSpriteAsset(string assetPath, int maxTextureSize)
        {
            if (!System.IO.File.Exists(assetPath)) return;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

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
            if (importer.maxTextureSize != maxTextureSize)
            {
                importer.maxTextureSize = maxTextureSize;
                changed = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i] != null && scenes[i].path == scenePath)
                {
                    scenes[i].enabled = true;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
