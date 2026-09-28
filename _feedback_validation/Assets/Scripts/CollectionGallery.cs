using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Erudition
{
    public sealed class CollectionGallery : MonoBehaviour
    {
        public CollectionCatalog catalog;
        public CollectionCardView template;
        public RectTransform[] containers;
        public CollectionCardView[] Cards { get; private set; } = new CollectionCardView[0];

        public void Build(CryptogramGame game)
        {
            Cards = new CollectionCardView[catalog.cards.Length];
            for (var i = 0; i < Cards.Length; i++)
            {
                var definition = catalog.cards[i];
                var card = Instantiate(template, containers[(int)definition.group]);
                card.name = "Card_" + definition.id;
                card.title.text = definition.title;
                card.picture.sprite = definition.picture;
                card.picture.preserveAspect = true;
                card.target = definition.Target;
                foreach (var action in card.GetComponentsInChildren<UiAction>(true))
                    action.Configure(game, UiActionKind.CollectionDetails, i);
                card.gameObject.SetActive(true);
                Cards[i] = card;
            }
            template.gameObject.SetActive(false);
            game.authorCards = Group(CollectionGroup.Authors);
            game.themeCards = Group(CollectionGroup.Themes);
            game.bookCards = Group(CollectionGroup.Books);
            game.kindCards = Group(CollectionGroup.Kinds);
        }

        private CollectionCardView[] Group(CollectionGroup group) => Cards
            .Where((card, index) => catalog.cards[index].group == group).ToArray();

        public void Refresh(GameSave save, PuzzleEntry[] entries)
        {
            for (var i = 0; i < Cards.Length; i++)
                Cards[i].SetProgress(catalog.cards[i].Progress(save, entries));
        }
    }
}
