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
                ValidateAchievements(objects, report);
                ValidateSettings(objects, report);

                var game = objects.Select(go => go.GetComponent<CryptogramGame>())
                    .FirstOrDefault(component => component != null);
                if (game == null)
                {
                    report.Error("MainScene: не найден CryptogramGame.");
                }
                else
                {
                    ValidateHubScreen(game.mainMenuScreen, "mainMenuScreen", report);
                    ValidateHubScreen(game.collectionDetailsScreen, "collectionDetailsScreen", report);
                    ValidateHubScreen(game.victoryScreen, "victoryScreen", report);
                    ValidateHubScreen(game.noFeathersScreen, "noFeathersScreen", report);
                    ValidateHubScreen(game.statisticsScreen, "statisticsScreen", report);
                    ValidateHubScreen(game.levelsScreen, "levelsScreen", report);
                    ValidateHubScreen(game.achievementsScreen, "achievementsScreen", report);
                    ValidateHubScreen(game.shopScreen, "shopScreen", report);
                    ValidateHubScreen(game.settingsScreen, "settingsScreen", report);
                    ValidateHubScreen(game.debugScreen, "debugScreen", report);
                    ValidateHubScreen(game.defeatScreen, "defeatScreen", report);
                    if (game.classicBoard != null)
                        report.Warn("MainScene: classicBoard должен приходить из GameplayScene, а не быть сериализован в Hub.");
                }

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

    private static void ValidateHubScreen(GameObject screen, string fieldName, Report report)
    {
        if (screen == null)
        {
            report.Error("CryptogramGame: не назначен " + fieldName + ".");
            return;
        }
        if (!screen.name.StartsWith("Screen_", StringComparison.Ordinal))
            report.Warn("CryptogramGame." + fieldName + ": объект лучше называть Screen_* (" + screen.name + ").");
    }

    private static void ValidateSettings(GameObject[] objects, Report report)
    {
        var rows = objects.Select(go => go.GetComponent<SettingRowView>())
            .Where(row => row != null)
            .Distinct()
            .ToArray();

        if (rows.Length != 4)
            report.Warn("Настройки: ожидалось 4 SettingRowView, сейчас " + rows.Length + ".");

        var kinds = new HashSet<SettingKind>();
        foreach (var row in rows)
        {
            if (!kinds.Add(row.kind)) report.Error(row.name + ": повторяется SettingKind " + row.kind + ".");
            if (row.stateText == null) report.Error(row.name + ": не назначен stateText.");
            if (row.track == null) report.Error(row.name + ": не назначен track.");
            if (row.thumb == null) report.Error(row.name + ": не назначен thumb.");
            if (row.onPosition == null || row.offPosition == null)
                report.Error(row.name + ": не назначены позиции переключателя.");
        }

        if (rows.Length > 0)
            report.Ok("Настройки: " + rows.Length + " строк собраны в SettingRowView.");
    }

    private static void ValidateAchievements(GameObject[] objects, Report report)
    {
        var cards = objects.Select(go => go.GetComponent<AchievementCardView>())
            .Where(card => card != null)
            .Distinct()
            .ToArray();

        if (cards.Length == 0)
        {
            report.Error("MainScene: не найдены AchievementCardView.");
            return;
        }
        if (cards.Length > 31)
            report.Error("Достижений больше 31 — claimedAchievementMask больше не помещает все claimBit.");

        var bits = new HashSet<int>();
        foreach (var card in cards)
        {
            if (card.titleText == null) report.Error(card.name + ": не назначен titleText.");
            if (card.descriptionText == null) report.Error(card.name + ": не назначен descriptionText.");
            if (card.icon == null) report.Error(card.name + ": не назначена icon.");
            if (card.target <= 0) report.Warn(card.name + ": target должен быть больше 0.");
            if (card.rewardAmount <= 0) report.Warn(card.name + ": rewardAmount должен быть больше 0.");
            if (card.claimBit < 0 || card.claimBit >= 31 || !bits.Add(card.claimBit))
                report.Error(card.name + ": claimBit должен быть уникальным числом 0–30.");
        }

        report.Ok("Достижения: " + cards.Length + " карточек, claimBit проверены.");
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
            report.Ok("CollectionCatalog: " + (catalog.cards ?? Array.Empty<CollectionDefinition>()).Length
                + " карточек, кэш готов.");
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
