#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Erudition.EditorTools
{
    [InitializeOnLoad]
    public static class PrefabSceneSyncEditor
    {
        private const string MainScenePath = "Assets/Scenes/MainScene.unity";
        private const string GameplayScenePath = "Assets/Scenes/GameplayScene.unity";
        private const string AutoSessionKey = "Erudition.PrefabSceneSync.AutoChecked.v2";

        private sealed class Family
        {
            public string label;
            public string prefabPath;
            public string preferredName;
            public Func<Scene, IEnumerable<GameObject>> find;
            public bool required = true;

            public Family(string label, string prefabPath, string preferredName,
                Func<Scene, IEnumerable<GameObject>> find, bool required = true)
            {
                this.label = label;
                this.prefabPath = prefabPath;
                this.preferredName = preferredName;
                this.find = find;
                this.required = required;
            }
        }

        private sealed class SyncReport
        {
            public int converted;
            public int familiesChanged;
            public readonly List<string> lines = new List<string>();
        }

        private sealed class ValidationReport
        {
            public bool ok = true;
            public readonly List<string> lines = new List<string>();
        }

        private static readonly Family[] MainFamilies =
        {
            new Family("Нижняя навигация", "Assets/Prefabs/BottomNavigation.prefab", "BottomNavigation",
                scene => FindExact(scene, "BottomNavigation")),
            new Family("Счётчик перьев", "Assets/Prefabs/FeatherEnergyWidget.prefab", "FeatherEnergy",
                scene => FindExact(scene, "FeatherEnergy")),
            new Family("Карточка уровня", "Assets/Prefabs/CollectionCard.prefab", "CollectionCard_Template",
                scene => FindExact(scene, "CollectionCard_Template")),
            new Family("Карточки достижений", "Assets/Prefabs/AchievementCard.prefab", "AchievementCard_1",
                scene => FindPrefix(scene, "AchievementCard_")),
            new Family("Карточки магазина", "Assets/Prefabs/ShopItemCard.prefab", "ShopItem_Feathers_1",
                scene => FindPrefix(scene, "ShopItem_")),
            new Family("Блок наград", "Assets/Prefabs/RewardCard.prefab", "RewardsPanel",
                scene => FindExact(scene, "RewardsPanel")),
            new Family("Экран подробностей", "Assets/Prefabs/CollectionDetails.prefab", "Screen_CollectionDetails",
                scene => FindExact(scene, "Screen_CollectionDetails")),
            new Family("Экран настроек", "Assets/Prefabs/SettingsScreen.prefab", "Screen_Settings",
                scene => FindExact(scene, "Screen_Settings"))
        };

        private static readonly Family[] GameplayFamilies =
        {
            // First connect reusable children. TopBar is processed afterwards so its
            // prefab contains these objects as nested prefab instances instead of copies.
            new Family("Плашка эрудиции", "Assets/Prefabs/EruditionBadge.prefab", "EruditionBadge",
                scene => FindExact(scene, "EruditionBadge")),
            new Family("Кнопка подсказки", "Assets/Prefabs/HintButton.prefab", "Button_Hint",
                scene => FindExact(scene, "Button_Hint")),
            new Family("Сердца", "Assets/Prefabs/HeartIndicator.prefab", "Heart_01",
                scene => FindPrefix(scene, "Heart_")),
            new Family("Верхняя панель", "Assets/Prefabs/TopBar.prefab", "TopBar",
                scene => FindExact(scene, "TopBar")),
            new Family("Клавиши", "Assets/Prefabs/KeyboardKey.prefab", "Key_Й",
                scene => FindPrefix(scene, "Key_")),
            new Family("Клетки криптограммы", "Assets/Prefabs/LetterCell.prefab", "Cell_000",
                FindLetterCells)
        };

        static PrefabSceneSyncEditor()
        {
            EditorApplication.delayCall += AutoSyncIfSafe;
        }

        [MenuItem("Tools/Erudition/Префабы/Синхронизировать сцены и префабы")]
        public static void SyncFromScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Префабы", "Остановите Play Mode перед синхронизацией.", "ОК");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            try
            {
                var report = RunSync();
                var validation = ValidateInternal();
                if (!validation.ok)
                    throw new InvalidOperationException("После синхронизации остались несвязанные объекты.\n" +
                                                        string.Join("\n", validation.lines));

                EditorUtility.DisplayDialog("Префабы синхронизированы",
                    "Обновлено семейств: " + report.familiesChanged +
                    "\nПодключено объектов к prefab: " + report.converted +
                    "\n\n" + string.Join("\n", report.lines), "ОК");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Ошибка синхронизации prefab",
                    exception.Message + "\n\nСцены не нужно чинить вручную — исправьте ошибку и запустите инструмент ещё раз.",
                    "ОК");
            }
        }

        [MenuItem("Tools/Erudition/Префабы/Проверить связи")]
        public static void ValidateLinks()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Префабы", "Остановите Play Mode перед проверкой.", "ОК");
                return;
            }

            var report = ValidateInternal();
            var title = report.ok ? "Связи prefab в порядке" : "Найдены проблемы prefab";
            EditorUtility.DisplayDialog(title, string.Join("\n", report.lines), "ОК");
            if (!report.ok)
                Debug.LogError("[PrefabSceneSync] " + string.Join("\n", report.lines));
        }

        private static void AutoSyncIfSafe()
        {
            if (SessionState.GetBool(AutoSessionKey, false))
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoSyncIfSafe;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode || HasDirtyLoadedScene())
                return;

            try
            {
                var before = ValidateInternal();
                if (before.ok)
                {
                    SessionState.SetBool(AutoSessionKey, true);
                    return;
                }

                var report = RunSync();
                var after = ValidateInternal();
                if (!after.ok)
                {
                    Debug.LogError("[PrefabSceneSync] Автосинхронизация не завершена:\n" +
                                   string.Join("\n", after.lines));
                    return;
                }

                SessionState.SetBool(AutoSessionKey, true);
                if (report.converted > 0)
                    Debug.Log("[PrefabSceneSync] Сцены переведены на настоящие prefab instances. " +
                              "Подключено объектов: " + report.converted + ".");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static SyncReport RunSync()
        {
            var report = new SyncReport();
            SyncScene(MainScenePath, MainFamilies, report);
            SyncScene(GameplayScenePath, GameplayFamilies, report);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return report;
        }

        private static void SyncScene(string scenePath, Family[] families, SyncReport report)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                throw new FileNotFoundException("Не найдена сцена", scenePath);

            var scene = SceneManager.GetSceneByPath(scenePath);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            var changed = false;
            try
            {
                foreach (var family in families)
                    changed |= SyncFamily(scene, family, report);

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException("Unity не смог сохранить " + scenePath);
                }
            }
            finally
            {
                if (openedHere && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static bool SyncFamily(Scene scene, Family family, SyncReport report)
        {
            var objects = family.find(scene)
                .Where(go => go != null)
                .Distinct()
                .OrderBy(HierarchyPath, StringComparer.Ordinal)
                .ToArray();

            if (objects.Length == 0)
            {
                if (family.required)
                    throw new InvalidOperationException(
                        "В " + scene.path + " не найден объект для prefab «" + family.label + "».");
                return false;
            }

            if (objects.All(go => ConnectedTo(go, family.prefabPath)))
            {
                report.lines.Add(family.label + ": уже связано (" + objects.Length + ")");
                return false;
            }

            var source = objects.FirstOrDefault(go =>
                             !ConnectedTo(go, family.prefabPath) &&
                             string.Equals(go.name, family.preferredName, StringComparison.Ordinal))
                         ?? objects.FirstOrDefault(go => !ConnectedTo(go, family.prefabPath))
                         ?? objects[0];

            if (PrefabUtility.IsPartOfPrefabInstance(source))
            {
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(source);
                if (root != source)
                    throw new InvalidOperationException(
                        "Нельзя использовать вложенный prefab как источник: " + HierarchyPath(source));
                PrefabUtility.UnpackPrefabInstance(source, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(source, family.prefabPath, out var saved);
            if (!saved || prefab == null)
                throw new InvalidOperationException("Не удалось обновить " + family.prefabPath);

            NormalizePrefabRootName(family.prefabPath);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(family.prefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Не удалось загрузить " + family.prefabPath);

            var toConvert = objects.Where(go => !ConnectedTo(go, family.prefabPath)).ToArray();
            foreach (var go in toConvert)
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(go))
                    continue;

                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                if (root != go)
                    throw new InvalidOperationException(
                        "Ожидался корневой объект prefab, но найден вложенный: " + HierarchyPath(go));

                PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }

            var settings = new ConvertToPrefabInstanceSettings
            {
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                componentsNotMatchedBecomesOverride = true,
                gameObjectsNotMatchedBecomesOverride = true,
                recordPropertyOverridesOfMatches = true,
                changeRootNameToAssetName = false,
                logInfo = false
            };

            PrefabUtility.ConvertToPrefabInstances(toConvert, prefab, settings,
                InteractionMode.AutomatedAction);

            foreach (var go in objects)
            {
                if (!ConnectedTo(go, family.prefabPath))
                    throw new InvalidOperationException(
                        "После конвертации объект не связан с " + family.prefabPath + ": " +
                        HierarchyPath(go));
            }

            report.converted += toConvert.Length;
            report.familiesChanged++;
            report.lines.Add(family.label + ": " + toConvert.Length + " → " +
                             family.prefabPath);
            return true;
        }

        private static void NormalizePrefabRootName(string prefabPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return;

            try
            {
                var desiredName = Path.GetFileNameWithoutExtension(prefabPath);
                if (!string.Equals(root.name, desiredName, StringComparison.Ordinal))
                {
                    root.name = desiredName;
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static ValidationReport ValidateInternal()
        {
            var report = new ValidationReport();
            ValidateScene(MainScenePath, MainFamilies, report);
            ValidateScene(GameplayScenePath, GameplayFamilies, report);
            return report;
        }

        private static void ValidateScene(string scenePath, Family[] families, ValidationReport report)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                report.ok = false;
                report.lines.Add("Нет сцены: " + scenePath);
                return;
            }

            var scene = SceneManager.GetSceneByPath(scenePath);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            try
            {
                foreach (var family in families)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(family.prefabPath);
                    if (prefab == null)
                    {
                        report.ok = false;
                        report.lines.Add("Нет prefab: " + family.prefabPath);
                        continue;
                    }

                    var objects = family.find(scene).Where(go => go != null).Distinct().ToArray();
                    if (objects.Length == 0)
                    {
                        if (family.required)
                        {
                            report.ok = false;
                            report.lines.Add(family.label + ": объект на сцене не найден");
                        }
                        continue;
                    }

                    var bad = objects.Where(go => !ConnectedTo(go, family.prefabPath)).ToArray();
                    if (bad.Length == 0)
                    {
                        report.lines.Add("✓ " + family.label + ": " + objects.Length);
                    }
                    else
                    {
                        report.ok = false;
                        report.lines.Add("✗ " + family.label + ": " + bad.Length +
                                         " из " + objects.Length + " не связаны");
                        foreach (var go in bad.Take(4))
                            report.lines.Add("  " + HierarchyPath(go));
                    }
                }
            }
            finally
            {
                if (openedHere && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static bool ConnectedTo(GameObject go, string prefabPath)
        {
            if (go == null || !PrefabUtility.IsPartOfPrefabInstance(go))
                return false;

            // Nested prefab instances are valid and expected here. For example,
            // BottomNavigation can live inside SettingsScreen and Heart_01 inside TopBar.
            // GetOutermostPrefabInstanceRoot would return the parent prefab and falsely
            // report these objects as detached, so validate against the nearest prefab asset.
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            return string.Equals(path, prefabPath, StringComparison.Ordinal);
        }

        private static IEnumerable<GameObject> FindExact(Scene scene, string name)
        {
            return AllObjects(scene).Where(go => string.Equals(go.name, name, StringComparison.Ordinal));
        }

        private static IEnumerable<GameObject> FindPrefix(Scene scene, string prefix)
        {
            return AllObjects(scene).Where(go => go.name.StartsWith(prefix, StringComparison.Ordinal));
        }

        private static IEnumerable<GameObject> FindLetterCells(Scene scene)
        {
            return AllObjects(scene).Where(go => go.GetComponent<LetterCellView>() != null);
        }

        private static IEnumerable<GameObject> AllObjects(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    yield return transform.gameObject;
            }
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

        private static bool HasDirtyLoadedScene()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                    return true;
            }
            return false;
        }
    }
}
#endif
