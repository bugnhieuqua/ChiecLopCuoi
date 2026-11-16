using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChiecLopCuoi.NPC;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.Gameplay
{
    /// <summary>
    /// Quản lý trạng thái trận đấu đối kháng 2 bên:
    /// - Máu P1 (Cow Lốp) & P2 (Boss Nữ) + Thanh máu UI trên HUD
    /// - Thanh Combo P1/P2 & Thanh Nộ (Revenge P1/P2) trên HUD
    /// - Đồng hồ đếm ngược 90s (K.O. Timer)
    /// - Điều kiện thắng/thua: Hạ gục đối phương, chạm tượng căn cứ, hoặc hết giờ.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        public enum BattleState
        {
            Playing,
            EncounterCutscene,
            Finished
        }

        [Header("Battle State")]
        public BattleState State = BattleState.Playing;

        [Header("Round Timer")]
        public float roundTimeSeconds = 120f;
        private float _currentTimer;
        public bool isTimerPaused = false;
        public int SecondsRemaining => Mathf.CeilToInt(Mathf.Max(0f, _currentTimer));
        public int LastSecondsRemainingAtFinish { get; private set; }
        public float PlayerHealth01 => Mathf.Clamp01(p1CurrentHealth / Mathf.Max(1f, p1MaxHealth));
        public float EnemyHealth01 => Mathf.Clamp01(p2CurrentHealth / Mathf.Max(1f, p2MaxHealth));

        [Header("Health Config")]
        public const float BasePlayerMaxHealth = 150f;
        public float p1MaxHealth = 150f;
        public float p1CurrentHealth = 150f;
        public float p2MaxHealth = 180f;
        public float p2CurrentHealth = 180f;

        [Header("Combo & Revenge (Nộ) System")]
        public int p1ComboCount = 0;
        public int p1MaxCombo = 0;
        [Range(0f, 100f)] public float p1Revenge = 0f;
        public int p2ComboCount = 0;
        [Range(0f, 100f)] public float p2Revenge = 0f;
        public float comboResetSeconds = 3.8f;
        private float _p1ComboTimer = 0f;
        private float _p2ComboTimer = 0f;

        [Header("Base Statues (Win by reaching enemy statue)")]
        public float playerStatueX = -56.0f;
        public float enemyStatueX = 215.0f;
        public float statueTouchRadius = 3.2f;

        [Header("Combatants")]
        public CowLopPlayerController cowLop;
        public NpcCombatController npcFemale;

        [Header("HUD UI References")]
        public TMP_Text timerText;
        public RectTransform p1HealthFillRect;
        public RectTransform p2HealthFillRect;
        public TMP_Text p1HealthValueText;
        public TMP_Text p2HealthValueText;
        public TMP_Text p1NameText;
        public TMP_Text p2NameText;
        private float _p1InitialWidth = 288f;
        private float _p2InitialWidth = 288f;
        private bool _playerUpgradesApplied;

        [Header("HUD Combo & Revenge References")]
        public RectTransform p1ComboFillRect;
        public RectTransform p2ComboFillRect;
        public Image p1ComboFillImage;
        public Image p2ComboFillImage;
        private float _p1ComboMaxWidth = 136f;
        private float _p2ComboMaxWidth = 136f;
        public Image p1RevengeArcImage;
        public Image p2RevengeArcImage;
        public TMP_Text p1RevengeText;
        public TMP_Text p2RevengeText;
        public bool isBossHudVisible = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            AutoBindReferences();
            ApplyPersistedPlayerUpgrades();

            _currentTimer = roundTimeSeconds > 0f ? roundTimeSeconds : 120f;
            LastSecondsRemainingAtFinish = SecondsRemaining;
            p1CurrentHealth = p1MaxHealth;
            p2CurrentHealth = p2MaxHealth;
            p1ComboCount = 0;
            p1MaxCombo = 0;
            p1Revenge = 0f;
            p2ComboCount = 0;
            p2Revenge = 0f;

            SyncCharacterHealth();
            // Chỉ hiện thanh máu/combo của Boss (P2) khi gặp Boss
            bool bossAlreadyUnlocked = npcFemale != null && npcFemale.combatUnlocked;
            SetBossHUDVisible(bossAlreadyUnlocked);
            UpdateHealthBarsUI();
            UpdateComboAndRevengeUI();
            UpdateTimerUI();
        }

        private void ApplyPersistedPlayerUpgrades()
        {
            if (_playerUpgradesApplied) return;
            _playerUpgradesApplied = true;

            float hpBonus = ChiecLopCuoi.Database.GameRepository.GetHpUpgradeBonus();
            float atkBonus = ChiecLopCuoi.Database.GameRepository.GetAtkUpgradeBonus();

            // Máu cơ bản của Cow Lốp ở cả 6 màn chơi luôn là 150, chỉ tăng khi nâng cấp.
            p1MaxHealth = BasePlayerMaxHealth + Mathf.Max(0f, hpBonus);

            float bossHpMult = ChiecLopCuoi.Database.GameRepository.GetBossHpMultiplier();
            if (bossHpMult > 1.001f)
            {
                p2MaxHealth = Mathf.Round(p2MaxHealth * bossHpMult);
            }

            if (cowLop != null && atkBonus > 0f)
            {
                cowLop.attackDamage += atkBonus;
                cowLop.tireThrowDamage += atkBonus;
                cowLop.jumpSlamDamage += atkBonus;
                cowLop.tireSpinDamage += atkBonus;
            }
        }

        public void SetBossHUDVisible(bool visible)
        {
            isBossHudVisible = visible;
            var hudCanvas = GameObject.Find("HUD_Canvas");
            if (hudCanvas == null) return;
            var notebookTf = hudCanvas.transform.Find("HUD_Notebook");
            if (notebookTf == null) return;

            foreach (Transform child in notebookTf)
            {
                if (child == null) continue;
                if (child.name == "P2_Round_Slots")
                {
                    child.gameObject.SetActive(false);
                    continue;
                }
                if (child.name.StartsWith("P2_"))
                {
                    child.gameObject.SetActive(visible);
                }
            }
        }

        public void AutoBindReferences()
        {
            if (cowLop == null) cowLop = FindFirstObjectByType<CowLopPlayerController>();
            if (npcFemale == null) npcFemale = FindFirstObjectByType<NpcCombatController>();

            var hudCanvas = GameObject.Find("HUD_Canvas");
            if (hudCanvas != null)
            {
                var notebookTf = hudCanvas.transform.Find("HUD_Notebook");
                if (notebookTf != null)
                {
                    // Bind P1 & P2 Combo Fill Images (HUD_Notebook layout từ Level 1)
                    var p1ComboTf = notebookTf.Find("P1_Combo_Fill");
                    if (p1ComboTf != null)
                    {
                        p1ComboFillImage = p1ComboTf.GetComponent<Image>();
                        if (p1ComboFillImage != null)
                        {
                            p1ComboFillImage.type = Image.Type.Filled;
                            p1ComboFillImage.fillMethod = Image.FillMethod.Horizontal;
                            p1ComboFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                            p1ComboFillImage.fillAmount = 0f;
                        }
                    }

                    var p2ComboTf = notebookTf.Find("P2_Combo_Fill");
                    if (p2ComboTf != null)
                    {
                        p2ComboFillImage = p2ComboTf.GetComponent<Image>();
                        if (p2ComboFillImage != null)
                        {
                            p2ComboFillImage.type = Image.Type.Filled;
                            p2ComboFillImage.fillMethod = Image.FillMethod.Horizontal;
                            p2ComboFillImage.fillOrigin = (int)Image.OriginHorizontal.Right;
                            p2ComboFillImage.fillAmount = 0f;
                        }
                    }

                    var p1WingTf = notebookTf.Find("P1_Portrait_Wing");
                    if (p1WingTf != null) p1RevengeArcImage = p1WingTf.GetComponent<Image>();

                    var p2WingTf = notebookTf.Find("P2_Portrait_Wing");
                    if (p2WingTf != null) p2RevengeArcImage = p2WingTf.GetComponent<Image>();

                    var p1RevTxtTf = notebookTf.Find("P1_Revenge_Text");
                    if (p1RevTxtTf != null) p1RevengeText = p1RevTxtTf.GetComponent<TMP_Text>();

                    var p2RevTxtTf = notebookTf.Find("P2_Revenge_Text");
                    if (p2RevTxtTf != null) p2RevengeText = p2RevTxtTf.GetComponent<TMP_Text>();

                    EnsureCombatHudLabels(notebookTf);
                }
            }
        }

        private void EnsureCombatHudLabels(Transform notebookTf)
        {
            p1NameText = EnsureNotebookLabel(
                notebookTf,
                "P1_Name_Label",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(188f, -2f),
                new Vector2(320f, 26f),
                TextAlignmentOptions.Left,
                19f,
                new Color(1f, 0.92f, 0.58f, 1f));
            if (p1NameText != null) p1NameText.text = "Cow Lốp";

            p2NameText = EnsureNotebookLabel(
                notebookTf,
                "P2_Name_Label",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-188f, -2f),
                new Vector2(360f, 26f),
                TextAlignmentOptions.Right,
                19f,
                new Color(1f, 0.68f, 0.62f, 1f));
            if (p2NameText != null)
            {
                string bossTitle = npcFemale != null ? npcFemale.GetDisplayBossName() : "Nữ Áo Đỏ";
                p2NameText.text = ChiecLopCuoi.Database.GameRepository.IsHardMode()
                    ? $"{bossTitle} [KHÓ]"
                    : bossTitle;
            }

            p1HealthValueText = EnsureNotebookLabel(
                notebookTf,
                "P1_Health_Value",
                new Vector2(0f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(472f, -57f),
                new Vector2(240f, 34f),
                TextAlignmentOptions.Center,
                20f,
                Color.white);

            p2HealthValueText = EnsureNotebookLabel(
                notebookTf,
                "P2_Health_Value",
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-472f, -57f),
                new Vector2(240f, 34f),
                TextAlignmentOptions.Center,
                20f,
                Color.white);
        }

        private static TMP_Text EnsureNotebookLabel(
            Transform notebookTf,
            string childName,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 size,
            TextAlignmentOptions alignment,
            float fontSize,
            Color textColor)
        {
            if (notebookTf == null) return null;

            Transform existing = notebookTf.Find(childName);
            GameObject go = existing != null
                ? existing.gameObject
                : new GameObject(childName, typeof(RectTransform), typeof(TextMeshProUGUI));
            if (existing == null)
            {
                go.transform.SetParent(notebookTf, false);
                go.layer = 5;
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var tmp = go.GetComponent<TMP_Text>();
            ChiecLopCuoi.UI.ThemeFontManager.ApplyStoryFont(tmp);
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = alignment;
            tmp.color = textColor;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            if (tmp.gameObject.activeInHierarchy && tmp.fontSharedMaterial != null && tmp.fontSharedMaterial.HasProperty("_OutlineWidth"))
            {
                tmp.outlineWidth = 0.2f;
                tmp.outlineColor = new Color32(14, 10, 8, 255);
            }

            go.transform.SetAsLastSibling();
            return tmp;
        }

        private void SyncCharacterHealth()
        {
            if (cowLop != null)
            {
                cowLop.maxHealth = p1MaxHealth;
                cowLop.currentHealth = p1CurrentHealth;
            }
            if (npcFemale != null)
            {
                npcFemale.maxHealth = p2MaxHealth;
                npcFemale.currentHealth = p2CurrentHealth;
            }
        }

        private void Update()
        {
            if (State != BattleState.Playing) return;

            // 1. Đếm ngược thời gian
            if (!isTimerPaused)
            {
                _currentTimer -= Time.deltaTime;
                if (_currentTimer <= 0f)
                {
                    _currentTimer = 0f;
                    UpdateTimerUI();
                    OnTimeOut();
                    return;
                }
                UpdateTimerUI();
            }

            // 2. Giảm chuỗi Combo nếu quá thời gian không đánh trúng
            UpdateComboTimers();

            // 3. Kiểm tra chạm tượng đối phương
            CheckStatueVictory();
        }

        private void UpdateComboTimers()
        {
            bool changed = false;
            if (p1ComboCount > 0)
            {
                _p1ComboTimer -= Time.deltaTime;
                if (_p1ComboTimer <= 0f)
                {
                    p1ComboCount = 0;
                    changed = true;
                }
            }

            if (p2ComboCount > 0)
            {
                _p2ComboTimer -= Time.deltaTime;
                if (_p2ComboTimer <= 0f)
                {
                    p2ComboCount = 0;
                    changed = true;
                }
            }

            // Hiệu ứng nhấp nháy chữ REVENGE khi đầy 100% Nộ
            if (p1Revenge >= 100f && p1RevengeText != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 9f);
                p1RevengeText.color = Color.Lerp(new Color(1f, 0.85f, 0.2f, 1f), new Color(1f, 0.25f, 0.15f, 1f), pulse);
            }

            if (changed)
            {
                UpdateComboAndRevengeUI();
            }
        }

        public void RegisterComboHit(bool isPlayer1, float damageDealt)
        {
            if (isPlayer1)
            {
                p1ComboCount++;
                if (p1ComboCount > p1MaxCombo) p1MaxCombo = p1ComboCount;
                _p1ComboTimer = comboResetSeconds;
                AddRevenge(true, 9f);

                if (p1ComboCount >= 3 && cowLop != null)
                {
                    CombatFloatingText.Spawn(
                        cowLop.transform.position + new Vector3(0f, 1.95f, 0f),
                        $"COMBO x{p1ComboCount}!",
                        new Color(0.35f, 0.88f, 1f, 1f),
                        3.6f,
                        0.75f
                    );
                }
            }
            else
            {
                p2ComboCount++;
                _p2ComboTimer = comboResetSeconds;
                AddRevenge(false, 10f);
            }

            UpdateComboAndRevengeUI();
        }

        public float GetComboDamageMultiplier(bool isPlayer1)
        {
            int combo = isPlayer1 ? p1ComboCount : p2ComboCount;
            // Mỗi đòn combo tăng 4% sát thương, tối đa +28%
            return 1f + Mathf.Min(0.28f, combo * 0.04f);
        }

        public void AddRevenge(bool isPlayer1, float amount)
        {
            if (isPlayer1)
            {
                bool wasFull = p1Revenge >= 100f;
                p1Revenge = Mathf.Clamp(p1Revenge + amount, 0f, 100f);
                if (!wasFull && p1Revenge >= 100f && cowLop != null)
                {
                    CombatFloatingText.Spawn(
                        cowLop.transform.position + new Vector3(0f, 2.15f, 0f),
                        "NỘ ĐẦY 100%! [U / L CƯỜNG HÓA]",
                        new Color(1f, 0.35f, 0.15f, 1f),
                        4.4f,
                        1.25f
                    );
                }
            }
            else
            {
                p2Revenge = Mathf.Clamp(p2Revenge + amount, 0f, 100f);
            }

            UpdateComboAndRevengeUI();
        }

        public bool TryConsumeFullRevenge(bool isPlayer1)
        {
            if (isPlayer1)
            {
                if (p1Revenge >= 100f)
                {
                    p1Revenge = 0f;
                    UpdateComboAndRevengeUI();
                    return true;
                }
                return false;
            }
            else
            {
                if (p2Revenge >= 100f)
                {
                    p2Revenge = 0f;
                    UpdateComboAndRevengeUI();
                    return true;
                }
                return false;
            }
        }

        private void CheckStatueVictory()
        {
            // P1 (Cow Lốp) sau khi hạ Boss Nữ phải tiến về Tượng/Cổng cuối map mới chiến thắng
            if (cowLop != null && npcFemale != null && npcFemale.IsDead &&
                cowLop.transform.position.x >= enemyStatueX - statueTouchRadius)
            {
                EndBattle(playerWon: true, "CHIẾM TƯỢNG ĐỊCH THÀNH CÔNG!");
                return;
            }

            // P2 (NPC Nữ) chạm tượng bên Trái của Người chơi -> P1 Thua
            if (npcFemale != null && !npcFemale.IsDead &&
                Mathf.Abs(npcFemale.transform.position.x - playerStatueX) <= statueTouchRadius)
            {
                EndBattle(playerWon: false, "CĂN CỨ ĐÃ BỊ ĐỐI PHƯƠNG CHIẾM!");
            }
        }

        public void HealPlayer1(float amount)
        {
            if (State == BattleState.Finished || amount <= 0f) return;
            p1CurrentHealth = Mathf.Clamp(p1CurrentHealth + amount, 0f, p1MaxHealth);
            if (cowLop != null) cowLop.currentHealth = p1CurrentHealth;
            UpdateHealthBarsUI();
        }

        public void DamagePlayer1(float damage)
        {
            if (State != BattleState.Playing) return;

            // Né chiêu trên không: Khi Cow Lốp đang nhảy (!IsGrounded) thì miễn nhiễm sát thương hoàn toàn
            if (cowLop != null && !cowLop.IsGrounded)
            {
                CombatFloatingText.Spawn(
                    cowLop.transform.position + new Vector3(0f, 1.55f, 0f),
                    "NÉ TRÁNH!",
                    new Color(0.38f, 0.92f, 1f, 1f),
                    3.8f
                );
                return;
            }

            p1CurrentHealth = Mathf.Max(0f, p1CurrentHealth - damage);
            if (cowLop != null)
            {
                cowLop.currentHealth = p1CurrentHealth;
                CombatFloatingText.Spawn(
                    cowLop.transform.position + new Vector3(0f, 1.35f, 0f),
                    $"-{Mathf.RoundToInt(damage)}",
                    new Color(1f, 0.32f, 0.28f, 1f),
                    4.0f
                );
            }

            // Nhận sát thương cũng tích Nộ (Revenge)
            AddRevenge(true, 10f);
            UpdateHealthBarsUI();

            if (AudioManager.Instance != null) AudioManager.Instance.PlayHitImpact();

            if (p1CurrentHealth <= 0f)
            {
                if (cowLop != null) cowLop.OnDefeated();
                EndBattle(playerWon: false, "BẠN ĐÃ GỤC NGÃ TRƯỚC ĐỐI THỦ!");
            }
        }

        public void DamagePlayer2(float damage)
        {
            if (State != BattleState.Playing) return;

            p2CurrentHealth = Mathf.Max(0f, p2CurrentHealth - damage);
            if (npcFemale != null)
            {
                npcFemale.currentHealth = p2CurrentHealth;
                bool isBigHit = damage >= 25f;
                CombatFloatingText.Spawn(
                    npcFemale.transform.position + new Vector3(0f, 1.45f, 0f),
                    isBigHit ? $"-{Mathf.RoundToInt(damage)} CRIT!" : $"-{Mathf.RoundToInt(damage)}",
                    isBigHit ? new Color(1f, 0.25f, 0.15f, 1f) : new Color(1f, 0.85f, 0.22f, 1f),
                    isBigHit ? 4.8f : 4.1f
                );
            }

            AddRevenge(false, 8f);
            UpdateHealthBarsUI();

            if (AudioManager.Instance != null) AudioManager.Instance.PlayHitImpact();

            if (p2CurrentHealth <= 0f)
            {
                if (npcFemale != null) npcFemale.OnDefeated();
                SetBossHUDVisible(false);
                if (StageZoneGateManager.Instance != null)
                {
                    StageZoneGateManager.Instance.NotifyBossDefeatedGoToFinish();
                }
            }
        }

        private void OnTimeOut()
        {
            _currentTimer = 0f;
            EndBattle(playerWon: false, "HẾT GIỜ — BẠN ĐÃ THUA CUỘC (0 SAO, 0 LỐP)!");
        }

        public void EndBattle(bool playerWon, string reason)
        {
            if (State == BattleState.Finished) return;
            State = BattleState.Finished;
            LastSecondsRemainingAtFinish = playerWon ? SecondsRemaining : 0;

            Debug.Log($"<color=yellow>[BattleManager]</color> KẾT THÚC TRẬN ĐẤU! PlayerWon={playerWon} | Lý do: {reason}");

            // Chiến thắng: Cow Lốp phát animation Victory (xoay lốp ăn mừng)
            if (playerWon && cowLop != null)
            {
                cowLop.OnVictory();
            }

            if (DuelEncounterManager.Instance != null)
            {
                DuelEncounterManager.Instance.ShowMatchResult(playerWon, reason);
            }
            else if (AudioManager.Instance != null)
            {
                // Không có màn kết quả duel (scene khác): vẫn phát nhạc thắng/thua.
                if (playerWon) AudioManager.Instance.PlayVictory();
                else AudioManager.Instance.PlayDefeat();
            }
        }

        public void UpdateHealthBarsUI()
        {
            if (p1HealthFillRect != null)
            {
                float ratio1 = Mathf.Clamp01(p1CurrentHealth / Mathf.Max(1f, p1MaxHealth));
                p1HealthFillRect.sizeDelta = new Vector2(_p1InitialWidth * ratio1, p1HealthFillRect.sizeDelta.y);
            }

            if (p2HealthFillRect != null)
            {
                float ratio2 = Mathf.Clamp01(p2CurrentHealth / Mathf.Max(1f, p2MaxHealth));
                p2HealthFillRect.sizeDelta = new Vector2(_p2InitialWidth * ratio2, p2HealthFillRect.sizeDelta.y);
            }

            if (p1HealthValueText != null)
            {
                p1HealthValueText.text = $"{Mathf.Max(0, Mathf.CeilToInt(p1CurrentHealth))} / {Mathf.Max(1, Mathf.CeilToInt(p1MaxHealth))}";
            }

            if (p2HealthValueText != null)
            {
                p2HealthValueText.text = $"{Mathf.Max(0, Mathf.CeilToInt(p2CurrentHealth))} / {Mathf.Max(1, Mathf.CeilToInt(p2MaxHealth))}";
            }
        }

        public void UpdateComboAndRevengeUI()
        {
            float comboRatio1 = Mathf.Clamp01(p1ComboCount / 10f);
            float comboRatio2 = Mathf.Clamp01(p2ComboCount / 10f);

            // 1. P1 & P2 Combo Fill (Image.fillAmount + RectTransform fallback)
            if (p1ComboFillImage != null)
            {
                p1ComboFillImage.fillAmount = comboRatio1;
            }
            else if (p1ComboFillRect != null)
            {
                p1ComboFillRect.sizeDelta = new Vector2(_p1ComboMaxWidth * comboRatio1, p1ComboFillRect.sizeDelta.y);
            }

            if (p2ComboFillImage != null)
            {
                p2ComboFillImage.fillAmount = comboRatio2;
            }
            else if (p2ComboFillRect != null)
            {
                p2ComboFillRect.sizeDelta = new Vector2(_p2ComboMaxWidth * comboRatio2, p2ComboFillRect.sizeDelta.y);
            }

            // 2. P1 & P2 Revenge Arc & Text
            if (p1RevengeArcImage != null)
            {
                float r1 = Mathf.Clamp01(p1Revenge / 100f);
                p1RevengeArcImage.color = r1 >= 1f
                    ? new Color(1f, 0.38f, 0.12f, 1f)
                    : Color.Lerp(new Color(0.55f, 0.75f, 0.55f, 0.85f), new Color(0.2f, 1f, 0.35f, 1f), r1);
            }

            if (p1RevengeText != null && p1Revenge < 100f)
            {
                p1RevengeText.color = new Color(0.55f, 1f, 0.45f, 0.9f);
            }

            if (p2RevengeArcImage != null)
            {
                float r2 = Mathf.Clamp01(p2Revenge / 100f);
                p2RevengeArcImage.color = r2 >= 1f
                    ? new Color(1f, 0.25f, 0.25f, 1f)
                    : Color.Lerp(new Color(0.55f, 0.75f, 0.55f, 0.85f), new Color(0.2f, 1f, 0.35f, 1f), r2);
            }
        }

        private void UpdateTimerUI()
        {
            if (timerText != null)
            {
                int sec = Mathf.CeilToInt(_currentTimer);
                timerText.text = sec.ToString("000");
            }
        }
    }
}
