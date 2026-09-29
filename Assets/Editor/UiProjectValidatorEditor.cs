#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UiProjectValidatorEditor
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private const string GameplayScenePath = "Assets/Scenes/GameplayScene.unity";
    private const string CatalogPath = "Assets/Data/CollectionCatalog.asset";

    private static readonly HashSet<string> AllowedScreenGroups = new HashSet<string>(StringComparer.Ordinal)
    {
        "Backgrounds", "Header", "Content", "Actions", "Navigation", "Overlay", "Input",
        "EnergyStatus", "EnergyInfo", "DebugActions", "Footer"
    };

    private sealed class Report
    {
        public int errors;
        public int warnings;
        public readonly List<string> lines = new List<string>();

        public void Error(string message)
        {
            errors++;
            lines.Add("✗ " + message);
            Debug.LogError("[UI Check] " + message);
        }

        public void Warn(string message)
        {
            warnings++;
            lines.Add("! " + message);
            Debug.LogWarning("[UI Check] " + message);
        }

        public void Ok(string message)
        {
            lines.Add("✓ " + message);
        }
    }

    [MenuItem("Tools/Erudition/UI/Проверить проект")]
    public static void ValidateProject()
    {
        var report = new Report();
        ValidateScene(MainScenePath, report);
        ValidateScene(GameplayScenePath, report);
        ValidateCatalog(report);

        var title = report.errors == 0 ? "UI-проект в порядке" : "Найдены проблемы UI";
        var summary = "Ошибок: " + report.errors + "\nПредупреждений: " + report.warnings;
        var details = string.Join("\n", report.lines.Take(18));
        if (report.lines.Count > 18) details += "\n… ещё " + (report.lines.Count - 18) + " строк в Console.";

        EditorUtility.DisplayDialog(title, summary + "\n\n" + details, "ОК");
        Debug.Log("[UI Check] Завершено. " + summary.Replace("\n", ", "));
    }

    private static void ValidateScene(string path, Report report)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
        {
            report.Error("Не найдена сцена: " + path);
            return;
        }

        var scene = SceneManager.GetSceneByPath(path);
        var openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        try
        {
            var objects = AllObjects(scene).ToArray();
            var screens = objects.Where(go => go.name.StartsWith("Screen_", StringComparison.Ordinal)).ToArray();
            if (screens.Length == 0)
                report.Error(path + ": нет объектов Screen_*.");

            foreach (var duplicate in screens.GroupBy(go => go.name).Where(group => group.Count() > 1))
                report.Error(path + ": повторяется экран " + duplicate.Key + " (" + duplicate.Count() + ").");

            foreach (var screen in screens)
                ValidateScreen(screen, report);

            foreach (var oldBackground in objects.Where(go =>
                         go.name == "Background_EdgeExtension_AutoFit" || go.name == "Illustration_Background"))
                report.Warn(path + ": осталось старое имя фона " + HierarchyPath(oldBackground) + ".");

            if (path == MainScenePath)
            {
                var gallery = objects.Select(go => go.GetComponent<CollectionGallery>())
                    .FirstOrDefault(component => component != null);
                if (gallery == null)
                {
                    report.Error("MainScene: не найден CollectionGallery.");
                }
                else
                {
                    if (gallery.catalog == null) report.Error("CollectionGallery: не назначен catalog.");
                    if (gallery.template == null) report.Error("CollectionGallery: не назначен template.");
                    if (gallery.authorsContainer == null) report.Error("CollectionGallery: не назначен authorsContainer.");
                    if (gallery.themesContainer == null) report.Error("CollectionGallery: не назначен themesContainer.");
                    if (gallery.booksContainer == null) report.Error("CollectionGallery: не назначен booksContainer.");
                    if (gallery.kindsContainer == null) report.Error("CollectionGallery: не назначен kindsContainer.");
                    if (gallery.catalog != null && gallery.template != null
                        && gallery.authorsContainer != null && gallery.themesContainer != null
                        && gallery.booksContainer != null && gallery.kindsContainer != null)
                        report.Ok("CollectionGallery: ссылки назначены.");
                }
            }

            report.Ok(path + ": экранов " + screens.Length + ".");
        }
        finally
        {
            if (openedHere && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void ValidateScreen(GameObject screen, Report report)
    {
        var children = Enumerable.Range(0, screen.transform.childCount)
            .Select(index => screen.transform.GetChild(index).gameObject)
            .ToArray();

        if (!children.Any(child => child.name == "Content"))
            report.Warn(HierarchyPath(screen) + ": нет группы Content.");

        foreach (var duplicate in children.GroupBy(child => child.name).Where(group => group.Count() > 1))
            report.Error(HierarchyPath(screen) + ": две группы с именем " + duplicate.Key + ".");

        foreach (var child in children)
        {
            if (!AllowedScreenGroups.Contains(child.name))
                report.Warn(HierarchyPath(child) + ": объект лежит прямо в Screen_*; лучше поместить в логическую группу.");
        }

        var backgrounds = children.FirstOrDefault(child => child.name == "Backgrounds");
        if (backgrounds != null)
        {
            for (var i = 0; i < backgrounds.transform.childCount; i++)
            {
                var background = backgrounds.transform.GetChild(i).gameObject;
                if (!background.name.StartsWith("BG_", StringComparison.Ordinal))
                    report.Warn(HierarchyPath(background) + ": фон лучше назвать BG_<Screen>_<Role>.");
            }
        }
    }

    private static void ValidateCatalog(Report report)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CollectionCatalog>(CatalogPath);
        if (catalog == null)
        {
            report.Error("Не найден каталог: " + CatalogPath);
            return;
        }

        var cardIds = new HashSet<string>(StringComparer.Ordinal);
        var phraseIds = new HashSet<string>(StringComparer.Ordinal);
        var textKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var card in catalog.cards ?? Array.Empty<CollectionDefinition>())
        {
            if (card == null)
            {
                report.Error("CollectionCatalog: найдена пустая карточка.");
                continue;
            }

            var label = string.IsNullOrWhiteSpace(card.title) ? "<без названия>" : card.title;
            if (string.IsNullOrWhiteSpace(card.id) || !cardIds.Add(card.id))
                report.Error(label + ": пустой или повторяющийся id карточки.");
            if (string.IsNullOrWhiteSpace(card.title))
                report.Error("Карточка " + card.id + ": не указано название.");
            if (string.IsNullOrWhiteSpace(card.subtitle))
                report.Warn(label + ": не указан подзаголовок.");
            if (card.picture == null)
                report.Warn(label + ": не назначена картинка.");

            var phrases = card.phrases ?? Array.Empty<CollectionPhrase>();
            foreach (var threshold in PuzzleGenerator.Thresholds)
            {
                var count = phrases.Count(phrase => phrase != null
                    && CollectionCatalog.ValidText(phrase.text)
                    && phrase.minimumErudition == threshold);
                if (count != 10)
                    report.Warn(label + " / " + PuzzleGenerator.DifficultyName(threshold)
                        + ": нужно 10 фраз, сейчас " + count + ".");
            }

            foreach (var phrase in phrases.Where(phrase => phrase != null))
            {
                if (string.IsNullOrWhiteSpace(phrase.id) || !phraseIds.Add(phrase.id))
                    report.Error(label + ": пустой или повторяющийся id фразы.");
                if (!CollectionCatalog.ValidText(phrase.text))
                    continue;

                var key = PuzzleGenerator.TextKey(phrase.text);
                if (!textKeys.Add(key))
                    report.Warn(label + ": повтор текста «" + phrase.text + "».");
            }
        }

        if (!catalog.CacheReady)
            report.Warn("CollectionCatalog: runtime-кэш устарел.");
        else
            report.Ok("CollectionCatalog: " + catalog.cards.Length + " карточек, кэш готов.");
    }

    private static IEnumerable<GameObject> AllObjects(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                yield return transform.gameObject;
    }

    private static string HierarchyPath(GameObject go)
    {
        var parts = new Stack<string>();
        var current = go == null ? null : go.transform;
        while (current != null)
        {
            parts.Push(current.name);
            current = current.parent;
        }
        return string.Join("/", parts);
    }
}
#endif
