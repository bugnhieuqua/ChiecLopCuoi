using UnityEngine;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.Gameplay
{
    /// <summary>
    /// Vật thể tương tác trong Rừng Đêm Level 3 (Thùng gỗ cổ / Cột mốc đá dưới gốc cây):
    /// Khi Cow Lốp tấn công trúng sẽ nứt vỡ và rơi ra Lốp Xe (+15) hoặc Thảo Dược Hồi Máu (+25 HP).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BreakableForestProp : MonoBehaviour
    {
        public float maxHealth = 28f;
        public float currentHealth = 28f;
        public bool dropHealthHerb = false;
        public int bonusTires = 15;
        public float bonusHeal = 25f;

        public bool IsBroken { get; private set; }

        private SpriteRenderer _sr;
        private Vector3 _originPos;
        private float _shakeTimer;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            currentHealth = maxHealth;
            _originPos = transform.position;
        }

        private void Start()
        {
            _originPos = transform.position;
            EnsureSprite();
        }

        public void EnsureSprite()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null && _sr.sprite == null)
            {
                _sr.sprite = CreateWoodenBarrelSprite();
                _sr.sortingLayerName = "Near_Layer";
                _sr.sortingOrder = 6;
            }
        }

        private void Update()
        {
            if (_shakeTimer > 0f && !IsBroken)
            {
                _shakeTimer -= Time.deltaTime;
                float ox = Mathf.Sin(Time.time * 55f) * 0.08f;
                transform.position = _originPos + new Vector3(ox, 0f, 0f);
                if (_shakeTimer <= 0f)
                {
                    transform.position = _originPos;
                }
            }
        }

        public void TakeDamage(float dmg)
        {
            if (IsBroken || dmg <= 0f) return;

            currentHealth = Mathf.Max(0f, currentHealth - dmg);
            _shakeTimer = 0.18f;

            if (currentHealth <= 0f)
            {
                BreakOpen();
            }
        }

        private void BreakOpen()
        {
            IsBroken = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup();

            if (dropHealthHerb)
            {
                if (BattleManager.Instance != null)
                {
                    BattleManager.Instance.HealPlayer1(bonusHeal);
                }
                CombatFloatingText.Spawn(
                    transform.position + new Vector3(0f, 1.25f, 0f),
                    $"THÙNG VỠ: +{Mathf.RoundToInt(bonusHeal)} HP!",
                    new Color(0.35f, 1f, 0.52f, 1f),
                    4.0f,
                    0.95f
                );
            }
            else
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.AddTires(bonusTires);
                }
                CombatFloatingText.Spawn(
                    transform.position + new Vector3(0f, 1.25f, 0f),
                    $"THÙNG VỠ: +{bonusTires} LỐP XE!",
                    new Color(1f, 0.85f, 0.25f, 1f),
                    4.0f,
                    0.95f
                );
            }

            gameObject.SetActive(false);
        }

        public static Sprite CreateWoodenBarrelSprite()
        {
            int w = 36, h = 42;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color darkWood = new Color(0.24f, 0.16f, 0.10f, 1f);
            Color midWood = new Color(0.38f, 0.25f, 0.15f, 1f);
            Color lightWood = new Color(0.48f, 0.33f, 0.20f, 1f);
            Color ironBand = new Color(0.22f, 0.25f, 0.28f, 1f);
            Color ironHighlight = new Color(0.45f, 0.50f, 0.55f, 1f);

            for (int y = 0; y < h; y++)
            {
                float ny = (y - h * 0.5f) / (h * 0.5f);
                int halfWidth = Mathf.RoundToInt(15f * (1f - 0.18f * ny * ny));
                for (int x = 0; x < w; x++)
                {
                    int dx = Mathf.Abs(x - w / 2);
                    if (dx > halfWidth || y < 1 || y >= h - 1)
                    {
                        tex.SetPixel(x, y, clear);
                        continue;
                    }

                    bool isBorder = dx == halfWidth || y == 1 || y == h - 2;
                    bool isBand = (y >= 8 && y <= 11) || (y >= h - 12 && y <= h - 9);
                    if (isBorder)
                    {
                        tex.SetPixel(x, y, new Color(0.10f, 0.07f, 0.05f, 1f));
                    }
                    else if (isBand)
                    {
                        tex.SetPixel(x, y, dx < 6 ? ironHighlight : ironBand);
                    }
                    else
                    {
                        bool plankSeam = (x % 6 == 0);
                        if (plankSeam) tex.SetPixel(x, y, darkWood);
                        else tex.SetPixel(x, y, dx < 7 ? lightWood : midWood);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 42f);
        }
    }
}
