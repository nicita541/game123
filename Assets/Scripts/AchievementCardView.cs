using UnityEngine;
using UnityEngine.UI;

namespace Erudition
{
    public sealed class AchievementCardView : MonoBehaviour
    {
        public Text titleText;
        public Text descriptionText;
        public Image icon;
        public Text progressText;
        public RectTransform progressFill;
        public RectTransform progressTrack;
        public int target = 1;
        public float fullProgressWidth = 610;

        public void SetProgress(int count, bool rewardClaimed = false)
        {
            var complete = count >= target;
            if (progressText != null)
            {
                progressText.resizeTextForBestFit = true;
                progressText.resizeTextMinSize = 16;
                progressText.resizeTextMaxSize = Mathf.Max(16, progressText.fontSize);
                progressText.text = rewardClaimed ? "Получено" : complete ? "Забрать" : Mathf.Min(count, target) + "/" + target;
            }
            if (progressFill != null)
            {
                var size = progressFill.sizeDelta;
                size.x = (progressTrack == null ? fullProgressWidth : progressTrack.rect.width) * Mathf.Clamp01((float)count / target);
                progressFill.sizeDelta = size;
            }
        }
    }
}
