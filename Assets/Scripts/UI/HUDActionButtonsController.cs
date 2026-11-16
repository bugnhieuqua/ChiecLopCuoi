using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.Gameplay;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Cụm nút hành động bằng biểu tượng trực quan ở góc dưới phải màn hình (không hiển thị phím tắt).
    /// Cho phép người chơi bấm chuột / chạm để Đánh, Xoay Lốp, Ném Lốp, Đập Lốp, Nhảy và Đỡ Đòn.
    /// Tự động gắn lên HUD_Canvas trong các màn chơi có CowLopPlayerController mà không cần chỉnh Scene thủ công.
    /// </summary>
    [DisallowMultipleComponent]
    public class HUDActionButtonsController : MonoBehaviour
    {
        private enum ActionKind
        {
            Attack,
            TireSpin,
            TireThrow,
            JumpSlam,
            Jump,
            Block
        }

        private class ButtonSlot
        {
            public ActionKind kind;
            public RectTransform rect;
            public Image iconImage;
            public Image cooldownOverlay;
            public Image rageGlowRing;
            public float targetScale = 1f;
            public bool isHovered;
            public bool isPressed;
            public bool isKeyboardPressed;
        }

        private class ActionButtonPointerHandler : MonoBehaviour,
            IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
        {
            public HUDActionButtonsController owner;
            public ButtonSlot slot;

            public void OnPointerDown(PointerEventData eventData)
            {
                if (owner == null || slot == null) return;
                slot.isPressed = true;
                owner.HandleButtonPressed(slot, true);
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (owner == null || slot == null) return;
                slot.isPressed = false;
                owner.HandleButtonPressed(slot, false);
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (slot != null) slot.isHovered = true;
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (owner == null || slot == null) return;
                slot.isHovered = false;
                if (slot.isPressed)
                {
                    slot.isPressed = false;
                    owner.HandleButtonPressed(slot, false);
                }
            }
        }

        private const string ActionSpritesResourcePath = "UI/ui_action_buttons";

        private readonly List<ButtonSlot> _slots = new List<ButtonSlot>();
        private CowLopPlayerController _player;
        private CanvasGroup _canvasGroup;
        private Sprite _circleDiscSprite;
        private Sprite _circleRingSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapOnSceneLoaded()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryAttachToCurrentScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryAttachToCurrentScene();
        }

        public static void TryAttachToCurrentScene()
        {
            var player = FindAnyObjectByType<CowLopPlayerController>();
            if (player == null) return;

            var hudCanvas = GameObject.Find("HUD_Canvas");
            if (hudCanvas == null) return;

            VoiceCommandController.EnsureMounted(hudCanvas);

            Transform existingHub = hudCanvas.transform.Find("Bottom_Right_Action_Hub");
            if (existingHub != null)
            {
                var ctrl = existingHub.GetComponent<HUDActionButtonsController>();
                if (ctrl == null) ctrl = existingHub.gameObject.AddComponent<HUDActionButtonsController>();
                ctrl.BindPlayer(player);
                return;
            }

            GameObject hubGo = new GameObject("Bottom_Right_Action_Hub", typeof(RectTransform), typeof(CanvasGroup));
            hubGo.transform.SetParent(hudCanvas.transform, false);

            // Đặt ngay trên các thanh HUD cơ bản nhưng bên dưới các panel Pause / Hướng dẫn / Thắng-Thua
            int targetSiblingIndex = hudCanvas.transform.childCount - 1;
            for (int i = 0; i < hudCanvas.transform.childCount - 1; i++)
            {
                string childName = hudCanvas.transform.GetChild(i).name;
                if (childName.Contains("Menu") || childName.Contains("Panel") || childName.Contains("Pause") ||
                    childName.Contains("Victory") || childName.Contains("Defeat") || childName.Contains("Guide"))
                {
                    targetSiblingIndex = i;
                    break;
                }
            }
            hubGo.transform.SetSiblingIndex(targetSiblingIndex);

            RectTransform hubRt = hubGo.GetComponent<RectTransform>();
            hubRt.anchorMin = new Vector2(1f, 0f);
            hubRt.anchorMax = new Vector2(1f, 0f);
            hubRt.pivot = new Vector2(1f, 0f);
            hubRt.anchoredPosition = new Vector2(-28f, 24f);
            hubRt.sizeDelta = new Vector2(360f, 345f);

            var controller = hubGo.AddComponent<HUDActionButtonsController>();
            controller.BindPlayer(player);
        }

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            EnsureProceduralCircleSprites();
            BuildButtonsIfNeeded();
        }

        private void OnDisable()
        {
            if (_player != null) _player.SetUIBlock(false);
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].isPressed = false;
                _slots[i].isKeyboardPressed = false;
                _slots[i].isHovered = false;
            }
        }

        public void BindPlayer(CowLopPlayerController player)
        {
            _player = player;
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<CowLopPlayerController>();
            }

            bool isPlaying = _player != null &&
                             _player.enabled &&
                             _player.gameObject.activeInHierarchy &&
                             (BattleManager.Instance == null || BattleManager.Instance.State == BattleManager.BattleState.Playing) &&
                             Time.timeScale > 0.01f;

            if (_canvasGroup != null)
            {
                float targetAlpha = isPlaying ? 1f : 0f;
                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 8f);
                _canvasGroup.interactable = isPlaying;
                _canvasGroup.blocksRaycasts = isPlaying;
            }

            if (!isPlaying && _player != null)
            {
                _player.SetUIBlock(false);
            }

            UpdateSlotVisuals(isPlaying);
        }

        private void UpdateSlotVisuals(bool isPlaying)
        {
            bool fullRevenge = BattleManager.Instance != null && BattleManager.Instance.p1Revenge >= 100f;
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * 5.5f);

            for (int i = 0; i < _slots.Count; i++)
            {
                ButtonSlot slot = _slots[i];
                if (slot == null || slot.rect == null) continue;

                slot.isKeyboardPressed = isPlaying && IsKeyboardKeyHeld(slot.kind);

                float cooldown01 = GetCooldownNormalized(slot.kind);
                if (slot.cooldownOverlay != null)
                {
                    slot.cooldownOverlay.fillAmount = cooldown01;
                    slot.cooldownOverlay.enabled = cooldown01 > 0.005f;
                }

                if (slot.rageGlowRing != null)
                {
                    bool showRage = fullRevenge && cooldown01 <= 0.01f &&
                                    (slot.kind == ActionKind.TireSpin || slot.kind == ActionKind.JumpSlam || slot.kind == ActionKind.TireThrow);
                    slot.rageGlowRing.enabled = showRage;
                    if (showRage)
                    {
                        slot.rageGlowRing.color = new Color(1f, 0.84f, 0.22f, 0.45f + 0.45f * pulse);
                        float ringScale = 1.06f + 0.06f * pulse;
                        slot.rageGlowRing.rectTransform.localScale = new Vector3(ringScale, ringScale, 1f);
                    }
                }

                bool unavailable = cooldown01 > 0.005f || (slot.kind == ActionKind.Jump && _player != null && !_player.IsGrounded);
                if (slot.iconImage != null)
                {
                    Color targetTint = unavailable
                        ? new Color(0.72f, 0.74f, 0.78f, 0.88f)
                        : (slot.kind == ActionKind.Block && _player != null && _player.IsBlocking
                            ? new Color(0.85f, 1f, 0.85f, 1f)
                            : Color.white);
                    slot.iconImage.color = Color.Lerp(slot.iconImage.color, targetTint, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
                }

                float desiredScale = 1f;
                if (slot.isPressed || slot.isKeyboardPressed) desiredScale = 0.88f;
                else if (slot.isHovered && !unavailable) desiredScale = 1.07f;

                slot.targetScale = Mathf.Lerp(slot.targetScale, desiredScale, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                slot.rect.localScale = new Vector3(slot.targetScale, slot.targetScale, 1f);
            }
        }

        private static bool IsKeyboardKeyHeld(ActionKind kind)
        {
            switch (kind)
            {
                case ActionKind.Attack:    return CowLopPlayerController.IsSkillInputHeld(1);
                case ActionKind.TireThrow: return CowLopPlayerController.IsSkillInputHeld(2);
                case ActionKind.TireSpin:  return CowLopPlayerController.IsSkillInputHeld(3);
                case ActionKind.JumpSlam:  return CowLopPlayerController.IsSkillInputHeld(4);
                case ActionKind.Jump:      return CowLopPlayerController.IsSkillInputHeld(5);
                case ActionKind.Block:     return CowLopPlayerController.IsSkillInputHeld(6);
                default:                   return false;
            }
        }

        private float GetCooldownNormalized(ActionKind kind)
        {
            if (_player == null) return 0f;
            switch (kind)
            {
                case ActionKind.Attack: return _player.AttackCooldown01;
                case ActionKind.TireSpin: return _player.TireSpinCooldown01;
                case ActionKind.TireThrow: return _player.TireThrowCooldown01;
                case ActionKind.JumpSlam: return _player.JumpSlamCooldown01;
                default: return 0f;
            }
        }

        private void HandleButtonPressed(ButtonSlot slot, bool down)
        {
            if (_player == null || !_player.CanAcceptActionInput) return;

            switch (slot.kind)
            {
                case ActionKind.Attack:
                    if (down) _player.TriggerAttack();
                    break;
                case ActionKind.TireSpin:
                    if (down) _player.TriggerTireSpin();
                    break;
                case ActionKind.TireThrow:
                    if (down) _player.TriggerTireThrow();
                    break;
                case ActionKind.JumpSlam:
                    if (down) _player.TriggerJumpSlam();
                    break;
                case ActionKind.Jump:
                    if (down) _player.TriggerJump();
                    break;
                case ActionKind.Block:
                    _player.SetUIBlock(down);
                    break;
            }
        }

        private void BuildButtonsIfNeeded()
        {
            if (_slots.Count > 0) return;

            Dictionary<string, Sprite> sprites = LoadActionSprites();

            // Cụm vòng cung công thái học ở góc dưới phải (gán số 1-6 để đọc bằng giọng nói)
            CreateButtonSlot(ActionKind.Attack, "Btn_Action_Attack", GetSprite(sprites, "btn_action_attack"),
                new Vector2(-74f, 76f), 114f, supportsRageRing: false, skillNumberLabel: "1");

            CreateButtonSlot(ActionKind.TireThrow, "Btn_Action_TireThrow", GetSprite(sprites, "btn_action_throw"),
                new Vector2(-198f, 56f), 88f, supportsRageRing: true, skillNumberLabel: "2");

            CreateButtonSlot(ActionKind.TireSpin, "Btn_Action_TireSpin", GetSprite(sprites, "btn_action_spin"),
                new Vector2(-168f, 174f), 88f, supportsRageRing: true, skillNumberLabel: "3");

            CreateButtonSlot(ActionKind.JumpSlam, "Btn_Action_JumpSlam", GetSprite(sprites, "btn_action_slam"),
                new Vector2(-64f, 204f), 88f, supportsRageRing: true, skillNumberLabel: "4");

            CreateButtonSlot(ActionKind.Jump, "Btn_Action_Jump", GetSprite(sprites, "btn_action_jump"),
                new Vector2(-168f, 282f), 80f, supportsRageRing: false, skillNumberLabel: "5");

            CreateButtonSlot(ActionKind.Block, "Btn_Action_Block", GetSprite(sprites, "btn_action_block"),
                new Vector2(-292f, 152f), 80f, supportsRageRing: false, skillNumberLabel: "6");
        }

        private void CreateButtonSlot(ActionKind kind, string goName, Sprite iconSprite, Vector2 centerFromBottomRight, float size, bool supportsRageRing, string skillNumberLabel = null)
        {
            Transform existingTf = transform.Find(goName);
            bool isNew = existingTf == null;
            GameObject btnGo = isNew
                ? new GameObject(goName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                : existingTf.gameObject;
            if (isNew) btnGo.transform.SetParent(transform, false);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            if (rt == null) rt = btnGo.AddComponent<RectTransform>();

            // Chỉ áp bố cục mặc định khi tạo mới. Nếu nút đã có trong Scene (được
            // chỉnh tay trong Inspector) thì giữ nguyên vị trí/kích thước đã lưu.
            if (isNew)
            {
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = centerFromBottomRight;
                rt.sizeDelta = new Vector2(size, size);
            }

            Image iconImg = btnGo.GetComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = true;

            Image glowRing = null;
            if (supportsRageRing)
            {
                Transform ringTf = btnGo.transform.Find("Rage_Ring");
                GameObject ringGo = ringTf != null
                    ? ringTf.gameObject
                    : new GameObject("Rage_Ring", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                if (ringTf == null) ringGo.transform.SetParent(btnGo.transform, false);
                ringGo.transform.SetAsFirstSibling();

                RectTransform ringRt = ringGo.GetComponent<RectTransform>();
                ringRt.anchorMin = Vector2.zero;
                ringRt.anchorMax = Vector2.one;
                ringRt.offsetMin = new Vector2(-6f, -6f);
                ringRt.offsetMax = new Vector2(6f, 6f);

                glowRing = ringGo.GetComponent<Image>();
                glowRing.sprite = _circleRingSprite;
                glowRing.color = new Color(1f, 0.84f, 0.22f, 0f);
                glowRing.raycastTarget = false;
                glowRing.enabled = false;
            }

            Transform cdTf = btnGo.transform.Find("Cooldown_Sweep");
            GameObject cdGo = cdTf != null
                ? cdTf.gameObject
                : new GameObject("Cooldown_Sweep", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (cdTf == null) cdGo.transform.SetParent(btnGo.transform, false);

            RectTransform cdRt = cdGo.GetComponent<RectTransform>();
            cdRt.anchorMin = Vector2.zero;
            cdRt.anchorMax = Vector2.one;
            cdRt.offsetMin = new Vector2(3f, 3f);
            cdRt.offsetMax = new Vector2(-3f, -3f);

            Image cdImg = cdGo.GetComponent<Image>();
            cdImg.sprite = _circleDiscSprite;
            cdImg.type = Image.Type.Filled;
            cdImg.fillMethod = Image.FillMethod.Radial360;
            cdImg.fillOrigin = (int)Image.Origin360.Top;
            cdImg.fillClockwise = false;
            cdImg.fillAmount = 0f;
            cdImg.color = new Color(0.04f, 0.05f, 0.09f, 0.68f);
            cdImg.raycastTarget = false;
            cdImg.enabled = false;

            ButtonSlot slot = new ButtonSlot
            {
                kind = kind,
                rect = rt,
                iconImage = iconImg,
                cooldownOverlay = cdImg,
                rageGlowRing = glowRing
            };

            if (!string.IsNullOrEmpty(skillNumberLabel))
            {
                EnsureSkillNumberBadge(btnGo.transform, skillNumberLabel);
            }

            ActionButtonPointerHandler handler = btnGo.GetComponent<ActionButtonPointerHandler>();
            if (handler == null) handler = btnGo.AddComponent<ActionButtonPointerHandler>();
            handler.owner = this;
            handler.slot = slot;

            _slots.Add(slot);
        }

        private void EnsureSkillNumberBadge(Transform btnTransform, string numberText)
        {
            Transform existingBadge = btnTransform.Find("Skill_Number_Badge");
            GameObject badgeGo = existingBadge != null
                ? existingBadge.gameObject
                : new GameObject("Skill_Number_Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (existingBadge == null) badgeGo.transform.SetParent(btnTransform, false);

            RectTransform brt = badgeGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(10f, -10f);
            brt.sizeDelta = new Vector2(28f, 28f);

            Image bgImg = badgeGo.GetComponent<Image>();
            bgImg.sprite = _circleDiscSprite;
            bgImg.color = new Color(0.08f, 0.06f, 0.04f, 0.90f);
            bgImg.raycastTarget = false;

            Transform txtTf = badgeGo.transform.Find("Number");
            GameObject txtGo = txtTf != null
                ? txtTf.gameObject
                : new GameObject("Number", typeof(RectTransform), typeof(TextMeshProUGUI));
            if (txtTf == null) txtGo.transform.SetParent(badgeGo.transform, false);

            RectTransform trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TMP_Text tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.text = numberText;
            tmp.fontSize = 18f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.86f, 0.25f, 1f);
            tmp.raycastTarget = false;
            TMP_FontAsset cleanFont = ThemeFontManager.GetStoryFont();
            if (cleanFont != null) tmp.font = cleanFont;
            badgeGo.transform.SetAsLastSibling();
        }

        private static Dictionary<string, Sprite> LoadActionSprites()
        {
            var dict = new Dictionary<string, Sprite>();
            Sprite[] loaded = Resources.LoadAll<Sprite>(ActionSpritesResourcePath);
            if (loaded != null)
            {
                for (int i = 0; i < loaded.Length; i++)
                {
                    if (loaded[i] != null)
                    {
                        dict[loaded[i].name] = loaded[i];
                    }
                }
            }
            return dict;
        }

        private static Sprite GetSprite(Dictionary<string, Sprite> dict, string key)
        {
            return dict != null && dict.TryGetValue(key, out Sprite s) ? s : null;
        }

        private void EnsureProceduralCircleSprites()
        {
            if (_circleDiscSprite != null && _circleRingSprite != null) return;

            const int size = 96;
            float half = (size - 1) * 0.5f;

            Texture2D discTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Texture2D ringTex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float discAlpha = Mathf.Clamp01((0.94f - dist) * 28f);
                    discTex.SetPixel(x, y, new Color(1f, 1f, 1f, discAlpha));

                    float outer = Mathf.Clamp01((0.98f - dist) * 24f);
                    float inner = Mathf.Clamp01((dist - 0.80f) * 24f);
                    ringTex.SetPixel(x, y, new Color(1f, 1f, 1f, outer * inner));
                }
            }

            discTex.Apply();
            ringTex.Apply();

            _circleDiscSprite = Sprite.Create(discTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _circleRingSprite = Sprite.Create(ringTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
