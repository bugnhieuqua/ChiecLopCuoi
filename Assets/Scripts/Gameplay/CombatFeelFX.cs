using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChiecLopCuoi.Environment;

namespace ChiecLopCuoi.Gameplay
{
    /// <summary>
    /// Điều phối hiệu ứng cảm giác chiến đấu tập trung (Camera Shake, Hitstop, Flash trúng đòn).
    /// Chạy theo unscaledDeltaTime và tự khôi phục Time.timeScale an toàn khi kết thúc hoặc hủy đối tượng.
    /// </summary>
    public class CombatFeelFX : MonoBehaviour
    {
        private static CombatFeelFX _instance;

        private DragCamera2D _cachedCamera;
        private float _shakeRemaining;
        private float _shakeDuration;
        private float _shakeAmplitude;
        private float _hitstopRemaining;
        private bool _hitstopActive;
        private readonly Dictionary<SpriteRenderer, Coroutine> _flashRoutines = new Dictionary<SpriteRenderer, Coroutine>();

        private static CombatFeelFX EnsureInstance()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("[CombatFeelFX]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CombatFeelFX>();
            return _instance;
        }

        public static void ShakeCamera(float amplitude, float duration)
        {
            var inst = EnsureInstance();
            inst.StartShake(amplitude, duration);
        }

        public static void TriggerHitstop(float duration)
        {
            var inst = EnsureInstance();
            inst.StartHitstop(duration);
        }

        public static void FlashSprite(SpriteRenderer sr, Color flashColor, float duration = 0.11f)
        {
            if (sr == null) return;
            var inst = EnsureInstance();
            inst.StartFlash(sr, flashColor, duration);
        }

        private void StartShake(float amplitude, float duration)
        {
            if (amplitude <= 0f || duration <= 0f) return;
            if (amplitude >= _shakeAmplitude || _shakeRemaining <= 0f)
            {
                _shakeAmplitude = amplitude;
                _shakeDuration = duration;
                _shakeRemaining = duration;
            }
        }

        private void StartHitstop(float duration)
        {
            if (duration <= 0f) return;
            // Bỏ qua nếu game đang tạm dừng bởi PauseMenu hoặc dialog kết thúc
            if (Time.timeScale <= 0.01f && !_hitstopActive) return;

            _hitstopRemaining = Mathf.Max(_hitstopRemaining, duration);
            if (!_hitstopActive)
            {
                _hitstopActive = true;
                Time.timeScale = 0.08f;
            }
        }

        private void StartFlash(SpriteRenderer sr, Color flashColor, float duration)
        {
            if (_flashRoutines.TryGetValue(sr, out Coroutine running) && running != null)
            {
                StopCoroutine(running);
            }
            _flashRoutines[sr] = StartCoroutine(FlashRoutine(sr, flashColor, duration));
        }

        private IEnumerator FlashRoutine(SpriteRenderer sr, Color flashColor, float duration)
        {
            if (sr == null) yield break;
            sr.color = flashColor;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.03f, duration));
            if (sr != null)
            {
                sr.color = Color.white;
                _flashRoutines.Remove(sr);
            }
        }

        private void Update()
        {
            if (_hitstopActive)
            {
                _hitstopRemaining -= Time.unscaledDeltaTime;
                if (_hitstopRemaining <= 0f)
                {
                    _hitstopActive = false;
                    _hitstopRemaining = 0f;
                    // Chỉ khôi phục về 1 nếu bên ngoài chưa chủ động pause (= 0)
                    if (Time.timeScale > 0.01f)
                    {
                        Time.timeScale = 1f;
                    }
                }
            }

            if (_shakeRemaining > 0f)
            {
                _shakeRemaining -= Time.unscaledDeltaTime;
                if (_cachedCamera == null) _cachedCamera = FindFirstObjectByType<DragCamera2D>();
                if (_cachedCamera != null)
                {
                    if (_shakeRemaining <= 0f)
                    {
                        _cachedCamera.SetShakeOffset(Vector2.zero);
                    }
                    else
                    {
                        float t = Mathf.Clamp01(_shakeRemaining / Mathf.Max(0.001f, _shakeDuration));
                        float mag = _shakeAmplitude * t;
                        Vector2 offset = new Vector2(Random.Range(-mag, mag), Random.Range(-mag * 0.75f, mag * 0.75f));
                        _cachedCamera.SetShakeOffset(offset);
                    }
                }
            }
        }

        private void OnDisable()
        {
            RestoreTimeScaleAndShake();
        }

        private void OnDestroy()
        {
            RestoreTimeScaleAndShake();
            if (_instance == this) _instance = null;
        }

        private void RestoreTimeScaleAndShake()
        {
            if (_hitstopActive && Time.timeScale > 0.01f)
            {
                Time.timeScale = 1f;
            }
            _hitstopActive = false;
            _hitstopRemaining = 0f;
            _shakeRemaining = 0f;
            if (_cachedCamera != null) _cachedCamera.SetShakeOffset(Vector2.zero);
        }
    }
}
