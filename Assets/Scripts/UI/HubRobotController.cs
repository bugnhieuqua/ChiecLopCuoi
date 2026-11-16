using System.Collections.Generic;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Trợ lý Robot ở giữa bên Trái màn chiến đấu (Robot_Companion):
    /// - Nạp sprite Robot cắt nền (Resources/UI/Robot_Companion), đặt giữa-trái (88, 20) và giữ Circular_Movement_Controller ở góc trái-dưới (160, 160).
    /// - Tạo chuyển động nhún nổi + lắc nghiêng nhịp nhàng toàn khối thân robot.
    /// - Khung thoại nền trắng (UI/White_Card_Bg) mở ngay bên phải Robot, tự xoay vòng lời thoại & thách thức mỗi 7.5 giây.
    /// </summary>
    public class HubRobotController : MonoBehaviour
    {
        public const float DefaultCycleIntervalSeconds = 7.5f;
        public const float DefaultBubbleVisibleSeconds = 5.0f;
        public static readonly Vector2 RobotAnchoredPos = new Vector2(88f, 20f);
        public static readonly Vector2 RobotSize = new Vector2(148f, 178f);
        public static readonly Vector2 DefaultJoystickAnchoredPos = new Vector2(160f, 160f);

        private static Sprite _radialGlowSprite;
        private static Sprite _roundedBoxSprite;

        private RectTransform _visualRoot;
        private CanvasGroup _rootCanvasGroup;
        private CanvasGroup _bubbleCanvasGroup;
        private TMP_Text _headerText;
        private TMP_Text _speechText;
        private RectTransform _auraRt;

        private readonly List<string> _cycleMessages = new List<string>();
        private int _messageIndex;
        private float _cycleTimer;
        private float _bubbleVisibleTimer;
        private float _excitedTimer;
        private bool _bossUnlockedNotified;
        private bool _bossDefeatedNotified;
        private bool _lowHpWarned;
        private bool _initialShown;

        private CowLopPlayerController _player;
        private BattleManager _battleManager;

        public string CurrentDisplayedLine { get; private set; } = string.Empty;
        public float CurrentVisualTiltAngle => _visualRoot != null ? _visualRoot.localEulerAngles.z : 0f;

        public static HubRobotController EnsureMounted(GameObject hudCanvas)
        {
            if (hudCanvas == null) return null;

            RestoreMovementControllerPosition(hudCanvas.transform);

            Transform legacy = hudCanvas.transform.Find("Hub_robot");
            if (legacy != null) DestroyImmediate(legacy.gameObject);

            Transform existing = hudCanvas.transform.Find("Robot_Companion");
            GameObject robotGo = existing != null
                ? existing.gameObject
                : new GameObject("Robot_Companion", typeof(RectTransform), typeof(CanvasGroup));

            if (existing == null)
            {
                robotGo.transform.SetParent(hudCanvas.transform, false);
                robotGo.layer = hudCanvas.layer;

                // Đặt trên lớp HUD nền nhưng dưới Menu/Result/Victory/Guide
                int sibling = hudCanvas.transform.childCount - 1;
                for (int i = 0; i < hudCanvas.transform.childCount - 1; i++)
                {
                    string n = hudCanvas.transform.GetChild(i).name;
                    if (n.Contains("Menu") || n.Contains("Panel") || n.Contains("Pause") ||
                        n.Contains("Victory") || n.Contains("Defeat") || n.Contains("Guide"))
                    {
                        sibling = i;
                        break;
                    }
                }
                robotGo.transform.SetSiblingIndex(sibling);
            }

            var rt = robotGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = RobotAnchoredPos;
            rt.sizeDelta = RobotSize;

            var ctrl = robotGo.GetComponent<HubRobotController>();
            if (ctrl == null) ctrl = robotGo.AddComponent<HubRobotController>();
            ctrl.BuildVisualsIfNeeded();
            ctrl.RefreshLevelDialoguePool(SceneManager.GetActiveScene().name);
            return ctrl;
        }

        private static void RestoreMovementControllerPosition(Transform hudRoot)
        {
            if (hudRoot == null) return;
            Transform moveTf = hudRoot.Find("Circular_Movement_Controller");
            if (moveTf is RectTransform moveRt)
            {
                moveRt.anchorMin = Vector2.zero;
                moveRt.anchorMax = Vector2.zero;
                moveRt.pivot = new Vector2(0.5f, 0.5f);
                moveRt.anchoredPosition = DefaultJoystickAnchoredPos;
            }
        }

        private void Awake()
        {
            _rootCanvasGroup = GetComponent<CanvasGroup>();
            if (_rootCanvasGroup == null) _rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            BuildVisualsIfNeeded();
            RefreshLevelDialoguePool(SceneManager.GetActiveScene().name);
        }

        private void OnEnable()
        {
            _cycleTimer = 1.0f;
            _initialShown = false;
            _bossUnlockedNotified = false;
            _bossDefeatedNotified = false;
            _lowHpWarned = false;
        }

        private void Update()
        {
            if (_visualRoot == null) return;

            bool suppress = ShouldSuppressForOverlay();
            if (_rootCanvasGroup != null)
            {
                float targetRootAlpha = suppress ? 0f : 1f;
                _rootCanvasGroup.alpha = Mathf.MoveTowards(_rootCanvasGroup.alpha, targetRootAlpha, Time.unscaledDeltaTime * 7f);
                _rootCanvasGroup.blocksRaycasts = !suppress;
                _rootCanvasGroup.interactable = !suppress;
            }

            // Chuyển động nhún nổi + lắc nghiêng cả khối robot
            float t = Time.unscaledTime;
            float excitedBoost = _excitedTimer > 0f ? 1f : 0f;
            if (_excitedTimer > 0f) _excitedTimer = Mathf.Max(0f, _excitedTimer - Time.unscaledDeltaTime);

            float bobY = Mathf.Sin(t * (2.5f + excitedBoost * 2.2f)) * (5.5f + excitedBoost * 3.5f);
            float tiltZ = Mathf.Sin(t * (1.85f + excitedBoost * 3.2f)) * (6.2f + excitedBoost * 5.8f);
            float popScale = 1f + (_excitedTimer > 0f ? 0.10f * Mathf.Sin((_excitedTimer / 0.65f) * Mathf.PI) : 0f);

            _visualRoot.anchoredPosition = new Vector2(0f, bobY);
            _visualRoot.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
            _visualRoot.localScale = new Vector3(popScale, popScale, 1f);

            if (_auraRt != null)
            {
                float auraPulse = 1f + 0.08f * Mathf.Sin(t * 3.1f);
                _auraRt.localScale = new Vector3(auraPulse, auraPulse, 1f);
            }

            if (suppress) return;

            CheckCombatTriggers();
            TickDialogueCycle();
        }

        public void TriggerNextDialogue(string overrideHeader = null, string overrideLine = null)
        {
            if (_cycleMessages.Count == 0)
            {
                RefreshLevelDialoguePool(SceneManager.GetActiveScene().name);
            }

            string header = string.IsNullOrEmpty(overrideHeader) ? "TRỢ LÝ ROBOT" : overrideHeader;
            string line;
            if (!string.IsNullOrEmpty(overrideLine))
            {
                line = overrideLine;
            }
            else
            {
                line = _cycleMessages[_messageIndex % _cycleMessages.Count];
                _messageIndex = (_messageIndex + 1) % _cycleMessages.Count;
            }

            CurrentDisplayedLine = line;
            if (_headerText != null) _headerText.text = header;
            if (_speechText != null) _speechText.text = line;

            _bubbleVisibleTimer = DefaultBubbleVisibleSeconds;
            _cycleTimer = DefaultCycleIntervalSeconds;
            _excitedTimer = 0.65f;
        }

        private void CheckCombatTriggers()
        {
            if (_battleManager == null) _battleManager = BattleManager.Instance;
            if (_player == null) _player = FindFirstObjectByType<CowLopPlayerController>();

            if (!_initialShown)
            {
                if (_player == null || _player.IsGrounded)
                {
                    _initialShown = true;
                    TriggerNextDialogue();
                }
                return;
            }

            if (_battleManager != null && _battleManager.npcFemale != null && _battleManager.npcFemale.IsDead && !_bossDefeatedNotified)
            {
                _bossDefeatedNotified = true;
                TriggerNextDialogue("ĐÃ HẠ BOSS!", "Tuyệt vời! Hãy chạy tiếp về tượng CUỐI MAP (bên phải) để hoàn tất chiến thắng!");
                return;
            }

            if (_battleManager != null && _battleManager.npcFemale != null && _battleManager.npcFemale.combatUnlocked && !_bossUnlockedNotified)
            {
                _bossUnlockedNotified = true;
                TriggerNextDialogue("CẢNH BÁO BOSS!", "Tín hiệu Boss đã mở! Ta thách ngươi tung Xoay Lốp (3) + Đập Lốp (4) hạ gục trong 30 giây!");
                return;
            }

            if (_player != null && _player.maxHealth > 0f)
            {
                float hpRatio = _player.currentHealth / _player.maxHealth;
                if (hpRatio <= 0.35f && hpRatio > 0f && !_lowHpWarned)
                {
                    _lowHpWarned = true;
                    TriggerNextDialogue("NHẮC NHỞ KHẨN!", "Máu dưới 35% rồi! Hãy giữ phím 6 (H) để Đỡ đòn và chờ hồi máu từ quái!");
                }
                else if (hpRatio >= 0.6f)
                {
                    _lowHpWarned = false;
                }
            }
        }

        private void TickDialogueCycle()
        {
            if (_bubbleVisibleTimer > 0f)
            {
                _bubbleVisibleTimer = Mathf.Max(0f, _bubbleVisibleTimer - Time.unscaledDeltaTime);
            }

            if (_bubbleCanvasGroup != null)
            {
                float targetAlpha = _bubbleVisibleTimer > 0f ? 1f : 0f;
                _bubbleCanvasGroup.alpha = Mathf.MoveTowards(_bubbleCanvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 5.5f);
            }

            if (_initialShown)
            {
                _cycleTimer -= Time.unscaledDeltaTime;
                if (_cycleTimer <= 0f)
                {
                    TriggerNextDialogue();
                }
            }
        }

        private bool ShouldSuppressForOverlay()
        {
            if (SimpleTutorialController.HasActiveOverlay) return true;

            Transform hudRoot = transform.parent;
            if (hudRoot == null) return false;

            string[] modalNames = { "HUD_Menu", "Tutorial_Guide_Panel", "Result_KO_Panel", "Victory_Finale_Panel" };
            for (int i = 0; i < modalNames.Length; i++)
            {
                Transform t = hudRoot.Find(modalNames[i]);
                if (t != null && t.gameObject.activeInHierarchy) return true;
            }
            return false;
        }

        private void RefreshLevelDialoguePool(string sceneName)
        {
            _cycleMessages.Clear();
            _messageIndex = 0;

            switch (sceneName)
            {
                case "level2":
                    _cycleMessages.Add("Màn 2 mở khóa rồi! Ta cá ngươi không qua nổi bầy lính sa mạc mà mất ít hơn 30% máu!");
                    _cycleMessages.Add("Mẹo Robot: Khi đông quái áp sát, bấm 3 (U) Xoay Lốp để quét cả hai phía!");
                    _cycleMessages.Add("Nhớ dọn sạch trạm gác trước khi Boss Nữ mở khóa nhé Cow Lốp!");
                    break;
                case "level3":
                    _cycleMessages.Add("Thử thách Màn 3: Bẫy và quái dày hơn rồi đấy — dám giữ trọn 3 sao (>60 giây) không?");
                    _cycleMessages.Add("Dùng phím 2 (K) Ném Lốp từ xa để cấu rỉa trước khi lao vào áp sát!");
                    _cycleMessages.Add("Đừng quên nâng cấp Máu & Sát thương ở màn Chọn Màn bằng Lốp thưởng!");
                    break;
                case "level4":
                    _cycleMessages.Add("Màn 4! Ta đặt cược 1 chiếc lốp hồng: Boss màn này sẽ khiến ngươi phải dùng Đỡ đòn (6)!");
                    _cycleMessages.Add("Combo chuẩn: Đánh thường (1) x2 -> Đập Lốp (4) hất tung quái cực thấm!");
                    _cycleMessages.Add("Giữ Shift + A/D để chạy bứt tốc né chiêu diện rộng của Boss!");
                    break;
                case "level5":
                    _cycleMessages.Add("Áp chót Màn 5! Chỉ những cao thủ thực thụ mới hạ Boss màn này trong một hơi!");
                    _cycleMessages.Add("Hãy để ý thanh Revenge ở góc trên trái và canh nhịp tung tuyệt kỹ nhé!");
                    _cycleMessages.Add("Nhấn vào ta bất kỳ lúc nào nếu cần thêm gợi ý chiến thuật!");
                    break;
                case "level6":
                    _cycleMessages.Add("TRẬN CUỐI MÀN 6! Thắng màn này sẽ nhận ngay +30 Lốp Hồng vinh quang — Tiến lên!");
                    _cycleMessages.Add("Thử thách tối thượng: Hạ Boss cuối với mọi kỹ năng 1 -> 6 nhuần nhuyễn!");
                    _cycleMessages.Add("Bình tĩnh giữ 6 (H) đỡ đòn Boss rồi phản công ngay khi Boss vừa ra chiêu xong!");
                    break;
                default:
                    _cycleMessages.Add("Xin chào! Ta là Trợ Lý Robot! Ta thách ngươi phá đảo Màn 1 với trọn 3 sao (>60 giây còn lại)!");
                    _cycleMessages.Add("Thử ngay phím 1->4 để tung liên hoàn chiêu, phím 5 Nhảy và phím 6 Đỡ đòn!");
                    _cycleMessages.Add("Hạ gục quái có cơ hội rơi Thảo Dược hồi máu & Cầu Nộ đấy — hãy nhặt ngay!");
                    break;
            }
        }

        private void BuildVisualsIfNeeded()
        {
            Transform visualTf = transform.Find("Robot_Visual_Root");
            if (visualTf == null)
            {
                var visualGo = new GameObject("Robot_Visual_Root", typeof(RectTransform));
                visualGo.transform.SetParent(transform, false);
                visualGo.layer = gameObject.layer;
                _visualRoot = visualGo.GetComponent<RectTransform>();
                _visualRoot.anchorMin = Vector2.zero;
                _visualRoot.anchorMax = Vector2.one;
                _visualRoot.offsetMin = Vector2.zero;
                _visualRoot.offsetMax = Vector2.zero;
                _visualRoot.pivot = new Vector2(0.5f, 0.35f);

                // Vòng hào quang cyan nhẹ phía sau robot
                var auraGo = new GameObject("Robot_Aura", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                auraGo.transform.SetParent(_visualRoot, false);
                auraGo.layer = gameObject.layer;
                _auraRt = auraGo.GetComponent<RectTransform>();
                _auraRt.anchorMin = new Vector2(0.5f, 0.5f);
                _auraRt.anchorMax = new Vector2(0.5f, 0.5f);
                _auraRt.anchoredPosition = Vector2.zero;
                _auraRt.sizeDelta = new Vector2(150f, 150f);
                var auraImg = auraGo.GetComponent<Image>();
                auraImg.sprite = GetOrCreateRadialGlowSprite();
                auraImg.color = new Color(0.24f, 0.84f, 1f, 0.32f);
                auraImg.raycastTarget = false;

                // Ảnh Robot chính (cắt nền)
                var bodyGo = new GameObject("Img_Robot_Companion", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                bodyGo.transform.SetParent(_visualRoot, false);
                bodyGo.layer = gameObject.layer;
                var bodyRt = bodyGo.GetComponent<RectTransform>();
                bodyRt.anchorMin = Vector2.zero;
                bodyRt.anchorMax = Vector2.one;
                bodyRt.offsetMin = Vector2.zero;
                bodyRt.offsetMax = Vector2.zero;

                var bodyImg = bodyGo.GetComponent<Image>();
                bodyImg.sprite = Resources.Load<Sprite>("UI/Robot_Companion");
                bodyImg.preserveAspect = true;
                bodyImg.color = Color.white;
                bodyImg.raycastTarget = true;

                var btn = bodyGo.GetComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(OnRobotClicked);
            }
            else
            {
                _visualRoot = visualTf as RectTransform;
                _auraRt = _visualRoot.Find("Robot_Aura") as RectTransform;
                var img = _visualRoot.Find("Img_Robot_Companion")?.GetComponent<Image>();
                if (img != null && img.sprite == null)
                {
                    img.sprite = Resources.Load<Sprite>("UI/Robot_Companion");
                }
            }

            Transform bubbleTf = transform.Find("Robot_Speech_Bubble");
            if (bubbleTf == null)
            {
                var bubbleGo = new GameObject("Robot_Speech_Bubble", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                bubbleGo.transform.SetParent(transform, false);
                bubbleGo.layer = gameObject.layer;
                var brt = bubbleGo.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(1f, 0.5f);
                brt.anchorMax = new Vector2(1f, 0.5f);
                brt.pivot = new Vector2(0f, 0.5f);
                // Nằm ngay bên phải Robot ở giữa-trái, trên vòng di chuyển (160,160) và dưới thanh máu P1
                brt.anchoredPosition = new Vector2(12f, 12f);
                brt.sizeDelta = new Vector2(364f, 102f);

                var bgImg = bubbleGo.GetComponent<Image>();
                Sprite whiteCard = Resources.Load<Sprite>("UI/White_Card_Bg");
                if (whiteCard != null)
                {
                    bgImg.sprite = whiteCard;
                    bgImg.type = Image.Type.Simple;
                }
                else
                {
                    bgImg.sprite = GetOrCreateRoundedBoxSprite();
                    bgImg.type = Image.Type.Sliced;
                }
                bgImg.color = new Color(1f, 1f, 1f, 0.96f);
                bgImg.raycastTarget = false;

                var outline = bubbleGo.AddComponent<Outline>();
                outline.effectColor = new Color32(56, 189, 248, 210);
                outline.effectDistance = new Vector2(2f, -2f);

                _bubbleCanvasGroup = bubbleGo.GetComponent<CanvasGroup>();
                _bubbleCanvasGroup.alpha = 0f;
                _bubbleCanvasGroup.blocksRaycasts = false;
                _bubbleCanvasGroup.interactable = false;

                var headerGo = new GameObject("Txt_Robot_Header", typeof(RectTransform), typeof(TextMeshProUGUI));
                headerGo.transform.SetParent(bubbleGo.transform, false);
                headerGo.layer = gameObject.layer;
                var hrt = headerGo.GetComponent<RectTransform>();
                hrt.anchorMin = new Vector2(0f, 1f);
                hrt.anchorMax = new Vector2(1f, 1f);
                hrt.pivot = new Vector2(0.5f, 1f);
                hrt.offsetMin = new Vector2(16f, -26f);
                hrt.offsetMax = new Vector2(-14f, -6f);
                _headerText = headerGo.GetComponent<TMP_Text>();
                ThemeFontManager.ApplyStoryFont(_headerText);
                _headerText.fontSize = 15f;
                _headerText.fontStyle = FontStyles.Bold;
                _headerText.color = new Color32(12, 118, 184, 255);
                _headerText.alignment = TextAlignmentOptions.MidlineLeft;
                _headerText.text = "TRỢ LÝ ROBOT";
                _headerText.raycastTarget = false;

                var bodyTxtGo = new GameObject("Txt_Robot_Speech", typeof(RectTransform), typeof(TextMeshProUGUI));
                bodyTxtGo.transform.SetParent(bubbleGo.transform, false);
                bodyTxtGo.layer = gameObject.layer;
                var trt = bodyTxtGo.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(16f, 8f);
                trt.offsetMax = new Vector2(-14f, -26f);
                _speechText = bodyTxtGo.GetComponent<TMP_Text>();
                ThemeFontManager.ApplyStoryFont(_speechText);
                _speechText.fontSize = 17.5f;
                _speechText.fontStyle = FontStyles.Bold;
                _speechText.color = new Color32(30, 41, 59, 255);
                _speechText.alignment = TextAlignmentOptions.TopLeft;
                _speechText.enableWordWrapping = true;
                _speechText.overflowMode = TextOverflowModes.Truncate;
                _speechText.raycastTarget = false;
            }
            else
            {
                _bubbleCanvasGroup = bubbleTf.GetComponent<CanvasGroup>();
                _headerText = bubbleTf.Find("Txt_Robot_Header")?.GetComponent<TMP_Text>();
                _speechText = bubbleTf.Find("Txt_Robot_Speech")?.GetComponent<TMP_Text>();
            }
        }

        private void OnRobotClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            TriggerNextDialogue();
        }

        private static Sprite GetOrCreateRadialGlowSprite()
        {
            if (_radialGlowSprite != null) return _radialGlowSprite;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            _radialGlowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _radialGlowSprite;
        }

        private static Sprite GetOrCreateRoundedBoxSprite()
        {
            if (_roundedBoxSprite != null) return _roundedBoxSprite;
            const int size = 32;
            const int r = 6;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int cx = Mathf.Clamp(x, r, size - 1 - r);
                    int cy = Mathf.Clamp(y, r, size - 1 - r);
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float a = dist <= r ? 1f : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            _roundedBoxSprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(r + 2, r + 2, r + 2, r + 2));
            return _roundedBoxSprite;
        }
    }
}
