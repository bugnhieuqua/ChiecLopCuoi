using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChiecLopCuoi.Gameplay;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.NPC
{
    /// <summary>
    /// Bộ điều khiển Boss Nữ (NPC_Female) sử dụng Animator & AnimationClip từ duy nhất char_female_gift_bag_sheet.png:
    /// - Đứng vững trên mặt đất (groundY = -3.56 ở Level 1, -3.76 ở Level 2 & 3 với Pivot BottomCenter).
    /// - Đầy đủ các trạng thái Animator: Idle, Walk, Run, Jump, ThrowGift (Attack 1), ThrowBag (Attack 2), Block, Hurt.
    /// - Cơ chế Boss Phase 2 (Cuồng Nộ khi máu < 50%): tăng tốc độ, giảm Cooldown, tăng sát thương.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class NpcCombatController : MonoBehaviour
    {
        public enum NpcState
        {
            Idle,
            Walk,
            Run,
            Jump,
            ThrowGift,
            ThrowBag,
            Block,
            Hurt
        }

        [Header("Target Reference")]
        public Transform playerTarget;

        [Header("Movement & Spacing")]
        public float moveSpeed = 3.6f;
        public float preferredDistance = 2.15f;
        public float distanceTolerance = 0.45f;
        public float minStageX = -54.0f;
        public float maxStageX = 216.0f;
        public float arenaTriggerPlayerX = 48.0f;
        public bool faceRightByDefault = true;

        [Header("HUD Display")]
        public string bossDisplayName = string.Empty;

        [Header("Health & Combat Stats")]
        public float maxHealth = 180f;
        public float currentHealth = 180f;
        public float attackDamage = 14f;
        public float jumpAttackDamage = 20f;
        public bool IsDead { get; private set; }
        public bool IsPhase2Enraged { get; private set; }

        [Header("Combat AI Timing")]
        public bool combatUnlocked = false;
        public float attackCooldown = 1.0f;
        public float decisionInterval = 0.12f;
        [Range(0f, 1f)] public float attackChance = 0.65f;
        [Range(0f, 1f)] public float blockChance = 0.10f;
        [Range(0f, 1f)] public float jumpAttackChance = 0.25f;

        [Header("Jump Arc Settings")]
        public float jumpHeight = 2.0f;
        public float jumpDuration = 0.62f;

        /// <summary>Trả về tên Boss hiển thị trên HUD theo màn chơi hiện tại.</summary>
        public string GetDisplayBossName()
        {
            if (!string.IsNullOrEmpty(bossDisplayName))
            {
                return bossDisplayName;
            }

            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            switch (scene)
            {
                case "SampleScene": return "Bóng Hình Áo Đỏ";
                case "level2": return "Nỗi Buồn Phản Bội";
                case "level3": return "Vọng Ảnh Tự Ti";
                case "level4": return "Cơn Giận Đèn Lồng";
                case "level5": return "Linh Hồn Hệ Thống";
                case "level6": return "Nữ Áo Đỏ";
                default: return "Nữ Áo Đỏ";
            }
        }

        [Header("Sliced Sub-Sprites (From char_female_gift_bag_sheet.png)")]
        public Sprite[] idleSprites;
        public Sprite[] walkSprites;
        public Sprite[] runSprites;
        public Sprite[] jumpSprites;
        public Sprite[] throwGiftSprites;
        public Sprite[] throwBagSprites;
        public Sprite[] blockSprites;
        public Sprite[] hurtSprites;

        [Header("Animation FPS (Fallback)")]
        public float idleFps = 4f;
        public float walkFps = 10f;
        public float runFps = 12f;
        public float jumpFps = 12f;
        public float attackFps = 11f;

        private SpriteRenderer _sr;
        private Animator _animator;
        private NpcState _state = NpcState.Idle;
        private NpcState _lastAnimatorState = NpcState.Idle;
        private int _frameIndex;
        private float _frameTimer;
        private float _decisionTimer;
        private float _attackCooldownTimer;
        private bool _isBusyAction;
        public float groundY = -3.56f;
        private float _groundY;
        private float _baseScaleX = 1.85f;
        private float _baseScaleY = 1.85f;
        private bool _useThrowBagNext = false;
        private bool _isHardMode;
        private float _nextHurtStaggerTime;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            if (_animator != null)
            {
                _animator.enabled = true;
            }

            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
            if (activeScene.Contains("sample"))
            {
                groundY = -3.56f;
                minStageX = -52.0f;
                maxStageX = 80.0f;
                arenaTriggerPlayerX = 48.0f;
            }
            else
            {
                groundY = -3.76f;
                minStageX = -56.0f;
                maxStageX = 216.0f;
                arenaTriggerPlayerX = 175.0f;
            }
            _groundY = groundY;

            var pos = transform.position;
            pos.y = _groundY;
            transform.position = pos;

            float absX = Mathf.Abs(transform.localScale.x);
            float absY = Mathf.Abs(transform.localScale.y);
            _baseScaleX = (absX > 0.5f && absX < 2.2f) ? absX : 1.85f;
            _baseScaleY = (absY > 0.5f && absY < 2.2f) ? absY : 1.85f;
            transform.localScale = new Vector3(-_baseScaleX, _baseScaleY, 1f);

#if UNITY_EDITOR
            AutoLoadSpritesInEditor();
#endif
        }

        private void Start()
        {
            if (playerTarget == null)
            {
                var cowLop = FindFirstObjectByType<CowLopPlayerController>();
                if (cowLop != null) playerTarget = cowLop.transform;
            }
            ApplyDifficultyScaling();
        }

        private void ApplyDifficultyScaling()
        {
            _isHardMode = ChiecLopCuoi.Database.GameRepository.IsHardMode();
            if (!_isHardMode) return;
            attackDamage *= ChiecLopCuoi.Database.GameRepository.GetBossDmgMultiplier();
            jumpAttackDamage *= ChiecLopCuoi.Database.GameRepository.GetBossDmgMultiplier();
            moveSpeed *= 1.30f;
            attackCooldown = 0.36f;
            decisionInterval = 0.04f;
            attackChance = 0.70f;
            blockChance = 0.06f;
            jumpAttackChance = 0.24f;
            if (BattleManager.Instance == null)
            {
                maxHealth = Mathf.Round(maxHealth * ChiecLopCuoi.Database.GameRepository.GetBossHpMultiplier());
                currentHealth = maxHealth;
            }
        }

        private void Update()
        {
            if (IsDead) return;

            if (BattleManager.Instance != null && BattleManager.Instance.State != BattleManager.BattleState.Playing)
            {
                SetState(NpcState.Idle);
                SyncAnimatorAndFallback();
                return;
            }

            if (playerTarget != null && !_isBusyAction)
            {
                FaceTowardsPlayer();

                if (!combatUnlocked)
                {
                    HandlePreCombatAdvance();
                }
                else
                {
                    HandleCombatDecision();
                    if (!_isBusyAction)
                    {
                        HandleSpacing();
                    }
                }
            }

            if (_attackCooldownTimer > 0f)
            {
                _attackCooldownTimer -= Time.deltaTime;
            }

            SyncAnimatorAndFallback();
        }

        private void CheckAndTriggerPhase2()
        {
            if (IsPhase2Enraged || IsDead) return;
            if (currentHealth <= maxHealth * 0.5f)
            {
                IsPhase2Enraged = true;
                moveSpeed *= 1.32f;
                bool isHard = _isHardMode || ChiecLopCuoi.Database.GameRepository.IsHardMode();
                attackCooldown = isHard ? 0.28f : 0.72f;
                decisionInterval = isHard ? 0.03f : 0.08f;
                attackDamage *= 1.35f;
                jumpAttackDamage *= 1.35f;
                attackChance = isHard ? 0.72f : 0.65f;
                blockChance = isHard ? 0.05f : 0.08f;
                jumpAttackChance = isHard ? 0.23f : 0.27f;

                if (BattleManager.Instance != null)
                {
                    BattleManager.Instance.AddRevenge(false, 100f);
                }

                CombatFloatingText.Spawn(
                    transform.position + new Vector3(0f, 2.0f, 0f),
                    "BOSS NỮ CUỒNG NỘ — PHASE 2!",
                    new Color(1f, 0.22f, 0.35f, 1f),
                    4.8f,
                    1.4f
                );

                if (_sr != null)
                {
                    _sr.color = new Color(1f, 0.78f, 0.82f, 1f);
                }
            }
        }

        private void FaceTowardsPlayer()
        {
            if (playerTarget == null) return;
            float dx = playerTarget.position.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.05f) return;

            bool targetIsRight = dx > 0f;
            float sign = (targetIsRight == faceRightByDefault) ? 1f : -1f;
            transform.localScale = new Vector3(sign * _baseScaleX, _baseScaleY, 1f);
        }

        private void HandlePreCombatAdvance()
        {
            bool allZonesCleared = false;
            if (StageZoneGateManager.Instance != null)
            {
                allZonesCleared = StageZoneGateManager.Instance.AllZonesCleared;
            }
            else
            {
                var monsters = FindObjectsByType<MonsterCombatController>(FindObjectsSortMode.None);
                bool anyAlive = false;
                foreach (var m in monsters)
                {
                    if (m != null && !m.IsDead)
                    {
                        anyAlive = true;
                        break;
                    }
                }
                allZonesCleared = !anyAlive;
            }

            bool playerNearArena = playerTarget != null &&
                (playerTarget.position.x >= arenaTriggerPlayerX || Mathf.Abs(playerTarget.position.x - transform.position.x) <= 15f);

            if (!allZonesCleared && !playerNearArena)
            {
                SetState(NpcState.Idle);
                return;
            }

            float dx = playerTarget.position.x - transform.position.x;
            float dir = Mathf.Sign(dx);
            Vector3 newPos = transform.position;
            newPos.x += dir * (moveSpeed * 0.9f) * Time.deltaTime;
            newPos.x = Mathf.Clamp(newPos.x, minStageX, maxStageX);
            newPos.y = _groundY;
            transform.position = newPos;
            SetState(NpcState.Walk);
        }

        private void HandleSpacing()
        {
            float dx = playerTarget.position.x - transform.position.x;
            float absDist = Mathf.Abs(dx);

            Vector3 newPos = transform.position;

            if (absDist > preferredDistance + distanceTolerance)
            {
                float dir = Mathf.Sign(dx);
                bool shouldRun = IsPhase2Enraged || _isHardMode;
                float chaseMult = shouldRun ? 1.25f : 1.0f;
                newPos.x += dir * moveSpeed * chaseMult * Time.deltaTime;
                SetState(shouldRun ? NpcState.Run : NpcState.Walk);
            }
            else if (absDist < preferredDistance - distanceTolerance && _attackCooldownTimer > 0.25f && !_isHardMode)
            {
                float dir = -Mathf.Sign(dx);
                newPos.x += dir * (moveSpeed * 0.75f) * Time.deltaTime;
                SetState(NpcState.Walk);
            }
            else
            {
                SetState(NpcState.Idle);
            }

            newPos.x = Mathf.Clamp(newPos.x, minStageX, maxStageX);
            newPos.y = _groundY;
            transform.position = newPos;
        }

        private void HandleCombatDecision()
        {
            float dist = Mathf.Abs(playerTarget.position.x - transform.position.x);
            if (dist > preferredDistance + 2.0f) return;

            _decisionTimer += Time.deltaTime;
            if (_decisionTimer < decisionInterval) return;

            float roll = Random.value;
            if (roll < attackChance)
            {
                if (_attackCooldownTimer <= 0f)
                {
                    _decisionTimer = 0f;
                    StartCoroutine(PerformAttackRoutine());
                }
                else
                {
                    // Giữ timer sẵn sàng để tung chiêu ngay khi hết hồi chiêu (~1s ở Thường, dồn dập ở Khó)
                    _decisionTimer = decisionInterval;
                }
            }
            else if (roll < attackChance + blockChance)
            {
                if (_attackCooldownTimer <= 0f)
                {
                    _decisionTimer = 0f;
                    StartCoroutine(PerformAttackRoutine());
                }
                else if (_attackCooldownTimer > 0.25f)
                {
                    _decisionTimer = 0f;
                    StartCoroutine(PerformBlockRoutine());
                }
                else
                {
                    _decisionTimer = decisionInterval;
                }
            }
            else if (roll < attackChance + blockChance + jumpAttackChance)
            {
                if (_attackCooldownTimer <= 0f)
                {
                    _decisionTimer = 0f;
                    StartCoroutine(PerformJumpAttackRoutine());
                }
                else
                {
                    _decisionTimer = decisionInterval;
                }
            }
            else
            {
                _decisionTimer = 0f;
            }
        }

        private IEnumerator PerformAttackRoutine()
        {
            _isBusyAction = true;
            _attackCooldownTimer = attackCooldown;

            // Luân phiên giữa 2 đòn đánh của Boss Nữ: ThrowGift (Ném quà) & ThrowBag (Quăng túi xách)
            NpcState atkState = _useThrowBagNext ? NpcState.ThrowBag : NpcState.ThrowGift;
            _useThrowBagNext = !_useThrowBagNext;
            if (_animator != null) _animator.speed = _isHardMode ? 1.65f : 1.35f;
            SetState(atkState, resetFrame: true);
            PlayAnimatorImmediate(atkState);

            float duration = _isHardMode ? 0.34f : 0.42f;
            yield return new WaitForSeconds(duration * 0.48f);

            DealDamageToPlayer(preferredDistance + 1.35f, attackDamage);

            yield return new WaitForSeconds(duration * 0.52f);

            if (_animator != null) _animator.speed = 1f;
            _isBusyAction = false;
            SetState(NpcState.Idle, resetFrame: true);
        }

        private IEnumerator PerformBlockRoutine()
        {
            _isBusyAction = true;
            SetState(NpcState.Block, resetFrame: true);
            PlayAnimatorImmediate(NpcState.Block);

            yield return new WaitForSeconds(_isHardMode ? 0.28f : 0.38f);

            _isBusyAction = false;
            SetState(NpcState.Idle, resetFrame: true);
        }

        private IEnumerator PerformJumpAttackRoutine()
        {
            _isBusyAction = true;
            _attackCooldownTimer = attackCooldown;

            // Cảnh báo ngắn trước đòn nặng của Boss để người chơi kịp né hoặc đỡ
            SetState(NpcState.Idle, resetFrame: true);
            CombatFloatingText.Spawn(
                transform.position + new Vector3(0f, 2.35f, 0f),
                "! ĐÒN NẶNG !",
                new Color(1f, 0.25f, 0.15f, 1f),
                5.4f,
                0.45f
            );
            float warnDuration = _isHardMode ? 0.24f : 0.36f;
            float warnElapsed = 0f;
            Color origColor = _sr != null ? _sr.color : Color.white;
            while (warnElapsed < warnDuration)
            {
                warnElapsed += Time.deltaTime;
                if (_sr != null)
                {
                    float flash = 0.5f + 0.5f * Mathf.Sin(warnElapsed * 28f);
                    _sr.color = Color.Lerp(origColor, new Color(1f, 0.35f, 0.28f, 1f), flash);
                }
                yield return null;
            }
            if (_sr != null) _sr.color = origColor;

            if (_animator != null) _animator.speed = _isHardMode ? 1.55f : 1.25f;
            SetState(NpcState.Jump, resetFrame: true);
            PlayAnimatorImmediate(NpcState.Jump);

            float actualJumpDur = _isHardMode ? Mathf.Min(jumpDuration, 0.42f) : Mathf.Min(jumpDuration, 0.50f);
            float elapsed = 0f;
            float startX = transform.position.x;
            float dir = playerTarget != null ? Mathf.Sign(playerTarget.position.x - startX) : -1f;
            bool dealtJumpHit = false;

            while (elapsed < actualJumpDur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / actualJumpDur);
                float yOffset = Mathf.Sin(t * Mathf.PI) * jumpHeight;

                Vector3 pos = transform.position;
                pos.x = Mathf.Clamp(pos.x + dir * (moveSpeed * 0.72f) * Time.deltaTime, minStageX, maxStageX);
                pos.y = _groundY + yOffset;
                transform.position = pos;

                if (t > 0.52f && !dealtJumpHit)
                {
                    dealtJumpHit = true;
                    SetState(NpcState.ThrowBag, resetFrame: true);
                    PlayAnimatorImmediate(NpcState.ThrowBag);
                    DealDamageToPlayer(preferredDistance + 1.65f, jumpAttackDamage);
                }

                yield return null;
            }

            Vector3 finalPos = transform.position;
            finalPos.y = _groundY;
            transform.position = finalPos;

            if (_animator != null) _animator.speed = 1f;
            _isBusyAction = false;
            SetState(NpcState.Idle, resetFrame: true);
        }

        private void DealDamageToPlayer(float range, float rawDamage)
        {
            if (IsDead || playerTarget == null) return;
            if (BattleManager.Instance == null || BattleManager.Instance.State != BattleManager.BattleState.Playing) return;

            float dist = Mathf.Abs(playerTarget.position.x - transform.position.x);
            if (dist <= range)
            {
                var hero = BattleManager.Instance.cowLop;
                if (hero != null && !hero.IsGrounded)
                {
                    BattleManager.Instance.DamagePlayer1(rawDamage);
                    return;
                }

                float finalDmg = rawDamage;
                if (hero != null && hero.IsBlocking)
                {
                    finalDmg *= 0.2f;
                    CombatFloatingText.Spawn(
                        playerTarget.position + new Vector3(0f, 1.55f, 0f),
                        $"ĐỠ ĐÒN (-{Mathf.RoundToInt(finalDmg)})",
                        new Color(0.35f, 0.85f, 1f, 1f),
                        3.6f
                    );
                }
                BattleManager.Instance.DamagePlayer1(finalDmg);
                BattleManager.Instance.RegisterComboHit(false, finalDmg);
            }
        }

        public void ReceiveAttackFromPlayer(float rawDamage)
        {
            if (IsDead) return;

            float finalDmg = rawDamage;
            if (_state == NpcState.Block)
            {
                finalDmg *= 0.25f;
                CombatFloatingText.Spawn(
                    transform.position + new Vector3(0f, 1.6f, 0f),
                    $"BOSS ĐỠ ĐÒN (-{Mathf.RoundToInt(finalDmg)})",
                    new Color(0.75f, 0.85f, 1f, 1f),
                    3.5f
                );
            }
            else
            {
                TriggerHurtReaction();
                CombatFeelFX.FlashSprite(_sr, new Color(1f, 0.45f, 0.45f, 1f), 0.11f);
            }

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.DamagePlayer2(finalDmg);
            }
            else
            {
                currentHealth = Mathf.Max(0f, currentHealth - finalDmg);
            }

            CheckAndTriggerPhase2();
        }

        public void OnDefeated()
        {
            IsDead = true;
            StopAllCoroutines();
            _isBusyAction = true;
            CombatFeelFX.TriggerHitstop(0.06f);
            CombatFeelFX.ShakeCamera(0.28f, 0.28f);
            if (_sr != null)
            {
                _sr.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            }

            // Phát animation KO (Animator) + xoá người ngã gục qua transform
            if (_animator != null && _animator.enabled && _animator.runtimeAnimatorController != null)
            {
                // Chỉ gọi khi Controller thực sự có parameter/state "KO" để tránh lỗi console
                AnimatorSafe.TrySetTrigger(_animator, "KO");
                AnimatorSafe.TryPlayState(_animator, "KO", 0, 0f);
            }
            StartCoroutine(KOFallRoutine(0.55f, -90f));
        }

        /// <summary>Xoá ngã gục (KO): xoay người nằm xuống đất, chìm nhẹ xuống.</summary>
        private IEnumerator KOFallRoutine(float duration, float angleZ)
        {
            float elapsed = 0f;
            Vector3 basePos = transform.position;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.rotation = Quaternion.Euler(0f, 0f, angleZ * t);
                transform.position = new Vector3(basePos.x, basePos.y - 0.25f * t, basePos.z);
                yield return null;
            }
        }

        public void TriggerHurtReaction()
        {
            if (IsDead) return;
            // Không ngắt đòn đánh đang vung hoặc lúc đang đỡ; đồng thời giới hạn giãn cách bị khựng để Boss không bị stunlock
            if (_isBusyAction || _state == NpcState.Block) return;
            if (Time.time < _nextHurtStaggerTime) return;
            _nextHurtStaggerTime = Time.time + (_isHardMode ? 2.2f : 1.5f);
            StartCoroutine(PerformHurtRoutine());
        }

        private IEnumerator PerformHurtRoutine()
        {
            _isBusyAction = true;
            SetState(NpcState.Hurt, resetFrame: true);
            PlayAnimatorImmediate(NpcState.Hurt);
            if (_sr != null)
            {
                _sr.color = new Color(1f, 0.42f, 0.42f, 1f);
            }
            yield return new WaitForSeconds(0.14f);
            _isBusyAction = false;
            if (_sr != null)
            {
                _sr.color = IsPhase2Enraged ? new Color(1f, 0.78f, 0.82f, 1f) : Color.white;
            }
            SetState(NpcState.Idle, resetFrame: true);
        }

        private void SetState(NpcState newState, bool resetFrame = false)
        {
            if (_state == newState && !resetFrame) return;
            _state = newState;
            _frameIndex = 0;
            _frameTimer = 0f;
        }

        private void PlayAnimatorImmediate(NpcState targetState)
        {
            if (_animator == null || !_animator.enabled || _animator.runtimeAnimatorController == null) return;
            string stateName = targetState.ToString();
            if (targetState == NpcState.ThrowGift || targetState == NpcState.ThrowBag || targetState == NpcState.Jump || targetState == NpcState.Hurt)
            {
                _animator.SetTrigger(stateName);
            }
            _animator.Play(stateName, 0, 0f);
            _lastAnimatorState = targetState;
        }

        private void SyncAnimatorAndFallback()
        {
            if (_animator != null && _animator.enabled && _animator.runtimeAnimatorController != null)
            {
                float speedParam = 0f;
                if (_state == NpcState.Walk) speedParam = 1.0f;
                else if (_state == NpcState.Run) speedParam = 2.5f;

                _animator.SetFloat("Speed", speedParam);
                _animator.SetBool("IsGrounded", Mathf.Abs(transform.position.y - _groundY) < 0.05f);
                _animator.SetBool("Block", _state == NpcState.Block);

                if (_state != _lastAnimatorState && !_isBusyAction)
                {
                    switch (_state)
                    {
                        case NpcState.Idle:
                            _animator.Play("Idle", 0);
                            break;
                        case NpcState.Walk:
                            _animator.Play("Walk", 0);
                            break;
                        case NpcState.Run:
                            _animator.Play("Run", 0);
                            break;
                    }
                    _lastAnimatorState = _state;
                }
                return;
            }

            // Fallback bằng mảng Sprite nếu Animator tắt
            Sprite[] activeSprites = GetSpritesForState(_state, out float fps);
            if (activeSprites == null || activeSprites.Length == 0) return;

            _frameTimer += Time.deltaTime;
            float interval = 1f / Mathf.Max(1f, fps);
            if (_frameTimer >= interval)
            {
                _frameTimer -= interval;
                _frameIndex = (_frameIndex + 1) % activeSprites.Length;
            }

            int idx = Mathf.Clamp(_frameIndex, 0, activeSprites.Length - 1);
            if (activeSprites[idx] != null)
            {
                _sr.sprite = activeSprites[idx];
            }
        }

        private Sprite[] GetSpritesForState(NpcState state, out float fps)
        {
            switch (state)
            {
                case NpcState.Walk:
                    fps = walkFps;
                    return walkSprites != null && walkSprites.Length > 0 ? walkSprites : runSprites;
                case NpcState.Run:
                    fps = runFps;
                    return runSprites != null && runSprites.Length > 0 ? runSprites : walkSprites;
                case NpcState.Jump:
                    fps = jumpFps;
                    return jumpSprites != null && jumpSprites.Length > 0 ? jumpSprites : runSprites;
                case NpcState.ThrowGift:
                    fps = attackFps;
                    return throwGiftSprites != null && throwGiftSprites.Length > 0 ? throwGiftSprites : runSprites;
                case NpcState.ThrowBag:
                    fps = attackFps;
                    return throwBagSprites != null && throwBagSprites.Length > 0 ? throwBagSprites : throwGiftSprites;
                case NpcState.Block:
                    fps = idleFps;
                    return blockSprites != null && blockSprites.Length > 0 ? blockSprites : idleSprites;
                case NpcState.Hurt:
                    fps = attackFps;
                    return hurtSprites != null && hurtSprites.Length > 0 ? hurtSprites : idleSprites;
                default:
                    fps = idleFps;
                    return idleSprites;
            }
        }

#if UNITY_EDITOR
        public void AutoLoadSpritesInEditor()
        {
            const string sheetPath = "Assets/Art/Characters/NPC_Female/char_female_gift_bag_sheet.png";
            var dict = new Dictionary<string, Sprite>();
            Object[] allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(sheetPath);
            if (allAssets != null)
            {
                foreach (Object asset in allAssets)
                {
                    if (asset is Sprite sp)
                    {
                        dict[sp.name] = sp;
                    }
                }
            }

            Sprite GetSub(string key) => dict.TryGetValue(key, out Sprite s) ? s : null;
            Sprite[] GetRange(string prefix, int count)
            {
                var list = new List<Sprite>();
                for (int i = 0; i < count; i++)
                {
                    var s = GetSub($"{prefix}_{i}");
                    if (s != null) list.Add(s);
                }
                return list.ToArray();
            }

            idleSprites = GetRange("Female_Idle", 4);
            walkSprites = GetRange("Female_Walk", 8);
            runSprites = GetRange("Female_Run", 8);
            jumpSprites = GetRange("Female_Jump", 8);
            throwGiftSprites = GetRange("Female_ThrowGift", 6);
            throwBagSprites = GetRange("Female_ThrowBag", 8);
            blockSprites = new Sprite[] { GetSub("Female_Idle_1"), GetSub("Female_Idle_2") };
            hurtSprites = new Sprite[] { GetSub("Female_Jump_1"), GetSub("Female_Idle_3") };

            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null && idleSprites != null && idleSprites.Length > 0 && idleSprites[0] != null)
            {
                _sr.sprite = idleSprites[0];
            }
        }
#endif
    }
}
