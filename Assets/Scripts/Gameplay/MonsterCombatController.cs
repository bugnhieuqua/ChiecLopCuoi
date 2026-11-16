using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.Gameplay
{
    /// <summary>
    /// Điều khiển 4 loài Quỷ (Enemies: Quỷ Ghen, Quỷ Đau, Quỷ Tự Ti, Quỷ Giận):
    /// - Sử dụng Animator & AnimationClip tạo từ duy nhất enemy_demons_pack_spritesheet.png (không dùng ảnh cắt lẻ, xoá bỏ hoàn toàn Monsters cũ).
    /// - Đầy đủ các trạng thái Animator: Idle, Walk, Attack, Hurt.
    /// - Gắn thanh máu World-Space trên đầu, hiển thị số sát thương nổi, rơi vật phẩm khi chết.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class MonsterCombatController : MonoBehaviour
    {
        public enum MonsterType
        {
            DarkSwordsman,   // Legacy -> Tự động chuyển sang PainDemon
            DemonBeast,      // Legacy -> Tự động chuyển sang AngerDemon
            JealousyDemon,   // quyghen (Quỷ Ghen)
            AngerDemon,      // quygian (Quỷ Giận)
            PainDemon,       // quydau (Quỷ Đau)
            InsecurityDemon  // quytuti (Quỷ Tự Ti)
        }

        [Header("Monster Identity")]
        public MonsterType monsterType = MonsterType.JealousyDemon;
        public string monsterName = "Quỷ Ghen";

        [Header("Stats")]
        public float maxHealth = 95f;
        public float currentHealth = 95f;
        public float moveSpeed = 2.6f;
        public float attackRange = 1.9f;
        public float aggroRange = 13.5f;
        public float attackDamage = 9f;
        public float attackCooldown = 1.75f;
        public float groundY = -3.56f;

        [Header("Animation Sprites (From enemy_demons_pack_spritesheet.png + enemy_demons_walk.png)")]
        public Sprite[] idleWalkSprites;
        public Sprite[] walkSprites;
        public Sprite[] attackSprites;
        public float animFps = 10f;

        public bool IsDead { get; private set; }

        private SpriteRenderer _sr;
        private Animator _animator;
        private MemeDeathSound _memeSfx;
        private CowLopPlayerController _cowLop;
        private MonsterOverheadHealthBar _overheadBar;
        private float _animTimer;
        private int _animFrame;
        private bool _isAttacking;
        private float _lastAttackTime = -10f;
        private Vector3 _baseScale;
        private float _patrolOriginX;
        private int _patrolDir = -1;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            _memeSfx = GetComponent<MemeDeathSound>();
            if (_memeSfx == null)
            {
                _memeSfx = gameObject.AddComponent<MemeDeathSound>();
            }
            if (_animator != null)
            {
                _animator.enabled = true;
            }

            _baseScale = transform.localScale;
            if (Mathf.Abs(_baseScale.x) < 0.1f) _baseScale = new Vector3(2.5f, 2.5f, 1f);
            _patrolOriginX = transform.position.x;

            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
            groundY = activeScene.Contains("sample") ? -3.56f : -3.76f;

            SanitizeDemonTypeOnly();
            ApplyPresetStatsByType();
            ApplyDifficultyScaling();
#if UNITY_EDITOR
            LoadMonsterSpritesInEditor();
#endif
        }

        private void SanitizeDemonTypeOnly()
        {
            if (monsterType == MonsterType.DarkSwordsman)
            {
                monsterType = MonsterType.PainDemon;
            }
            else if (monsterType == MonsterType.DemonBeast)
            {
                monsterType = MonsterType.AngerDemon;
            }
        }

        private void Start()
        {
            currentHealth = maxHealth;
            _cowLop = FindFirstObjectByType<CowLopPlayerController>();

            Vector3 pos = transform.position;
            pos.y = groundY;
            transform.position = pos;

            float barHeight = monsterType == MonsterType.AngerDemon ? 3.60f : 3.45f;
            _overheadBar = MonsterOverheadHealthBar.AttachTo(transform, GetDisplayName(), barHeight);
            _overheadBar.SetHealth(currentHealth, maxHealth);
        }

        public void ApplyPresetStatsByType()
        {
            SanitizeDemonTypeOnly();
            animFps = 10f;
            attackCooldown = 1.75f;
            switch (monsterType)
            {
                case MonsterType.JealousyDemon:
                    monsterName = "Quỷ Ghen";
                    maxHealth = 90f;
                    attackDamage = 10f;
                    moveSpeed = 3.0f;
                    attackRange = 1.85f;
                    break;
                case MonsterType.AngerDemon:
                    monsterName = "Quỷ Giận";
                    maxHealth = 130f;
                    attackDamage = 14f;
                    moveSpeed = 2.6f;
                    attackRange = 2.05f;
                    break;
                case MonsterType.PainDemon:
                    monsterName = "Quỷ Đau";
                    maxHealth = 105f;
                    attackDamage = 11f;
                    moveSpeed = 2.75f;
                    attackRange = 1.95f;
                    break;
                case MonsterType.InsecurityDemon:
                default:
                    monsterName = "Quỷ Tự Ti";
                    maxHealth = 125f;
                    attackDamage = 12f;
                    moveSpeed = 2.3f;
                    attackRange = 2.0f;
                    break;
            }
            currentHealth = maxHealth;
        }

        private void ApplyDifficultyScaling()
        {
            if (!ChiecLopCuoi.Database.GameRepository.IsHardMode()) return;
            maxHealth = Mathf.Round(maxHealth * ChiecLopCuoi.Database.GameRepository.GetMonsterHpMultiplier());
            currentHealth = maxHealth;
            attackDamage *= ChiecLopCuoi.Database.GameRepository.GetMonsterDmgMultiplier();
            attackCooldown *= ChiecLopCuoi.Database.GameRepository.GetEnemyCooldownMultiplier();
            if (_overheadBar != null)
            {
                _overheadBar.SetHealth(currentHealth, maxHealth);
            }
        }

        public string GetDisplayName()
        {
            return !string.IsNullOrEmpty(monsterName) ? monsterName : monsterType.ToString();
        }

        public string GetDemonPrefix()
        {
            return monsterType switch
            {
                MonsterType.JealousyDemon => "quyghen",
                MonsterType.AngerDemon => "quygian",
                MonsterType.PainDemon => "quydau",
                MonsterType.InsecurityDemon => "quytuti",
                _ => "quyghen"
            };
        }

#if UNITY_EDITOR
        public void LoadMonsterSpritesInEditor()
        {
            SanitizeDemonTypeOnly();
            string prefix = GetDemonPrefix();

            var idleList = new List<Sprite>();
            var walkList = new List<Sprite>();
            var atkList = new List<Sprite>();

            Object[] all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Enemies/enemy_demons_pack_spritesheet.png");
            if (all != null && all.Length > 0)
            {
                var dict = new Dictionary<string, Sprite>();
                foreach (var obj in all)
                {
                    if (obj is Sprite sp) dict[sp.name] = sp;
                }

                for (int i = 0; i < 8; i++)
                {
                    if (dict.TryGetValue($"{prefix}_idle_{i}", out var sp)) idleList.Add(sp);
                }
                for (int i = 0; i < 10; i++)
                {
                    if (dict.TryGetValue($"{prefix}_attack_{i}", out var sp)) atkList.Add(sp);
                }
            }

            Object[] walkAll = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Enemies/enemy_demons_walk.png");
            if (walkAll != null && walkAll.Length > 0)
            {
                var wDict = new Dictionary<string, Sprite>();
                foreach (var obj in walkAll)
                {
                    if (obj is Sprite sp) wDict[sp.name] = sp;
                }

                for (int i = 0; i < 6; i++)
                {
                    if (wDict.TryGetValue($"{prefix}_walk_{i}", out var sp)) walkList.Add(sp);
                }
            }

            if (idleList.Count > 0) idleWalkSprites = idleList.ToArray();
            if (walkList.Count > 0) walkSprites = walkList.ToArray();
            if (atkList.Count > 0) attackSprites = atkList.ToArray();

            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null && idleWalkSprites != null && idleWalkSprites.Length > 0)
            {
                _sr.sprite = idleWalkSprites[0];
            }

            if (_animator == null) _animator = GetComponent<Animator>();
            if (_animator == null) _animator = gameObject.AddComponent<Animator>();
            string ctrlPath = $"Assets/Animations/Enemies/{prefix}_Animator.controller";
            var ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath);
            if (ctrl != null)
            {
                _animator.runtimeAnimatorController = ctrl;
                _animator.enabled = true;
            }
        }
#endif

        private void Update()
        {
            if (IsDead) return;

            if (BattleManager.Instance != null && BattleManager.Instance.State != BattleManager.BattleState.Playing)
            {
                SetAnimatorLocomotion(false);
                return;
            }

            if (_cowLop == null)
            {
                _cowLop = FindFirstObjectByType<CowLopPlayerController>();
                if (_cowLop == null) return;
            }

            float distToPlayer = _cowLop.transform.position.x - transform.position.x;
            float absDist = Mathf.Abs(distToPlayer);

            if (absDist > aggroRange)
            {
                PatrolAroundOrigin();
                SetAnimatorLocomotion(true);
                AnimateLoopFallback(walkSprites != null && walkSprites.Length > 0 ? walkSprites : idleWalkSprites);
                return;
            }

            FaceDirection(distToPlayer >= 0f ? 1 : -1);

            if (!_isAttacking)
            {
                if (absDist > attackRange)
                {
                    float dir = Mathf.Sign(distToPlayer);
                    Vector3 pos = transform.position;
                    pos.x += dir * moveSpeed * Time.deltaTime;
                    pos.y = groundY;
                    transform.position = pos;

                    SetAnimatorLocomotion(true);
                    AnimateLoopFallback(walkSprites != null && walkSprites.Length > 0 ? walkSprites : idleWalkSprites);
                }
                else
                {
                    if (Time.time - _lastAttackTime >= attackCooldown)
                    {
                        StartCoroutine(AttackRoutine());
                    }
                    else
                    {
                        SetAnimatorLocomotion(false);
                        AnimateLoopFallback(idleWalkSprites);
                    }
                }
            }
        }

        private float _lastAnimSyncX;
        private bool _hasAnimSyncX;

        private void SetAnimatorLocomotion(bool isMoving)
        {
            if (_animator != null && _animator.enabled && _animator.runtimeAnimatorController != null)
            {
                _animator.SetBool("IsMoving", isMoving);
                _animator.SetFloat("Speed", isMoving ? 1f : 0f);

                if (isMoving && !_isAttacking && Time.deltaTime > 0.0001f)
                {
                    float curX = transform.position.x;
                    float measuredSpeed = _hasAnimSyncX ? Mathf.Abs(curX - _lastAnimSyncX) / Time.deltaTime : moveSpeed;
                    _lastAnimSyncX = curX;
                    _hasAnimSyncX = true;

                    float refSpeed = 2.35f;
                    _animator.speed = measuredSpeed > 0.05f
                        ? Mathf.Clamp(measuredSpeed / refSpeed, 0.85f, 1.35f)
                        : 1f;
                }
                else
                {
                    _lastAnimSyncX = transform.position.x;
                    _hasAnimSyncX = true;
                    _animator.speed = 1f;
                }
            }
        }

        private void PatrolAroundOrigin()
        {
            Vector3 pos = transform.position;
            pos.x += _patrolDir * (moveSpeed * 0.45f) * Time.deltaTime;
            pos.y = groundY;

            if (pos.x > _patrolOriginX + 2.8f) _patrolDir = -1;
            else if (pos.x < _patrolOriginX - 2.8f) _patrolDir = 1;

            transform.position = pos;
            FaceDirection(_patrolDir);
        }

        private void FaceDirection(int dir)
        {
            float absX = Mathf.Abs(_baseScale.x);
            // Sprite gốc của 4 loài Quỷ (quyghen, quydau, quytuti, quygian) quay mặt sang PHẢI (+X)
            transform.localScale = new Vector3(dir >= 0 ? absX : -absX, _baseScale.y, _baseScale.z);
        }

        private void AnimateLoopFallback(Sprite[] sprites)
        {
            if (_animator != null && _animator.enabled && _animator.runtimeAnimatorController != null) return;
            if (sprites == null || sprites.Length == 0 || _sr == null) return;
            _animTimer += Time.deltaTime;
            if (_animTimer >= 1f / animFps)
            {
                _animTimer = 0f;
                _animFrame = (_animFrame + 1) % sprites.Length;
                _sr.sprite = sprites[_animFrame];
            }
        }

        private IEnumerator AttackRoutine()
        {
            _isAttacking = true;
            _lastAttackTime = Time.time;

            if (_animator != null && _animator.enabled && _animator.runtimeAnimatorController != null)
            {
                _animator.SetTrigger("Attack");
                _animator.Play("Attack", 0, 0f);
                int count = (attackSprites != null && attackSprites.Length > 0) ? attackSprites.Length : 6;
                float totalDur = Mathf.Max(count, 5) / 11f;
                yield return new WaitForSeconds(totalDur * 0.5f);
                if (!IsDead) DealDamageIfInRange();
                yield return new WaitForSeconds(totalDur * 0.5f);
            }
            else if (attackSprites != null && attackSprites.Length > 0)
            {
                for (int i = 0; i < attackSprites.Length; i++)
                {
                    if (IsDead) yield break;
                    _sr.sprite = attackSprites[i];

                    if (i == attackSprites.Length / 2)
                    {
                        DealDamageIfInRange();
                    }
                    yield return new WaitForSeconds(0.10f);
                }
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
                DealDamageIfInRange();
            }

            _isAttacking = false;
        }

        private void DealDamageIfInRange()
        {
            if (_cowLop == null || IsDead) return;
            if (BattleManager.Instance != null && BattleManager.Instance.State != BattleManager.BattleState.Playing) return;

            float dist = Mathf.Abs(_cowLop.transform.position.x - transform.position.x);
            if (dist <= attackRange + 0.5f)
            {
                float finalDmg = attackDamage;

                if (_cowLop.IsGrounded && _cowLop.IsBlocking)
                {
                    finalDmg = Mathf.Max(1f, Mathf.Round(attackDamage * 0.2f));
                    CombatFloatingText.Spawn(
                        _cowLop.transform.position + new Vector3(0f, 1.55f, 0f),
                        $"ĐỠ ĐÒN (-{Mathf.RoundToInt(finalDmg)})",
                        new Color(0.35f, 0.85f, 1f, 1f),
                        3.6f
                    );
                }

                if (BattleManager.Instance != null)
                {
                    BattleManager.Instance.DamagePlayer1(finalDmg);
                }
            }
        }

        public void TakeDamage(float dmg, bool isCritOrRage = false)
        {
            if (IsDead) return;
            currentHealth = Mathf.Max(0f, currentHealth - dmg);

            if (_overheadBar != null)
            {
                _overheadBar.SetHealth(currentHealth, maxHealth);
            }

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.RegisterComboHit(true, dmg);
            }

            if (_animator != null && _animator.enabled && _animator.runtimeAnimatorController != null && !_isAttacking)
            {
                _animator.speed = 1f;
                _animator.SetTrigger("Hurt");
            }

            Color textColor = isCritOrRage
                ? new Color(1f, 0.28f, 0.15f, 1f)
                : new Color(1f, 0.86f, 0.22f, 1f);
            string label = isCritOrRage ? $"-{Mathf.RoundToInt(dmg)} CRIT!" : $"-{Mathf.RoundToInt(dmg)}";
            CombatFloatingText.Spawn(transform.position + new Vector3(0f, 1.35f, 0f), label, textColor, isCritOrRage ? 4.7f : 4.0f);

            CombatFeelFX.FlashSprite(
                _sr,
                isCritOrRage ? new Color(1f, 0.72f, 0.28f, 1f) : new Color(1f, 0.35f, 0.35f, 1f),
                isCritOrRage ? 0.14f : 0.10f);
            if (isCritOrRage)
            {
                CombatFeelFX.ShakeCamera(0.16f, 0.14f);
            }

            if (currentHealth <= 0f)
            {
                StartCoroutine(DieRoutine());
            }
        }

        private IEnumerator DieRoutine()
        {
            IsDead = true;

            // Phát meme âm thanh ngẫu nhiên khi quái vật chết (MemeDeathSound.cs)
            if (_memeSfx != null)
            {
                _memeSfx.PlayDeathMeme();
            }

            // Phát animation Die (Animator): chìm dần + xoay ngã qua transform
            if (_animator != null && _animator.enabled && _animator.runtimeAnimatorController != null)
            {
                _animator.speed = 1f;
                _animator.SetTrigger("Die");
                _animator.Play("Die", 0, 0f);
            }

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.AddRevenge(true, 15f);
            }

            MonsterDropItem.SpawnDropsForMonster(transform.position, groundY);

            if (StageZoneGateManager.Instance != null)
            {
                StageZoneGateManager.Instance.NotifyMonsterDefeated(this);
            }

            if (_overheadBar != null)
            {
                Destroy(_overheadBar.gameObject);
            }

            float elapsed = 0f;
            while (elapsed < 0.45f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.45f);
                if (_sr != null)
                {
                    _sr.color = new Color(1f, 0.3f, 0.3f, 1f - t);
                }
                transform.position += Vector3.down * (0.45f * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, 0f, -90f * t);
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
