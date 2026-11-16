using System.Collections.Generic;
using UnityEngine;

namespace ChiecLopCuoi.Gameplay
{
    public partial class CowLopPlayerController
    {
        private void PlayAnimatorStateImmediate(CowLopAnimState targetState, string stateName)
        {
            if (_animator == null || !_animator.enabled || _animator.runtimeAnimatorController == null) return;
            _animator.SetTrigger(stateName);
            _animator.Play(stateName, 0, 0f);
            _lastPlayedAnimatorState = targetState;
        }

        private void SyncAnimatorAndFallback()
        {
            // Nếu có Animator + AnimatorController thì điều khiển qua Animator
            if (_animator != null && _animator.enabled && _animator.runtimeAnimatorController != null)
            {
                int moveDirParam = 0;
                float speedParam = 0f;
                bool isRunningParam = false;

                if (_state == CowLopAnimState.WalkRight)
                {
                    moveDirParam = 1;
                    speedParam = 1f;
                }
                else if (_state == CowLopAnimState.WalkLeft)
                {
                    moveDirParam = -1;
                    speedParam = 1f;
                }
                else if (_state == CowLopAnimState.Run)
                {
                    moveDirParam = _facingDir;
                    speedParam = 2.5f;
                    isRunningParam = true;
                }

                _animator.SetInteger("MoveDir", moveDirParam);
                _animator.SetFloat("Speed", speedParam);
                _animator.SetBool("IsRunning", isRunningParam);
                _animator.SetBool("IsGrounded", _isGrounded);
                _animator.SetBool("Block", _state == CowLopAnimState.Block);

                // Đảm bảo chuyển trạng thái locomotion tức thì không trễ
                if (_state != _lastPlayedAnimatorState && _actionTimer <= 0f)
                {
                    switch (_state)
                    {
                        case CowLopAnimState.Idle:
                            _animator.Play("Idle", 0);
                            break;
                        case CowLopAnimState.WalkRight:
                            _animator.Play("WalkRight", 0);
                            break;
                        case CowLopAnimState.WalkLeft:
                            _animator.Play("WalkLeft", 0);
                            break;
                        case CowLopAnimState.Run:
                            _animator.Play("Run", 0);
                            break;
                        case CowLopAnimState.Block:
                            _animator.Play("Block", 0);
                            break;
                    }
                    _lastPlayedAnimatorState = _state;
                }
                return;
            }

            // Fallback bằng mảng Sprite nếu Animator bị tắt
            Sprite[] activeFrames = GetCurrentStateSprites(out float fps);
            if (activeFrames == null || activeFrames.Length == 0) return;

            _frameTimer += Time.deltaTime;
            float interval = 1f / Mathf.Max(1f, fps);
            if (_frameTimer >= interval)
            {
                _frameTimer -= interval;
                _frameIndex = (_frameIndex + 1) % activeFrames.Length;
            }

            int safeIndex = Mathf.Clamp(_frameIndex, 0, activeFrames.Length - 1);
            if (activeFrames[safeIndex] != null)
            {
                _sr.sprite = activeFrames[safeIndex];
            }
        }

        private Sprite[] GetCurrentStateSprites(out float fps)
        {
            switch (_state)
            {
                case CowLopAnimState.WalkRight:
                    fps = walkFps;
                    return walkRightSprites != null && walkRightSprites.Length > 0 ? walkRightSprites : idleSprites;
                case CowLopAnimState.WalkLeft:
                    fps = walkFps;
                    return walkLeftSprites != null && walkLeftSprites.Length > 0 ? walkLeftSprites : walkRightSprites;
                case CowLopAnimState.Run:
                    fps = runFps;
                    return runSprites != null && runSprites.Length > 0 ? runSprites : walkRightSprites;
                case CowLopAnimState.Jump:
                    fps = 8f;
                    return jumpSprites != null && jumpSprites.Length > 0 ? jumpSprites : runSprites;
                case CowLopAnimState.Attack:
                    fps = attackFps;
                    return attackSprites != null && attackSprites.Length > 0 ? attackSprites : idleSprites;
                case CowLopAnimState.TireSpin:
                    fps = attackFps;
                    return tireSpinSprites != null && tireSpinSprites.Length > 0 ? tireSpinSprites : attackSprites;
                case CowLopAnimState.Block:
                    fps = 8f;
                    return blockSprites != null && blockSprites.Length > 0 ? blockSprites : tireSpinSprites;
                case CowLopAnimState.TireThrow:
                    fps = attackFps;
                    return tireThrowSprites != null && tireThrowSprites.Length > 0 ? tireThrowSprites : attackSprites;
                case CowLopAnimState.JumpSlam:
                    fps = attackFps;
                    return jumpSlamSprites != null && jumpSlamSprites.Length > 0 ? jumpSlamSprites : attackSprites;
                default:
                    fps = idleFps;
                    return idleSprites;
            }
        }

#if UNITY_EDITOR
        public void AutoLoadSpritesInEditor()
        {
            const string rightSheetPath = "Assets/Art/Characters/CowLop/cowlop_right.png";
            const string baseSheetPath = "Assets/Art/Characters/CowLop/CowLop_Base_Actions.png";
            const string skillsSheetPath = "Assets/Art/Characters/CowLop/CowLop_Skills.png";
            const string koSheetPath = "Assets/Art/Characters/CowLop/CowLop_KO.png";

            var dict = new Dictionary<string, Sprite>();
            void LoadIntoDict(string path)
            {
                Object[] allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                if (allAssets == null) return;
                foreach (Object asset in allAssets)
                {
                    if (asset is Sprite sp) dict[sp.name] = sp;
                }
            }

            LoadIntoDict(rightSheetPath);
            LoadIntoDict(baseSheetPath);
            LoadIntoDict(skillsSheetPath);
            LoadIntoDict(koSheetPath);

            Sprite GetSub(string name)
            {
                return dict.TryGetValue(name, out Sprite s) ? s : null;
            }

            // Đứng yên, Đỡ đòn, Nhảy, Bị thương từ CowLop_Base_Actions.png (16 frames)
            idleSprites = FilterNonNull(
                GetSub("CowLop_Idle_0"),
                GetSub("CowLop_Idle_1"),
                GetSub("CowLop_Idle_2"),
                GetSub("CowLop_Idle_3"));

            blockSprites = FilterNonNull(
                GetSub("CowLop_Block_0"),
                GetSub("CowLop_Block_1"),
                GetSub("CowLop_Block_2"),
                GetSub("CowLop_Block_3"));

            jumpSprites = FilterNonNull(
                GetSub("CowLop_Jump_0"),
                GetSub("CowLop_Jump_1"),
                GetSub("CowLop_Jump_2"),
                GetSub("CowLop_Jump_3"));

            hurtSprites = FilterNonNull(
                GetSub("CowLop_Hurt_0"),
                GetSub("CowLop_Hurt_1"),
                GetSub("CowLop_Hurt_2"),
                GetSub("CowLop_Hurt_3"));

            // Bước sang phải & Bước sang trái (lật flipX): 8 frames đầy đủ 2 bước Phải - Trái từ hàng 0 của cowlop_right.png
            walkRightSprites = FilterNonNull(
                GetSub("cowlop_right_0"),
                GetSub("cowlop_right_1"),
                GetSub("cowlop_right_2"),
                GetSub("cowlop_right_3"),
                GetSub("cowlop_right_4"),
                GetSub("cowlop_right_5"),
                GetSub("cowlop_right_6"),
                GetSub("cowlop_right_7"));

            walkLeftSprites = walkRightSprites;

            // Chạy: 8 frames sải chân nâng gối luân phiên 2 chân từ hàng 1 của cowlop_right.png
            runSprites = FilterNonNull(
                GetSub("cowlop_right_8"),
                GetSub("cowlop_right_9"),
                GetSub("cowlop_right_10"),
                GetSub("cowlop_right_11"),
                GetSub("cowlop_right_12"),
                GetSub("cowlop_right_13"),
                GetSub("cowlop_right_14"),
                GetSub("cowlop_right_15"));

            // Đánh thường & 3 tuyệt kỹ từ CowLop_Skills.png (24 frames)
            attackSprites = FilterNonNull(
                GetSub("CowLop_Attack_0"),
                GetSub("CowLop_Attack_1"),
                GetSub("CowLop_Attack_2"),
                GetSub("CowLop_Attack_3"),
                GetSub("CowLop_Attack_4"),
                GetSub("CowLop_Attack_5"));

            tireSpinSprites = FilterNonNull(
                GetSub("CowLop_TireSpin_0"),
                GetSub("CowLop_TireSpin_1"),
                GetSub("CowLop_TireSpin_2"),
                GetSub("CowLop_TireSpin_3"),
                GetSub("CowLop_TireSpin_4"),
                GetSub("CowLop_TireSpin_5"));

            tireThrowSprites = FilterNonNull(
                GetSub("CowLop_TireThrow_0"),
                GetSub("CowLop_TireThrow_1"),
                GetSub("CowLop_TireThrow_2"),
                GetSub("CowLop_TireThrow_3"),
                GetSub("CowLop_TireThrow_4"),
                GetSub("CowLop_TireThrow_5"));

            jumpSlamSprites = FilterNonNull(
                GetSub("CowLop_JumpSlam_0"),
                GetSub("CowLop_JumpSlam_1"),
                GetSub("CowLop_JumpSlam_2"),
                GetSub("CowLop_JumpSlam_3"),
                GetSub("CowLop_JumpSlam_4"),
                GetSub("CowLop_JumpSlam_5"));

            koSprites = FilterNonNull(
                GetSub("CowLop_KO_0"),
                GetSub("CowLop_KO_1"),
                GetSub("CowLop_KO_2"),
                GetSub("CowLop_KO_3"));

            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null && idleSprites != null && idleSprites.Length > 0 && idleSprites[0] != null)
            {
                _sr.sprite = idleSprites[0];
            }
        }

        private static Sprite[] FilterNonNull(params Sprite[] items)
        {
            var list = new List<Sprite>();
            foreach (var it in items)
            {
                if (it != null) list.Add(it);
            }
            return list.ToArray();
        }
#endif
    }
}
