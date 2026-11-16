#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.Database;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Quản lý các tương tác trên màn hình Login / Start Screen:
    /// - Nút Play chính (mở Sổ tay tiểu sử lần đầu hoặc sang màn Chọn Level)
    /// - Nút Thoát "X" (btn_components_4) góc trên phải kèm xác nhận thoát sạch
    /// - Bật nhạc nền Login 48kHz chất lượng cao
    /// </summary>
    public class LoginScreenManager : MonoBehaviour
    {
        [Header("Target Scene")]
        [SerializeField] private string gameplaySceneName = "LevelSelectScene";

        [Header("UI References")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private StoryBookManager storyBookManager;
        [SerializeField] private GameObject loreCanvasOrPanel;

        [Header("Quit Modal Assets (HUD_Menu)")]
        [SerializeField] private Sprite pauseBannerSprite;
        [SerializeField] private Sprite pauseBoardSprite;
        [SerializeField] private Sprite decoTiresCornerSprite;
        [SerializeField] private Sprite decoTireFlatSprite;
        [SerializeField] private Sprite confirmCheckSprite;
        [SerializeField] private Sprite cancelCrossSprite;

        private GameObject _quitConfirmModal;

        private void Awake()
        {
            if (storyBookManager == null)
            {
                storyBookManager = FindFirstObjectByType<StoryBookManager>(FindObjectsInactive.Include);
            }

            if (playButton == null)
            {
                var found = transform.Find("Btn_Play");
                if (found != null) playButton = found.GetComponent<Button>();
                if (playButton == null) playButton = GetComponentInChildren<Button>();
            }

            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayClicked);
                playButton.onClick.AddListener(OnPlayClicked);
            }

            if (exitButton == null)
            {
                var foundExit = transform.Find("Btn_Exit");
                if (foundExit != null) exitButton = foundExit.GetComponent<Button>();
            }
            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitClicked);
                exitButton.onClick.AddListener(OnExitClicked);
            }

            AudioSettingsModal.AttachQuickAudioButton(
                transform,
                "Btn_Audio_Settings",
                new Vector2(1f, 1f),
                new Vector2(-128f, -46f),
                new Vector2(68f, 68f));

            // Nếu modal thoát đã được bake sẵn trong Scene (để chỉnh trong Inspector)
            // thì dùng lại và gắn sự kiện cho 2 nút, tránh tạo bản sao lúc chạy.
            if (_quitConfirmModal == null)
            {
                var existingModal = transform.Find("Quit_Confirm_Modal");
                if (existingModal != null)
                {
                    _quitConfirmModal = existingModal.gameObject;
                    BindQuitModalButtons();
                }
            }
        }

        /// <summary>Gắn sự kiện cho 2 nút trong modal thoát (dùng cho modal bake sẵn trong Scene).</summary>
        private void BindQuitModalButtons()
        {
            if (_quitConfirmModal == null) return;

            Transform msgTf = _quitConfirmModal.transform.Find("Pause_Board_Stone/Message");
            if (msgTf != null)
            {
                var msgTmp = msgTf.GetComponent<TMP_Text>();
                if (msgTmp != null)
                {
                    ThemeFontManager.ApplyLoginFont(msgTmp);
                }
            }

            Transform confirmTf = _quitConfirmModal.transform.Find("Pause_Board_Stone/Btn_Confirm_Quit");
            if (confirmTf != null)
            {
                var confirmBtn = confirmTf.GetComponent<Button>();
                if (confirmBtn != null)
                {
                    confirmBtn.onClick.RemoveListener(ConfirmQuitGame);
                    confirmBtn.onClick.AddListener(ConfirmQuitGame);
                }
            }

            Transform cancelTf = _quitConfirmModal.transform.Find("Pause_Board_Stone/Btn_Cancel_Quit");
            if (cancelTf != null)
            {
                var cancelBtn = cancelTf.GetComponent<Button>();
                if (cancelBtn != null)
                {
                    cancelBtn.onClick.RemoveListener(CancelQuitGame);
                    cancelBtn.onClick.AddListener(CancelQuitGame);
                }
            }
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);
        }

        private void Start()
        {
            Time.timeScale = 1f;

            // Modal thoát được bake sẵn trong Scene để chỉnh trong Inspector,
            // nhưng phải ẩn khi bắt đầu chơi (chỉ mở khi bấm nút X).
            if (_quitConfirmModal != null)
            {
                _quitConfirmModal.SetActive(false);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLoginBGM();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && _quitConfirmModal != null && _quitConfirmModal.activeSelf)
            {
                _quitConfirmModal.SetActive(false);
            }
        }

        public void OnExitClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            ShowQuitConfirmModal();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void WebGLExitFullscreen();
#endif

        public void ConfirmQuitGame()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            Debug.Log("[LoginScreenManager] Thoát game theo yêu cầu người chơi.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
            if (_quitConfirmModal != null)
            {
                _quitConfirmModal.SetActive(false);
            }
            Screen.fullScreen = false;
            WebGLExitFullscreen();
#else
            Application.Quit();
#endif
        }

        private void CancelQuitGame()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            if (_quitConfirmModal != null)
            {
                _quitConfirmModal.SetActive(false);
            }
        }

        private void ShowQuitConfirmModal()
        {
            if (_quitConfirmModal == null)
            {
                Transform existing = transform.Find("Quit_Confirm_Modal");
                _quitConfirmModal = existing != null ? existing.gameObject : BuildQuitConfirmModal();
            }
            if (_quitConfirmModal != null)
            {
                // Modal bake sẵn trong Scene không có listener tuần tự hoá -> gắn lại mỗi lần mở.
                BindQuitModalButtons();
                _quitConfirmModal.transform.SetAsLastSibling();
                _quitConfirmModal.SetActive(true);
            }
        }

        private void EnsureQuitModalSpritesLoaded()
        {
#if UNITY_EDITOR
            if (pauseBannerSprite == null)
                pauseBannerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MenuPause/Sprites/Pause_Banner_Title.png");
            if (pauseBoardSprite == null)
                pauseBoardSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MenuPause/Sprites/Pause_Board_Stone.png");
            if (decoTiresCornerSprite == null)
                decoTiresCornerSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MenuPause/Sprites/Deco_Tires_Corner.png");
            if (decoTireFlatSprite == null)
                decoTireFlatSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MenuPause/Sprites/Deco_Tire_Flat.png");

            if (confirmCheckSprite == null || cancelCrossSprite == null)
            {
                Object[] subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Btn_map/btn_components.png");
                foreach (Object asset in subAssets)
                {
                    if (asset is Sprite sp)
                    {
                        if (confirmCheckSprite == null && sp.name == "btn_components_5")
                            confirmCheckSprite = sp;
                        else if (cancelCrossSprite == null && sp.name == "btn_components_4")
                            cancelCrossSprite = sp;
                    }
                }
            }
#endif
        }

        private GameObject BuildQuitConfirmModal()
        {
            EnsureQuitModalSpritesLoaded();

            // Lớp phủ tối toàn màn hình giống HUD_Menu
            var root = new GameObject("Quit_Confirm_Modal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(transform, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.80f);

            // Banner tiêu đề phía trên khung đá giống HUD_Menu
            CreateSpriteLayer(root.transform, "Pause_Banner_Title", pauseBannerSprite,
                new Vector2(0f, 320f), new Vector2(490f, 178.839f), preserveAspect: false, raycastTarget: false);

            // Khung đá chính HUD_Menu
            GameObject board = CreateSpriteLayer(root.transform, "Pause_Board_Stone", pauseBoardSprite,
                new Vector2(0f, 0f), new Vector2(540f, 406f), preserveAspect: false, raycastTarget: true);
            if (pauseBoardSprite == null)
            {
                board.GetComponent<Image>().color = new Color(0.58f, 0.56f, 0.48f, 1f);
            }

            // Câu hỏi xác nhận trên bảng đá
            var msgGo = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            msgGo.transform.SetParent(board.transform, false);
            var msgRt = msgGo.GetComponent<RectTransform>();
            msgRt.anchorMin = new Vector2(0.5f, 0.5f);
            msgRt.anchorMax = new Vector2(0.5f, 0.5f);
            msgRt.anchoredPosition = new Vector2(0f, 44f);
            msgRt.sizeDelta = new Vector2(370f, 110f);

            var msgTmp = msgGo.GetComponent<TextMeshProUGUI>();
            msgTmp.text = "Bạn có muốn out không?";
            msgTmp.fontSize = 34f;
            msgTmp.fontStyle = FontStyles.Bold;
            msgTmp.alignment = TextAlignmentOptions.Center;
            msgTmp.textWrappingMode = TextWrappingModes.Normal;
            msgTmp.color = new Color(0.16f, 0.08f, 0.03f, 1f);
            TMP_FontAsset loginFont = ThemeFontManager.GetLoginFont() ?? ThemeFontManager.GetStoryFont();
            if (loginFont != null) msgTmp.font = loginFont;

            // Nút tích "V" (btn_components_5) xác nhận out và nút "X" (btn_components_4) hủy thoát
            CreateIconModalButton(board.transform, "Btn_Confirm_Quit", confirmCheckSprite,
                new Vector2(-95f, -62f), new Vector2(106f, 102f), ConfirmQuitGame);

            CreateIconModalButton(board.transform, "Btn_Cancel_Quit", cancelCrossSprite,
                new Vector2(95f, -62f), new Vector2(106f, 102f), CancelQuitGame);

            // Hai cụm lốp trang trí hai góc dưới giống HUD_Menu
            CreateSpriteLayer(root.transform, "Deco_Tires_Corner", decoTiresCornerSprite,
                new Vector2(-248f, -155f), new Vector2(165f, 170f), preserveAspect: false, raycastTarget: false);
            CreateSpriteLayer(root.transform, "Deco_Tire_Flat", decoTireFlatSprite,
                new Vector2(226f, -172f), new Vector2(130f, 142f), preserveAspect: false, raycastTarget: false);

            return root;
        }

        private static GameObject CreateSpriteLayer(
            Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size, bool preserveAspect, bool raycastTarget)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = preserveAspect;
            img.raycastTarget = raycastTarget;
            img.color = sprite != null ? Color.white : new Color(0f, 0f, 0f, 0f);
            return go;
        }

        private static void CreateIconModalButton(
            Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = Color.white;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 0.96f, 0.85f, 1f);
            cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(onClick);
        }

        public void OnPlayClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayClick();
            }

            Time.timeScale = 1f;

            // Lần bấm PLAY đầu tiên trong phiên: mở Tiểu sử trước rồi mới sang Chọn Màn
            if (!StoryBookManager.HasReadLoreThisSession)
            {
                if (storyBookManager == null)
                {
                    storyBookManager = FindFirstObjectByType<StoryBookManager>(FindObjectsInactive.Include);
                }

                if (storyBookManager != null)
                {
                    Debug.Log("[LoginScreenManager] First PLAY in session -> Open StoryBookManager -> LevelSelectScene.");
                    storyBookManager.loadSceneOnComplete = true;
                    storyBookManager.targetSceneName = "LevelSelectScene";
                    storyBookManager.OpenBook();
                    return;
                }
            }

            string target = string.IsNullOrEmpty(gameplaySceneName) || gameplaySceneName == "SampleScene"
                ? "LevelSelectScene"
                : gameplaySceneName;
            Debug.Log($"[LoginScreenManager] Play Button Clicked -> {target}.");
            SceneManager.LoadScene(target);
        }
    }
}
