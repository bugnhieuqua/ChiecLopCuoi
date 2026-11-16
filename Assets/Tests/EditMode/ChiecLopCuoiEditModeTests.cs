using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using ChiecLopCuoi.Database;
using ChiecLopCuoi.Gameplay;

namespace ChiecLopCuoi.Tests.EditMode
{
    public class ChiecLopCuoiEditModeTests
    {
        private string _tempDbPath;
        private string _realDbPath;

        [SetUp]
        public void SetUp()
        {
            _realDbPath = Path.Combine(Application.dataPath, "Database", "game_runtime.db");
            _tempDbPath = Path.Combine(Path.GetTempPath(), $"chieclopcuoi_test_{Guid.NewGuid():N}.db");
            DatabaseManager.Initialize(_tempDbPath);
        }

        [TearDown]
        public void TearDown()
        {
            DatabaseManager.Close();
            if (!string.IsNullOrEmpty(_tempDbPath) && File.Exists(_tempDbPath))
            {
                File.Delete(_tempDbPath);
            }
            if (!string.IsNullOrEmpty(_realDbPath))
            {
                DatabaseManager.Initialize(_realDbPath);
                DatabaseManager.Close();
            }
        }

        [Test]
        public void DatabaseManager_InitializesAndMigratesToVersion5WithoutLegacyKeys()
        {
            Assert.IsTrue(DatabaseManager.IsInitialized);
            Assert.AreEqual(5f, GameRepository.GetConfigFloat("config_schema_version", 0f), 0.001f);
            Assert.AreEqual(-1f, GameRepository.GetConfigFloat("castle_spawn_x", -1f), 0.001f);
            Assert.AreEqual(-1f, GameRepository.GetConfigFloat("enemy_form1_tire_cost", -1f), 0.001f);
        }

        [Test]
        public void EvaluateStarsByRemainingSeconds_Respects60sAnd30sThresholds()
        {
            Assert.AreEqual(0, GameRepository.EvaluateStarsByRemainingSeconds(false, 90));
            Assert.AreEqual(0, GameRepository.EvaluateStarsByRemainingSeconds(true, 0));
            Assert.AreEqual(1, GameRepository.EvaluateStarsByRemainingSeconds(true, 1));
            Assert.AreEqual(1, GameRepository.EvaluateStarsByRemainingSeconds(true, 29));
            Assert.AreEqual(2, GameRepository.EvaluateStarsByRemainingSeconds(true, 30));
            Assert.AreEqual(2, GameRepository.EvaluateStarsByRemainingSeconds(true, 59));
            Assert.AreEqual(3, GameRepository.EvaluateStarsByRemainingSeconds(true, 60));
            Assert.AreEqual(3, GameRepository.EvaluateStarsByRemainingSeconds(true, 120));
        }

        [Test]
        public void SaveLevelClearRecord_AccumulatesTiresBeyond18AndKeepsBestStars()
        {
            for (int i = 0; i < 8; i++)
            {
                GameRepository.SaveLevelClearRecord("SampleScene", 3, out int best);
                Assert.AreEqual(3, best);
            }

            // Sau 8 lần thắng 3 sao -> tổng lốp = 24 (> 18)
            Assert.AreEqual(24, GameRepository.GetTotalCollectedTires());

            // Thắng lại với 1 sao không làm tụt kỷ lục 3 sao
            GameRepository.SaveLevelClearRecord("SampleScene", 1, out int bestAfterLower);
            Assert.AreEqual(3, bestAfterLower);
            Assert.AreEqual(3, GameRepository.GetLevelStars("SampleScene"));
            Assert.AreEqual(25, GameRepository.GetTotalCollectedTires());
        }

        [Test]
        public void TireUpgrades_SpendTiresIncreaseStatsAndRespectMaxTier()
        {
            // Khởi tạo 21 lốp để thử cả 5 bậc nâng cấp Máu (2 + 3 + 4 + 5 + 6 = 20 lốp -> +350 HP => 500 HP)
            GameRepository.SetConfigFloat(GameRepository.TotalCollectedTiresKey, 21f);
            Assert.AreEqual(21, GameRepository.GetAvailableTires());

            float[] expectedHpBonus = { 50f, 110f, 180f, 260f, 350f };
            int[] expectedRemaining = { 19, 16, 12, 7, 1 };
            for (int tier = 1; tier <= 5; tier++)
            {
                Assert.IsTrue(GameRepository.TryPurchaseUpgrade(isHpUpgrade: true));
                Assert.AreEqual(tier, GameRepository.GetHpUpgradeTier());
                Assert.AreEqual(expectedHpBonus[tier - 1], GameRepository.GetHpUpgradeBonus(), 0.01f);
                Assert.AreEqual(expectedRemaining[tier - 1], GameRepository.GetAvailableTires());
            }

            // Bậc tối đa 5 -> không mua thêm HP; ATK bậc 1 cần 2 lốp nhưng chỉ còn 1 -> từ chối
            Assert.IsFalse(GameRepository.TryPurchaseUpgrade(isHpUpgrade: true));
            Assert.IsFalse(GameRepository.TryPurchaseUpgrade(isHpUpgrade: false));
            Assert.AreEqual(0, GameRepository.GetAtkUpgradeTier());

            // Nâng cấp 5 bậc Sát thương (+5, +11, +18, +26, +36)
            GameRepository.SetConfigFloat(GameRepository.TotalCollectedTiresKey, 41f);
            float[] expectedAtkBonus = { 5f, 11f, 18f, 26f, 36f };
            for (int tier = 1; tier <= 5; tier++)
            {
                Assert.IsTrue(GameRepository.TryPurchaseUpgrade(isHpUpgrade: false));
                Assert.AreEqual(tier, GameRepository.GetAtkUpgradeTier());
                Assert.AreEqual(expectedAtkBonus[tier - 1], GameRepository.GetAtkUpgradeBonus(), 0.01f);
            }
            Assert.IsFalse(GameRepository.TryPurchaseUpgrade(isHpUpgrade: false));
        }

        [Test]
        public void DifficultyMode_AppliesExpectedMultipliersAndClamps()
        {
            GameRepository.SetDifficultyMode(0);
            Assert.IsFalse(GameRepository.IsHardMode());
            Assert.AreEqual(1.0f, GameRepository.GetBossCooldownMultiplier(), 0.001f);
            Assert.AreEqual(1.0f, GameRepository.GetBossHpMultiplier(), 0.001f);

            GameRepository.SetDifficultyMode(1);
            Assert.IsTrue(GameRepository.IsHardMode());
            Assert.AreEqual(0.45f, GameRepository.GetBossCooldownMultiplier(), 0.001f);
            Assert.AreEqual(1.40f, GameRepository.GetBossHpMultiplier(), 0.001f);

            GameRepository.SetDifficultyMode(99);
            Assert.AreEqual(1, GameRepository.GetDifficultyMode());
        }

        [Test]
        public void SkillInputLookup_OutOfRangeIndicesReturnFalseSafely()
        {
            Assert.IsFalse(CowLopPlayerController.IsSkillInputDown(0));
            Assert.IsFalse(CowLopPlayerController.IsSkillInputDown(7));
            Assert.IsFalse(CowLopPlayerController.IsSkillInputHeld(0));
            Assert.IsFalse(CowLopPlayerController.IsSkillInputHeld(7));
        }

        [Test]
        public void LoadingSceneAssets_ArePresentInResourcesAndBuildSettings()
        {
            Assert.IsNotNull(Resources.Load<Sprite>("UI/Loading_Bg"), "Resources/UI/Loading_Bg phải load được dạng Sprite.");
            Assert.IsNotNull(Resources.Load<Sprite>("UI/Pink_Tire"), "Resources/UI/Pink_Tire phải load được dạng Sprite.");

            bool foundInBuild = false;
            foreach (var s in UnityEditor.EditorBuildSettings.scenes)
            {
                if (s != null && s.enabled && s.path == "Assets/Scenes/LoadingScene.unity")
                {
                    foundInBuild = true;
                    break;
                }
            }
            Assert.IsTrue(foundInBuild, "Assets/Scenes/LoadingScene.unity phải được bật trong EditorBuildSettings.");
        }

        [Test]
        public void Campaign6Clear_OneStarPerLevel_GrantsBigPinkTireAnd30TiresOnce()
        {
            string[] allLevels = { "SampleScene", "level2", "level3", "level4", "level5", "level6" };
            int beforeTires = GameRepository.GetAvailableTires();

            foreach (string lv in allLevels)
            {
                GameRepository.SaveLevelClearRecord(lv, 1, out int best);
                Assert.AreEqual(1, best);
            }

            Assert.AreEqual(6, GameRepository.GetClearedLevelCount());
            Assert.IsFalse(GameRepository.HasClaimedCampaign6PinkTireReward());

            int bonusFirst = GameRepository.TryGrantCampaign6PinkTireReward();
            Assert.AreEqual(30, bonusFirst);
            Assert.IsTrue(GameRepository.HasClaimedCampaign6PinkTireReward());
            Assert.AreEqual(beforeTires + 6 + 30, GameRepository.GetAvailableTires());

            // Gọi lại lần 2 không được cộng trùng
            int bonusSecond = GameRepository.TryGrantCampaign6PinkTireReward();
            Assert.AreEqual(0, bonusSecond);
            Assert.AreEqual(beforeTires + 6 + 30, GameRepository.GetAvailableTires());
        }

        [Test]
        public void HubRobot_SpriteIsPresentInResources()
        {
            Assert.IsNotNull(Resources.Load<Sprite>("UI/Robot_Companion"), "Resources/UI/Robot_Companion phải load được dạng Sprite.");
            Assert.IsNotNull(Resources.Load<Sprite>("UI/White_Card_Bg"), "Resources/UI/White_Card_Bg phải load được dạng Sprite.");
            Assert.IsNotNull(Resources.Load<Sprite>("UI/Btn_Map_Blue"), "Resources/UI/Btn_Map_Blue phải load được dạng Sprite.");
        }

        [Test]
        public void SkyTires_SixSpritesArePresentInResources()
        {
            string[] paths = { "Tires/tire_01", "Tires/tire_02", "Tires/tire_03", "Tires/tire_04", "Tires/tire_05", "UI/Pink_Tire" };
            foreach (string p in paths)
            {
                Assert.IsNotNull(Resources.Load<Sprite>(p), $"Resources/{p} phải load được dạng Sprite.");
            }
        }

        [Test]
        public void ActionControlsAndVoice_MultipleSpriteSlicesAreIntact()
        {
            Sprite[] actionSprites = Resources.LoadAll<Sprite>("UI/ui_action_buttons");
            Assert.AreEqual(6, actionSprites.Length, "ui_action_buttons phải giữ nguyên chế độ Multiple với đủ 6 icon điều khiển 1->6.");

            Sprite[] voiceSprites = Resources.LoadAll<Sprite>("UI/ui_voice_toggle");
            Assert.AreEqual(2, voiceSprites.Length, "ui_voice_toggle phải giữ nguyên chế độ Multiple với đủ 2 icon.");
        }

        [Test]
        public void ResetProgressConfigAndSaveData_ResetsCampaignAndTiresWhilePreservingAudio()
        {
            GameRepository.SetAudioBgmVolume(0.42f);
            GameRepository.SaveLevelClearRecord("SampleScene", 3, out _);
            GameRepository.TryPurchaseUpgrade(true);
            GameRepository.SetDifficultyMode(1);

            GameRepository.ResetProgressConfigAndSaveData(preserveAudioSettings: true);

            Assert.AreEqual(0, GameRepository.GetClearedLevelCount());
            Assert.AreEqual(0, GameRepository.GetLevelStars("SampleScene"));
            Assert.AreEqual(0, GameRepository.GetTotalCollectedTires());
            Assert.AreEqual(0, GameRepository.GetAvailableTires());
            Assert.AreEqual(0, GameRepository.GetHpUpgradeTier());
            Assert.AreEqual(0, GameRepository.GetDifficultyMode());
            Assert.IsFalse(GameRepository.HasClaimedCampaign6PinkTireReward());
            Assert.AreEqual(0.42f, GameRepository.GetAudioBgmVolume(), 0.001f);
        }
    }
}
