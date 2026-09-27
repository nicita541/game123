using UnityEngine;
using UnityEngine.UI;

namespace Erudition
{
    // Authored illustrations are untouched by default. A separate optional edge
    // underlay extends their borders outside the device safe area.
    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    public sealed class SceneBackdrop : MonoBehaviour
    {
        public RectTransform artworkFrame;
        public RawImage picture;
        [Tooltip("Optional full-screen extension. Leave off to edit the background RectTransform freely.")]
        public bool extendBeyondSafeArea;
        private readonly Vector3[] corners = new Vector3[4];

        private void OnEnable() { Fit(); }
        private void LateUpdate() { Fit(); }

        public void Fit()
        {
            if (!extendBeyondSafeArea) return;
            if (picture == null || artworkFrame == null) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var canvasRect = (RectTransform)canvas.rootCanvas.transform;
            var parent = picture.rectTransform.parent as RectTransform;
            if (parent == null || artworkFrame.rect.width <= 0 || artworkFrame.rect.height <= 0) return;
            canvasRect.GetWorldCorners(corners);
            var lower = parent.InverseTransformPoint(corners[0]);
            var upper = parent.InverseTransformPoint(corners[2]);
            var frameLower = artworkFrame.InverseTransformPoint(corners[0]);
            var frameUpper = artworkFrame.InverseTransformPoint(corners[2]);
            var r = picture.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
            r.pivot = new Vector2(.5f, .5f);
            r.localPosition = (lower + upper) * .5f;
            r.sizeDelta = new Vector2(upper.x - lower.x, upper.y - lower.y);
            var frame = artworkFrame.rect;
            picture.uvRect = new Rect((frameLower.x-frame.xMin)/frame.width, (frameLower.y-frame.yMin)/frame.height,
                (frameUpper.x-frameLower.x)/frame.width, (frameUpper.y-frameLower.y)/frame.height);
        }
    }
}
