using UnityEngine;
using UnityEngine.UI;
using ChiecLopCuoi.Audio;

namespace ChiecLopCuoi.UI
{
    public partial class HUDMenuController
    {
#if UNITY_EDITOR
        public void LoadSpritesIfMissing()
        {
            if (spriteMenuBoard == null)
            {
                spriteMenuBoard = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MenuPause/Sprites/Pause_Board_Stone.png");
            }
            if (spriteMenuRibbon == null)
            {
                spriteMenuRibbon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MenuPause/Sprites/Pause_Banner_Title.png");
            }
            if (spriteMenuGuide == null)
            {
                spriteMenuGuide = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MenuPause/Sprites/Btn_Menu_List.png");
            }
            if (spriteBtnHome == null)
            {
                spriteBtnHome = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MenuPause/Sprites/Btn_Home.png");
            }
            if (spriteHuongDanBoard == null)
            {
                spriteHuongDanBoard = LoadSubSprite("Assets/Art/Btn_map/btn_huongdan.png", "btn_huongdan_0");
            }
            if (spriteMenuClose == null)
            {
                spriteMenuClose = LoadSubSprite("Assets/Art/Btn_map/btn_components.png", "btn_components_4");
            }
            if (spriteBtnMap == null)
            {
                spriteBtnMap = LoadSubSprite("Assets/Art/Btn_map/btn_map.png", "btn_map_0");
            }
        }

        private static Sprite LoadSubSprite(string assetPath, string spriteName)
        {
            var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (all != null)
            {
                foreach (var a in all)
                {
                    if (a is Sprite sp && sp.name == spriteName) return sp;
                }
                foreach (var a in all)
                {
                    if (a is Sprite sp) return sp;
                }
            }
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
#endif

        /// <summary>
        /// Tự động tìm và liên kết các nút trong HUD_Menu và nút Pause
        /// </summary>
        public void AutoBindReferences()
        {
            // 1. Tìm menuPanel (HUD_Menu)
            if (menuPanel == null)
            {
                Transform t = transform.Find("HUD_Menu");
                if (t != null) menuPanel = t.gameObject;
                else
                {
                    var canvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        t = canvas.transform.Find("HUD_Menu");
                        if (t != null) menuPanel = t.gameObject;
                    }
                    if (menuPanel == null)
                    {
                        var allGo = Resources.FindObjectsOfTypeAll<GameObject>();
                        foreach (var go in allGo)
                        {
                            if (go.scene == gameObject.scene && go.name == "HUD_Menu")
                            {
                                menuPanel = go;
                                break;
                            }
                        }
                    }
                }
            }

            // 2. Tìm pauseButton (Btn_Pause)
            if (pauseButton == null)
            {
                var found = GameObject.Find("Btn_Pause");
                if (found != null) pauseButton = found.GetComponent<Button>();
                if (pauseButton == null)
                {
                    var allButtons = Resources.FindObjectsOfTypeAll<Button>();
                    foreach (var b in allButtons)
                    {
                        if (b.gameObject.scene == gameObject.scene && (b.gameObject.name == "Btn_Pause" || b.gameObject.name == "Pause_Button"))
                        {
                            pauseButton = b;
                            break;
                        }
                    }
                }
            }

            // 3. Tìm các nút và slider bên trong HUD_Menu
            if (menuPanel != null)
            {
                // Play / Resume Button
                if (playButton == null)
                {
                    var t = menuPanel.transform.Find("Pause_Stone_Board/Main_Buttons_Row/Btn_Play_Resume")
                         ?? menuPanel.transform.Find("btn_menu_3");
                    if (t != null) playButton = t.GetComponent<Button>();
                }

                // Restart Button
                if (restartButton == null)
                {
                    var t = menuPanel.transform.Find("Pause_Stone_Board/Main_Buttons_Row/Btn_Restart")
                         ?? menuPanel.transform.Find("btn_menu_4");
                    if (t != null) restartButton = t.GetComponent<Button>();
                }

                // Guide / Menu Button
                if (guideButton == null)
                {
                    var t = menuPanel.transform.Find("Pause_Stone_Board/Main_Buttons_Row/Btn_Menu_Guide")
                         ?? menuPanel.transform.Find("btn_menu_5");
                    if (t != null) guideButton = t.GetComponent<Button>();
                }

                // Home Button (Trang chủ)
                if (homeButton == null)
                {
                    var t = menuPanel.transform.Find("Pause_Stone_Board/Main_Buttons_Row/Btn_Home");
                    if (t != null) homeButton = t.GetComponent<Button>();
                }

                // Quit Button (nếu có)
                if (quitButton == null)
                {
                    var t = menuPanel.transform.Find("btn_menu_6");
                    if (t != null) quitButton = t.GetComponent<Button>();
                }

                // Sliders & Mute Icons
                if (musicSlider == null)
                {
                    var t = menuPanel.transform.Find("Pause_Stone_Board/Music_Row/Slider_Music");
                    if (t != null) musicSlider = t.GetComponent<Slider>();
                }
                _musicIconImage = AudioManager.BindMuteIconButton(
                    menuPanel.transform.Find("Pause_Stone_Board/Music_Row/Icon_Music"),
                    () => { if (AudioManager.Instance != null) AudioManager.Instance.ToggleBgmMute(); });

                if (soundSlider == null)
                {
                    var t = menuPanel.transform.Find("Pause_Stone_Board/Sound_Row/Slider_Sound");
                    if (t != null) soundSlider = t.GetComponent<Slider>();
                }
                _soundIconImage = AudioManager.BindMuteIconButton(
                    menuPanel.transform.Find("Pause_Stone_Board/Sound_Row/Icon_Sound"),
                    () => { if (AudioManager.Instance != null) AudioManager.Instance.ToggleSfxMute(); });

                EnsurePauseHomeButton();
                EnsurePauseSlidersMatchResultPanel();
            }
        }

        /// <summary>
        /// Đảm bảo nút Trang chủ (Btn_Home) luôn tồn tại và căn đều 4 nút trên hàng chính của Pause Menu.
        /// </summary>
        public void EnsurePauseHomeButton()
        {
            if (menuPanel == null) return;

            var bottomIconsRow = menuPanel.transform.Find("Pause_Stone_Board/Bottom_Icons_Row");
            if (bottomIconsRow != null) bottomIconsRow.gameObject.SetActive(false);

            var floatingTire = menuPanel.transform.Find("Deco_Tire_Flat");
            if (floatingTire != null) floatingTire.gameObject.SetActive(false);

            var decoLeft = menuPanel.transform.Find("Deco_Tires_BottomLeft");
            if (decoLeft != null) decoLeft.SetAsFirstSibling();

            var mainRow = menuPanel.transform.Find("Pause_Stone_Board/Main_Buttons_Row") as RectTransform;
            if (mainRow == null) return;

            mainRow.sizeDelta = new Vector2(420f, 74f);

            var guideTf = mainRow.Find("Btn_Menu_Guide") as RectTransform;
            var playTf = mainRow.Find("Btn_Play_Resume") as RectTransform;
            var restartTf = mainRow.Find("Btn_Restart") as RectTransform;
            var homeTf = mainRow.Find("Btn_Home") as RectTransform;

            if (homeTf == null)
            {
                var homeGo = new GameObject("Btn_Home", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                homeGo.transform.SetParent(mainRow, false);
                homeGo.layer = 5;
                homeTf = homeGo.GetComponent<RectTransform>();

                var img = homeGo.GetComponent<Image>();
                Sprite spHome = spriteBtnHome;
                if (spHome == null)
                {
                    var resultHomeImg = menuPanel.transform.parent?.Find("Result_KO_Panel/Result_Stone_Board/Result_Buttons_Row/Btn_Home")?.GetComponent<Image>();
                    if (resultHomeImg != null) spHome = resultHomeImg.sprite;
                }
                img.sprite = spHome;
                img.preserveAspect = true;
                img.raycastTarget = true;

                var btn = homeGo.GetComponent<Button>();
                var colors = btn.colors;
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                btn.colors = colors;
                homeButton = btn;
            }
            else if (homeButton == null)
            {
                homeButton = homeTf.GetComponent<Button>();
            }

            SetButtonLayoutAndCaption(guideTf, new Vector2(-117f, 0f), new Vector2(58f, 58f));
            SetButtonLayoutAndCaption(playTf, new Vector2(-39f, 0f), new Vector2(64f, 64f));
            SetButtonLayoutAndCaption(restartTf, new Vector2(39f, 0f), new Vector2(58f, 58f));
            SetButtonLayoutAndCaption(homeTf, new Vector2(117f, 0f), new Vector2(58f, 58f));
        }

        private static void SetButtonLayoutAndCaption(RectTransform rt, Vector2 pos, Vector2 size)
        {
            if (rt == null) return;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Transform lblTf = rt.Find("Label");
            if (lblTf != null)
            {
                if (Application.isPlaying) Destroy(lblTf.gameObject);
                else DestroyImmediate(lblTf.gameObject);
            }
        }

        /// <summary>
        /// Đồng bộ cấu trúc 2 thanh trượt âm lượng của Pause Menu theo đúng cơ chế sạch của Result_KO_Panel
        /// (dùng Slider_Track_Clean + Fill Area/Fill_Green + Handle knob trượt mượt, bỏ track in sẵn knob giả).
        /// </summary>
        private void EnsurePauseSlidersMatchResultPanel()
        {
            if (menuPanel == null) return;

            Sprite cleanTrack = null;
            Sprite cleanFill = null;
            Sprite cleanKnob = null;

            // Ưu tiên lấy đúng Sprite đang dùng ở Result_KO_Panel nếu có trong Scene
            var canvas = menuPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var koMusic = canvas.transform.Find("Result_KO_Panel/Result_Stone_Board/Music_Row/Slider_Music")?.GetComponent<Slider>();
                if (koMusic != null)
                {
                    cleanTrack = koMusic.transform.Find("Background")?.GetComponent<Image>()?.sprite;
                    cleanFill = koMusic.fillRect != null ? koMusic.fillRect.GetComponent<Image>()?.sprite : null;
                    cleanKnob = koMusic.handleRect != null ? koMusic.handleRect.GetComponent<Image>()?.sprite : null;
                }
            }

            if (cleanTrack == null) cleanTrack = Resources.Load<Sprite>("UI/Slider_Track_Clean");
            if (cleanFill == null) cleanFill = Resources.Load<Sprite>("UI/Slider_Fill_Green");

            ConfigureCleanVolumeSlider(musicSlider, cleanTrack, cleanFill, cleanKnob);
            ConfigureCleanVolumeSlider(soundSlider, cleanTrack, cleanFill, cleanKnob);
        }

        private static void ConfigureCleanVolumeSlider(Slider slider, Sprite cleanTrack, Sprite cleanFill, Sprite cleanKnob)
        {
            if (slider == null) return;

            slider.interactable = true;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.direction = Slider.Direction.LeftToRight;

            // 1. Background dùng Slider_Track_Clean (không chứa hình knob/fill giả)
            var bgTf = slider.transform.Find("Background");
            if (bgTf != null && cleanTrack != null)
            {
                var bgImg = bgTf.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.sprite = cleanTrack;
                    bgImg.type = Image.Type.Simple;
                    bgImg.color = Color.white;
                    bgImg.raycastTarget = true;
                }
            }

            // 2. Tạo hoặc đồng bộ Fill Area + Fill (y hệt Result_KO_Panel)
            Transform fillAreaTf = slider.transform.Find("Fill Area");
            if (fillAreaTf == null)
            {
                var faGo = new GameObject("Fill Area", typeof(RectTransform));
                faGo.transform.SetParent(slider.transform, false);
                fillAreaTf = faGo.transform;
            }
            if (bgTf != null)
            {
                fillAreaTf.SetSiblingIndex(bgTf.GetSiblingIndex() + 1);
            }
            var faRt = fillAreaTf as RectTransform;
            if (faRt != null)
            {
                faRt.anchorMin = new Vector2(0f, 0.31f);
                faRt.anchorMax = new Vector2(1f, 0.67f);
                faRt.anchoredPosition = new Vector2(-1f, 0f);
                faRt.sizeDelta = new Vector2(-38f, 0f);
            }

            Transform fillTf = fillAreaTf.Find("Fill");
            if (fillTf == null)
            {
                var fGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fGo.transform.SetParent(fillAreaTf, false);
                fillTf = fGo.transform;
            }
            var fillRt = fillTf as RectTransform;
            var fillImg = fillTf.GetComponent<Image>();
            if (fillImg != null)
            {
                if (cleanFill != null) fillImg.sprite = cleanFill;
                fillImg.type = Image.Type.Simple;
                fillImg.color = Color.white;
                fillImg.raycastTarget = false;
            }
            if (fillRt != null)
            {
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = new Vector2(slider.normalizedValue, 1f);
                fillRt.anchoredPosition = Vector2.zero;
                fillRt.sizeDelta = Vector2.zero;
                slider.fillRect = fillRt;
            }

            // 3. Đồng bộ Handle Slide Area + Handle theo chuẩn Result_KO_Panel
            Transform handleAreaTf = slider.transform.Find("Handle Slide Area");
            if (handleAreaTf != null)
            {
                handleAreaTf.SetAsLastSibling();
                var hsaRt = handleAreaTf as RectTransform;
                if (hsaRt != null)
                {
                    hsaRt.anchorMin = Vector2.zero;
                    hsaRt.anchorMax = Vector2.one;
                    hsaRt.anchoredPosition = new Vector2(-1f, 0f);
                    hsaRt.sizeDelta = new Vector2(-38f, 0f);
                }

                Transform handleTf = handleAreaTf.Find("Handle");
                if (handleTf != null)
                {
                    var hRt = handleTf as RectTransform;
                    if (hRt != null)
                    {
                        hRt.sizeDelta = new Vector2(34f, 34f);
                        slider.handleRect = hRt;
                    }
                    var hImg = handleTf.GetComponent<Image>();
                    if (hImg != null)
                    {
                        if (cleanKnob != null) hImg.sprite = cleanKnob;
                        hImg.color = Color.white;
                        hImg.raycastTarget = true;
                        slider.targetGraphic = hImg;
                    }
                }
            }
        }

    }
}
