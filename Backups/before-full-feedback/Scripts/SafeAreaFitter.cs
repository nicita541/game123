using UnityEngine;

namespace Erudition
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private Vector2 extraPadding = new Vector2(24, 32);
        [SerializeField] private Vector2 designSize = new Vector2(1080, 2340);
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
            FitNormalized(new Rect(area.x / Screen.width, area.y / Screen.height, area.width / Screen.width, area.height / Screen.height));
        }

        public void FitNormalized(Rect area)
        {
            if (target == null) target = GetComponent<RectTransform>();
            var parent = target.parent as RectTransform;
            if (parent == null) return;
            var available = new Vector2(parent.rect.width * area.width, parent.rect.height * area.height) - extraPadding * 2;
            var scale = Mathf.Min(available.x / designSize.x, available.y / designSize.y);
            target.anchorMin = target.anchorMax = new Vector2(area.center.x, area.center.y);
            target.pivot = new Vector2(.5f, .5f);
            target.sizeDelta = designSize;
            target.anchoredPosition = Vector2.zero;
            target.localScale = Vector3.one * Mathf.Max(.01f, scale);
        }
    }
}
