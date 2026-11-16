using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.Database;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Quan ly Trang Chon Level moi (LevelSelectScene.unity):
    /// - Nut Ngoi nha (btn_components_2 tu Assets/Art/Btn_map/btn_components.png) goc tren trai quay ve LoginScene.
    /// - 3 o chon man (1, 2, 3) can doi giua man hinh cung 3 ngoi sao tien do ben duoi moi o.
    /// - Chi mo san Man 1; Man 2 & 3 khoa bang icon o khoa (btn_components_3) cho toi khi thang man truoc trong SQLite.
    /// </summary>
    public class LevelSelectManager : MonoBehaviour
    {
        [System.Serializable]
        public class LevelSlotUI
        {
            public int levelNumber = 1;
            public string sceneName = "SampleScene";
            public string requiredClearedKey = string.Empty;
            public Button button;
            public GameObject numberGroup;
            public GameObject lockIconObject;
            public Image[] starImages;
            public RectTransform cardRect;
        }

        [Header("Navigation & UI")]
        [SerializeField] private Button homeButton;
        [SerializeField] private Button storyButton;
        [SerializeField] private Button difficultyButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button audioSettingsButton;
        [SerializeField] private StoryBookManager storyBookManager;
        [SerializeField] private TMP_Text pageTitleText;
        [SerializeField] private TMP_Text toastText;
        [SerializeField] private TMP_Text tireCountText;
        [SerializeField] private Image tireBadgeIcon;
        [SerializeField] private Sprite goldStarSprite;
        [SerializeField] private Sprite emptyStarSprite;
        [SerializeField] private LevelSlotUI[] levelSlots = new LevelSlotUI[6];

        private float _toastHideAt;
        private int _shakingSlotIndex = -1;
        private float _shakeTimer;
        private Vector2 _shakeOrigin;
        private TMP_Text _upgradeBtnLabel;
        private TMP_Text _difficultyBtnLabel;
        private Image _difficultyBtnBg;

        private void Awake()
        {
            if (homeButton != null)
            {
                homeButton.onClick.RemoveListener(OnHomeClicked);
                homeButton.onClick.AddListener(OnHomeClicked);
            }

            if (storyBookManager == null)
            {
                storyBookManager = FindFirstObjectByType<StoryBookManager>(FindObjectsInactive.Include);
            }
            if (storyButton != null)
            {
                storyButton.onClick.RemoveListener(OnStoryClicked);
                storyButton.onClick.AddListener(OnStoryClicked);
            }

            SetupTopHudQuickButtons();

            if (pageTitleText != null)
            {
                ThemeFontManager.ApplyLoginFont(pageTitleText);
                pageTitleText.color = Color.white;
            }
            if (toastText != null)
            {
                ThemeFontManager.ApplyGlobalUiFont(toastText);
                toastText.gameObject.SetActive(false);
            }
            if (tireCountText == null)
            {
                var badgeTf = GameObject.Find("Canvas_HUD/Top_Right_Bar/Tire_Counter_Badge/Txt_TireCount")
                    ?? GameObject.Find("Canvas_HUD/Tire_Counter_Badge/Txt_TireCount");
                if (badgeTf != null) tireCountText = badgeTf.GetComponent<TMP_Text>();
            }
            if (tireBadgeIcon == null)
            {
                var iconTf = GameObject.Find("Canvas_HUD/Top_Right_Bar/Tire_Counter_Badge/Icon_Tire")
                    ?? GameObject.Find("Canvas_HUD/Tire_Counter_Badge/Icon_Tire");
                if (iconTf != null) tireBadgeIcon = iconTf.GetComponent<Image>();
            }
            if (tireCountText != null)
            {
                ThemeFontManager.ApplyGlobalUiFont(tireCountText);
                tireCountText.color = new Color32(42, 24, 16, 255);
            }

            RefreshAllSlots();
            RefreshTireBadge();

            Transform upgradeModalTf = GameObject.Find("Canvas_HUD")?.transform.Find("Tire_Upgrade_Modal");
            if (upgradeModalTf != null && upgradeModalTf.gameObject.activeSelf)
            {
                upgradeModalTf.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            Time.timeScale = 1f;
            RefreshAllSlots();
            RefreshTireBadge();
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLoginBGM();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnHomeClicked();
            }

            if (toastText != null && toastText.gameObject.activeSelf && Time.unscaledTime > _toastHideAt)
            {
                toastText.gameObject.SetActive(false);
            }

            if (_shakingSlotIndex >= 0 && _shakingSlotIndex < levelSlots.Length && levelSlots[_shakingSlotIndex].cardRect != null)
            {
                _shakeTimer -= Time.unscaledDeltaTime;
                RectTransform rt = levelSlots[_shakingSlotIndex].cardRect;
                if (_shakeTimer <= 0f)
                {
                    rt.anchoredPosition = _shakeOrigin;
                    _shakingSlotIndex = -1;
                }
                else
                {
                    float offset = Mathf.Sin(_shakeTimer * 48f) * 10f * (_shakeTimer / 0.35f);
                    rt.anchoredPosition = new Vector2(_shakeOrigin.x + offset, _shakeOrigin.y);
                }
            }
        }

        /// <summary>
        /// Đọc tiến độ CSDL và cập nhật trạng thái mở/khóa cùng đúng 1-3 sao vàng cho từng màn 1-6.
        /// </summary>
        public void RefreshAllSlots()
        {
            if (levelSlots == null) return;

            for (int i = 0; i < levelSlots.Length; i++)
            {
                LevelSlotUI slot = levelSlots[i];
                if (slot == null) continue;

                bool unlocked = IsSlotUnlocked(i);
                int earnedStars = GameRepository.GetLevelStars(slot.sceneName);

                if (slot.numberGroup != null) slot.numberGroup.SetActive(unlocked);
                if (slot.lockIconObject != null) slot.lockIconObject.SetActive(!unlocked);

                if (slot.starImages != null)
                {
                    for (int s = 0; s < slot.starImages.Length; s++)
                    {
                        Image star = slot.starImages[s];
                        if (star == null) continue;
                        bool lit = s < earnedStars;
                        star.sprite = lit ? (goldStarSprite != null ? goldStarSprite : star.sprite) : (emptyStarSprite != null ? emptyStarSprite : star.sprite);
                        star.color = lit ? Color.white : new Color(0.55f, 0.58f, 0.64f, 0.92f);
                    }
                }

                if (slot.button != null)
                {
                    int capturedIndex = i;
                    slot.button.onClick.RemoveAllListeners();
                    slot.button.onClick.AddListener(() => OnLevelSlotClicked(capturedIndex));
                }
            }
        }

        public void RefreshTireBadge()
        {
            string availStr = GameRepository.GetAvailableTires().ToString();
            if (tireCountText != null)
            {
                tireCountText.text = availStr;
            }
            if (_upgradeBtnLabel != null)
            {
                _upgradeBtnLabel.text = availStr;
            }
            if (tireBadgeIcon != null && GameRepository.HasClaimedCampaign6PinkTireReward())
            {
                Sprite pinkSp = Resources.Load<Sprite>("UI/Pink_Tire");
                if (pinkSp != null)
                {
                    tireBadgeIcon.sprite = pinkSp;
                    tireBadgeIcon.color = Color.white;
                }
                else
                {
                    tireBadgeIcon.color = new Color(1f, 0.45f, 0.78f, 1f);
                }
            }
        }

        private void SetupTopHudQuickButtons()
        {
            GameObject hudObj = GameObject.Find("Canvas_HUD");
            if (hudObj == null) return;
            Transform hud = hudObj.transform;
            Transform bar = hud.Find("Top_Right_Bar");
            bool hasSceneBar = bar != null;

            Button audioBtn = audioSettingsButton != null ? audioSettingsButton : FindHudButton(hud, "Btn_Audio_Settings");
            if (audioBtn != null)
            {
                var img = audioBtn.GetComponent<Image>();
                Sprite gearSp = Resources.Load<Sprite>("UI/Btn_Settings_Wood")
                    ?? Resources.Load<Sprite>("UI/Btn_Settings")
                    ?? Resources.Load<Sprite>("UI/Icon_Sound");
                if (img != null && gearSp != null)
                {
                    img.sprite = gearSp;
                    img.preserveAspect = true;
                }
                audioBtn.onClick.RemoveAllListeners();
                audioBtn.onClick.AddListener(() =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                    AudioSettingsModal.ShowOrCreate(hud);
                });
            }
            else
            {
                AudioSettingsModal.AttachQuickAudioButton(
                    hud,
                    "Btn_Audio_Settings",
                    new Vector2(1f, 1f),
                    new Vector2(-192f, -84f),
                    new Vector2(82f, 82f));
            }

            if (!hasSceneBar)
            {
                if (storyButton != null)
                {
                    var srt = storyButton.GetComponent<RectTransform>();
                    if (srt != null)
                    {
                        srt.anchoredPosition = new Vector2(-84f, -84f);
                        srt.sizeDelta = new Vector2(86f, 86f);
                    }
                }
                var badgeRt = hud.Find("Tire_Counter_Badge") as RectTransform;
                if (badgeRt != null)
                {
                    badgeRt.anchoredPosition = new Vector2(-352f, -84f);
                    badgeRt.sizeDelta = new Vector2(194f, 68f);
                }
            }

            Button upgBtn = upgradeButton != null ? upgradeButton : FindHudButton(hud, "Btn_Upgrade_Tires");
            if (upgBtn == null)
            {
                upgBtn = AudioSettingsModal.CreateIconButton(
                    bar != null ? bar : hud,
                    "Btn_Upgrade_Tires",
                    new Vector2(1f, 1f),
                    new Vector2(-476f, -84f),
                    new Vector2(86f, 86f),
                    "UI/Btn_Upgrade_Wood",
                    () =>
                    {
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                        TireUpgradeModal.ShowOrCreate(hud, RefreshTireBadge);
                    });
                var urt = upgBtn.GetComponent<RectTransform>();
                if (urt != null && !hasSceneBar)
                {
                    urt.anchorMin = new Vector2(1f, 1f);
                    urt.anchorMax = new Vector2(1f, 1f);
                    urt.anchoredPosition = new Vector2(-476f, -84f);
                }
                _upgradeBtnLabel = EnsureCornerBadgeLabel(upgBtn.transform, GameRepository.GetAvailableTires().ToString());
            }
            else
            {
                var uImg = upgBtn.GetComponent<Image>();
                Sprite upgSp = Resources.Load<Sprite>("UI/Btn_Upgrade_Wood");
                if (uImg != null && upgSp != null)
                {
                    uImg.sprite = upgSp;
                    uImg.type = Image.Type.Simple;
                    uImg.preserveAspect = true;
                    uImg.color = Color.white;
                }
                _upgradeBtnLabel = EnsureCornerBadgeLabel(upgBtn.transform, GameRepository.GetAvailableTires().ToString());
                upgBtn.onClick.RemoveAllListeners();
                upgBtn.onClick.AddListener(() =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
                    TireUpgradeModal.ShowOrCreate(hud, RefreshTireBadge);
                });
            }

            if (storyButton != null)
            {
                var sImg = storyButton.GetComponent<Image>();
                Sprite menuSp = Resources.Load<Sprite>("UI/Btn_Menu_Wood");
                if (sImg != null && menuSp != null)
                {
                    sImg.sprite = menuSp;
                    sImg.type = Image.Type.Simple;
                    sImg.preserveAspect = true;
                    sImg.color = Color.white;
                }
            }

            Button diffBtn = difficultyButton != null ? difficultyButton : FindHudButton(hud, "Btn_Difficulty");
            if (diffBtn == null)
            {
                diffBtn = AudioSettingsModal.CreateWoodButton(
                    bar != null ? bar : hud,
                    "Btn_Difficulty",
                    new Vector2(-786f, -84f),
                    new Vector2(222f, 76f),
                    string.Empty,
                    Color.white,
                    new Color32(58, 28, 12, 255),
                    ToggleDifficultyMode,
                    out _difficultyBtnLabel);
                var drt = diffBtn.GetComponent<RectTransform>();
                if (drt != null && !hasSceneBar)
                {
                    drt.anchorMin = new Vector2(1f, 1f);
                    drt.anchorMax = new Vector2(1f, 1f);
                    drt.anchoredPosition = new Vector2(-786f, -84f);
                }
                _difficultyBtnBg = diffBtn.GetComponent<Image>();
            }
            else
            {
                _difficultyBtnLabel = diffBtn.GetComponentInChildren<TMP_Text>(true);
                _difficultyBtnBg = diffBtn.GetComponent<Image>();
                StylePlaqueButton(diffBtn, _difficultyBtnLabel, null, new Color32(58, 28, 12, 255), 21f, preserveLayout: hasSceneBar);
                diffBtn.onClick.RemoveAllListeners();
                diffBtn.onClick.AddListener(ToggleDifficultyMode);
            }
            RefreshDifficultyButtonVisual();
        }

        private static Button FindHudButton(Transform hud, string name)
        {
            if (hud == null) return null;
            Transform bar = hud.Find("Top_Right_Bar");
            Transform found = (bar != null ? bar.Find(name) : null) ?? hud.Find(name);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private static void StylePlaqueButton(Button btn, TMP_Text label, Sprite sprite, Color32 textColor, float fontSize, bool preserveLayout)
        {
            if (btn == null) return;
            if (!preserveLayout)
            {
                var rt = btn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(1f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                }
            }
            var img = btn.GetComponent<Image>();
            if (img != null && sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.color = Color.white;
            }
            if (label != null)
            {
                TMP_FontAsset bodyFont = Resources.Load<TMP_FontAsset>("Fonts/BeVietnamPro SDF");
                if (bodyFont != null) label.font = bodyFont;
                label.fontStyle = FontStyles.Bold;
                label.fontSize = fontSize;
                label.color = textColor;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.enableAutoSizing = true;
                label.fontSizeMin = 14f;
                label.fontSizeMax = fontSize;
                if (label.gameObject.activeInHierarchy && label.fontSharedMaterial != null && label.fontSharedMaterial.HasProperty("_OutlineWidth"))
                {
                    label.outlineWidth = 0f;
                }
                var lrt = label.GetComponent<RectTransform>();
                if (lrt != null)
                {
                    lrt.offsetMin = new Vector2(18f, 8f);
                    lrt.offsetMax = new Vector2(-18f, -8f);
                }
            }
        }

        private static TMP_Text EnsureCornerBadgeLabel(Transform parentBtn, string countText)
        {
            if (parentBtn == null) return null;
            Transform pill = parentBtn.Find("Badge_Pill");
            if (pill == null)
            {
                var pillGo = new GameObject("Badge_Pill", typeof(RectTransform), typeof(Image));
                pillGo.transform.SetParent(parentBtn, false);
                var prt = pillGo.GetComponent<RectTransform>();
                prt.anchorMin = new Vector2(1f, 0f);
                prt.anchorMax = new Vector2(1f, 0f);
                prt.pivot = new Vector2(1f, 0f);
                prt.anchoredPosition = new Vector2(-2f, 2f);
                prt.sizeDelta = new Vector2(26f, 24f);

                var pImg = pillGo.GetComponent<Image>();
                pImg.color = new Color32(42, 24, 14, 240);
                pImg.raycastTarget = false;
                pill = pillGo.transform;
            }

            // Re-use any existing TMP_Text if it was baked under parentBtn directly
            TMP_Text txt = pill.GetComponentInChildren<TMP_Text>(true);
            if (txt == null)
            {
                TMP_Text oldChild = parentBtn.GetComponentInChildren<TMP_Text>(true);
                if (oldChild != null && oldChild.transform.parent == parentBtn)
                {
                    oldChild.transform.SetParent(pill, false);
                    txt = oldChild;
                }
            }
            if (txt == null)
            {
                var tGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                tGo.transform.SetParent(pill, false);
                txt = tGo.GetComponent<TextMeshProUGUI>();
            }

            var trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TMP_FontAsset bodyFont = Resources.Load<TMP_FontAsset>("Fonts/BeVietnamPro SDF");
            if (bodyFont != null) txt.font = bodyFont;
            txt.fontStyle = FontStyles.Bold;
            txt.fontSize = 15f;
            txt.enableAutoSizing = false;
            txt.color = new Color32(255, 242, 198, 255);
            txt.alignment = TextAlignmentOptions.Center;
            txt.raycastTarget = false;
            txt.text = countText;
            return txt;
        }

        private void ToggleDifficultyMode()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            int nextMode = GameRepository.IsHardMode() ? 0 : 1;
            GameRepository.SetDifficultyMode(nextMode);
            RefreshDifficultyButtonVisual();

            if (toastText != null)
            {
                toastText.text = nextMode == 1
                    ? "Đã bật Độ khó: KHÓ (Quái/Boss +35~40% HP & Sát thương, +30% Lốp rơi!)"
                    : "Đã chuyển Độ khó: THƯỜNG";
                toastText.gameObject.SetActive(true);
                _toastHideAt = Time.unscaledTime + 2.6f;
            }
        }

        private void RefreshDifficultyButtonVisual()
        {
            bool hard = GameRepository.IsHardMode();
            if (_difficultyBtnBg != null)
            {
                Sprite targetSp = Resources.Load<Sprite>(hard ? "UI/Btn_Plaque_Hard" : "UI/Btn_Plaque_Wood");
                if (targetSp != null)
                {
                    _difficultyBtnBg.sprite = targetSp;
                    _difficultyBtnBg.type = Image.Type.Simple;
                    _difficultyBtnBg.preserveAspect = false;
                    _difficultyBtnBg.color = Color.white;
                }
                else
                {
                    _difficultyBtnBg.color = hard
                        ? new Color(0.68f, 0.18f, 0.16f, 0.96f)
                        : new Color(0.20f, 0.36f, 0.56f, 0.96f);
                }
            }
            if (_difficultyBtnLabel != null)
            {
                TMP_FontAsset bodyFont = Resources.Load<TMP_FontAsset>("Fonts/BeVietnamPro SDF");
                if (bodyFont != null) _difficultyBtnLabel.font = bodyFont;
                _difficultyBtnLabel.text = hard ? "Độ khó: KHÓ" : "Độ khó: Thường";
                _difficultyBtnLabel.fontStyle = FontStyles.Bold;
                _difficultyBtnLabel.textWrappingMode = TextWrappingModes.NoWrap;
                _difficultyBtnLabel.enableAutoSizing = true;
                _difficultyBtnLabel.fontSizeMin = 14f;
                _difficultyBtnLabel.fontSizeMax = 19.5f;
                _difficultyBtnLabel.color = hard
                    ? new Color32(255, 242, 198, 255)
                    : new Color32(52, 22, 6, 255);
                if (_difficultyBtnLabel.gameObject.activeInHierarchy && _difficultyBtnLabel.fontSharedMaterial != null && _difficultyBtnLabel.fontSharedMaterial.HasProperty("_OutlineWidth"))
                {
                    _difficultyBtnLabel.outlineWidth = 0f;
                }
            }
        }

        private static bool IsSceneCleared(string sceneKey)
        {
            if (string.IsNullOrEmpty(sceneKey)) return false;
            try
            {
                return GameRepository.GetConfigFloat($"{sceneKey}_cleared", 0f) > 0.5f;
            }
            catch
            {
                return false;
            }
        }

        private bool IsSlotUnlocked(int index)
        {
            if (levelSlots == null || index < 0 || index >= levelSlots.Length) return false;
            for (int i = 0; i <= index; i++)
            {
                LevelSlotUI s = levelSlots[i];
                if (s == null) return false;
                if (!string.IsNullOrEmpty(s.requiredClearedKey) && !IsSceneCleared(s.requiredClearedKey))
                {
                    return false;
                }
                if (i > 0)
                {
                    LevelSlotUI prev = levelSlots[i - 1];
                    if (prev != null && !string.IsNullOrEmpty(prev.sceneName) && !IsSceneCleared(prev.sceneName))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void OnLevelSlotClicked(int index)
        {
            if (levelSlots == null || index < 0 || index >= levelSlots.Length) return;
            LevelSlotUI slot = levelSlots[index];
            if (slot == null) return;

            bool unlocked = IsSlotUnlocked(index);
            if (!unlocked)
            {
                TriggerLockedFeedback(index, slot.levelNumber);
                return;
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            Time.timeScale = 1f;
            LoadingSceneController.LoadWithLoading(slot.sceneName);
        }

        private void TriggerLockedFeedback(int index, int levelNumber)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();

            if (levelSlots[index].cardRect != null)
            {
                if (_shakingSlotIndex == index)
                {
                    levelSlots[index].cardRect.anchoredPosition = _shakeOrigin;
                }
                _shakingSlotIndex = index;
                _shakeOrigin = levelSlots[index].cardRect.anchoredPosition;
                _shakeTimer = 0.35f;
            }

            if (toastText != null)
            {
                toastText.text = $"Hãy vượt qua Màn {levelNumber - 1} để mở Màn {levelNumber}!";
                toastText.gameObject.SetActive(true);
                _toastHideAt = Time.unscaledTime + 2.2f;
            }
        }

        public void OnHomeClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            Time.timeScale = 1f;
            SceneManager.LoadScene("LoginScene");
        }

        public void OnStoryClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();

            if (storyBookManager == null)
            {
                storyBookManager = FindFirstObjectByType<StoryBookManager>(FindObjectsInactive.Include);
            }
            if (storyBookManager != null)
            {
                storyBookManager.loadSceneOnComplete = false;
                storyBookManager.targetSceneName = string.Empty;
                storyBookManager.OpenBook();
            }
        }
    }
}
