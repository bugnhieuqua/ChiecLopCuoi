using System.Collections;
using TMPro;
using UnityEngine;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Quản lý hiển thị số sát thương bay lên (Floating Damage Text), chữ thông báo trạng thái
    /// và thanh máu World-Space gắn trên đầu mỗi Quái vật (MonsterOverheadHealthBar).
    /// </summary>
    public class CombatFloatingText : MonoBehaviour
    {
        private TextMeshPro _tmp;
        private Vector3 _velocity;
        private float _duration;
        private float _elapsed;
        private Color _startColor;
        private Vector3 _initialScale;

        public static void Spawn(Vector3 worldPos, string text, Color color, float fontSize = 4.2f, float duration = 0.85f)
        {
            GameObject go = new GameObject("FloatingCombatText");
            go.transform.position = worldPos + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.05f, 0.25f), -1f);

            var floating = go.AddComponent<CombatFloatingText>();
            floating.Setup(text, color, fontSize, duration);
        }

        private void Setup(string text, Color color, float fontSize, float duration)
        {
            _duration = Mathf.Max(0.25f, duration);
            _startColor = color;
            _velocity = new Vector3(Random.Range(-0.35f, 0.35f), 1.85f, 0f);
            _initialScale = Vector3.one;

            _tmp = gameObject.AddComponent<TextMeshPro>();
            ThemeFontManager.ApplyGlobalUiFont(_tmp);
            _tmp.text = text;
            _tmp.fontSize = fontSize;
            _tmp.color = color;
            _tmp.alignment = TextAlignmentOptions.Center;
            _tmp.fontStyle = FontStyles.Bold;
            _tmp.textWrappingMode = TextWrappingModes.NoWrap;
            _tmp.outlineWidth = 0.22f;
            _tmp.outlineColor = new Color32(15, 15, 15, 240);

            var mr = _tmp.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = "Near_Layer";
                mr.sortingOrder = 50;
            }

            transform.localScale = _initialScale * 0.65f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            // Nảy nhẹ lúc đầu rồi bay lên chậm dần
            transform.position += _velocity * Time.deltaTime;
            _velocity.y = Mathf.Lerp(1.85f, 0.45f, t);

            if (t < 0.2f)
            {
                float pop = Mathf.Lerp(0.65f, 1.18f, t / 0.2f);
                transform.localScale = _initialScale * pop;
            }
            else if (t < 0.4f)
            {
                float settle = Mathf.Lerp(1.18f, 1.0f, (t - 0.2f) / 0.2f);
                transform.localScale = _initialScale * settle;
            }

            if (_tmp != null && t > 0.55f)
            {
                float alpha = Mathf.Lerp(1f, 0f, (t - 0.55f) / 0.45f);
                _tmp.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);
            }

            if (_elapsed >= _duration)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Thanh máu World-Space tự động gắn trên đầu Quái vật.
    /// Tự động chống bị lật ngược khi quái đảo chiều localScale.x.
    /// </summary>
    public class MonsterOverheadHealthBar : MonoBehaviour
    {
        private Transform _target;
        private Vector3 _worldOffset = new Vector3(0f, 1.55f, 0f);
        private Transform _fillTransform;
        private SpriteRenderer _fillRenderer;
        private TextMeshPro _nameLabel;
        private float _displayedRatio = 1f;
        private float _targetRatio = 1f;

        private static Sprite _sharedWhiteSprite;

        public static Sprite GetWhiteSprite()
        {
            if (_sharedWhiteSprite != null) return _sharedWhiteSprite;
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] cols = new Color[16];
            for (int i = 0; i < 16; i++) cols[i] = Color.white;
            tex.SetPixels(cols);
            tex.Apply();
            _sharedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _sharedWhiteSprite;
        }

        public static MonsterOverheadHealthBar AttachTo(Transform monsterTransform, string displayName, float heightOffset = 1.55f)
        {
            Transform existing = monsterTransform.Find("Overhead_HP_Bar");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            GameObject root = new GameObject("Overhead_HP_Bar");
            root.transform.SetParent(monsterTransform, false);
            root.transform.localPosition = new Vector3(0f, heightOffset, 0f);

            var bar = root.AddComponent<MonsterOverheadHealthBar>();
            bar.BuildVisuals(monsterTransform, displayName, heightOffset);
            return bar;
        }

        private void BuildVisuals(Transform target, string displayName, float heightOffset)
        {
            _target = target;
            _worldOffset = new Vector3(0f, heightOffset, 0f);
            Sprite white = GetWhiteSprite();

            const float barWidth = 1.25f;
            const float barHeight = 0.14f;

            // 1. Khung viền đen
            GameObject borderGo = new GameObject("Border");
            borderGo.transform.SetParent(transform, false);
            borderGo.transform.localScale = new Vector3(barWidth + 0.06f, barHeight + 0.06f, 1f);
            var borderSr = borderGo.AddComponent<SpriteRenderer>();
            borderSr.sprite = white;
            borderSr.color = new Color(0.06f, 0.06f, 0.08f, 0.92f);
            borderSr.sortingLayerName = "Near_Layer";
            borderSr.sortingOrder = 32;

            // 2. Nền tối bên trong
            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(transform, false);
            bgGo.transform.localScale = new Vector3(barWidth, barHeight, 1f);
            var bgSr = bgGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = white;
            bgSr.color = new Color(0.22f, 0.10f, 0.10f, 0.90f);
            bgSr.sortingLayerName = "Near_Layer";
            bgSr.sortingOrder = 33;

            // 3. Pivot bên trái cho thanh máu Fill
            GameObject fillPivot = new GameObject("FillPivot");
            fillPivot.transform.SetParent(transform, false);
            fillPivot.transform.localPosition = new Vector3(-barWidth * 0.5f, 0f, 0f);
            _fillTransform = fillPivot.transform;

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillPivot.transform, false);
            fillGo.transform.localPosition = new Vector3(barWidth * 0.5f, 0f, 0f);
            fillGo.transform.localScale = new Vector3(barWidth, barHeight, 1f);
            _fillRenderer = fillGo.AddComponent<SpriteRenderer>();
            _fillRenderer.sprite = white;
            _fillRenderer.color = new Color(0.95f, 0.24f, 0.18f, 1f);
            _fillRenderer.sortingLayerName = "Near_Layer";
            _fillRenderer.sortingOrder = 34;

            // 4. Tên quái nhỏ phía trên thanh máu
            GameObject nameGo = new GameObject("NameLabel");
            nameGo.transform.SetParent(transform, false);
            nameGo.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            _nameLabel = nameGo.AddComponent<TextMeshPro>();
            ThemeFontManager.ApplyGlobalUiFont(_nameLabel);
            _nameLabel.text = displayName;
            _nameLabel.fontSize = 2.4f;
            _nameLabel.alignment = TextAlignmentOptions.Center;
            _nameLabel.color = new Color(1f, 0.92f, 0.72f, 0.95f);
            _nameLabel.fontStyle = FontStyles.Bold;
            _nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _nameLabel.outlineWidth = 0.2f;
            _nameLabel.outlineColor = new Color32(10, 10, 10, 230);
            var mr = _nameLabel.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = "Near_Layer";
                mr.sortingOrder = 35;
            }
        }

        public void SetHealth(float current, float max)
        {
            _targetRatio = max > 0.01f ? Mathf.Clamp01(current / max) : 0f;
            if (_fillRenderer != null)
            {
                _fillRenderer.color = Color.Lerp(new Color(0.95f, 0.15f, 0.12f, 1f), new Color(1f, 0.42f, 0.15f, 1f), _targetRatio);
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // Giữ thanh máu không bị lật ngược khi Quái đảo localScale.x
            Vector3 parentScale = _target.localScale;
            float signX = parentScale.x < 0f ? -1f : 1f;
            float absX = Mathf.Max(0.01f, Mathf.Abs(parentScale.x));
            float absY = Mathf.Max(0.01f, Mathf.Abs(parentScale.y));
            transform.localScale = new Vector3(signX / absX, 1f / absY, 1f);
            transform.localPosition = new Vector3(0f, _worldOffset.y / absY, 0f);

            _displayedRatio = Mathf.MoveTowards(_displayedRatio, _targetRatio, Time.deltaTime * 4.5f);
            if (_fillTransform != null)
            {
                _fillTransform.localScale = new Vector3(_displayedRatio, 1f, 1f);
            }
        }
    }
}
