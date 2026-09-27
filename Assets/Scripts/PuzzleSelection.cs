using System;
using System.Collections.Generic;
using System.Linq;

namespace Erudition
{
    public static class PuzzleSelection
    {
        // Pick a difficulty, then a kind, then a text. Thousands of generated
        // stories must not drown out the smaller authored collections.
        public static int Choose(PuzzleEntry[] entries, PuzzleMode mode, int erudition,
            IList<string> recent, Func<string, bool> canFit, Func<int, int> random)
        {
            var eligible = Enumerable.Range(0, entries.Length)
                .Where(i => entries[i].mode == mode && entries[i].minimumErudition <= erudition)
                .GroupBy(i => PuzzleGenerator.TextKey(entries[i].text))
                .Select(g => g.OrderBy(i => entries[i].minimumErudition).First()).ToList();
            // Relax only the oldest history entries if a small pool is exhausted.
            for (var skip = 0; skip <= recent.Count; skip++)
            {
                var excluded = new HashSet<string>(recent.Skip(skip));
                var pool = eligible.Where(i => !excluded.Contains(PuzzleGenerator.TextKey(entries[i].text))).ToList();
                while (pool.Count > 0)
                {
                    var tiers = pool.GroupBy(i => PuzzleGenerator.Tier(entries[i].minimumErudition)).ToArray();
                    var kinds = tiers[random(tiers.Length)].GroupBy(i => entries[i].kind).ToArray();
                    var texts = kinds[random(kinds.Length)].ToArray();
                    var chosen = texts[random(texts.Length)];
                    if (canFit(entries[chosen].text)) return chosen;
                    pool.Remove(chosen);
                }
            }
            return -1;
        }
    }
}
