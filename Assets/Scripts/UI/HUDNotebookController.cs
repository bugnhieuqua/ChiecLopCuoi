using UnityEngine;
using UnityEngine.UI;
using ChiecLopCuoi.Gameplay;

/// <summary>
/// Điều khiển HUD "sổ tay" (HUD_Notebook): hiển thị đồng hồ đếm ngược bằng sprite số 0-9
/// và cập nhật thanh máu 2 phe theo dữ liệu của BattleManager.
/// </summary>
[DisallowMultipleComponent]
public class HUDNotebookController : MonoBehaviour
{
    [Header("Thanh máu (Image kiểu Filled)")]
    public Image playerHealthFill;   // P1 - thanh bên trái
    public Image enemyHealthFill;    // P2 - thanh bên phải

    [Header("Đồng hồ đếm ngược")]
    [Tooltip("Khung viền đồng hồ đếm ngược")]
    public GameObject timerFrame;
    [Tooltip("3 ô số theo thứ tự trăm - chục - đơn vị")]
    public Image[] timerDigits = new Image[3];
    [Tooltip("Sprite số 0..9 cắt từ ui_notebook.png")]
    public Sprite[] digitSprites = new Sprite[10];
    public float digitSpacing = 46f;
    public float digitWidth = 44f;
    public float digitHeight = 56f;
    public float digitY = -106f;
    public Color normalDigitColor = Color.white;
    public Color dangerDigitColor = new Color(1f, 0.22f, 0.18f);
    public int dangerThreshold = 10;

    [Header("Hiệu ứng máu")]
    [Tooltip("Tốc độ thanh máu chạy về giá trị mục tiêu (0 = tức thời)")]
    public float healthLerpSpeed = 6f;

    private float _displayPlayer = 1f;
    private float _displayEnemy = 1f;
    private float _targetPlayer = 1f;
    private float _targetEnemy = 1f;

    private void Awake()
    {
        AutoBindReferences();
        ConfigureFill(playerHealthFill, Image.OriginHorizontal.Left);
        ConfigureFill(enemyHealthFill, Image.OriginHorizontal.Right);
        SetDigitsVisible(0);
    }

    private void OnEnable()
    {
        RefreshAll();
    }

    private void AutoBindReferences()
    {
        if (timerFrame == null)
        {
            Transform tf = transform.Find("Timer_Frame");
            if (tf != null) timerFrame = tf.gameObject;
        }

        if (playerHealthFill == null)
        {
            Transform t = transform.Find("P1_HealthBar_Fill");
            if (t != null) playerHealthFill = t.GetComponent<Image>();
        }

        if (enemyHealthFill == null)
        {
            Transform t = transform.Find("P2_HealthBar_Fill");
            if (t != null) enemyHealthFill = t.GetComponent<Image>();
        }

        if (timerDigits == null || timerDigits.Length == 0)
        {
            timerDigits = new Image[3];
            for (int i = 0; i < timerDigits.Length; i++)
            {
                Transform t = transform.Find("Timer_Digit_" + i);
                if (t != null) timerDigits[i] = t.GetComponent<Image>();
            }
        }

        HideUnusedRoundSlotsAndKO();
        EnsureCleanComboAndPillSprites();
        EnsureCircularPortraitMask();
    }

    /// <summary>Đảm bảo khung avatar P1 và P2 dùng Mask hình tròn khớp lòng khung, cắt gọn mép ảnh nhân vật.</summary>
    public void EnsureCircularPortraitMask()
    {
        Sprite circleMask = Resources.Load<Sprite>("UI/UI_Circle_Mask");
        if (circleMask == null) return;

        ApplyCircularMaskToFrame("P1_Portrait_Frame", "P1_Portrait", circleMask, new Vector2(-2f, -38f), new Vector2(138f, 188f));
        ApplyCircularMaskToFrame("P2_Portrait_Frame", "P2_Portrait", circleMask, new Vector2(0f, -54f), new Vector2(134f, 212f));
    }

    private void ApplyCircularMaskToFrame(string frameName, string portraitChildName, Sprite circleMask, Vector2 portraitPos, Vector2 portraitSize)
    {
        Transform frameTf = transform.Find(frameName);
        if (frameTf == null) return;

        Transform vpTf = frameTf.Find("Portrait_Viewport");
        if (vpTf == null)
        {
            var vpGo = new GameObject("Portrait_Viewport", typeof(RectTransform));
            vpGo.transform.SetParent(frameTf, false);
            vpGo.layer = 5;
            vpTf = vpGo.transform;
        }

        if (vpTf is RectTransform vprt)
        {
            vprt.anchorMin = Vector2.zero;
            vprt.anchorMax = Vector2.one;
            vprt.pivot = new Vector2(0.5f, 0.5f);
            vprt.anchoredPosition = Vector2.zero;
            vprt.offsetMin = new Vector2(12f, 12f);
            vprt.offsetMax = new Vector2(-12f, -12f);
        }

        RectMask2D rectMask = vpTf.GetComponent<RectMask2D>();
        if (rectMask != null)
        {
            if (Application.isPlaying) Destroy(rectMask);
            else DestroyImmediate(rectMask);
        }

        Image maskImg = vpTf.GetComponent<Image>();
        if (maskImg == null) maskImg = vpTf.gameObject.AddComponent<Image>();
        maskImg.sprite = circleMask;
        maskImg.type = Image.Type.Simple;
        maskImg.preserveAspect = true;
        maskImg.color = new Color(0.08f, 0.09f, 0.11f, 0.94f);
        maskImg.raycastTarget = false;

        Mask mask = vpTf.GetComponent<Mask>();
        if (mask == null) mask = vpTf.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        Transform charTf = vpTf.Find(portraitChildName);
        if (charTf != null)
        {
            if (charTf is RectTransform crt)
            {
                crt.anchorMin = new Vector2(0.5f, 0.5f);
                crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = portraitPos;
                crt.sizeDelta = portraitSize;
            }
            Image pImg = charTf.GetComponent<Image>();
            if (pImg != null)
            {
                pImg.preserveAspect = true;
                pImg.raycastTarget = false;
            }
        }
    }

    /// <summary>Đảm bảo nền thanh Combo (P1/P2) và khung dùng sprite bo tròn 9-slice, không hở góc đen.</summary>
    public void EnsureCleanComboAndPillSprites()
    {
        Sprite capSp = Resources.Load<Sprite>("UI/UI_Capsule_Dark");
        ApplyComboLayerFix("P1_Combo_Back", "P1_Combo_Frame", new Vector2(148f, -96f), capSp);
        ApplyComboLayerFix("P2_Combo_Back", "P2_Combo_Frame", new Vector2(-148f, -96f), capSp);
    }

    private void ApplyComboLayerFix(string backName, string frameName, Vector2 anchoredPos, Sprite capSp)
    {
        Transform backTf = transform.Find(backName);
        if (backTf != null)
        {
            if (backTf is RectTransform brt)
            {
                brt.anchoredPosition = anchoredPos;
                brt.sizeDelta = new Vector2(636f, 24f);
            }
            Image backImg = backTf.GetComponent<Image>();
            if (backImg != null && capSp != null)
            {
                backImg.sprite = capSp;
                backImg.type = Image.Type.Sliced;
            }
        }

        Transform frameTf = transform.Find(frameName);
        if (frameTf != null)
        {
            Image frameImg = frameTf.GetComponent<Image>();
            if (frameImg != null && frameImg.sprite != null)
            {
                frameImg.type = Image.Type.Sliced;
            }
        }
    }

    /// <summary>Ẩn 3 chấm Bo3 thừa (game đánh 1 hiệp) và chữ K.O. khi đang giao đấu.</summary>
    public void HideUnusedRoundSlotsAndKO()
    {
        Transform p1Slots = transform.Find("P1_Round_Slots");
        if (p1Slots != null) p1Slots.gameObject.SetActive(false);

        Transform p2Slots = transform.Find("P2_Round_Slots");
        if (p2Slots != null) p2Slots.gameObject.SetActive(false);

        SetKOLabelVisible(false);
    }

    public void SetKOLabelVisible(bool visible)
    {
        Transform koLabel = transform.Find("KO_Label");
        if (koLabel != null) koLabel.gameObject.SetActive(visible);
    }

    private void ConfigureFill(Image image, Image.OriginHorizontal origin)
    {
        if (image == null) return;

        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)origin;
        image.fillAmount = 1f;
    }

    private void RefreshAll()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null)
        {
            SetDigitsVisible(0);
            if (timerFrame != null) timerFrame.SetActive(false);
            return;
        }

        if (battle.State == BattleManager.BattleState.Playing)
        {
            if (timerFrame != null) timerFrame.SetActive(true);
            SetTimer(battle.SecondsRemaining);
        }
        else
        {
            SetDigitsVisible(0);
            if (timerFrame != null) timerFrame.SetActive(false);
        }

        _targetPlayer = _displayPlayer = battle.PlayerHealth01;
        _targetEnemy = _displayEnemy = battle.EnemyHealth01;
        ApplyHealthToBars();
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle != null)
        {
            _targetPlayer = battle.PlayerHealth01;
            _targetEnemy = battle.EnemyHealth01;
            if (battle.State == BattleManager.BattleState.Playing)
            {
                if (timerFrame != null && !timerFrame.activeSelf) timerFrame.SetActive(true);
                SetTimer(battle.SecondsRemaining);
            }
            else if (timerFrame != null && timerFrame.activeSelf)
            {
                SetDigitsVisible(0);
                timerFrame.SetActive(false);
            }
        }

        if (healthLerpSpeed <= 0f)
        {
            _displayPlayer = _targetPlayer;
            _displayEnemy = _targetEnemy;
        }
        else
        {
            float k = 1f - Mathf.Exp(-healthLerpSpeed * Time.deltaTime);
            _displayPlayer = Mathf.Lerp(_displayPlayer, _targetPlayer, k);
            _displayEnemy = Mathf.Lerp(_displayEnemy, _targetEnemy, k);
        }

        ApplyHealthToBars();
    }

    private void ApplyHealthToBars()
    {
        if (playerHealthFill != null) playerHealthFill.fillAmount = _displayPlayer;
        if (enemyHealthFill != null) enemyHealthFill.fillAmount = _displayEnemy;
    }

    /// <summary>Hiển thị số giây còn lại bằng các ô sprite số (ẩn số 0 vô nghĩa ở đầu).</summary>
    public void SetTimer(int seconds)
    {
        string text = Mathf.Max(0, seconds).ToString();

        int visible = Mathf.Min(text.Length, timerDigits.Length);
        int firstIndex = text.Length - visible;
        bool danger = seconds <= dangerThreshold;

        RectTransform frameRt = timerFrame != null ? timerFrame.GetComponent<RectTransform>() : null;
        float w = digitWidth;
        float h = digitHeight;
        float spacing = digitSpacing;
        if (frameRt != null)
        {
            Vector2 frameSize = frameRt.rect.size;
            if (frameSize.x > 10f && frameSize.y > 10f)
            {
                w = Mathf.Min(digitWidth, frameSize.x * 0.16f);
                h = Mathf.Min(digitHeight, frameSize.y * 0.52f);
                spacing = Mathf.Min(digitSpacing, frameSize.x * 0.16f);
            }
        }

        float totalWidth = (visible - 1) * spacing;

        for (int i = 0; i < timerDigits.Length; i++)
        {
            Image slot = timerDigits[i];
            if (slot == null) continue;

            bool use = i < visible;
            slot.gameObject.SetActive(use);
            if (!use) continue;

            int digit = text[firstIndex + i] - '0';
            slot.sprite = (digitSprites != null && digit >= 0 && digit < digitSprites.Length) ? digitSprites[digit] : null;
            slot.color = danger ? dangerDigitColor : normalDigitColor;
            slot.preserveAspect = true;

            RectTransform rt = slot.rectTransform;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);

            float offsetX = -totalWidth * 0.5f + i * spacing;
            if (frameRt != null && rt.parent != null)
            {
                Vector3 frameWorldCenter = frameRt.TransformPoint(frameRt.rect.center + new Vector2(0f, frameRt.rect.height * 0.04f));
                Vector3 centerInParent = rt.parent.InverseTransformPoint(frameWorldCenter);
                rt.localPosition = new Vector3(centerInParent.x + offsetX, centerInParent.y, rt.localPosition.z);
            }
            else
            {
                rt.anchoredPosition = new Vector2(offsetX, digitY);
            }
        }
    }

    private void SetDigitsVisible(int count)
    {
        if (timerDigits == null) return;
        for (int i = 0; i < timerDigits.Length; i++)
        {
            if (timerDigits[i] != null)
                timerDigits[i].gameObject.SetActive(i < count);
        }
    }
}
