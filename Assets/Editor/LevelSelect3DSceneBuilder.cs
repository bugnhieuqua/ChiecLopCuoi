#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ChiecLopCuoi.UI;
using ChiecLopCuoi.Environment;

namespace ChiecLopCuoi.EditorTools
{
    public static class LevelSelect3DSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/LevelSelectScene.unity";
        private const string BgFarPath = "Assets/Art/UI/LevelSelect/level_bg_far_sky.png";
        private const string BgMidPath = "Assets/Art/UI/LevelSelect/level_bg_mid_gate.png";
        private const string BgNearHillsPath = "Assets/Art/UI/LevelSelect/level_bg_near_hills.png";
        private const string BgNearBranchLeftPath = "Assets/Art/UI/LevelSelect/level_bg_near_branch_left.png";
        private const string BgNearBambooRightPath = "Assets/Art/UI/LevelSelect/level_bg_near_bamboo_right.png";
        private const string CowLopCutoutPath = "Assets/Art/UI/LevelSelect/level_select_cowlop_cutout.png";
        private const string HeroineCutoutPath = "Assets/Art/UI/LevelSelect/level_select_heroine_cutout.png";
        private const string MistPath = "Assets/Art/UI/Login3D/login_mist_soft.png";
        private const string BtnComponentsPath = "Assets/Art/Btn_map/btn_components.png";
        private const string TileWoodPath = "Assets/Art/UI/LevelSelect/level_tile_wood.png";
        private const string GoldStarPath = "Assets/Art/UI/LevelSelect/star_gold.png";
        private const string EmptyStarPath = "Assets/Art/UI/LevelSelect/star_empty.png";

        [MenuItem("Tools/LevelSelect/Build 3D LevelSelect Scene")]
        public static string Build3DLevelSelectScene()
        {
            ConfigureSingleSpriteImporter(BgFarPath, FilterMode.Bilinear, new Vector2(0.5f, 0.5f));
            ConfigureSingleSpriteImporter(BgMidPath, FilterMode.Bilinear, new Vector2(0.5f, 0.5f));
            ConfigureSingleSpriteImporter(BgNearHillsPath, FilterMode.Bilinear, new Vector2(0.5f, 0.5f));
            ConfigureSingleSpriteImporter(BgNearBranchLeftPath, FilterMode.Bilinear, new Vector2(0.5f, 0.5f));
            ConfigureSingleSpriteImporter(BgNearBambooRightPath, FilterMode.Bilinear, new Vector2(0.5f, 0.5f));
            ConfigureSingleSpriteImporter(CowLopCutoutPath, FilterMode.Bilinear, new Vector2(0.5f, 0f));
            ConfigureSingleSpriteImporter(HeroineCutoutPath, FilterMode.Bilinear, new Vector2(0.5f, 0f));

            Sprite bgFarSprite = LoadFirstSprite(BgFarPath);
            Sprite bgMidSprite = LoadFirstSprite(BgMidPath);
            Sprite bgNearHillsSprite = LoadFirstSprite(BgNearHillsPath);
            Sprite bgNearLeftSprite = LoadFirstSprite(BgNearBranchLeftPath);
            Sprite bgNearRightSprite = LoadFirstSprite(BgNearBambooRightPath);
            Sprite cowLopSprite = LoadFirstSprite(CowLopCutoutPath);
            Sprite heroineSprite = LoadFirstSprite(HeroineCutoutPath);
            Sprite mistSprite = LoadFirstSprite(MistPath);
            Sprite tileSprite = LoadFirstSprite(TileWoodPath);
            Sprite goldStarSprite = LoadFirstSprite(GoldStarPath);
            Sprite emptyStarSprite = LoadFirstSprite(EmptyStarPath);
            Sprite homeIconSprite = LoadNamedSubSprite(BtnComponentsPath, "btn_components_2");
            Sprite storyIconSprite = LoadNamedSubSprite(BtnComponentsPath, "btn_components_6");
            Sprite lockIconSprite = LoadNamedSubSprite(BtnComponentsPath, "btn_components_3");
            Sprite[] numSprites =
            {
                LoadFirstSprite("Assets/Art/UI/LevelSelect/level_num_1.png"),
                LoadFirstSprite("Assets/Art/UI/LevelSelect/level_num_2.png"),
                LoadFirstSprite("Assets/Art/UI/LevelSelect/level_num_3.png"),
                LoadFirstSprite("Assets/Art/UI/LevelSelect/level_num_4.png"),
                LoadFirstSprite("Assets/Art/UI/LevelSelect/level_num_5.png"),
                LoadFirstSprite("Assets/Art/UI/LevelSelect/level_num_6.png")
            };

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Main Camera (Perspective FOV 44 giống LoginScene 3D)
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(18, 28, 34, 255);
            cam.orthographic = false;
            cam.fieldOfView = 44f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();

            // 2. EventSystem
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            // 3. Stage3D Root (3-layer scenery + alternating mist + characters + floating particles)
            var stageRoot = new GameObject("Stage3D_Root");

            var bgFarGo = CreateSpriteObject("BG_Valley_Far", stageRoot.transform, bgFarSprite,
                new Vector3(0f, 0f, 10.8f), new Vector3(3.20f, 3.20f, 1f), Color.white, -16);

            var mistFarMidGo = CreateSpriteObject("Mist_ValleyFarMid", stageRoot.transform, mistSprite,
                new Vector3(-0.8f, -1.2f, 8.8f), new Vector3(6.4f, 1.85f, 1f),
                new Color(0.98f, 0.96f, 0.90f, 0.30f), -12);

            var bgMidGo = CreateSpriteObject("BG_Valley_Mid", stageRoot.transform, bgMidSprite,
                new Vector3(0f, 0f, 6.9f), new Vector3(2.60f, 2.60f, 1f), Color.white, -8);

            var mistMidGo = CreateSpriteObject("Mist_ValleyMid", stageRoot.transform, mistSprite,
                new Vector3(-1.2f, -1.7f, 5.3f), new Vector3(5.0f, 1.55f, 1f),
                new Color(0.96f, 0.99f, 0.95f, 0.34f), -2);

            var bgNearHillsGo = CreateSpriteObject("BG_Valley_Near_Hills", stageRoot.transform, bgNearHillsSprite,
                new Vector3(0f, 0f, 3.75f), new Vector3(2.12f, 2.12f, 1f), Color.white, 2);

            var bgNearLeftGo = CreateSpriteObject("BG_Valley_Near_LeftBranch", stageRoot.transform, bgNearLeftSprite,
                new Vector3(0f, 0f, 3.45f), new Vector3(2.07f, 2.07f, 1f), Color.white, 3);

            var bgNearRightGo = CreateSpriteObject("BG_Valley_Near_RightBamboo", stageRoot.transform, bgNearRightSprite,
                new Vector3(0f, 0f, 3.40f), new Vector3(2.06f, 2.06f, 1f), Color.white, 3);

            // Hai nhân vật tách nền đứng ở hai triền đồi (Z = +2.2)
            var cowLopGo = CreateSpriteObject("Actor_CowLop", stageRoot.transform, cowLopSprite,
                new Vector3(-7.05f, -4.45f, 2.2f), new Vector3(0.54f, 0.54f, 1f), Color.white, 10);

            var heroineGo = CreateSpriteObject("Actor_Heroine", stageRoot.transform, heroineSprite,
                new Vector3(7.15f, -4.55f, 2.2f), new Vector3(0.45f, 0.45f, 1f), Color.white, 10);

            var mistForeGo = CreateSpriteObject("Mist_ValleyFore", stageRoot.transform, mistSprite,
                new Vector3(1.5f, -2.95f, 1.4f), new Vector3(4.6f, 1.35f, 1f),
                new Color(0.95f, 0.98f, 0.92f, 0.24f), 12);

            // Hạt phát sáng bay lơ lửng trong thung lũng 3D
            var fxGo = new GameObject("StageFX_3D");
            fxGo.transform.SetParent(stageRoot.transform, false);
            fxGo.AddComponent<LoginFirefliesAndEmbersFX>();

            // 4. World-Space 3D Canvas cho Tiêu đề LEVEL & 3 Thẻ Màn (Z = -0.3)
            var worldCanvasGo = new GameObject("WorldCards_Canvas");
            worldCanvasGo.transform.position = new Vector3(0f, 0f, -0.3f);
            worldCanvasGo.transform.localScale = Vector3.one * 0.007257f; // 1080px khớp chiều cao frustum tại Z=-0.3
            var worldCanvas = worldCanvasGo.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.worldCamera = cam;
            worldCanvas.sortingOrder = 25;

            var worldRt = worldCanvasGo.GetComponent<RectTransform>();
            worldRt.sizeDelta = new Vector2(1920f, 1080f);
            worldCanvasGo.AddComponent<GraphicRaycaster>();

            var cleanFont = ThemeFontManager.GetStoryFont();
            var zebrawoodFont = ThemeFontManager.GetLoginFont() ?? cleanFont;

            var titleTmp = CreateTMP("Txt_LevelTitle", worldCanvasGo.transform, "LEVEL", 84f,
                new Vector2(0f, 345f), new Vector2(680f, 110f), Color.white);
            if (zebrawoodFont != null) titleTmp.font = zebrawoodFont;
            titleTmp.outlineWidth = 0f;
            titleTmp.extraPadding = true;

            var cardsRowGo = new GameObject("LevelCards_Grid", typeof(RectTransform));
            cardsRowGo.transform.SetParent(worldCanvasGo.transform, false);
            var rowRt = cardsRowGo.GetComponent<RectTransform>();
            rowRt.anchoredPosition = new Vector2(0f, -20f);
            rowRt.sizeDelta = new Vector2(920f, 620f);

            const int totalLevels = 6;
            var levelSlots = new LevelSelectManager.LevelSlotUI[totalLevels];
            var slotTransforms = new Transform[totalLevels];
            string[] sceneNames = { "SampleScene", "level2", "level3", "level4", "level5", "level6" };
            string[] requiredKeys = { string.Empty, "SampleScene", "level2", "level3", "level4", "level5" };
            float[] colX = { -265f, 0f, 265f };
            float[] rowY = { 122f, -142f };

            for (int i = 0; i < totalLevels; i++)
            {
                int lv = i + 1;
                int col = i % 3;
                int row = i / 3;
                var slotGo = new GameObject($"Level_Slot_{lv}", typeof(RectTransform));
                slotGo.transform.SetParent(cardsRowGo.transform, false);
                var slotRt = slotGo.GetComponent<RectTransform>();
                slotRt.sizeDelta = new Vector2(210f, 250f);
                slotRt.anchoredPosition = new Vector2(colX[col], rowY[row]);
                slotTransforms[i] = slotRt;

                // Thẻ gỗ bấm chọn màn
                var cardGo = new GameObject($"Btn_LevelCard_{lv}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                cardGo.transform.SetParent(slotGo.transform, false);
                var cardRt = cardGo.GetComponent<RectTransform>();
                cardRt.sizeDelta = new Vector2(180f, 180f);
                cardRt.anchoredPosition = new Vector2(0f, 16f);

                var cardImg = cardGo.GetComponent<Image>();
                cardImg.sprite = tileSprite;
                cardImg.preserveAspect = true;
                cardImg.raycastTarget = true;

                var cardBtn = cardGo.GetComponent<Button>();
                cardBtn.targetGraphic = cardImg;
                cardBtn.transition = Selectable.Transition.ColorTint;

                // Số 1..6 trên mặt thẻ
                var numGo = new GameObject("Number_Group", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                numGo.transform.SetParent(cardGo.transform, false);
                var numRt = numGo.GetComponent<RectTransform>();
                numRt.sizeDelta = new Vector2(102f, 102f);
                numRt.anchoredPosition = Vector2.zero;
                var numImg = numGo.GetComponent<Image>();
                numImg.sprite = numSprites[i];
                numImg.preserveAspect = true;
                numImg.raycastTarget = false;

                // Ổ khóa (btn_components_3) khi chưa mở màn
                var lockGo = new GameObject("Lock_Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                lockGo.transform.SetParent(cardGo.transform, false);
                var lockRt = lockGo.GetComponent<RectTransform>();
                lockRt.sizeDelta = new Vector2(112f, 112f);
                lockRt.anchoredPosition = Vector2.zero;
                var lockImg = lockGo.GetComponent<Image>();
                lockImg.sprite = lockIconSprite;
                lockImg.preserveAspect = true;
                lockImg.raycastTarget = false;
                lockGo.SetActive(i > 0);

                // Hàng 3 ngôi sao bên dưới thẻ
                var starsRow = new GameObject("Stars_Row", typeof(RectTransform));
                starsRow.transform.SetParent(slotGo.transform, false);
                var starsRt = starsRow.GetComponent<RectTransform>();
                starsRt.sizeDelta = new Vector2(150f, 44f);
                starsRt.anchoredPosition = new Vector2(0f, -96f);

                var stars = new Image[3];
                float[] starX = { -44f, 0f, 44f };
                float[] starY = { 0f, 5f, 0f };
                for (int s = 0; s < 3; s++)
                {
                    var starGo = new GameObject($"Star_{s + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    starGo.transform.SetParent(starsRow.transform, false);
                    var stRt = starGo.GetComponent<RectTransform>();
                    stRt.sizeDelta = new Vector2(38f, 38f);
                    stRt.anchoredPosition = new Vector2(starX[s], starY[s]);
                    var stImg = starGo.GetComponent<Image>();
                    stImg.sprite = emptyStarSprite;
                    stImg.preserveAspect = true;
                    stImg.raycastTarget = false;
                    stars[s] = stImg;
                }

                levelSlots[i] = new LevelSelectManager.LevelSlotUI
                {
                    levelNumber = lv,
                    sceneName = sceneNames[i],
                    requiredClearedKey = requiredKeys[i],
                    button = cardBtn,
                    numberGroup = numGo,
                    lockIconObject = lockGo,
                    starImages = stars,
                    cardRect = cardRt
                };
            }

            // 5. ScreenSpaceOverlay HUD (Btn_Home góc trên trái + Toast thông báo phía dưới)
            var hudCanvasGo = new GameObject("Canvas_HUD");
            var hudCanvas = hudCanvasGo.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 50;
            hudCanvasGo.AddComponent<GraphicRaycaster>();
            var hudScaler = hudCanvasGo.AddComponent<CanvasScaler>();
            hudScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            hudScaler.referenceResolution = new Vector2(1920f, 1080f);
            hudScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            hudScaler.matchWidthOrHeight = 0.5f;

            var homeGo = new GameObject("Btn_Home", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            homeGo.transform.SetParent(hudCanvasGo.transform, false);
            var homeRt = homeGo.GetComponent<RectTransform>();
            homeRt.anchorMin = new Vector2(0f, 1f);
            homeRt.anchorMax = new Vector2(0f, 1f);
            homeRt.pivot = new Vector2(0.5f, 0.5f);
            homeRt.sizeDelta = new Vector2(114f, 114f);
            homeRt.anchoredPosition = new Vector2(92f, -84f);

            var homeImg = homeGo.GetComponent<Image>();
            homeImg.sprite = homeIconSprite;
            homeImg.preserveAspect = true;
            homeImg.raycastTarget = true;

            var homeBtn = homeGo.GetComponent<Button>();
            homeBtn.targetGraphic = homeImg;
            homeBtn.transition = Selectable.Transition.ColorTint;

            // Nút Mở lại Tiểu sử góc trên phải (btn_components_6)
            var storyBtnGo = new GameObject("Btn_Story", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            storyBtnGo.transform.SetParent(hudCanvasGo.transform, false);
            var storyBtnRt = storyBtnGo.GetComponent<RectTransform>();
            storyBtnRt.anchorMin = new Vector2(1f, 1f);
            storyBtnRt.anchorMax = new Vector2(1f, 1f);
            storyBtnRt.pivot = new Vector2(0.5f, 0.5f);
            storyBtnRt.sizeDelta = new Vector2(114f, 114f);
            storyBtnRt.anchoredPosition = new Vector2(-92f, -84f);

            var storyBtnImg = storyBtnGo.GetComponent<Image>();
            storyBtnImg.sprite = storyIconSprite;
            storyBtnImg.preserveAspect = true;
            storyBtnImg.raycastTarget = true;

            var storyBtn = storyBtnGo.GetComponent<Button>();
            storyBtn.targetGraphic = storyBtnImg;
            storyBtn.transition = Selectable.Transition.ColorTint;

            // Khởi tạo LoreCanvas trong LevelSelectScene để mở lại Tiểu sử bất kỳ lúc nào
            StoryBookManager storyMgr = null;
            var lorePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/LoreCanvas.prefab");
            if (lorePrefab != null)
            {
                var loreInstance = (GameObject)PrefabUtility.InstantiatePrefab(lorePrefab);
                loreInstance.name = "LoreCanvas";
                storyMgr = loreInstance.GetComponent<StoryBookManager>();
                if (storyMgr != null)
                {
                    storyMgr.loadSceneOnComplete = false;
                    storyMgr.targetSceneName = string.Empty;
                    storyMgr.autoOpenOnStartInGameplayScene = false;
                    if (storyMgr.lorePanel != null) storyMgr.lorePanel.SetActive(false);
                    EditorUtility.SetDirty(storyMgr);
                }
            }

            var toastTmp = CreateTMP("Toast_Notice", hudCanvasGo.transform, string.Empty, 36f,
                Vector2.zero, new Vector2(980f, 72f), new Color32(255, 224, 130, 255));
            var toastRt = toastTmp.rectTransform;
            toastRt.anchorMin = new Vector2(0.5f, 0f);
            toastRt.anchorMax = new Vector2(0.5f, 0f);
            toastRt.anchoredPosition = new Vector2(0f, 92f);
            toastTmp.outlineWidth = 0.25f;
            toastTmp.outlineColor = new Color32(24, 14, 8, 240);
            toastTmp.gameObject.SetActive(false);

            // 6. LevelSelectManager + LevelSelect3DStageController
            var mgrGo = new GameObject("LevelSelectController");
            var mgr = mgrGo.AddComponent<LevelSelectManager>();
            var mgrSo = new SerializedObject(mgr);
            mgrSo.FindProperty("homeButton").objectReferenceValue = homeBtn;
            mgrSo.FindProperty("storyButton").objectReferenceValue = storyBtn;
            mgrSo.FindProperty("storyBookManager").objectReferenceValue = storyMgr;
            mgrSo.FindProperty("pageTitleText").objectReferenceValue = titleTmp;
            mgrSo.FindProperty("toastText").objectReferenceValue = toastTmp;
            mgrSo.FindProperty("goldStarSprite").objectReferenceValue = goldStarSprite;
            mgrSo.FindProperty("emptyStarSprite").objectReferenceValue = emptyStarSprite;
            var slotsProp = mgrSo.FindProperty("levelSlots");
            slotsProp.arraySize = totalLevels;
            for (int i = 0; i < totalLevels; i++)
            {
                var p = slotsProp.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("levelNumber").intValue = levelSlots[i].levelNumber;
                p.FindPropertyRelative("sceneName").stringValue = levelSlots[i].sceneName;
                p.FindPropertyRelative("requiredClearedKey").stringValue = levelSlots[i].requiredClearedKey;
                p.FindPropertyRelative("button").objectReferenceValue = levelSlots[i].button;
                p.FindPropertyRelative("numberGroup").objectReferenceValue = levelSlots[i].numberGroup;
                p.FindPropertyRelative("lockIconObject").objectReferenceValue = levelSlots[i].lockIconObject;
                p.FindPropertyRelative("cardRect").objectReferenceValue = levelSlots[i].cardRect;
                var spProp = p.FindPropertyRelative("starImages");
                spProp.arraySize = 3;
                for (int s = 0; s < 3; s++)
                {
                    spProp.GetArrayElementAtIndex(s).objectReferenceValue = levelSlots[i].starImages[s];
                }
            }
            mgrSo.ApplyModifiedProperties();

            var stage3D = mgrGo.AddComponent<LevelSelect3DStageController>();
            var stSo = new SerializedObject(stage3D);
            stSo.FindProperty("stageCamera").objectReferenceValue = cam;
            stSo.FindProperty("bgFarLayer").objectReferenceValue = bgFarGo.transform;
            stSo.FindProperty("fogFarMidLayer").objectReferenceValue = mistFarMidGo.transform;
            stSo.FindProperty("bgMidLayer").objectReferenceValue = bgMidGo.transform;
            stSo.FindProperty("fogMidLayer").objectReferenceValue = mistMidGo.transform;
            stSo.FindProperty("bgNearHills").objectReferenceValue = bgNearHillsGo.transform;
            stSo.FindProperty("bgNearLeftBranch").objectReferenceValue = bgNearLeftGo.transform;
            stSo.FindProperty("bgNearRightBamboo").objectReferenceValue = bgNearRightGo.transform;
            stSo.FindProperty("fogForeLayer").objectReferenceValue = mistForeGo.transform;
            stSo.FindProperty("cowLopCutout").objectReferenceValue = cowLopGo.transform;
            stSo.FindProperty("heroineCutout").objectReferenceValue = heroineGo.transform;
            stSo.FindProperty("worldCardsRoot").objectReferenceValue = worldCanvasGo.transform;
            stSo.FindProperty("titleTmpText").objectReferenceValue = titleTmp;
            var slotRefs = stSo.FindProperty("levelCardSlots");
            slotRefs.arraySize = totalLevels;
            for (int i = 0; i < totalLevels; i++)
            {
                slotRefs.GetArrayElementAtIndex(i).objectReferenceValue = slotTransforms[i];
            }
            stSo.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return "[LevelSelect3DSceneBuilder] Built full 3D LevelSelectScene (Perspective FOV 44°, layered cutouts, mist, fireflies, 3D WorldSpace cards).";
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

        private static TextMeshProUGUI CreateTMP(
            string name, Transform parent, string text, float fontSize, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
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

        private static Sprite LoadNamedSubSprite(string assetPath, string spriteName)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (obj is Sprite sub && sub.name == spriteName) return sub;
            }
            return null;
        }
    }
}
#endif
