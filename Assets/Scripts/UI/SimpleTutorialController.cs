using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.Gameplay;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Điều phối cơ chế Hướng dẫn Người chơi lần đầu (3 chặng đơn giản + nút Skip):
    /// - Chặng 1 (LoginScene): Làm mờ xung quanh, zoom/nhấp nháy nút PLAY.
    /// - Chặng 2 (LevelSelectScene): Zoom 6 Màn chơi -> thanh công cụ các biểu tượng (cho phép bấm thử mở hướng dẫn/bảng) -> nhắc chọn Màn 1.
    /// - Chặng 3 (SampleScene - Màn 1): Chỉ dẫn cụm Di chuyển và cụm Chiêu thức 1 -> 6.
    /// </summary>
    public class SimpleTutorialController : MonoBehaviour
    {
        public const string TutorialDonePrefKey = "ChiecLopCuoi_TutorialDone";
        public const string TutorialStagePrefKey = "ChiecLopCuoi_TutorialStage";

        private static SimpleTutorialController _instance;

        private TutorialSpotlightOverlay _overlay;
        private Coroutine _sceneRoutine;
        private int _stepIndex;
        private bool _advanceRequested;
        private bool _pausedTimerForTutorial;

        public static bool IsTutorialDone()
            => PlayerPrefs.GetInt(TutorialDonePrefKey, 0) == 1;

        public static bool HasActiveOverlay
        {
            get
            {
                GameObject canvasGo = GameObject.Find("Tutorial_Spotlight_Canvas");
                return canvasGo != null && canvasGo.activeInHierarchy;
            }
        }

        public static void ResetTutorialProgress()
        {
            PlayerPrefs.SetInt(TutorialDonePrefKey, 0);
            PlayerPrefs.SetInt(TutorialStagePrefKey, 0);
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapOnLoad()
        {
            if (IsTutorialDone()) return;
            EnsureInstance();
            _instance.BeginForActiveScene();
        }

        private static SimpleTutorialController EnsureInstance()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("[SimpleTutorialController]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SimpleTutorialController>();
            SceneManager.sceneLoaded += _instance.OnSceneLoaded;
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _overlay = gameObject.AddComponent<TutorialSpotlightOverlay>();
            _overlay.Initialize(OnNextClicked, SkipAllTutorial);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _instance = null;
            }
            RestorePausedTimerIfNeeded();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsTutorialDone())
            {
                CleanupSelf();
                return;
            }
            BeginForActiveScene();
        }

        public void BeginForActiveScene()
        {
            if (IsTutorialDone())
            {
                CleanupSelf();
                return;
            }

            if (_sceneRoutine != null)
            {
                StopCoroutine(_sceneRoutine);
                _sceneRoutine = null;
            }

            RestorePausedTimerIfNeeded();
            if (_overlay != null) _overlay.Hide();

            string sceneName = SceneManager.GetActiveScene().name;
            int stage = PlayerPrefs.GetInt(TutorialStagePrefKey, 0);

            if (sceneName == "LoginScene" && stage <= 0)
            {
                _sceneRoutine = StartCoroutine(RunLoginStageRoutine());
            }
            else if (sceneName == "LevelSelectScene" && stage <= 1)
            {
                _sceneRoutine = StartCoroutine(RunLevelSelectStageRoutine());
            }
            else if (sceneName == "SampleScene" && stage <= 2)
            {
                _sceneRoutine = StartCoroutine(RunCombatStageRoutine());
            }
        }

        private void OnNextClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            _advanceRequested = true;
        }

        public void SkipAllTutorial()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            CompleteTutorial();
        }

        private void CompleteTutorial()
        {
            if (_sceneRoutine != null)
            {
                StopCoroutine(_sceneRoutine);
                _sceneRoutine = null;
            }
            PlayerPrefs.SetInt(TutorialDonePrefKey, 1);
            PlayerPrefs.SetInt(TutorialStagePrefKey, 3);
            PlayerPrefs.Save();

            RestorePausedTimerIfNeeded();
            CleanupSelf();
        }

        private void CleanupSelf()
        {
            if (_overlay != null) _overlay.Hide();
            if (gameObject != null) Destroy(gameObject);
        }

        private void RestorePausedTimerIfNeeded()
        {
            if (_pausedTimerForTutorial && BattleManager.Instance != null)
            {
                BattleManager.Instance.isTimerPaused = false;
            }
            _pausedTimerForTutorial = false;
        }

        // =====================================================================
        // CHẶNG 1: LOGIN SCENE (Spotlight & Zoom nút PLAY)
        // =====================================================================
        private IEnumerator RunLoginStageRoutine()
        {
            yield return null;
            yield return null;

            GameObject playObj = GameObject.Find("Btn_Play");
            RectTransform playRt = playObj != null ? playObj.GetComponent<RectTransform>() : null;
            if (playRt == null) yield break;

            bool playClicked = false;
            Button playBtn = playObj.GetComponent<Button>();
            UnityEngine.Events.UnityAction onPlay = () => playClicked = true;
            if (playBtn != null) playBtn.onClick.AddListener(onPlay);

            _overlay.ShowStep(
                playRt,
                "CHÀO MỪNG ĐẾN VỚI CHIẾC LỐP CUỐI",
                "Nhấn vào nút PLAY đang nhấp nháy để bắt đầu hành trình!",
                showNextButton: false,
                padding: 20f,
                zoomTarget: true,
                cameraFovZoomDelta: -3.5f);

            while (!playClicked && SceneManager.GetActiveScene().name == "LoginScene" && !IsTutorialDone())
            {
                bool blockingModalOpen = IsAnyLoginModalOpen();
                _overlay.SetDimmersSuppressed(blockingModalOpen);
                yield return null;
            }

            if (playBtn != null) playBtn.onClick.RemoveListener(onPlay);
            _overlay.Hide();

            if (!IsTutorialDone())
            {
                PlayerPrefs.SetInt(TutorialStagePrefKey, 1);
                PlayerPrefs.Save();
            }
        }

        // =====================================================================
        // CHẶNG 2: LEVEL SELECT SCENE (6 Màn -> Các biểu tượng -> Màn 1)
        // =====================================================================
        private IEnumerator RunLevelSelectStageRoutine()
        {
            yield return null;
            yield return null;

            // Bước 2.1: Zoom / Spotlight 6 Màn chơi
            RectTransform gridRt = FindRect("LevelCards_Grid");
            if (gridRt != null)
            {
                yield return RunWaitNextStep(
                    gridRt,
                    "MÀN CHƠI (1 -> 6)",
                    "Đây là 6 màn chiến đấu. Hoàn thành màn trước (chỉ cần từ 1 sao trở lên) để mở khóa màn tiếp theo!",
                    padding: 28f,
                    nextLabel: "Tiếp >>",
                    fovDelta: -3.0f);
            }
            if (IsTutorialDone()) yield break;

            // Bước 2.2: Spotlight thanh các biểu tượng chức năng & nhắc bấm thử
            RectTransform topBarRt = FindRect("Canvas_HUD/Top_Right_Bar")
                ?? FindRect("Canvas_HUD/Btn_Upgrade_Tires")
                ?? FindRect("Canvas_HUD/Btn_Audio_Settings");
            if (topBarRt != null)
            {
                yield return RunWaitNextStepWithModalSupport(
                    topBarRt,
                    "BIỂU TƯỢNG CHỨC NĂNG",
                    "Nhấn vào từng biểu tượng (Độ khó, Lốp, Nâng cấp, Âm thanh, Cốt truyện) để mở bảng hướng dẫn/cài đặt — hoặc bấm Tiếp!",
                    padding: 18f,
                    nextLabel: "Tiếp >>");
            }
            if (IsTutorialDone()) yield break;

            // Bước 2.3: Spotlight nút Trang chủ
            RectTransform homeRt = FindRect("Canvas_HUD/Btn_Home");
            if (homeRt != null)
            {
                yield return RunWaitNextStep(
                    homeRt,
                    "QUAY VỀ TRANG CHỦ",
                    "Biểu tượng Ngôi nhà góc trên trái giúp bạn quay lại màn hình chính bất kỳ lúc nào.",
                    padding: 16f,
                    nextLabel: "Tiếp >>");
            }
            if (IsTutorialDone()) yield break;

            // Bước 2.4: Nhắc chọn Màn 1
            RectTransform lv1Rt = FindRect("Btn_LevelCard_1");
            if (lv1Rt != null)
            {
                bool lv1Clicked = false;
                Button lv1Btn = lv1Rt.GetComponent<Button>();
                UnityEngine.Events.UnityAction onLv1 = () => lv1Clicked = true;
                if (lv1Btn != null) lv1Btn.onClick.AddListener(onLv1);

                _overlay.ShowStep(
                    lv1Rt,
                    "CHỌN MÀN 1 ĐỂ BẮT ĐẦU",
                    "Bây giờ hãy nhấn vào ô Màn 1 đang nhấp nháy để vào trận chiến đầu tiên!",
                    showNextButton: false,
                    padding: 22f,
                    zoomTarget: true,
                    cameraFovZoomDelta: -4.0f);

                while (!lv1Clicked && SceneManager.GetActiveScene().name == "LevelSelectScene" && !IsTutorialDone())
                {
                    _overlay.SetDimmersSuppressed(IsAnyLevelSelectModalOpen());
                    yield return null;
                }

                if (lv1Btn != null) lv1Btn.onClick.RemoveListener(onLv1);
                _overlay.Hide();
            }

            if (!IsTutorialDone())
            {
                PlayerPrefs.SetInt(TutorialStagePrefKey, 2);
                PlayerPrefs.Save();
            }
        }

        // =====================================================================
        // CHẶNG 3: SAMPLE SCENE / MÀN 1 (Di chuyển & Chiêu thức 1 -> 6)
        // =====================================================================
        private IEnumerator RunCombatStageRoutine()
        {
            // Chờ HUD_Canvas gắn xong, đóng Tiểu sử (nếu mở) và CowLop hoàn tất rơi nhập màn xuống đất
            float waitTimer = 0f;
            while (waitTimer < 5f)
            {
                var story = FindFirstObjectByType<StoryBookManager>(FindObjectsInactive.Include);
                bool loreBlocking = story != null && story.lorePanel != null && story.lorePanel.activeInHierarchy;
                RectTransform actionHub = FindRect("HUD_Canvas/Bottom_Right_Action_Hub");
                var player = FindFirstObjectByType<ChiecLopCuoi.Gameplay.CowLopPlayerController>();
                bool playerGrounded = player == null || player.IsGrounded;
                if (!loreBlocking && actionHub != null && playerGrounded)
                {
                    break;
                }
                waitTimer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (IsTutorialDone()) yield break;

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.isTimerPaused = true;
                _pausedTimerForTutorial = true;
            }

            // Bước 3.1: Di chuyển
            RectTransform moveRt = FindRect("HUD_Canvas/Circular_Movement_Controller");
            yield return RunWaitNextStep(
                moveRt,
                "HƯỚNG DẪN DI CHUYỂN",
                "Dùng phím A / D (hoặc ← / →) để đi, giữ Shift để Chạy nhanh, W / Space để Nhảy — hoặc kéo vòng tròn góc trái!",
                padding: 20f,
                nextLabel: "Tiếp >>");
            if (IsTutorialDone()) yield break;

            // Bước 3.2: Chiêu thức 1 -> 6
            RectTransform skillsRt = FindRect("HUD_Canvas/Bottom_Right_Action_Hub");
            yield return RunWaitNextStep(
                skillsRt,
                "CHIÊU THỨC (PHÍM 1 -> 6)",
                "1 (J): Đánh | 2 (K): Ném lốp | 3 (U): Xoay lốp | 4 (L): Đập lốp | 5 (Space): Nhảy | 6 (H): Đỡ đòn (hoặc bấm trực tiếp nút)!",
                padding: 22f,
                nextLabel: "Bắt đầu!");

            CompleteTutorial();
        }

        private IEnumerator RunWaitNextStep(
            RectTransform target, string title, string message,
            float padding, string nextLabel, float fovDelta = 0f)
        {
            _advanceRequested = false;
            _overlay.ShowStep(target, title, message, showNextButton: true, nextLabel: nextLabel, padding: padding, zoomTarget: true, cameraFovZoomDelta: fovDelta);
            while (!_advanceRequested && !IsTutorialDone())
            {
                yield return null;
            }
        }

        private IEnumerator RunWaitNextStepWithModalSupport(
            RectTransform target, string title, string message,
            float padding, string nextLabel)
        {
            _advanceRequested = false;
            _overlay.ShowStep(target, title, message, showNextButton: true, nextLabel: nextLabel, padding: padding, zoomTarget: false);
            while (!_advanceRequested && !IsTutorialDone())
            {
                _overlay.SetDimmersSuppressed(IsAnyLevelSelectModalOpen());
                yield return null;
            }
        }

        private static RectTransform FindRect(string pathOrName)
        {
            GameObject go = GameObject.Find(pathOrName);
            return go != null ? go.GetComponent<RectTransform>() : null;
        }

        private static bool IsAnyLoginModalOpen()
        {
            var story = FindFirstObjectByType<StoryBookManager>(FindObjectsInactive.Include);
            if (story != null && story.lorePanel != null && story.lorePanel.activeInHierarchy) return true;
            if (IsActiveNamedObject("Audio_Settings_Modal")) return true;
            if (IsActiveNamedObject("Quit_Confirm_Modal")) return true;
            return false;
        }

        private static bool IsAnyLevelSelectModalOpen()
        {
            var story = FindFirstObjectByType<StoryBookManager>(FindObjectsInactive.Include);
            if (story != null && story.lorePanel != null && story.lorePanel.activeInHierarchy) return true;
            if (IsActiveNamedObject("Tire_Upgrade_Modal")) return true;
            if (IsActiveNamedObject("Audio_Settings_Modal")) return true;
            return false;
        }

        private static bool IsActiveNamedObject(string name)
        {
            GameObject go = GameObject.Find(name);
            return go != null && go.activeInHierarchy;
        }
    }
}
