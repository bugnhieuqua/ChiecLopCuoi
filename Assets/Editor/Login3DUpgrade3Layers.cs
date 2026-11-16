#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.Editor
{
    public static class Login3DUpgrade3Layers
    {
        private const string ScenePath = "Assets/Scenes/LoginScene.unity";
        private const string BgFarPath = "Assets/Art/UI/Login3D/login_bg_far_sky.png";
        private const string BgMidPath = "Assets/Art/UI/Login3D/login_bg_mid_village.png";
        private const string BgNearBoulderPath = "Assets/Art/UI/Login3D/login_bg_near_boulder.png";
        private const string BgNearBambooLeftPath = "Assets/Art/UI/Login3D/login_bg_near_bamboo_left.png";
        private const string BgNearRightPath = "Assets/Art/UI/Login3D/login_bg_near_right.png";
        private const string MistPath = "Assets/Art/UI/Login3D/login_mist_soft.png";

        [MenuItem("Tools/Login/Upgrade Login Scene to 3-Layer Parallax")]
        public static string UpgradeLoginTo3Layers()
        {
            ConfigureSingleSpriteImporter(BgFarPath, FilterMode.Bilinear, new Vector2(0.5f, 0.5f));
            ConfigureSingleSpriteImporter(BgMidPath, FilterMode.Bilinear, new Vector2(0.5f, 0.5f));
            ConfigureSingleSpriteImporter(BgNearBoulderPath, FilterMode.Bilinear, new Vector2(0.5f, 0.5f));
            ConfigureSingleSpriteImporter(BgNearBambooLeftPath, FilterMode.Bilinear, new Vector2(0.5f, 0.5f));
            ConfigureSingleSpriteImporter(BgNearRightPath, FilterMode.Bilinear, new Vector2(0.5f, 0.5f));

            Sprite farSprite = LoadFirstSprite(BgFarPath);
            Sprite midSprite = LoadFirstSprite(BgMidPath);
            Sprite nearBoulderSprite = LoadFirstSprite(BgNearBoulderPath);
            Sprite nearLeftSprite = LoadFirstSprite(BgNearBambooLeftPath);
            Sprite nearRightSprite = LoadFirstSprite(BgNearRightPath);
            Sprite mistSprite = LoadFirstSprite(MistPath);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var stageGo = GameObject.Find("Login_3D_Stage");
            if (stageGo == null)
            {
                return "[Login3DUpgrade3Layers] ERROR: Login_3D_Stage not found in LoginScene.unity";
            }

            Transform stageRoot = stageGo.transform;

            // Xóa nền đơn cũ và các lớp nền 3D cũ nếu chạy lại idempotent
            string[] oldNames =
            {
                "BG_Night_Layer",
                "BG_Night_Far",
                "Fog_FarMid_Layer",
                "BG_Night_Mid",
                "BG_Night_Near_Boulder",
                "BG_Night_Near_BambooLeft",
                "BG_Night_Near_Right"
            };
            foreach (string oldName in oldNames)
            {
                Transform child = stageRoot.Find(oldName);
                if (child != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            // Bảo toàn Dark_Vignette_Mood & Fog_Mid_Layer, điều chỉnh Z hợp lý giữa lớp giữa và cận cảnh
            Transform vignette = stageRoot.Find("Dark_Vignette_Mood");
            if (vignette != null)
            {
                vignette.localPosition = new Vector3(0f, 0f, 5.8f);
                var vsr = vignette.GetComponent<SpriteRenderer>();
                if (vsr != null) vsr.sortingOrder = -6;
            }

            Transform fogMid = stageRoot.Find("Fog_Mid_Layer");
            if (fogMid != null)
            {
                fogMid.localPosition = new Vector3(0f, -2.1f, 4.8f);
                var fsr = fogMid.GetComponent<SpriteRenderer>();
                if (fsr != null)
                {
                    fsr.sortingOrder = 1;
                    if (mistSprite == null) mistSprite = fsr.sprite;
                }
            }

            // Tạo 3 lớp chiều sâu + lớp sương mù xen kẽ Far/Mid
            var bgFarGo = CreateSpriteObject("BG_Night_Far", stageRoot, farSprite,
                new Vector3(0f, 0f, 10.4f), new Vector3(3.45f, 3.45f, 1f), Color.white, -16);

            var fogFarMidGo = CreateSpriteObject("Fog_FarMid_Layer", stageRoot, mistSprite,
                new Vector3(0.5f, -1.35f, 8.6f), new Vector3(6.2f, 1.85f, 1f),
                new Color(0.56f, 0.64f, 0.85f, 0.28f), -13);

            var bgMidGo = CreateSpriteObject("BG_Night_Mid", stageRoot, midSprite,
                new Vector3(0f, 0f, 6.8f), new Vector3(2.84f, 2.84f, 1f), Color.white, -10);

            var bgNearBoulderGo = CreateSpriteObject("BG_Night_Near_Boulder", stageRoot, nearBoulderSprite,
                new Vector3(0f, 0f, 3.65f), new Vector3(2.31f, 2.31f, 1f), Color.white, 2);

            var bgNearLeftGo = CreateSpriteObject("BG_Night_Near_BambooLeft", stageRoot, nearLeftSprite,
                new Vector3(0f, 0f, 3.55f), new Vector3(2.29f, 2.29f, 1f), Color.white, 3);

            var bgNearRightGo = CreateSpriteObject("BG_Night_Near_Right", stageRoot, nearRightSprite,
                new Vector3(0f, 0f, 3.50f), new Vector3(2.28f, 2.28f, 1f), Color.white, 3);

            // Sắp lại thứ tự sibling sạch sẽ trong Login_3D_Stage
            bgFarGo.transform.SetSiblingIndex(1);
            fogFarMidGo.transform.SetSiblingIndex(2);
            bgMidGo.transform.SetSiblingIndex(3);

            var ctrl = Object.FindFirstObjectByType<Login3DStageController>(FindObjectsInactive.Include);
            if (ctrl != null)
            {
                var so = new SerializedObject(ctrl);
                so.FindProperty("backgroundLayer").objectReferenceValue = null;
                so.FindProperty("bgFarLayer").objectReferenceValue = bgFarGo.transform;
                so.FindProperty("fogFarMidLayer").objectReferenceValue = fogFarMidGo.transform;
                so.FindProperty("bgMidLayer").objectReferenceValue = bgMidGo.transform;
                so.FindProperty("bgNearBoulder").objectReferenceValue = bgNearBoulderGo.transform;
                so.FindProperty("bgNearBambooLeft").objectReferenceValue = bgNearLeftGo.transform;
                so.FindProperty("bgNearRight").objectReferenceValue = bgNearRightGo.transform;
                so.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return "[Login3DUpgrade3Layers] Upgraded LoginScene.unity to 3 depth layers + alternating mist + swaying near bamboo & vignette preservation.";
        }

        private static GameObject CreateSpriteObject(
            string name, Transform parent, Sprite sprite, Vector3 pos, Vector3 scale, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go;
        }

        private static void ConfigureSingleSpriteImporter(string assetPath, FilterMode filterMode, Vector2 pivot)
        {
            if (!File.Exists(assetPath)) return;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePivot = pivot;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = filterMode;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static Sprite LoadFirstSprite(string assetPath)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sp != null) return sp;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (obj is Sprite sub) return sub;
            }
            return null;
        }
    }
}
#endif
