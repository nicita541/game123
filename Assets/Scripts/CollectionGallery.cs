using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Erudition
{
    public sealed class CollectionGallery : MonoBehaviour
    {
        public CollectionCatalog catalog;
        public CollectionCardView template;
        public RectTransform authorsContainer;
        public RectTransform themesContainer;
        public RectTransform booksContainer;
        public RectTransform kindsContainer;
        public CollectionCardView[] Cards { get; private set; } = new CollectionCardView[0];

        private string lastSolvedIds;

        public void Build(CryptogramGame game)
        {
            if (catalog == null) return;
            if (!catalog.CacheReady)
                Debug.LogError("CollectionCatalog cache is stale. In Unity run Tools/Erudition/Пересчитать кэш уровней.", catalog);

            Cards = new CollectionCardView[catalog.cards.Length];
            for (var i = 0; i < Cards.Length; i++)
            {
                var definition = catalog.cards[i];
                var container = ContainerFor(definition.group);
                if (container == null)
                {
                    Debug.LogError("Не назначен контейнер для вкладки " + definition.group + ": " + definition.title, this);
                    continue;
                }

                var card = Instantiate(template, container);
                card.name = "Card_" + definition.id;
                if (card.title != null) card.title.text = definition.title;
                if (card.subtitle != null) card.subtitle.text = definition.DisplaySubtitle;
                card.picture.sprite = definition.picture;
                card.picture.preserveAspect = true;
                card.target = definition.Target;
                foreach (var action in card.GetComponentsInChildren<UiAction>(true))
                    action.Configure(game, UiActionKind.CollectionLevel, i);
                card.gameObject.SetActive(true);
                Cards[i] = card;
            }
            template.gameObject.SetActive(false);
            game.authorCards = Group(CollectionGroup.Authors);
            game.themeCards = Group(CollectionGroup.Themes);
            game.bookCards = Group(CollectionGroup.Books);
            game.kindCards = Group(CollectionGroup.Kinds);
        }

        private RectTransform ContainerFor(CollectionGroup group)
        {
            switch (group)
            {
                case CollectionGroup.Authors: return authorsContainer;
                case CollectionGroup.Themes: return themesContainer;
                case CollectionGroup.Books: return booksContainer;
                case CollectionGroup.Kinds: return kindsContainer;
                default: return null;
            }
        }

        private CollectionCardView[] Group(CollectionGroup group) => Cards
            .Where((card, index) => card != null && catalog.cards[index].group == group).ToArray();

        public void Refresh(GameSave save, PuzzleEntry[] entries)
        {
            if (save == null || entries == null || Cards == null) return;
            var solvedIdsText = save.solvedPuzzleIds ?? "";
            if (solvedIdsText == lastSolvedIds) return;

            var solved = new HashSet<string>(solvedIdsText.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
            for (var i = 0; i < Cards.Length; i++)
                if (Cards[i] != null)
                    Cards[i].SetProgress(catalog.cards[i].Progress(solved, entries));
            lastSolvedIds = solvedIdsText;
        }
    }
}
