using System.Collections.Generic;
using UnityEngine;

namespace ChiecLopCuoi.Environment
{
    /// <summary>
    /// Hiệu ứng đom đóm bay đêm lập lòe và lá rơi nhẹ nhàng dưới ánh trăng tại Level 2.
    /// Tự động sinh một nhóm đom đóm trôi nhẹ nhàng trong khung nhìn Camera.
    /// </summary>
    public class NightFirefliesFX : MonoBehaviour
    {
        [Header("Firefly Settings")]
        [Range(10, 50)] public int fireflyCount = 25;
        public Color fireflyColor = new Color(0.85f, 0.95f, 0.35f, 0.85f);
        public float minSpeed = 0.4f;
        public float maxSpeed = 1.2f;
        public float floatRadiusX = 14f;
        public float floatRadiusY = 4.5f;

        private class Firefly
        {
            public GameObject go;
            public SpriteRenderer renderer;
            public Vector3 basePos;
            public float speedX;
            public float speedY;
            public float timeOffset;
            public float twinkleFrequency;
        }

        private readonly List<Firefly> _fireflies = new List<Firefly>();
        private Camera _mainCam;
        private Sprite _circleSprite;

        void Start()
        {
            _mainCam = Camera.main;
            CreateCircleSprite();
            SpawnFireflies();
        }

        private void CreateCircleSprite()
        {
            int res = 16;
            Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float center = (res - 1) * 0.5f;
            float maxR = center;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(1f - (dist / maxR));
                    alpha = Mathf.Pow(alpha, 1.8f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 16);
        }

        private void SpawnFireflies()
        {
            Vector3 center = transform.position;
            for (int i = 0; i < fireflyCount; i++)
            {
                GameObject fGo = new GameObject($"Firefly_{i:00}");
                fGo.transform.SetParent(transform);

                SpriteRenderer sr = fGo.AddComponent<SpriteRenderer>();
                sr.sprite = _circleSprite;
                sr.color = fireflyColor;
                sr.sortingLayerName = "Near_Layer";
                sr.sortingOrder = 14; // Giữa mặt đất và tiền cảnh

                float scale = Random.Range(0.08f, 0.22f);
                fGo.transform.localScale = new Vector3(scale, scale, 1f);

                Vector3 startPos = center + new Vector3(
                    Random.Range(-floatRadiusX, floatRadiusX),
                    Random.Range(-floatRadiusY, floatRadiusY),
                    Random.Range(-0.5f, 0.5f)
                );
                fGo.transform.position = startPos;

                _fireflies.Add(new Firefly
                {
                    go = fGo,
                    renderer = sr,
                    basePos = startPos,
                    speedX = Random.Range(minSpeed, maxSpeed) * (Random.value > 0.5f ? 1f : -1f),
                    speedY = Random.Range(minSpeed * 0.5f, maxSpeed * 0.7f),
                    timeOffset = Random.Range(0f, 10f),
                    twinkleFrequency = Random.Range(2.0f, 4.5f)
                });
            }
        }

        void LateUpdate()
        {
            if (_mainCam == null)
            {
                _mainCam = Camera.main;
                if (_mainCam == null) return;
            }

            Vector3 camPos = _mainCam.transform.position;
            float t = Time.time;

            for (int i = 0; i < _fireflies.Count; i++)
            {
                Firefly f = _fireflies[i];
                if (f.go == null) continue;

                // Cập nhật vị trí uốn lượn hình sin nhẹ nhàng
                float waveX = Mathf.Sin(t * 0.8f + f.timeOffset) * 0.8f;
                float waveY = Mathf.Cos(t * 1.2f + f.timeOffset * 1.5f) * 0.5f;

                f.basePos.x += f.speedX * Time.deltaTime;
                f.basePos.y += Mathf.Sin(t * f.speedY + f.timeOffset) * 0.3f * Time.deltaTime;

                // Vòng lặp quanh camera để đom đóm luôn hiện diện quanh nhân vật
                if (f.basePos.x > camPos.x + floatRadiusX) f.basePos.x = camPos.x - floatRadiusX;
                else if (f.basePos.x < camPos.x - floatRadiusX) f.basePos.x = camPos.x + floatRadiusX;

                if (f.basePos.y > camPos.y + floatRadiusY) f.basePos.y = camPos.y - floatRadiusY;
                else if (f.basePos.y < camPos.y - floatRadiusY) f.basePos.y = camPos.y + floatRadiusY;

                f.go.transform.position = new Vector3(f.basePos.x + waveX, f.basePos.y + waveY, f.basePos.z);

                // Độ lập lòe ánh sáng đom đóm
                float twinkle = 0.35f + 0.65f * (Mathf.Sin(t * f.twinkleFrequency + f.timeOffset) * 0.5f + 0.5f);
                Color c = fireflyColor;
                c.a = twinkle * fireflyColor.a;
                f.renderer.color = c;
            }
        }
    }
}
