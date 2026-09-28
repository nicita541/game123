using System.Linq;
using Erudition;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CollectionCatalog))]
public sealed class CollectionCatalogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Карточка = название + картинка Sprite + вкладка + фразы. " +
            "Карточки и прогресс создаются автоматически. ID назначаются автоматически; порядок можно менять. " +
            "Инструкция: Docs/ADDING_CONTENT.md", MessageType.Info);
        DrawDefaultInspector();
        var catalog = (CollectionCatalog)target;
        if (GUILayout.Button("Добавить новую карточку"))
        {
            Undo.RecordObject(catalog, "Add collection");
            catalog.cards = catalog.cards.Concat(new[] { new CollectionDefinition { title = "Новая коллекция" } }).ToArray();
            catalog.EnsureIds();
            EditorUtility.SetDirty(catalog);
        }
        if (GUILayout.Button("Проверить контент"))
        {
            catalog.EnsureIds();
            EditorUtility.SetDirty(catalog);
            var warnings = 0;
            var board = Object.FindFirstObjectByType<PuzzleBoard>(FindObjectsInactive.Include);
            foreach (var card in catalog.cards)
            {
                if (string.IsNullOrWhiteSpace(card.title) || card.picture == null)
                { Debug.LogWarning("Укажите название и картинку: " + card.title, catalog); warnings++; }
                if (!card.includeLegacy && card.phrases.Length == 0)
                { Debug.LogWarning("Нет фраз: " + card.title, catalog); warnings++; }
                foreach (var phrase in card.phrases)
                {
                    if (!CollectionCatalog.ValidText(phrase.text))
                    { Debug.LogWarning("Нужен непустой текст с русскими буквами: " + card.title, catalog); warnings++; }
                    else if (board != null && !board.CanFit(phrase.text))
                    { Debug.LogWarning("Фраза не помещается в сетку: " + phrase.text, catalog); warnings++; }
                }
            }
            if (board == null) Debug.Log("Для проверки длины фраз откройте GameplayScene и повторите проверку.", catalog);
            Debug.Log("Каталог: " + catalog.cards.Length + " карточек, " + catalog.Build().Count() + " новых фраз, предупреждений: " + warnings, catalog);
        }
    }
}
