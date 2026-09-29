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
        public Sprite picture;
        public CollectionGroup group;
        public CollectionPhrase[] phrases = Array.Empty<CollectionPhrase>();
        // Compatibility with the original catalogue and accumulated counters.
        [HideInInspector] public bool includeLegacy;
        [HideInInspector] public CollectionGroup legacyGroup;
        [HideInInspector] public int legacyIndex;
        [HideInInspector] public int legacyTarget;

        public bool Contains(PuzzleEntry entry)
        {
            if (entry.collectionId == id) return true;
            // The level picker deduplicates identical texts. Solving that text
            // through another collection must still count toward this one.
            if (phrases.Any(p => p != null && CollectionCatalog.ValidText(p.text)
                && PuzzleGenerator.TextKey(p.text) == PuzzleGenerator.TextKey(entry.text))) return true;
            if (!includeLegacy) return false;
            return legacyGroup == CollectionGroup.Authors ? entry.authorIndex == legacyIndex
                : legacyGroup == CollectionGroup.Themes ? entry.themeIndex == legacyIndex
                : legacyGroup == CollectionGroup.Books ? entry.bookIndex == legacyIndex
                : (int)entry.kind == legacyIndex;
        }

        // LegacyTarget is kept only so old serialized assets stay compatible.
        // Runtime totals always come from the real unique phrases currently present in Entries.
        public int Target => includeLegacy ? Math.Max(1, legacyTarget) : Math.Max(1,
            phrases.Where(p => p != null && CollectionCatalog.ValidText(p.text))
                .Select(p => PuzzleGenerator.TextKey(p.text)).Distinct().Count());

        public int ActualTarget(PuzzleEntry[] entries)
        {
            if (entries == null) return Target;
            return entries.Where(Contains)
                .Select(e => PuzzleGenerator.TextKey(e.text))
                .Distinct()
                .Count();
        }

        public int Progress(GameSave save, PuzzleEntry[] entries)
        {
            if (save == null || entries == null) return 0;
            var solved = new HashSet<string>(save.solvedPuzzleIds.Split('|'));
            return entries.Where(e => solved.Contains(e.id) && Contains(e))
                .Select(e => PuzzleGenerator.TextKey(e.text))
                .Distinct()
                .Count();
        }
    }

    [CreateAssetMenu(fileName = "CollectionCatalog", menuName = "Erudition/Collection Catalog")]
    public sealed class CollectionCatalog : ScriptableObject
    {
        public CollectionDefinition[] cards = Array.Empty<CollectionDefinition>();

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
        private void OnValidate() { EnsureIds(); }
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
#endif
    }
}
