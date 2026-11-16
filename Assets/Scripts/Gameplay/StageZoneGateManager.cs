using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ChiecLopCuoi.Audio;
using ChiecLopCuoi.UI;

namespace ChiecLopCuoi.Gameplay
{
    /// <summary>
    /// Quản lý sự kiện 4 Chặng Chốt Chặn (Ambush Gates) trên suốt chiều dài 280m của Map Level 2.
    /// Người chơi cần tiêu diệt hết cụm quái canh gác ở từng chặng để mở khóa đường tiến tới Đấu Trường Boss Nữ.
    /// </summary>
    public class StageZoneGateManager : MonoBehaviour
    {
        public static StageZoneGateManager Instance { get; private set; }

        [Serializable]
        public class StageZone
        {
            public string zoneName;
            public float minX;
            public float maxX;
            public float gateLimitX;
            [NonSerialized] public List<MonsterCombatController> monsters = new List<MonsterCombatController>();
            [NonSerialized] public bool isCleared;
        }

        [Header("Cấu hình 4 Chặng Chốt Chặn (280m Map: -60 đến +220)")]
        public List<StageZone> zones = new List<StageZone>
        {
            new StageZone { zoneName = "CHẶNG 1/4: CỔNG LÀNG ĐÊM", minX = -60f, maxX = 18f, gateLimitX = 15f },
            new StageZone { zoneName = "CHẶNG 2/4: XÓM KHUNG CỬI", minX = 18f, maxX = 75f, gateLimitX = 72f },
            new StageZone { zoneName = "CHẶNG 3/4: RỪNG TRE TỐI", minX = 75f, maxX = 135f, gateLimitX = 132f },
            new StageZone { zoneName = "CHẶNG 4/4: ĐỒN TIỀN TIÊU", minX = 135f, maxX = 184f, gateLimitX = 182f }
        };

        public int CurrentZoneIndex { get; private set; }
        public int TotalMonstersCount { get; private set; }
        public int TotalMonstersKilled { get; private set; }
        public bool AllZonesCleared { get; private set; }

        private CowLopPlayerController _player;
        private TextMeshProUGUI _objectiveBannerText;
        private Image _objectiveBannerBg;
        private CanvasGroup _bannerCanvasGroup;
        private RectTransform _bannerRect;
        private string _lastBannerContent = string.Empty;
        private float _bannerPopScale = 1f;
        private float _bannerAlpha = 0f;
        private float _lastGateWarningTime = -10f;
        private float _bannerHighlightTimer;

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
            _player = FindFirstObjectByType<CowLopPlayerController>();
            RegisterAllMonstersIntoZones();
            EnsureObjectiveBannerUI();
            RefreshZoneProgression();
        }

        private void Update()
        {
            UpdateBannerVisuals();
            CheckGateWarningPrompt();
        }

        private void UpdateBannerVisuals()
        {
            if (_bannerCanvasGroup != null && _bannerAlpha < 1f)
            {
                _bannerAlpha = Mathf.MoveTowards(_bannerAlpha, 1f, Time.deltaTime * 3.6f);
                _bannerCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, _bannerAlpha);
            }

            if (_bannerRect != null && Mathf.Abs(_bannerPopScale - 1f) > 0.001f)
            {
                _bannerPopScale = Mathf.Lerp(_bannerPopScale, 1f, 1f - Mathf.Exp(-9f * Time.deltaTime));
                _bannerRect.localScale = new Vector3(_bannerPopScale, _bannerPopScale, 1f);
            }

            if (_objectiveBannerBg == null) return;

            bool hasCustomSprite = _objectiveBannerBg.sprite != null;
            Color baseColor = hasCustomSprite
                ? new Color(0.14f, 0.16f, 0.22f, 0.96f)
                : new Color(0.06f, 0.08f, 0.12f, 0.92f);
            Color glowColor = hasCustomSprite
                ? new Color(0.24f, 0.48f, 0.30f, 0.98f)
                : new Color(0.14f, 0.44f, 0.20f, 0.95f);

            if (_bannerHighlightTimer > 0f)
            {
                _bannerHighlightTimer -= Time.deltaTime;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4.5f);
                _objectiveBannerBg.color = Color.Lerp(baseColor, glowColor, pulse * 0.7f);
            }
            else
            {
                _objectiveBannerBg.color = Color.Lerp(_objectiveBannerBg.color, baseColor, 1f - Mathf.Exp(-8f * Time.deltaTime));
            }
        }

        public void RegisterAllMonstersIntoZones()
        {
            foreach (var z in zones)
            {
                z.monsters.Clear();
                z.isCleared = false;
            }

            var allMonsters = FindObjectsByType<MonsterCombatController>(FindObjectsSortMode.None);
            TotalMonstersCount = allMonsters.Length;
            TotalMonstersKilled = 0;

            foreach (var m in allMonsters)
            {
                if (m == null) continue;
                float mx = m.transform.position.x;
                bool assigned = false;

                for (int i = 0; i < zones.Count; i++)
                {
                    if (mx >= zones[i].minX && mx < zones[i].maxX)
                    {
                        zones[i].monsters.Add(m);
                        assigned = true;
                        break;
                    }
                }

                if (!assigned && zones.Count > 0)
                {
                    zones[zones.Count - 1].monsters.Add(m);
                }
            }
        }

        public void NotifyMonsterDefeated(MonsterCombatController monster)
        {
            TotalMonstersKilled++;
            RefreshZoneProgression();
        }

        public void NotifyBossDefeatedGoToFinish()
        {
            AllZonesCleared = true;
            if (zones != null) CurrentZoneIndex = zones.Count;
            _bannerHighlightTimer = 4.5f;
            UpdateBannerText("<color=#69F0AE>ĐÃ HẠ BOSS!</color> Tiến về <color=#FFD54F>CUỐI MAP >>></color> để chiến thắng!");

            if (_player != null)
            {
                CombatFloatingText.Spawn(
                    _player.transform.position + Vector3.up * 2.2f,
                    "TIẾN VỀ CUỐI MAP >>>",
                    new Color(0.35f, 1f, 0.55f, 1f),
                    5.2f,
                    1.6f);
            }
        }

        private void RefreshZoneProgression()
        {
            if (BattleManager.Instance != null && BattleManager.Instance.npcFemale != null && BattleManager.Instance.npcFemale.IsDead)
            {
                AllZonesCleared = true;
                return;
            }

            if (zones == null || zones.Count == 0)
            {
                AllZonesCleared = true;
                return;
            }

            for (int i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                int aliveCount = CountAliveMonsters(zone);

                if (aliveCount > 0)
                {
                    CurrentZoneIndex = i;
                    AllZonesCleared = false;
                    UpdateBannerText($"Chặng {i + 1}/{zones.Count}  |  Quái còn: <color=#FF5252>{aliveCount}/{zone.monsters.Count}</color>  (Diệt: {TotalMonstersKilled}/{TotalMonstersCount})");
                    return;
                }

                if (!zone.isCleared)
                {
                    zone.isCleared = true;
                    OnZoneCleared(i, zone);
                }
            }

            AllZonesCleared = true;
            CurrentZoneIndex = zones.Count;
            UpdateBannerText("<color=#69F0AE>Đã quét sạch quái!</color> Tiến về Đông đấu <color=#FFD54F>Boss Nữ</color> >>>");
        }

        private int CountAliveMonsters(StageZone zone)
        {
            int alive = 0;
            for (int i = 0; i < zone.monsters.Count; i++)
            {
                var m = zone.monsters[i];
                if (m != null && !m.IsDead)
                {
                    alive++;
                }
            }
            return alive;
        }

        private void OnZoneCleared(int zoneIdx, StageZone zone)
        {
            if ( TotalMonstersKilled == 0 ) return; // Tránh báo lúc khởi tạo nếu zone trống

            _bannerHighlightTimer = 3.2f;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGateCleared();
            }

            if (_player != null)
            {
                string msg = zoneIdx < zones.Count - 1
                    ? $"MỞ ĐƯỜNG CHẶNG {zoneIdx + 2}! >>>"
                    : "ĐƯỜNG TỚI ĐẤU TRƯỜNG BOSS ĐÃ MỞ! >>>";
                CombatFloatingText.Spawn(_player.transform.position + Vector3.up * 2.1f, msg, new Color(0.35f, 1f, 0.55f, 1f), 4.8f, 1.4f);
            }
        }

        private void CheckGateWarningPrompt()
        {
            if (AllZonesCleared || _player == null || CurrentZoneIndex >= zones.Count) return;

            var activeZone = zones[CurrentZoneIndex];
            if (_player.transform.position.x >= activeZone.gateLimitX - 0.6f && Time.time - _lastGateWarningTime > 2.2f)
            {
                _lastGateWarningTime = Time.time;
                int alive = CountAliveMonsters(activeZone);
                CombatFloatingText.Spawn(
                    _player.transform.position + Vector3.up * 1.9f,
                    $"CHỐT CHẶN: Diệt nốt {alive} quái để đi tiếp!",
                    new Color(1f, 0.45f, 0.25f, 1f),
                    3.8f,
                    1.1f
                );
            }
        }

        /// <summary>
        /// Trả về giới hạn X tối đa hiện tại mà Cow Lốp và Camera được phép đi tới.
        /// </summary>
        public float GetClampedMaxX(float defaultStageMaxX)
        {
            if (AllZonesCleared || zones == null || CurrentZoneIndex >= zones.Count)
            {
                return defaultStageMaxX;
            }
            return Mathf.Min(defaultStageMaxX, zones[CurrentZoneIndex].gateLimitX);
        }

        private void EnsureObjectiveBannerUI()
        {
            var hudCanvas = GameObject.Find("HUD_Canvas");
            if (hudCanvas == null) return;

            GameObject bannerGo;
            Transform existing = hudCanvas.transform.Find("Zone_Objective_Banner");
            bool isNew = existing == null;
            if (!isNew)
            {
                bannerGo = existing.gameObject;
            }
            else
            {
                bannerGo = new GameObject("Zone_Objective_Banner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bannerGo.transform.SetParent(hudCanvas.transform, false);
            }

            _bannerRect = bannerGo.GetComponent<RectTransform>();

            // Nếu banner đã có sẵn trong Scene (được chỉnh tay trong Inspector) thì
            // giữ nguyên bố cục; chỉ căn tự động khi tạo mới.
            if (isNew)
            {
                _bannerRect.anchorMin = new Vector2(0.5f, 1f);
                _bannerRect.anchorMax = new Vector2(0.5f, 1f);
                _bannerRect.pivot = new Vector2(0.5f, 1f);

                // Căn X khớp với Timer_Frame (nếu có) và đặt Y bên dưới đáy Timer_Frame để không đè khung
                float centerX = 0f;
                float bannerY = -168f;
                Transform timerFrameTf = hudCanvas.transform.Find("HUD_Notebook/Timer_Frame");
                if (timerFrameTf != null)
                {
                    RectTransform frameRt = timerFrameTf.GetComponent<RectTransform>();
                    RectTransform canvasRt = hudCanvas.GetComponent<RectTransform>();
                    if (frameRt != null && canvasRt != null)
                    {
                        Vector3 worldBottom = frameRt.TransformPoint(new Vector2(frameRt.rect.center.x, frameRt.rect.yMin));
                        Vector3 localBottom = canvasRt.InverseTransformPoint(worldBottom);
                        centerX = localBottom.x;
                        float topOfCanvas = canvasRt.rect.yMax;
                        bannerY = Mathf.Min(-168f, (localBottom.y - topOfCanvas) - 8f);
                    }
                }

                _bannerRect.anchoredPosition = new Vector2(centerX, bannerY);
                _bannerRect.sizeDelta = new Vector2(1100f, 64f);
            }

            _bannerCanvasGroup = bannerGo.GetComponent<CanvasGroup>();
            if (_bannerCanvasGroup == null) _bannerCanvasGroup = bannerGo.AddComponent<CanvasGroup>();
            _bannerAlpha = 1f;
            _bannerCanvasGroup.alpha = 1f;
            _bannerCanvasGroup.blocksRaycasts = false;
            _bannerCanvasGroup.interactable = false;

            Sprite bannerSprite = Resources.Load<Sprite>("UI/ui_stage_banner");
            _objectiveBannerBg = bannerGo.GetComponent<Image>();
            if (bannerSprite != null)
            {
                _objectiveBannerBg.sprite = bannerSprite;
                _objectiveBannerBg.type = Image.Type.Sliced;
                _objectiveBannerBg.color = new Color(0.14f, 0.16f, 0.22f, 0.96f);
            }
            else
            {
                _objectiveBannerBg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
            }
            _objectiveBannerBg.raycastTarget = false;

            Transform existingTextTf = bannerGo.transform.Find("Objective_Text");
            bool isNewText = existingTextTf == null;
            GameObject textGo = !isNewText
                ? existingTextTf.gameObject
                : new GameObject("Objective_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            if (isNewText) textGo.transform.SetParent(bannerGo.transform, false);

            var trt = textGo.GetComponent<RectTransform>();
            if (isNew || isNewText)
            {
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(44f, 4f);
                trt.offsetMax = new Vector2(-44f, -4f);
            }

            _objectiveBannerText = textGo.GetComponent<TextMeshProUGUI>();
            if (isNew || isNewText || _objectiveBannerText.font == null)
            {
                ChiecLopCuoi.UI.ThemeFontManager.ApplyGlobalUiFont(_objectiveBannerText);
            }
            _objectiveBannerText.textWrappingMode = TextWrappingModes.NoWrap;
            _objectiveBannerText.overflowMode = TextOverflowModes.Ellipsis;
            _objectiveBannerText.enableAutoSizing = true;
            _objectiveBannerText.fontSizeMin = 16f;
            _objectiveBannerText.fontSizeMax = 24f;
            _objectiveBannerText.fontSize = 22f;
            _objectiveBannerText.characterSpacing = 0.5f;
            _objectiveBannerText.alignment = TextAlignmentOptions.Center;
            _objectiveBannerText.color = new Color(0.96f, 0.96f, 0.98f, 1f);
            _objectiveBannerText.fontStyle = FontStyles.Normal;
            _objectiveBannerText.outlineWidth = 0.22f;
            _objectiveBannerText.outlineColor = new Color32(8, 10, 16, 255);
            _objectiveBannerText.raycastTarget = false;
        }

        private void UpdateBannerText(string content)
        {
            if (_objectiveBannerText != null)
            {
                _objectiveBannerText.text = content;
                if (content != _lastBannerContent)
                {
                    _lastBannerContent = content;
                    _bannerPopScale = 1.045f;
                }
            }
        }
    }
}
