using UnityEngine;
using TMPro;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Điều phối sân khấu 3D (Perspective Camera) cho Trang Login:
    /// - Nghiêng Camera 3D mượt theo vị trí chuột + nhịp điện ảnh.
    /// - Tách sâu từng lớp: Nền đêm (Z=+8.2) -> Ánh trăng/Vignette (Z=+6.8) -> Dàn Yêu quái (Z=+2.1~+3.1) -> CowLop (Z=+1.2) -> Tiêu đề CHIẾC LỐP CUỐI (Z=-0.4).
    /// - Tạo chuyển động thở/rình rập/lửa bập bùng cho nhân vật, quái và quay khối 3D nhẹ cho tiêu đề.
    /// </summary>
    public class Login3DStageController : MonoBehaviour
    {
        [Header("Camera 3D Perspective")]
        [SerializeField] private Camera stageCamera;
        [SerializeField] private float cameraFov = 44f;
        [SerializeField] private Vector3 baseCameraPos = new Vector3(0f, 0f, -10f);
        [SerializeField] private float mouseParallaxOffset = 0.55f;
        [SerializeField] private float mouseTiltDegrees = 2.8f;

        [Header("Animated Stage Actors")]
        [SerializeField] private Transform backgroundLayer;
        [SerializeField] private Transform heroCowLop;
        [SerializeField] private SpriteRenderer heroFistGlow;
        [SerializeField] private Transform demonAssassin;
        [SerializeField] private Transform demonHorn;
        [SerializeField] private Transform demonWolf;
        [SerializeField] private Transform npcFemaleBoss;

        private Vector3 _npcBasePos;
        private Vector3 _npcBaseScale = Vector3.one;
        private bool _npcCached;
        [SerializeField] private Transform titleLogoGroup;
        [SerializeField] private TMP_Text titleTmpText;

        [Header("3-Layer Depth Parallax & Mist")]
        [SerializeField] private Transform bgFarLayer;
        [SerializeField] private Transform fogFarMidLayer;
        [SerializeField] private Transform bgMidLayer;
        [SerializeField] private Transform bgNearBoulder;
        [SerializeField] private Transform bgNearBambooLeft;
        [SerializeField] private Transform bgNearRight;

        [Header("Atmospheric Fog & Demon Eye Glints")]
        [SerializeField] private Transform fogMidLayer;
        [SerializeField] private Transform fogForeLayer;
        [SerializeField] private SpriteRenderer[] demonEyeGlints;
        [SerializeField] private RectTransform playButtonRect;

        private Vector3 _bgBasePos;
        private Vector3 _bgFarBasePos;
        private Vector3 _fogFarMidBasePos;
        private Vector3 _bgMidBasePos;
        private Vector3 _bgNearBoulderBasePos;
        private Vector3 _bgNearLeftBasePos;
        private Vector3 _bgNearRightBasePos;
        private Vector3 _heroBasePos;
        private Vector3 _heroBaseScale;
        private Vector3 _assassinBasePos;
        private Vector3 _hornBasePos;
        private Vector3 _wolfBasePos;
        private Vector3 _titleBasePos;
        private Vector3 _fogMidBasePos;
        private Vector3 _fogForeBasePos;
        private Vector3 _playBtnBaseScale = Vector3.one;

        private float _curMouseX;
        private float _curMouseY;
        private bool _cachedBases;

        private void Start()
        {
            EnsureCameraPerspective();
            CacheBaseTransforms();
            ApplyLoginTitleStyle();
        }

        private void EnsureCameraPerspective()
        {
            if (stageCamera == null) stageCamera = Camera.main;
            if (stageCamera == null) return;

            stageCamera.orthographic = false;
            stageCamera.fieldOfView = cameraFov;
            stageCamera.nearClipPlane = 0.3f;
            stageCamera.farClipPlane = 100f;
            stageCamera.transform.position = baseCameraPos;

            FitLayerToFrustum(backgroundLayer, 1.20f);
            FitLayerToFrustum(bgFarLayer, 1.20f);
            FitLayerToFrustum(bgMidLayer, 1.20f);
            FitLayerToFrustum(bgNearBoulder, 1.20f);
            FitLayerToFrustum(bgNearBambooLeft, 1.20f);
            FitLayerToFrustum(bgNearRight, 1.20f);
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
            if (bgNearBoulder != null) _bgNearBoulderBasePos = bgNearBoulder.localPosition;
            if (bgNearBambooLeft != null) _bgNearLeftBasePos = bgNearBambooLeft.localPosition;
            if (bgNearRight != null) _bgNearRightBasePos = bgNearRight.localPosition;
            if (heroCowLop != null)
            {
                _heroBasePos = heroCowLop.localPosition;
                _heroBaseScale = heroCowLop.localScale;
            }
            if (demonAssassin != null) _assassinBasePos = demonAssassin.localPosition;
            if (demonHorn != null) _hornBasePos = demonHorn.localPosition;
            if (demonWolf != null) _wolfBasePos = demonWolf.localPosition;
            if (titleLogoGroup != null) _titleBasePos = titleLogoGroup.localPosition;
            if (fogMidLayer != null) _fogMidBasePos = fogMidLayer.localPosition;
            if (fogForeLayer != null) _fogForeBasePos = fogForeLayer.localPosition;
            if (playButtonRect != null) _playBtnBaseScale = playButtonRect.localScale;
        }

        private void ApplyLoginTitleStyle()
        {
            if (titleTmpText == null) return;
            ThemeFontManager.ApplyLoginFont(titleTmpText);
        }

        private void Update()
        {
            CacheBaseTransforms();
            float t = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            UpdateCameraParallax(t, dt);
            AnimateHero(t);
            AnimateDemons(t);
            AnimateNpcBoss(t);
            AnimateTitle3D(t);
            AnimateAtmosphereAndEyes(t);
        }

        private void UpdateCameraParallax(float t, float dt)
        {
            if (stageCamera == null) return;

            float normX = Screen.width > 0 ? Mathf.Clamp((Input.mousePosition.x / Screen.width) * 2f - 1f, -1f, 1f) : 0f;
            float normY = Screen.height > 0 ? Mathf.Clamp((Input.mousePosition.y / Screen.height) * 2f - 1f, -1f, 1f) : 0f;

            _curMouseX = Mathf.Lerp(_curMouseX, normX, 1f - Mathf.Exp(-5.5f * dt));
            _curMouseY = Mathf.Lerp(_curMouseY, normY, 1f - Mathf.Exp(-5.5f * dt));

            float driftX = Mathf.Sin(t * 0.45f) * 0.16f;
            float driftY = Mathf.Cos(t * 0.38f) * 0.10f;

            stageCamera.transform.position = new Vector3(
                baseCameraPos.x + _curMouseX * mouseParallaxOffset + driftX,
                baseCameraPos.y + _curMouseY * (mouseParallaxOffset * 0.6f) + driftY,
                baseCameraPos.z
            );

            // Đổ góc nhìn 3D nhẹ hướng vào trung tâm sân khấu
            stageCamera.transform.localRotation = Quaternion.Euler(
                -_curMouseY * (mouseTiltDegrees * 0.65f) + Mathf.Sin(t * 0.35f) * 0.45f,
                _curMouseX * mouseTiltDegrees + Mathf.Cos(t * 0.3f) * 0.65f,
                0f
            );

            if (backgroundLayer != null)
            {
                backgroundLayer.localPosition = new Vector3(
                    _bgBasePos.x - driftX * 0.25f,
                    _bgBasePos.y - driftY * 0.20f,
                    _bgBasePos.z
                );
            }

            if (bgFarLayer != null)
            {
                // Lớp xa (trăng & trời đêm) trôi rất ít so với camera để tạo chiều sâu vô tận
                bgFarLayer.localPosition = new Vector3(
                    _bgFarBasePos.x + _curMouseX * 0.14f + driftX * 0.42f,
                    _bgFarBasePos.y + _curMouseY * 0.08f + driftY * 0.32f,
                    _bgFarBasePos.z
                );
            }

            if (bgMidLayer != null)
            {
                // Lớp giữa (làng cổ & cổng làng): rung nhẹ êm dịu + dịch chiều sâu trung bình
                float tremorX = Mathf.Sin(t * 5.2f) * 0.010f + Mathf.Cos(t * 9.4f) * 0.004f;
                float tremorY = Mathf.Cos(t * 6.1f) * 0.008f;
                bgMidLayer.localPosition = new Vector3(
                    _bgMidBasePos.x - driftX * 0.14f + tremorX,
                    _bgMidBasePos.y - driftY * 0.10f + tremorY,
                    _bgMidBasePos.z
                );
            }

            if (bgNearBoulder != null)
            {
                // Tảng đá cận cảnh: rung nhẹ chắc nhịp hơn chút và phản hồi parallax mạnh
                float tremorX = Mathf.Sin(t * 5.6f + 0.7f) * 0.014f + Mathf.Cos(t * 10.1f) * 0.005f;
                float tremorY = Mathf.Cos(t * 6.6f + 0.4f) * 0.011f;
                bgNearBoulder.localPosition = new Vector3(
                    _bgNearBoulderBasePos.x - _curMouseX * 0.12f - driftX * 0.22f + tremorX,
                    _bgNearBoulderBasePos.y - _curMouseY * 0.08f - driftY * 0.16f + tremorY,
                    _bgNearBoulderBasePos.z
                );
            }

            if (bgNearBambooLeft != null)
            {
                // Khóm tre trái cận cảnh: đung đưa gió thổi lệch pha + rung nhẹ tán lá
                float swayX = Mathf.Sin(t * 1.45f) * 0.055f + Mathf.Sin(t * 5.8f + 1.1f) * 0.011f;
                float swayY = Mathf.Cos(t * 1.15f) * 0.025f + Mathf.Cos(t * 6.4f + 0.9f) * 0.009f;
                float angleZ = Mathf.Sin(t * 1.35f) * 0.72f + Mathf.Sin(t * 4.8f) * 0.15f;
                bgNearBambooLeft.localPosition = new Vector3(
                    _bgNearLeftBasePos.x - _curMouseX * 0.16f - driftX * 0.28f + swayX,
                    _bgNearLeftBasePos.y - _curMouseY * 0.10f - driftY * 0.18f + swayY,
                    _bgNearLeftBasePos.z
                );
                bgNearBambooLeft.localRotation = Quaternion.Euler(0f, 0f, angleZ);
            }

            if (bgNearRight != null)
            {
                // Cành cây & bụi tre phải cận cảnh: đung đưa gió lệch pha với bên trái
                float swayX = Mathf.Sin(t * 1.28f + 2.1f) * 0.055f + Mathf.Cos(t * 5.5f + 2.3f) * 0.011f;
                float swayY = Mathf.Cos(t * 1.08f + 1.7f) * 0.024f + Mathf.Sin(t * 6.2f + 1.4f) * 0.009f;
                float angleZ = Mathf.Cos(t * 1.22f + 1.9f) * 0.74f + Mathf.Cos(t * 5.1f + 0.8f) * 0.15f;
                bgNearRight.localPosition = new Vector3(
                    _bgNearRightBasePos.x - _curMouseX * 0.16f - driftX * 0.28f + swayX,
                    _bgNearRightBasePos.y - _curMouseY * 0.10f - driftY * 0.18f + swayY,
                    _bgNearRightBasePos.z
                );
                bgNearRight.localRotation = Quaternion.Euler(0f, 0f, angleZ);
            }
        }

        private void AnimateHero(float t)
        {
            if (heroCowLop == null) return;

            float breath = Mathf.Sin(t * 2.15f);
            float sway = Mathf.Cos(t * 1.15f);

            heroCowLop.localPosition = new Vector3(
                _heroBasePos.x + sway * 0.04f,
                _heroBasePos.y + breath * 0.055f,
                _heroBasePos.z
            );
            heroCowLop.localScale = new Vector3(
                _heroBaseScale.x * (1f - breath * 0.012f),
                _heroBaseScale.y * (1f + breath * 0.018f),
                _heroBaseScale.z
            );
            heroCowLop.localRotation = Quaternion.Euler(0f, -4f + sway * 1.8f, breath * 0.9f);

            if (heroFistGlow != null)
            {
                float firePulse = 0.55f + 0.45f * (Mathf.Sin(t * 9.5f) * 0.6f + Mathf.Cos(t * 14.3f) * 0.4f);
                Color c = heroFistGlow.color;
                c.a = Mathf.Clamp(firePulse, 0.35f, 1f);
                heroFistGlow.color = c;
            }
        }

        private void AnimateDemons(float t)
        {
            AnimateDemonActor(demonAssassin, _assassinBasePos, t, speed: 2.6f, ampY: 0.07f, tiltY: 7f, phase: 0.4f);
            AnimateDemonActor(demonHorn, _hornBasePos, t, speed: 1.75f, ampY: 0.09f, tiltY: 5f, phase: 1.9f);
            AnimateDemonActor(demonWolf, _wolfBasePos, t, speed: 3.1f, ampY: 0.06f, tiltY: 6f, phase: 3.4f);
        }

        private void AnimateNpcBoss(float t)
        {
            if (npcFemaleBoss == null) return;
            if (!_npcCached)
            {
                _npcBasePos = npcFemaleBoss.localPosition;
                _npcBaseScale = npcFemaleBoss.localScale;
                _npcCached = true;
            }

            float breath = Mathf.Sin(t * 1.95f + 1.1f);
            float sway = Mathf.Cos(t * 1.05f + 0.6f);

            npcFemaleBoss.localPosition = new Vector3(
                _npcBasePos.x - sway * 0.035f,
                _npcBasePos.y + breath * 0.05f,
                _npcBasePos.z
            );
            npcFemaleBoss.localScale = new Vector3(
                _npcBaseScale.x * (1f - breath * 0.01f),
                _npcBaseScale.y * (1f + breath * 0.016f),
                _npcBaseScale.z
            );
            npcFemaleBoss.localRotation = Quaternion.Euler(0f, 6f - sway * 2.0f, -breath * 0.8f);
        }

        private static void AnimateDemonActor(
            Transform actor, Vector3 basePos, float t, float speed, float ampY, float tiltY, float phase)
        {
            if (actor == null) return;

            float wave = Mathf.Sin(t * speed + phase);
            float side = Mathf.Cos(t * (speed * 0.6f) + phase);

            actor.localPosition = new Vector3(
                basePos.x + side * 0.055f,
                basePos.y + wave * ampY,
                basePos.z
            );
            actor.localRotation = Quaternion.Euler(wave * 1.6f, tiltY + side * 2.5f, -side * 1.2f);
        }

        private void AnimateTitle3D(float t)
        {
            if (titleLogoGroup == null) return;

            float bob = Mathf.Sin(t * 1.45f) * 0.075f;
            float yaw3D = Mathf.Sin(t * 0.95f) * 4.5f;
            float pitch3D = Mathf.Cos(t * 1.25f) * 2.2f;

            titleLogoGroup.localPosition = new Vector3(_titleBasePos.x, _titleBasePos.y + bob, _titleBasePos.z);
            titleLogoGroup.localRotation = Quaternion.Euler(pitch3D, yaw3D, 0f);

            if (titleTmpText != null)
            {
                // Ánh kim chuyển sắc vàng cam nhẹ trên từng chữ
                float glow = 0.88f + 0.12f * Mathf.Sin(t * 2.8f);
                titleTmpText.color = new Color(1f, 0.84f * glow + 0.06f, 0.24f, 1f);
            }
        }

        private void AnimateAtmosphereAndEyes(float t)
        {
            if (fogFarMidLayer != null)
            {
                fogFarMidLayer.localPosition = new Vector3(
                    _fogFarMidBasePos.x + Mathf.Sin(t * 0.19f + 0.9f) * 1.35f,
                    _fogFarMidBasePos.y + Mathf.Cos(t * 0.15f) * 0.10f,
                    _fogFarMidBasePos.z
                );
            }
            if (fogMidLayer != null)
            {
                fogMidLayer.localPosition = new Vector3(
                    _fogMidBasePos.x + Mathf.Sin(t * 0.28f) * 1.15f,
                    _fogMidBasePos.y + Mathf.Cos(t * 0.22f) * 0.14f,
                    _fogMidBasePos.z
                );
            }
            if (fogForeLayer != null)
            {
                fogForeLayer.localPosition = new Vector3(
                    _fogForeBasePos.x + Mathf.Cos(t * 0.36f) * 1.65f,
                    _fogForeBasePos.y + Mathf.Sin(t * 0.27f) * 0.16f,
                    _fogForeBasePos.z
                );
            }
            if (demonEyeGlints != null)
            {
                for (int i = 0; i < demonEyeGlints.Length; i++)
                {
                    SpriteRenderer eye = demonEyeGlints[i];
                    if (eye == null) continue;
                    float pulse = 0.48f + 0.52f * (0.5f + 0.5f * Mathf.Sin(t * 4.2f + i * 1.35f));
                    Color c = eye.color;
                    c.a = Mathf.Clamp(pulse, 0.35f, 1f);
                    eye.color = c;
                }
            }
            if (playButtonRect != null)
            {
                float s = 1f + 0.025f * Mathf.Sin(t * 2.4f);
                playButtonRect.localScale = new Vector3(_playBtnBaseScale.x * s, _playBtnBaseScale.y * s, _playBtnBaseScale.z);
            }
        }
    }
}
