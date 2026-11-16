using System.Collections.Generic;
using UnityEngine;

namespace ChiecLopCuoi.Database
{
    /// <summary>
    /// Repository cung cấp các hàm nghiệp vụ đọc/ghi CSDL cho Gameplay mà không hardcode.
    /// </summary>
    public static class GameRepository
    {
        // ====================================================================
        // 1. GAME CONFIGURATION (ĐỌC / GHI CẤU HÌNH)
        // ====================================================================

        public static float GetConfigFloat(string key, float defaultValue)
        {
            string sql = "SELECT value_real FROM GameConfig WHERE config_key = @key LIMIT 1;";
            var pKey = new SqliteParameter("@key", key);
            return DatabaseManager.ExecuteScalar(sql, defaultValue, pKey);
        }

        public static string GetConfigString(string key, string defaultValue)
        {
            string sql = "SELECT value_text FROM GameConfig WHERE config_key = @key LIMIT 1;";
            var pKey = new SqliteParameter("@key", key);
            return DatabaseManager.ExecuteScalar(sql, defaultValue, pKey);
        }

        public static void SetConfigFloat(string key, float value)
        {
            string sql = @"
                INSERT INTO GameConfig (config_key, value_real, value_text)
                VALUES (@key, @val, '')
                ON CONFLICT(config_key) DO UPDATE SET value_real = @val;
            ";
            var pKey = new SqliteParameter("@key", key);
            var pVal = new SqliteParameter("@val", value);
            DatabaseManager.ExecuteNonQuery(sql, pKey, pVal);
        }

        public static void SetConfigString(string key, string value)
        {
            string sql = @"
                INSERT INTO GameConfig (config_key, value_real, value_text)
                VALUES (@key, 0, @val)
                ON CONFLICT(config_key) DO UPDATE SET value_text = @val;
            ";
            var pKey = new SqliteParameter("@key", key);
            var pVal = new SqliteParameter("@val", value);
            DatabaseManager.ExecuteNonQuery(sql, pKey, pVal);
        }

        // ====================================================================
        // 2. PLAYER DATA
        // ====================================================================

        public static void SavePlayerData(int tires, int population)
        {
            const string sql = @"INSERT OR REPLACE INTO PlayerData (id, tires, population, last_save)
                VALUES (1, @tires, @population, CURRENT_TIMESTAMP);";
            DatabaseManager.ExecuteNonQuery(sql,
                new SqliteParameter("@tires", tires),
                new SqliteParameter("@population", population));
        }

        public static void LoadPlayerData(out int tires, out int population)
        {
            int loadedTires = 200;
            int loadedPopulation = 0;
            DatabaseManager.ExecuteReader("SELECT tires, population FROM PlayerData WHERE id = 1 LIMIT 1;", reader =>
            {
                if (!reader.Read()) return;
                loadedTires = reader.GetInt32(0);
                loadedPopulation = reader.GetInt32(1);
            });
            tires = loadedTires;
            population = loadedPopulation;
        }

        // ====================================================================
        // 3. LEVEL STARS & COLLECTED TIRES PROGRESS (0..3 SAO/MÀN, LỐP TÍCH LŨY KHÔNG GIỚI HẠN)
        // ====================================================================

        public const string TotalCollectedTiresKey = "collected_tires_total";

        public static int GetTotalCollectedTires()
            => Mathf.Max(0, Mathf.RoundToInt(GetConfigFloat(TotalCollectedTiresKey, 0f)));

        public static int GetLevelStars(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return 0;
            return Mathf.Clamp(Mathf.RoundToInt(GetConfigFloat($"{sceneName}_stars", 0f)), 0, 3);
        }

        public static int EvaluateStarsByRemainingSeconds(bool playerWon, int secondsRemaining)
        {
            if (!playerWon || secondsRemaining <= 0) return 0;
            if (secondsRemaining >= 60) return 3; // 120 - 60s
            if (secondsRemaining >= 30) return 2; // 59 - 30s
            return 1;                             // 29 - 1s
        }

        /// <summary>
        /// Lưu kỷ lục sao cao nhất mỗi màn chơi (0..3) và cộng thưởng Lốp mỗi lần chiến thắng (không giới hạn 18).
        /// </summary>
        public static int SaveLevelClearRecord(string sceneName, int earnedStars, out int newBestStars)
        {
            int clamped = Mathf.Clamp(earnedStars, 0, 3);
            int prevBest = GetLevelStars(sceneName);
            newBestStars = Mathf.Max(prevBest, clamped);

            if (!string.IsNullOrEmpty(sceneName) && newBestStars > 0)
            {
                SetConfigFloat($"{sceneName}_cleared", 1f);
                SetConfigFloat($"{sceneName}_stars", newBestStars);
            }

            if (clamped > 0)
            {
                SetConfigFloat(TotalCollectedTiresKey, GetTotalCollectedTires() + clamped);
            }
            return clamped;
        }

        // ====================================================================
        // 4. LƯU CẤU HÌNH ÂM LƯỢNG NHẠC & ÂM THANH (SQLITE)
        // ====================================================================

        public const string AudioBgmVolumeKey = "audio_bgm_volume";
        public const string AudioSfxVolumeKey = "audio_sfx_volume";

        public static float GetAudioBgmVolume()
            => Mathf.Clamp01(GetConfigFloat(AudioBgmVolumeKey, 0.8f));

        public static void SetAudioBgmVolume(float volume)
            => SetConfigFloat(AudioBgmVolumeKey, Mathf.Clamp01(volume));

        public static float GetAudioSfxVolume()
            => Mathf.Clamp01(GetConfigFloat(AudioSfxVolumeKey, 1.0f));

        public static void SetAudioSfxVolume(float volume)
            => SetConfigFloat(AudioSfxVolumeKey, Mathf.Clamp01(volume));

        // ====================================================================
        // 5. NÂNG CẤP CHỈ SỐ COW LỐP BẰNG LỐP & THỐNG KÊ CHIẾN DỊCH
        // ====================================================================

        public const string SpentTiresKey = "spent_tires_total";
        public const string HpUpgradeTierKey = "upgrade_hp_tier";
        public const string AtkUpgradeTierKey = "upgrade_atk_tier";
        public const string Campaign6ClearRewardClaimedKey = "campaign_6_clear_pink_tire_reward_claimed";
        public const int MaxUpgradeTier = 5;
        public const int Campaign6ClearBonusTires = 30;

        private static readonly string[] CampaignSceneKeys =
        {
            "SampleScene", "level2", "level3", "level4", "level5", "level6"
        };

        private static readonly int[] UpgradeTierCosts = { 2, 3, 4, 5, 6 };
        private static readonly float[] HpBonusByTier = { 0f, 50f, 110f, 180f, 260f, 350f };
        private static readonly float[] AtkBonusByTier = { 0f, 5f, 11f, 18f, 26f, 36f };

        public static int GetSpentTires()
            => Mathf.Max(0, Mathf.RoundToInt(GetConfigFloat(SpentTiresKey, 0f)));

        public static int GetAvailableTires()
            => Mathf.Max(0, GetTotalCollectedTires() - GetSpentTires());

        public static int GetHpUpgradeTier()
            => Mathf.Clamp(Mathf.RoundToInt(GetConfigFloat(HpUpgradeTierKey, 0f)), 0, MaxUpgradeTier);

        public static int GetAtkUpgradeTier()
            => Mathf.Clamp(Mathf.RoundToInt(GetConfigFloat(AtkUpgradeTierKey, 0f)), 0, MaxUpgradeTier);

        public static float GetHpUpgradeBonus()
            => HpBonusByTier[GetHpUpgradeTier()];

        public static float GetAtkUpgradeBonus()
            => AtkBonusByTier[GetAtkUpgradeTier()];

        public static float GetHpBonusForTier(int tier)
            => HpBonusByTier[Mathf.Clamp(tier, 0, MaxUpgradeTier)];

        public static float GetAtkBonusForTier(int tier)
            => AtkBonusByTier[Mathf.Clamp(tier, 0, MaxUpgradeTier)];

        public static int GetNextUpgradeCost(int currentTier)
            => (currentTier >= 0 && currentTier < UpgradeTierCosts.Length) ? UpgradeTierCosts[currentTier] : 0;

        public static bool TryPurchaseUpgrade(bool isHpUpgrade)
        {
            int currentTier = isHpUpgrade ? GetHpUpgradeTier() : GetAtkUpgradeTier();
            if (currentTier >= MaxUpgradeTier) return false;

            int cost = GetNextUpgradeCost(currentTier);
            int available = GetAvailableTires();
            if (cost <= 0 || available < cost) return false;

            SetConfigFloat(SpentTiresKey, GetSpentTires() + cost);
            SetConfigFloat(isHpUpgrade ? HpUpgradeTierKey : AtkUpgradeTierKey, currentTier + 1);
            return true;
        }

        public static int GetTotalStarsEarned()
        {
            int total = 0;
            for (int i = 0; i < CampaignSceneKeys.Length; i++)
            {
                total += GetLevelStars(CampaignSceneKeys[i]);
            }
            return total;
        }

        public static int GetClearedLevelCount()
        {
            int count = 0;
            for (int i = 0; i < CampaignSceneKeys.Length; i++)
            {
                if (GetConfigFloat($"{CampaignSceneKeys[i]}_cleared", 0f) > 0.5f || GetLevelStars(CampaignSceneKeys[i]) > 0)
                {
                    count++;
                }
            }
            return count;
        }

        public static bool HasClaimedCampaign6PinkTireReward()
            => GetConfigFloat(Campaign6ClearRewardClaimedKey, 0f) > 0.5f;

        /// <summary>
        /// Trao thưởng Chiếc Lốp To Màu Hồng + 100 Lốp Hồng một lần khi hoàn thành cả 6 màn (chỉ cần >= 1 sao/màn).
        /// </summary>
        public static int TryGrantCampaign6PinkTireReward()
        {
            if (GetClearedLevelCount() < CampaignSceneKeys.Length) return 0;
            if (HasClaimedCampaign6PinkTireReward()) return 0;

            SetConfigFloat(Campaign6ClearRewardClaimedKey, 1f);
            SetConfigFloat(TotalCollectedTiresKey, GetTotalCollectedTires() + Campaign6ClearBonusTires);
            return Campaign6ClearBonusTires;
        }

        // ====================================================================
        // 6. CHẾ ĐỘ ĐỘ KHÓ (0 = THƯỜNG, 1 = KHÓ)
        // ====================================================================

        public const string DifficultyModeKey = "difficulty_mode";

        public static int GetDifficultyMode()
            => Mathf.Clamp(Mathf.RoundToInt(GetConfigFloat(DifficultyModeKey, 0f)), 0, 1);

        public static void SetDifficultyMode(int mode)
            => SetConfigFloat(DifficultyModeKey, Mathf.Clamp(mode, 0, 1));

        public static bool IsHardMode() => GetDifficultyMode() == 1;

        public static float GetMonsterHpMultiplier() => IsHardMode() ? 1.35f : 1.0f;
        public static float GetMonsterDmgMultiplier() => IsHardMode() ? 1.35f : 1.0f;
        public static float GetBossHpMultiplier() => IsHardMode() ? 1.40f : 1.0f;
        public static float GetBossDmgMultiplier() => IsHardMode() ? 1.30f : 1.0f;
        public static float GetEnemyCooldownMultiplier() => IsHardMode() ? 0.85f : 1.0f;
        public static float GetBossCooldownMultiplier() => IsHardMode() ? 0.45f : 1.0f;
        public static float GetHardTireDropMultiplier() => IsHardMode() ? 1.30f : 1.0f;

        // ====================================================================
        // 7. ĐẶT LẠI TOÀN BỘ TIẾN TRÌNH ĐỂ KIỂM THỬ TỪ ĐẦU
        // ====================================================================

        /// <summary>
        /// Đưa toàn bộ tiến trình màn chơi, sao, lốp thu thập, nâng cấp và thưởng 6 màn về 0
        /// (giữ nguyên âm lượng nhạc nền / hiệu ứng nếu preserveAudioSettings = true).
        /// </summary>
        public static void ResetProgressConfigAndSaveData(bool preserveAudioSettings = true)
        {
            if (preserveAudioSettings)
            {
                DatabaseManager.ExecuteNonQuery(
                    "DELETE FROM GameConfig WHERE config_key NOT IN (@bgm, @sfx, 'config_schema_version');",
                    new SqliteParameter("@bgm", AudioBgmVolumeKey),
                    new SqliteParameter("@sfx", AudioSfxVolumeKey));
            }
            else
            {
                DatabaseManager.ExecuteNonQuery(
                    "DELETE FROM GameConfig WHERE config_key <> 'config_schema_version';");
            }

            SavePlayerData(0, 0);
        }
    }
}
