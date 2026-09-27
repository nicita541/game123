using UnityEngine;
using UnityEngine.UI;

namespace Erudition
{
    public sealed class CollectionCardView : MonoBehaviour
    {
        public Text title;
        public Text progressText;
        public RectTransform progressFill;
        public RectTransform progressTrack;
        public int target = 20;
        public float fullProgressWidth = 210;

        public void SetProgress(int count)
        {
            if (progressText != null) progressText.text = Mathf.Min(count, target) + "/" + target;
            if (progressFill != null)
            {
                var size = progressFill.sizeDelta;
                size.x = (progressTrack == null ? fullProgressWidth : progressTrack.rect.width) * Mathf.Clamp01((float)count / target);
                progressFill.sizeDelta = size;
            }
        }
    }
}
