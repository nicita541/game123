using System;

namespace Erudition
{
    public struct PuzzleReward
    {
        public int points, basePoints, independence, accuracy, pace;
        public float multiplier;

        public static PuzzleReward Calculate(int puzzleErudition, PuzzleProgress progress)
        {
            var tier = PuzzleGenerator.Tier(puzzleErudition);
            var reward = new PuzzleReward { basePoints = new[] { 5, 10, 15, 20, 25 }[tier] };
            reward.independence = progress.hintsUsed == 0 ? 2 : progress.hintsUsed == 1 ? 1 : 0;
            reward.accuracy = progress.mistakesInLevel == 0 ? 2 : progress.mistakesInLevel == 1 ? 1 : 0;
            // Active play only; a generous time allowance scales with the cells
            // actually hidden at the start, including legacy saved attempts.
            reward.pace = progress.scoringRevision > 0 && progress.initialHiddenLetters > 0
                && progress.activeSeconds <= Math.Max(60, progress.initialHiddenLetters * 8) ? 1 : 0;
            var gap = Math.Max(0, progress.playerTierAtStart - tier);
            reward.multiplier = 1f / (1 + gap);
            reward.points = Math.Max(1, (int)Math.Floor((reward.basePoints + reward.independence + reward.accuracy + reward.pace) * reward.multiplier));
            return reward;
        }

        public string Explanation => "Основа " + basePoints + " · без подсказок +" + independence
            + " · точность +" + accuracy + " · темп +" + pace
            + (multiplier < 1 ? "\nЗадание ниже твоего уровня: ×" + multiplier.ToString("0.##") : "");
    }
}
