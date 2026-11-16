using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ChiecLopCuoi.NPC;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.Database;

namespace ChiecLopCuoi.Gameplay
{
    /// <summary>
    /// Quản lý sự kiện gặp mặt đối đầu (Duel Encounter Cutscene) & Bảng Tổng Kết Vinh Danh cuối màn:
    /// 1. Khi Cow Lốp vượt qua 4 chặng và chạm mặt Boss Nữ ở cuối màn -> Hiển thị "QUYẾT CHIẾN!".
    /// 2. Khi kết thúc trận đấu -> Hiển thị Bảng Tổng Kết Vinh Danh (Số quái tiêu diệt, Max Combo, Máu còn lại, Hạng S/A/B, +100 Lốp thưởng lưu SQLite Database).
    /// </summary>
    public partial class DuelEncounterManager : MonoBehaviour
    {
        public static DuelEncounterManager Instance { get; private set; }

        [Header("Characters")]
        public CowLopPlayerController cowLop;
        public NpcCombatController npcFemale;

        [Header("Encounter Trigger Settings")]
        public float encounterDistance = 4.2f;
        public float postEncounterSeparation = 3.2f;
        public float bannerDuration = 2.0f;

        [Header("Next Level Progression")]
        public string nextSceneName = "level2";

        [Header("Result_KO_Panel References (Level 1 Standard UI)")]
        public GameObject resultKoPanel;
        public TMP_Text resultHeaderText;
        [Tooltip("Font dự phòng nếu ThemeFontManager chưa sẵn sàng")]
        public TMP_FontAsset winLoseFont;
        public Image[] resultStarImages;
        public TMP_Text resultRewardText;
        public Sprite resultGoldStarSprite;
        public Sprite resultEmptyStarSprite;
        public Button btnRematch;
        public Button btnHome;
        public Button btnContinue;
        public Slider resultMusicSlider;
        public Slider resultSoundSlider;
        private Image _resultMusicIconImage;
        private Image _resultSoundIconImage;

        [Header("UI Banner / Result Elements (Encounter Cutscene Only)")]
        public GameObject encounterOverlayRoot;
        public TMP_Text titleText;
        public TMP_Text subtitleText;
        public Button actionButton;
        public TMP_Text actionButtonText;

        private bool _hasTriggeredEncounter;
        private bool _isShowingResult;
        private bool _lastResultPlayerWon;
        private bool _cachedHeaderDefaults;
        private Color _origHeaderColor;
        private bool _origHeaderEnableGradient;
        private VertexGradient _origHeaderGradient;
        private GameObject _koSplashOverlay;
        private Coroutine _resultSequenceRoutine;
        private Coroutine _tireFlyRoutine;
        private int _lastSecondsRemaining;
        private int _lastEarnedStars;
        private int _lastDeltaTires;
        private readonly List<GameObject> _activeRewardTires = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            string activeScene = SceneManager.GetActiveScene().name;
            if (activeScene == "SampleScene") nextSceneName = "level2";
            else if (activeScene == "level2") nextSceneName = "level3";
            else if (activeScene == "level3") nextSceneName = "level4";
            else if (activeScene == "level4") nextSceneName = "level5";
            else if (activeScene == "level5") nextSceneName = "level6";
            else if (activeScene == "level6") nextSceneName = "LevelSelectScene";
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
            AudioManager.OnBgmVolumeChanged += HandleResultBgmVolumeChanged;
            AudioManager.OnSfxVolumeChanged += HandleResultSfxVolumeChanged;
            SyncResultAudioControls();
        }

        private void OnDisable()
        {
            AudioManager.OnBgmVolumeChanged -= HandleResultBgmVolumeChanged;
            AudioManager.OnSfxVolumeChanged -= HandleResultSfxVolumeChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (cowLop == null) cowLop = FindFirstObjectByType<CowLopPlayerController>();
            if (npcFemale == null) npcFemale = FindFirstObjectByType<NpcCombatController>();

            if (npcFemale != null)
            {
                npcFemale.combatUnlocked = false;
            }

            BindResultKOPanel();
            if (resultKoPanel != null)
            {
                resultKoPanel.SetActive(false);
            }

            // Lớp K.O. splash có thể đang bật sẵn trong Scene để chỉnh sửa bằng
            // Inspector; luôn ẩn lúc bắt đầu, chỉ hiện trong chuỗi K.O.
            var hudTf = GameObject.Find("HUD_Canvas") != null ? GameObject.Find("HUD_Canvas").transform : null;
            var koSplashTf = hudTf != null ? hudTf.Find("KO_Splash_Overlay") : null;
            if (koSplashTf != null)
            {
                _koSplashOverlay = koSplashTf.gameObject;
                koSplashTf.gameObject.SetActive(false);
            }

            var notebookCtrl = hudTf != null ? hudTf.GetComponentInChildren<HUDNotebookController>(true) : null;
            if (notebookCtrl != null)
            {
                notebookCtrl.HideUnusedRoundSlotsAndKO();
            }

            EnsureEncounterUIExists();
            if (encounterOverlayRoot != null)
            {
                encounterOverlayRoot.SetActive(false);
            }
        }

        private void Update()
        {
            // Hỗ trợ phím tắt khi đang mở bảng Kết quả Result_KO_Panel
            bool panelActive = (resultKoPanel != null && resultKoPanel.activeSelf) ||
                               (encounterOverlayRoot != null && encounterOverlayRoot.activeSelf);
            if (_isShowingResult && panelActive)
            {
                if (_lastResultPlayerWon)
                {
                    if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        OnContinueClicked();
                        return;
                    }
                    if (Input.GetKeyDown(KeyCode.M))
                    {
                        OnReturnMainMenuClicked();
                        return;
                    }
                }
                else
                {
                    if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                    {
                        OnRetryClicked();
                        return;
                    }
                    if (Input.GetKeyDown(KeyCode.M))
                    {
                        OnReturnMainMenuClicked();
                        return;
                    }
                }
            }

            if (_hasTriggeredEncounter) return;
            if (cowLop == null || npcFemale == null) return;
            if (BattleManager.Instance != null && BattleManager.Instance.State != BattleManager.BattleState.Playing) return;

            float dist = Mathf.Abs(cowLop.transform.position.x - npcFemale.transform.position.x);
            if (dist <= encounterDistance)
            {
                _hasTriggeredEncounter = true;
                StartCoroutine(PlayEncounterSequence());
            }
        }

        private IEnumerator PlayEncounterSequence()
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.State = BattleManager.BattleState.EncounterCutscene;
                // Hiển thị thanh máu & combo thứ 2 (P2 - Boss) khi chính thức chạm mặt Boss!
                BattleManager.Instance.SetBossHUDVisible(true);
            }

            cowLop.SetUIMoveDirection(0);

            // Nhạc dồn dập cho đoạn chạm mặt Boss (tự trả lại nhạc nền chiến đấu sau đó)
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBossEncounterMusic(bannerDuration + 2.5f);
            }

            EnsureEncounterUIExists();
            if (encounterOverlayRoot != null)
            {
                if (subtitleText != null)
                {
                    subtitleText.gameObject.SetActive(false);
                }
                if (actionButton != null)
                {
                    actionButton.gameObject.SetActive(false);
                }

                encounterOverlayRoot.transform.SetAsLastSibling();
                encounterOverlayRoot.SetActive(true);
            }

            float center = (cowLop.transform.position.x + npcFemale.transform.position.x) * 0.5f;
            float leftTargetX = center - postEncounterSeparation * 0.5f;
            float rightTargetX = center + postEncounterSeparation * 0.5f;

            bool cowIsLeft = cowLop.transform.position.x <= npcFemale.transform.position.x;
            float cowGoalX = cowIsLeft ? leftTargetX : rightTargetX;
            float npcGoalX = cowIsLeft ? rightTargetX : leftTargetX;

            float elapsed = 0f;
            Vector3 cowStart = cowLop.transform.position;
            Vector3 npcStart = npcFemale.transform.position;

            while (elapsed < bannerDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.45f);
                float smooth = Mathf.SmoothStep(0f, 1f, t);

                cowLop.transform.position = new Vector3(
                    Mathf.Lerp(cowStart.x, cowGoalX, smooth),
                    cowStart.y,
                    cowStart.z
                );

                npcFemale.transform.position = new Vector3(
                    Mathf.Lerp(npcStart.x, npcGoalX, smooth),
                    npcStart.y,
                    npcStart.z
                );

                if (encounterOverlayRoot != null)
                {
                    float pulse = 1f + Mathf.Sin(elapsed * 12f) * 0.04f;
                    encounterOverlayRoot.transform.localScale = new Vector3(pulse, pulse, 1f);
                }

                yield return null;
            }

            if (encounterOverlayRoot != null)
            {
                encounterOverlayRoot.transform.localScale = Vector3.one;
                encounterOverlayRoot.SetActive(false);
            }

            if (npcFemale != null)
            {
                npcFemale.combatUnlocked = true;
            }

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.State = BattleManager.BattleState.Playing;
            }
        }

        private void EnsureEncounterUIExists()
        {
            var hudCanvas = GameObject.Find("HUD_Canvas");
            if (hudCanvas == null) return;

            Sprite duelBannerSprite = Resources.Load<Sprite>("UI/ui_boss_duel_banner");
            Sprite quyetChienSprite = Resources.Load<Sprite>("UI/ui_quyet_chien");

            var existing = hudCanvas.transform.Find("Duel_Encounter_Banner");
            if (existing != null)
            {
                encounterOverlayRoot = existing.gameObject;
                var titleTf = existing.Find("Banner_Title");
                var subTf = existing.Find("Banner_Subtitle");
                var btnTf = existing.Find("Action_Button");
                if (titleTf != null) titleText = titleTf.GetComponent<TMP_Text>();
                if (subTf != null)
                {
                    subtitleText = subTf.GetComponent<TMP_Text>();
                    subTf.gameObject.SetActive(false);
                }
                if (btnTf != null)
                {
                    actionButton = btnTf.GetComponent<Button>();
                    actionButtonText = btnTf.GetComponentInChildren<TMP_Text>();
                }
                ApplyBossDuelBannerVisuals(existing.gameObject, duelBannerSprite, quyetChienSprite, titleText, subtitleText, false);
                return;
            }

            // 1. Tạo thanh băng rôn chiến trận Quyết Chiến Boss
            var rootGo = new GameObject("Duel_Encounter_Banner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            rootGo.transform.SetParent(hudCanvas.transform, false);

            if (duelBannerSprite == null)
            {
                CreateBorderLine(rootGo.transform, "Border_Top", new Vector2(0.5f, 1f), new Vector2(0f, -4f));
                CreateBorderLine(rootGo.transform, "Border_Bottom", new Vector2(0.5f, 0f), new Vector2(0f, 4f));
            }

            // 2. Chữ tiêu đề dự phòng (chỉ bật nếu chưa có sprite UI/ui_quyet_chien)
            var titleGo = new GameObject("Banner_Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(rootGo.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(1100f, 90f);
            titleRt.anchoredPosition = Vector2.zero;

            var tmpTitle = titleGo.GetComponent<TextMeshProUGUI>();
            ChiecLopCuoi.UI.ThemeFontManager.ApplyGlobalUiFont(tmpTitle);
            tmpTitle.text = "QUYẾT CHIẾN!";
            tmpTitle.fontSize = 54;
            tmpTitle.characterSpacing = 1f;
            tmpTitle.alignment = TextAlignmentOptions.Center;
            tmpTitle.color = new Color(1f, 0.86f, 0.22f, 1f);
            tmpTitle.fontStyle = FontStyles.Normal;
            tmpTitle.raycastTarget = false;
            titleText = tmpTitle;

            // 3. Nút hành động (ẩn mặc định)
            var btnGo = new GameObject("Action_Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(rootGo.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0.5f);
            btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.sizeDelta = new Vector2(460f, 54f);
            btnRt.anchoredPosition = new Vector2(0f, -96f);

            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = new Color(0.15f, 0.72f, 0.32f, 1f);
            btnImg.raycastTarget = true;
            actionButton = btnGo.GetComponent<Button>();

            var btnTextGo = new GameObject("Button_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRt = btnTextGo.GetComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero;
            btnTextRt.anchorMax = Vector2.one;
            btnTextRt.sizeDelta = Vector2.zero;
            btnTextRt.anchoredPosition = Vector2.zero;

            var tmpBtn = btnTextGo.GetComponent<TextMeshProUGUI>();
            ChiecLopCuoi.UI.ThemeFontManager.ApplyGlobalUiFont(tmpBtn);
            tmpBtn.text = "TIẾP TỤC";
            tmpBtn.fontSize = 21;
            tmpBtn.alignment = TextAlignmentOptions.Center;
            tmpBtn.color = Color.white;
            tmpBtn.fontStyle = FontStyles.Bold;
            tmpBtn.raycastTarget = false;
            actionButtonText = tmpBtn;

            btnGo.SetActive(false);
            encounterOverlayRoot = rootGo;
            ApplyBossDuelBannerVisuals(rootGo, duelBannerSprite, quyetChienSprite, titleText, null, true);
        }

        private static void ApplyBossDuelBannerVisuals(GameObject rootGo, Sprite duelBannerSprite, Sprite quyetChienSprite, TMP_Text title, TMP_Text subtitle, bool applyLayout)
        {
            if (rootGo == null) return;

            var rt = rootGo.GetComponent<RectTransform>();
            if (rt != null && applyLayout)
            {
                rt.anchorMin = duelBannerSprite != null ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f);
                rt.anchorMax = duelBannerSprite != null ? new Vector2(0.5f, 0.5f) : new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 40f);
                rt.sizeDelta = duelBannerSprite != null ? new Vector2(1240f, 200f) : new Vector2(0f, 220f);
            }

            var bgImg = rootGo.GetComponent<Image>();
            if (bgImg != null)
            {
                if (duelBannerSprite != null)
                {
                    bgImg.sprite = duelBannerSprite;
                    bgImg.type = Image.Type.Simple;
                    bgImg.preserveAspect = false;
                    bgImg.color = Color.white;
                }
                else
                {
                    bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.90f);
                }
                bgImg.raycastTarget = true;
            }

            if (subtitle != null)
            {
                subtitle.gameObject.SetActive(false);
            }

            if (duelBannerSprite != null)
            {
                var topLine = rootGo.transform.Find("Border_Top");
                if (topLine != null) topLine.gameObject.SetActive(false);
                var botLine = rootGo.transform.Find("Border_Bottom");
                if (botLine != null) botLine.gameObject.SetActive(false);
            }

            Transform graphicTf = rootGo.transform.Find("Banner_Graphic");
            if (quyetChienSprite != null)
            {
                GameObject graphicGo = graphicTf != null
                    ? graphicTf.gameObject
                    : new GameObject("Banner_Graphic", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                if (graphicTf == null) graphicGo.transform.SetParent(rootGo.transform, false);

                var grt = graphicGo.GetComponent<RectTransform>();
                grt.anchorMin = new Vector2(0.5f, 0.5f);
                grt.anchorMax = new Vector2(0.5f, 0.5f);
                grt.pivot = new Vector2(0.5f, 0.5f);
                grt.anchoredPosition = new Vector2(0f, 2f);
                grt.sizeDelta = new Vector2(860f, 196f);

                var gimg = graphicGo.GetComponent<Image>();
                gimg.sprite = quyetChienSprite;
                gimg.preserveAspect = true;
                gimg.color = Color.white;
                gimg.raycastTarget = false;
                graphicGo.SetActive(true);
                graphicGo.transform.SetAsLastSibling();

                if (title != null) title.gameObject.SetActive(false);
            }
            else if (title != null)
            {
                if (graphicTf != null) graphicTf.gameObject.SetActive(false);
                title.gameObject.SetActive(true);
                title.text = "QUYẾT CHIẾN!";
                if (title.rectTransform != null)
                {
                    title.rectTransform.anchoredPosition = Vector2.zero;
                }
            }
        }

        private void CreateBorderLine(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos)
        {
            var lineGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lineGo.transform.SetParent(parent, false);
            var rt = lineGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, anchor.y);
            rt.anchorMax = new Vector2(1f, anchor.y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(0f, 5f);

            var img = lineGo.GetComponent<Image>();
            img.color = new Color(1f, 0.78f, 0.2f, 0.95f);
            img.raycastTarget = false;
        }

    }
}
