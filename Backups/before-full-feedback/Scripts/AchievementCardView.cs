using UnityEngine;
using UnityEngine.UI;

namespace Erudition
{
    public sealed class AchievementCardView : MonoBehaviour
    {
        public Text progressText;
        public RectTransform progressFill;
        public int target = 1;
        public float fullProgressWidth = 610;

        public void SetProgress(int count)
        {
            if (progressText != null) progressText.text = Mathf.Min(count, target) + "/" + target;
            if (progressFill != null)
            {
                var size = progressFill.sizeDelta;
                size.x = fullProgressWidth * Mathf.Clamp01((float)count / target);
                progressFill.sizeDelta = size;
            }
        }
    }
}
