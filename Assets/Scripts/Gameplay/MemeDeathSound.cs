using System.Collections.Generic;
using UnityEngine;
using ChiecLopCuoi.Audio;

namespace ChiecLopCuoi.Gameplay
{
    /// <summary>
    /// Phát một meme âm thanh ngẫu nhiên khi quái vật bị tiêu diệt.
    /// - Tự nạp toàn bộ clip trong Assets/Resources/Audio/Memes (không cần gắn tay
    ///   từng clip vào Scene), nên chỉ cần thả thêm file .wav vào thư mục đó.
    /// - Có thể gắn danh sách "Meme Clips" riêng cho từng quái nếu muốn ghi đè.
    /// - Tự tạo AudioSource nếu GameObject chưa có.
    /// - Mặc định phát qua một AudioSource dùng chung (2D) để âm thanh không bị cắt
    ///   khi quái vật bị SetActive(false) ngay sau khi chết.
    /// - Tránh phát lặp lại đúng clip vừa phát.
    ///
    /// Cách dùng: gắn script vào quái vật rồi gọi PlayDeathMeme() trong hàm chết.
    /// </summary>
    [DisallowMultipleComponent]
    public class MemeDeathSound : MonoBehaviour
    {
        private const string ClipResourceFolder = "Audio/Memes";

        [Header("Danh sách meme âm thanh")]
        [Tooltip("Để trống sẽ tự nạp ngẫu nhiên các clip trong Assets/Resources/Audio/Memes.")]
        public List<AudioClip> memeClips = new List<AudioClip>();

        [Header("Cấu hình")]
        [Tooltip("Âm lượng phát (Unity giới hạn 0-1). Để 1 để phát hết cỡ; các clip đã được tăng âm 1.5x trong file .wav.")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("Dao động cao độ ngẫu nhiên để mỗi lần nghe tự nhiên hơn (x = thấp nhất, y = cao nhất).")]
        public Vector2 pitchRange = new Vector2(0.96f, 1.04f);

        [Tooltip("Không phát lại đúng clip vừa phát (tránh lặp liên tiếp).")]
        public bool avoidImmediateRepeat = true;

        [Tooltip("Phát qua AudioSource dùng chung để âm thanh vẫn tiếp tục khi quái vật biến mất.")]
        public bool useSharedAudioSource = true;

        [Header("Hạ nhạc nền khi phát")]
        [Tooltip("Hạ nhỏ nhạc nền trong lúc meme phát để nghe rõ, sau đó tự trả lại.")]
        public bool duckBGMWhilePlaying = true;

        [Range(0f, 1f)]
        [Tooltip("Nhạc nền còn bao nhiêu phần khi meme phát (0.18 = còn 18%).")]
        public float bgmDuckScale = 0.18f;

        private AudioSource _localSource;
        private static AudioSource _sharedSource;
        private static AudioClip _lastPlayedClip;
        private int _lastIndex = -1;

        private void Awake()
        {
            EnsureClipsLoaded();

            if (!useSharedAudioSource)
            {
                EnsureLocalSource();
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
            ApplySfxVolume(_sharedSource, sfxVol);
            ApplySfxVolume(_localSource, sfxVol);
        }

        private static void ApplySfxVolume(AudioSource source, float sfxVol)
        {
            if (source == null) return;
            bool muted = sfxVol <= 0.001f;
            source.mute = muted;
            source.volume = muted ? 0f : Mathf.Clamp01(sfxVol);
        }

        /// <summary>Nạp bộ clip meme từ Resources nếu danh sách đang trống.</summary>
        private void EnsureClipsLoaded()
        {
            if (memeClips == null) memeClips = new List<AudioClip>();
            memeClips.RemoveAll(c => c == null);
            if (memeClips.Count > 0) return;

            memeClips.AddRange(Resources.LoadAll<AudioClip>(ClipResourceFolder));
        }

        /// <summary>Gọi khi quái vật chết.</summary>
        public void PlayDeathMeme()
        {
            float sfxVol = AudioManager.Instance != null ? AudioManager.Instance.sfxVolume : 1f;
            if (sfxVol <= 0.001f) return;

            EnsureClipsLoaded();
            if (memeClips.Count == 0) return;

            AudioClip clip = PickClip();
            if (clip == null) return;

            AudioSource source = useSharedAudioSource ? GetSharedSource() : EnsureLocalSource();
            if (source == null) return;

            ApplySfxVolume(source, sfxVol);
            float pitch = Random.Range(pitchRange.x, pitchRange.y);
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);

            // Hạ nhạc nền trong lúc meme phát để meme luôn nghe rõ.
            if (duckBGMWhilePlaying && AudioManager.Instance != null)
            {
                float clipLength = clip.loadState == AudioDataLoadState.Loaded ? clip.length : 1.5f;
                float holdSeconds = clipLength / Mathf.Max(0.1f, pitch) + 0.1f;
                AudioManager.Instance.DuckBGMForMeme(holdSeconds, bgmDuckScale);
            }
        }

        private AudioClip PickClip()
        {
            int count = memeClips.Count;
            int index = count == 1 ? 0 : Random.Range(0, count);

            if (avoidImmediateRepeat && count > 1)
            {
                // Thử tối đa vài lần để tránh cả clip vừa phát lẫn index vừa dùng.
                for (int attempt = 0; attempt < 4 && memeClips[index] == _lastPlayedClip; attempt++)
                {
                    index = Random.Range(0, count);
                }
                if (memeClips[index] == _lastPlayedClip)
                {
                    index = (_lastIndex + 1) % count;
                }
            }

            _lastIndex = index;
            _lastPlayedClip = memeClips[index];
            return _lastPlayedClip;
        }

        private AudioSource EnsureLocalSource()
        {
            if (_localSource != null) return _localSource;

            _localSource = GetComponent<AudioSource>();
            if (_localSource == null)
            {
                _localSource = gameObject.AddComponent<AudioSource>();
            }
            ConfigureSource(_localSource);
            return _localSource;
        }

        private static AudioSource GetSharedSource()
        {
            if (_sharedSource != null) return _sharedSource;

            // Tìm lại phòng trường hợp domain reload / scene change.
            GameObject existing = GameObject.Find("[MemeDeathAudio]");
            if (existing != null)
            {
                _sharedSource = existing.GetComponent<AudioSource>();
            }
            if (_sharedSource == null)
            {
                GameObject go = existing != null ? existing : new GameObject("[MemeDeathAudio]");
                _sharedSource = go.GetComponent<AudioSource>();
                if (_sharedSource == null)
                {
                    _sharedSource = go.AddComponent<AudioSource>();
                }
                ConfigureSource(_sharedSource);
                if (go.scene.IsValid())
                {
                    Object.DontDestroyOnLoad(go);
                }
            }
            return _sharedSource;
        }

        private static void ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f; // 2D: nghe rõ ở mọi vị trí
            float sfxVol = AudioManager.Instance != null ? AudioManager.Instance.sfxVolume : 1f;
            ApplySfxVolume(source, sfxVol);
            source.priority = 0;      // Ưu tiên cao nhất, tránh bị hệ thống audio cắt vì quá nhiều nguồn
        }
    }
}
