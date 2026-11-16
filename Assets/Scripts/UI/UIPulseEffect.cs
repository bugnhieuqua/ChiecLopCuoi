using UnityEngine;

namespace ChiecLopCuoi.UI
{
    /// <summary>
    /// Tạo hiệu ứng phóng to / thu nhỏ nhẹ nhàng (breathing / pulse) liên tục cho UI button.
    /// </summary>
    public class UIPulseEffect : MonoBehaviour
    {
        [Header("Scale Settings")]
        [SerializeField] private float minScale = 0.95f;
        [SerializeField] private float maxScale = 1.05f;
        [SerializeField] private float speed = 2.5f;

        [Header("Timing")]
        [SerializeField] private bool useUnscaledTime = true;

        private Vector3 initialScale;

        private void Awake()
        {
            initialScale = transform.localScale;
        }

        private void Update()
        {
            float t = useUnscaledTime ? Time.unscaledTime : Time.time;
            // Dùng sóng sin chuẩn hóa từ 0 đến 1
            float factor = (Mathf.Sin(t * speed) + 1f) * 0.5f;
            float currentScaleMultiplier = Mathf.Lerp(minScale, maxScale, factor);

            transform.localScale = initialScale * currentScaleMultiplier;
        }
    }
}
