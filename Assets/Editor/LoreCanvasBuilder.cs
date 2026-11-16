using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.Editor
{
    public static class LoreCanvasBuilder
    {
        private const string BackdropPath = "Assets/Art/UI/Menus/story_ancient_backdrop.png";
        private const string SkipButtonPath = "Assets/Art/UI/Menus/btn_skip_wood.png";
        private const string BtnComponentsPath = "Assets/Art/Btn_map/btn_components.png";
        private const string PrefabPath = "Assets/Prefabs/UI/LoreCanvas.prefab";

        [MenuItem("Tools/Build Lore Canvas")]
        public static void BuildLoreCanvas()
        {
            RebuildLorePrefabAndSyncAllScenes();
        }

        [MenuItem("Tools/Story/Rebuild Lore Prefab And Sync Scenes")]
        public static string RebuildLorePrefabAndSyncAllScenes()
        {
            ConfigureSingleSpriteImporter(BackdropPath);
            ConfigureSingleSpriteImporter(SkipButtonPath);

            Sprite backdropSprite = LoadFirstSprite(BackdropPath);
            Sprite skipPlaqueSprite = LoadFirstSprite(SkipButtonPath);
            Sprite prevArrowSprite = LoadSprite(BtnComponentsPath, "btn_components_12");
            Sprite nextArrowSprite = LoadSprite(BtnComponentsPath, "btn_components_13");

            if (backdropSprite == null)
            {
                Debug.LogError("[LoreCanvasBuilder] Missing story_ancient_backdrop.png");
                return "Failed: Missing backdrop sprite";
            }

            // 1. Tạo hoặc làm mới Prefab LoreCanvas dùng chung cho toàn game
            var canvasGo = CreateLoreCanvasHierarchy(backdropSprite, skipPlaqueSprite, prevArrowSprite, nextArrowSprite);

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(canvasGo, PrefabPath);
            Object.DestroyImmediate(canvasGo);

            // 2. Đồng bộ sang LoginScene.unity (PLAY lần đầu -> Tiểu sử -> LevelSelectScene)
            SyncLoreToLoginScene(savedPrefab);

            // 3. Đồng bộ sang SampleScene.unity (tắt tự mở vì Tiểu sử đã chuyển sang trước Chọn Màn)
            SyncLoreToGameplayScene("Assets/Scenes/SampleScene.unity", savedPrefab);

            // 4. Dựng lại LevelSelectScene với nút Btn_Story góc trên phải + LoreCanvas để xem lại
            ChiecLopCuoi.EditorTools.LevelSelect3DSceneBuilder.Build3DLevelSelectScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "[LoreCanvasBuilder] Built LoreCanvas.prefab (ancient backdrop + Prev/Next btn_components_12/13 + wooden SKIP) and synced LoginScene, SampleScene, LevelSelectScene.";
        }

        private static GameObject CreateLoreCanvasHierarchy(
            Sprite backdropSprite,
            Sprite skipPlaqueSprite,
            Sprite prevArrowSprite,
            Sprite nextArrowSprite)
        {
            var fontAsset = ThemeFontManager.GetStoryFont();

            var canvasGo = new GameObject("LoreCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<GraphicRaycaster>();

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // LorePanel toàn màn hình
            var lorePanelGo = new GameObject("LorePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lorePanelGo.transform.SetParent(canvasGo.transform, false);
            var panelRt = lorePanelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            var panelBg = lorePanelGo.GetComponent<Image>();
            panelBg.color = new Color(0.03f, 0.02f, 0.02f, 0.92f);
            panelBg.raycastTarget = true;

            // Nền cổ thư / đêm tái sinh phủ tràn khung
            var bgNotebookGo = new GameObject("BG_Notebook", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
            bgNotebookGo.transform.SetParent(lorePanelGo.transform, false);
            var bgRt = bgNotebookGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImage = bgNotebookGo.GetComponent<Image>();
            bgImage.sprite = backdropSprite;
            bgImage.color = Color.white;
            bgImage.raycastTarget = false;
            var arf = bgNotebookGo.GetComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = backdropSprite != null
                ? backdropSprite.rect.width / Mathf.Max(1f, backdropSprite.rect.height)
                : 16f / 9f;

            // Khung an toàn 16:9 căn chuẩn tọa độ chữ trên tờ giấy cổ giữa màn
            var contentFrameGo = new GameObject("Parchment_Content_Frame", typeof(RectTransform));
            contentFrameGo.transform.SetParent(bgNotebookGo.transform, false);
            var frameRt = contentFrameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = new Vector2(0.5f, 0.5f);
            frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.sizeDelta = new Vector2(1920f, 1080f);
            frameRt.anchoredPosition = Vector2.zero;

            // Tiêu đề "TIỂU SỬ" dùng UTM Zebrawood SDF
            var loreTitleFont = ThemeFontManager.GetLoginFont() ?? fontAsset;
            var titleTmp = CreateTMP("Txt_LoreTitle", contentFrameGo.transform, "TIỂU SỬ", 52f,
                new Vector2(6f, 295f), new Vector2(680f, 72f), new Color32(108, 34, 16, 255),
                TextAlignmentOptions.Center, loreTitleFont);

            // Nội dung chữ Tiếng Việt rộng thoáng trên trang giấy cổ
            var storyTmp = CreateTMP("Text_StoryContent", contentFrameGo.transform, string.Empty, 28f,
                new Vector2(6f, 16f), new Vector2(1140f, 430f), new Color32(42, 26, 14, 255),
                TextAlignmentOptions.TopLeft, fontAsset);
            storyTmp.textWrappingMode = TextWrappingModes.Normal;
            storyTmp.lineSpacing = 14f;
            storyTmp.richText = true;

            // Bộ điều hướng trang ở mép dưới tờ giấy: Về trước (btn_components_12) | Trang X/3 | Tiếp theo (btn_components_13)
            var prevBtn = CreateIconButton("Btn_PrevPage", contentFrameGo.transform, prevArrowSprite,
                new Vector2(-175f, -278f), new Vector2(92f, 92f));

            var pageTmp = CreateTMP("Txt_PageIndicator", contentFrameGo.transform, "Trang 1 / 3", 28f,
                new Vector2(6f, -278f), new Vector2(210f, 52f), new Color32(78, 44, 18, 255),
                TextAlignmentOptions.Center, fontAsset);

            var nextBtn = CreateIconButton("Btn_NextPage", contentFrameGo.transform, nextArrowSprite,
                new Vector2(185f, -278f), new Vector2(92f, 92f));
            nextBtn.gameObject.AddComponent<UIPulseEffect>();

            // Nút SKIP gỗ mới đặt ở góc trên bên phải
            var skipGo = new GameObject("Btn_SkipAll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            skipGo.transform.SetParent(lorePanelGo.transform, false);
            var skipRt = skipGo.GetComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(1f, 1f);
            skipRt.anchorMax = new Vector2(1f, 1f);
            skipRt.pivot = new Vector2(0.5f, 0.5f);
            skipRt.sizeDelta = new Vector2(226f, 92f);
            skipRt.anchoredPosition = new Vector2(-148f, -72f);

            var skipImg = skipGo.GetComponent<Image>();
            skipImg.sprite = skipPlaqueSprite;
            skipImg.preserveAspect = true;
            skipImg.raycastTarget = true;

            var skipBtn = skipGo.GetComponent<Button>();
            skipBtn.targetGraphic = skipImg;
            skipBtn.transition = Selectable.Transition.ColorTint;

            var skipTxt = CreateTMP("Txt_Skip", skipGo.transform, "SKIP >>", 30f,
                new Vector2(0f, 2f), new Vector2(190f, 58f), new Color32(255, 240, 196, 255),
                TextAlignmentOptions.Center, fontAsset);
            skipTxt.outlineWidth = 0.24f;
            skipTxt.outlineColor = new Color32(46, 22, 8, 245);

            // Gắn StoryBookManager và kết nối toàn bộ UI & Events
            var manager = canvasGo.AddComponent<StoryBookManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("titleText").objectReferenceValue = titleTmp;
            so.FindProperty("storyText").objectReferenceValue = storyTmp;
            so.FindProperty("pageIndicatorText").objectReferenceValue = pageTmp;
            so.FindProperty("lorePanel").objectReferenceValue = lorePanelGo;
            so.FindProperty("prevButton").objectReferenceValue = prevBtn;
            so.FindProperty("nextOrSkipButton").objectReferenceValue = nextBtn;
            so.FindProperty("skipButton").objectReferenceValue = skipBtn;
            so.FindProperty("storyFontSize").floatValue = 28f;
            so.FindProperty("autoOpenOnStartInGameplayScene").boolValue = false;
            so.ApplyModifiedProperties();

            lorePanelGo.SetActive(false);
            return canvasGo;
        }

        private static void SyncLoreToLoginScene(GameObject prefab)
        {
            string scenePath = "Assets/Scenes/LoginScene.unity";
            if (!File.Exists(scenePath) || prefab == null) return;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            RemoveExistingLoreCanvases();

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "LoreCanvas";
            var mgr = instance.GetComponent<StoryBookManager>();
            if (mgr != null)
            {
                mgr.loadSceneOnComplete = true;
                mgr.targetSceneName = "LevelSelectScene";
                mgr.autoOpenOnStartInGameplayScene = false;
                if (mgr.lorePanel != null) mgr.lorePanel.SetActive(false);
                EditorUtility.SetDirty(mgr);
            }

            var loginMgr = Object.FindFirstObjectByType<LoginScreenManager>();
            if (loginMgr != null && mgr != null)
            {
                var lso = new SerializedObject(loginMgr);
                lso.FindProperty("storyBookManager").objectReferenceValue = mgr;
                lso.FindProperty("loreCanvasOrPanel").objectReferenceValue = mgr.lorePanel;
                lso.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SyncLoreToGameplayScene(string scenePath, GameObject prefab)
        {
            if (!File.Exists(scenePath) || prefab == null) return;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            RemoveExistingLoreCanvases();

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "LoreCanvas";
            var mgr = instance.GetComponent<StoryBookManager>();
            if (mgr != null)
            {
                mgr.loadSceneOnComplete = false;
                mgr.targetSceneName = string.Empty;
                mgr.autoOpenOnStartInGameplayScene = false;
                var hudCanvas = GameObject.Find("HUD_Canvas");
                if (hudCanvas != null) mgr.gameplayUI = hudCanvas;
                if (mgr.lorePanel != null) mgr.lorePanel.SetActive(false);
                EditorUtility.SetDirty(mgr);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void RemoveExistingLoreCanvases()
        {
            foreach (var mgr in Object.FindObjectsByType<StoryBookManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mgr != null) Undo.DestroyObjectImmediate(mgr.gameObject);
            }
            var stray = GameObject.Find("LoreCanvas");
            if (stray != null) Undo.DestroyObjectImmediate(stray);
        }

        private static TextMeshProUGUI CreateTMP(
            string name, Transform parent, string text, float fontSize, Vector2 pos, Vector2 size,
            Color color, TextAlignmentOptions align, TMP_FontAsset fontAsset)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button CreateIconButton(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            return btn;
        }

        private static void ConfigureSingleSpriteImporter(string assetPath)
        {
            if (!File.Exists(assetPath)) return;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static Sprite LoadFirstSprite(string path)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is Sprite sub) return sub;
            }
            return null;
        }

        private static Sprite LoadSprite(string path, string spriteName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is Sprite sp && a.name == spriteName) return sp;
            }
            foreach (var a in assets)
            {
                if (a is Sprite sp) return sp;
            }
            return null;
        }
    }
}
