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
                var subtitle = card.transform.Find("Text_Subtitle")?.GetComponent<Text>();
                if (subtitle != null) subtitle.text = Subtitle(definition);
                card.picture.sprite = definition.picture;
                card.picture.preserveAspect = true;
                card.target = definition.ActualTarget(game.Entries);
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

        private static string Subtitle(CollectionDefinition definition)
        {
            switch (definition.title)
            {
                case "А. С. Пушкин": return "Стихи, поэмы, письма";
                case "Л. Н. Толстой": return "Романы, рассказы, мысли";
                case "Ф. М. Достоевский": return "Романы, повести, мысли";
                case "А. П. Чехов": return "Рассказы, пьесы, цитаты";
                case "Н. В. Гоголь": return "Повести, поэмы, проза";
                case "И. С. Тургенев": return "Проза, рассказы, мысли";
                case "Природа": return "Животные, растения, мир";
                case "Любовь": return "Чувства, отношения, письма";
                case "Мудрость": return "Мысли, афоризмы, цитаты";
                case "Поэзия": return "Стихи и поэмы";
                case "Романы": return "Романы и проза";
                case "Сказки": return "Волшебные истории";
            }
            return definition.group == CollectionGroup.Authors ? "Цитаты и произведения"
                : definition.group == CollectionGroup.Themes ? "Тематическая подборка"
                : definition.group == CollectionGroup.Books ? "Книги и произведения"
                : "Подборка уровней";
        }

        private CollectionCardView[] Group(CollectionGroup group) => Cards
            .Where((card, index) => catalog.cards[index].group == group).ToArray();

        public void Refresh(GameSave save, PuzzleEntry[] entries)
        {
            for (var i = 0; i < Cards.Length; i++)
            {
                Cards[i].target = catalog.cards[i].ActualTarget(entries);
                Cards[i].SetProgress(catalog.cards[i].Progress(save, entries));
            }
        }
    }
}
