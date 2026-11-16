using UnityEngine;
using UnityEngine.EventSystems;
using ChiecLopCuoi.Gameplay;

namespace ChiecLopCuoi.Environment
{
    /// <summary>
    /// Điều khiển Camera 2D theo sát nhân vật Cow Lốp (hoặc kéo chuột ngang),
    /// đồng bộ giới hạn với độ dài Map 280m [-60, +220] và Chốt chặn (StageZoneGateManager).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class DragCamera2D : MonoBehaviour
    {
        [Header("Follow Target (Nhân vật chính)")]
        public Transform followTarget;
        public float followOffsetX = 1.6f;

        [Header("Tốc độ & Độ mượt")]
        [Range(0.5f, 5f)] public float dragSpeed = 1.2f;
        [Range(0.01f, 0.5f)] public float smoothTime = 0.14f;

        [Header("Giới hạn bản đồ (Tự động hoặc Thủ công)")]
        public SpriteRenderer groundRenderer;
        public float mapMinX = -60f;
        public float mapMaxX = 220f;
        public bool lockY = true;

        private Camera _cam;
        private Vector3 _dragOriginWorld;
        private bool _isDragging;
        private float _targetX;
        private float _velocityX;
        private float _fixedY;
        private float _fixedZ;
        private float _minCamX;
        private float _maxCamX;
        private Vector2 _shakeOffset;

        public void SetShakeOffset(Vector2 offset)
        {
            _shakeOffset = offset;
        }

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _fixedY = transform.position.y;
            _fixedZ = transform.position.z;
            _targetX = transform.position.x;

            // Tự động đồng bộ nếu scene còn lưu giá trị cũ nhỏ hơn 280m
            if (mapMinX > -55f) mapMinX = -60f;
            if (mapMaxX < 200f) mapMaxX = 220f;
        }

        private void Start()
        {
            if (followTarget == null)
            {
                var player = FindFirstObjectByType<CowLopPlayerController>();
                if (player != null) followTarget = player.transform;
            }
            UpdateCameraLimits();
            if (followTarget != null)
            {
                _targetX = Mathf.Clamp(followTarget.position.x + followOffsetX, _minCamX, _maxCamX);
                transform.position = new Vector3(_targetX, _fixedY, _fixedZ);
            }
        }

        public void UpdateCameraLimits()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            float halfHeight = _cam.orthographicSize;
            float halfWidth = halfHeight * _cam.aspect;

            float effectiveMaxX = mapMaxX;
            if (StageZoneGateManager.Instance != null)
            {
                // Cho phép camera nhìn thấy thêm một chút qua chốt chặn (+6m) để thấy cổng/chốt phía trước
                effectiveMaxX = Mathf.Min(mapMaxX, StageZoneGateManager.Instance.GetClampedMaxX(mapMaxX) + 6.0f);
            }

            _minCamX = mapMinX + halfWidth;
            _maxCamX = effectiveMaxX - halfWidth;

            if (_minCamX > _maxCamX)
            {
                float center = (mapMinX + effectiveMaxX) * 0.5f;
                _minCamX = _maxCamX = center;
            }

            _targetX = Mathf.Clamp(_targetX, _minCamX, _maxCamX);
        }

        private void LateUpdate()
        {
            UpdateCameraLimits();
            HandlePointerDrag();
            HandleKeyboardScroll();

            if (!_isDragging && followTarget != null)
            {
                _targetX = Mathf.Clamp(followTarget.position.x + followOffsetX, _minCamX, _maxCamX);
            }

            float newX = Mathf.SmoothDamp(transform.position.x, _targetX, ref _velocityX, smoothTime);
            newX = Mathf.Clamp(newX, _minCamX, _maxCamX);

            float baseY = lockY ? _fixedY : transform.position.y;
            transform.position = new Vector3(
                newX + _shakeOffset.x,
                baseY + _shakeOffset.y,
                _fixedZ
            );
        }

        private void HandlePointerDrag()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                _dragOriginWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
                _isDragging = true;
            }

            if (Input.GetMouseButton(0) && _isDragging)
            {
                Vector3 currentWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
                float deltaX = (_dragOriginWorld.x - currentWorld.x) * dragSpeed;
                _targetX = Mathf.Clamp(_targetX + deltaX, _minCamX, _maxCamX);
                _dragOriginWorld = currentWorld;
            }

            if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
            }
        }

        private void HandleKeyboardScroll()
        {
            if (followTarget != null) return;
            float horizontal = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(horizontal) > 0.01f)
            {
                float keySpeed = _cam.orthographicSize * 2.5f * dragSpeed;
                _targetX = Mathf.Clamp(_targetX + horizontal * keySpeed * Time.deltaTime, _minCamX, _maxCamX);
            }
        }
    }
}
