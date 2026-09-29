using UnityEngine;
using UnityEngine.UI;

namespace Erudition
{
    public enum AchievementMetric
    {
        Solved,
        EruditionLevels,
        ClassicSolved,
        UniqueSolved,
        PerfectWins,
        BestEveningStreak
    }

    public enum AchievementRewardKind
    {
        Coins,
        Hints,
        Feathers,
        InfiniteFeathers
    }

    public sealed class AchievementCardView : MonoBehaviour
    {
        public Text titleText;
        public Text descriptionText;
        public Image icon;
        public AchievementMetric metric;
        public bool special;
        public AchievementRewardKind rewardKind;
        [Min(0)] public int rewardAmount = 100;
        [Range(0, 30)] public int claimBit;
        public Text progressText;
        public RectTransform progressFill;
        public RectTransform progressTrack;
        public int target = 1;
        public float fullProgressWidth = 610;

        public string RewardDescription()
        {
            switch (rewardKind)
            {
                case AchievementRewardKind.Coins:
                    return rewardAmount + " монет — можно потратить в магазине на перья и подсказки.";
                case AchievementRewardKind.Hints:
                    return rewardAmount + " подсказок — добавятся в запас и помогут открыть буквы в сложных криптограммах.";
                case AchievementRewardKind.Feathers:
                    return rewardAmount + " перьев — добавятся поверх текущего запаса.";
                case AchievementRewardKind.InfiniteFeathers:
                    var duration = rewardAmount == 60 ? "1 час" : rewardAmount + " мин.";
                    return duration + " бесконечных перьев — уровни запускаются без расхода перьев. После получения появится таймер действия бонуса.";
                default:
                    return "Награда за выполнение достижения.";
            }
        }

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
