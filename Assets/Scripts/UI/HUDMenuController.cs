using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using ChiecLopCuoi.Audio;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Điều khiển riêng biệt:
    /// 1. Menu Tạm Dừng (HUD_Menu / Pause Menu):
    ///    - Khi bấm Nút Pause (Hình 2 - ||) hoặc phím ESC: Dừng trận đấu (Time.timeScale = 0) và mở menu gỗ.
    ///    - Menu gồm: Play (tiếp tục), Restart (chơi lại), Hướng Dẫn (mở bảng F12), Quit (về Login).
    ///    - Hoạt động trơn tru, có cơ chế debounce chống double-click, tuyệt đối không bị tắt sau 1s.
    /// 2. Bảng Hướng Dẫn Chơi Game (Tutorial_Guide_Panel / F12):
    ///    - Kích hoạt riêng bằng phím F12 (hoặc bấm nút "Hướng Dẫn" trong Pause Menu).
    ///    - Thiết kế sang trọng, thoáng đãng, phân tách 2 cột rõ ràng, không đè chữ, không có ô vuông lỗi font.
    /// </summary>
    [DisallowMultipleComponent]
    public partial class HUDMenuController : MonoBehaviour
    {
        private static HUDMenuController _instance;
        public static HUDMenuController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<HUDMenuController>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Menu Pause References (HUD_Menu)")]
        [Tooltip("Root GameObject của menu Pause (HUD_Menu)")]
        public GameObject menuPanel;

        [Header("Nút Pause trên HUD (Hình 2)")]
        [Tooltip("Nút Pause trên màn hình chơi (Btn_Pause)")]
        public Button pauseButton;

        [Header("Các nút trong HUD_Menu")]
        [Tooltip("Nút Play - Tiếp tục chơi")]
        public Button playButton;

        [Tooltip("Nút Restart - Chơi lại màn hiện tại")]
        public Button restartButton;

        [Tooltip("Nút Hướng Dẫn / Menu - Mở Bảng Hướng Dẫn F12")]
        public Button guideButton;

        [Tooltip("Nút Trang chủ - Quay về màn hình Trang chủ (LoginScene)")]
        public Button homeButton;

        [Tooltip("Nút Quit - Về trang Login")]
        public Button quitButton;

        [Tooltip("Nút X đóng trên góc bảng gỗ HUD_Menu")]
        public Button menuCloseButton;

        [Header("Thanh Âm Lượng (Sliders)")]
        public Slider musicSlider;
        public Slider soundSlider;
        private Image _musicIconImage;
        private Image _soundIconImage;

        [Header("Bảng Hướng Dẫn Chơi Game (F12)")]
        [Tooltip("Root GameObject của Bảng Hướng Dẫn chơi game")]
        public GameObject guidePanel;

        [Tooltip("Nút đóng góc trên Bảng Hướng Dẫn")]
        public Button guideCloseButton;

        [Tooltip("Nút Đã hiểu / Đóng dưới chân Bảng Hướng Dẫn")]
        public Button guideGotItButton;

        [Header("Sprites from MenuPause & Btn_map")]
        public Sprite spriteMenuBoard;
        public Sprite spriteMenuRibbon;
        public Sprite spriteMenuClose;
        public Sprite spriteMenuGuide;
        public Sprite spriteBtnHome;
        public Sprite spriteHuongDanBoard;
        public Sprite spriteBtnMap;

        [Header("Audio SFX")]
        [Tooltip("Âm thanh click nút")]
        public AudioClip clickSfxClip;
        private AudioSource audioSource;

        private float _previousTimeScale = 1f;
        private float _lastActionTime = -1f;
        private bool _wasMenuOpenBeforeGuide = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }

            InitAudio();
#if UNITY_EDITOR
            LoadSpritesIfMissing();
#endif
            AutoBindReferences();
            EnsureGuidePanelExists();
        }

        private void Start()
        {
            Time.timeScale = 1f;

            // Ban đầu ẩn cả menu pause và guide panel
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
            }
            if (guidePanel != null)
            {
                guidePanel.SetActive(false);
            }

            RegisterButtonEvents();
        }

        private void Update()
        {
            // Phím F12: Ẩn / Hiện riêng biệt Bảng Hướng Dẫn chơi game (không liên quan đến Pause)
            if (Input.GetKeyDown(KeyCode.F12))
            {
                ToggleGuidePanel();
            }

            // Phím Escape: Đóng Bảng Hướng Dẫn nếu đang mở, hoặc Bật / Tắt Menu Pause
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (guidePanel != null && guidePanel.activeSelf)
                {
                    CloseGuidePanel();
                }
                else if (menuPanel != null && menuPanel.activeSelf)
                {
                    OnPlayClicked();
                }
                else
                {
                    OpenPauseMenu();
                }
            }
        }

        private void OnEnable()
        {
            AudioManager.OnBgmVolumeChanged += HandleBgmVolumeChanged;
            AudioManager.OnSfxVolumeChanged += HandleSfxVolumeChanged;
            SyncAudioControlsFromManager();
        }

        private void OnDisable()
        {
            AudioManager.OnBgmVolumeChanged -= HandleBgmVolumeChanged;
            AudioManager.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        private void InitAudio()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.ignoreListenerPause = true;

            if (clickSfxClip == null)
            {
                clickSfxClip = Resources.Load<AudioClip>("Audio/UI/ui_click");
#if UNITY_EDITOR
                if (clickSfxClip == null)
                {
                    clickSfxClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI/ui_click.wav");
                }
#endif
            }
        }

        private void RegisterButtonEvents()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(OnPauseClicked);
                pauseButton.onClick.AddListener(OnPauseClicked);
                var img = pauseButton.GetComponent<Image>();
                if (img != null) img.raycastTarget = true;
                pauseButton.interactable = true;
            }

            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayClicked);
                playButton.onClick.AddListener(OnPlayClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (guideButton != null)
            {
                guideButton.onClick.RemoveListener(OpenGuidePanel);
                guideButton.onClick.AddListener(OpenGuidePanel);
            }

            if (homeButton != null)
            {
                homeButton.onClick.RemoveListener(OnHomeClicked);
                homeButton.onClick.AddListener(OnHomeClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnQuitClicked);
                quitButton.onClick.AddListener(OnQuitClicked);
            }

            if (menuCloseButton != null)
            {
                menuCloseButton.onClick.RemoveListener(OnPlayClicked);
                menuCloseButton.onClick.AddListener(OnPlayClicked);
            }

            if (guideCloseButton != null)
            {
                guideCloseButton.onClick.RemoveListener(CloseGuidePanel);
                guideCloseButton.onClick.AddListener(CloseGuidePanel);
            }

            if (guideGotItButton != null)
            {
                guideGotItButton.onClick.RemoveListener(CloseGuidePanel);
                guideGotItButton.onClick.AddListener(CloseGuidePanel);
            }

            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveAllListeners();
                musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (soundSlider != null)
            {
                soundSlider.onValueChanged.RemoveAllListeners();
                soundSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
            }

            SyncAudioControlsFromManager();
        }

        private void SyncAudioControlsFromManager()
        {
            if (AudioManager.Instance == null) return;
            HandleBgmVolumeChanged(AudioManager.Instance.bgmVolume);
            HandleSfxVolumeChanged(AudioManager.Instance.sfxVolume);
        }

        private void HandleBgmVolumeChanged(float value)
        {
            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(value);
            }
            AudioManager.UpdateMuteIconVisual(_musicIconImage, value <= 0.001f);
        }

        private void HandleSfxVolumeChanged(float value)
        {
            if (soundSlider != null)
            {
                soundSlider.SetValueWithoutNotify(value);
            }
            AudioManager.UpdateMuteIconVisual(_soundIconImage, value <= 0.001f);
            if (audioSource != null)
            {
                bool muted = value <= 0.001f;
                audioSource.mute = muted;
                audioSource.volume = muted ? 0f : Mathf.Clamp01(value);
            }
        }

        public void OnMusicVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBGMVolume(value);
            }
        }

        public void OnSoundVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value);
            }
        }

        // =========================================================================
        // XỬ LÝ MENU PAUSE (TRƠN TRU, CHỐNG DOUBLE-CLICK, KHÔNG BỊ TẮT SAU 1S)
        // =========================================================================

        /// <summary>
        /// Nhấn nút Pause (Hình 2): Tạm dừng trò chơi và mở Menu HUD_Menu
        /// </summary>
        public void OnPauseClicked()
        {
            // Chống double-click
            if (Time.unscaledTime - _lastActionTime < 0.25f) return;
            _lastActionTime = Time.unscaledTime;

            OpenPauseMenu();
        }

        public void OpenPauseMenu()
        {
            PlayClickSound();

            // Nếu đang mở Bảng Hướng Dẫn thì đóng lại để tránh đè nhau
            if (guidePanel != null && guidePanel.activeSelf)
            {
                guidePanel.SetActive(false);
            }

            if (menuPanel == null)
            {
                AutoBindReferences();
            }

            if (menuPanel != null)
            {
                EnsurePauseSlidersMatchResultPanel();
                SyncAudioControlsFromManager();

                var menuImg = menuPanel.GetComponent<Image>();
                if (menuImg == null)
                {
                    menuImg = menuPanel.AddComponent<Image>();
                }
                menuImg.color = new Color(0f, 0f, 0f, 0.65f);
                menuImg.raycastTarget = true;

                menuPanel.transform.SetAsLastSibling();
                var rt = menuPanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = Vector2.zero;
                    rt.localScale = Vector3.one;
                }
                menuPanel.SetActive(true);
            }

            Time.timeScale = 0f;
            Debug.Log("<color=yellow>[HUDMenuController]</color> Game PAUSED. Đã mở HUD_Menu.");
        }

        /// <summary>
        /// Nhấn nút Play (btn_menu_3): Tiếp tục chơi game
        /// </summary>
        public void OnPlayClicked()
        {
            if (Time.unscaledTime - _lastActionTime < 0.25f) return;
            _lastActionTime = Time.unscaledTime;

            PlayClickSound();

            Time.timeScale = 1f;

            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
            }
            if (guidePanel != null)
            {
                guidePanel.SetActive(false);
            }

            Debug.Log("<color=green>[HUDMenuController]</color> Game RESUMED. Tiếp tục chơi.");
        }

        /// <summary>
        /// Nhấn nút Restart (btn_menu_4): Chơi lại màn hiện tại
        /// </summary>
        public void OnRestartClicked()
        {
            if (Time.unscaledTime - _lastActionTime < 0.25f) return;
            _lastActionTime = Time.unscaledTime;

            PlayClickSound();

            Time.timeScale = 1f;
            Debug.Log("<color=cyan>[HUDMenuController]</color> RESTART. Tải lại màn chơi.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Nhấn nút Trang chủ (Btn_Home): Quay về màn hình Trang chủ (LoginScene)
        /// </summary>
        public void OnHomeClicked()
        {
            ReturnToHomeScene("HOME");
        }

        /// <summary>
        /// Nhấn nút Quit (btn_menu_6): Thoát về trang Login
        /// </summary>
        public void OnQuitClicked()
        {
            ReturnToHomeScene("QUIT");
        }

        private void ReturnToHomeScene(string source)
        {
            if (Time.unscaledTime - _lastActionTime < 0.25f) return;
            _lastActionTime = Time.unscaledTime;

            PlayClickSound();

            Time.timeScale = 1f;
            if (menuPanel != null) menuPanel.SetActive(false);
            if (guidePanel != null) guidePanel.SetActive(false);
            Debug.Log($"<color=orange>[HUDMenuController]</color> {source}. Quay về Trang chủ (LoginScene).");

            if (Application.CanStreamedLevelBeLoaded("LoginScene"))
            {
                SceneManager.LoadScene("LoginScene");
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }

        public void PlayClickSound()
        {
            if (AudioManager.Instance != null && AudioManager.Instance.IsSfxMuted) return;

            if (audioSource != null && clickSfxClip != null)
            {
                float vol = AudioManager.Instance != null ? AudioManager.Instance.sfxVolume : 1f;
                audioSource.mute = false;
                audioSource.volume = vol;
                audioSource.PlayOneShot(clickSfxClip, 1f);
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayClick();
            }
        }
    }
}
