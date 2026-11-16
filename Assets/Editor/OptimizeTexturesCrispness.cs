#if UNITY_EDITOR
using ChiecLopCuoi.Environment;
using ChiecLopCuoi.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class OptimizeTexturesCrispness
{
    private const string HudPrefabPath = "Assets/Resources/Prefabs/HUD_Canvas.prefab";

    [MenuItem("Tools/Optimize All Textures (Crisp Bilinear + Uncompressed)")]
    public static void OptimizeAllTextures()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art", "Assets/Resources" });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool modified = false;

            // Đảm bảo các ảnh lốp mới trong Resources/Tires được import dạng Sprite (Single)
            if (path.StartsWith("Assets/Resources/Tires/") || path.StartsWith("Assets/Resources/UI/"))
            {
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    modified = true;
                }
                if (importer.spriteImportMode == SpriteImportMode.None)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    modified = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    modified = true;
                }
            }

            // Giữ Point nếu sprite vốn là pixel-art, còn lại dùng Bilinear nét mịn
            if (importer.filterMode != FilterMode.Point && importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                modified = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                modified = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                modified = true;
            }

            if (importer.anisoLevel < 4)
            {
                importer.anisoLevel = 4;
                modified = true;
            }

            if (importer.maxTextureSize < 2048)
            {
                importer.maxTextureSize = 2048;
                modified = true;
            }

            if (modified)
            {
                importer.SaveAndReimport();
                count++;
                Debug.Log($"[OptimizeTextures] Optimized: {path} -> Crisp Uncompressed RGBA32, No Mipmaps, Aniso 4, 2048");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"[OptimizeTextures] Checked {guids.Length} textures across Assets/Art & Assets/Resources, updated {count} textures.");
    }

    [MenuItem("Tools/Optimize All Scene Cameras (SMAA + PostProcess)")]
    public static void OptimizeAllSceneCameras()
    {
        string activeScenePath = SceneManager.GetActiveScene().path;
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (buildScene == null || !buildScene.enabled || string.IsNullOrEmpty(buildScene.path)) continue;
            Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            bool dirty = false;

            Camera cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                UniversalAdditionalCameraData uac = cam.GetComponent<UniversalAdditionalCameraData>();
                if (uac == null)
                {
                    uac = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                    dirty = true;
                }
                if (!uac.renderPostProcessing)
                {
                    uac.renderPostProcessing = true;
                    dirty = true;
                }
                if (uac.antialiasing != AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                {
                    uac.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    dirty = true;
                }
                if (uac.antialiasingQuality != AntialiasingQuality.High)
                {
                    uac.antialiasingQuality = AntialiasingQuality.High;
                    dirty = true;
                }
            }

            if (scene.name == "LoginScene" || scene.name == "LevelSelectScene" || scene.name == "SampleScene")
            {
                if (Object.FindFirstObjectByType<DriftingSkyTiresFX>() == null)
                {
                    var fxGo = new GameObject("Sky_Drifting_Tires");
                    fxGo.AddComponent<DriftingSkyTiresFX>();
                    dirty = true;
                }
            }

            if (dirty)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[OptimizeCameras] Updated SMAA & scene setup in {buildScene.path}");
            }
        }

        if (!string.IsNullOrEmpty(activeScenePath) && System.IO.File.Exists(activeScenePath))
        {
            EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
        }
    }

    [MenuItem("Tools/UI/Bake & Show Victory_Finale_Panel in HUD Prefab")]
    public static void BakeAndShowVictoryFinalePanelInPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[OptimizeTextures] Không tìm thấy Prefab {HudPrefabPath}");
            return;
        }

        try
        {
            Transform finaleTf = prefabRoot.transform.Find("Victory_Finale_Panel");
            if (finaleTf != null)
            {
                VictoryFinalePanel panel = finaleTf.GetComponent<VictoryFinalePanel>()
                    ?? finaleTf.gameObject.AddComponent<VictoryFinalePanel>();
                panel.EnsureBindings();
                finaleTf.gameObject.SetActive(true);
                finaleTf.SetAsLastSibling();

                Transform rewardTxt = finaleTf.Find("Stone_Board/Center_Showcase/Reward_Pink_Tire_Text");
                if (rewardTxt != null && rewardTxt.GetComponent<TMPro.TMP_Text>() != null)
                {
                    rewardTxt.GetComponent<TMPro.TMP_Text>().text = $"+{ChiecLopCuoi.Database.GameRepository.Campaign6ClearBonusTires} Lốp Hồng! (Thưởng 6 Màn)";
                }
            }
            else
            {
                VictoryFinalePanel.ShowOrCreate(prefabRoot.transform);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, HudPrefabPath);
            Debug.Log($"[OptimizeTextures] Đã bake cụm Lốp Hồng mới & BẬT sẵn Victory_Finale_Panel trong {HudPrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    [MenuItem("Tools/Project/Apply Full Crispness, Bake Victory Panel & Sky Tires")]
    public static void ApplyAllPolishAndBake()
    {
        OptimizeAllTextures();
        OptimizeAllSceneCameras();
        BakeAndShowVictoryFinalePanelInPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath));
    }
}
#endif
