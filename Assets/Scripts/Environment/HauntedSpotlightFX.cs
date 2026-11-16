using UnityEngine;
using ChiecLopCuoi.Gameplay;

namespace ChiecLopCuoi.Environment
{
    /// <summary>
    /// Hiệu ứng Rừng Đêm Ma Ám cho Level 3:
    /// Tạo lớp màn đêm mờ ảo kèm quầng sáng Spotlight bám theo nhân vật chính Cow Lốp
    /// và nhấp nháy đom đóm ma trơi xanh lục trong rừng sâu.
    /// </summary>
    public class HauntedSpotlightFX : MonoBehaviour
    {
        [Header("Spotlight Settings")]
        public Color nightOverlayColor = new Color(0.02f, 0.05f, 0.07f, 0.42f);
        public Color spotlightGlowColor = new Color(0.55f, 0.92f, 0.65f, 0.16f);
        public float spotlightRadiusScale = 9.5f;

        private Transform _playerTf;
        private Transform _glowTf;
        private SpriteRenderer _glowSr;

        private void Start()
        {
            var cowLop = FindFirstObjectByType<CowLopPlayerController>();
            if (cowLop != null) _playerTf = cowLop.transform;

            CreateSoftGlowAura();
        }

        private void CreateSoftGlowAura()
        {
            var existing = transform.Find("Haunted_Glow_Aura");
            if (existing != null)
            {
                _glowTf = existing;
                _glowSr = existing.GetComponent<SpriteRenderer>();
                return;
            }

            var glowGo = new GameObject("Haunted_Glow_Aura");
            glowGo.transform.SetParent(transform, false);
            _glowTf = glowGo.transform;
            _glowSr = glowGo.AddComponent<SpriteRenderer>();
            _glowSr.sortingLayerName = "Near_Layer";
            _glowSr.sortingOrder = 15;
            _glowSr.sprite = BuildRadialGlowSprite(64);
            _glowSr.color = spotlightGlowColor;
            _glowTf.localScale = new Vector3(spotlightRadiusScale, spotlightRadiusScale * 0.72f, 1f);
        }

        private void LateUpdate()
        {
            if (_playerTf == null)
            {
                var cowLop = FindFirstObjectByType<CowLopPlayerController>();
                if (cowLop != null) _playerTf = cowLop.transform;
            }

            if (_playerTf != null && _glowTf != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 2.8f) * 0.06f;
                _glowTf.position = _playerTf.position + new Vector3(0f, 1.35f, 0f);
                _glowTf.localScale = new Vector3(spotlightRadiusScale * pulse, spotlightRadiusScale * 0.72f * pulse, 1f);
            }
        }

        private static Sprite BuildRadialGlowSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float center = (size - 1) * 0.5f;
            float maxR = center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / maxR;
                    float dy = (y - center) / maxR;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
