using System.Collections.Generic;
using UnityEngine;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.NPC;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.Gameplay
{
    /// <summary>
    /// Đạn chiếc lốp xoay tròn bay ra thực tế khi Cow Lốp sử dụng kỹ năng Ném Lốp (Phím K).
    /// Có khả năng gây sát thương cho Quái vật và Boss Nữ trên đường bay.
    /// </summary>
    public class TireProjectile : MonoBehaviour
    {
        public float speed = 11.5f;
        public float maxDistance = 7.5f;
        public float damage = 18f;
        public float hitRadius = 0.85f;
        public int maxTargetsToPierce = 2;
        public bool isRageEmpowered;

        private int _direction = 1;
        private Vector3 _startPos;
        private int _hitCount;
        private readonly HashSet<int> _hitInstanceIds = new HashSet<int>();
        private static Sprite _cachedTireSprite;

        public static void Spawn(Vector3 spawnPos, int direction, float damage, bool isRageEmpowered, Sprite fallbackSprite = null)
        {
            GameObject go = new GameObject("TireProjectile_Flying");
            go.transform.position = spawnPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 18;
            sr.sprite = GetTireSprite(fallbackSprite);
            sr.color = isRageEmpowered ? new Color(1f, 0.55f, 0.2f, 1f) : Color.white;

            float scale = isRageEmpowered ? 0.85f : 0.65f;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var proj = go.AddComponent<TireProjectile>();
            proj._direction = direction >= 0 ? 1 : -1;
            proj._startPos = spawnPos;
            proj.damage = damage;
            proj.isRageEmpowered = isRageEmpowered;
            if (isRageEmpowered)
            {
                proj.speed = 13.5f;
                proj.maxDistance = 9.0f;
                proj.maxTargetsToPierce = 3;
            }
        }

        private static Sprite GetTireSprite(Sprite fallback)
        {
#if UNITY_EDITOR
            if (_cachedTireSprite == null)
            {
                _cachedTireSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MenuPause/Sprites/Deco_Tire_Flat.png");
            }
#endif
            if (_cachedTireSprite != null) return _cachedTireSprite;
            return fallback != null ? fallback : MonsterOverheadHealthBar.GetWhiteSprite();
        }

        private void Update()
        {
            // Di chuyển theo hướng ném và xoay tròn chiếc lốp
            transform.position += Vector3.right * (_direction * speed * Time.deltaTime);
            transform.Rotate(0f, 0f, -_direction * 760f * Time.deltaTime);

            CheckHits();

            if (Vector3.Distance(_startPos, transform.position) >= maxDistance || _hitCount >= maxTargetsToPierce)
            {
                Destroy(gameObject);
            }
        }

        private void CheckHits()
        {
            // 1. Kiểm tra va chạm với toàn bộ Quái vật trên đường bay
            var monsters = FindObjectsByType<MonsterCombatController>(FindObjectsSortMode.None);
            foreach (var monster in monsters)
            {
                if (monster == null || monster.IsDead) continue;
                int id = monster.GetInstanceID();
                if (_hitInstanceIds.Contains(id)) continue;

                float dist = Vector2.Distance(transform.position, monster.transform.position + Vector3.up * 0.45f);
                if (dist <= hitRadius)
                {
                    _hitInstanceIds.Add(id);
                    _hitCount++;
                    monster.TakeDamage(damage, isRageEmpowered);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayHitImpact();
                    if (_hitCount >= maxTargetsToPierce) return;
                }
            }

            // 2. Kiểm tra va chạm với Boss Nữ (NPC_Female)
            var npc = FindFirstObjectByType<NpcCombatController>();
            if (npc != null && !npc.IsDead && npc.combatUnlocked)
            {
                int id = npc.GetInstanceID();
                if (!_hitInstanceIds.Contains(id))
                {
                    float dist = Vector2.Distance(transform.position, npc.transform.position + Vector3.up * 0.5f);
                    if (dist <= hitRadius)
                    {
                        _hitInstanceIds.Add(id);
                        _hitCount++;
                        npc.ReceiveAttackFromPlayer(damage);
                        if (BattleManager.Instance != null)
                        {
                            BattleManager.Instance.RegisterComboHit(true, damage);
                        }
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayHitImpact();
                    }
                }
            }
        }
    }
}
