using System.Collections.Generic;
using UnityEngine;

namespace ChiecLopCuoi.Environment
{
    /// <summary>
    /// Hiệu ứng không khí Tuyết rơi đa tầng chiều sâu & Đom đóm phát sáng bay lượn.
    /// Hỗ trợ cả Camera 2D Orthographic (Level 1) và Camera 3D Perspective (Login, Chọn Level).
    /// </summary>
    public class AtmosphereSnowFirefliesFX : MonoBehaviour
    {
        [Header("Mode & Camera")]
        [SerializeField] private bool trackCameraXY = true;
        [SerializeField] private bool useUnscaledTime = false;

        [Header("Tuyết rơi (Falling Snow)")]
        [SerializeField] private bool enableSnow = true;
        [Range(0, 60)] [SerializeField] private int farSnowCount = 28;
        [Range(0, 60)] [SerializeField] private int midSnowCount = 24;
        [Range(0, 50)] [SerializeField] private int nearSnowCount = 18;
        [SerializeField] private Color snowColor = new Color(0.94f, 0.98f, 1f, 0.88f);
        [SerializeField] private float windDriftX = -0.42f;
        [SerializeField] private float snowScaleMultiplier = 1f;

        [Header("Đom đóm bay (Flying Fireflies)")]
        [SerializeField] private bool enableFireflies = true;
        [Range(0, 50)] [SerializeField] private int fireflyCount = 26;
        [SerializeField] private Color fireflyWarmColor = new Color(1f, 0.87f, 0.34f, 0.95f);
        [SerializeField] private Color fireflyLimeColor = new Color(0.75f, 1f, 0.40f, 0.92f);
        [SerializeField] private float fireflyScaleMultiplier = 1f;
        [Range(0.2f, 1f)] [SerializeField] private float fireflyTopViewportRatio = 0.78f;

        [Header("Sorting Layers & Orders (Far / Mid / Near)")]
        [SerializeField] private string farSortingLayer = "Mid_Layer";
        [SerializeField] private int farSortingOrder = 1;
        [SerializeField] private string midSortingLayer = "Near_Layer";
        [SerializeField] private int midSortingOrder = 6;
        [SerializeField] private string nearSortingLayer = "Near_Layer";
        [SerializeField] private int nearSortingOrder = 14;

        [Header("3D Depth Range (khi Camera Perspective)")]
        [SerializeField] private float farDepthZ = 4.0f;
        [SerializeField] private float midDepthZ = 1.4f;
        [SerializeField] private float nearDepthZ = -1.25f;

        private struct Snowflake
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector3 pos;
            public float fallSpeed;
            public float driftFactor;
            public float swayFreq;
            public float swayAmp;
            public float phase;
            public float baseAlpha;
        }

        private struct Firefly
        {
            public Transform transform;
            public SpriteRenderer coreRenderer;
            public SpriteRenderer haloRenderer;
            public Vector3 pos;
            public float velocityX;
            public float velocityY;
            public float swayFreqX;
            public float swayFreqY;
            public float swayAmp;
            public float twinkleFreq;
            public float phase;
            public float baseScale;
            public Color baseColor;
        }

        private readonly List<Snowflake> _snowflakes = new List<Snowflake>();
        private readonly List<Firefly> _fireflies = new List<Firefly>();

        private Camera _mainCam;
        private Texture2D _coreTex;
        private Texture2D _haloTex;
        private Sprite _coreSprite;
        private Sprite _haloSprite;
        private Material _spriteMat;

        public void ConfigurePreset(
            bool trackCamXY,
            bool unscaled,
            bool snow,
            int farSnow,
            int midSnow,
            int nearSnow,
            float snowScale,
            bool fireflies,
            int fireflyTotal,
            float fireflyScale,
            float fireflyTopRatio,
            string farLayer, int farOrder,
            string midLayer, int midOrder,
            string nearLayer, int nearOrder,
            float zFar, float zMid, float zNear)
        {
            trackCameraXY = trackCamXY;
            useUnscaledTime = unscaled;
            enableSnow = snow;
            farSnowCount = farSnow;
            midSnowCount = midSnow;
            nearSnowCount = nearSnow;
            snowScaleMultiplier = snowScale;
            enableFireflies = fireflies;
            fireflyCount = fireflyTotal;
            fireflyScaleMultiplier = fireflyScale;
            fireflyTopViewportRatio = fireflyTopRatio;
            farSortingLayer = farLayer;
            farSortingOrder = farOrder;
            midSortingLayer = midLayer;
            midSortingOrder = midOrder;
            nearSortingLayer = nearLayer;
            nearSortingOrder = nearOrder;
            farDepthZ = zFar;
            midDepthZ = zMid;
            nearDepthZ = zNear;
        }

        private void Awake()
        {
            _mainCam = Camera.main;
            CreateResources();
        }

        private void Start()
        {
            if (_mainCam == null) _mainCam = Camera.main;

            if (enableSnow)
            {
                SpawnSnowTier(farSnowCount, 0);
                SpawnSnowTier(midSnowCount, 1);
                SpawnSnowTier(nearSnowCount, 2);
            }

            if (enableFireflies && fireflyCount > 0)
            {
                SpawnFireflies();
            }
        }

        private void OnDestroy()
        {
            if (_coreSprite != null) Destroy(_coreSprite);
            if (_haloSprite != null) Destroy(_haloSprite);
            if (_coreTex != null) Destroy(_coreTex);
            if (_haloTex != null) Destroy(_haloTex);
            if (_spriteMat != null) Destroy(_spriteMat);
        }

        private void LateUpdate()
        {
            if (_mainCam == null)
            {
                _mainCam = Camera.main;
                if (_mainCam == null) return;
            }

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt <= 0f) return;
            dt = Mathf.Min(dt, 0.05f);

            float t = useUnscaledTime ? Time.unscaledTime : Time.time;
            Vector3 anchor = GetAnchorPosition();

            if (enableSnow) UpdateSnowflakes(dt, t, anchor);
            if (enableFireflies) UpdateFireflies(dt, t, anchor);
        }

        private void CreateResources()
        {
            _coreTex = BuildRadialTexture(32, falloffPower: 1.85f, brightCore: true);
            _haloTex = BuildRadialTexture(32, falloffPower: 2.55f, brightCore: false);

            _coreSprite = Sprite.Create(_coreTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            _haloSprite = Sprite.Create(_haloTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _spriteMat = new Material(shader);
            }
        }

        private static Texture2D BuildRadialTexture(int size, float falloffPower, bool brightCore)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normDist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float core = Mathf.Clamp01(1f - normDist);
                    float alpha = Mathf.Pow(core, falloffPower);
                    if (brightCore && core > 0.55f)
                    {
                        alpha = Mathf.Clamp01(alpha + (core - 0.55f) * 0.72f);
                    }
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private Vector3 GetAnchorPosition()
        {
            if (trackCameraXY && _mainCam != null)
            {
                return new Vector3(_mainCam.transform.position.x, _mainCam.transform.position.y, 0f);
            }
            return new Vector3(transform.position.x, transform.position.y, 0f);
        }

        private Vector2 GetHalfExtentsAtZ(float worldZ)
        {
            if (_mainCam == null) return new Vector2(14f, 6.5f);

            float aspect = Mathf.Max(1.6f, _mainCam.aspect);
            if (_mainCam.orthographic)
            {
                float orthoH = _mainCam.orthographicSize;
                return new Vector2(orthoH * aspect + 2.2f, orthoH + 1.3f);
            }

            float dist = Mathf.Max(1.5f, Mathf.Abs(worldZ - _mainCam.transform.position.z));
            float halfH = dist * Mathf.Tan(_mainCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfW = halfH * aspect;
            return new Vector2(halfW + 1.6f, halfH + 1.0f);
        }

        private void GetTierSettings(int tier, out string layerName, out int order, out float zPos)
        {
            bool is2D = _mainCam == null || _mainCam.orthographic;
            if (tier == 0)
            {
                layerName = farSortingLayer;
                order = farSortingOrder;
                zPos = is2D ? 0f : farDepthZ;
            }
            else if (tier == 1)
            {
                layerName = midSortingLayer;
                order = midSortingOrder;
                zPos = is2D ? 0f : midDepthZ;
            }
            else
            {
                layerName = nearSortingLayer;
                order = nearSortingOrder;
                zPos = is2D ? 0f : nearDepthZ;
            }
        }

        private void SpawnSnowTier(int count, int tier)
        {
            if (count <= 0) return;

            GetTierSettings(tier, out string layerName, out int order, out float zPos);
            Vector3 anchor = GetAnchorPosition();
            Vector2 bounds = GetHalfExtentsAtZ(zPos);

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Snow_T{tier}_{i:00}");
                go.transform.SetParent(transform, false);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _coreSprite;
                if (_spriteMat != null) sr.sharedMaterial = _spriteMat;
                sr.sortingLayerName = layerName;
                sr.sortingOrder = order;

                float scale;
                float alpha;
                float fallSpeed;
                float swayAmp;
                if (tier == 0)
                {
                    scale = Random.Range(0.06f, 0.11f);
                    alpha = Random.Range(0.30f, 0.48f);
                    fallSpeed = Random.Range(0.68f, 1.15f);
                    swayAmp = Random.Range(0.14f, 0.30f);
                }
                else if (tier == 1)
                {
                    scale = Random.Range(0.09f, 0.15f);
                    alpha = Random.Range(0.45f, 0.68f);
                    fallSpeed = Random.Range(1.05f, 1.65f);
                    swayAmp = Random.Range(0.22f, 0.44f);
                }
                else
                {
                    scale = Random.Range(0.13f, 0.20f);
                    alpha = Random.Range(0.56f, 0.80f);
                    fallSpeed = Random.Range(1.40f, 2.20f);
                    swayAmp = Random.Range(0.28f, 0.58f);
                }

                scale *= snowScaleMultiplier;
                go.transform.localScale = new Vector3(scale, scale, 1f);

                Color c = snowColor;
                c.a = alpha * snowColor.a;
                sr.color = c;

                Vector3 pos = new Vector3(
                    anchor.x + Random.Range(-bounds.x, bounds.x),
                    anchor.y + Random.Range(-bounds.y, bounds.y),
                    zPos
                );
                go.transform.position = pos;

                _snowflakes.Add(new Snowflake
                {
                    transform = go.transform,
                    renderer = sr,
                    pos = pos,
                    fallSpeed = fallSpeed,
                    driftFactor = 0.55f + tier * 0.35f + Random.Range(-0.12f, 0.18f),
                    swayFreq = Random.Range(0.9f, 2.1f),
                    swayAmp = swayAmp,
                    phase = Random.Range(0f, Mathf.PI * 2f),
                    baseAlpha = c.a
                });
            }
        }

        private void SpawnFireflies()
        {
            Vector3 anchor = GetAnchorPosition();
            for (int i = 0; i < fireflyCount; i++)
            {
                int tier = (i % 3 == 0) ? 1 : (i % 2 == 0 ? 2 : 0);
                // Trên Ortho (Level 1) đặt ở Mid/Near, trên 3D đặt ở lớp Mid & Near để nổi rõ
                if (tier == 0 && _mainCam != null && !_mainCam.orthographic) tier = 1;

                GetTierSettings(tier, out string layerName, out int baseOrder, out float zPos);
                if (_mainCam != null && _mainCam.orthographic)
                {
                    baseOrder = (i % 2 == 0) ? nearSortingOrder - 1 : midSortingOrder;
                    layerName = (i % 2 == 0) ? nearSortingLayer : midSortingLayer;
                }

                Vector2 bounds = GetHalfExtentsAtZ(zPos);
                float minY = -bounds.y * 0.92f;
                float maxY = bounds.y * fireflyTopViewportRatio;

                var go = new GameObject($"Firefly_{i:00}");
                go.transform.SetParent(transform, false);

                var coreSr = go.AddComponent<SpriteRenderer>();
                coreSr.sprite = _coreSprite;
                if (_spriteMat != null) coreSr.sharedMaterial = _spriteMat;
                coreSr.sortingLayerName = layerName;
                coreSr.sortingOrder = baseOrder;

                var haloGo = new GameObject("Halo");
                haloGo.transform.SetParent(go.transform, false);
                haloGo.transform.localPosition = Vector3.zero;
                haloGo.transform.localScale = new Vector3(2.9f, 2.9f, 1f);

                var haloSr = haloGo.AddComponent<SpriteRenderer>();
                haloSr.sprite = _haloSprite;
                if (_spriteMat != null) haloSr.sharedMaterial = _spriteMat;
                haloSr.sortingLayerName = layerName;
                haloSr.sortingOrder = baseOrder - 1;

                float scale = (tier == 2 ? Random.Range(0.11f, 0.17f) : Random.Range(0.08f, 0.13f)) * fireflyScaleMultiplier;
                go.transform.localScale = new Vector3(scale, scale, 1f);

                Color baseCol = Color.Lerp(fireflyLimeColor, fireflyWarmColor, Random.value);
                Vector3 pos = new Vector3(
                    anchor.x + Random.Range(-bounds.x, bounds.x),
                    anchor.y + Random.Range(minY, maxY),
                    zPos
                );
                go.transform.position = pos;

                float dirSign = Random.value > 0.5f ? 1f : -1f;
                _fireflies.Add(new Firefly
                {
                    transform = go.transform,
                    coreRenderer = coreSr,
                    haloRenderer = haloSr,
                    pos = pos,
                    velocityX = dirSign * Random.Range(0.24f, 0.68f),
                    velocityY = Random.Range(-0.14f, 0.22f),
                    swayFreqX = Random.Range(0.75f, 1.55f),
                    swayFreqY = Random.Range(1.05f, 2.05f),
                    swayAmp = Random.Range(0.32f, 0.85f),
                    twinkleFreq = Random.Range(1.7f, 3.6f),
                    phase = Random.Range(0f, Mathf.PI * 2f),
                    baseScale = scale,
                    baseColor = baseCol
                });
            }
        }

        private void UpdateSnowflakes(float dt, float t, Vector3 anchor)
        {
            for (int i = 0; i < _snowflakes.Count; i++)
            {
                Snowflake s = _snowflakes[i];
                if (s.transform == null) continue;

                Vector2 bounds = GetHalfExtentsAtZ(s.pos.z);
                s.pos.y -= s.fallSpeed * dt;
                s.pos.x += windDriftX * s.driftFactor * dt;

                s.pos = WrapPosition(s.pos, anchor, bounds.x, bounds.y, -bounds.y);
                _snowflakes[i] = s;

                float swayX = Mathf.Sin(t * s.swayFreq + s.phase) * s.swayAmp;
                s.transform.position = new Vector3(s.pos.x + swayX, s.pos.y, s.pos.z);

                if (s.renderer != null)
                {
                    float shimmer = 0.85f + 0.15f * Mathf.Sin(t * (s.swayFreq * 1.4f) + s.phase);
                    Color c = snowColor;
                    c.a = s.baseAlpha * shimmer;
                    s.renderer.color = c;
                }
            }
        }

        private void UpdateFireflies(float dt, float t, Vector3 anchor)
        {
            for (int i = 0; i < _fireflies.Count; i++)
            {
                Firefly f = _fireflies[i];
                if (f.transform == null) continue;

                Vector2 bounds = GetHalfExtentsAtZ(f.pos.z);
                float minY = -bounds.y * 0.92f;
                float maxY = bounds.y * fireflyTopViewportRatio;

                f.pos.x += f.velocityX * dt;
                f.pos.y += f.velocityY * dt;

                f.pos = WrapPosition(f.pos, anchor, bounds.x, maxY, minY);
                _fireflies[i] = f;

                float offsetX = Mathf.Sin(t * f.swayFreqX + f.phase) * f.swayAmp;
                float offsetY = Mathf.Cos(t * f.swayFreqY + f.phase * 1.3f) * (f.swayAmp * 0.58f);
                f.transform.position = new Vector3(f.pos.x + offsetX, f.pos.y + offsetY, f.pos.z);

                float pulse = 0.5f + 0.5f * Mathf.Sin(t * f.twinkleFreq + f.phase);
                float twinkle = 0.24f + 0.76f * Mathf.Pow(pulse, 1.45f);

                float scalePulse = f.baseScale * (0.86f + 0.34f * twinkle);
                f.transform.localScale = new Vector3(scalePulse, scalePulse, 1f);

                if (f.coreRenderer != null)
                {
                    Color coreCol = f.baseColor;
                    coreCol.a = f.baseColor.a * twinkle;
                    f.coreRenderer.color = coreCol;
                }
                if (f.haloRenderer != null)
                {
                    Color haloCol = f.baseColor;
                    haloCol.a = f.baseColor.a * 0.36f * twinkle;
                    f.haloRenderer.color = haloCol;
                }
            }
        }

        private static Vector3 WrapPosition(Vector3 pos, Vector3 anchor, float halfW, float topOffset, float bottomOffset)
        {
            float left = anchor.x - halfW;
            float right = anchor.x + halfW;
            float bottom = anchor.y + bottomOffset;
            float top = anchor.y + topOffset;
            float width = right - left;
            float height = top - bottom;

            if (pos.x < left) pos.x += width;
            else if (pos.x > right) pos.x -= width;

            if (pos.y < bottom) pos.y += height;
            else if (pos.y > top) pos.y -= height;

            return pos;
        }
    }
}
