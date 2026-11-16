using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ChiecLopCuoi.NPC;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.Gameplay
{
    /// <summary>
    /// Bộ điều khiển nhân vật chính Cow Lốp sử dụng Animator & AnimationClip từ duy nhất CowLop.png:
    /// - Bước bên phải (D / Mũi tên Phải): state WalkRight (6 frame bước phải)
    /// - Bước bên trái (A / Mũi tên Trái): state WalkLeft (chu kỳ bước trái riêng trong CowLop.png)
    /// - Chạy nhanh (Giữ Shift + A/D hoặc nhấp đúp A/D): state Run (chu kỳ chạy sải chân tốc độ cao)
    /// - Nhảy (W / Space), Đỡ đòn (H)
    /// - Kỹ năng đánh nhau (Animator Combat Clips):
    ///   + J: Đánh thường cận chiến (Attack - 6 frame đấm + đá cao)
    ///   + K: Phóng chiếc lốp xoay tầm xa (TireThrow)
    ///   + L: Đập lốp nhảy cao diện rộng (JumpSlam)
    ///   + U: Tuyệt kỹ Xoay Lốp liên hoàn (TireSpin)
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public partial class CowLopPlayerController : MonoBehaviour
    {
        public enum CowLopAnimState
        {
            Idle,
            WalkRight,
            WalkLeft,
            Run,
            Jump,
            Attack,
            TireSpin,
            TireThrow,
            JumpSlam,
            Block
        }

        [Header("Movement Settings")]
        public float walkSpeed = 4.8f;
        public float runSpeed = 8.4f;
        public float jumpVelocity = 13.05f;
        public float gravity = 24f;
        public float groundY = -3.76f;
        public float minX = -58.0f;
        public float maxX = 218.0f;

        [Header("Combat Stats")]
        public float maxHealth = 150f;
        public float currentHealth = 150f;
        public float meleeRange = 2.8f;
        public float tireThrowRange = 7.5f;
        public float attackDamage = 19f;
        public float tireThrowDamage = 24f;
        public float jumpSlamDamage = 34f;
        public float tireSpinDamage = 38f;

        [Header("Skill Cooldowns (Seconds)")]
        public float attackCooldown = 0.35f;
        public float tireThrowCooldown = 1.4f;
        public float jumpSlamCooldown = 2.4f;
        public float tireSpinCooldown = 3.8f;

        private float _nextAttackReadyTime;
        private float _nextTireThrowReadyTime;
        private float _nextJumpSlamReadyTime;
        private float _nextTireSpinReadyTime;

        [Header("Sliced Sub-Sprites (From CowLop Sheets)")]
        public Sprite[] idleSprites;
        public Sprite[] walkRightSprites;
        public Sprite[] walkLeftSprites;
        public Sprite[] runSprites;
        public Sprite[] jumpSprites;
        public Sprite[] blockSprites;
        public Sprite[] hurtSprites;
        public Sprite[] attackSprites;
        public Sprite[] tireSpinSprites;
        public Sprite[] tireThrowSprites;
        public Sprite[] jumpSlamSprites;
        public Sprite[] koSprites;

        [Header("Animation Speed (Fallback FPS)")]
        public float idleFps = 6f;
        public float walkFps = 12f;
        public float runFps = 15f;
        public float attackFps = 12f;

        private SpriteRenderer _sr;
        private Animator _animator;
        private CowLopAnimState _state = CowLopAnimState.Idle;
        private CowLopAnimState _lastPlayedAnimatorState = CowLopAnimState.Idle;
        private int _frameIndex;
        private float _frameTimer;
        private float _actionTimer;
        private float _verticalVelocity;
        private bool _isGrounded = true;
        private int _facingDir = 1; // 1 = phải, -1 = trái
        [Header("Entrance Drop")]
        public bool playDropInOnStart = true;
        public float dropStartHeight = 8.0f;
        public float dropGravityScale = 1.45f;

        private float _baseScaleX = 2.81f;
        private float _baseScaleY = 2.81f;
        private float _runSfxCooldown;
        private bool _isDropInActive;
        private float _landingSquashTimer;

        // Hỗ trợ nhấp đúp A/D để chạy nhanh (ngoài giữ phím Shift)
        private int _lastTapDir = 0;
        private float _lastTapTime = -10f;
        private bool _doubleTapRunning = false;

        private bool _isDefeated;
        private int _uiMoveDir = 0;
        private bool _uiBlocking = false;
        private float _timedBlockUntil = -1f;

        public bool IsBlocking => _state == CowLopAnimState.Block;
        public bool IsGrounded => _isGrounded;
        public bool CanAcceptActionInput => !_isDefeated && enabled && gameObject.activeInHierarchy && (BattleManager.Instance == null || BattleManager.Instance.State == BattleManager.BattleState.Playing);

        public float AttackCooldown01 => attackCooldown > 0f ? Mathf.Clamp01((_nextAttackReadyTime - Time.time) / attackCooldown) : 0f;
        public float TireSpinCooldown01 => tireSpinCooldown > 0f ? Mathf.Clamp01((_nextTireSpinReadyTime - Time.time) / tireSpinCooldown) : 0f;
        public float TireThrowCooldown01 => tireThrowCooldown > 0f ? Mathf.Clamp01((_nextTireThrowReadyTime - Time.time) / tireThrowCooldown) : 0f;
        public float JumpSlamCooldown01 => jumpSlamCooldown > 0f ? Mathf.Clamp01((_nextJumpSlamReadyTime - Time.time) / jumpSlamCooldown) : 0f;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            if (_animator != null)
            {
                _animator.enabled = true;
            }

            _baseScaleX = Mathf.Abs(transform.localScale.x) > 0.01f ? Mathf.Abs(transform.localScale.x) : 2.81f;
            _baseScaleY = Mathf.Abs(transform.localScale.y) > 0.01f ? Mathf.Abs(transform.localScale.y) : 2.81f;

            string sceneName = SceneManager.GetActiveScene().name.ToLower();
            if (sceneName.Contains("sample"))
            {
                groundY = -3.56f;
                minX = -48.0f;
                maxX = 77.0f;
            }
            else
            {
                groundY = -3.76f;
                minX = -58.0f;
                maxX = 218.0f;
            }

            _facingDir = 1;
            transform.localScale = new Vector3(_baseScaleX, _baseScaleY, 1f);

            var pos = transform.position;
            if (playDropInOnStart)
            {
                pos.y = groundY + Mathf.Max(0f, dropStartHeight);
                _isGrounded = false;
                _verticalVelocity = -2.5f;
                _isDropInActive = true;
                _state = CowLopAnimState.Jump;
            }
            else
            {
                pos.y = groundY;
                _isGrounded = true;
                _verticalVelocity = 0f;
                _isDropInActive = false;
            }
            transform.position = pos;

#if UNITY_EDITOR
            AutoLoadSpritesInEditor();
#endif
        }

        private void Update()
        {
            if (_isDefeated)
            {
                return;
            }

            if (BattleManager.Instance != null && BattleManager.Instance.State != BattleManager.BattleState.Playing)
            {
                SyncAnimatorAndFallback();
                return;
            }

            if (!_isDropInActive)
            {
                HandleInputAndMovement();
            }
            HandleVerticalPhysics();
            SyncAnimatorAndFallback();
        }

        private void LateUpdate()
        {
            ApplyFacingFlip();
        }

        private void ApplyFacingFlip()
        {
            // Giữ localScale dương cố định để Collider và vị trí không bị lệch; thêm squash nhẹ lúc tiếp đất nhập màn
            if (_landingSquashTimer > 0f)
            {
                _landingSquashTimer = Mathf.Max(0f, _landingSquashTimer - Time.deltaTime);
                float t = _landingSquashTimer / 0.14f;
                float scaleX = _baseScaleX * (1f + 0.12f * t);
                float scaleY = _baseScaleY * (1f - 0.12f * t);
                transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }
            else
            {
                transform.localScale = new Vector3(_baseScaleX, _baseScaleY, 1f);
            }
            if (_sr == null) return;

            // Cả cowlop_right.png và CowLop_Skills.png đều được vẽ hướng mặt sang PHẢI
            // -> Đi sang Phải / đánh về Phải: flipX = false; Đi sang Trái (WalkLeft) / đánh về Trái: flipX = true
            _sr.flipX = (_facingDir < 0);
        }

        private struct KeyBindingGroup
        {
            public KeyCode[] legacyKeys;
            public Key[] inputSystemKeys;
        }

        private static readonly KeyBindingGroup[] SkillBindings = new KeyBindingGroup[]
        {
            // Index 0 -> Chiêu 1: Đánh thường (J / 1 / Numpad 1)
            new KeyBindingGroup
            {
                legacyKeys = new[] { KeyCode.J, KeyCode.Alpha1, KeyCode.Keypad1 },
                inputSystemKeys = new[] { Key.J, Key.Digit1, Key.Numpad1 }
            },
            // Index 1 -> Chiêu 2: Ném lốp (K / 2 / Numpad 2)
            new KeyBindingGroup
            {
                legacyKeys = new[] { KeyCode.K, KeyCode.Alpha2, KeyCode.Keypad2 },
                inputSystemKeys = new[] { Key.K, Key.Digit2, Key.Numpad2 }
            },
            // Index 2 -> Chiêu 3: Xoay lốp (U / 3 / Numpad 3)
            new KeyBindingGroup
            {
                legacyKeys = new[] { KeyCode.U, KeyCode.Alpha3, KeyCode.Keypad3 },
                inputSystemKeys = new[] { Key.U, Key.Digit3, Key.Numpad3 }
            },
            // Index 3 -> Chiêu 4: Đập lốp nhảy cao (L / 4 / Numpad 4)
            new KeyBindingGroup
            {
                legacyKeys = new[] { KeyCode.L, KeyCode.Alpha4, KeyCode.Keypad4 },
                inputSystemKeys = new[] { Key.L, Key.Digit4, Key.Numpad4 }
            },
            // Index 4 -> Chiêu 5: Nhảy (W / UpArrow / Space / 5 / Numpad 5)
            new KeyBindingGroup
            {
                legacyKeys = new[] { KeyCode.W, KeyCode.UpArrow, KeyCode.Space, KeyCode.Alpha5, KeyCode.Keypad5 },
                inputSystemKeys = new[] { Key.W, Key.UpArrow, Key.Space, Key.Digit5, Key.Numpad5 }
            },
            // Index 5 -> Chiêu 6: Đỡ đòn (H / 6 / Numpad 6)
            new KeyBindingGroup
            {
                legacyKeys = new[] { KeyCode.H, KeyCode.Alpha6, KeyCode.Keypad6 },
                inputSystemKeys = new[] { Key.H, Key.Digit6, Key.Numpad6 }
            }
        };

        /// <summary>Kiểm tra nhấn xuống theo số thứ tự chiêu 1..6 (hỗ trợ cả phím số 1-6, Numpad 1-6 và phím tắt chữ).</summary>
        public static bool IsSkillInputDown(int skillIndex1To6)
        {
            int idx = skillIndex1To6 - 1;
            if (idx < 0 || idx >= SkillBindings.Length) return false;
            var group = SkillBindings[idx];
            for (int i = 0; i < group.legacyKeys.Length; i++)
            {
                if (Input.GetKeyDown(group.legacyKeys[i])) return true;
            }
            var kb = Keyboard.current;
            if (kb != null)
            {
                for (int i = 0; i < group.inputSystemKeys.Length; i++)
                {
                    if (kb[group.inputSystemKeys[i]].wasPressedThisFrame) return true;
                }
            }
            return false;
        }

        /// <summary>Kiểm tra đang giữ theo số thứ tự chiêu 1..6 (hỗ trợ cả phím số 1-6, Numpad 1-6 và phím tắt chữ).</summary>
        public static bool IsSkillInputHeld(int skillIndex1To6)
        {
            int idx = skillIndex1To6 - 1;
            if (idx < 0 || idx >= SkillBindings.Length) return false;
            var group = SkillBindings[idx];
            for (int i = 0; i < group.legacyKeys.Length; i++)
            {
                if (Input.GetKey(group.legacyKeys[i])) return true;
            }
            var kb = Keyboard.current;
            if (kb != null)
            {
                for (int i = 0; i < group.inputSystemKeys.Length; i++)
                {
                    if (kb[group.inputSystemKeys[i]].isPressed) return true;
                }
            }
            return false;
        }

        private void HandleInputAndMovement()
        {
            if (_actionTimer > 0f)
            {
                _actionTimer -= Time.deltaTime;
                if (_actionTimer <= 0f)
                {
                    _state = CowLopAnimState.Idle;
                    _frameIndex = 0;
                }
            }

            // Phím kỹ năng chiến đấu (1-4 hoặc J/K/U/L)
            if (IsSkillInputDown(1))
            {
                TriggerAttack();
                return;
            }
            if (IsSkillInputDown(2))
            {
                TriggerTireThrow();
                return;
            }
            if (IsSkillInputDown(3))
            {
                TriggerTireSpin();
                return;
            }
            if (IsSkillInputDown(4))
            {
                TriggerJumpSlam();
                return;
            }

            // Giữ H hoặc phím 6, giữ nút Đỡ UI, hoặc giữ Đỡ theo thời hạn từ lệnh thoại "Phòng thủ"
            bool blockKeyHeld = IsSkillInputHeld(6);
            bool holdingBlock = (blockKeyHeld || _uiBlocking || Time.time < _timedBlockUntil) && _isGrounded && _actionTimer <= 0f;
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                _animator.SetBool("Block", holdingBlock);
            }
            if (holdingBlock)
            {
                _state = CowLopAnimState.Block;
                return;
            }

            // Nhảy (W / UpArrow / Space / phím 5)
            if (IsSkillInputDown(5))
            {
                TriggerJump();
            }

            // Phát hiện nhấp đúp phím A/D để kích hoạt Chạy nhanh (Run)
            var kb = Keyboard.current;
            bool leftDown = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) ||
                            (kb != null && (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame));
            bool rightDown = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) ||
                             (kb != null && (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame));

            if (leftDown)
            {
                float dt = Time.time - _lastTapTime;
                if (_lastTapDir == -1 && dt > 0.02f && dt < 0.28f) _doubleTapRunning = true;
                _lastTapDir = -1;
                _lastTapTime = Time.time;
            }
            if (rightDown)
            {
                float dt = Time.time - _lastTapTime;
                if (_lastTapDir == 1 && dt > 0.02f && dt < 0.28f) _doubleTapRunning = true;
                _lastTapDir = 1;
                _lastTapTime = Time.time;
            }

            // Di chuyển ngang (A/D, Mũi tên Trái/Phải hoặc Joystick tròn đã đồng bộ / nút UI)
            float moveX = _uiMoveDir;
            bool joyRun = false;
            if (Mathf.Approximately(moveX, 0f) && HorizontalCircularController.Instance != null)
            {
                moveX = HorizontalCircularController.Instance.Horizontal;
                joyRun = HorizontalCircularController.Instance.IsPointerDragging && Mathf.Abs(moveX) > 0.85f;
            }
            if (Mathf.Approximately(moveX, 0f))
            {
                if (HorizontalCircularController.IsLeftInputHeld()) moveX = -1f;
                else if (HorizontalCircularController.IsRightInputHeld()) moveX = 1f;
            }

            if (Mathf.Abs(moveX) <= 0.01f)
            {
                _doubleTapRunning = false;
            }

            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool isRunning = shiftHeld || _doubleTapRunning || joyRun;
            float speed = isRunning ? runSpeed : walkSpeed;

            if (Mathf.Abs(moveX) > 0.01f)
            {
                _facingDir = moveX > 0f ? 1 : -1;

                // Lấy giới hạn động theo Chốt chặn (StageZoneGateManager)
                float effectiveMaxX = StageZoneGateManager.Instance != null
                    ? StageZoneGateManager.Instance.GetClampedMaxX(maxX)
                    : maxX;

                Vector3 pos = transform.position;
                pos.x = Mathf.Clamp(pos.x + moveX * speed * Time.deltaTime, minX, effectiveMaxX);
                transform.position = pos;

                if (_isGrounded && _actionTimer <= 0f)
                {
                    if (isRunning)
                    {
                        _state = CowLopAnimState.Run;
                    }
                    else
                    {
                        _state = _facingDir > 0 ? CowLopAnimState.WalkRight : CowLopAnimState.WalkLeft;
                    }

                    _runSfxCooldown -= Time.deltaTime;
                    if (_runSfxCooldown <= 0f)
                    {
                        _runSfxCooldown = isRunning ? 0.28f : 0.42f;
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayRun();
                    }
                }
            }
            else if (_isGrounded && _actionTimer <= 0f)
            {
                _state = CowLopAnimState.Idle;
            }
        }

        private void HandleVerticalPhysics()
        {
            if (!_isGrounded || _verticalVelocity > 0f)
            {
                float activeGravity = _isDropInActive ? gravity * Mathf.Max(1f, dropGravityScale) : gravity;
                Vector3 pos = transform.position;
                pos.y += _verticalVelocity * Time.deltaTime;
                _verticalVelocity -= activeGravity * Time.deltaTime;

                if (pos.y <= groundY)
                {
                    pos.y = groundY;
                    _verticalVelocity = 0f;
                    _isGrounded = true;
                    if (_isDropInActive)
                    {
                        _isDropInActive = false;
                        _landingSquashTimer = 0.14f;
                        _state = CowLopAnimState.Idle;
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayHitImpact();
                    }
                }
                transform.position = pos;
            }
        }

        public void SetUIMoveDirection(int dir)
        {
            _uiMoveDir = Mathf.Clamp(dir, -1, 1);
        }

        public void SetUIBlock(bool blocking)
        {
            _uiBlocking = blocking;
            if (!blocking) _timedBlockUntil = -1f;
        }

        /// <summary>
        /// Kích hoạt đỡ đòn có thời hạn (phục vụ lệnh giọng nói "Phòng thủ").
        /// Tự động ngắt sớm nếu người chơi nhảy hoặc tung chiêu tấn công.
        /// </summary>
        public void TriggerTimedBlock(float duration = 1.6f)
        {
            if (!CanAcceptActionInput || !_isGrounded) return;
            _timedBlockUntil = Mathf.Max(_timedBlockUntil, Time.time + Mathf.Max(0.3f, duration));
            if (_actionTimer <= 0f)
            {
                _state = CowLopAnimState.Block;
                PlayAnimatorStateImmediate(CowLopAnimState.Block, "Block");
            }
        }

    }
}
