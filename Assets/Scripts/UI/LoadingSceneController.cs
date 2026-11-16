using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Điều phối màn hình Loading (HUB_load) toàn trang trước khi vào các màn chơi:
    /// - Nền Loading_Bg full trang (tải sẵn trong Resources/UI/Loading_Bg.jpg để tương thích WebGL không CORS).
    /// - Đường load tiến độ màu hồng cùng Chiếc Lốp Màu Hồng lăn xoay phía trên từ 0% -> 100%.
    /// </summary>
    public class LoadingSceneController : MonoBehaviour
    {
        public const string LoadingSceneName = "LoadingScene";
        public static string PendingTargetScene = "SampleScene";
        public static float MinimumDisplaySeconds = 1.25f;

        private const float TrackWidth = 520f;
        private const float TrackHeight = 32f;

        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private RectTransform pinkTireRect;
        [SerializeField] private TMP_Text percentText;

        private static Sprite _whiteSprite;

        public float CurrentProgress { get; private set; }
        public bool IsActivated { get; private set; }

        /// <summary>
        /// Chuyển sang LoadingScene để tải bất đồng bộ màn chơi đích; fallback về load thẳng nếu chưa có LoadingScene.
        /// </summary>
        public static void LoadWithLoading(string targetSceneName)
        {
            PendingTargetScene = string.IsNullOrEmpty(targetSceneName) ? "SampleScene" : targetSceneName;
            Time.timeScale = 1f;

            if (Application.CanStreamedLevelBeLoaded(LoadingSceneName))
            {
                SceneManager.LoadScene(LoadingSceneName);
            }
            else
            {
                SceneManager.LoadScene(PendingTargetScene);
            }
        }

        private void Awake()
        {
            EnsureHubLoadHierarchy();
        }

        private void Start()
        {
            Time.timeScale = 1f;
            StartCoroutine(RunAsyncLoadRoutine());
        }

        public void EnsureHubLoadHierarchy()
        {
            GameObject canvasGo = GameObject.Find("HUB_load");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("HUB_load", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            }

            var canvas = canvasGo.GetComponent<Canvas>();
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = mainCam;
                canvas.planeDistance = 5f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Transform root = canvasGo.transform;

            // 1. Nền đen đồng bộ với studio tối của ảnh Loading_Bg
            Image backdrop = EnsureChildImage(root, "Img_White_Backdrop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            backdrop.sprite = GetOrCreateWhiteSprite();
            backdrop.color = Color.black;
            backdrop.raycastTarget = false;

            // 2. Ảnh nền Loading_Bg giữ đúng tỉ lệ khung hình trên nền đen
            if (backgroundImage == null)
            {
                backgroundImage = EnsureChildImage(root, "Img_Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            Sprite bgSprite = Resources.Load<Sprite>("UI/Loading_Bg");
            if (bgSprite != null)
            {
                backgroundImage.sprite = bgSprite;
                backgroundImage.color = Color.white;
                backgroundImage.preserveAspect = true;
                var fitter = backgroundImage.GetComponent<AspectRatioFitter>() ?? backgroundImage.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                if (bgSprite.rect.height > 0f)
                {
                    fitter.aspectRatio = bgSprite.rect.width / bgSprite.rect.height;
                }
            }
            backgroundImage.raycastTarget = false;

            // 3. Cụm Đường load + Lốp xe màu hồng dời sang giữa-phải màn hình (x = +430, y = 0)
            Transform groupTf = root.Find("Loading_Track_Group");
            RectTransform groupRt;
            if (groupTf == null)
            {
                var groupGo = new GameObject("Loading_Track_Group", typeof(RectTransform));
                groupGo.transform.SetParent(root, false);
                groupRt = groupGo.GetComponent<RectTransform>();
            }
            else
            {
                groupRt = (RectTransform)groupTf;
            }
            groupRt.anchorMin = new Vector2(0.5f, 0.5f);
            groupRt.anchorMax = new Vector2(0.5f, 0.5f);
            groupRt.pivot = new Vector2(0.5f, 0.5f);
            groupRt.anchoredPosition = new Vector2(430f, 0f);
            groupRt.sizeDelta = new Vector2(TrackWidth, TrackHeight);

            Image borderImg = EnsureCenteredImage(groupRt, "Img_Load_Border", Vector2.zero, new Vector2(TrackWidth + 10f, TrackHeight + 10f));
            borderImg.sprite = GetOrCreateWhiteSprite();
            borderImg.color = new Color32(222, 86, 158, 255);
            borderImg.raycastTarget = false;

            Image trackImg = EnsureCenteredImage(groupRt, "Img_Load_Track", Vector2.zero, new Vector2(TrackWidth, TrackHeight));
            trackImg.sprite = GetOrCreateWhiteSprite();
            trackImg.color = new Color32(252, 236, 245, 255);
            trackImg.raycastTarget = false;

            if (fillImage == null)
            {
                fillImage = EnsureCenteredImage(groupRt, "Img_Load_Fill", Vector2.zero, new Vector2(TrackWidth, TrackHeight));
            }
            fillImage.sprite = GetOrCreateWhiteSprite();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;
            fillImage.color = new Color32(255, 96, 178, 255);
            fillImage.raycastTarget = false;

            if (pinkTireRect == null)
            {
                Image tireImg = EnsureCenteredImage(groupRt, "Img_Pink_Tire", new Vector2(-TrackWidth * 0.5f, 58f), new Vector2(88f, 88f));
                Sprite tireSprite = Resources.Load<Sprite>("UI/Pink_Tire");
                if (tireSprite != null)
                {
                    tireImg.sprite = tireSprite;
                    tireImg.color = Color.white;
                }
                else
                {
                    tireImg.sprite = Resources.Load<Sprite>("Tires/tire_02");
                    tireImg.color = new Color(1f, 0.48f, 0.78f, 1f);
                }
                tireImg.preserveAspect = true;
                tireImg.raycastTarget = false;
                pinkTireRect = tireImg.rectTransform;
            }

            if (percentText == null)
            {
                Transform txtTf = groupRt.Find("Txt_Loading_Percent");
                if (txtTf == null)
                {
                    var txtGo = new GameObject("Txt_Loading_Percent", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtGo.transform.SetParent(groupRt, false);
                    txtTf = txtGo.transform;
                }
                var txtRt = (RectTransform)txtTf;
                txtRt.anchorMin = new Vector2(0.5f, 0.5f);
                txtRt.anchorMax = new Vector2(0.5f, 0.5f);
                txtRt.pivot = new Vector2(0.5f, 0.5f);
                txtRt.anchoredPosition = new Vector2(0f, -40f);
                txtRt.sizeDelta = new Vector2(420f, 44f);
                percentText = txtTf.GetComponent<TMP_Text>();
            }

            if (percentText != null)
            {
                ThemeFontManager.ApplyStoryFont(percentText);
                percentText.fontSize = 26f;
                percentText.fontStyle = FontStyles.Bold;
                percentText.alignment = TextAlignmentOptions.Center;
                percentText.color = new Color32(255, 168, 216, 255);
                percentText.text = "Đang tải... 0%";
            }

            ApplyProgressVisual(0f, 0f);
        }

        private IEnumerator RunAsyncLoadRoutine()
        {
            string target = string.IsNullOrEmpty(PendingTargetScene) ? "SampleScene" : PendingTargetScene;
            if (!Application.CanStreamedLevelBeLoaded(target))
            {
                target = "SampleScene";
            }

            AsyncOperation op = SceneManager.LoadSceneAsync(target);
            if (op == null)
            {
                SceneManager.LoadScene(target);
                yield break;
            }

            op.allowSceneActivation = false;
            float elapsed = 0f;
            float minDuration = Mathf.Max(0.25f, MinimumDisplaySeconds);

            while (true)
            {
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;

                float sceneNorm = Mathf.Clamp01(op.progress / 0.9f);
                float timeNorm = Mathf.Clamp01(elapsed / minDuration);
                float shown = Mathf.Min(sceneNorm, timeNorm);

                ApplyProgressVisual(shown, dt);

                if (op.progress >= 0.9f && timeNorm >= 1f)
                {
                    break;
                }

                yield return null;
            }

            ApplyProgressVisual(1f, 0f);
            IsActivated = true;
            yield return null;
            op.allowSceneActivation = true;
        }

        private void ApplyProgressVisual(float progress01, float deltaTime)
        {
            CurrentProgress = Mathf.Clamp01(progress01);

            if (fillImage != null)
            {
                fillImage.fillAmount = CurrentProgress;
            }

            if (pinkTireRect != null)
            {
                float x = Mathf.Lerp(-TrackWidth * 0.5f, TrackWidth * 0.5f, CurrentProgress);
                pinkTireRect.anchoredPosition = new Vector2(x, 58f);
                if (deltaTime > 0f)
                {
                    pinkTireRect.Rotate(0f, 0f, -380f * deltaTime);
                }
            }

            if (percentText != null)
            {
                percentText.text = $"Đang tải... {Mathf.RoundToInt(CurrentProgress * 100f)}%";
            }
        }

        private static Image EnsureChildImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offMin, Vector2 offMax)
        {
            Transform tf = parent.Find(name);
            GameObject go = tf != null ? tf.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (tf == null) go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
            return go.GetComponent<Image>();
        }

        private static Image EnsureCenteredImage(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            Transform tf = parent.Find(name);
            GameObject go = tf != null ? tf.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (tf == null) go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return go.GetComponent<Image>();
        }

        private static Sprite GetOrCreateWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }
    }
}
