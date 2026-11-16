using System.Collections.Generic;
using UnityEngine;
using ChiecLopCuoi.Audio;

namespace ChiecLopCuoi.Gameplay
{
    /// <summary>
    /// Phát một meme "K.O." ngẫu nhiên khi có đấu thủ gục ngã (Cow Lốp hoặc Boss Nữ).
    /// - Singleton tự khởi tạo và tồn tại xuyên Scene.
    /// - Tự nạp toàn bộ clip trong Assets/Resources/Audio/MemesKO.
    /// - Tránh phát lặp lại đúng clip vừa phát.
    ///
    /// Cách dùng: chỉ cần gọi <see cref="PlayRandom"/> tại thời điểm KO.
    /// </summary>
    [DisallowMultipleComponent]
    public class MemeKoSound : MonoBehaviour
    {
        private const string ClipResourceFolder = "Audio/MemesKO";

        public static MemeKoSound Instance { get; private set; }

        [Header("Danh sách meme KO")]
        [Tooltip("Để trống sẽ tự nạp các clip trong Assets/Resources/Audio/MemesKO.")]
        public List<AudioClip> koClips = new List<AudioClip>();

        [Header("Cấu hình")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("Dao động cao độ ngẫu nhiên (x = thấp nhất, y = cao nhất).")]
        public Vector2 pitchRange = new Vector2(0.97f, 1.03f);

        [Tooltip("Không phát lại đúng clip vừa phát.")]
        public bool avoidImmediateRepeat = true;

        [Header("Hạ nhạc nền khi phát")]
        [Tooltip("Hạ nhỏ nhạc nền trong lúc meme K.O. phát để nghe rõ, sau đó tự trả lại.")]
        public bool duckBGMWhilePlaying = true;

        [Range(0f, 1f)]
        [Tooltip("Nhạc nền còn bao nhiêu phần khi meme K.O. phát (0.18 = còn 18%).")]
        public float bgmDuckScale = 0.18f;

        private AudioSource _source;
        private static AudioClip _lastPlayedClip;
        private int _lastIndex = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance != null) return;

            var go = new GameObject("[MemeKoSound]");
            go.AddComponent<MemeKoSound>();
            DontDestroyOnLoad(go);
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

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f; // 2D
            _source.priority = 0;      // Ưu tiên cao nhất, tránh bị cắt khi nhiều nguồn audio
            HandleSfxVolumeChanged(AudioManager.Instance != null ? AudioManager.Instance.sfxVolume : 1f);

            if (koClips == null) koClips = new List<AudioClip>();
            if (koClips.Count == 0)
            {
                koClips.AddRange(Resources.LoadAll<AudioClip>(ClipResourceFolder));
            }
        }

        private void OnEnable()
        {
            AudioManager.OnSfxVolumeChanged += HandleSfxVolumeChanged;
            if (AudioManager.Instance != null)
            {
                HandleSfxVolumeChanged(AudioManager.Instance.sfxVolume);
            }
        }

        private void OnDisable()
        {
            AudioManager.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
        }

        private void HandleSfxVolumeChanged(float sfxVol)
        {
            if (_source == null) return;
            bool muted = sfxVol <= 0.001f;
            _source.mute = muted;
            _source.volume = muted ? 0f : Mathf.Clamp01(sfxVol);
        }

        /// <summary>Gọi khi có đấu thủ bị hạ gục (K.O.).</summary>
        public static void PlayRandom()
        {
            if (Instance == null) AutoInitialize();
            if (Instance != null) Instance.Play();
        }

        private void Play()
        {
            float sfxVol = AudioManager.Instance != null ? AudioManager.Instance.sfxVolume : 1f;
            if (sfxVol <= 0.001f) return;
            if (koClips == null || koClips.Count == 0) return;

            AudioClip clip = PickClip();
            if (clip == null || _source == null) return;

            HandleSfxVolumeChanged(sfxVol);
            _source.pitch = Random.Range(pitchRange.x, pitchRange.y);
            _source.PlayOneShot(clip, volume);

            // Hạ nhạc nền trong lúc meme K.O. phát để meme luôn nghe rõ.
            if (duckBGMWhilePlaying && AudioManager.Instance != null)
            {
                float clipLength = clip.loadState == AudioDataLoadState.Loaded ? clip.length : 1.5f;
                float holdSeconds = clipLength / Mathf.Max(0.1f, _source.pitch) + 0.1f;
                AudioManager.Instance.DuckBGMForMeme(holdSeconds, bgmDuckScale);
            }
        }

        private AudioClip PickClip()
        {
            int count = koClips.Count;
            int index = count == 1 ? 0 : Random.Range(0, count);

            if (avoidImmediateRepeat && count > 1)
            {
                for (int attempt = 0; attempt < 4 && koClips[index] == _lastPlayedClip; attempt++)
                {
                    index = Random.Range(0, count);
                }
                if (koClips[index] == _lastPlayedClip)
                {
                    index = (_lastIndex + 1) % count;
                }
            }

            _lastIndex = index;
            _lastPlayedClip = koClips[index];
            return _lastPlayedClip;
        }
    }
}
