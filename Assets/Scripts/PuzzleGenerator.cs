using System;
using System.Linq;

namespace Erudition
{
    public static class PuzzleGenerator
    {
        public static readonly int[] Thresholds = { 0, 60, 160, 320, 560 };
        public static int Tier(int erudition) => Math.Max(0, Array.FindLastIndex(Thresholds, value => value <= erudition));
        public static string DifficultyName(int erudition) => new[] { "Лёгкий", "Средний", "Сложный", "Эксперт", "Мастер" }[Tier(erudition)];
        public static string KindName(PuzzleKind kind) => new[] { "Цитаты и мысли", "Истории", "Пословицы", "Загадки" }[(int)kind];
        public static int HiddenLetters(int erudition, PuzzleMode mode)
            => (mode == PuzzleMode.Classic ? new[] { 2, 4, 6, 9, 12 } : new[] { 2, 3, 4, 5, 7 })[Tier(erudition)];
        public static int WordCount(string text) => text.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        public static string TextKey(string text) => new string(text.ToUpperInvariant().Replace('Ё', 'Е').Where(char.IsLetterOrDigit).ToArray());

        public static int StartingClues(int letters, int erudition)
        {
            if (letters <= 1) return 0;
            var amount = new[] { 6, 6, 5, 5, 6 }[Tier(erudition)];
            return Math.Min(letters - 1, amount);
        }

        // The live game now has one authored database: CollectionCatalog.
        // Legacy generators and PuzzleLibrary phrases are intentionally excluded.
        public static PuzzleEntry[] Build(PuzzleLibrary library, CollectionCatalog catalog = null)
        {
            if (catalog != null) return catalog.Build().ToArray();
            return library?.entries ?? Array.Empty<PuzzleEntry>();
        }
    }
}
