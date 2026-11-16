using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChiecLopCuoi.NPC;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.Gameplay
{
    public partial class CowLopPlayerController
    {
        public bool TriggerJump()
        {
            if (!CanAcceptActionInput || !_isGrounded) return false;
            _timedBlockUntil = -1f;

            _verticalVelocity = jumpVelocity;
            _isGrounded = false;
            if (_actionTimer <= 0f)
            {
                _state = CowLopAnimState.Jump;
                PlayAnimatorStateImmediate(CowLopAnimState.Jump, "Jump");
            }
            if (AudioManager.Instance != null) AudioManager.Instance.PlayJump();
            return true;
        }

        private bool CheckSkillReady(float readyTime, string skillName)
        {
            if (Time.time >= readyTime) return true;
            float remain = readyTime - Time.time;
            CombatFloatingText.Spawn(
                transform.position + new Vector3(0f, 1.75f, 0f),
                $"{skillName}: Hồi chiêu {remain:0.0}s",
                new Color(0.78f, 0.86f, 0.95f, 0.95f),
                3.1f,
                0.55f
            );
            return false;
        }

        public void TriggerAttack()
        {
            if (!CanAcceptActionInput) return;
            if (Time.time < _nextAttackReadyTime) return;
            _timedBlockUntil = -1f;
            _nextAttackReadyTime = Time.time + attackCooldown;

            _state = CowLopAnimState.Attack;
            _frameIndex = 0;
            _frameTimer = 0f;
            _actionTimer = 0.48f;
            PlayAnimatorStateImmediate(CowLopAnimState.Attack, "Attack");

            if (AudioManager.Instance != null) AudioManager.Instance.PlayWhoosh(0.85f);

            float comboMult = BattleManager.Instance != null ? BattleManager.Instance.GetComboDamageMultiplier(true) : 1f;
            DealDamageToTargets(meleeRange, attackDamage * comboMult, isRageSkill: false);
        }

        public void TriggerTireSpin()
        {
            if (!CanAcceptActionInput) return;
            if (!CheckSkillReady(_nextTireSpinReadyTime, "Xoay Lốp [U]")) return;
            _timedBlockUntil = -1f;
            _nextTireSpinReadyTime = Time.time + tireSpinCooldown;

            bool isRage = BattleManager.Instance != null && BattleManager.Instance.TryConsumeFullRevenge(true);
            float mult = isRage ? 1.65f : 1f;
            float range = isRage ? meleeRange + 1.8f : meleeRange + 0.9f;

            _state = CowLopAnimState.TireSpin;
            _frameIndex = 0;
            _frameTimer = 0f;
            _actionTimer = 0.62f;
            PlayAnimatorStateImmediate(CowLopAnimState.TireSpin, "TireSpin");

            if (AudioManager.Instance != null) AudioManager.Instance.PlayWhoosh(1.15f);

            if (isRage)
            {
                CombatFeelFX.TriggerHitstop(0.045f);
                CombatFeelFX.ShakeCamera(0.22f, 0.20f);
                CombatFloatingText.Spawn(
                    transform.position + new Vector3(0f, 2.05f, 0f),
                    "SIÊU TUYỆT KỸ: CUỒNG PHONG LỐP!",
                    new Color(1f, 0.3f, 0.12f, 1f),
                    4.8f,
                    1.15f
                );
            }

            StartCoroutine(TireSpinMultiHitRoutine(range, tireSpinDamage * mult, isRage));
        }

        private IEnumerator TireSpinMultiHitRoutine(float range, float totalDamage, bool isRage)
        {
            DealDamageToTargets(range, totalDamage * 0.55f, isRage);
            yield return new WaitForSeconds(0.24f);
            DealDamageToTargets(range, totalDamage * 0.45f, isRage);
        }

        public void TriggerTireThrow()
        {
            if (!CanAcceptActionInput) return;
            if (!CheckSkillReady(_nextTireThrowReadyTime, "Ném Lốp [K]")) return;
            _timedBlockUntil = -1f;
            _nextTireThrowReadyTime = Time.time + tireThrowCooldown;

            _state = CowLopAnimState.TireThrow;
            _frameIndex = 0;
            _frameTimer = 0f;
            _actionTimer = 0.48f;
            PlayAnimatorStateImmediate(CowLopAnimState.TireThrow, "TireThrow");

            if (AudioManager.Instance != null) AudioManager.Instance.PlayWhoosh(1.0f);

            float comboMult = BattleManager.Instance != null ? BattleManager.Instance.GetComboDamageMultiplier(true) : 1f;
            bool isEmpowered = BattleManager.Instance != null && BattleManager.Instance.p1Revenge >= 100f;

            Vector3 spawnPos = transform.position + new Vector3(_facingDir * 0.85f, 0.65f, 0f);
            Sprite fallback = (tireSpinSprites != null && tireSpinSprites.Length > 0) ? tireSpinSprites[0] : null;
            TireProjectile.Spawn(spawnPos, _facingDir, tireThrowDamage * comboMult, isEmpowered, fallback);
        }

        public void TriggerJumpSlam()
        {
            if (!CanAcceptActionInput) return;
            if (!CheckSkillReady(_nextJumpSlamReadyTime, "Đập Lốp [L]")) return;
            _timedBlockUntil = -1f;
            _nextJumpSlamReadyTime = Time.time + jumpSlamCooldown;

            bool isRage = BattleManager.Instance != null && BattleManager.Instance.TryConsumeFullRevenge(true);
            float mult = isRage ? 1.65f : 1f;

            _state = CowLopAnimState.JumpSlam;
            _frameIndex = 0;
            _frameTimer = 0f;
            _actionTimer = 0.56f;
            PlayAnimatorStateImmediate(CowLopAnimState.JumpSlam, "JumpSlam");

            if (_isGrounded)
            {
                _verticalVelocity = jumpVelocity * 0.78f;
                _isGrounded = false;
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayWhoosh(1.25f);

            if (isRage)
            {
                CombatFeelFX.TriggerHitstop(0.055f);
                CombatFeelFX.ShakeCamera(0.28f, 0.24f);
                CombatFloatingText.Spawn(
                    transform.position + new Vector3(0f, 2.05f, 0f),
                    "SIÊU TUYỆT KỸ: THIÊN THẠCH LỐP!",
                    new Color(1f, 0.32f, 0.15f, 1f),
                    4.8f,
                    1.15f
                );
            }
            else
            {
                CombatFeelFX.ShakeCamera(0.14f, 0.14f);
            }

            DealDamageToTargets(meleeRange + (isRage ? 1.6f : 0.75f), jumpSlamDamage * mult, isRage);
        }

        private void DealDamageToTargets(float range, float damage, bool isRageSkill)
        {
            bool hitAny = false;

            // 1. Gây sát thương lên Boss Nữ (NPC_Female)
            if (BattleManager.Instance != null && BattleManager.Instance.npcFemale != null)
            {
                var npc = BattleManager.Instance.npcFemale;
                if (!npc.IsDead && npc.combatUnlocked)
                {
                    float dx = npc.transform.position.x - transform.position.x;
                    bool isFacingNpc = (_facingDir > 0 && dx >= -0.6f) || (_facingDir < 0 && dx <= 0.6f) || isRageSkill;
                    if (isFacingNpc && Mathf.Abs(dx) <= range)
                    {
                        npc.ReceiveAttackFromPlayer(damage);
                        BattleManager.Instance.RegisterComboHit(true, damage);
                        hitAny = true;
                    }
                }
            }

            // 2. Gây sát thương lên tất cả Quái vật trong tầm đánh
            var monsters = FindObjectsByType<MonsterCombatController>(FindObjectsSortMode.None);
            foreach (var monster in monsters)
            {
                if (monster == null || monster.IsDead) continue;
                float dx = monster.transform.position.x - transform.position.x;
                bool isFacingMonster = (_facingDir > 0 && dx >= -0.6f) || (_facingDir < 0 && dx <= 0.6f) || isRageSkill;
                if (isFacingMonster && Mathf.Abs(dx) <= range)
                {
                    monster.TakeDamage(damage, isRageSkill);
                    hitAny = true;
                }
            }

            // 3. Gây sát thương lên Thùng gỗ / Cột mốc trong Rừng Đêm Level 3
            var props = FindObjectsByType<BreakableForestProp>(FindObjectsSortMode.None);
            foreach (var prop in props)
            {
                if (prop == null || prop.IsBroken) continue;
                float dx = prop.transform.position.x - transform.position.x;
                bool isFacingProp = (_facingDir > 0 && dx >= -0.6f) || (_facingDir < 0 && dx <= 0.6f) || isRageSkill;
                if (isFacingProp && Mathf.Abs(dx) <= range)
                {
                    prop.TakeDamage(damage);
                    hitAny = true;
                }
            }

            if (hitAny && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayHitImpact();
            }
        }

        public void OnDefeated()
        {
            if (_isDefeated) return;
            _isDefeated = true;
            _actionTimer = 999f;
            _state = CowLopAnimState.Idle;
            _uiMoveDir = 0;
            _uiBlocking = false;
            StopAllCoroutines();

            bool hasKoState = _animator != null && _animator.enabled && _animator.runtimeAnimatorController != null
                && AnimatorSafe.HasState(_animator, "KO", 0);

            if (hasKoState)
            {
                if (_sr != null)
                {
                    _sr.enabled = true;
                    _sr.color = Color.white;
                }
                AnimatorSafe.TrySetTrigger(_animator, "KO");
                AnimatorSafe.TryPlayState(_animator, "KO", 0, 0f);
                StartCoroutine(KOFadeAndHideRoutine(holdBeforeFade: 0.55f, fadeDuration: 0.45f));
            }
            else if (koSprites != null && koSprites.Length > 0)
            {
                StartCoroutine(KOSpriteFallbackAndHideRoutine());
            }
            else
            {
                if (_sr != null) _sr.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                StartCoroutine(KOFallRoutine(0.55f, -90f));
            }
        }

        /// <summary>Chiến thắng: phát animation Victory (xoay lốp ăn mừng).</summary>
        public void OnVictory()
        {
            _actionTimer = 999f;
            if (_animator != null && _animator.enabled && _animator.runtimeAnimatorController != null)
            {
                // Chỉ gọi khi Controller thực sự có parameter/state "Victory" để tránh lỗi console
                AnimatorSafe.TrySetTrigger(_animator, "Victory");
                AnimatorSafe.TryPlayState(_animator, "Victory", 0, 0f);
            }
        }

        /// <summary>Đợi animation ngã xuống nền hoàn tất rồi mờ dần và ẩn nhân vật.</summary>
        private IEnumerator KOFadeAndHideRoutine(float holdBeforeFade, float fadeDuration)
        {
            float holdElapsed = 0f;
            while (holdElapsed < holdBeforeFade)
            {
                holdElapsed += Mathf.Min(Time.deltaTime, 0.05f);
                yield return null;
            }

            if (_sr != null)
            {
                Color baseColor = _sr.color;
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Mathf.Min(Time.deltaTime, 0.05f);
                    float alpha = Mathf.Lerp(baseColor.a, 0f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeDuration)));
                    _sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                    yield return null;
                }

                _sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
                _sr.enabled = false;
            }
        }

        private IEnumerator KOSpriteFallbackAndHideRoutine()
        {
            if (_sr != null && koSprites != null)
            {
                _sr.enabled = true;
                _sr.color = Color.white;
                for (int i = 0; i < koSprites.Length; i++)
                {
                    if (koSprites[i] != null) _sr.sprite = koSprites[i];
                    yield return new WaitForSeconds(0.125f);
                }
            }

            yield return KOFadeAndHideRoutine(holdBeforeFade: 0.15f, fadeDuration: 0.45f);
        }

        /// <summary>Dự phòng ngã gục (KO) nếu controller chưa có state KO.</summary>
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

            yield return KOFadeAndHideRoutine(holdBeforeFade: 0.15f, fadeDuration: 0.4f);
        }

    }
}
