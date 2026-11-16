using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.Gameplay;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Quản lý giao diện và nội dung Sổ tay tiểu sử (Lore Story Book):
    /// - Cỡ chữ 26pt rõ nét dễ đọc trên trang giấy bên phải.
    /// - Hoạt động ở cả LoginScene (khi bấm nút Play) VÀ tự động mở ở đầu SampleScene nếu chạy thử trực tiếp từ SampleScene.
    /// </summary>
    public class StoryBookManager : MonoBehaviour
    {
        public static bool HasReadLoreThisSession = false;

        [Header("UI References")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI storyText;
        public TextMeshProUGUI pageIndicatorText;
        public GameObject lorePanel;   // Panel chứa khung cổ thư
        public GameObject gameplayUI;  // Chứa HUD chiến trường (nếu cùng scene)
        public Button prevButton;
        public Button nextOrSkipButton;
        public Button skipButton;

        [Header("Scene Transition Settings")]
        public bool loadSceneOnComplete = true;
        public string targetSceneName = "LevelSelectScene";
        public bool autoOpenOnStartInGameplayScene = false;

        [Header("Typography Settings")]
        public float storyFontSize = 28f;

        [Header("Story Content")]
        [TextArea(4, 10)]
        [SerializeField] private string[] pages = {
            // Hồi 1
            "<b><size=34><color=#7A1F10>HỒI 1: KIẾP TRƯỚC — SỰ PHẢN BỘI</color></size></b>\n\n" +
            "Cow Lốp từng là một chàng trai bình thường, hết lòng yêu thương người con gái của mình và tin tưởng tuyệt đối vào người bạn thân nhất.\n\n" +
            "Rồi một ngày, anh bàng hoàng phát hiện sự thật phũ phàng: chính người yêu và người bạn thân ấy đã phản bội anh sau lưng. Không oán hận, không trả thù, anh lặng lẽ rời đi — để rồi gục ngã trong cô đơn và tuyệt vọng giữa đêm mưa tầm tã.",

            // Hồi 2
            "<b><size=34><color=#7A1F10>HỒI 2: TRỌNG SINH — TRỞ VỀ QUÁ KHỨ</color></size></b>\n\n" +
            "Khi mở mắt ra, Cow Lốp nhận ra mình đã trở về căn phòng trọ tồi tàn của 10 năm trước. Mọi thứ vẫn chưa mất hết.\n\n" +
            "Một thực thể bí ẩn tự xưng là <b>\"Hệ Thống\"</b> xuất hiện, ban cho anh cơ hội thứ hai để \"sửa chữa sai lầm\". Nhưng nó chẳng hề tốt bụng — nó ép anh làm những nhiệm vụ kỳ quặc để thao túng hành vi, hòng lộ rõ bản chất là hiện thân của những tổn thương quá khứ đang tìm cách chiếm đoạt linh hồn và thay thế anh.",

            // Hồi 3
            "<b><size=34><color=#7A1F10>HỒI 3: TRẬN CHIẾN SINH TỬ</color></size></b>\n\n" +
            "Để giải thoát bản thân khỏi vòng lặp bi kịch, Cow Lốp buộc phải đối mặt với các <b>\"Quỷ Nội Tâm\"</b>: <i>Quỷ Ghen Tuông, Quỷ Đau Khổ, Quỷ Tự Ti</i> và <i>Quỷ Giận Dữ</i>.\n\n" +
            "Đỉnh điểm là trận chiến sinh tử với đối thủ <b>Nữ áo đỏ</b> — hiện thân của sự phản bội và nỗi đau năm xưa — để định đoạt xem ai sẽ phá huỷ được linh hồn (bức tượng) của đối phương trước, chấm dứt mọi đau thương."
        };

        private int currentPage = 0;

        private void Awake()
        {
            InitializeBookBindings();
        }

        private void InitializeBookBindings()
        {
            if (lorePanel == null)
            {
                var lp = transform.Find("LorePanel");
                if (lp != null) lorePanel = lp.gameObject;
            }

            if (storyText == null && lorePanel != null)
            {
                var st = lorePanel.transform.Find("BG_Notebook/Text_StoryContent");
                if (st != null) storyText = st.GetComponent<TextMeshProUGUI>();
                if (storyText == null) storyText = lorePanel.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (titleText == null && lorePanel != null)
            {
                var tt = lorePanel.transform.Find("BG_Notebook/Txt_LoreTitle");
                if (tt != null) titleText = tt.GetComponent<TextMeshProUGUI>();
            }

            if (pageIndicatorText == null && lorePanel != null)
            {
                var pt = lorePanel.transform.Find("BG_Notebook/Txt_PageIndicator");
                if (pt != null) pageIndicatorText = pt.GetComponent<TextMeshProUGUI>();
            }

            if (storyText != null && storyFontSize >= 20f)
            {
                ThemeFontManager.ApplyStoryFont(storyText);
                storyText.fontSize = storyFontSize;
                storyText.lineSpacing = 14f;
                storyText.richText = true;
            }

            if (prevButton == null && lorePanel != null)
            {
                var pTf = lorePanel.transform.Find("BG_Notebook/Btn_PrevPage");
                if (pTf != null) prevButton = pTf.GetComponent<Button>();
            }

            if (nextOrSkipButton == null && lorePanel != null)
            {
                var btnTf = lorePanel.transform.Find("BG_Notebook/Btn_NextPage")
                    ?? lorePanel.transform.Find("BG_Notebook/Btn_Skip_NextPage");
                if (btnTf != null) nextOrSkipButton = btnTf.GetComponent<Button>();
                if (nextOrSkipButton == null) nextOrSkipButton = lorePanel.GetComponentInChildren<Button>(true);
            }

            if (skipButton == null && lorePanel != null)
            {
                var sTf = lorePanel.transform.Find("Btn_SkipAll")
                    ?? lorePanel.transform.Find("BG_Notebook/Btn_SkipAll");
                if (sTf != null) skipButton = sTf.GetComponent<Button>();
            }

            if (prevButton != null && prevButton.onClick.GetPersistentEventCount() == 0)
            {
                prevButton.onClick.RemoveListener(OnPrevClicked);
                prevButton.onClick.AddListener(OnPrevClicked);
            }

            if (nextOrSkipButton != null && nextOrSkipButton.onClick.GetPersistentEventCount() == 0)
            {
                nextOrSkipButton.onClick.RemoveListener(OnSkipOrNextClicked);
                nextOrSkipButton.onClick.AddListener(OnSkipOrNextClicked);
            }

            if (skipButton != null && skipButton.onClick.GetPersistentEventCount() == 0)
            {
                skipButton.onClick.RemoveListener(OnSkipAllClicked);
                skipButton.onClick.AddListener(OnSkipAllClicked);
            }
        }

        private void Start()
        {
            string activeScene = SceneManager.GetActiveScene().name;
            if (activeScene == "LoginScene")
            {
                // Ở màn Login: không reset HasReadLoreThisSession để các lần PLAY sau trong phiên vào thẳng Chọn màn
                loadSceneOnComplete = true;
                targetSceneName = "LevelSelectScene";
                if (lorePanel != null) lorePanel.SetActive(false);
            }
            else if (activeScene == "LevelSelectScene")
            {
                loadSceneOnComplete = false;
                targetSceneName = string.Empty;
                if (lorePanel != null) lorePanel.SetActive(false);
            }
            else
            {
                loadSceneOnComplete = false;
                if (!HasReadLoreThisSession && autoOpenOnStartInGameplayScene)
                {
                    OpenBook();
                }
                else
                {
                    if (lorePanel != null) lorePanel.SetActive(false);
                    if (gameplayUI != null) gameplayUI.SetActive(true);
                }
            }
        }

        private void Update()
        {
            if (lorePanel != null && lorePanel.activeInHierarchy)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.RightArrow))
                {
                    OnSkipOrNextClicked();
                }
                else if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    OnPrevClicked();
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    OnSkipAllClicked();
                }
            }
        }

        public void OpenBook()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            InitializeBookBindings();
            currentPage = 0;
            if (storyText != null && storyFontSize >= 20f)
            {
                storyText.fontSize = storyFontSize;
            }
            ShowPage(0);

            if (lorePanel != null)
            {
                lorePanel.SetActive(true);
                Canvas.ForceUpdateCanvases();
                if (lorePanel.transform is RectTransform rt)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                }
            }

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.isTimerPaused = true;
            }
        }

        public void OnPrevClicked()
        {
            if (currentPage <= 0) return;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayClick();
            }

            currentPage--;
            ShowPage(currentPage);
        }

        public void OnSkipOrNextClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayClick();
            }

            currentPage++;
            if (currentPage < pages.Length)
            {
                ShowPage(currentPage);
            }
            else
            {
                StartGameplay();
            }
        }

        public void OnSkipAllClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayClick();
            }
            StartGameplay();
        }

        private void ShowPage(int index)
        {
            if (storyText != null && pages != null && index >= 0 && index < pages.Length)
            {
                ThemeFontManager.ApplyStoryFont(storyText);
                storyText.fontSize = storyFontSize;
                storyText.text = pages[index];
            }

            if (titleText != null)
            {
                ThemeFontManager.ApplyLoginFont(titleText);
            }

            if (pageIndicatorText != null && pages != null && pages.Length > 0)
            {
                ThemeFontManager.ApplyStoryFont(pageIndicatorText);
                pageIndicatorText.text = $"Trang {index + 1} / {pages.Length}";
            }

            if (prevButton != null)
            {
                bool canPrev = index > 0;
                prevButton.interactable = canPrev;
                var img = prevButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = canPrev ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.42f);
                }
            }
        }

        public void StartGameplay()
        {
            HasReadLoreThisSession = true;

            if (lorePanel != null)
            {
                lorePanel.SetActive(false);
            }

            if (gameplayUI != null)
            {
                gameplayUI.SetActive(true);
            }

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.isTimerPaused = false;
            }

            Debug.Log("[StoryBookManager] Bắt đầu vào Game!");

            if (loadSceneOnComplete && !string.IsNullOrEmpty(targetSceneName))
            {
                Debug.Log("[StoryBookManager] Chuyển cảnh sang: " + targetSceneName);
                SceneManager.LoadScene(targetSceneName);
            }
        }
    }
}
