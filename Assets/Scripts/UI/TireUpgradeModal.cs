using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.Database;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Bảng dùng Lốp thu thập để nâng cấp Sinh lực (HP) và Sức đánh (ATK) cho Cow Lốp tại LevelSelectScene.
    /// Lưu tiến độ nâng cấp vào SQLite thông qua GameRepository.
    /// </summary>
    public class TireUpgradeModal : MonoBehaviour
    {
        private GameObject _root;
        private TMP_Text _balanceText;
        private TMP_Text _hpStatsText;
        private TMP_Text _atkStatsText;
        private Button _btnUpgradeHp;
        private Button _btnUpgradeAtk;
        private TMP_Text _btnHpLabel;
        private TMP_Text _btnAtkLabel;
        private TMP_Text _feedbackText;
        private Image[] _hpPips;
        private Image[] _atkPips;
        private Action _onUpgradedCallback;

        public static TireUpgradeModal ShowOrCreate(Transform parentCanvas, Action onUpgraded = null)
        {
            if (parentCanvas == null) return null;

            Transform existing = parentCanvas.Find("Tire_Upgrade_Modal");
            TireUpgradeModal modal = existing != null
                ? existing.GetComponent<TireUpgradeModal>()
                : null;

            if (modal == null)
            {
                if (existing != null)
                {
                    modal = existing.gameObject.AddComponent<TireUpgradeModal>();
                    modal.EnsureBindings();
                }
                else
                {
                    GameObject overlay = AudioSettingsModal.CreateStoneModalOverlay(
                        parentCanvas,
                        "Tire_Upgrade_Modal",
                        "NÂNG CẤP",
                        new Vector2(660f, 436f),
                        out RectTransform boardRt);

                    modal = overlay.AddComponent<TireUpgradeModal>();
                    modal.BuildBody(overlay, boardRt);
                }
            }

            modal._onUpgradedCallback = onUpgraded;
            modal.Open();
            return modal;
        }

        private void BuildBody(GameObject overlay, RectTransform boardRt)
        {
            _root = overlay;

            AudioSettingsModal.CreateIconButton(
                boardRt,
                "Btn_Close_Upgrade",
                new Vector2(1f, 1f),
                new Vector2(-38f, -38f),
                new Vector2(56f, 56f),
                "UI/Btn_Close_Wood",
                Close);

            var balRow = new GameObject("Balance_Strip", typeof(RectTransform));
            balRow.transform.SetParent(boardRt, false);
            balRow.layer = 5;
            var balRt = balRow.GetComponent<RectTransform>();
            balRt.anchorMin = new Vector2(0.5f, 1f);
            balRt.anchorMax = new Vector2(0.5f, 1f);
            balRt.pivot = new Vector2(0.5f, 0.5f);
            balRt.anchoredPosition = new Vector2(0f, -84f);

            var balIconGo = new GameObject("Icon_Tire", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            balIconGo.transform.SetParent(balRow.transform, false);
            balIconGo.layer = 5;
            var iimg = balIconGo.GetComponent<Image>();
            iimg.raycastTarget = false;

            _balanceText = AudioSettingsModal.CreateText(
                balRow.transform,
                "Balance_Text",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(40f, 34f),
                "",
                24f,
                new Color32(46, 20, 8, 255),
                TextAlignmentOptions.Left,
                useLoginFont: false,
                readableBodyFont: true);

            ConfigureBalanceStrip(balRow.transform, _balanceText);

            CreateUpgradeCard(
                boardRt,
                "HP_Upgrade_Card",
                new Vector2(0f, 20f),
                "Sinh lực (HP)",
                out _hpStatsText,
                out _hpPips,
                out _btnUpgradeHp,
                out _btnHpLabel,
                () => OnPurchaseClicked(isHpUpgrade: true));

            CreateUpgradeCard(
                boardRt,
                "ATK_Upgrade_Card",
                new Vector2(0f, -72f),
                "Sức đánh (ATK)",
                out _atkStatsText,
                out _atkPips,
                out _btnUpgradeAtk,
                out _btnAtkLabel,
                () => OnPurchaseClicked(isHpUpgrade: false));

            _feedbackText = AudioSettingsModal.CreateText(
                boardRt,
                "Feedback_Text",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 44f),
                new Vector2(400f, 26f),
                "",
                16.5f,
                new Color(0.65f, 0.14f, 0.10f, 1f),
                TextAlignmentOptions.Center,
                useLoginFont: false,
                readableBodyFont: true);
            _feedbackText.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        }

        private static void CreateUpgradeCard(
            Transform parent,
            string cardName,
            Vector2 anchoredPos,
            string title,
            out TMP_Text statsText,
            out Image[] tierPips,
            out Button upgradeBtn,
            out TMP_Text upgradeBtnLabel,
            UnityEngine.Events.UnityAction onClick)
        {
            var cardGo = new GameObject(cardName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardGo.transform.SetParent(parent, false);
            cardGo.layer = 5;

            var cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = anchoredPos;
            cardRt.sizeDelta = new Vector2(388f, 82f);

            var cardImg = cardGo.GetComponent<Image>();
            Sprite pillSp = Resources.Load<Sprite>("UI/UI_Capsule_Dark");
            if (pillSp != null)
            {
                cardImg.sprite = pillSp;
                cardImg.type = Image.Type.Sliced;
            }
            cardImg.color = new Color(0.95f, 0.90f, 0.78f, 0.58f);

            TMP_Text titleTmp = AudioSettingsModal.CreateText(
                cardGo.transform,
                "Title",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(18f, -10f),
                new Vector2(280f, 26f),
                title,
                20f,
                new Color32(46, 20, 6, 255),
                TextAlignmentOptions.Left,
                useLoginFont: false,
                readableBodyFont: true);
            titleTmp.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);

            tierPips = new Image[GameRepository.MaxUpgradeTier];
            for (int i = 0; i < tierPips.Length; i++)
            {
                var pipGo = new GameObject($"Tier_Pip_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                pipGo.transform.SetParent(cardGo.transform, false);
                pipGo.layer = 5;
                var pipRt = pipGo.GetComponent<RectTransform>();
                pipRt.anchorMin = new Vector2(0f, 1f);
                pipRt.anchorMax = new Vector2(0f, 1f);
                pipRt.pivot = new Vector2(0f, 1f);
                pipRt.anchoredPosition = new Vector2(18f + i * 38f, -38f);
                pipRt.sizeDelta = new Vector2(30f, 8f);
                var pipImg = pipGo.GetComponent<Image>();
                if (pillSp != null) { pipImg.sprite = pillSp; pipImg.type = Image.Type.Sliced; }
                pipImg.color = new Color(0.45f, 0.40f, 0.34f, 0.55f);
                pipImg.raycastTarget = false;
                tierPips[i] = pipImg;
            }

            statsText = AudioSettingsModal.CreateText(
                cardGo.transform,
                "Stats",
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(18f, 10f),
                new Vector2(285f, 26f),
                "",
                17.5f,
                new Color32(58, 36, 18, 255),
                TextAlignmentOptions.Left,
                useLoginFont: false,
                readableBodyFont: true);
            statsText.GetComponent<RectTransform>().pivot = new Vector2(0f, 0f);

            upgradeBtn = AudioSettingsModal.CreateIconButton(
                cardGo.transform,
                "Btn_Buy",
                new Vector2(1f, 0.5f),
                new Vector2(-46f, 9f),
                new Vector2(54f, 54f),
                "UI/Btn_Upgrade_Wood",
                onClick);

            upgradeBtnLabel = AudioSettingsModal.CreateText(
                cardGo.transform,
                "Cost_Label",
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-46f, -29f),
                new Vector2(86f, 22f),
                "2 Lốp",
                15f,
                new Color32(34, 88, 28, 255),
                TextAlignmentOptions.Center,
                useLoginFont: false,
                readableBodyFont: true);
            upgradeBtnLabel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        }

        private void Awake()
        {
            EnsureBindings();
        }

        private static void ConfigureBalanceStrip(Transform balStrip, TMP_Text balanceText)
        {
            if (balStrip == null) return;

            if (balStrip is RectTransform balRt)
            {
                balRt.anchorMin = new Vector2(0.5f, 1f);
                balRt.anchorMax = new Vector2(0.5f, 1f);
                balRt.pivot = new Vector2(0.5f, 0.5f);
                balRt.anchoredPosition = new Vector2(0f, -84f);
                balRt.sizeDelta = new Vector2(240f, 38f);
            }

            var layout = balStrip.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = balStrip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var iconImg = balStrip.Find("Icon_Tire")?.GetComponent<Image>();
            if (iconImg != null)
            {
                Sprite tireSp = Resources.Load<Sprite>("Tires/tire_02");
                if (tireSp != null) iconImg.sprite = tireSp;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;

                if (iconImg.rectTransform != null)
                {
                    iconImg.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    iconImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    iconImg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    iconImg.rectTransform.sizeDelta = new Vector2(30f, 30f);
                }

                var iconLe = iconImg.GetComponent<LayoutElement>();
                if (iconLe == null) iconLe = iconImg.gameObject.AddComponent<LayoutElement>();
                iconLe.minWidth = 30f;
                iconLe.preferredWidth = 30f;
                iconLe.minHeight = 30f;
                iconLe.preferredHeight = 30f;
            }

            if (balanceText != null)
            {
                balanceText.alignment = TextAlignmentOptions.Left;
                balanceText.enableAutoSizing = false;
                balanceText.fontSize = 24f;
                balanceText.textWrappingMode = TextWrappingModes.NoWrap;

                var brt = balanceText.GetComponent<RectTransform>();
                if (brt != null)
                {
                    brt.anchorMin = new Vector2(0.5f, 0.5f);
                    brt.anchorMax = new Vector2(0.5f, 0.5f);
                    brt.pivot = new Vector2(0.5f, 0.5f);
                    brt.sizeDelta = new Vector2(40f, 34f);
                }

                var fitter = balanceText.GetComponent<ContentSizeFitter>();
                if (fitter == null) fitter = balanceText.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
        }

        public void EnsureBindings()
        {
            if (_root == null) _root = gameObject;
            Transform board = transform.Find("Stone_Board");
            if (board == null) return;

            Transform balStrip = board.Find("Balance_Strip");
            if (balStrip != null)
            {
                if (_balanceText == null) _balanceText = balStrip.Find("Balance_Text")?.GetComponent<TMP_Text>();
                ConfigureBalanceStrip(balStrip, _balanceText);
            }
            if (_feedbackText == null) _feedbackText = board.Find("Feedback_Text")?.GetComponent<TMP_Text>();

            Transform hpCard = board.Find("HP_Upgrade_Card");
            if (hpCard != null)
            {
                if (_hpStatsText == null) _hpStatsText = hpCard.Find("Stats")?.GetComponent<TMP_Text>();
                if (_btnUpgradeHp == null) _btnUpgradeHp = hpCard.Find("Btn_Buy")?.GetComponent<Button>();
                if (_btnHpLabel == null) _btnHpLabel = hpCard.Find("Cost_Label")?.GetComponent<TMP_Text>();
                if (_hpPips == null || _hpPips.Length == 0) _hpPips = CollectPips(hpCard);
                if (_btnUpgradeHp != null)
                {
                    _btnUpgradeHp.onClick.RemoveAllListeners();
                    _btnUpgradeHp.onClick.AddListener(() => OnPurchaseClicked(isHpUpgrade: true));
                }
            }

            Transform atkCard = board.Find("ATK_Upgrade_Card");
            if (atkCard != null)
            {
                if (_atkStatsText == null) _atkStatsText = atkCard.Find("Stats")?.GetComponent<TMP_Text>();
                if (_btnUpgradeAtk == null) _btnUpgradeAtk = atkCard.Find("Btn_Buy")?.GetComponent<Button>();
                if (_btnAtkLabel == null) _btnAtkLabel = atkCard.Find("Cost_Label")?.GetComponent<TMP_Text>();
                if (_atkPips == null || _atkPips.Length == 0) _atkPips = CollectPips(atkCard);
                if (_btnUpgradeAtk != null)
                {
                    _btnUpgradeAtk.onClick.RemoveAllListeners();
                    _btnUpgradeAtk.onClick.AddListener(() => OnPurchaseClicked(isHpUpgrade: false));
                }
            }

            Button closeBtn = board.Find("Btn_Close_Upgrade")?.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(Close);
            }
        }

        private static Image[] CollectPips(Transform card)
        {
            var arr = new Image[GameRepository.MaxUpgradeTier];
            Sprite sampleSprite = card.Find("Tier_Pip_0")?.GetComponent<Image>()?.sprite;
            for (int i = 0; i < arr.Length; i++)
            {
                Transform pipTf = card.Find($"Tier_Pip_{i}");
                if (pipTf == null)
                {
                    var pipGo = new GameObject($"Tier_Pip_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    pipGo.transform.SetParent(card, false);
                    pipGo.layer = 5;
                    var img = pipGo.GetComponent<Image>();
                    if (sampleSprite != null)
                    {
                        img.sprite = sampleSprite;
                        img.type = Image.Type.Sliced;
                    }
                    img.raycastTarget = false;
                    pipTf = pipGo.transform;
                }

                if (pipTf is RectTransform pipRt)
                {
                    pipRt.anchorMin = new Vector2(0f, 1f);
                    pipRt.anchorMax = new Vector2(0f, 1f);
                    pipRt.pivot = new Vector2(0f, 1f);
                    pipRt.anchoredPosition = new Vector2(18f + i * 38f, -38f);
                    pipRt.sizeDelta = new Vector2(30f, 8f);
                }

                arr[i] = pipTf.GetComponent<Image>();
            }
            return arr;
        }

        private void Update()
        {
            if (_root != null && _root.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        public void Open()
        {
            EnsureBindings();
            if (_root != null)
            {
                _root.transform.SetAsLastSibling();
                _root.SetActive(true);
            }
            if (_feedbackText != null) _feedbackText.text = string.Empty;
            RefreshUI();
        }

        public void Close()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            if (_root != null) _root.SetActive(false);
        }

        private void OnPurchaseClicked(bool isHpUpgrade)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();

            if (GameRepository.TryPurchaseUpgrade(isHpUpgrade))
            {
                if (_feedbackText != null)
                {
                    _feedbackText.color = new Color(0.12f, 0.45f, 0.16f, 1f);
                    _feedbackText.text = isHpUpgrade
                        ? "Đã nâng cấp Sinh Lực cho Cow Lốp!"
                        : "Đã nâng cấp Sức Đánh cho Cow Lốp!";
                }
                RefreshUI();
                _onUpgradedCallback?.Invoke();
            }
            else
            {
                if (_feedbackText != null)
                {
                    _feedbackText.color = new Color(0.68f, 0.14f, 0.10f, 1f);
                    _feedbackText.text = "Không đủ Lốp khả dụng để nâng cấp!";
                }
            }
        }

        public void RefreshUI()
        {
            int avail = GameRepository.GetAvailableTires();
            int hpTier = GameRepository.GetHpUpgradeTier();
            int atkTier = GameRepository.GetAtkUpgradeTier();

            if (_balanceText != null)
            {
                _balanceText.text = avail.ToString();
                if (_balanceText.transform.parent is RectTransform stripRt)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(stripRt);
                }
            }

            UpdateTrackRow(
                hpTier,
                GameRepository.GetHpBonusForTier(hpTier),
                hpTier < GameRepository.MaxUpgradeTier ? GameRepository.GetHpBonusForTier(hpTier + 1) : GameRepository.GetHpBonusForTier(hpTier),
                "HP",
                avail,
                _hpStatsText,
                _hpPips,
                _btnUpgradeHp,
                _btnHpLabel);

            UpdateTrackRow(
                atkTier,
                GameRepository.GetAtkBonusForTier(atkTier),
                atkTier < GameRepository.MaxUpgradeTier ? GameRepository.GetAtkBonusForTier(atkTier + 1) : GameRepository.GetAtkBonusForTier(atkTier),
                "DMG",
                avail,
                _atkStatsText,
                _atkPips,
                _btnUpgradeAtk,
                _btnAtkLabel);
        }

        private static void UpdateTrackRow(
            int tier,
            float currentBonus,
            float nextBonus,
            string unit,
            int availableTires,
            TMP_Text statsText,
            Image[] pips,
            Button buyButton,
            TMP_Text buyLabel)
        {
            if (pips != null)
            {
                for (int i = 0; i < pips.Length; i++)
                {
                    if (pips[i] == null) continue;
                    pips[i].color = i < tier
                        ? new Color32(246, 198, 66, 255)
                        : new Color(0.45f, 0.40f, 0.34f, 0.65f);
                }
            }

            bool maxed = tier >= GameRepository.MaxUpgradeTier;
            if (statsText != null)
            {
                statsText.text = maxed
                    ? $"Tối đa: +{currentBonus:0} {unit}"
                    : $"+{currentBonus:0} → +{nextBonus:0} {unit}";
            }

            if (buyButton != null && buyLabel != null)
            {
                Image btnImg = buyButton.GetComponent<Image>();
                if (maxed)
                {
                    buyLabel.text = "TỐI ĐA";
                    buyLabel.color = new Color32(102, 74, 34, 255);
                    if (btnImg != null) btnImg.color = new Color(0.65f, 0.60f, 0.52f, 0.55f);
                    buyButton.interactable = false;
                }
                else
                {
                    int cost = GameRepository.GetNextUpgradeCost(tier);
                    bool canAfford = availableTires >= cost;
                    buyLabel.text = $"{cost} Lốp";
                    buyButton.interactable = canAfford;
                    buyLabel.color = canAfford
                        ? new Color32(28, 92, 24, 255)
                        : new Color32(118, 86, 58, 225);
                    if (btnImg != null)
                    {
                        btnImg.color = canAfford
                            ? Color.white
                            : new Color(0.62f, 0.54f, 0.46f, 0.92f);
                    }
                }
            }
        }
    }
}
