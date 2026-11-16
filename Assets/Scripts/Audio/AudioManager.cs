using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChiecLopCuoi.Database;

namespace ChiecLopCuoi.Audio
{
    /// <summary>
    /// Quản lý phát âm thanh tập trung cho Game (BGM, SFX, UI).
    /// Tự động khởi tạo và tự động kích hoạt âm thanh cho mọi Scene và UI Button.
    /// Tách biệt hoàn toàn 2 kênh & 2 sự kiện:
    /// - Kênh Nhạc (BGM): bgmSource + encounterSource -> OnBgmVolumeChanged
    /// - Kênh Âm thanh nghe (SFX/Meme/UI/Kết quả): sfxSource, uiSource, resultSource, MemeDeathSound, MemeKoSound -> OnSfxVolumeChanged
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public static event Action<float> OnBgmVolumeChanged;
        public static event Action<float> OnSfxVolumeChanged;

        private const float MuteThreshold = 0.001f;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioSource encounterSource;
        private AudioSource resultSource;

        private Coroutine _bossEncounterRoutine;
        private Coroutine _memeDuckRoutine;
        private Coroutine _resultJingleRoutine;

        private float _lastNonZeroBgmVolume = 0.8f;
        private float _lastNonZeroSfxVolume = 1.0f;
        private float _activeBgmDuckFactor = 1.0f;
        private float _encounterMixFactor = 0f;

        public bool IsBgmMuted => bgmVolume <= MuteThreshold;
        public bool IsSfxMuted => sfxVolume <= MuteThreshold;

        [Header("Default BGM Clips")]
        public AudioClip bgmLoginClip;
        public AudioClip bgmBattleClip;
        [Tooltip("Nhạc dồn dập phát khi Cow Lốp chạm mặt Boss Nữ (đoạn QUYẾT CHIẾN).")]
        public AudioClip bgmBossEncounterClip;

        [Header("Default SFX Clips")]
        public AudioClip sfxRunClip;
        public AudioClip sfxJumpClip;
        public AudioClip sfxHitImpactClip;
        public AudioClip sfxWhooshClip;
        public AudioClip sfxPickupClip;
        public AudioClip sfxVictoryClip;
        public AudioClip sfxDefeatClip;
        public AudioClip sfxGateClearedClip;
        public AudioClip sfxRageClip;

        [Header("Default UI Clips")]
        public AudioClip uiBtnAttackClip;
        public AudioClip uiBtnDefendClip;
        public AudioClip uiBtnRetreatClip;
        public AudioClip uiClickClip;

        [Header("Settings")]
        [Range(0f, 1f)] public float bgmVolume = 0.8f;
        [Range(0f, 1f)] public float sfxVolume = 1.0f;
        [Range(0f, 1f)] public float uiVolume = 1.0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject go = null;
                var prefab = Resources.Load<GameObject>("AudioManager");
                if (prefab != null)
                {
                    go = Instantiate(prefab);
                }
                else
                {
                    go = new GameObject("[AudioManager]");
                    go.AddComponent<AudioManager>();
                }

                go.name = "[AudioManager]";
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            DatabaseManager.Initialize();
            bgmVolume = GameRepository.GetAudioBgmVolume();
            sfxVolume = GameRepository.GetAudioSfxVolume();
            uiVolume = sfxVolume;
            if (bgmVolume > MuteThreshold) _lastNonZeroBgmVolume = bgmVolume;
            if (sfxVolume > MuteThreshold) _lastNonZeroSfxVolume = sfxVolume;

            InitAudioSources();
            ApplyBgmVolumeToSources();
            ApplySfxVolumeToSources();
            EnsureBossEncounterClip();

#if UNITY_EDITOR
            LoadEditorClipsIfMissing();
#endif
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            // Kích hoạt ngay cho Scene đầu tiên khi game bắt đầu
            HandleSceneAudio(SceneManager.GetActiveScene());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HandleSceneAudio(scene);
        }

        private void HandleSceneAudio(Scene scene)
        {
            string sceneName = scene.name.ToLower();

            // Dừng nhạc gặp Boss (nếu còn) trước khi chuyển Scene
            StopBossEncounterMusic();
            StopResultJingle();

            // 1. Tự động phát BGM theo Scene (LevelSelect thuộc nhạc Menu/Login)
            if (sceneName.Contains("login") || sceneName.Contains("levelselect"))
            {
                PlayLoginBGM();
            }
            else if (sceneName.Contains("sample") || sceneName.Contains("game") || sceneName.Contains("battle") || sceneName.Contains("level"))
            {
                PlayBattleBGM();
            }

            // 2. Tự động gắn âm thanh click cho mọi UI Button trong Scene
            AutoHookAllButtonsInScene();
        }

        private static readonly System.Collections.Generic.HashSet<int> _hookedButtonIds = new System.Collections.Generic.HashSet<int>();

        public void AutoHookAllButtonsInScene()
        {
            var buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                int id = btn.GetInstanceID();
                if (_hookedButtonIds.Contains(id)) continue;
                _hookedButtonIds.Add(id);

                string bName = btn.gameObject.name;
                btn.onClick.AddListener(() => OnButtonSoundTrigger(bName));
            }
        }

        private void OnButtonSoundTrigger(string buttonName)
        {
            string lower = buttonName.ToLower();
            if (lower.Contains("attack"))
            {
                PlayAttackButton();
            }
            else if (lower.Contains("shield") || lower.Contains("defend"))
            {
                PlayDefendButton();
            }
            else if (lower.Contains("retreat"))
            {
                PlayRetreatButton();
            }
            else
            {
                PlayClick();
            }
        }

        private void InitAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                bgmSource.volume = bgmVolume;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.volume = sfxVolume;
            }

            if (uiSource == null)
            {
                uiSource = gameObject.AddComponent<AudioSource>();
                uiSource.loop = false;
                uiSource.playOnAwake = false;
                uiSource.volume = uiVolume;
            }

            if (encounterSource == null)
            {
                encounterSource = gameObject.AddComponent<AudioSource>();
                encounterSource.loop = true;
                encounterSource.playOnAwake = false;
                encounterSource.spatialBlend = 0f;
                encounterSource.volume = 0f;
            }

            if (resultSource == null)
            {
                resultSource = gameObject.AddComponent<AudioSource>();
                resultSource.loop = false;
                resultSource.playOnAwake = false;
                resultSource.spatialBlend = 0f;
                resultSource.volume = sfxVolume;
                resultSource.priority = 0;
            }
        }

        private void EnsureBossEncounterClip()
        {
            if (bgmBossEncounterClip != null) return;
            bgmBossEncounterClip = Resources.Load<AudioClip>("Audio/bgm_boss_encounter");
        }

#if UNITY_EDITOR
        public void LoadEditorClipsIfMissing()
        {
            var epicLogin = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/bgm_login_epic.wav");
            if (epicLogin != null) bgmLoginClip = epicLogin;
            else if (bgmLoginClip == null) bgmLoginClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/bgm_login.ogg");

            var epicBattle = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/bgm_battle_epic.wav");
            if (epicBattle != null) bgmBattleClip = epicBattle;
            else if (bgmBattleClip == null) bgmBattleClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/bgm_battle.ogg");

            if (sfxRunClip == null) sfxRunClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_run.wav");
            if (sfxJumpClip == null) sfxJumpClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_jump.wav");
            if (sfxHitImpactClip == null) sfxHitImpactClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/hit_48k.wav");
            if (sfxWhooshClip == null) sfxWhooshClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/whoosh_48k.wav");
            if (sfxPickupClip == null) sfxPickupClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_pickup.wav");
            if (sfxVictoryClip == null) sfxVictoryClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_victory_royale.wav");
            if (sfxDefeatClip == null) sfxDefeatClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_defeat_dexter.wav");
            if (sfxGateClearedClip == null) sfxGateClearedClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/sfx_victory_music.wav");

            if (uiBtnAttackClip == null) uiBtnAttackClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ui_btn_attack.wav");
            if (uiBtnDefendClip == null) uiBtnDefendClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ui_btn_defend.wav");
            if (uiBtnRetreatClip == null) uiBtnRetreatClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ui_btn_retreat.wav");
            if (uiClickClip == null) uiClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/click.wav");
        }
#endif

        #region BGM Methods
        public void PlayBGM(AudioClip clip, bool loop = true)
        {
            if (clip == null) return;
            if (bgmSource == null) InitAudioSources();
            if (bgmSource == null) return;

            bool sameClip = bgmSource.clip == clip;
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            _activeBgmDuckFactor = 1f;

            if (IsBgmMuted)
            {
                ApplyBgmVolumeToSources();
                return;
            }

            if (!sameClip || !bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
            ApplyBgmVolumeToSources();
        }

        public void PlayLoginBGM()
        {
            if (bgmLoginClip != null) PlayBGM(bgmLoginClip);
        }

        public void PlayBattleBGM()
        {
            if (bgmBattleClip != null) PlayBGM(bgmBattleClip);
        }

        /// <summary>
        /// Nhạc dồn dập khi Cow Lốp chạm mặt Boss (đoạn QUYẾT CHIẾN):
        /// hạ nhỏ nhạc nền chiến đấu, phát nhạc gặp Boss rồi tự trả lại âm lượng BGM.
        /// </summary>
        public void PlayBossEncounterMusic(float holdSeconds = 4.5f)
        {
            EnsureBossEncounterClip();
            if (bgmBossEncounterClip == null) return;

            if (encounterSource == null) InitAudioSources();
            if (encounterSource == null) return;

            if (_bossEncounterRoutine != null)
            {
                StopCoroutine(_bossEncounterRoutine);
            }
            if (_memeDuckRoutine != null)
            {
                StopCoroutine(_memeDuckRoutine);
                _memeDuckRoutine = null;
            }
            _bossEncounterRoutine = StartCoroutine(BossEncounterMusicRoutine(Mathf.Max(0.5f, holdSeconds)));
        }

        public void StopBossEncounterMusic()
        {
            if (_bossEncounterRoutine != null)
            {
                StopCoroutine(_bossEncounterRoutine);
                _bossEncounterRoutine = null;
            }

            _encounterMixFactor = 0f;
            _activeBgmDuckFactor = 1f;

            if (encounterSource != null)
            {
                encounterSource.Stop();
                encounterSource.clip = null;
            }

            ApplyBgmVolumeToSources();
        }

        /// <summary>
        /// Hạ nhạc nền tạm thời (mặc định còn 18%) để meme nghe rõ,
        /// sau đó tự trả lại âm lượng BGM. Dùng cho meme quái chết và meme K.O.
        /// </summary>
        public void DuckBGMForMeme(float duration, float duckScale = 0.18f)
        {
            if (bgmSource == null || IsBgmMuted || IsSfxMuted) return;

            // Nhạc gặp Boss đã tự hạ BGM, tránh 2 routine giành nhau.
            if (_bossEncounterRoutine != null) return;

            if (_memeDuckRoutine != null)
            {
                StopCoroutine(_memeDuckRoutine);
            }
            _memeDuckRoutine = StartCoroutine(MemeDuckRoutine(Mathf.Max(0.15f, duration), Mathf.Clamp01(duckScale)));
        }

        private IEnumerator MemeDuckRoutine(float duration, float duckScale)
        {
            const float fadeDown = 0.05f;
            const float fadeUp = 0.4f;

            float from = _activeBgmDuckFactor;
            float t = 0f;
            while (t < fadeDown)
            {
                t += Time.unscaledDeltaTime;
                _activeBgmDuckFactor = Mathf.Lerp(from, duckScale, Mathf.Clamp01(t / fadeDown));
                ApplyBgmVolumeToSources();
                yield return null;
            }
            _activeBgmDuckFactor = duckScale;
            ApplyBgmVolumeToSources();

            yield return new WaitForSecondsRealtime(duration);

            from = _activeBgmDuckFactor;
            t = 0f;
            while (t < fadeUp)
            {
                t += Time.unscaledDeltaTime;
                _activeBgmDuckFactor = Mathf.Lerp(from, 1f, Mathf.Clamp01(t / fadeUp));
                ApplyBgmVolumeToSources();
                yield return null;
            }

            _activeBgmDuckFactor = 1f;
            ApplyBgmVolumeToSources();
            _memeDuckRoutine = null;
        }

        private IEnumerator BossEncounterMusicRoutine(float holdSeconds)
        {
            encounterSource.clip = bgmBossEncounterClip;
            encounterSource.loop = true;
            _encounterMixFactor = 0f;
            if (!IsBgmMuted) encounterSource.Play();
            ApplyBgmVolumeToSources();

            const float fadeInDuration = 0.3f;
            float t = 0f;
            while (t < fadeInDuration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / fadeInDuration);
                _encounterMixFactor = Mathf.Lerp(0f, 0.95f, u);
                _activeBgmDuckFactor = Mathf.Lerp(1f, 0.08f, u);
                ApplyBgmVolumeToSources();
                yield return null;
            }

            yield return new WaitForSecondsRealtime(holdSeconds);

            const float fadeOutDuration = 1.2f;
            t = 0f;
            while (t < fadeOutDuration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / fadeOutDuration);
                _encounterMixFactor = Mathf.Lerp(0.95f, 0f, u);
                _activeBgmDuckFactor = Mathf.Lerp(0.08f, 1f, u);
                ApplyBgmVolumeToSources();
                yield return null;
            }

            _encounterMixFactor = 0f;
            _activeBgmDuckFactor = 1f;
            encounterSource.Stop();
            encounterSource.clip = null;
            ApplyBgmVolumeToSources();
            _bossEncounterRoutine = null;
        }

        public void StopBGM()
        {
            if (bgmSource != null) bgmSource.Stop();
        }
        #endregion

        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            if (bgmVolume > MuteThreshold)
            {
                _lastNonZeroBgmVolume = bgmVolume;
            }

            ApplyBgmVolumeToSources();
            GameRepository.SetAudioBgmVolume(bgmVolume);
            OnBgmVolumeChanged?.Invoke(bgmVolume);
        }

        public void ToggleBgmMute()
        {
            if (IsBgmMuted)
            {
                SetBGMVolume(_lastNonZeroBgmVolume > MuteThreshold ? _lastNonZeroBgmVolume : 0.8f);
            }
            else
            {
                SetBGMVolume(0f);
            }
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            uiVolume = sfxVolume;
            if (sfxVolume > MuteThreshold)
            {
                _lastNonZeroSfxVolume = sfxVolume;
            }

            ApplySfxVolumeToSources();
            GameRepository.SetAudioSfxVolume(sfxVolume);
            OnSfxVolumeChanged?.Invoke(sfxVolume);
        }

        public void ToggleSfxMute()
        {
            if (IsSfxMuted)
            {
                SetSFXVolume(_lastNonZeroSfxVolume > MuteThreshold ? _lastNonZeroSfxVolume : 1.0f);
            }
            else
            {
                SetSFXVolume(0f);
            }
        }

        private void ApplyBgmVolumeToSources()
        {
            bool muted = IsBgmMuted;
            if (bgmSource != null)
            {
                bgmSource.mute = muted;
                bgmSource.volume = muted ? 0f : bgmVolume * _activeBgmDuckFactor;
                if (muted)
                {
                    if (bgmSource.isPlaying) bgmSource.Pause();
                }
                else if (bgmSource.clip != null && !bgmSource.isPlaying)
                {
                    bgmSource.UnPause();
                    if (!bgmSource.isPlaying) bgmSource.Play();
                }
            }

            if (encounterSource != null)
            {
                encounterSource.mute = muted;
                encounterSource.volume = muted ? 0f : _encounterMixFactor * bgmVolume;
                if (muted)
                {
                    if (encounterSource.isPlaying) encounterSource.Pause();
                }
                else if (_bossEncounterRoutine != null && encounterSource.clip != null && !encounterSource.isPlaying)
                {
                    encounterSource.UnPause();
                    if (!encounterSource.isPlaying) encounterSource.Play();
                }
            }
        }

        private void ApplySfxVolumeToSources()
        {
            bool muted = IsSfxMuted;
            float vol = muted ? 0f : sfxVolume;

            if (sfxSource != null)
            {
                sfxSource.mute = muted;
                sfxSource.volume = vol;
            }
            if (uiSource != null)
            {
                uiSource.mute = muted;
                uiSource.volume = muted ? 0f : uiVolume;
            }
            if (resultSource != null)
            {
                resultSource.mute = muted;
                resultSource.volume = vol;
            }
        }

        #region SFX Methods
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (IsSfxMuted || clip == null || sfxSource == null) return;
            sfxSource.mute = false;
            sfxSource.volume = sfxVolume;
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        public void PlayRun() => PlaySFX(sfxRunClip, 0.6f);
        public void PlayJump() => PlaySFX(sfxJumpClip);
        public void PlayHitImpact(float volumeScale = 1.0f) => PlaySFX(sfxHitImpactClip, volumeScale);
        public void PlayWhoosh(float volumeScale = 1.0f) => PlaySFX(sfxWhooshClip, volumeScale);
        public void PlayPickup() => PlaySFX(sfxPickupClip);
        public void PlayVictory() => PlayResultJingle(sfxVictoryClip);
        public void PlayDefeat() => PlayResultJingle(sfxDefeatClip);
        public void PlayGateCleared() => PlaySFX(sfxGateClearedClip, 0.95f);
        public void PlayRageActivation() => PlaySFX(sfxVictoryClip, 0.85f);

        /// <summary>
        /// Nhạc thắng/thua ở màn kết quả: tắt dần nhạc nền chiến đấu rồi mới phát jingle
        /// để người chơi nghe rõ (BGM 0.8 đang át gần hết tiếng thắng/thua).
        /// </summary>
        private void PlayResultJingle(AudioClip clip)
        {
            if (_resultJingleRoutine != null)
            {
                StopCoroutine(_resultJingleRoutine);
                _resultJingleRoutine = null;
            }
            if (resultSource != null) resultSource.Stop();
            _resultJingleRoutine = StartCoroutine(PlayResultJingleRoutine(clip));
        }

        private IEnumerator PlayResultJingleRoutine(AudioClip clip)
        {
            // Trận đấu đã kết thúc: tắt dần BGM chiến đấu để jingle nổi rõ.
            if (bgmSource != null && bgmSource.isPlaying && !IsBgmMuted)
            {
                const float fade = 0.25f;
                float fromDuck = _activeBgmDuckFactor;
                float t = 0f;
                while (t < fade)
                {
                    t += Time.unscaledDeltaTime;
                    _activeBgmDuckFactor = Mathf.Lerp(fromDuck, 0f, Mathf.Clamp01(t / fade));
                    ApplyBgmVolumeToSources();
                    yield return null;
                }
                bgmSource.Stop();
                _activeBgmDuckFactor = 1f;
                ApplyBgmVolumeToSources();
            }

            if (clip != null && resultSource != null && !IsSfxMuted)
            {
                resultSource.Stop();
                resultSource.clip = clip;
                resultSource.mute = false;
                resultSource.volume = sfxVolume;
                resultSource.Play();
            }

            _resultJingleRoutine = null;
        }

        /// <summary>Dừng nhạc thắng/thua (dùng khi chuyển Scene để không bị rớt lại màn sau).</summary>
        public void StopResultJingle()
        {
            if (_resultJingleRoutine != null)
            {
                StopCoroutine(_resultJingleRoutine);
                _resultJingleRoutine = null;
            }
            if (resultSource != null) resultSource.Stop();
        }
        #endregion

        #region UI Methods
        public void PlayUI(AudioClip clip, float volumeScale = 1f)
        {
            if (IsSfxMuted || clip == null || uiSource == null) return;
            uiSource.mute = false;
            uiSource.volume = uiVolume;
            uiSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        public void PlayClick() => PlayUI(uiClickClip);
        public void PlayAttackButton() => PlayUI(uiBtnAttackClip);
        public void PlayDefendButton() => PlayUI(uiBtnDefendClip);
        public void PlayRetreatButton() => PlayUI(uiBtnRetreatClip);
        #endregion

        #region Shared UI Audio Helpers
        public static Image BindMuteIconButton(Transform iconTransform, UnityEngine.Events.UnityAction onClick)
        {
            if (iconTransform == null) return null;
            var img = iconTransform.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;

            var btn = iconTransform.GetComponent<Button>();
            if (btn == null) btn = iconTransform.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.targetGraphic = img;
            btn.onClick.RemoveAllListeners();
            if (onClick != null) btn.onClick.AddListener(onClick);
            return img;
        }

        public static void UpdateMuteIconVisual(Image iconImage, bool isMuted)
        {
            if (iconImage == null) return;
            iconImage.color = isMuted ? new Color(0.55f, 0.55f, 0.55f, 0.50f) : Color.white;
        }
        #endregion
    }
}
