using UnityEngine;
using TMPro;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Điều phối sân khấu 3D (Perspective Camera) cho Trang Chọn Level (LevelSelectScene):
    /// - Nghiêng Camera 3D mượt theo vị trí chuột + nhịp trôi điện ảnh.
    /// - Tách sâu từng lớp: Phong cảnh Làng Quê (Z=+9.2) -> Sương mù thung lũng (Z=+5.5, Z=+3.8) -> CowLop & Nữ chính (Z=+2.2) -> Thẻ Level 3D WorldSpace (Z=-0.3).
    /// - Tạo chuyển động hít thở cho hai nhân vật và hiệu ứng nghiêng khối 3D cho cụm thẻ màn chơi 1-2-3.
    /// </summary>
    public class LevelSelect3DStageController : MonoBehaviour
    {
        [Header("Camera 3D Perspective")]
        [SerializeField] private Camera stageCamera;
        [SerializeField] private float cameraFov = 44f;
        [SerializeField] private Vector3 baseCameraPos = new Vector3(0f, 0f, -10f);
        [SerializeField] private float mouseParallaxOffset = 0.45f;
        [SerializeField] private float mouseTiltDegrees = 2.4f;

        [Header("Depth Landscape & Mist")]
        [SerializeField] private Transform backgroundLayer;
        [SerializeField] private Transform bgFarLayer;
        [SerializeField] private Transform fogFarMidLayer;
        [SerializeField] private Transform bgMidLayer;
        [SerializeField] private Transform fogMidLayer;
        [SerializeField] private Transform bgNearHills;
        [SerializeField] private Transform bgNearLeftBranch;
        [SerializeField] private Transform bgNearRightBamboo;
        [SerializeField] private Transform fogForeLayer;

        [Header("Side Characters (3D Cutouts)")]
        [SerializeField] private Transform cowLopCutout;
        [SerializeField] private Transform heroineCutout;

        [Header("3D World-Space Cards & Title")]
        [SerializeField] private Transform worldCardsRoot;
        [SerializeField] private Transform[] levelCardSlots;
        [SerializeField] private TMP_Text titleTmpText;

        private Vector3 _bgBasePos;
        private Vector3 _bgFarBasePos;
        private Vector3 _fogFarMidBasePos;
        private Vector3 _bgMidBasePos;
        private Vector3 _fogMidBasePos;
        private Vector3 _bgNearHillsBasePos;
        private Vector3 _bgNearLeftBasePos;
        private Vector3 _bgNearRightBasePos;
        private Vector3 _fogForeBasePos;
        private Vector3 _cowLopBasePos;
        private Vector3 _cowLopBaseScale = Vector3.one;
        private Vector3 _heroineBasePos;
        private Vector3 _heroineBaseScale = Vector3.one;
        private Vector3 _cardsRootBasePos;
        private Vector3[] _slotBasePositions;

        private float _curMouseX;
        private float _curMouseY;
        private bool _cachedBases;

        private void Start()
        {
            EnsureCameraPerspective();
            CacheBaseTransforms();
            if (titleTmpText != null)
            {
                ThemeFontManager.ApplyLoginFont(titleTmpText);
                titleTmpText.color = Color.white;
            }
        }

        private void EnsureCameraPerspective()
        {
            if (stageCamera == null) stageCamera = Camera.main;
            if (stageCamera == null) return;

            stageCamera.orthographic = false;
            stageCamera.fieldOfView = cameraFov;
            stageCamera.nearClipPlane = 0.3f;
            stageCamera.farClipPlane = 120f;
            stageCamera.transform.position = baseCameraPos;

            // Bảo đảm nền phủ kín mọi tỷ lệ màn hình (kể cả ultrawide 21:9) khi nghiêng camera 3D
            FitLayerToFrustum(backgroundLayer, 1.20f);
            FitLayerToFrustum(bgFarLayer, 1.20f);
            FitLayerToFrustum(bgMidLayer, 1.20f);
            FitLayerToFrustum(bgNearHills, 1.20f);
            FitLayerToFrustum(bgNearLeftBranch, 1.20f);
            FitLayerToFrustum(bgNearRightBamboo, 1.20f);
        }

        private void FitLayerToFrustum(Transform layer, float margin)
        {
            if (layer == null || stageCamera == null) return;
            var sr = layer.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;

            float dist = Mathf.Max(1f, layer.position.z - baseCameraPos.z);
            float frustumH = 2f * dist * Mathf.Tan(cameraFov * 0.5f * Mathf.Deg2Rad);
            float aspect = Mathf.Max(1.6f, stageCamera.aspect);
            float frustumW = frustumH * aspect;
            Vector2 spSize = sr.sprite.bounds.size;
            float needScaleX = (frustumW * margin) / Mathf.Max(0.01f, spSize.x);
            float needScaleY = (frustumH * margin) / Mathf.Max(0.01f, spSize.y);
            float s = Mathf.Max(needScaleX, needScaleY);
            layer.localScale = new Vector3(s, s, 1f);
        }

        private void CacheBaseTransforms()
        {
            if (_cachedBases) return;
            _cachedBases = true;

            if (backgroundLayer != null) _bgBasePos = backgroundLayer.localPosition;
            if (bgFarLayer != null) _bgFarBasePos = bgFarLayer.localPosition;
            if (fogFarMidLayer != null) _fogFarMidBasePos = fogFarMidLayer.localPosition;
            if (bgMidLayer != null) _bgMidBasePos = bgMidLayer.localPosition;
            if (fogMidLayer != null) _fogMidBasePos = fogMidLayer.localPosition;
            if (bgNearHills != null) _bgNearHillsBasePos = bgNearHills.localPosition;
            if (bgNearLeftBranch != null) _bgNearLeftBasePos = bgNearLeftBranch.localPosition;
            if (bgNearRightBamboo != null) _bgNearRightBasePos = bgNearRightBamboo.localPosition;
            if (fogForeLayer != null) _fogForeBasePos = fogForeLayer.localPosition;

            if (cowLopCutout != null)
            {
                _cowLopBasePos = cowLopCutout.localPosition;
                _cowLopBaseScale = cowLopCutout.localScale;
            }
            if (heroineCutout != null)
            {
                _heroineBasePos = heroineCutout.localPosition;
                _heroineBaseScale = heroineCutout.localScale;
            }
            if (worldCardsRoot != null)
            {
                _cardsRootBasePos = worldCardsRoot.localPosition;
            }
            if (levelCardSlots != null && levelCardSlots.Length > 0)
            {
                _slotBasePositions = new Vector3[levelCardSlots.Length];
                for (int i = 0; i < levelCardSlots.Length; i++)
                {
                    if (levelCardSlots[i] != null)
                    {
                        _slotBasePositions[i] = levelCardSlots[i].localPosition;
                    }
                }
            }
        }

        private void Update()
        {
            CacheBaseTransforms();
            float t = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            UpdateCameraParallax(t, dt);
            AnimateMist(t);
            AnimateCharacters(t);
            AnimateWorldCards3D(t);
        }

        private void UpdateCameraParallax(float t, float dt)
        {
            if (stageCamera == null) return;

            float normX = Screen.width > 0 ? Mathf.Clamp((Input.mousePosition.x / Screen.width) * 2f - 1f, -1f, 1f) : 0f;
            float normY = Screen.height > 0 ? Mathf.Clamp((Input.mousePosition.y / Screen.height) * 2f - 1f, -1f, 1f) : 0f;

            _curMouseX = Mathf.Lerp(_curMouseX, normX, 1f - Mathf.Exp(-5.5f * dt));
            _curMouseY = Mathf.Lerp(_curMouseY, normY, 1f - Mathf.Exp(-5.5f * dt));

            float driftX = Mathf.Sin(t * 0.42f) * 0.14f;
            float driftY = Mathf.Cos(t * 0.34f) * 0.09f;

            stageCamera.transform.position = new Vector3(
                baseCameraPos.x + _curMouseX * mouseParallaxOffset + driftX,
                baseCameraPos.y + _curMouseY * (mouseParallaxOffset * 0.6f) + driftY,
                baseCameraPos.z
            );

            stageCamera.transform.localRotation = Quaternion.Euler(
                -_curMouseY * (mouseTiltDegrees * 0.65f) + Mathf.Sin(t * 0.32f) * 0.4f,
                _curMouseX * mouseTiltDegrees + Mathf.Cos(t * 0.28f) * 0.55f,
                0f
            );

            if (backgroundLayer != null)
            {
                backgroundLayer.localPosition = new Vector3(
                    _bgBasePos.x - driftX * 0.2f,
                    _bgBasePos.y - driftY * 0.16f,
                    _bgBasePos.z
                );
            }

            if (bgFarLayer != null)
            {
                // Trời hừng đông & đỉnh núi xa gần như neo sâu để tách khối khỏi lớp giữa
                bgFarLayer.localPosition = new Vector3(
                    _bgFarBasePos.x + _curMouseX * 0.12f + driftX * 0.38f,
                    _bgFarBasePos.y + _curMouseY * 0.07f + driftY * 0.28f,
                    _bgFarBasePos.z
                );
            }

            if (bgMidLayer != null)
            {
                float tremorX = Mathf.Sin(t * 5.0f) * 0.008f + Mathf.Cos(t * 9.0f) * 0.003f;
                float tremorY = Mathf.Cos(t * 5.8f) * 0.006f;
                bgMidLayer.localPosition = new Vector3(
                    _bgMidBasePos.x - driftX * 0.14f + tremorX,
                    _bgMidBasePos.y - driftY * 0.10f + tremorY,
                    _bgMidBasePos.z
                );
            }

            if (bgNearHills != null)
            {
                float tremorX = Mathf.Sin(t * 5.4f + 0.6f) * 0.011f + Mathf.Cos(t * 9.7f) * 0.004f;
                float tremorY = Mathf.Cos(t * 6.3f + 0.4f) * 0.009f;
                bgNearHills.localPosition = new Vector3(
                    _bgNearHillsBasePos.x - _curMouseX * 0.10f - driftX * 0.20f + tremorX,
                    _bgNearHillsBasePos.y - _curMouseY * 0.07f - driftY * 0.14f + tremorY,
                    _bgNearHillsBasePos.z
                );
            }

            ApplyFoliageSway(bgNearLeftBranch, _bgNearLeftBasePos, driftX, driftY, t, 0f, 1.38f);
            ApplyFoliageSway(bgNearRightBamboo, _bgNearRightBasePos, driftX, driftY, t, 2.15f, 1.24f);
        }

        private void ApplyFoliageSway(
            Transform target, Vector3 basePos, float driftX, float driftY, float t, float phase, float speed)
        {
            if (target == null) return;
            float swayX = Mathf.Sin(t * speed + phase) * 0.052f + Mathf.Sin(t * 5.4f + phase) * 0.010f;
            float swayY = Mathf.Cos(t * (speed * 0.82f) + phase * 0.7f) * 0.022f + Mathf.Cos(t * 6.1f + phase) * 0.008f;
            float angleZ = Mathf.Sin(t * (speed * 0.94f) + phase) * 0.72f + Mathf.Cos(t * 4.9f + phase) * 0.14f;

            target.localPosition = new Vector3(
                basePos.x - _curMouseX * 0.14f - driftX * 0.25f + swayX,
                basePos.y - _curMouseY * 0.09f - driftY * 0.16f + swayY,
                basePos.z
            );
            target.localRotation = Quaternion.Euler(0f, 0f, angleZ);
        }

        private void AnimateMist(float t)
        {
            if (fogFarMidLayer != null)
            {
                fogFarMidLayer.localPosition = new Vector3(
                    _fogFarMidBasePos.x + Mathf.Sin(t * 0.18f + 1.1f) * 1.35f,
                    _fogFarMidBasePos.y + Mathf.Cos(t * 0.14f) * 0.10f,
                    _fogFarMidBasePos.z
                );
            }

            if (fogMidLayer != null)
            {
                fogMidLayer.localPosition = new Vector3(
                    _fogMidBasePos.x + Mathf.Sin(t * 0.24f) * 1.15f,
                    _fogMidBasePos.y + Mathf.Cos(t * 0.19f) * 0.12f,
                    _fogMidBasePos.z
                );
            }

            if (fogForeLayer != null)
            {
                fogForeLayer.localPosition = new Vector3(
                    _fogForeBasePos.x + Mathf.Cos(t * 0.31f) * 1.45f,
                    _fogForeBasePos.y + Mathf.Sin(t * 0.23f) * 0.14f,
                    _fogForeBasePos.z
                );
            }
        }

        private void AnimateCharacters(float t)
        {
            if (cowLopCutout != null)
            {
                float breath = Mathf.Sin(t * 2.0f);
                float sway = Mathf.Cos(t * 1.05f);
                cowLopCutout.localPosition = new Vector3(
                    _cowLopBasePos.x + sway * 0.035f,
                    _cowLopBasePos.y + breath * 0.055f,
                    _cowLopBasePos.z
                );
                cowLopCutout.localScale = new Vector3(
                    _cowLopBaseScale.x * (1f - breath * 0.01f),
                    _cowLopBaseScale.y * (1f + breath * 0.016f),
                    _cowLopBaseScale.z
                );
                cowLopCutout.localRotation = Quaternion.Euler(0f, -8f + sway * 2.2f, breath * 0.8f);
            }

            if (heroineCutout != null)
            {
                float breath = Mathf.Sin(t * 1.75f + 1.4f);
                float sway = Mathf.Cos(t * 0.92f + 0.8f);
                heroineCutout.localPosition = new Vector3(
                    _heroineBasePos.x + sway * 0.035f,
                    _heroineBasePos.y + breath * 0.05f,
                    _heroineBasePos.z
                );
                heroineCutout.localScale = new Vector3(
                    _heroineBaseScale.x * (1f - breath * 0.008f),
                    _heroineBaseScale.y * (1f + breath * 0.014f),
                    _heroineBaseScale.z
                );
                heroineCutout.localRotation = Quaternion.Euler(0f, 8f + sway * 2.2f, -breath * 0.75f);
            }
        }

        private void AnimateWorldCards3D(float t)
        {
            if (worldCardsRoot != null)
            {
                float floatY = Mathf.Sin(t * 1.35f) * 0.055f;
                worldCardsRoot.localPosition = new Vector3(
                    _cardsRootBasePos.x,
                    _cardsRootBasePos.y + floatY,
                    _cardsRootBasePos.z
                );
                worldCardsRoot.localRotation = Quaternion.Euler(
                    _curMouseY * 3.4f + Mathf.Cos(t * 1.1f) * 1.1f,
                    -_curMouseX * 4.2f + Mathf.Sin(t * 0.9f) * 1.6f,
                    0f
                );
            }

            if (levelCardSlots != null && _slotBasePositions != null)
            {
                for (int i = 0; i < levelCardSlots.Length && i < _slotBasePositions.Length; i++)
                {
                    Transform slot = levelCardSlots[i];
                    if (slot == null) continue;
                    float wave = Mathf.Sin(t * 2.1f + i * 1.05f);
                    slot.localPosition = new Vector3(
                        _slotBasePositions[i].x,
                        _slotBasePositions[i].y + wave * 2.6f,
                        _slotBasePositions[i].z - wave * 5.0f
                    );
                }
            }
        }
    }
}
