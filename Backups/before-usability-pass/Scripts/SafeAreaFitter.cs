using UnityEngine;

namespace Erudition
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private Vector2 extraPadding = new Vector2(24, 32);
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            if (target == null) target = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
                Apply();
        }

        private void Apply()
        {
            if (target == null || Screen.width <= 0 || Screen.height <= 0) return;
            var area = Screen.safeArea;
            lastSafeArea = area;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            target.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            target.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            target.offsetMin = extraPadding;
            target.offsetMax = -extraPadding;
        }
    }
}
