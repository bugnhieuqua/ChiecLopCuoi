using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.Database;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Màn tổng kết kết thúc chiến dịch khi Cow Lốp hoàn thành toàn bộ 6 màn (dù chỉ đạt 1 sao/màn).
    /// Hiển thị 3 sao, Chiếc Lốp To Màu Hồng và thưởng +30 Lốp Hồng cùng 2 nút điều hướng.
    /// </summary>
    public class VictoryFinalePanel : MonoBehaviour
    {
        private GameObject _root;
        private RectTransform _pinkTireRt;
        private TMP_Text _rewardText;
        private int _lastBonusGranted;

        public static VictoryFinalePanel ShowOrCreate(Transform parentCanvas)
        {
            if (parentCanvas == null) return null;

            Transform existing = parentCanvas.Find("Victory_Finale_Panel");
            VictoryFinalePanel panel = existing != null
                ? existing.GetComponent<VictoryFinalePanel>()
                : null;

            if (panel == null)
            {
                if (existing != null)
                {
                    panel = existing.gameObject.AddComponent<VictoryFinalePanel>();
                    panel.EnsureBindings();
                }
                else
                {
                    GameObject overlay = AudioSettingsModal.CreateStoneModalOverlay(
                        parentCanvas,
                        "Victory_Finale_Panel",
                        "CHIẾN THẮNG!",
                        new Vector2(720f, 540f),
                        out RectTransform boardRt);

                    panel = overlay.AddComponent<VictoryFinalePanel>();
                    panel.BuildBody(overlay, boardRt);
                }
            }

            panel.Open();
            return panel;
        }

        private void BuildBody(GameObject overlay, RectTransform boardRt)
        {
            _root = overlay;
            EnsureCenterShowcase(boardRt);

            AudioSettingsModal.CreateIconButton(
                boardRt,
                "Btn_To_LevelSelect",
                new Vector2(0.5f, 0f),
                new Vector2(-72f, 54f),
                new Vector2(68f, 68f),
                "UI/Btn_Menu_Wood",
                () => LoadSceneClean("LevelSelectScene"));

            AudioSettingsModal.CreateIconButton(
                boardRt,
                "Btn_To_Home",
                new Vector2(0.5f, 0f),
                new Vector2(72f, 54f),
                new Vector2(68f, 68f),
                "UI/Btn_Home_Wood",
                () => LoadSceneClean("LoginScene"));
        }

        private void Awake()
        {
            EnsureBindings();
        }

        private void Update()
        {
            if (_pinkTireRt != null && gameObject.activeInHierarchy)
            {
                float s = 1f + 0.06f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.8f));
                _pinkTireRt.localScale = new Vector3(s, s, 1f);
            }
        }

        public void EnsureBindings()
        {
            if (_root == null) _root = gameObject;
            Transform board = transform.Find("Stone_Board");
            if (board == null) return;

            bool hasBakedPinkShowcase = board.Find("Center_Showcase/Emblem_Pink_Tire") != null;
            if (!hasBakedPinkShowcase && board is RectTransform boardRt)
            {
                boardRt.sizeDelta = new Vector2(720f, 540f);
            }

            HideOrDestroyLegacyChild(board, "Epilogue_Text");
            HideOrDestroyLegacyChild(board, "Stats_Bg");
            HideOrDestroyLegacyChild(board, "Campaign_Stats_Text");
            EnsureCenterShowcase(board);

            Button btnLevelSelect = board.Find("Btn_To_LevelSelect")?.GetComponent<Button>();
            if (btnLevelSelect != null)
            {
                if (!hasBakedPinkShowcase && btnLevelSelect.transform is RectTransform lrt) lrt.anchoredPosition = new Vector2(-72f, 54f);
                btnLevelSelect.onClick.RemoveAllListeners();
                btnLevelSelect.onClick.AddListener(() => LoadSceneClean("LevelSelectScene"));
            }
            Button btnHome = board.Find("Btn_To_Home")?.GetComponent<Button>();
            if (btnHome != null)
            {
                if (!hasBakedPinkShowcase && btnHome.transform is RectTransform hrt) hrt.anchoredPosition = new Vector2(72f, 54f);
                btnHome.onClick.RemoveAllListeners();
                btnHome.onClick.AddListener(() => LoadSceneClean("LoginScene"));
            }
        }

        private static void HideOrDestroyLegacyChild(Transform board, string childName)
        {
            Transform child = board.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        private void EnsureCenterShowcase(Transform board)
        {
            Transform existingShowcase = board.Find("Center_Showcase");
            if (existingShowcase != null)
            {
                Transform oldTire = existingShowcase.Find("Emblem_Tire");
                if (oldTire != null) oldTire.gameObject.SetActive(false);
                Transform existingPink = existingShowcase.Find("Emblem_Pink_Tire");
                if (existingPink != null)
                {
                    _pinkTireRt = existingPink as RectTransform;
                    _rewardText = existingShowcase.Find("Reward_Pink_Tire_Text")?.GetComponent<TMP_Text>();
                    return;
                }
            }

            RectTransform srt;
            if (existingShowcase == null)
            {
                var showcaseGo = new GameObject("Center_Showcase", typeof(RectTransform));
                showcaseGo.transform.SetParent(board, false);
                showcaseGo.layer = 5;
                srt = showcaseGo.GetComponent<RectTransform>();
            }
            else
            {
                srt = (RectTransform)existingShowcase;
            }

            srt.anchorMin = new Vector2(0.5f, 0.5f);
            srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(0f, 14f);
            srt.sizeDelta = new Vector2(560f, 300f);

            Sprite starSprite = LoadGoldStarSprite(board);
            if (srt.Find("Star_Left") == null)
            {
                CreateDecoImage(srt, "Star_Left", starSprite, new Vector2(-72f, 104f), new Vector2(46f, 46f), 14f, Color.white);
                CreateDecoImage(srt, "Star_Right", starSprite, new Vector2(72f, 104f), new Vector2(46f, 46f), -14f, Color.white);
                CreateDecoImage(srt, "Star_Center", starSprite, new Vector2(0f, 114f), new Vector2(60f, 60f), 0f, Color.white);
            }

            Sprite pinkTireSprite = Resources.Load<Sprite>("UI/Victory_Pink_Tire") ?? Resources.Load<Sprite>("UI/Pink_Tire");
            Color tireTint = Color.white;
            if (pinkTireSprite == null)
            {
                pinkTireSprite = Resources.Load<Sprite>("Tires/tire_02");
                tireTint = new Color(1f, 0.44f, 0.78f, 1f);
            }

            Image bigPinkTire = CreateDecoImage(srt, "Emblem_Pink_Tire", pinkTireSprite, new Vector2(0f, -4f), new Vector2(180f, 180f), 0f, tireTint);
            _pinkTireRt = bigPinkTire.rectTransform;

            // Hàng hiển thị thưởng +30 Lốp Hồng!
            CreateDecoImage(srt, "Icon_Mini_Pink_Tire", pinkTireSprite, new Vector2(-82.51f, -109f), new Vector2(71.77f, 36f), 0f, tireTint);

            var txtGo = new GameObject("Reward_Pink_Tire_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(srt, false);
            txtGo.layer = 5;
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 0.5f);
            trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.anchoredPosition = new Vector2(300.25f, -109f);
            trt.sizeDelta = new Vector2(697.75f, 42f);

            _rewardText = txtGo.GetComponent<TMP_Text>();
            ThemeFontManager.ApplyStoryFont(_rewardText);
            _rewardText.fontSize = 23f;
            _rewardText.fontStyle = FontStyles.Bold;
            _rewardText.alignment = TextAlignmentOptions.Left;
            _rewardText.color = new Color32(182, 24, 108, 255);
        }

        private static Image CreateDecoImage(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size, float zAngle, Color tint)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localRotation = Quaternion.Euler(0f, 0f, zAngle);

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = sprite != null ? tint : new Color(1f, 0.45f, 0.78f, 1f);
            return img;
        }

        private static Sprite LoadGoldStarSprite(Transform board)
        {
            Transform koStar = board.parent?.parent?.Find("Result_KO_Panel/Result_Stone_Board/Result_Stars_Row/Star_1");
            Image koImg = koStar != null ? koStar.GetComponent<Image>() : null;
            if (koImg != null && koImg.sprite != null) return koImg.sprite;
            return Resources.Load<Sprite>("UI/star_gold");
        }

        public void Open()
        {
            EnsureBindings();
            _lastBonusGranted = GameRepository.TryGrantCampaign6PinkTireReward();
            if (_rewardText != null)
            {
                int rewardAmount = _lastBonusGranted > 0
                    ? _lastBonusGranted
                    : GameRepository.Campaign6ClearBonusTires;
                _rewardText.text = $"+{rewardAmount} Lốp Hồng!";
            }

            if (_root != null)
            {
                _root.transform.SetAsLastSibling();
                _root.SetActive(true);
            }
        }

        private static void LoadSceneClean(string sceneName)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
