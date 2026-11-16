using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using ChiecLopCuoi.Audio;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Popup cài đặt nhanh Nhạc (BGM) & Âm thanh (SFX) dùng chung cho LoginScene và LevelSelectScene,
    /// đồng thời cung cấp helper dựng khung bảng đá chuẩn phong cách game cho các modal khác.
    /// </summary>
    public class AudioSettingsModal : MonoBehaviour
    {
        private GameObject _root;
        private Slider _musicSlider;
        private Slider _soundSlider;
        private Image _musicIcon;
        private Image _soundIcon;
        private bool _isSyncing;

        public static Button AttachQuickAudioButton(
            Transform parentCanvas,
            string objectName,
            Vector2 anchor,
            Vector2 anchoredPos,
            Vector2 size)
        {
            if (parentCanvas == null) return null;

            Transform existing = FindChildRecursive(parentCanvas, objectName);
            GameObject btnGo = existing != null
                ? existing.gameObject
                : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            if (existing == null)
            {
                btnGo.transform.SetParent(parentCanvas, false);
                btnGo.layer = 5;

                var rt = btnGo.GetComponent<RectTransform>();
                rt.anchorMin = anchor;
                rt.anchorMax = anchor;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = size;
            }

            var img = btnGo.GetComponent<Image>();
            Sprite gearSp = Resources.Load<Sprite>("UI/Btn_Settings_Wood")
                ?? Resources.Load<Sprite>("UI/Btn_Settings")
                ?? Resources.Load<Sprite>("UI/Icon_Sound");
            img.sprite = gearSp;
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = true;

            var btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.08f, 1.05f, 0.92f, 1f);
            cb.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
            btn.colors = cb;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                ShowOrCreate(parentCanvas);
            });

            return btn;
        }

        private static Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root == null) return null;
            Transform direct = root.Find(targetName);
            if (direct != null) return direct;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), targetName);
                if (found != null) return found;
            }
            return null;
        }

        public static AudioSettingsModal ShowOrCreate(Transform parentCanvas)
        {
            if (parentCanvas == null) return null;

            Transform existing = parentCanvas.Find("Audio_Settings_Modal");
            AudioSettingsModal modal = existing != null
                ? existing.GetComponent<AudioSettingsModal>()
                : null;

            if (modal == null)
            {
                if (existing != null)
                {
                    modal = existing.gameObject.AddComponent<AudioSettingsModal>();
                    modal.EnsureBindings();
                }
                else
                {
                    GameObject overlay = CreateStoneModalOverlay(
                        parentCanvas,
                        "Audio_Settings_Modal",
                        "ÂM THANH",
                        new Vector2(560f, 360f),
                        out RectTransform boardRt);

                    modal = overlay.AddComponent<AudioSettingsModal>();
                    modal.BuildBody(overlay, boardRt);
                }
            }

            modal.Open();
            return modal;
        }

        public static GameObject CreateStoneModalOverlay(
            Transform parentCanvas,
            string overlayName,
            string headerTitle,
            Vector2 boardSize,
            out RectTransform boardRt)
        {
            var overlayGo = new GameObject(overlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayGo.transform.SetParent(parentCanvas, false);
            overlayGo.layer = 5;

            var overlayRt = overlayGo.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            var dimImg = overlayGo.GetComponent<Image>();
            dimImg.color = new Color(0.03f, 0.03f, 0.06f, 0.84f);
            dimImg.raycastTarget = true;

            var boardGo = new GameObject("Stone_Board", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            boardGo.transform.SetParent(overlayGo.transform, false);
            boardGo.layer = 5;
            boardRt = boardGo.GetComponent<RectTransform>();
            boardRt.anchorMin = new Vector2(0.5f, 0.5f);
            boardRt.anchorMax = new Vector2(0.5f, 0.5f);
            boardRt.pivot = new Vector2(0.5f, 0.5f);
            boardRt.anchoredPosition = Vector2.zero;
            boardRt.sizeDelta = boardSize;

            var boardImg = boardGo.GetComponent<Image>();
            Sprite stoneSp = Resources.Load<Sprite>("UI/Pause_Board_Stone");
            if (stoneSp != null)
            {
                boardImg.sprite = stoneSp;
                boardImg.type = Image.Type.Simple;
                boardImg.color = Color.white;
            }
            else
            {
                boardImg.color = new Color(0.24f, 0.22f, 0.20f, 0.96f);
            }

            Sprite plaqueSp = Resources.Load<Sprite>("UI/Btn_Plaque_Wood");
            if (plaqueSp != null)
            {
                var bannerGo = new GameObject("Title_Banner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bannerGo.transform.SetParent(boardGo.transform, false);
                bannerGo.layer = 5;

                var bannerRt = bannerGo.GetComponent<RectTransform>();
                bannerRt.anchorMin = new Vector2(0.5f, 1f);
                bannerRt.anchorMax = new Vector2(0.5f, 1f);
                bannerRt.pivot = new Vector2(0.5f, 0.5f);
                bannerRt.anchoredPosition = new Vector2(0f, -12f);
                bannerRt.sizeDelta = new Vector2(Mathf.Clamp(boardSize.x * 0.48f, 260f, 340f), 68f);

                var bannerImg = bannerGo.GetComponent<Image>();
                bannerImg.sprite = plaqueSp;
                bannerImg.preserveAspect = false;
                bannerImg.raycastTarget = false;

                TMP_Text headerTmp = CreateText(
                    bannerGo.transform,
                    "Header_Text",
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero,
                    headerTitle,
                    27f,
                    new Color32(48, 20, 4, 255),
                    TextAlignmentOptions.Center,
                    useLoginFont: false,
                    readableBodyFont: true);
                var hrt = headerTmp.GetComponent<RectTransform>();
                hrt.pivot = new Vector2(0.5f, 0.5f);
                hrt.offsetMin = new Vector2(20f, 8f);
                hrt.offsetMax = new Vector2(-20f, -8f);
                headerTmp.fontStyle = FontStyles.Bold;
                headerTmp.textWrappingMode = TextWrappingModes.NoWrap;
                headerTmp.enableAutoSizing = true;
                headerTmp.fontSizeMin = 20f;
                headerTmp.fontSizeMax = 28f;
            }
            else
            {
                CreateText(
                    boardGo.transform,
                    "Header_Text",
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -44f),
                    new Vector2(Mathf.Max(260f, boardSize.x - 190f), 40f),
                    headerTitle,
                    27f,
                    new Color32(42, 16, 4, 255),
                    TextAlignmentOptions.Center,
                    useLoginFont: false,
                    readableBodyFont: false);
            }

            return overlayGo;
        }

        public static TMP_Text CreateText(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 size,
            string text,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            bool useLoginFont = false,
            bool readableBodyFont = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            go.layer = 5;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (readableBodyFont)
            {
                ThemeFontManager.ApplyStoryFont(tmp);
                tmp.fontStyle = FontStyles.Bold;
            }
            else if (useLoginFont)
            {
                ThemeFontManager.ApplyLoginFont(tmp);
                tmp.fontStyle = FontStyles.Normal;
            }
            else
            {
                TMP_FontAsset globalFont = ThemeFontManager.GetGlobalUiFont();
                if (globalFont != null) tmp.font = globalFont;
                else ThemeFontManager.ApplyStoryFont(tmp);
                tmp.fontStyle = FontStyles.Normal;
            }

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>Tạo nút icon gỗ đồng bộ từ bộ btn_components.</summary>
        public static Button CreateIconButton(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 anchoredPos,
            Vector2 size,
            string spriteResourcePath,
            UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.layer = 5;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            Sprite sp = Resources.Load<Sprite>(spriteResourcePath);
            if (sp != null) img.sprite = sp;
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.08f, 1.06f, 0.96f, 1f);
            cb.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
            cb.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.55f);
            btn.colors = cb;
            btn.onClick.AddListener(onClick);
            return btn;
        }

        public static Button CreatePillButton(
            Transform parent,
            string name,
            Vector2 anchoredPos,
            Vector2 size,
            string label,
            Color bgColor,
            UnityAction onClick,
            out TMP_Text labelTmp)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.layer = 5;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            Sprite pillSp = Resources.Load<Sprite>("UI/UI_Capsule_Dark");
            if (pillSp != null)
            {
                img.sprite = pillSp;
                img.type = Image.Type.Sliced;
            }
            img.color = bgColor;
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            btn.colors = cb;
            btn.onClick.AddListener(onClick);

            labelTmp = CreateText(
                go.transform,
                "Label",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                label,
                19f,
                Color.white,
                TextAlignmentOptions.Center);
            var lrt = labelTmp.GetComponent<RectTransform>();
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.offsetMin = new Vector2(6f, 2f);
            lrt.offsetMax = new Vector2(-6f, -2f);

            return btn;
        }

        /// <summary>Tạo nút bảng gỗ thủ công đồng bộ phong cách UI của trò chơi.</summary>
        public static Button CreateWoodButton(
            Transform parent,
            string name,
            Vector2 anchoredPos,
            Vector2 size,
            string label,
            Color tintColor,
            Color textColor,
            UnityAction onClick,
            out TMP_Text labelTmp,
            string spriteResourcePath = "UI/Btn_Plaque_Wood")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.layer = 5;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            Sprite woodSp = Resources.Load<Sprite>(spriteResourcePath) ?? Resources.Load<Sprite>("UI/Btn_Plaque_Wood");
            if (woodSp != null)
            {
                img.sprite = woodSp;
                img.preserveAspect = false;
            }
            img.color = tintColor;
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.08f, 1.06f, 0.96f, 1f);
            cb.pressedColor = new Color(0.86f, 0.84f, 0.80f, 1f);
            cb.disabledColor = new Color(0.84f, 0.80f, 0.74f, 0.92f);
            btn.colors = cb;
            btn.onClick.AddListener(onClick);

            labelTmp = CreateText(
                go.transform,
                "Label",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                label,
                19f,
                textColor,
                TextAlignmentOptions.Center);
            var lrt = labelTmp.GetComponent<RectTransform>();
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.offsetMin = new Vector2(22f, 12f);
            lrt.offsetMax = new Vector2(-22f, -12f);
            labelTmp.textWrappingMode = TextWrappingModes.NoWrap;
            labelTmp.enableAutoSizing = true;
            labelTmp.fontSizeMin = 13f;
            labelTmp.fontSizeMax = 19f;

            return btn;
        }

        private void BuildBody(GameObject overlay, RectTransform boardRt)
        {
            _root = overlay;

            CreateAudioRow(boardRt, "Music_Row", new Vector2(0f, 26f), "UI/Btn_Music_Wood", out _musicIcon, out _musicSlider);
            CreateAudioRow(boardRt, "Sound_Row", new Vector2(0f, -48f), "UI/Btn_Sound_Wood", out _soundIcon, out _soundSlider);

            if (_musicIcon != null)
            {
                AudioManager.BindMuteIconButton(_musicIcon.transform, () =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.ToggleBgmMute();
                });
            }
            if (_soundIcon != null)
            {
                AudioManager.BindMuteIconButton(_soundIcon.transform, () =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.ToggleSfxMute();
                });
            }

            if (_musicSlider != null)
            {
                _musicSlider.onValueChanged.AddListener(val =>
                {
                    if (_isSyncing || AudioManager.Instance == null) return;
                    AudioManager.Instance.SetBGMVolume(val);
                });
            }
            if (_soundSlider != null)
            {
                _soundSlider.onValueChanged.AddListener(val =>
                {
                    if (_isSyncing || AudioManager.Instance == null) return;
                    AudioManager.Instance.SetSFXVolume(val);
                });
            }

            CreateIconButton(
                boardRt,
                "Btn_Close_Audio",
                new Vector2(1f, 1f),
                new Vector2(-36f, -36f),
                new Vector2(56f, 56f),
                "UI/Btn_Close_Wood",
                Close);
        }

        private static void CreateAudioRow(
            Transform parent,
            string rowName,
            Vector2 anchoredPos,
            string iconResourcePath,
            out Image iconImg,
            out Slider slider)
        {
            var rowGo = new GameObject(rowName, typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            rowGo.layer = 5;
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 0.5f);
            rowRt.anchorMax = new Vector2(0.5f, 0.5f);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = anchoredPos;
            rowRt.sizeDelta = new Vector2(326f, 58f);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(rowGo.transform, false);
            iconGo.layer = 5;
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(29f, 0f);
            iconRt.sizeDelta = new Vector2(58f, 58f);
            iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = Resources.Load<Sprite>(iconResourcePath)
                ?? Resources.Load<Sprite>(iconResourcePath.Contains("Music") ? "UI/Icon_Music" : "UI/Icon_Sound");
            iconImg.preserveAspect = true;

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(rowGo.transform, false);
            sliderGo.layer = 5;
            var sliderRt = sliderGo.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0f, 0.5f);
            sliderRt.anchorMax = new Vector2(1f, 0.5f);
            sliderRt.pivot = new Vector2(0.5f, 0.5f);
            sliderRt.offsetMin = new Vector2(74f, -15f);
            sliderRt.offsetMax = new Vector2(-6f, 15f);

            Sprite trackSp = Resources.Load<Sprite>("UI/Slider_Track_Clean");
            Sprite fillSp = Resources.Load<Sprite>("UI/Slider_Fill_Green");
            Sprite knobSp = Resources.Load<Sprite>("UI/Pause_Slider_Knob");

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(sliderGo.transform, false);
            bgGo.layer = 5;
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.25f);
            bgRt.anchorMax = new Vector2(1f, 0.75f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            if (trackSp != null) { bgImg.sprite = trackSp; bgImg.type = Image.Type.Sliced; }
            else bgImg.color = new Color(0.18f, 0.16f, 0.14f, 1f);

            var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            fillAreaGo.layer = 5;
            var fillAreaRt = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRt.offsetMin = new Vector2(4f, 0f);
            fillAreaRt.offsetMax = new Vector2(-4f, 0f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            fillGo.layer = 5;
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.GetComponent<Image>();
            if (fillSp != null) { fillImg.sprite = fillSp; fillImg.type = Image.Type.Sliced; }
            else fillImg.color = new Color(0.32f, 0.78f, 0.28f, 1f);

            var handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            handleAreaGo.layer = 5;
            var handleAreaRt = handleAreaGo.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(8f, 0f);
            handleAreaRt.offsetMax = new Vector2(-8f, 0f);

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            handleGo.layer = 5;
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(22f, 28f);
            var handleImg = handleGo.GetComponent<Image>();
            if (knobSp != null) { handleImg.sprite = knobSp; handleImg.preserveAspect = true; }
            else handleImg.color = new Color(0.95f, 0.80f, 0.35f, 1f);

            slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
        }

        private void Awake()
        {
            EnsureBindings();
        }

        public void EnsureBindings()
        {
            if (_root == null) _root = gameObject;
            Transform board = transform.Find("Stone_Board");
            if (board == null) return;

            if (_musicIcon == null) _musicIcon = board.Find("Music_Row/Icon")?.GetComponent<Image>();
            if (_soundIcon == null) _soundIcon = board.Find("Sound_Row/Icon")?.GetComponent<Image>();
            if (_musicSlider == null) _musicSlider = board.Find("Music_Row/Slider")?.GetComponent<Slider>();
            if (_soundSlider == null) _soundSlider = board.Find("Sound_Row/Slider")?.GetComponent<Slider>();

            if (_musicIcon != null)
            {
                AudioManager.BindMuteIconButton(_musicIcon.transform, () =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.ToggleBgmMute();
                });
            }
            if (_soundIcon != null)
            {
                AudioManager.BindMuteIconButton(_soundIcon.transform, () =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.ToggleSfxMute();
                });
            }
            if (_musicSlider != null)
            {
                _musicSlider.onValueChanged.RemoveAllListeners();
                _musicSlider.onValueChanged.AddListener(val =>
                {
                    if (_isSyncing || AudioManager.Instance == null) return;
                    AudioManager.Instance.SetBGMVolume(val);
                });
            }
            if (_soundSlider != null)
            {
                _soundSlider.onValueChanged.RemoveAllListeners();
                _soundSlider.onValueChanged.AddListener(val =>
                {
                    if (_isSyncing || AudioManager.Instance == null) return;
                    AudioManager.Instance.SetSFXVolume(val);
                });
            }

            Button closeBtn = board.Find("Btn_Close_Audio")?.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(Close);
            }
        }

        private void OnEnable()
        {
            EnsureBindings();
            AudioManager.OnBgmVolumeChanged += HandleBgmChanged;
            AudioManager.OnSfxVolumeChanged += HandleSfxChanged;
            SyncFromAudioManager();
        }

        private void OnDisable()
        {
            AudioManager.OnBgmVolumeChanged -= HandleBgmChanged;
            AudioManager.OnSfxVolumeChanged -= HandleSfxChanged;
        }

        private void Update()
        {
            if (_root != null && _root.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        public void Open()
        {
            EnsureBindings();
            if (_root != null)
            {
                _root.transform.SetAsLastSibling();
                _root.SetActive(true);
            }
            SyncFromAudioManager();
        }

        public void Close()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            if (_root != null) _root.SetActive(false);
        }

        private void SyncFromAudioManager()
        {
            if (AudioManager.Instance == null) return;
            HandleBgmChanged(AudioManager.Instance.bgmVolume);
            HandleSfxChanged(AudioManager.Instance.sfxVolume);
        }

        private void HandleBgmChanged(float value)
        {
            _isSyncing = true;
            if (_musicSlider != null) _musicSlider.SetValueWithoutNotify(value);
            AudioManager.UpdateMuteIconVisual(_musicIcon, value <= 0.001f);
            _isSyncing = false;
        }

        private void HandleSfxChanged(float value)
        {
            _isSyncing = true;
            if (_soundSlider != null) _soundSlider.SetValueWithoutNotify(value);
            AudioManager.UpdateMuteIconVisual(_soundIcon, value <= 0.001f);
            _isSyncing = false;
        }
    }
}
