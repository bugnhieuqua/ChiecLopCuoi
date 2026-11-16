using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Lớp phủ Spotlight đơn giản (4 tấm tối bao quanh vùng RectTransform mục tiêu,
    /// không dùng custom stencil shader để tương thích 100% WebGL):
    /// - Vùng spotlight bên trong giữ nguyên độ sáng và cho phép click xuyên thẳng.
    /// - Viền nhấp nháy + nhịp zoom nhẹ thu hút chú ý vào mục tiêu.
    /// - Thanh thông báo gọn với nút Tiếp và Skip.
    /// </summary>
    public class TutorialSpotlightOverlay : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _canvasRt;
        private readonly Image[] _dimPanels = new Image[4]; // Top, Bottom, Left, Right
        private RectTransform _highlightFrame;
        private readonly Image[] _frameEdges = new Image[4];

        private RectTransform _bannerRt;
        private TMP_Text _titleText;
        private TMP_Text _bodyText;
        private Button _nextButton;
        private TMP_Text _nextBtnLabel;
        private Button _skipButton;

        private RectTransform _targetRt;
        private Camera _targetCanvasCam;
        private Vector3 _targetOrigScale = Vector3.one;
        private float _targetPadding = 16f;
        private bool _enableTargetZoomPulse = true;
        private bool _dimVisible = true;

        private Camera _zoomCam;
        private float _origCamFov = -1f;
        private float _targetCamFovOffset;

        private Action _onNext;
        private Action _onSkip;

        private static Sprite _whiteSprite;
        private readonly Vector3[] _corners = new Vector3[4];

        public bool IsShowing => _canvas != null && _canvas.gameObject.activeSelf;

        public void Initialize(Action onNext, Action onSkip)
        {
            _onNext = onNext;
            _onSkip = onSkip;
            EnsureUI();
        }

        public void ShowStep(
            RectTransform target,
            string title,
            string message,
            bool showNextButton,
            string nextLabel = "Tiếp >>",
            float padding = 16f,
            bool zoomTarget = true,
            float cameraFovZoomDelta = 0f)
        {
            EnsureUI();
            RestoreCurrentTargetVisuals();

            _targetRt = target;
            _targetPadding = padding;
            _enableTargetZoomPulse = zoomTarget;
            _targetCanvasCam = ResolveSourceCanvasCamera(target);

            if (_targetRt != null)
            {
                _targetOrigScale = _targetRt.localScale;
            }

            if (cameraFovZoomDelta < -0.1f && Camera.main != null && !Camera.main.orthographic)
            {
                _zoomCam = Camera.main;
                _origCamFov = _zoomCam.fieldOfView;
                _targetCamFovOffset = cameraFovZoomDelta;
            }
            else
            {
                _zoomCam = null;
                _origCamFov = -1f;
                _targetCamFovOffset = 0f;
            }

            if (_titleText != null) _titleText.text = title ?? string.Empty;
            if (_bodyText != null) _bodyText.text = message ?? string.Empty;

            if (_nextButton != null)
            {
                _nextButton.gameObject.SetActive(showNextButton);
                if (_nextBtnLabel != null)
                {
                    _nextBtnLabel.text = string.IsNullOrEmpty(nextLabel) ? "Tiếp >>" : nextLabel;
                }
            }

            _canvas.gameObject.SetActive(true);
            _dimVisible = true;
            UpdateSpotlightGeometry(0f);
        }

        public void SetDimmersSuppressed(bool suppressed)
        {
            _dimVisible = !suppressed;
            for (int i = 0; i < _dimPanels.Length; i++)
            {
                if (_dimPanels[i] != null) _dimPanels[i].gameObject.SetActive(_dimVisible);
            }
            if (_highlightFrame != null) _highlightFrame.gameObject.SetActive(_dimVisible && _targetRt != null);
        }

        public void Hide()
        {
            RestoreCurrentTargetVisuals();
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            RestoreCurrentTargetVisuals();
        }

        private void LateUpdate()
        {
            if (_canvas == null || !_canvas.gameObject.activeSelf) return;
            UpdateSpotlightGeometry(Time.unscaledTime);
        }

        private void UpdateSpotlightGeometry(float unscaledTime)
        {
            if (_canvasRt == null) return;

            float pulse01 = 0.5f + 0.5f * Mathf.Sin(unscaledTime * 5.2f);

            // Zoom nhẹ trên chính mục tiêu UI + khung viền nhấp nháy
            if (_targetRt != null && _targetRt.gameObject.activeInHierarchy)
            {
                if (_enableTargetZoomPulse)
                {
                    float s = 1f + 0.055f * pulse01;
                    _targetRt.localScale = _targetOrigScale * s;
                }
                if (_zoomCam != null && _origCamFov > 0f)
                {
                    _zoomCam.fieldOfView = Mathf.Lerp(
                        _zoomCam.fieldOfView,
                        _origCamFov + _targetCamFovOffset,
                        Mathf.Clamp01(Time.unscaledDeltaTime * 6f));
                }

                if (TryGetTargetRectInOverlay(_targetRt, _targetCanvasCam, out Rect hole))
                {
                    ApplyFourDimPanels(hole, _dimVisible);
                    ApplyHighlightFrame(hole, pulse01, _dimVisible);
                    PositionBannerSmartly(hole);
                    return;
                }
            }

            // Fallback khi chưa có target cụ thể: phủ tối nhẹ toàn màn hình
            ApplyFullDim(_dimVisible);
            if (_highlightFrame != null) _highlightFrame.gameObject.SetActive(false);
            PositionBannerTop();
        }

        private bool TryGetTargetRectInOverlay(RectTransform target, Camera srcCam, out Rect result)
        {
            target.GetWorldCorners(_corners);
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            for (int i = 0; i < 4; i++)
            {
                Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(srcCam, _corners[i]);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, screenPt, null, out Vector2 localPt))
                {
                    if (localPt.x < minX) minX = localPt.x;
                    if (localPt.x > maxX) maxX = localPt.x;
                    if (localPt.y < minY) minY = localPt.y;
                    if (localPt.y > maxY) maxY = localPt.y;
                }
            }

            if (minX >= maxX || minY >= maxY)
            {
                result = default;
                return false;
            }

            minX -= _targetPadding;
            maxX += _targetPadding;
            minY -= _targetPadding;
            maxY += _targetPadding;

            Rect cRect = _canvasRt.rect;
            minX = Mathf.Clamp(minX, cRect.xMin, cRect.xMax);
            maxX = Mathf.Clamp(maxX, cRect.xMin, cRect.xMax);
            minY = Mathf.Clamp(minY, cRect.yMin, cRect.yMax);
            maxY = Mathf.Clamp(maxY, cRect.yMin, cRect.yMax);

            result = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return result.width > 4f && result.height > 4f;
        }

        private void ApplyFourDimPanels(Rect hole, bool visible)
        {
            Rect full = _canvasRt.rect;

            // Top: trên lỗ hổng
            SetDimRect(_dimPanels[0], visible, full.xMin, hole.yMax, full.xMax, full.yMax);
            // Bottom: dưới lỗ hổng
            SetDimRect(_dimPanels[1], visible, full.xMin, full.yMin, full.xMax, hole.yMin);
            // Left: bên trái lỗ hổng (kẹp giữa yMin..yMax của lỗ)
            SetDimRect(_dimPanels[2], visible, full.xMin, hole.yMin, hole.xMin, hole.yMax);
            // Right: bên phải lỗ hổng
            SetDimRect(_dimPanels[3], visible, hole.xMax, hole.yMin, full.xMax, hole.yMax);
        }

        private void ApplyFullDim(bool visible)
        {
            Rect full = _canvasRt.rect;
            SetDimRect(_dimPanels[0], visible, full.xMin, full.yMin, full.xMax, full.yMax);
            for (int i = 1; i < 4; i++)
            {
                if (_dimPanels[i] != null) _dimPanels[i].gameObject.SetActive(false);
            }
        }

        private static void SetDimRect(Image img, bool visible, float xMin, float yMin, float xMax, float yMax)
        {
            if (img == null) return;
            bool hasArea = visible && (xMax - xMin) > 0.5f && (yMax - yMin) > 0.5f;
            img.gameObject.SetActive(hasArea);
            if (!hasArea) return;

            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
            rt.sizeDelta = new Vector2(xMax - xMin, yMax - yMin);
        }

        private void ApplyHighlightFrame(Rect hole, float pulse01, bool visible)
        {
            if (_highlightFrame == null) return;
            _highlightFrame.gameObject.SetActive(visible);
            if (!visible) return;

            float expand = Mathf.Lerp(2f, 10f, pulse01);
            _highlightFrame.anchoredPosition = hole.center;
            _highlightFrame.sizeDelta = new Vector2(hole.width + expand, hole.height + expand);

            Color blinkColor = Color.Lerp(
                new Color(1f, 0.84f, 0.22f, 0.92f),
                new Color(1f, 0.45f, 0.82f, 1f),
                pulse01);
            for (int i = 0; i < _frameEdges.Length; i++)
            {
                if (_frameEdges[i] != null) _frameEdges[i].color = blinkColor;
            }
        }

        private void PositionBannerSmartly(Rect hole)
        {
            if (_bannerRt == null || _canvasRt == null) return;

            _bannerRt.anchorMin = new Vector2(0.5f, 0.5f);
            _bannerRt.anchorMax = new Vector2(0.5f, 0.5f);
            _bannerRt.pivot = new Vector2(0.5f, 0.5f);

            float halfW = _canvasRt.rect.width * 0.5f;
            float halfH = _canvasRt.rect.height * 0.5f;
            float bw = _bannerRt.sizeDelta.x;
            float bh = _bannerRt.sizeDelta.y;
            const float gap = 14f;
            const float marginX = 24f;
            const float marginY = 18f;

            // Ưu tiên đặt sát ngay PHÍA TRÊN khung highlight (cách 14px); nếu đụng mép trên thì đặt sát ngay PHÍA DƯỚI.
            float yAbove = hole.yMax + gap + bh * 0.5f;
            float yBelow = hole.yMin - gap - bh * 0.5f;
            float chosenY;
            if (yAbove + bh * 0.5f <= halfH - marginY)
            {
                chosenY = yAbove;
            }
            else if (yBelow - bh * 0.5f >= -halfH + marginY)
            {
                chosenY = yBelow;
            }
            else
            {
                chosenY = Mathf.Clamp(yAbove, -halfH + bh * 0.5f + marginY, halfH - bh * 0.5f - marginY);
            }

            float clampedX = Mathf.Clamp(hole.center.x, -halfW + bw * 0.5f + marginX, halfW - bw * 0.5f - marginX);
            _bannerRt.anchoredPosition = new Vector2(clampedX, chosenY);
        }

        private void PositionBannerTop()
        {
            if (_bannerRt == null) return;
            _bannerRt.anchorMin = new Vector2(0.5f, 0.5f);
            _bannerRt.anchorMax = new Vector2(0.5f, 0.5f);
            _bannerRt.pivot = new Vector2(0.5f, 0.5f);
            float halfH = _canvasRt != null ? _canvasRt.rect.height * 0.5f : 540f;
            _bannerRt.anchoredPosition = new Vector2(0f, halfH - _bannerRt.sizeDelta.y * 0.5f - 28f);
        }

        private void RestoreCurrentTargetVisuals()
        {
            if (_targetRt != null)
            {
                _targetRt.localScale = _targetOrigScale;
                _targetRt = null;
            }
            if (_zoomCam != null && _origCamFov > 0f)
            {
                _zoomCam.fieldOfView = _origCamFov;
                _zoomCam = null;
                _origCamFov = -1f;
            }
        }

        private static Camera ResolveSourceCanvasCamera(RectTransform target)
        {
            if (target == null) return null;
            Canvas c = target.GetComponentInParent<Canvas>();
            if (c == null || c.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return c.worldCamera != null ? c.worldCamera : Camera.main;
        }

        private void EnsureUI()
        {
            if (_canvas != null) return;

            var canvasGo = new GameObject("Tutorial_Spotlight_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 400;
            _canvasRt = canvasGo.GetComponent<RectTransform>();

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            string[] dimNames = { "Dim_Top", "Dim_Bottom", "Dim_Left", "Dim_Right" };
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject(dimNames[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(_canvasRt, false);
                var img = go.GetComponent<Image>();
                img.sprite = GetWhiteSprite();
                img.color = new Color(0.03f, 0.04f, 0.09f, 0.74f);
                img.raycastTarget = true; // Chặn click nhầm vùng tối, chỉ lọt qua lỗ spotlight
                _dimPanels[i] = img;
            }

            // Khung viền sáng nhấp nháy quanh vùng spotlight
            var frameGo = new GameObject("Highlight_Frame", typeof(RectTransform));
            frameGo.transform.SetParent(_canvasRt, false);
            _highlightFrame = frameGo.GetComponent<RectTransform>();
            _highlightFrame.anchorMin = new Vector2(0.5f, 0.5f);
            _highlightFrame.anchorMax = new Vector2(0.5f, 0.5f);

            const float thickness = 5f;
            _frameEdges[0] = CreateFrameEdge(_highlightFrame, "Edge_Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, thickness));
            _frameEdges[1] = CreateFrameEdge(_highlightFrame, "Edge_Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, thickness));
            _frameEdges[2] = CreateFrameEdge(_highlightFrame, "Edge_Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(thickness, 0f));
            _frameEdges[3] = CreateFrameEdge(_highlightFrame, "Edge_Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(thickness, 0f));

            // Thanh thông báo hướng dẫn (nền trắng hình học đặt sát khung highlight)
            var bannerGo = new GameObject("Top_Notice_Banner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bannerGo.transform.SetParent(_canvasRt, false);
            _bannerRt = bannerGo.GetComponent<RectTransform>();
            _bannerRt.anchorMin = new Vector2(0.5f, 0.5f);
            _bannerRt.anchorMax = new Vector2(0.5f, 0.5f);
            _bannerRt.pivot = new Vector2(0.5f, 0.5f);
            _bannerRt.anchoredPosition = new Vector2(0f, 320f);
            _bannerRt.sizeDelta = new Vector2(900f, 134f);

            var bannerBg = bannerGo.GetComponent<Image>();
            Sprite whiteCardSprite = Resources.Load<Sprite>("UI/White_Card_Bg");
            bannerBg.sprite = whiteCardSprite != null ? whiteCardSprite : GetWhiteSprite();
            bannerBg.color = new Color(1f, 1f, 1f, 0.98f);
            bannerBg.raycastTarget = true;

            var borderOutline = bannerGo.AddComponent<Outline>();
            borderOutline.effectColor = new Color32(56, 152, 226, 225);
            borderOutline.effectDistance = new Vector2(2.5f, -2.5f);

            _titleText = CreateBannerText(_bannerRt, "Txt_Tutorial_Title", new Vector2(-105f, 36f), new Vector2(640f, 36f), 25f, new Color32(14, 72, 148, 255));
            ThemeFontManager.ApplyLoginFont(_titleText);

            _bodyText = CreateBannerText(_bannerRt, "Txt_Tutorial_Body", new Vector2(-105f, -16f), new Vector2(640f, 66f), 21f, new Color32(30, 41, 59, 255));
            ThemeFontManager.ApplyStoryFont(_bodyText);

            _nextButton = CreateActionButton(_bannerRt, "Btn_Tutorial_Next", "Tiếp >>", new Vector2(330f, -22f), new Vector2(176f, 54f),
                new Color32(34, 118, 210, 255), Color.white, () => _onNext?.Invoke(), out _nextBtnLabel);

            _skipButton = CreateActionButton(_bannerRt, "Btn_Tutorial_Skip", "Skip", new Vector2(368f, 38f), new Vector2(104f, 40f),
                new Color32(52, 128, 206, 255), Color.white, () => _onSkip?.Invoke(), out _);

            _canvas.gameObject.SetActive(false);
        }

        private static Image CreateFrameEdge(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = sizeDelta;
            var img = go.GetComponent<Image>();
            img.sprite = GetWhiteSprite();
            img.raycastTarget = false;
            return img;
        }

        private static TMP_Text CreateBannerText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = go.GetComponent<TMP_Text>();
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.color = color;
            return tmp;
        }

        private static Button CreateActionButton(
            Transform parent, string name, string labelText, Vector2 pos, Vector2 size,
            Color32 bgColor, Color textColor, Action onClick, out TMP_Text labelTmp)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            Sprite btnMapSprite = Resources.Load<Sprite>("UI/Btn_Map_Blue");
            if (btnMapSprite != null)
            {
                img.sprite = btnMapSprite;
                img.color = Color.white;
            }
            else
            {
                img.sprite = GetWhiteSprite();
                img.color = bgColor;
            }

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGo.transform.SetParent(go.transform, false);
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            labelTmp = lblGo.GetComponent<TMP_Text>();
            ThemeFontManager.ApplyStoryFont(labelTmp);
            labelTmp.text = labelText;
            labelTmp.fontSize = 20f;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = textColor;
            var txtOutline = lblGo.AddComponent<Outline>();
            txtOutline.effectColor = new Color32(10, 32, 74, 215);
            txtOutline.effectDistance = new Vector2(1.2f, -1.2f);
            return btn;
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }
    }
}
