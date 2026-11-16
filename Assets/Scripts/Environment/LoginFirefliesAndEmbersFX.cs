using System.Collections.Generic;
using UnityEngine;

namespace ChiecLopCuoi.Environment
{
    /// <summary>
    /// Hiệu ứng đom đóm vàng-xanh rơi chéo lập lòe và tàn lửa cam bay trong không gian 3D (Perspective)
    /// kết hợp màn tối nhẹ giúp nhân vật và tiêu đề trang Login nổi bật rõ khối.
    /// </summary>
    public class LoginFirefliesAndEmbersFX : MonoBehaviour
    {
        [Header("Particle Counts")]
        [SerializeField, Range(10, 60)] private int fireflyCount = 40;
        [SerializeField, Range(8, 40)] private int emberCount = 24;

        [Header("3D Volume Bounds")]
        [SerializeField] private Vector2 horizontalBounds = new Vector2(-10.5f, 10.5f);
        [SerializeField] private Vector2 verticalBounds = new Vector2(-5.5f, 5.5f);
        [SerializeField] private Vector2 depthBounds = new Vector2(-1.2f, 4.6f);

        private struct FloatingMote
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector3 pos;
            public float fallOrRiseSpeed;
            public float driftSpeedX;
            public float swayFreq;
            public float swayAmp;
            public float twinkleFreq;
            public float phase;
            public float baseScale;
            public Color baseColor;
            public bool isEmber;
        }

        private readonly List<FloatingMote> _motes = new List<FloatingMote>();
        private Sprite _softGlowSprite;
        private Material _additiveMat;

        private void Awake()
        {
            CreateSoftGlowSprite();
            CreateAdditiveMaterial();
        }

        private void Start()
        {
            SpawnParticles();
        }

        private void CreateSoftGlowSprite()
        {
            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                    float core = Mathf.Clamp01(1f - dist);
                    float alpha = Mathf.Pow(core, 2.2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            _softGlowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }

        private void CreateAdditiveMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _additiveMat = new Material(shader);
            }
        }

        private void SpawnParticles()
        {
            for (int i = 0; i < fireflyCount; i++) SpawnMote(isEmber: false, index: i);
            for (int i = 0; i < emberCount; i++) SpawnMote(isEmber: true, index: i);
        }

        private void SpawnMote(bool isEmber, int index)
        {
            var go = new GameObject(isEmber ? $"Ember_{index:00}" : $"Firefly_{index:00}");
            go.transform.SetParent(transform, false);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _softGlowSprite;
            if (_additiveMat != null) sr.sharedMaterial = _additiveMat;
            sr.sortingOrder = isEmber ? 22 : 24;

            Vector3 startPos;
            float baseScale;
            Color color;
            float speedY;

            if (isEmber)
            {
                startPos = new Vector3(
                    Random.Range(horizontalBounds.x * 0.85f, horizontalBounds.y * 0.35f),
                    Random.Range(verticalBounds.x, verticalBounds.y * 0.7f),
                    Random.Range(-0.8f, 2.4f)
                );
                baseScale = Random.Range(0.08f, 0.19f);
                color = Color.Lerp(new Color(1f, 0.52f, 0.12f, 0.9f), new Color(1f, 0.86f, 0.25f, 0.95f), Random.value);
                speedY = Random.Range(0.45f, 1.05f);
            }
            else
            {
                startPos = new Vector3(
                    Random.Range(horizontalBounds.x, horizontalBounds.y),
                    Random.Range(verticalBounds.x, verticalBounds.y),
                    Random.Range(depthBounds.x, depthBounds.y)
                );
                baseScale = Random.Range(0.11f, 0.28f);
                color = Color.Lerp(new Color(0.72f, 1f, 0.38f, 0.88f), new Color(1f, 0.92f, 0.42f, 0.92f), Random.value);
                speedY = -Random.Range(0.22f, 0.56f);
            }

            go.transform.localPosition = startPos;
            go.transform.localScale = new Vector3(baseScale, baseScale, 1f);
            sr.color = color;

            _motes.Add(new FloatingMote
            {
                transform = go.transform,
                renderer = sr,
                pos = startPos,
                fallOrRiseSpeed = speedY,
                driftSpeedX = Random.Range(-0.28f, 0.32f),
                swayFreq = Random.Range(0.9f, 2.1f),
                swayAmp = Random.Range(0.18f, 0.55f),
                twinkleFreq = Random.Range(1.8f, 4.4f),
                phase = Random.Range(0f, Mathf.PI * 2f),
                baseScale = baseScale,
                baseColor = color,
                isEmber = isEmber
            });
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float t = Time.unscaledTime;

            for (int i = 0; i < _motes.Count; i++)
            {
                FloatingMote m = _motes[i];
                if (m.transform == null) continue;

                m.pos.y += m.fallOrRiseSpeed * dt;
                m.pos.x += m.driftSpeedX * dt;

                if (m.isEmber && m.pos.y > verticalBounds.y + 0.4f)
                {
                    m.pos.y = verticalBounds.x - 0.3f;
                    m.pos.x = Random.Range(horizontalBounds.x * 0.85f, horizontalBounds.y * 0.35f);
                }
                else if (!m.isEmber && m.pos.y < verticalBounds.x - 0.4f)
                {
                    m.pos.y = verticalBounds.y + 0.3f;
                    m.pos.x = Random.Range(horizontalBounds.x, horizontalBounds.y);
                }

                if (m.pos.x > horizontalBounds.y + 0.5f) m.pos.x = horizontalBounds.x - 0.4f;
                else if (m.pos.x < horizontalBounds.x - 0.5f) m.pos.x = horizontalBounds.y + 0.4f;

                float swayX = Mathf.Sin(t * m.swayFreq + m.phase) * m.swayAmp;
                float swayZ = Mathf.Cos(t * (m.swayFreq * 0.7f) + m.phase) * 0.16f;
                m.transform.localPosition = new Vector3(m.pos.x + swayX, m.pos.y, m.pos.z + swayZ);

                float pulse = 0.32f + 0.68f * (Mathf.Sin(t * m.twinkleFreq + m.phase) * 0.5f + 0.5f);
                Color c = m.baseColor;
                c.a = m.baseColor.a * pulse;
                m.renderer.color = c;

                float s = m.baseScale * (0.78f + 0.36f * pulse);
                m.transform.localScale = new Vector3(s, s, 1f);

                _motes[i] = m;
            }
        }
    }
}
