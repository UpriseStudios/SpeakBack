using UnityEngine;

namespace Uprise.SpeakBack.UI
{
    /// <summary>
    /// Fits its RectTransform to <see cref="Screen.safeArea"/> so content avoids notches and
    /// gesture bars. Re-applies only when the safe area or screen size changes.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _appliedSafeArea;
        private Vector2Int _appliedScreenSize;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != _appliedSafeArea ||
                Screen.width != _appliedScreenSize.x ||
                Screen.height != _appliedScreenSize.y)
            {
                Apply();
            }
        }

        private void Apply()
        {
            _appliedSafeArea = Screen.safeArea;
            _appliedScreenSize = new Vector2Int(Screen.width, Screen.height);

            ComputeAnchors(_appliedSafeArea, _appliedScreenSize, out var min, out var max);
            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }

        /// <summary>Converts a pixel safe area into normalised anchors; falls back to full screen for a zero-sized screen.</summary>
        public static void ComputeAnchors(Rect safeArea, Vector2Int screenSize, out Vector2 anchorMin, out Vector2 anchorMax)
        {
            if (screenSize.x <= 0 || screenSize.y <= 0)
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                return;
            }

            anchorMin = new Vector2(
                Mathf.Clamp01(safeArea.xMin / screenSize.x),
                Mathf.Clamp01(safeArea.yMin / screenSize.y));
            anchorMax = new Vector2(
                Mathf.Clamp01(safeArea.xMax / screenSize.x),
                Mathf.Clamp01(safeArea.yMax / screenSize.y));
        }
    }
}
