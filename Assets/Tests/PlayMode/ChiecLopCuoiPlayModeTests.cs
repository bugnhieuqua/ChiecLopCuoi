using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.Database;
using ChiecLopCuoi.Environment;
using ChiecLopCuoi.Gameplay;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.Tests.PlayMode
{
    public class ChiecLopCuoiPlayModeTests
    {
        private Keyboard _virtualKeyboard;
        private int _savedTutorialDone;
        private int _savedTutorialStage;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            _savedTutorialDone = PlayerPrefs.GetInt(SimpleTutorialController.TutorialDonePrefKey, 0);
            _savedTutorialStage = PlayerPrefs.GetInt(SimpleTutorialController.TutorialStagePrefKey, 0);
            // Mặc định tắt tutorial cho các test gameplay độc lập; test tutorial riêng sẽ Reset lại.
            PlayerPrefs.SetInt(SimpleTutorialController.TutorialDonePrefKey, 1);
            _virtualKeyboard = InputSystem.AddDevice<Keyboard>("TestVirtualKeyboard");
            _virtualKeyboard.MakeCurrent();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            PlayerPrefs.SetInt(SimpleTutorialController.TutorialDonePrefKey, _savedTutorialDone);
            PlayerPrefs.SetInt(SimpleTutorialController.TutorialStagePrefKey, _savedTutorialStage);
            PlayerPrefs.Save();
            if (_virtualKeyboard != null)
            {
                InputSystem.QueueStateEvent(_virtualKeyboard, new KeyboardState());
                InputSystem.RemoveDevice(_virtualKeyboard);
                _virtualKeyboard = null;
            }
        }

        private void PressVirtualKey(params Key[] keys)
        {
            if (_virtualKeyboard == null) return;
            _virtualKeyboard.MakeCurrent();
            InputSystem.QueueStateEvent(_virtualKeyboard, keys != null && keys.Length > 0 ? new KeyboardState(keys) : new KeyboardState());
        }

        [UnityTest]
        public IEnumerator SampleScene_LoadsBootstrapAndAtmosphereEffects()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            yield return null;

            Assert.IsNotNull(BattleManager.Instance);
            Assert.IsNotNull(HorizontalCircularController.Instance);
            Assert.IsNotNull(VoiceCommandController.Instance);
            Assert.IsFalse(VoiceCommandController.Instance.IsVoiceEnabled, "Giọng nói phải mặc định tắt khi vào màn.");

            var fxObj = GameObject.Find("Level1_Atmosphere_FX");
            Assert.IsNotNull(fxObj);
            var fxComp = fxObj.GetComponent<AtmosphereSnowFirefliesFX>();
            Assert.IsNotNull(fxComp);
            Assert.GreaterOrEqual(fxObj.transform.childCount, 80, "Tuyết rơi và đom đóm phải được khởi tạo đủ hạt.");
        }

        [UnityTest]
        public IEnumerator SampleScene_HorizontalCircularController_SyncsWithKeysDAndA()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            yield return null;

            var hc = HorizontalCircularController.Instance;
            var hero = Object.FindFirstObjectByType<CowLopPlayerController>();
            var bm = BattleManager.Instance;
            Assert.IsNotNull(hc);
            Assert.IsNotNull(hero);
            bm.HealPlayer1(150f);

            // Xác nhận CowLop rơi từ trên cao xuống nhập màn rồi mới di chuyển
            Assert.Greater(hero.transform.position.y, hero.groundY + 1f, "CowLop phải khởi đầu ở trên cao để rơi nhập màn.");
            float dropWait = 0f;
            while (!hero.IsGrounded && dropWait < 3f)
            {
                bm.HealPlayer1(150f);
                dropWait += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(hero.IsGrounded, "CowLop phải tiếp đất sau khi rơi nhập màn.");

            float xStart = hero.transform.position.x;

            // Giữ D -> Horizontal = +1, nhân vật đi sang phải
            PressVirtualKey(Key.D);
            for (int i = 0; i < 12; i++) yield return null;
            Assert.Greater(hc.Horizontal, 0.9f);
            Assert.Greater(hero.transform.position.x, xStart);

            // Nhả phím -> tự hồi về 0
            PressVirtualKey();
            for (int i = 0; i < 8; i++) yield return null;
            Assert.AreEqual(0f, hc.Horizontal, 0.01f);

            // Giữ A -> Horizontal = -1, nhân vật đi sang trái
            float xBeforeA = hero.transform.position.x;
            PressVirtualKey(Key.A);
            for (int i = 0; i < 12; i++) yield return null;
            Assert.Less(hc.Horizontal, -0.9f);
            Assert.Less(hero.transform.position.x, xBeforeA);

            PressVirtualKey();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SampleScene_Keyboard1To6_TriggerAllSixSkillsAndBlock()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            yield return null;

            var hero = Object.FindFirstObjectByType<CowLopPlayerController>();
            var bm = BattleManager.Instance;
            Assert.IsNotNull(hero);
            bm.HealPlayer1(150f);

            // Đợi CowLop rơi nhập màn chạm đất trước khi bấm chiêu
            float dropWait = 0f;
            while (!hero.IsGrounded && dropWait < 3f)
            {
                bm.HealPlayer1(150f);
                dropWait += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(hero.IsGrounded);

            // Chiêu 1 (Digit1 -> Attack)
            PressVirtualKey(Key.Digit1);
            yield return null;
            Assert.Greater(hero.AttackCooldown01, 0f);
            PressVirtualKey();
            yield return null;

            // Chiêu 2 (Digit2 -> TireThrow)
            PressVirtualKey(Key.Digit2);
            yield return null;
            Assert.Greater(hero.TireThrowCooldown01, 0f);
            PressVirtualKey();
            yield return null;

            // Chiêu 3 (Digit3 -> TireSpin)
            PressVirtualKey(Key.Digit3);
            yield return null;
            Assert.Greater(hero.TireSpinCooldown01, 0f);
            PressVirtualKey();
            yield return null;

            // Chiêu 4 (Digit4 -> JumpSlam)
            PressVirtualKey(Key.Digit4);
            yield return null;
            Assert.Greater(hero.JumpSlamCooldown01, 0f);
            PressVirtualKey();
            yield return null;

            // Đợi chạm đất rồi thử Chiêu 5 (Digit5 -> Jump)
            float waitT = 0f;
            while (!hero.IsGrounded && waitT < 2f)
            {
                bm.HealPlayer1(150f);
                waitT += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(hero.IsGrounded);
            PressVirtualKey(Key.Digit5);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsFalse(hero.IsGrounded);
            PressVirtualKey();

            // Đợi chạm đất rồi thử Chiêu 6 (Digit6 -> Block)
            waitT = 0f;
            while (!hero.IsGrounded && waitT < 2f)
            {
                bm.HealPlayer1(150f);
                waitT += Time.deltaTime;
                yield return null;
            }
            PressVirtualKey(Key.Digit6);
            for (int i = 0; i < 4; i++) yield return null;
            Assert.IsTrue(hero.IsBlocking);

            PressVirtualKey();
            for (int i = 0; i < 4; i++) yield return null;
            Assert.IsFalse(hero.IsBlocking);
        }

        [UnityTest]
        public IEnumerator LoginAndLevelSelect_SpawnAtmosphereEffects()
        {
            yield return SceneManager.LoadSceneAsync("LoginScene", LoadSceneMode.Single);
            yield return null;
            var loginSnow = GameObject.Find("Login_Snow_FX");
            Assert.IsNotNull(loginSnow);
            Assert.Greater(loginSnow.transform.childCount, 30);

            yield return SceneManager.LoadSceneAsync("LevelSelectScene", LoadSceneMode.Single);
            yield return null;
            var levelSelectFx = GameObject.Find("LevelSelect_Atmosphere_FX");
            Assert.IsNotNull(levelSelectFx);
            Assert.Greater(levelSelectFx.transform.childCount, 50);
        }

        [UnityTest]
        public IEnumerator AllSixCombatLevels_BasePlayerHpIs150_AndIncreasesOnlyWithUpgrades()
        {
            string realDbPath = Path.Combine(Application.dataPath, "Database", "game_runtime.db");
            string tempDbPath = Path.Combine(Path.GetTempPath(), $"chieclopcuoi_playtest_{System.Guid.NewGuid():N}.db");
            string[] combatScenes = { "SampleScene", "level2", "level3", "level4", "level5", "level6" };

            try
            {
                DatabaseManager.Initialize(tempDbPath);
                Assert.AreEqual(0f, GameRepository.GetHpUpgradeBonus(), 0.001f);

                foreach (string sceneName in combatScenes)
                {
                    yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                    yield return null;

                    var bm = BattleManager.Instance;
                    var hero = UnityEngine.Object.FindFirstObjectByType<CowLopPlayerController>();
                    Assert.IsNotNull(bm, $"BattleManager missing in {sceneName}");
                    Assert.IsNotNull(hero, $"CowLopPlayerController missing in {sceneName}");
                    Assert.AreEqual(150f, bm.p1MaxHealth, 0.01f, $"{sceneName} base p1MaxHealth must be 150");
                    Assert.AreEqual(150f, bm.p1CurrentHealth, 0.01f, $"{sceneName} base p1CurrentHealth must be 150");
                    Assert.AreEqual(150f, hero.maxHealth, 0.01f, $"{sceneName} hero.maxHealth must be 150");
                    Assert.AreEqual(150f, hero.currentHealth, 0.01f, $"{sceneName} hero.currentHealth must be 150");
                }

                // Tích đủ 21 lốp để thử cả 5 bậc nâng cấp máu: +50 (200), +110 (260), +180 (330), +260 (410), +350 (500)
                for (int i = 0; i < 7; i++)
                {
                    GameRepository.SaveLevelClearRecord("SampleScene", 3, out _);
                }

                float[] expectedUpgradedHp = { 200f, 260f, 330f, 410f, 500f };
                foreach (float expectedHp in expectedUpgradedHp)
                {
                    Assert.IsTrue(GameRepository.TryPurchaseUpgrade(true));
                    yield return SceneManager.LoadSceneAsync("level6", LoadSceneMode.Single);
                    yield return null;

                    var bm = BattleManager.Instance;
                    var hero = UnityEngine.Object.FindFirstObjectByType<CowLopPlayerController>();
                    Assert.IsNotNull(bm);
                    Assert.IsNotNull(hero);
                    Assert.AreEqual(expectedHp, bm.p1MaxHealth, 0.01f);
                    Assert.AreEqual(expectedHp, hero.maxHealth, 0.01f);
                }
            }
            finally
            {
                DatabaseManager.Initialize(realDbPath);
                if (File.Exists(tempDbPath))
                {
                    File.Delete(tempDbPath);
                }
            }
        }

        [UnityTest]
        public IEnumerator AllSixCombatLevels_MonstersHaveMemeDeathSound_AndPlayOnKill()
        {
            string realDbPath = Path.Combine(Application.dataPath, "Database", "game_runtime.db");
            string tempDbPath = Path.Combine(Path.GetTempPath(), $"chieclopcuoi_memetest_{System.Guid.NewGuid():N}.db");
            string[] combatScenes = { "SampleScene", "level2", "level3", "level4", "level5", "level6" };

            try
            {
                DatabaseManager.Initialize(tempDbPath);
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetSFXVolume(1f);
                }

                var loadedMemes = Resources.LoadAll<AudioClip>("Audio/Memes");
                Assert.Greater(loadedMemes.Length, 0, "Resources/Audio/Memes must contain meme clips.");
                foreach (var clip in loadedMemes)
                {
                    Assert.IsNotNull(clip);
                    Assert.Greater(clip.length, 0f, $"Meme clip '{clip.name}' must have non-zero duration.");
                }

                foreach (string sceneName in combatScenes)
                {
                    yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                    yield return null;

                    var monsters = UnityEngine.Object.FindObjectsByType<MonsterCombatController>(FindObjectsSortMode.None);
                    Assert.Greater(monsters.Length, 0, $"Expected monsters in {sceneName}");

                    foreach (var m in monsters)
                    {
                        var meme = m.GetComponent<MemeDeathSound>();
                        Assert.IsNotNull(meme, $"Monster '{m.name}' in {sceneName} is missing MemeDeathSound.");
                        Assert.Greater(meme.memeClips.Count, 0, $"Monster '{m.name}' in {sceneName} loaded 0 meme clips.");
                    }

                    // Kiểm tra phát meme thực tế khi giết 1 quái vật ở màn 1 và màn 3
                    if (sceneName == "SampleScene" || sceneName == "level3")
                    {
                        var target = monsters[0];
                        target.TakeDamage(99999f);
                        yield return null;

                        Assert.IsTrue(target.IsDead, $"Monster '{target.name}' in {sceneName} did not die.");
                        var sharedObj = GameObject.Find("[MemeDeathAudio]");
                        Assert.IsNotNull(sharedObj, $"[MemeDeathAudio] was not created when killing monster in {sceneName}");
                        var sharedSource = sharedObj.GetComponent<AudioSource>();
                        Assert.IsNotNull(sharedSource);
                        Assert.IsFalse(sharedSource.mute);
                        Assert.Greater(sharedSource.volume, 0.5f);
                        Assert.IsTrue(sharedSource.isPlaying, $"Meme death AudioSource is not playing after killing monster in {sceneName}");
                    }
                }
            }
            finally
            {
                DatabaseManager.Initialize(realDbPath);
                if (File.Exists(tempDbPath))
                {
                    File.Delete(tempDbPath);
                }
            }
        }

        [UnityTest]
        public IEnumerator SampleScene_CowLopWalkAndRun_CycleThroughAllAlternatingSprites()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            yield return null;

            var hero = UnityEngine.Object.FindFirstObjectByType<CowLopPlayerController>();
            Assert.IsNotNull(hero);
            var sr = hero.GetComponent<SpriteRenderer>();
            var animator = hero.GetComponent<Animator>();
            Assert.IsNotNull(sr);
            Assert.IsNotNull(animator);

            Assert.IsNotNull(hero.walkRightSprites);
            Assert.IsNotNull(hero.walkLeftSprites);
            Assert.IsNotNull(hero.runSprites);
            Assert.AreEqual(8, hero.walkRightSprites.Length, "WalkRight phải có đủ 8 frame luân phiên 2 chân.");
            Assert.AreEqual(8, hero.walkLeftSprites.Length, "WalkLeft phải có đủ 8 frame luân phiên 2 chân.");
            Assert.AreEqual(8, hero.runSprites.Length, "Run phải có đủ 8 frame luân phiên 2 chân.");

            // Chờ CowLop rơi nhập màn chạm đất
            float dropWait = 0f;
            while (!hero.IsGrounded && dropWait < 3f)
            {
                dropWait += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(hero.IsGrounded);

            // Giữ D để đi sang phải trong ~0.8s (WalkRight 12 FPS -> 1 chu kỳ = 0.667s)
            var seenWalkSprites = new System.Collections.Generic.HashSet<string>();
            PressVirtualKey(Key.D);
            float elapsed = 0f;
            while (elapsed < 0.85f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (sr.sprite != null && sr.sprite.name.StartsWith("cowlop_right_"))
                {
                    seenWalkSprites.Add(sr.sprite.name);
                }
            }
            PressVirtualKey();
            yield return null;

            Assert.GreaterOrEqual(seenWalkSprites.Count, 6, $"Walk phải chuyển qua các frame 0..7, chỉ thấy: {string.Join(", ", seenWalkSprites)}");
            bool hasWalkHalf1 = seenWalkSprites.Contains("cowlop_right_0") || seenWalkSprites.Contains("cowlop_right_1")
                || seenWalkSprites.Contains("cowlop_right_2") || seenWalkSprites.Contains("cowlop_right_3");
            bool hasWalkHalf2 = seenWalkSprites.Contains("cowlop_right_4") || seenWalkSprites.Contains("cowlop_right_5")
                || seenWalkSprites.Contains("cowlop_right_6") || seenWalkSprites.Contains("cowlop_right_7");
            Assert.IsTrue(hasWalkHalf1 && hasWalkHalf2, $"Walk phải có cả nửa chu kỳ chân trái và chân phải: {string.Join(", ", seenWalkSprites)}");

            // Chuyển sang state Run trong Animator (~0.65s, 15 FPS -> 1 chu kỳ = 0.533s)
            var seenRunSprites = new System.Collections.Generic.HashSet<string>();
            animator.SetInteger("MoveDir", 1);
            animator.SetFloat("Speed", 8.4f);
            animator.SetBool("IsRunning", true);
            animator.SetBool("IsGrounded", true);
            animator.Play("Run", 0, 0f);

            elapsed = 0f;
            while (elapsed < 0.65f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                animator.Play("Run", 0, Mathf.Repeat(elapsed / (8f / 15f), 1f));
                animator.Update(0f);
                if (sr.sprite != null && sr.sprite.name.StartsWith("cowlop_right_"))
                {
                    seenRunSprites.Add(sr.sprite.name);
                }
            }

            Assert.GreaterOrEqual(seenRunSprites.Count, 6, $"Run phải chuyển qua các frame 8..15, chỉ thấy: {string.Join(", ", seenRunSprites)}");
            bool hasRunHalf1 = seenRunSprites.Contains("cowlop_right_8") || seenRunSprites.Contains("cowlop_right_9")
                || seenRunSprites.Contains("cowlop_right_10") || seenRunSprites.Contains("cowlop_right_11");
            bool hasRunHalf2 = seenRunSprites.Contains("cowlop_right_12") || seenRunSprites.Contains("cowlop_right_13")
                || seenRunSprites.Contains("cowlop_right_14") || seenRunSprites.Contains("cowlop_right_15");
            Assert.IsTrue(hasRunHalf1 && hasRunHalf2, $"Run phải có cả nửa chu kỳ chân trái và chân phải: {string.Join(", ", seenRunSprites)}");
        }

        [UnityTest]
        public IEnumerator LoadingScene_LoadsPendingTargetSceneWithPinkTireAndProgress()
        {
            float prevMin = LoadingSceneController.MinimumDisplaySeconds;
            LoadingSceneController.MinimumDisplaySeconds = 0.35f;
            try
            {
                LoadingSceneController.LoadWithLoading("SampleScene");
                yield return null;
                yield return null;

                Assert.AreEqual("LoadingScene", SceneManager.GetActiveScene().name);
                Assert.IsNotNull(GameObject.Find("HUB_load"));
                Assert.IsNotNull(GameObject.Find("HUB_load/Img_Background"));
                var backdropImg = GameObject.Find("HUB_load/Img_White_Backdrop")?.GetComponent<UnityEngine.UI.Image>();
                Assert.IsNotNull(backdropImg);
                Assert.AreEqual(Color.black, backdropImg.color);
                var trackGroupRt = GameObject.Find("HUB_load/Loading_Track_Group")?.GetComponent<RectTransform>();
                Assert.IsNotNull(trackGroupRt);
                Assert.Greater(trackGroupRt.anchoredPosition.x, 200f, "Thanh load phải nằm ở phía bên phải.");
                Assert.AreEqual(0f, trackGroupRt.anchoredPosition.y, 1f, "Thanh load phải nằm giữa chiều dọc.");
                Assert.IsNotNull(GameObject.Find("HUB_load/Loading_Track_Group/Img_Pink_Tire"));
                Assert.IsNotNull(GameObject.Find("HUB_load/Loading_Track_Group/Img_Load_Fill"));

                float timeout = 12f;
                while (SceneManager.GetActiveScene().name != "SampleScene" && timeout > 0f)
                {
                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }
                Assert.AreEqual("SampleScene", SceneManager.GetActiveScene().name, "LoadingScene phải tự chuyển vào SampleScene.");
            }
            finally
            {
                LoadingSceneController.MinimumDisplaySeconds = prevMin;
            }
        }

        [UnityTest]
        public IEnumerator SimpleTutorialController_GuidesLoginAndCombat_AndSkipWorks()
        {
            SimpleTutorialController.ResetTutorialProgress();
            yield return SceneManager.LoadSceneAsync("LoginScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var ctrlGo = new GameObject("[SimpleTutorialController]");
            Object.DontDestroyOnLoad(ctrlGo);
            var ctrl = ctrlGo.AddComponent<SimpleTutorialController>();
            ctrl.BeginForActiveScene();
            yield return null;
            yield return null;

            Assert.IsNotNull(GameObject.Find("Tutorial_Spotlight_Canvas"), "LoginScene phải hiển thị spotlight hướng dẫn nút PLAY.");
            var bannerImg = GameObject.Find("Tutorial_Spotlight_Canvas/Top_Notice_Banner")?.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(bannerImg);
            Assert.AreEqual("White_Card_Bg", bannerImg.sprite != null ? bannerImg.sprite.name : string.Empty, "Khung chỉ dẫn phải dùng nền trắng White_Card_Bg.");
            var skipBtn = GameObject.Find("Tutorial_Spotlight_Canvas/Top_Notice_Banner/Btn_Tutorial_Skip")?.GetComponent<UnityEngine.UI.Button>();
            Assert.IsNotNull(skipBtn, "Phải có nút Skip trong thanh thông báo hướng dẫn.");
            Assert.AreEqual("Btn_Map_Blue", skipBtn.GetComponent<UnityEngine.UI.Image>()?.sprite?.name, "Nút Skip phải dùng sprite Btn_Map_Blue.");
            skipBtn.onClick.Invoke();
            yield return null;

            Assert.IsTrue(SimpleTutorialController.IsTutorialDone(), "Bấm Skip phải lưu trạng thái hoàn tất hướng dẫn.");

            // Kiểm tra tiếp chặng 3 tại SampleScene (sau Reset -> nút Tiếp -> Bắt đầu)
            SimpleTutorialController.ResetTutorialProgress();
            PlayerPrefs.SetInt(SimpleTutorialController.TutorialStagePrefKey, 2);
            PlayerPrefs.Save();

            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var ctrlGo2 = new GameObject("[SimpleTutorialController]");
            Object.DontDestroyOnLoad(ctrlGo2);
            var ctrl2 = ctrlGo2.AddComponent<SimpleTutorialController>();
            ctrl2.BeginForActiveScene();

            float wait = 3f;
            while (GameObject.Find("Tutorial_Spotlight_Canvas/Top_Notice_Banner/Btn_Tutorial_Next") == null && wait > 0f)
            {
                wait -= Time.unscaledDeltaTime;
                yield return null;
            }

            var nextBtn = GameObject.Find("Tutorial_Spotlight_Canvas/Top_Notice_Banner/Btn_Tutorial_Next")?.GetComponent<UnityEngine.UI.Button>();
            Assert.IsNotNull(nextBtn, "Chặng 3 trên SampleScene phải hiện thanh chỉ dẫn Di chuyển + nút Tiếp.");
            Assert.IsTrue(BattleManager.Instance != null && BattleManager.Instance.isTimerPaused, "Timer trận đấu phải tạm dừng trong lúc xem hướng dẫn.");

            nextBtn.onClick.Invoke();
            yield return null;
            nextBtn.onClick.Invoke();
            yield return null;

            Assert.IsTrue(SimpleTutorialController.IsTutorialDone(), "Hoàn tất bước Chiêu 1->6 phải đánh dấu TutorialDone.");
            Assert.IsFalse(BattleManager.Instance != null && BattleManager.Instance.isTimerPaused, "Kết thúc hướng dẫn phải mở lại Timer trận đấu.");
        }

        [UnityTest]
        public IEnumerator VictoryFinalePanel_ShowsBigPinkTireAnd30RewardOnCampaignClear()
        {
            string realDb = Path.Combine(Application.dataPath, "Database", "game_runtime.db");
            string tempDb = Path.Combine(Path.GetTempPath(), $"chieclopcuoi_finale_{System.Guid.NewGuid():N}.db");
            DatabaseManager.Initialize(tempDb);
            try
            {
                string[] allLevels = { "SampleScene", "level2", "level3", "level4", "level5", "level6" };
                foreach (string lv in allLevels)
                {
                    GameRepository.SaveLevelClearRecord(lv, 1, out _);
                }

                yield return SceneManager.LoadSceneAsync("level6", LoadSceneMode.Single);
                yield return null;

                var hud = GameObject.Find("HUD_Canvas");
                Assert.IsNotNull(hud);

                var panel = VictoryFinalePanel.ShowOrCreate(hud.transform);
                yield return null;

                Assert.IsNotNull(panel);
                Assert.IsNotNull(GameObject.Find("HUD_Canvas/Victory_Finale_Panel/Stone_Board/Center_Showcase/Emblem_Pink_Tire"));
                var rewardTmp = GameObject.Find("HUD_Canvas/Victory_Finale_Panel/Stone_Board/Center_Showcase/Reward_Pink_Tire_Text")?.GetComponent<TMPro.TMP_Text>();
                Assert.IsNotNull(rewardTmp);
                Assert.AreEqual("+30 Lốp Hồng!", rewardTmp.text);
                StringAssert.DoesNotContain("Thưởng 6 Màn", rewardTmp.text);
                Assert.IsTrue(GameRepository.HasClaimedCampaign6PinkTireReward());

                // Mở lại lần 2 cũng giữ nguyên đúng dòng chữ +30 Lốp Hồng!
                VictoryFinalePanel.ShowOrCreate(hud.transform);
                yield return null;
                Assert.AreEqual("+30 Lốp Hồng!", rewardTmp.text);
            }
            finally
            {
                DatabaseManager.Close();
                if (File.Exists(tempDb)) File.Delete(tempDb);
                DatabaseManager.Initialize(realDb);
            }
        }

        [UnityTest]
        public IEnumerator Levels4To6_HaveRoundTime150Seconds()
        {
            string[] scenes = { "level4", "level5", "level6" };
            foreach (string s in scenes)
            {
                yield return SceneManager.LoadSceneAsync(s, LoadSceneMode.Single);
                yield return null;

                var bm = Object.FindFirstObjectByType<BattleManager>();
                Assert.IsNotNull(bm, $"Scene {s} phải có BattleManager.");
                Assert.AreEqual(150f, bm.roundTimeSeconds, 0.01f, $"Màn {s} phải có roundTimeSeconds = 150s.");
                Assert.AreEqual(150, bm.SecondsRemaining, $"Màn {s} lúc bắt đầu phải hiển thị 150s.");
            }
        }

        [UnityTest]
        public IEnumerator HubRobot_MountsMidLeftWithWhiteCard_AndRestoresJoystick()
        {
            int prevTut = PlayerPrefs.GetInt(SimpleTutorialController.TutorialDonePrefKey, 0);
            PlayerPrefs.SetInt(SimpleTutorialController.TutorialDonePrefKey, 1);
            PlayerPrefs.Save();
            try
            {
                yield return SceneManager.LoadSceneAsync("level2", LoadSceneMode.Single);
                yield return null;

                var hubGo = GameObject.Find("HUD_Canvas/Robot_Companion");
                Assert.IsNotNull(hubGo, "Robot_Companion phải tự gắn lên HUD_Canvas ở các màn chiến đấu.");

                var hubRt = hubGo.GetComponent<RectTransform>();
                Assert.AreEqual(0f, hubRt.anchorMin.x, 0.01f);
                Assert.AreEqual(0.5f, hubRt.anchorMin.y, 0.01f, "Robot_Companion phải neo ở giữa bên trái (y = 0.5).");
                Assert.AreEqual(HubRobotController.RobotAnchoredPos.x, hubRt.anchoredPosition.x, 1f);
                Assert.AreEqual(HubRobotController.RobotAnchoredPos.y, hubRt.anchoredPosition.y, 1f);

                var moveRt = GameObject.Find("HUD_Canvas/Circular_Movement_Controller")?.GetComponent<RectTransform>();
                Assert.IsNotNull(moveRt);
                Assert.AreEqual(HubRobotController.DefaultJoystickAnchoredPos.x, moveRt.anchoredPosition.x, 1f, "Vòng tròn di chuyển phải trả về góc trái-dưới (160, 160).");

                var robotImg = GameObject.Find("HUD_Canvas/Robot_Companion/Robot_Visual_Root/Img_Robot_Companion")?.GetComponent<UnityEngine.UI.Image>();
                Assert.IsNotNull(robotImg);
                Assert.IsNotNull(robotImg.sprite, "Img_Robot_Companion phải nạp được sprite Robot_Companion.");

                var bubbleImg = GameObject.Find("HUD_Canvas/Robot_Companion/Robot_Speech_Bubble")?.GetComponent<UnityEngine.UI.Image>();
                Assert.IsNotNull(bubbleImg);
                Assert.AreEqual("White_Card_Bg", bubbleImg.sprite != null ? bubbleImg.sprite.name : string.Empty, "Khung thoại Robot phải dùng nền trắng White_Card_Bg.");

                var headerTxt = GameObject.Find("HUD_Canvas/Robot_Companion/Robot_Speech_Bubble/Txt_Robot_Header")?.GetComponent<TMPro.TMP_Text>();
                Assert.IsNotNull(headerTxt);
                Assert.IsFalse(headerTxt.text.Contains("HUB"), "Không được hiển thị chữ HUB trên giao diện.");

                var robotCtrl = hubGo.GetComponent<HubRobotController>();
                Assert.IsNotNull(robotCtrl);

                float initialTilt = robotCtrl.CurrentVisualTiltAngle;
                float wait = 0f;
                while (string.IsNullOrEmpty(robotCtrl.CurrentDisplayedLine) && wait < 3f)
                {
                    wait += Time.unscaledDeltaTime;
                    yield return null;
                }
                Assert.IsFalse(string.IsNullOrEmpty(robotCtrl.CurrentDisplayedLine), "Hub_robot phải tự động hiện lời thoại thách thức.");

                string firstLine = robotCtrl.CurrentDisplayedLine;
                robotCtrl.TriggerNextDialogue();
                yield return null;
                Assert.AreNotEqual(firstLine, robotCtrl.CurrentDisplayedLine, "Khi đổi câu thoại tiếp theo phải sang lời thách thức mới.");

                // Đợi vài khung hình để kiểm tra thân robot đang lắc nghiêng chuyển động
                for (int i = 0; i < 6; i++) yield return null;
                float movedTilt = robotCtrl.CurrentVisualTiltAngle;
                Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(initialTilt, movedTilt)), 0.05f, "Robot_Visual_Root phải có chuyển động lắc nghiêng liên tục.");
            }
            finally
            {
                PlayerPrefs.SetInt(SimpleTutorialController.TutorialDonePrefKey, prevTut);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator SampleScene_JumpingIgnoresEnemyDamage_AndDefeatingBossRequiresReachingMapEndToWin()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            yield return null;

            var bm = BattleManager.Instance;
            var hero = Object.FindFirstObjectByType<CowLopPlayerController>();
            Assert.IsNotNull(bm);
            Assert.IsNotNull(hero);
            Assert.GreaterOrEqual(hero.maxX, bm.enemyStatueX, "SampleScene hero.maxX phải đủ lớn để đi tới tượng cuối map.");

            float dropWait = 0f;
            while (!hero.IsGrounded && dropWait < 3f)
            {
                bm.HealPlayer1(bm.p1MaxHealth);
                dropWait += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(hero.IsGrounded);
            bm.HealPlayer1(bm.p1MaxHealth);

            // 1. Nhảy lên -> không dính sát thương
            PressVirtualKey(Key.Digit5);
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsFalse(hero.IsGrounded, "CowLop phải đang ở trên không sau khi nhấn Nhảy.");
            PressVirtualKey();

            float hpBeforeAirHit = bm.p1CurrentHealth;
            bm.DamagePlayer1(45f);
            Assert.AreEqual(hpBeforeAirHit, bm.p1CurrentHealth, 0.01f, "Khi đang nhảy trên không không được dính sát thương.");

            // 2. Hạ Boss -> Chưa được hiện Win ngay, phải chạy về cuối map (enemyStatueX) mới Win
            bm.DamagePlayer2(bm.p2MaxHealth + 200f);
            yield return null;
            Assert.IsNotNull(bm.npcFemale);
            Assert.IsTrue(bm.npcFemale.IsDead, "Boss phải đã bị hạ gục.");
            Assert.AreEqual(BattleManager.BattleState.Playing, bm.State, "Hạ Boss xong chưa được Win ngay trước khi tới cuối map.");

            hero.transform.position = new Vector3(bm.enemyStatueX, hero.groundY, hero.transform.position.z);
            yield return null;
            yield return null;
            Assert.AreEqual(BattleManager.BattleState.Finished, bm.State, "Khi tiến về tượng cuối map sau khi hạ Boss phải chuyển sang trạng thái Win.");
            Assert.IsTrue(bm.LastSecondsRemainingAtFinish >= 0);
        }

        [UnityTest]
        public IEnumerator SkyDriftingTires_SpawnsAndDriftsLeftToRightInLoginAndSampleScene()
        {
            yield return SceneManager.LoadSceneAsync("LoginScene", LoadSceneMode.Single);
            yield return null;

            var loginTires = Object.FindFirstObjectByType<DriftingSkyTiresFX>();
            Assert.IsNotNull(loginTires, "LoginScene phải có hiệu ứng lốp trôi trên nền trời.");
            Assert.GreaterOrEqual(loginTires.TireCount, 6);
            float loginStartX = loginTires.GetTireX(0);
            yield return new WaitForSeconds(0.25f);
            Assert.Greater(loginTires.GetTireX(0), loginStartX, "Lốp trên nền trời LoginScene phải trôi từ Trái sang Phải.");

            yield return SceneManager.LoadSceneAsync("LevelSelectScene", LoadSceneMode.Single);
            yield return null;

            var selectTires = Object.FindFirstObjectByType<DriftingSkyTiresFX>();
            Assert.IsNotNull(selectTires, "LevelSelectScene (Trang chọn level) phải có hiệu ứng lốp trôi trên nền trời.");
            Assert.GreaterOrEqual(selectTires.TireCount, 6);
            float selectStartX = selectTires.GetTireX(0);
            yield return new WaitForSeconds(0.25f);
            Assert.Greater(selectTires.GetTireX(0), selectStartX, "Lốp trên nền trời LevelSelectScene phải trôi từ Trái sang Phải.");

            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            yield return null;

            var level1Tires = Object.FindFirstObjectByType<DriftingSkyTiresFX>();
            Assert.IsNotNull(level1Tires, "SampleScene (Level 1) phải có hiệu ứng lốp trôi trên nền trời.");
            Assert.GreaterOrEqual(level1Tires.TireCount, 6);
            float lv1StartX = level1Tires.GetTireX(0);
            yield return new WaitForSeconds(0.25f);
            Assert.Greater(level1Tires.GetTireX(0), lv1StartX, "Lốp trên nền trời SampleScene phải trôi từ Trái sang Phải.");

            // Xác nhận Victory_Finale_Panel dù bật sẵn trong Prefab để chỉnh UI nhưng vẫn tự ẩn lúc vào trận
            var finalePanel = GameObject.Find("HUD_Canvas")?.transform.Find("Victory_Finale_Panel");
            Assert.IsNotNull(finalePanel);
            Assert.IsFalse(finalePanel.gameObject.activeSelf, "Victory_Finale_Panel phải tự ẩn lúc bắt đầu màn chiến đấu.");
        }
    }
}
