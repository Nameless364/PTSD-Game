using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame
{
    /// <summary>
    /// UI แสดงระดับการตรวจพบของศัตรู แบบ Far Cry / Payday
    /// Arc สีน้ำตาล/ทอง = สงสัย | Arc สีแดงกระพริบ = โดนจับ
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DetectionIndicatorUI : MonoBehaviour
    {
        public static DetectionIndicatorUI Instance { get; private set; }

        [Header("Arc Image")]
        [Tooltip("Image (Filled / Radial 360) แสดงระดับ detection")]
        public Image arcImage;

        [Header("Optional References")]
        [Tooltip("Text '!' ที่กระพริบตอนโดนจับ (optional)")]
        public Text alertText;
        [Tooltip("Image ไอคอนตา (optional) จะ tint สีตาม state")]
        public Image eyeIcon;

        [Header("Colors")]
        public Color suspiciousColor = new Color(0.85f, 0.65f, 0.25f, 1f);
        public Color detectedColor   = new Color(0.9f,  0.1f,  0.1f,  1f);
        public Color safeEyeColor    = new Color(1f, 1f, 1f, 0.35f);

        [Header("Animation")]
        public float fillSpeed     = 3f;
        public float drainSpeed    = 1.5f;
        public float flashSpeed    = 8f;
        public float scaleDetected = 1.15f;

        // ---- Internal State ----
        private float _targetFill  = 0f;
        private float _currentFill = 0f;
        private CanvasGroup _cg;
        private RectTransform _rect;
        private Vector3 _baseScale;

        // --- Public API ---
        public void SetDetectionLevel(float level)
        {
            _targetFill = Mathf.Clamp01(level);
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _cg        = GetComponent<CanvasGroup>();
            _rect      = GetComponent<RectTransform>();
            _baseScale = _rect.localScale;
        }

        void Update()
        {
            if (arcImage == null) return;

            // --- เติม / ลด fill ---
            float speed  = (_currentFill < _targetFill) ? fillSpeed : drainSpeed;
            _currentFill = Mathf.MoveTowards(_currentFill, _targetFill, speed * Time.deltaTime);
            arcImage.fillAmount = _currentFill;

            float flash = (Mathf.Sin(Time.time * flashSpeed) + 1f) * 0.5f;
            bool isDetected = _currentFill >= 0.99f;

            // --- สีและ scale ของ Arc ---
            if (isDetected)
            {
                arcImage.color   = Color.Lerp(detectedColor * 0.55f, detectedColor, flash);
                _rect.localScale = _baseScale * Mathf.Lerp(1f, scaleDetected, flash);
            }
            else
            {
                arcImage.color   = Color.Lerp(suspiciousColor, detectedColor, _currentFill);
                _rect.localScale = _baseScale;
            }

            // --- Alert "!" Text ---
            if (alertText != null)
            {
                Color ac = alertText.color;
                ac.a = isDetected ? flash : 0f;
                alertText.color = ac;
            }

            // --- Eye icon tint ---
            if (eyeIcon != null)
            {
                Color eyeCol = Color.Lerp(safeEyeColor, detectedColor, _currentFill * _currentFill);
                eyeCol.a = Mathf.Max(safeEyeColor.a, _currentFill);
                eyeIcon.color = eyeCol;
            }

            // --- Fade Canvas Group ---
            float targetAlpha = (_currentFill > 0.01f) ? 1f : 0f;
            _cg.alpha = Mathf.MoveTowards(_cg.alpha, targetAlpha, Time.deltaTime * 5f);
        }

        public void ResetInstant()
        {
            _currentFill = 0f;
            _targetFill  = 0f;
            if (arcImage) arcImage.fillAmount = 0f;
            if (_cg) _cg.alpha = 0f;
            if (alertText) { Color c = alertText.color; c.a = 0f; alertText.color = c; }
        }
    }
}
