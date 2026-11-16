using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using ChiecLopCuoi.Gameplay;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Điều khiển cảm ứng dạng hình tròn (Horizontal Circular Controller / Joystick)
    /// Hỗ trợ kéo Knob sang trái/phải và đồng bộ thời gian thực với phím A/D (hoặc Mũi tên Trái/Phải).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class HorizontalCircularController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public static HorizontalCircularController Instance { get; private set; }

        [Header("UI References")]
        [Tooltip("Vòng tròn đáy (Base)")]
        [SerializeField] private RectTransform baseRect;

        [Tooltip("Con lăn / Nút tròn kéo ở giữa (Knob)")]
        [SerializeField] private RectTransform knobRect;

        [Header("Cấu hình di chuyển")]
        [Tooltip("Bán kính tối đa được phép kéo Knob (pixel)")]
        [Range(20f, 200f)]
        [SerializeField] private float movementRange = 80f;

        [Tooltip("Vùng chết (Deadzone) để tránh rung lắc nhẹ")]
        [Range(0f, 0.5f)]
        [SerializeField] private float deadZone = 0.1f;

        [Tooltip("Chỉ cho phép di chuyển ngang (Trái / Phải)")]
        [SerializeField] private bool horizontalOnly = true;

        [Tooltip("Tự động hồi về tâm khi buông tay")]
        [SerializeField] private bool autoCenter = true;

        [Header("Giá trị đầu ra (Output)")]
        [Range(-1f, 1f)]
        [SerializeField] private float horizontalInput = 0f;

        public float Horizontal => Mathf.Abs(horizontalInput) < deadZone ? 0f : horizontalInput;
        public bool IsMovingLeft => Horizontal < -0.1f;
        public bool IsMovingRight => Horizontal > 0.1f;
        public bool IsPointerDragging => _isPointerDragging;

        public event Action<float> OnHorizontalChanged;

        private Vector2 _defaultKnobPos;
        private Canvas _canvas;
        private Camera _uiCamera;
        private bool _isPointerDragging;
        private float _lastKeyboardDir;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            if (baseRect == null) baseRect = GetComponent<RectTransform>();
            if (knobRect == null)
            {
                var knobChild = transform.Find("Knob");
                if (knobChild != null) knobRect = knobChild.GetComponent<RectTransform>();
            }
            if (knobRect != null) _defaultKnobPos = knobRect.anchoredPosition;

            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiCamera = _canvas.worldCamera;
            }
        }

        private void OnDisable()
        {
            _isPointerDragging = false;
            _lastKeyboardDir = 0f;
            ResetInput();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static bool IsLeftInputHeld()
        {
            var kb = Keyboard.current;
            return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ||
                   (kb != null && (kb.aKey.isPressed || kb.leftArrowKey.isPressed));
        }

        public static bool IsRightInputHeld()
        {
            var kb = Keyboard.current;
            return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ||
                   (kb != null && (kb.dKey.isPressed || kb.rightArrowKey.isPressed));
        }

        private void Update()
        {
            if (_isPointerDragging) return;

            bool canReadKeyboard = Time.timeScale > 0.01f &&
                                   (BattleManager.Instance == null || BattleManager.Instance.State == BattleManager.BattleState.Playing);

            float kbDir = 0f;
            if (canReadKeyboard)
            {
                bool leftHeld = IsLeftInputHeld();
                bool rightHeld = IsRightInputHeld();
                kbDir = (rightHeld ? 1f : 0f) - (leftHeld ? 1f : 0f);
            }

            if (!Mathf.Approximately(kbDir, _lastKeyboardDir))
            {
                _lastKeyboardDir = kbDir;
                horizontalInput = kbDir;
                OnHorizontalChanged?.Invoke(Horizontal);
            }

            if (knobRect != null && autoCenter)
            {
                Vector2 targetPos = _defaultKnobPos + new Vector2(Mathf.Clamp(horizontalInput, -1f, 1f) * movementRange, 0f);
                knobRect.anchoredPosition = Vector2.Lerp(knobRect.anchoredPosition, targetPos, 1f - Mathf.Exp(-24f * Time.unscaledDeltaTime));
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPointerDragging = true;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (baseRect == null || knobRect == null) return;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, _uiCamera, out localPoint))
            {
                if (horizontalOnly)
                {
                    localPoint.y = 0f;
                }

                // Giới hạn trong bán kính di chuyển
                Vector2 clamped = Vector2.ClampMagnitude(localPoint, movementRange);
                knobRect.anchoredPosition = clamped;

                // Tính toán input chuẩn hóa (-1.0f đến 1.0f)
                horizontalInput = Mathf.Clamp(clamped.x / movementRange, -1f, 1f);
                OnHorizontalChanged?.Invoke(Horizontal);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPointerDragging = false;
            _lastKeyboardDir = float.NaN;
            if (autoCenter && knobRect != null)
            {
                knobRect.anchoredPosition = _defaultKnobPos;
                horizontalInput = 0f;
                OnHorizontalChanged?.Invoke(0f);
            }
        }

        /// <summary>
        /// Cho phép kích hoạt nút bấm Trái/Phải thông qua script hoặc Button UI
        /// </summary>
        public void SetDirection(float value)
        {
            horizontalInput = Mathf.Clamp(value, -1f, 1f);
            if (knobRect != null)
            {
                knobRect.anchoredPosition = new Vector2(horizontalInput * movementRange, 0f);
            }
            OnHorizontalChanged?.Invoke(Horizontal);
        }

        public void ResetInput()
        {
            horizontalInput = 0f;
            if (knobRect != null)
            {
                knobRect.anchoredPosition = _defaultKnobPos;
            }
            OnHorizontalChanged?.Invoke(0f);
        }
    }
}
