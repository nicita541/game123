using System.Linq;
using Erudition;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CollectionCatalog))]
public sealed class CollectionCatalogEditor : Editor
{
    private const string CatalogPath = "Assets/Data/CollectionCatalog.asset";

    public override void OnInspectorGUI()
    {
        var catalog = (CollectionCatalog)target;

        EditorGUILayout.HelpBox("Карточка = название + картинка Sprite + вкладка + фразы. " +
            "После любых изменений контента нажмите «Пересчитать runtime-кэш». " +
            "Во время игры количество фраз и диапазоны карточек больше не вычисляются.", MessageType.Info);

        if (DrawDefaultInspector())
        {
            catalog.cacheVersion = 0;
            EditorUtility.SetDirty(catalog);
        }

        EditorGUILayout.Space(8);
        var cacheOk = catalog.CacheReady;
        EditorGUILayout.HelpBox(cacheOk
            ? "Runtime-кэш готов: " + catalog.cachedEntryTotal + " фраз."
            : "Runtime-кэш устарел. Перед Play/Build пересчитайте его.",
            cacheOk ? MessageType.Info : MessageType.Warning);

        if (GUILayout.Button("Пересчитать runtime-кэш"))
            RebuildAndSave(catalog);

        if (GUILayout.Button("Добавить новую карточку (5 уровней × 10 фраз)"))
        {
            Undo.RecordObject(catalog, "Add collection");
            catalog.cards = catalog.cards.Concat(new[] { CreateCardTemplate() }).ToArray();
            catalog.EnsureIds();
            catalog.cacheVersion = 0;
            EditorUtility.SetDirty(catalog);
            serializedObject.Update();
        }

        if (GUILayout.Button("Проверить контент"))
        {
            catalog.EnsureIds();
            EditorUtility.SetDirty(catalog);
            var warnings = 0;
            var board = Object.FindFirstObjectByType<PuzzleBoard>(FindObjectsInactive.Include);
            var allTexts = new System.Collections.Generic.HashSet<string>();
            foreach (var card in catalog.cards)
            {
                if (string.IsNullOrWhiteSpace(card.title) || card.picture == null)
                { Debug.LogWarning("Укажите название и картинку: " + card.title, catalog); warnings++; }
                if (string.IsNullOrWhiteSpace(card.subtitle))
                { Debug.LogWarning("Укажите подзаголовок карточки: " + card.title, catalog); warnings++; }

                for (var tier = 0; tier < PuzzleGenerator.Thresholds.Length; tier++)
                {
                    var threshold = PuzzleGenerator.Thresholds[tier];
                    var tierCount = card.phrases.Count(phrase => phrase != null
                        && CollectionCatalog.ValidText(phrase.text)
                        && phrase.minimumErudition == threshold);
                    if (tierCount != 10)
                    {
                        Debug.LogWarning(card.title + ": для уровня " + PuzzleGenerator.DifficultyName(threshold)
                            + " нужно 10 фраз, сейчас " + tierCount, catalog);
                        warnings++;
                    }
                }

                foreach (var phrase in card.phrases)
                {
                    if (!CollectionCatalog.ValidText(phrase.text))
                    { Debug.LogWarning("Нужен непустой текст с русскими буквами: " + card.title, catalog); warnings++; }
                    else
                    {
                        var key = PuzzleGenerator.TextKey(phrase.text);
                        if (!allTexts.Add(key))
                        { Debug.LogWarning("Повтор текста: " + phrase.text, catalog); warnings++; }
                        if (board != null && !board.CanFit(phrase.text))
                        { Debug.LogWarning("Фраза не помещается в сетку: " + phrase.text, catalog); warnings++; }
                    }
                }
            }
            if (board == null) Debug.Log("Для проверки длины фраз откройте GameplayScene и повторите проверку.", catalog);
            Debug.Log("Каталог: " + catalog.cards.Length + " карточек, " + catalog.Build().Count()
                + " фраз, предупреждений: " + warnings + ". Кэш: "
                + (catalog.CacheReady ? "готов" : "нужно пересчитать"), catalog);
        }
    }

    private static CollectionDefinition CreateCardTemplate()
    {
        var phrases = PuzzleGenerator.Thresholds
            .SelectMany(threshold => Enumerable.Range(0, 10)
                .Select(_ => new CollectionPhrase
                {
                    text = "",
                    source = "",
                    minimumErudition = threshold,
                    kind = PuzzleKind.Quotes,
                    answer = ""
                }))
            .ToArray();

        return new CollectionDefinition
        {
            title = "Новая коллекция",
            subtitle = "Краткое описание",
            group = CollectionGroup.Authors,
            phrases = phrases
        };
    }

    [MenuItem("Tools/Erudition/Пересчитать кэш уровней")]
    public static void RebuildMenu()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CollectionCatalog>(CatalogPath);
        if (catalog == null)
        {
            Debug.LogError("Не найден " + CatalogPath);
            return;
        }
        RebuildAndSave(catalog);
        Selection.activeObject = catalog;
    }

    private static void RebuildAndSave(CollectionCatalog catalog)
    {
        Undo.RecordObject(catalog, "Rebuild level runtime cache");
        catalog.RebuildRuntimeCache();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log("LEVEL_CACHE_READY: " + catalog.cards.Length + " карточек, "
            + catalog.cachedEntryTotal + " фраз. Runtime больше не пересчитывает статические значения.", catalog);
    }
}
