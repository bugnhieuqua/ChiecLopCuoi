#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.EditorTools
{
    public static class PauseMenuBuilder
    {
        public const string SpritesDir = "Assets/Art/MenuPause/Sprites";

        private static readonly string[] PlayableScenes =
        {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/level2.unity",
            "Assets/Scenes/level3.unity",
            "Assets/Scenes/level4.unity",
            "Assets/Scenes/level5.unity",
            "Assets/Scenes/level6.unity"
        };

        [MenuItem("Tools/Pause Menu/Configure Sprites and Rebuild HUD_Menu")]
        public static void RebuildAll()
        {
            ConfigureSprites();
            foreach (string s in PlayableScenes)
            {
                BuildPauseMenuInScene(s);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SharedLevelArchitectureTool.ExportLevel1SharedPrefabs();
            Debug.Log("<color=green>[PauseMenuBuilder]</color> Dựng lại HUD_Menu hoàn tất 100% trên cả 6 màn!");
        }

        [MenuItem("Tools/Pause Menu/Sync Home Button Across All Levels")]
        public static string SyncHomeButtonAcrossAllLevels()
        {
            ConfigureSprites();
            int count = 0;
            foreach (string scenePath in PlayableScenes)
            {
                if (!File.Exists(scenePath)) continue;
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var hudCanvas = GameObject.Find("HUD_Canvas");
                if (hudCanvas == null) continue;

                var ctrl = hudCanvas.GetComponentInChildren<HUDMenuController>(true);
                if (ctrl == null) continue;

                ctrl.LoadSpritesIfMissing();
                ctrl.AutoBindReferences();
                ctrl.EnsurePauseHomeButton();

                if (ctrl.menuPanel != null)
                {
                    var homeBtnImg = ctrl.menuPanel.transform.Find("Pause_Stone_Board/Main_Buttons_Row/Btn_Home")?.GetComponent<Image>();
                    if (homeBtnImg != null && ctrl.spriteBtnHome != null)
                    {
                        homeBtnImg.sprite = ctrl.spriteBtnHome;
                        EditorUtility.SetDirty(homeBtnImg);
                    }
                    ctrl.menuPanel.SetActive(false);
                }

                EditorUtility.SetDirty(ctrl);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                count++;
            }
            AssetDatabase.SaveAssets();
            SharedLevelArchitectureTool.ExportLevel1SharedPrefabs();
            Debug.Log($"<color=green>[PauseMenuBuilder]</color> Đã bổ sung nút Trang chủ (Btn_Home) vào HUD_Menu trên {count} màn!");
            return $"Synced Btn_Home across {count} scenes";
        }

        public static void ConfigureSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesDir });
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;

                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMode = (int)SpriteImportMode.Single;
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    settings.spritePivot = new Vector2(0.5f, 0.5f);
                    settings.spriteGenerateFallbackPhysicsShape = false;
                    importer.SetTextureSettings(settings);

                    EditorUtility.SetDirty(importer);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/{name}.png");
        }

        public static void BuildPauseMenuInScene(string scenePath)
        {
            if (!File.Exists(scenePath)) return;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var hudCanvas = GameObject.Find("HUD_Canvas");
            if (hudCanvas == null)
            {
                Debug.LogWarning($"[PauseMenuBuilder] Không tìm thấy HUD_Canvas trong {scenePath}");
                return;
            }

            // 1. Tìm hoặc tạo HUD_Menu
            Transform menuTf = hudCanvas.transform.Find("HUD_Menu");
            GameObject menuGo;
            if (menuTf != null)
            {
                menuGo = menuTf.gameObject;
                // Xoá toàn bộ các component và child cũ của HUD_Menu
                while (menuTf.childCount > 0)
                {
                    Object.DestroyImmediate(menuTf.GetChild(0).gameObject);
                }
            }
            else
            {
                menuGo = new GameObject("HUD_Menu", typeof(RectTransform));
                menuGo.transform.SetParent(hudCanvas.transform, false);
            }

            menuGo.layer = 5; // UI Layer
            var menuRt = menuGo.GetComponent<RectTransform>();
            menuRt.anchorMin = Vector2.zero;
            menuRt.anchorMax = Vector2.one;
            menuRt.sizeDelta = Vector2.zero;
            menuRt.anchoredPosition = Vector2.zero;
            menuRt.localScale = Vector3.one;

            // Nền phủ mờ màn hình
            var menuBg = menuGo.GetComponent<Image>();
            if (menuBg == null) menuBg = menuGo.AddComponent<Image>();
            menuBg.color = new Color(0f, 0f, 0f, 0.70f);
            menuBg.raycastTarget = true;

            // Load all new sprites
            Sprite spBanner = LoadSprite("Pause_Banner_Title");
            Sprite spBoard = LoadSprite("Pause_Board_Stone");
            Sprite spBtnMenu = LoadSprite("Btn_Menu_List");
            Sprite spBtnPlay = LoadSprite("Btn_Play_Resume");
            Sprite spBtnRestart = LoadSprite("Btn_Restart");
            Sprite spBtnHome = LoadSprite("Btn_Home");
            Sprite spBtnStar = LoadSprite("Btn_Star");
            Sprite spBtnSettings = LoadSprite("Btn_Settings");
            Sprite spBtnDiamond = LoadSprite("Btn_Diamond");
            Sprite spCoinsStack = LoadSprite("Icon_Coins_Stack");
            Sprite spIconMusic = LoadSprite("Icon_Music");
            Sprite spIconSound = LoadSprite("Icon_Sound");
            Sprite spSliderTrack = LoadSprite("Pause_Slider_Track");
            Sprite spSliderKnob = LoadSprite("Pause_Slider_Knob");
            Sprite spDecoTiresCorner = LoadSprite("Deco_Tires_Corner");
            Sprite spDecoTireFlat = LoadSprite("Deco_Tire_Flat");

            // 2. Banner Gỗ "CHIẾC LỐP CUỐI" (Đỉnh màn hình)
            var bannerGo = CreateImageObj(menuGo.transform, "Pause_Banner_Title", spBanner, new Vector2(490f, 134f), new Vector2(0f, 380f));
            var bannerRt = bannerGo.GetComponent<RectTransform>();
            bannerRt.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRt.anchorMax = new Vector2(0.5f, 0.5f);

            // 3. Thanh nút phụ phía trên bảng đá (Top Bar)
            var topBarGo = new GameObject("Pause_Top_Buttons_Bar", typeof(RectTransform));
            topBarGo.transform.SetParent(menuGo.transform, false);
            topBarGo.layer = 5;
            var topBarRt = topBarGo.GetComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0.5f, 0.5f);
            topBarRt.anchorMax = new Vector2(0.5f, 0.5f);
            topBarRt.sizeDelta = new Vector2(520f, 52f);
            topBarRt.anchoredPosition = new Vector2(0f, 280f);

            CreateButtonObj(topBarGo.transform, "Btn_Top_List", spBtnMenu, new Vector2(44f, 44f), new Vector2(-220f, 0f));
            CreateButtonObj(topBarGo.transform, "Btn_Top_Play", spBtnPlay, new Vector2(44f, 44f), new Vector2(-75f, 0f));
            CreateButtonObj(topBarGo.transform, "Btn_Top_Star", spBtnStar, new Vector2(42f, 42f), new Vector2(-25f, 0f));
            CreateButtonObj(topBarGo.transform, "Btn_Top_Settings", spBtnSettings, new Vector2(42f, 42f), new Vector2(25f, 0f));
            CreateButtonObj(topBarGo.transform, "Btn_Top_Diamond", spBtnDiamond, new Vector2(42f, 42f), new Vector2(75f, 0f));
            CreateImageObj(topBarGo.transform, "Icon_Top_Coins", spCoinsStack, new Vector2(62f, 50f), new Vector2(220f, 0f));

            // 4. Bảng Đá Chính (Center Stone Board)
            var stoneBoardGo = CreateImageObj(menuGo.transform, "Pause_Stone_Board", spBoard, new Vector2(540f, 406f), new Vector2(0f, 25f));
            var stoneBoardRt = stoneBoardGo.GetComponent<RectTransform>();
            stoneBoardRt.anchorMin = new Vector2(0.5f, 0.5f);
            stoneBoardRt.anchorMax = new Vector2(0.5f, 0.5f);

            // a. Tiêu đề "PAUSE" trên mặt bảng đá
            var pauseTxtGo = new GameObject("Text_Pause_Header", typeof(RectTransform), typeof(TextMeshProUGUI));
            pauseTxtGo.transform.SetParent(stoneBoardGo.transform, false);
            pauseTxtGo.layer = 5;
            var ptxtRt = pauseTxtGo.GetComponent<RectTransform>();
            ptxtRt.anchorMin = new Vector2(0.5f, 1f);
            ptxtRt.anchorMax = new Vector2(0.5f, 1f);
            ptxtRt.pivot = new Vector2(0.5f, 1f);
            ptxtRt.sizeDelta = new Vector2(300f, 40f);
            ptxtRt.anchoredPosition = new Vector2(0f, -18f);

            var tmpPause = pauseTxtGo.GetComponent<TextMeshProUGUI>();
            tmpPause.text = "PAUSE";
            tmpPause.fontSize = 32;
            tmpPause.alignment = TextAlignmentOptions.Center;
            tmpPause.color = new Color(0.96f, 0.72f, 0.42f, 1f);
            tmpPause.fontStyle = FontStyles.Bold;

            // b. Dòng Âm Nhạc (Music Row): Icon ♫ + Slider
            var musicRowGo = new GameObject("Music_Row", typeof(RectTransform));
            musicRowGo.transform.SetParent(stoneBoardGo.transform, false);
            musicRowGo.layer = 5;
            var musicRowRt = musicRowGo.GetComponent<RectTransform>();
            musicRowRt.sizeDelta = new Vector2(380f, 40f);
            musicRowRt.anchoredPosition = new Vector2(10f, 85f);

            CreateImageObj(musicRowGo.transform, "Icon_Music", spIconMusic, new Vector2(38f, 38f), new Vector2(-155f, 0f));
            var sliderMusic = CreateSliderObj(musicRowGo.transform, "Slider_Music", spSliderTrack, spSliderKnob, new Vector2(275f, 28f), new Vector2(25f, 0f));

            // c. Dòng Hiệu Ứng Âm Thanh (Sound Row): Icon 🔊 + Slider
            var soundRowGo = new GameObject("Sound_Row", typeof(RectTransform));
            soundRowGo.transform.SetParent(stoneBoardGo.transform, false);
            soundRowGo.layer = 5;
            var soundRowRt = soundRowGo.GetComponent<RectTransform>();
            soundRowRt.sizeDelta = new Vector2(380f, 40f);
            soundRowRt.anchoredPosition = new Vector2(10f, 35f);

            CreateImageObj(soundRowGo.transform, "Icon_Sound", spIconSound, new Vector2(38f, 38f), new Vector2(-155f, 0f));
            var sliderSound = CreateSliderObj(soundRowGo.transform, "Slider_Sound", spSliderTrack, spSliderKnob, new Vector2(275f, 28f), new Vector2(25f, 0f));

            // d. 4 Nút Hành Động Chính (Main Action Buttons): [≡ Hướng dẫn], [▶ Tiếp tục], [↺ Chơi lại], [⌂ Trang chủ]
            var mainBtnsGo = new GameObject("Main_Buttons_Row", typeof(RectTransform));
            mainBtnsGo.transform.SetParent(stoneBoardGo.transform, false);
            mainBtnsGo.layer = 5;
            var mainBtnsRt = mainBtnsGo.GetComponent<RectTransform>();
            mainBtnsRt.sizeDelta = new Vector2(420f, 74f);
            mainBtnsRt.anchoredPosition = new Vector2(0f, -40f);

            var btnGuide = CreateButtonObj(mainBtnsGo.transform, "Btn_Menu_Guide", spBtnMenu, new Vector2(58f, 58f), new Vector2(-129f, 4f));
            var btnPlay = CreateButtonObj(mainBtnsGo.transform, "Btn_Play_Resume", spBtnPlay, new Vector2(64f, 64f), new Vector2(-43f, 4f));
            var btnRestart = CreateButtonObj(mainBtnsGo.transform, "Btn_Restart", spBtnRestart, new Vector2(58f, 58f), new Vector2(43f, 4f));
            var btnHome = CreateButtonObj(mainBtnsGo.transform, "Btn_Home", spBtnHome, new Vector2(58f, 58f), new Vector2(129f, 4f));

            // e. Hàng Nút Tiện Ích Đáy Bảng Đá (Bottom Row): [★ Star], [⚙ Settings], [♦ Diamond], [Coins]
            var bottomRowGo = new GameObject("Bottom_Icons_Row", typeof(RectTransform));
            bottomRowGo.transform.SetParent(stoneBoardGo.transform, false);
            bottomRowGo.layer = 5;
            var bottomRowRt = bottomRowGo.GetComponent<RectTransform>();
            bottomRowRt.sizeDelta = new Vector2(320f, 50f);
            bottomRowRt.anchoredPosition = new Vector2(0f, -125f);

            CreateButtonObj(bottomRowGo.transform, "Btn_Star", spBtnStar, new Vector2(44f, 44f), new Vector2(-95f, 0f));
            CreateButtonObj(bottomRowGo.transform, "Btn_Settings", spBtnSettings, new Vector2(44f, 44f), new Vector2(-45f, 0f));
            CreateButtonObj(bottomRowGo.transform, "Btn_Diamond", spBtnDiamond, new Vector2(44f, 44f), new Vector2(5f, 0f));
            CreateImageObj(bottomRowGo.transform, "Icon_Coins", spCoinsStack, new Vector2(65f, 52f), new Vector2(85f, 0f));

            // 5. Trang Trí Lốp Xe 2 Góc Dưới
            var decoLeftGo = CreateImageObj(menuGo.transform, "Deco_Tires_BottomLeft", spDecoTiresCorner, new Vector2(165f, 170f), new Vector2(-280f, -220f));
            var decoRightGo = CreateImageObj(menuGo.transform, "Deco_Tire_BottomRight", spDecoTireFlat, new Vector2(130f, 142f), new Vector2(250f, -240f));

            // 6. Liên kết vào HUDMenuController
            var ctrl = hudCanvas.GetComponentInChildren<HUDMenuController>(true);
            if (ctrl != null)
            {
                ctrl.menuPanel = menuGo;
                ctrl.playButton = btnPlay;
                ctrl.restartButton = btnRestart;
                ctrl.guideButton = btnGuide;
                ctrl.homeButton = btnHome;
                ctrl.spriteBtnHome = spBtnHome;
                ctrl.musicSlider = sliderMusic;
                ctrl.soundSlider = sliderSound;
                ctrl.EnsurePauseHomeButton();

                // Nút Pause ngoài HUD (Btn_Pause)
                var pauseBtnGo = GameObject.Find("Btn_Pause");
                if (pauseBtnGo != null) ctrl.pauseButton = pauseBtnGo.GetComponent<Button>();

                EditorUtility.SetDirty(ctrl);
            }

            // Mặc định ban đầu menu ẩn
            menuGo.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"<color=green>[PauseMenuBuilder]</color> Đã dựng thành công HUD_Menu trong {scenePath}!");
        }

        private static GameObject CreateImageObj(Transform parent, string name, Sprite sp, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.raycastTarget = false;
            return go;
        }

        private static Button CreateButtonObj(Transform parent, string name, Sprite sp, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            btn.colors = colors;
            return btn;
        }

        private static Slider CreateSliderObj(Transform parent, string name, Sprite spTrack, Sprite spKnob, Vector2 size, Vector2 pos)
        {
            var sliderGo = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(parent, false);
            sliderGo.layer = 5;
            var sliderRt = sliderGo.GetComponent<RectTransform>();
            sliderRt.sizeDelta = size;
            sliderRt.anchoredPosition = pos;

            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            // Background
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(sliderGo.transform, false);
            bgGo.layer = 5;
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bgRt.anchoredPosition = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.sprite = spTrack;
            bgImg.raycastTarget = true;

            // Handle Slide Area
            var handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            handleAreaGo.layer = 5;
            var haRt = handleAreaGo.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.sizeDelta = new Vector2(-20f, 0f);
            haRt.anchoredPosition = Vector2.zero;

            // Handle
            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            handleGo.layer = 5;
            var hRt = handleGo.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(24f, 24f);
            hRt.anchoredPosition = Vector2.zero;
            var hImg = handleGo.GetComponent<Image>();
            hImg.sprite = spKnob;
            hImg.raycastTarget = true;

            slider.targetGraphic = hImg;
            slider.handleRect = hRt;

            return slider;
        }
    }
}
#endif
