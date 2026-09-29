using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Erudition
{
    public enum CollectionGroup { Authors, Themes, Books, Kinds }

    [Serializable]
    public sealed class CollectionPhrase
    {
        [HideInInspector] public string id;
        [TextArea(2, 6)] public string text;
        [Tooltip("Если пусто, используется название карточки.")] public string source;
        public PuzzleKind kind;
        [Min(0)] public int minimumErudition;
        public string answer;
    }

    [Serializable]
    public sealed class CollectionDefinition
    {
        [HideInInspector] public string id;
        public string title;
        [Tooltip("Короткая подпись под названием карточки.")] public string subtitle;
        public Sprite picture;
        public CollectionGroup group;

        public string DisplaySubtitle
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(subtitle)) return subtitle;
                return group == CollectionGroup.Authors ? "Цитаты и произведения"
                    : group == CollectionGroup.Themes ? "Тематическая подборка"
                    : group == CollectionGroup.Books ? "Книги и произведения"
                    : "Подборка уровней";
            }
        }
        public CollectionPhrase[] phrases = Array.Empty<CollectionPhrase>();

        // Compatibility with old serialized assets.
        [HideInInspector] public bool includeLegacy;
        [HideInInspector] public CollectionGroup legacyGroup;
        [HideInInspector] public int legacyIndex;
        [HideInInspector] public int legacyTarget;

        // Precomputed in the Unity Editor. Runtime never recalculates these values.
        [HideInInspector] public int cachedEntryStart;
        [HideInInspector] public int cachedEntryCount;
        [HideInInspector] public int cachedTarget;

        public int Target => Math.Max(0, cachedTarget);

        public bool Contains(PuzzleEntry entry)
        {
            if (entry == null) return false;
            if (entry.collectionId == id) return true;
            if (!includeLegacy) return false;
            return legacyGroup == CollectionGroup.Authors ? entry.authorIndex == legacyIndex
                : legacyGroup == CollectionGroup.Themes ? entry.themeIndex == legacyIndex
                : legacyGroup == CollectionGroup.Books ? entry.bookIndex == legacyIndex
                : (int)entry.kind == legacyIndex;
        }

        public bool TryGetCachedRange(int entriesLength, out int start, out int count)
        {
            start = cachedEntryStart;
            count = cachedEntryCount;
            return start >= 0 && count >= 0 && start <= entriesLength && start + count <= entriesLength;
        }

        public int Progress(ISet<string> solvedIds, PuzzleEntry[] entries)
        {
            if (solvedIds == null || entries == null || !TryGetCachedRange(entries.Length, out var start, out var count))
                return 0;
            var solved = 0;
            var end = start + count;
            for (var i = start; i < end; i++)
                if (entries[i] != null && solvedIds.Contains(entries[i].id)) solved++;
            return Math.Min(solved, Target);
        }

        public int Progress(GameSave save, PuzzleEntry[] entries)
        {
            if (save == null) return 0;
            var solved = new HashSet<string>(save.solvedPuzzleIds.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
            return Progress(solved, entries);
        }
    }

    [CreateAssetMenu(fileName = "CollectionCatalog", menuName = "Erudition/Collection Catalog")]
    public sealed class CollectionCatalog : ScriptableObject
    {
        public const int RuntimeCacheVersion = 1;

        public CollectionDefinition[] cards = Array.Empty<CollectionDefinition>();
        [HideInInspector] public int cacheVersion;
        [HideInInspector] public int cachedEntryTotal;

        public bool CacheReady => cacheVersion == RuntimeCacheVersion && cachedEntryTotal >= 0;

        public static bool ValidText(string text) => !string.IsNullOrWhiteSpace(text)
            && text.Any(c => c >= 'А' && c <= 'я' || c == 'Ё' || c == 'ё')
            && text.All(c => !char.IsLetter(c) || c >= 'А' && c <= 'я' || c == 'Ё' || c == 'ё');

        public IEnumerable<PuzzleEntry> Build()
        {
            foreach (var card in cards)
                foreach (var phrase in card.phrases ?? Array.Empty<CollectionPhrase>())
                {
                    if (phrase == null || !ValidText(phrase.text) || string.IsNullOrEmpty(phrase.id)) continue;
                    yield return new PuzzleEntry {
                        id = phrase.id, collectionId = card.id, text = phrase.text.Trim(),
                        source = string.IsNullOrWhiteSpace(phrase.source) ? card.title : phrase.source,
                        minimumErudition = phrase.minimumErudition, kind = phrase.kind, answer = phrase.answer,
                        authorIndex = card.includeLegacy && card.legacyGroup == CollectionGroup.Authors ? card.legacyIndex : -1,
                        themeIndex = card.includeLegacy && card.legacyGroup == CollectionGroup.Themes ? card.legacyIndex : -1,
                        bookIndex = card.includeLegacy && card.legacyGroup == CollectionGroup.Books ? card.legacyIndex : -1
                    };
                }
        }

#if UNITY_EDITOR
        public void EnsureIds()
        {
            var ids = new HashSet<string>();
            foreach (var card in cards)
            {
                if (string.IsNullOrEmpty(card.id) || !ids.Add(card.id))
                { card.id = Guid.NewGuid().ToString("N"); ids.Add(card.id); }
                foreach (var phrase in card.phrases ?? Array.Empty<CollectionPhrase>())
                {
                    if (phrase == null) continue;
                    if (string.IsNullOrEmpty(phrase.id) || !ids.Add(phrase.id))
                    { phrase.id = "phrase_" + Guid.NewGuid().ToString("N"); ids.Add(phrase.id); }
                }
            }
        }

        public void RebuildRuntimeCache()
        {
            EnsureIds();
            var entryStart = 0;
            foreach (var card in cards)
            {
                var valid = (card.phrases ?? Array.Empty<CollectionPhrase>())
                    .Where(phrase => phrase != null && ValidText(phrase.text) && !string.IsNullOrEmpty(phrase.id))
                    .ToArray();
                card.cachedEntryStart = entryStart;
                card.cachedEntryCount = valid.Length;
                card.cachedTarget = valid.Select(phrase => PuzzleGenerator.TextKey(phrase.text)).Distinct().Count();
                entryStart += valid.Length;
            }
            cachedEntryTotal = entryStart;
            cacheVersion = RuntimeCacheVersion;
        }
#endif
    }
}
