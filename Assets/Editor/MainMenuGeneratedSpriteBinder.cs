#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Erudition.EditorTools
{
    [InitializeOnLoad]
    public static class MainMenuGeneratedSpriteBinder
    {
        private const string MainScenePath = "Assets/Scenes/MainScene.unity";
        private const string ArtRoot = "Assets/Art/Generated/";

        private static bool _busy;

        static MainMenuGeneratedSpriteBinder()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += RepairIfMainSceneIsOpen;
        }

        [MenuItem("Erudition/Repair Main Menu Sprites")]
        public static void RepairFromMenu()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != MainScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;

                scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            }

            Repair(scene, true);
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path != MainScenePath)
                return;

            EditorApplication.delayCall += () =>
            {
                if (scene.IsValid() && scene.isLoaded)
                    Repair(scene, true);
            };
        }

        private static void RepairIfMainSceneIsOpen()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded && scene.path == MainScenePath)
                Repair(scene, false);
        }

        private static void Repair(Scene scene, bool saveScene)
        {
            if (_busy || !scene.IsValid() || !scene.isLoaded)
                return;

            _busy = true;
            try
            {
                EnsureSpriteImporter(ArtRoot + "main_primary_blue.png", new Vector4(70, 50, 70, 50));
                EnsureSpriteImporter(ArtRoot + "main_secondary_cream.png", new Vector4(65, 50, 65, 50));
                EnsureSpriteImporter(ArtRoot + "main_nav_panel.png", new Vector4(60, 35, 60, 35));
                EnsureSpriteImporter(ArtRoot + "main_round_blue.png", Vector4.zero);
                EnsureSpriteImporter(ArtRoot + "icon_open_book.png", Vector4.zero);
                EnsureSpriteImporter(ArtRoot + "icon_closed_book.png", Vector4.zero);
                EnsureSpriteImporter(ArtRoot + "icon_gear.png", Vector4.zero);
                EnsureSpriteImporter(ArtRoot + "icon_home.png", Vector4.zero);
                EnsureSpriteImporter(ArtRoot + "icon_stats.png", Vector4.zero);
                EnsureSpriteImporter(ArtRoot + "icon_trophy.png", Vector4.zero);
                EnsureSpriteImporter(ArtRoot + "icon_shop.png", Vector4.zero);

                var main = FindInScene(scene, "Screen_MainMenu");
                if (main == null)
                {
                    Debug.LogWarning("[MainMenuGeneratedSpriteBinder] Screen_MainMenu not found.");
                    return;
                }

                bool changed = false;

                var classic = FindChild(main.transform, "Button_Classic");
                var levels = FindChild(main.transform, "Button_Levels");
                var nav = FindChild(main.transform, "BottomNavigation");
                var settings = FindChild(main.transform, "Button_Settings");

                changed |= BindImage(classic, ArtRoot + "main_primary_blue.png", Image.Type.Sliced, false);
                changed |= BindImage(levels, ArtRoot + "main_secondary_cream.png", Image.Type.Sliced, false);
                changed |= BindImage(nav, ArtRoot + "main_nav_panel.png", Image.Type.Sliced, false);

                changed |= DisableChild(classic, "Surface_Fill");
                changed |= DisableChild(levels, "Surface_Fill");

                changed |= BindChildImage(classic, "Icon_Book", ArtRoot + "icon_open_book.png", Image.Type.Simple, true);
                changed |= BindChildImage(classic, "ArrowCircle", ArtRoot + "main_round_blue.png", Image.Type.Simple, true);

                changed |= BindChildImage(levels, "Icon_Book", ArtRoot + "icon_closed_book.png", Image.Type.Simple, true);
                changed |= DisableChild(levels, "ArrowCircle");

                changed |= BindChildImage(settings, "Icon_Gear", ArtRoot + "icon_gear.png", Image.Type.Simple, true);

                if (nav != null)
                {
                    changed |= BindNavIcon(nav.transform, "Button_Home", "Icon_Home", "icon_home.png");
                    changed |= BindNavIcon(nav.transform, "Button_Statistics", "Icon_Statistics", "icon_stats.png");
                    changed |= BindNavIcon(nav.transform, "Button_Achievements", "Icon_Trophy", "icon_trophy.png");
                    changed |= BindNavIcon(nav.transform, "Button_Shop", "Icon_Shop", "icon_shop.png");

                    var levelsNav = FindChild(nav.transform, "Button_Levels");
                    if (levelsNav != null && levelsNav.activeSelf)
                    {
                        Undo.RecordObject(levelsNav, "Hide obsolete Levels nav button");
                        levelsNav.SetActive(false);
                        EditorUtility.SetDirty(levelsNav);
                        changed = true;
                    }
                }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (saveScene)
                        EditorSceneManager.SaveScene(scene);
                    Debug.Log("[MainMenuGeneratedSpriteBinder] Main menu sprites repaired and bound.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _busy = false;
            }
        }

        private static bool BindNavIcon(Transform nav, string buttonName, string iconName, string fileName)
        {
            var button = FindChild(nav, buttonName);
            return BindChildImage(button, iconName, ArtRoot + fileName, Image.Type.Simple, true);
        }

        private static bool BindChildImage(GameObject parent, string childName, string assetPath, Image.Type type, bool preserveAspect)
        {
            if (parent == null)
                return false;

            var child = FindChild(parent.transform, childName);
            return BindImage(child, assetPath, type, preserveAspect);
        }

        private static bool BindImage(GameObject target, string assetPath, Image.Type type, bool preserveAspect)
        {
            if (target == null)
                return false;

            var image = target.GetComponent<Image>();
            if (image == null)
                return false;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Debug.LogError("[MainMenuGeneratedSpriteBinder] Sprite failed to load: " + assetPath);
                return false;
            }

            if (image.sprite == sprite && image.type == type && image.preserveAspect == preserveAspect && image.color == Color.white)
                return false;

            Undo.RecordObject(image, "Bind generated main menu sprite");
            image.sprite = sprite;
            image.type = type;
            image.preserveAspect = preserveAspect;
            image.color = Color.white;
            EditorUtility.SetDirty(image);
            return true;
        }

        private static bool DisableChild(GameObject parent, string childName)
        {
            if (parent == null)
                return false;

            var child = FindChild(parent.transform, childName);
            if (child == null || !child.activeSelf)
                return false;

            Undo.RecordObject(child, "Disable obsolete UI child");
            child.SetActive(false);
            EditorUtility.SetDirty(child);
            return true;
        }

        private static void EnsureSpriteImporter(string assetPath, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[MainMenuGeneratedSpriteBinder] Texture not found: " + assetPath);
                return;
            }

            bool dirty = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }

            if (!Approximately(importer.spriteBorder, border))
            {
                importer.spriteBorder = border;
                dirty = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                dirty = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }

            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                dirty = true;
            }

            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                dirty = true;
            }

            if (dirty)
                importer.SaveAndReimport();
        }

        private static bool Approximately(Vector4 a, Vector4 b)
        {
            return Mathf.Approximately(a.x, b.x)
                && Mathf.Approximately(a.y, b.y)
                && Mathf.Approximately(a.z, b.z)
                && Mathf.Approximately(a.w, b.w);
        }

        private static GameObject FindInScene(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindChild(root.transform, objectName);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static GameObject FindChild(Transform root, string objectName)
        {
            if (root == null)
                return null;

            if (root.name == objectName)
                return root.gameObject;

            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (t.name == objectName)
                    return t.gameObject;
            }
            return null;
        }
    }
}
#endif
