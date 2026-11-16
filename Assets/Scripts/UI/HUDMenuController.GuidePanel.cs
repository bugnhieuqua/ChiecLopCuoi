using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChiecLopCuoi.UI
{
    public partial class HUDMenuController
    {
        // =========================================================================
        // XỬ LÝ RIÊNG BẢNG HƯỚNG DẪN CHƠI GAME (PHÍM F12)
        // =========================================================================

        public void ToggleGuidePanel()
        {
            if (guidePanel == null) EnsureGuidePanelExists();

            if (guidePanel != null)
            {
                if (guidePanel.activeSelf) CloseGuidePanel();
                else OpenGuidePanel();
            }
        }

        public void OpenGuidePanel()
        {
            if (Time.unscaledTime - _lastActionTime < 0.25f) return;
            _lastActionTime = Time.unscaledTime;

            PlayClickSound();

            if (guidePanel == null) EnsureGuidePanelExists();

            if (guidePanel != null)
            {
                if (menuPanel != null && menuPanel.activeSelf)
                {
                    menuPanel.SetActive(false);
                    _wasMenuOpenBeforeGuide = true;
                }

                guidePanel.transform.SetAsLastSibling();
                var rt = guidePanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = Vector2.zero;
                    rt.localScale = Vector3.one;
                }

                // Panel_Container có thể bị tắt sẵn trong Scene; bật lại để nội
                // dung bảng hướng dẫn hiện ra khi mở.
                var container = guidePanel.transform.Find("Panel_Container");
                if (container != null && !container.gameObject.activeSelf)
                {
                    container.gameObject.SetActive(true);
                }

                guidePanel.SetActive(true);
            }

            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            Debug.Log("<color=cyan>[HUDMenuController]</color> BẢNG HƯỚNG DẪN: ĐÃ MỞ (F12).");
        }

        public void CloseGuidePanel()
        {
            if (Time.unscaledTime - _lastActionTime < 0.25f) return;
            _lastActionTime = Time.unscaledTime;

            PlayClickSound();

            if (guidePanel != null)
            {
                guidePanel.SetActive(false);
            }

            if (_wasMenuOpenBeforeGuide)
            {
                _wasMenuOpenBeforeGuide = false;
                if (menuPanel != null)
                {
                    menuPanel.SetActive(true);
                }
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = _previousTimeScale > 0.01f ? _previousTimeScale : 1f;
            }

            Debug.Log("<color=cyan>[HUDMenuController]</color> BẢNG HƯỚNG DẪN: ĐÃ ĐÓNG (F12).");
        }

        /// <summary>
        /// Tự động khởi tạo Bảng Hướng Dẫn Điều Khiển dùng đúng khung gỗ giấy da btn_huongdan.png,
        /// nút đóng dấu X dùng btn_components_4 và nút xác nhận dùng btn_map.png.
        /// </summary>
        public void EnsureGuidePanelExists()
        {
#if UNITY_EDITOR
            LoadSpritesIfMissing();
#endif
            var hudCanvas = (GetComponent<Canvas>() != null) ? gameObject : (GetComponentInParent<Canvas>()?.gameObject ?? GameObject.Find("HUD_Canvas"));
            if (hudCanvas == null) return;

            // Nếu đã có sẵn Tutorial_Guide_Panel trong Scene (do người dùng thiết kế trên Level 1),
            // giữ nguyên khung giấy da & nút X đó, chỉ đồng bộ bố cục chữ rõ đẹp và gắn tham chiếu nút!
            Transform existing = hudCanvas.transform.Find("Tutorial_Guide_Panel");
            if (existing != null)
            {
                guidePanel = existing.gameObject;
                if (guideCloseButton == null)
                {
                    var closeTf = existing.Find("Panel_Container/Btn_Close_Top") ?? existing.Find("Btn_Close_Top");
                    if (closeTf != null) guideCloseButton = closeTf.GetComponent<Button>();
                }
                if (guideGotItButton == null)
                {
                    var gotItTf = existing.Find("Panel_Container/Btn_GotIt") ?? existing.Find("Btn_GotIt");
                    if (gotItTf != null) guideGotItButton = gotItTf.GetComponent<Button>();
                }
                ConfigureExistingGuideLayout(existing);
                return;
            }

            // 1. Root Panel: Phủ mờ toàn màn hình (1920x1080)
            var rootGo = new GameObject("Tutorial_Guide_Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rootGo.transform.SetParent(hudCanvas.transform, false);
            var rootRt = rootGo.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;
            rootRt.anchoredPosition = Vector2.zero;

            var bgOverlay = rootGo.GetComponent<Image>();
            bgOverlay.color = new Color(0f, 0f, 0f, 0.72f);
            bgOverlay.raycastTarget = true;

            // 2. Bảng Gỗ Giấy Da Chính: Dùng btn_huongdan.png (btn_huongdan_0)
            var containerGo = new GameObject("Panel_Container", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            containerGo.transform.SetParent(rootGo.transform, false);
            var contRt = containerGo.GetComponent<RectTransform>();
            contRt.anchorMin = new Vector2(0.5f, 0.5f);
            contRt.anchorMax = new Vector2(0.5f, 0.5f);
            contRt.pivot = new Vector2(0.5f, 0.5f);
            contRt.sizeDelta = new Vector2(1160f, 880f);
            contRt.anchoredPosition = new Vector2(0f, 8f);

            var contImg = containerGo.GetComponent<Image>();
            if (spriteHuongDanBoard != null)
            {
                contImg.sprite = spriteHuongDanBoard;
                contImg.color = Color.white;
                contImg.preserveAspect = false;
            }
            else
            {
                contImg.color = new Color(0.85f, 0.72f, 0.55f, 0.98f);
            }
            contImg.raycastTarget = true;

            // 3. Tiêu đề nằm ngay trên kẹp gỗ phía trên của bảng btn_huongdan
            var titleGo = new GameObject("Title_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(containerGo.transform, false);
            var trt = titleGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 0.5f);
            trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(500f, 92f);
            trt.anchoredPosition = new Vector2(8f, 326f);

            var ttxt = titleGo.GetComponent<TextMeshProUGUI>();
            ThemeFontManager.ApplyGlobalUiFont(ttxt);
            ttxt.text = "HƯỚNG DẪN ĐIỀU KHIỂN";
            ttxt.fontSize = 27f;
            ttxt.alignment = TextAlignmentOptions.Center;
            ttxt.color = new Color(1f, 0.96f, 0.80f, 1f);
            ttxt.fontStyle = FontStyles.Bold;
            ttxt.outlineWidth = 0.22f;
            ttxt.outlineColor = new Color32(55, 25, 8, 255);
            ttxt.raycastTarget = false;

            // 4. Nút X đóng ở góc trên bên phải bảng gỗ (dùng đúng btn_components_4)
            var closeGo = new GameObject("Btn_Close_Top", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(containerGo.transform, false);
            var crt = closeGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(78f, 72f);
            crt.anchoredPosition = new Vector2(492f, 312f);

            var cimg = closeGo.GetComponent<Image>();
            if (spriteMenuClose != null)
            {
                cimg.sprite = spriteMenuClose;
                cimg.color = Color.white;
                cimg.preserveAspect = true;
            }
            else
            {
                cimg.color = new Color(0.85f, 0.25f, 0.2f, 1f);
            }
            cimg.raycastTarget = true;
            guideCloseButton = closeGo.GetComponent<Button>();

            // 5. Cột Trái trên mặt giấy da: Kỹ Năng & Di Chuyển Cow Lốp
            CreateGuideCard(
                containerGo.transform,
                "Col_CowLop",
                new Vector2(-235f, -18f),
                new Vector2(435f, 435f),
                "CHIẾN BINH COW LỐP",
                new Color(0.45f, 0.22f, 0.08f, 0.94f),
                "<color=#8B2500><b>• Phím A / D (hoặc Cần Tròn):</b></color> <color=#2B1708>Di chuyển Trái / Phải</color>\n" +
                "  <color=#5C3A21><i>(Giữ phím <b>Shift</b> để chạy bứt tốc)</i></color>\n\n" +
                "<color=#8B2500><b>• Phím W / Space:</b></color> <color=#2B1708>Nhảy cao né đòn</color>\n\n" +
                "<color=#8B2500><b>• Phím J:</b></color> <color=#2B1708><b>Đánh Thường</b> — Võ lốp cận chiến (12 ST)</color>\n\n" +
                "<color=#8B2500><b>• Phím U:</b></color> <color=#2B1708><b>Xoay Lốp (Tire Spin)</b> — Quét diện rộng</color>\n\n" +
                "<color=#8B2500><b>• Phím K:</b></color> <color=#2B1708><b>Ném Lốp Tầm Xa</b> — Phi lốp (18 ST)</color>\n\n" +
                "<color=#8B2500><b>• Phím L:</b></color> <color=#2B1708><b>Đập Lốp Nhảy Cao</b> — Diện rộng (25 ST)</color>\n\n" +
                "<color=#8B2500><b>• Phím H:</b></color> <color=#2B1708><b>Đỡ Đòn (Block)</b> — Giảm 70% sát thương</color>"
            );

            // 6. Cột Phải trên mặt giấy da: Quân Đội & Phím Hệ Thống
            CreateGuideCard(
                containerGo.transform,
                "Col_Army",
                new Vector2(220f, -18f),
                new Vector2(435f, 435f),
                "QUÂN ĐỘI & HỆ THỐNG",
                new Color(0.38f, 0.24f, 0.10f, 0.94f),
                "<color=#8B2500><b>• Phím 1:</b></color> <color=#2B1708><b>Triệu hồi Cuốc</b> — Thu gom Lốp Xe</color>\n\n" +
                "<color=#8B2500><b>• Phím 2:</b></color> <color=#2B1708><b>Triệu hồi Gậy</b> — Lính cận chiến tiền tuyến</color>\n\n" +
                "<color=#8B2500><b>• Phím 3:</b></color> <color=#2B1708><b>Triệu hồi Cung</b> — Xạ thủ bắn tỉa hậu tuyến</color>\n\n" +
                "<color=#8B2500><b>• Phím S / C:</b></color> <color=#2B1708>Phòng thủ giữ vị trí / Rút quân về thành</color>\n\n" +
                "<color=#8B2500><b>• Thanh Nộ 100%:</b></color> <color=#2B1708>Cường hóa đòn <b>U / L</b> gấp 1.65x</color>\n\n" +
                "<color=#8B2500><b>• Nút Pause (||) / ESC:</b></color> <color=#2B1708>Tạm dừng & Mở Menu</color>\n\n" +
                "<color=#8B2500><b>• Phím F12:</b></color> <color=#2B1708>Đóng / Mở Bảng Hướng Dẫn này</color>"
            );

            // 7. Nút ĐÃ HIỂU [F12] dưới đáy bảng (dùng đúng btn_map.png)
            var gotItGo = new GameObject("Btn_GotIt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            gotItGo.transform.SetParent(containerGo.transform, false);
            var grt = gotItGo.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.5f, 0.5f);
            grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.sizeDelta = new Vector2(280f, 72f);
            grt.anchoredPosition = new Vector2(0f, -296f);

            var gimg = gotItGo.GetComponent<Image>();
            if (spriteBtnMap != null)
            {
                gimg.sprite = spriteBtnMap;
                gimg.color = Color.white;
                gimg.preserveAspect = true;
            }
            else
            {
                gimg.color = new Color(0.18f, 0.52f, 0.85f, 1f);
            }
            gimg.raycastTarget = true;
            guideGotItButton = gotItGo.GetComponent<Button>();

            var gtextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            gtextGo.transform.SetParent(gotItGo.transform, false);
            var gtrt = gtextGo.GetComponent<RectTransform>();
            gtrt.anchorMin = Vector2.zero;
            gtrt.anchorMax = Vector2.one;
            gtrt.sizeDelta = Vector2.zero;
            gtrt.anchoredPosition = new Vector2(0f, 2f);

            var gtxt = gtextGo.GetComponent<TextMeshProUGUI>();
            ThemeFontManager.ApplyGlobalUiFont(gtxt);
            gtxt.text = "ĐÃ HIỂU [F12]";
            gtxt.fontSize = 20f;
            gtxt.alignment = TextAlignmentOptions.Center;
            gtxt.color = new Color(0.05f, 0.16f, 0.36f, 1f);
            gtxt.fontStyle = FontStyles.Bold;
            gtxt.outlineWidth = 0.15f;
            gtxt.outlineColor = new Color32(255, 255, 255, 220);
            gtxt.raycastTarget = false;

            SetLayerRecursively(rootGo, 5);

            guidePanel = rootGo;
            RegisterButtonEvents();

            Debug.Log("<color=green>[HUDMenuController]</color> Đã khởi tạo Bảng Hướng Dẫn bằng btn_huongdan + btn_components_4 + btn_map!");
        }

        private static void ConfigureExistingGuideLayout(Transform guideRoot)
        {
            if (guideRoot == null) return;

            Transform container = guideRoot.Find("Panel_Container");
            if (container != null)
            {
                var containerRt = container.GetComponent<RectTransform>();
                if (containerRt != null)
                {
                    containerRt.anchoredPosition = new Vector2(0f, -58f);
                    containerRt.sizeDelta = new Vector2(1220f, 720f);
                }

                Transform closeTf = container.Find("Btn_Close_Top");
                if (closeTf != null)
                {
                    var closeRt = closeTf.GetComponent<RectTransform>();
                    if (closeRt != null)
                    {
                        closeRt.anchorMin = new Vector2(0.5f, 0.5f);
                        closeRt.anchorMax = new Vector2(0.5f, 0.5f);
                        closeRt.anchoredPosition = new Vector2(458f, 276f);
                        closeRt.sizeDelta = new Vector2(74f, 68f);
                    }
                }
            }

            Transform contentTf = guideRoot.Find("Panel_Container/Content") ?? guideRoot.Find("Content");
            if (contentTf == null) return;

            var contentRt = contentTf.GetComponent<RectTransform>();
            if (contentRt != null)
            {
                contentRt.localRotation = Quaternion.identity;
                contentRt.anchorMin = new Vector2(0.20f, 0.12f);
                contentRt.anchorMax = new Vector2(0.85f, 0.77f);
                contentRt.anchoredPosition = Vector2.zero;
                contentRt.sizeDelta = Vector2.zero;
                contentRt.pivot = new Vector2(0.5f, 0.5f);
            }

            var contentTmp = contentTf.GetComponent<TMP_Text>();
            if (contentTmp != null)
            {
                TMP_FontAsset cleanFont = ThemeFontManager.GetStoryFont();
                if (cleanFont != null)
                {
                    ThemeFontManager.ApplyStoryFont(contentTmp);
                }
                contentTmp.textStyle = TMP_Style.NormalStyle;
                contentTmp.enableAutoSizing = false;
                contentTmp.fontSize = 27f;
                contentTmp.characterSpacing = 0f;
                contentTmp.lineSpacing = 12f;
                contentTmp.paragraphSpacing = 16f;
                contentTmp.alignment = TextAlignmentOptions.TopLeft;
                contentTmp.textWrappingMode = TextWrappingModes.Normal;
                contentTmp.outlineWidth = 0f;
                contentTmp.margin = new Vector4(8f, 6f, 8f, 6f);
                contentTmp.color = new Color(0.17f, 0.09f, 0.03f, 1f);
                contentTmp.text =
                    "<align=center><size=33><color=#7A1E00><b>HƯỚNG DẪN ĐIỀU KHIỂN</b></color></size></align>\n" +
                    "<color=#8B2500><b>• Phím A / D:</b></color> <color=#2B1708>Di chuyển Trái / Phải (<b>Shift</b>: chạy nhanh)</color>\n" +
                    "<color=#8B2500><b>• Phím W / Space:</b></color> <color=#2B1708>Nhảy cao né đòn</color>\n" +
                    "<color=#8B2500><b>• Phím J:</b></color> <color=#2B1708><b>Đánh Thường</b> - Võ lốp cận chiến (14 ST)</color>\n" +
                    "<color=#8B2500><b>• Phím K:</b></color> <color=#2B1708><b>Ném Lốp</b> - Phi lốp tầm xa (18 ST)</color>\n" +
                    "<color=#8B2500><b>• Phím U:</b></color> <color=#2B1708><b>Xoay Lốp</b> - Quét diện rộng (28 ST)</color>\n" +
                    "<color=#8B2500><b>• Phím L:</b></color> <color=#2B1708><b>Đập Lốp Nhảy Cao</b> - Chấn động (26 ST)</color>\n" +
                    "<color=#8B2500><b>• Phím H:</b></color> <color=#2B1708><b>Đỡ Đòn (Block)</b> - Giảm sát thương</color>\n" +
                    "<align=center><size=22><color=#5C3A21><i>Nhấn phím <b>F12</b> hoặc nút <b>X</b> để đóng bảng hướng dẫn</i></color></size></align>";
            }
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void CreateGuideCard(Transform parent, string name, Vector2 pos, Vector2 size, string header, Color headerColor, string content)
        {
            var cardGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardGo.transform.SetParent(parent, false);
            var rt = cardGo.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            // Nền bán trong suốt rất nhẹ để tôn trọn vẹn chất liệu giấy da của btn_huongdan.png
            var img = cardGo.GetComponent<Image>();
            img.color = new Color(0.62f, 0.42f, 0.22f, 0.12f);
            img.raycastTarget = false;

            // Khung viền mảnh màu nâu gỗ trên giấy da
            var borderGo = new GameObject("Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            borderGo.transform.SetParent(cardGo.transform, false);
            var brt = borderGo.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.sizeDelta = new Vector2(-4f, -4f);
            brt.anchoredPosition = Vector2.zero;
            var bimg = borderGo.GetComponent<Image>();
            bimg.color = new Color(0.45f, 0.26f, 0.10f, 0.16f);
            bimg.raycastTarget = false;

            // Thanh Banner Tiêu Đề của Cột
            var hBannerGo = new GameObject("Header_Banner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            hBannerGo.transform.SetParent(cardGo.transform, false);
            var hbrt = hBannerGo.GetComponent<RectTransform>();
            hbrt.anchorMin = new Vector2(0.5f, 1f);
            hbrt.anchorMax = new Vector2(0.5f, 1f);
            hbrt.pivot = new Vector2(0.5f, 1f);
            hbrt.sizeDelta = new Vector2(size.x - 16f, 38f);
            hbrt.anchoredPosition = new Vector2(0f, -8f);
            var hbimg = hBannerGo.GetComponent<Image>();
            hbimg.color = headerColor;
            hbimg.raycastTarget = false;

            var hGo = new GameObject("Header_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            hGo.transform.SetParent(hBannerGo.transform, false);
            var hrt = hGo.GetComponent<RectTransform>();
            hrt.anchorMin = Vector2.zero;
            hrt.anchorMax = Vector2.one;
            hrt.sizeDelta = Vector2.zero;
            hrt.anchoredPosition = Vector2.zero;
            var htxt = hGo.GetComponent<TextMeshProUGUI>();
            ThemeFontManager.ApplyGlobalUiFont(htxt);
            htxt.text = header;
            htxt.fontSize = 18f;
            htxt.alignment = TextAlignmentOptions.Center;
            htxt.color = new Color(1f, 0.94f, 0.76f, 1f);
            htxt.fontStyle = FontStyles.Bold;
            htxt.raycastTarget = false;

            // Nội dung hướng dẫn chữ nâu đậm sắc nét trên giấy da
            var cGo = new GameObject("Content", typeof(RectTransform), typeof(TextMeshProUGUI));
            cGo.transform.SetParent(cardGo.transform, false);
            var crt = cGo.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(-26f, -56f);
            crt.anchoredPosition = new Vector2(0f, -20f);
            var ctxt = cGo.GetComponent<TextMeshProUGUI>();
            ThemeFontManager.ApplyGlobalUiFont(ctxt);
            ctxt.text = content;
            ctxt.fontSize = 15.2f;
            ctxt.alignment = TextAlignmentOptions.TopLeft;
            ctxt.color = new Color(0.17f, 0.09f, 0.03f, 1f);
            ctxt.lineSpacing = 4f;
            ctxt.textWrappingMode = TextWrappingModes.Normal;
            ctxt.overflowMode = TextOverflowModes.Overflow;
            ctxt.raycastTarget = false;
        }

    }
}
