using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChiecLopCuoi.Environment
{
    /// <summary>
    /// Hiệu ứng các chiếc lốp xe trôi nhẹ từ Trái -> Phải trên nền trời ở Login và Màn 1 (SampleScene).
    /// Tận dụng các sprite lốp đã tải (Tires/tire_01..05 và UI/Pink_Tire), tự xoay vòng mượt mà phía sau núi và nhân vật.
    /// </summary>
    public class DriftingSkyTiresFX : MonoBehaviour
    {
        private struct DriftingTireEntry
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public float speedX;
            public float spinSpeedDeg;
            public float baseY;
            public float bobAmplitude;
            public float bobFrequency;
            public float phase;
            public float minX;
            public float maxX;
        }

        private static readonly string[] TireResourcePaths =
        {
            "Tires/tire_01",
            "Tires/tire_02",
            "Tires/tire_03",
            "Tires/tire_04",
            "Tires/tire_05",
            "UI/Pink_Tire"
        };

        private static bool _hooked;

        private readonly List<DriftingTireEntry> _entries = new List<DriftingTireEntry>();

        public int TireCount => _entries.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            if (_hooked) return;
            _hooked = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnInitialScene()
        {
            TryEnsureForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryEnsureForScene(scene);
        }

        public static DriftingSkyTiresFX TryEnsureForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;

            string sceneName = scene.name;
            if (!IsSupportedScene(sceneName)) return null;

            var existing = FindFirstObjectByType<DriftingSkyTiresFX>();
            if (existing != null)
            {
                existing.InitializeForScene(sceneName);
                return existing;
            }

            var go = new GameObject("Sky_Drifting_Tires");
            SceneManager.MoveGameObjectToScene(go, scene);
            var fx = go.AddComponent<DriftingSkyTiresFX>();
            fx.InitializeForScene(sceneName);
            return fx;
        }

        private static bool IsSupportedScene(string sceneName)
            => sceneName == "LoginScene" || sceneName == "LevelSelectScene" || sceneName == "SampleScene";

        public float GetTireX(int index)
        {
            if (index < 0 || index >= _entries.Count || _entries[index].transform == null) return 0f;
            return _entries[index].transform.position.x;
        }

        private void Awake()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (IsSupportedScene(sceneName))
            {
                InitializeForScene(sceneName);
            }
        }

        public void InitializeForScene(string sceneName)
        {
            if (_entries.Count > 0) return;

            var sprites = LoadAvailableTireSprites();
            if (sprites.Count == 0) return;

            if (sceneName == "LoginScene")
            {
                SpawnTires(
                    sprites,
                    count: 6,
                    minX: -12.5f,
                    maxX: 12.5f,
                    minY: 1.0f,
                    maxY: 4.3f,
                    zPos: 8.6f,
                    sortingOrder: -13,
                    minWorldSize: 0.90f,
                    maxWorldSize: 1.48f,
                    minSpeed: 0.45f,
                    maxSpeed: 0.95f);
            }
            else if (sceneName == "LevelSelectScene")
            {
                SpawnTires(
                    sprites,
                    count: 6,
                    minX: -12.5f,
                    maxX: 12.5f,
                    minY: 1.2f,
                    maxY: 4.4f,
                    zPos: 9.4f,
                    sortingOrder: -14,
                    minWorldSize: 0.90f,
                    maxWorldSize: 1.48f,
                    minSpeed: 0.45f,
                    maxSpeed: 0.95f);
            }
            else
            {
                SpawnTires(
                    sprites,
                    count: 10,
                    minX: -62f,
                    maxX: 96f,
                    minY: 1.8f,
                    maxY: 5.5f,
                    zPos: 22.0f,
                    sortingOrder: 0,
                    minWorldSize: 1.35f,
                    maxWorldSize: 2.35f,
                    minSpeed: 0.85f,
                    maxSpeed: 1.65f);
            }
        }

        private void SpawnTires(
            List<Sprite> sprites,
            int count,
            float minX,
            float maxX,
            float minY,
            float maxY,
            float zPos,
            int sortingOrder,
            float minWorldSize,
            float maxWorldSize,
            float minSpeed,
            float maxSpeed)
        {
            float span = maxX - minX;
            for (int i = 0; i < count; i++)
            {
                Sprite sp = sprites[i % sprites.Count];
                var child = new GameObject($"Sky_Tire_{i + 1}");
                child.transform.SetParent(transform, false);

                var sr = child.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = sortingOrder;

                float t01 = (i + 0.5f) / count;
                float startX = minX + t01 * span;
                float yFactor = Mathf.Abs(Mathf.Sin((i + 1) * 1.73f));
                float startY = Mathf.Lerp(minY, maxY, yFactor);
                child.transform.position = new Vector3(startX, startY, zPos);

                float worldHeight = sp.bounds.size.y > 0.01f ? sp.bounds.size.y : 1f;
                float desiredSize = Mathf.Lerp(minWorldSize, maxWorldSize, (i % 3) * 0.5f);
                float uniformScale = desiredSize / worldHeight;
                child.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);

                float alpha = Mathf.Lerp(0.46f, 0.72f, (i % 3) * 0.5f);
                sr.color = new Color(0.88f, 0.92f, 1f, alpha);

                _entries.Add(new DriftingTireEntry
                {
                    transform = child.transform,
                    renderer = sr,
                    speedX = Mathf.Lerp(minSpeed, maxSpeed, ((i * 37) % 10) / 9f),
                    spinSpeedDeg = -Mathf.Lerp(16f, 36f, ((i * 29) % 10) / 9f),
                    baseY = startY,
                    bobAmplitude = 0.14f + (i % 3) * 0.06f,
                    bobFrequency = 0.7f + (i % 4) * 0.22f,
                    phase = i * 1.15f,
                    minX = minX,
                    maxX = maxX
                });
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float time = Time.time;
            for (int i = 0; i < _entries.Count; i++)
            {
                DriftingTireEntry entry = _entries[i];
                if (entry.transform == null) continue;

                Vector3 pos = entry.transform.position;
                pos.x += entry.speedX * dt;
                if (pos.x > entry.maxX)
                {
                    pos.x = entry.minX;
                }
                pos.y = entry.baseY + Mathf.Sin(time * entry.bobFrequency + entry.phase) * entry.bobAmplitude;
                entry.transform.position = pos;

                entry.transform.Rotate(0f, 0f, entry.spinSpeedDeg * dt);
            }
        }

        private static List<Sprite> LoadAvailableTireSprites()
        {
            var result = new List<Sprite>();
            for (int i = 0; i < TireResourcePaths.Length; i++)
            {
                Sprite sp = Resources.Load<Sprite>(TireResourcePaths[i]);
                if (sp != null) result.Add(sp);
            }
            return result;
        }
    }
}
