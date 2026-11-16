using UnityEngine;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.Gameplay
{
    /// <summary>
    /// Vật phẩm rơi ra khi tiêu diệt Quái vật (Lốp xe, Thảo dược hồi máu, Tinh cầu Nộ).
    /// Có hiệu ứng văng vật lý xuống mặt đất và tự động hút về phía Cow Lốp khi lại gần.
    /// </summary>
    public class MonsterDropItem : MonoBehaviour
    {
        public enum DropType
        {
            TireScrap,
            HealthHerb,
            RageOrb
        }

        public DropType itemType = DropType.TireScrap;
        public int amount = 15;
        public float groundY = -3.45f;

        private Vector2 _velocity;
        private bool _landed;
        private float _spawnTime;
        private Transform _playerTransform;
        private CowLopPlayerController _playerController;

        private static Sprite _cachedTireSprite;
        private static Sprite _cachedHerbSprite;
        private static Sprite _cachedOrbSprite;

        public static void SpawnDropsForMonster(Vector3 position, float groundY)
        {
            // 1. Luôn rơi Lốp xe thưởng (+12 đến +20 Lốp, +30% ở chế độ Khó)
            float hardMult = ChiecLopCuoi.Database.GameRepository.GetHardTireDropMultiplier();
            int tireReward = Mathf.RoundToInt(Random.Range(12, 21) * hardMult);
            CreateSingleDrop(position + new Vector3(-0.25f, 0.4f, 0f), DropType.TireScrap, tireReward, groundY, new Vector2(Random.Range(-1.8f, -0.5f), 4.2f));

            // 2. 55% cơ hội rơi thêm Hoa / Thảo Dược Hồi Máu (+22 HP)
            if (Random.value <= 0.55f)
            {
                CreateSingleDrop(position + new Vector3(0.25f, 0.45f, 0f), DropType.HealthHerb, 22, groundY, new Vector2(Random.Range(0.5f, 1.8f), 4.6f));
            }
        }

        private static void CreateSingleDrop(Vector3 pos, DropType type, int amount, float groundY, Vector2 initialVelocity)
        {
            GameObject go = new GameObject($"Drop_{type}");
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 14;
            sr.sprite = GetSpriteForType(type);

            if (type == DropType.TireScrap)
            {
                go.transform.localScale = new Vector3(0.32f, 0.32f, 1f);
                sr.color = new Color(1f, 0.94f, 0.72f, 1f);
            }
            else if (type == DropType.HealthHerb)
            {
                go.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
                sr.color = Color.white;
            }
            else
            {
                go.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
                sr.color = Color.white;
            }

            var drop = go.AddComponent<MonsterDropItem>();
            drop.itemType = type;
            drop.amount = amount;
            drop.groundY = groundY;
            drop._velocity = initialVelocity;
        }

        private static Sprite GetSpriteForType(DropType type)
        {
            if (_cachedTireSprite == null)
            {
                _cachedTireSprite = Resources.Load<Sprite>("Tires/tire_01");
            }
            if (_cachedHerbSprite == null)
            {
                _cachedHerbSprite = Resources.Load<Sprite>("UI/Drop_HealthHerb");
            }
            if (_cachedOrbSprite == null)
            {
                _cachedOrbSprite = Resources.Load<Sprite>("UI/Drop_RageOrb");
            }
            Sprite chosen = type == DropType.TireScrap ? _cachedTireSprite :
                            type == DropType.HealthHerb ? _cachedHerbSprite : _cachedOrbSprite;
            return chosen != null ? chosen : MonsterOverheadHealthBar.GetWhiteSprite();
        }

        private void Start()
        {
            _spawnTime = Time.time;
            _playerController = FindFirstObjectByType<CowLopPlayerController>();
            if (_playerController != null)
            {
                _playerTransform = _playerController.transform;
            }
        }

        private void Update()
        {
            float age = Time.time - _spawnTime;
            if (age > 15f)
            {
                Destroy(gameObject);
                return;
            }

            // Vật lý nảy khi mới rơi ra
            if (!_landed)
            {
                _velocity.y -= 12.5f * Time.deltaTime;
                transform.position += (Vector3)(_velocity * Time.deltaTime);
                if (transform.position.y <= groundY)
                {
                    Vector3 p = transform.position;
                    p.y = groundY;
                    transform.position = p;
                    _landed = true;
                }
            }
            else
            {
                // Nhấp nhô nhẹ trên mặt đất
                float bob = Mathf.Sin((Time.time - _spawnTime) * 5f) * 0.06f;
                Vector3 p = transform.position;
                p.y = groundY + Mathf.Max(0f, bob);
                transform.position = p;
            }

            if (_playerTransform == null || age < 0.3f) return;

            float dist = Vector2.Distance(transform.position, _playerTransform.position + Vector3.up * 0.5f);

            // Tự động hút về phía người chơi khi ở trong bán kính 4.2m
            if (dist < 4.2f)
            {
                Vector3 targetPos = _playerTransform.position + Vector3.up * 0.65f;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, 9.5f * Time.deltaTime);

                if (dist < 0.85f)
                {
                    Collect();
                }
            }
        }

        private void Collect()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPickup();
            }

            Vector3 textPos = (_playerTransform != null ? _playerTransform.position : transform.position) + Vector3.up * 1.4f;

            switch (itemType)
            {
                case DropType.TireScrap:
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.AddTires(amount);
                    }
                    CombatFloatingText.Spawn(textPos, $"+{amount} LỐP", new Color(1f, 0.88f, 0.25f, 1f), 4.0f);
                    break;

                case DropType.HealthHerb:
                    if (BattleManager.Instance != null)
                    {
                        BattleManager.Instance.HealPlayer1(amount);
                    }
                    else if (_playerController != null)
                    {
                        _playerController.currentHealth = Mathf.Min(_playerController.maxHealth, _playerController.currentHealth + amount);
                    }
                    CombatFloatingText.Spawn(textPos + Vector3.up * 0.25f, $"+{amount} HP", new Color(0.3f, 1f, 0.45f, 1f), 4.2f);
                    break;

                case DropType.RageOrb:
                    if (BattleManager.Instance != null)
                    {
                        BattleManager.Instance.AddRevenge(true, amount);
                    }
                    CombatFloatingText.Spawn(textPos, $"+{amount}% NỘ", new Color(1f, 0.5f, 0.2f, 1f), 4.0f);
                    break;
            }

            Destroy(gameObject);
        }
    }
}
