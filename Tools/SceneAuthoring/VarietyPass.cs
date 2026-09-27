using System;
using System.Linq;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Run in the validation copy, then copy the saved scenes back to the project.
public static class VarietyPass
{
    public static void Run()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            var game = UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
            var ui = game.presentation;
            var group = UnityEngine.Object.Instantiate(game.collectionGroups[0], game.collectionGroups[0].transform.parent);
            group.name = "Group_TextKinds";
            var cards = group.GetComponentsInChildren<CollectionCardView>(true);
            foreach (var card in cards.Skip(4)) UnityEngine.Object.DestroyImmediate(card.gameObject);
            foreach (var t in group.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("ThemePreview_")).ToArray())
                UnityEngine.Object.DestroyImmediate(t.gameObject);
            cards = cards.Take(4).ToArray();
            var scenes = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/SpriteSheets/reference_collection_scenes_v2.png").OfType<Sprite>().OrderBy(s => s.name).ToArray();
            var decorations = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/SpriteSheets/reference_decor_v2.png").OfType<Sprite>().ToArray();
            Debug.Log("DECOR_SPRITES: " + string.Join(",", decorations.Select(s => s.name)));
            var pictures = new Sprite[4];
            for (var i = 0; i < 4; i++)
            {
                var card = cards[i]; card.name = "Card_" + (PuzzleKind)i;
                card.title.text = PuzzleGenerator.KindName((PuzzleKind)i); card.target = 20; card.SetProgress(0);
                card.title.fontSize = 32; card.title.resizeTextForBestFit = true; card.title.resizeTextMinSize = 26; card.title.resizeTextMaxSize = 32;
                var viewport = card.transform.Find("PortraitViewport");
                foreach (var image in viewport.GetComponentsInChildren<Image>(true))
                {
                    if (image.transform == viewport) continue;
                    if (image.name == "IllustratedBackground")
                    { image.sprite = scenes[(i + 2) % scenes.Length]; pictures[i] = image.sprite; }
                    else UnityEngine.Object.DestroyImmediate(image.gameObject);
                }
                foreach (var action in card.GetComponentsInChildren<UiAction>(true)) action.Configure(game, UiActionKind.CollectionDetails, 12 + i);
            }
            var reader = group.GetComponentInChildren<ScrollRect>(true);
            // Four cards use the first two authored rows; remove the empty third row.
            reader.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 920);
            game.kindCards = cards;
            game.collectionGroups = game.collectionGroups.Concat(new[] { group }).ToArray();
            var tab = UnityEngine.Object.Instantiate(game.collectionTabs[0], game.collectionTabs[0].transform.parent);
            tab.name = "Button_TextKinds"; tab.GetComponentInChildren<Text>().text = "Типы";
            tab.GetComponent<UiAction>().Configure(game, UiActionKind.CollectionTab, 3);
            game.collectionTabs = game.collectionTabs.Concat(new[] { tab }).ToArray();
            for (var i = 0; i < game.collectionTabs.Length; i++)
            {
                var rect = (RectTransform)game.collectionTabs[i].transform;
                rect.anchoredPosition = new Vector2((i - 1.5f) * 243, 0); rect.sizeDelta = new Vector2(240, 98);
                var text = game.collectionTabs[i].GetComponentInChildren<Text>(); text.fontSize = 34;
            }
            ui.collections = ui.collections.Concat(cards).ToArray();
            ui.collectionPictures = ui.collectionPictures.Concat(pictures).ToArray();
            group.SetActive(false);
            var panel = new GameObject("RewardExplanation", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(game.screens[4].transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchoredPosition = new Vector2(0, -663); panelRect.sizeDelta = new Vector2(900, 114);
            var surface = panel.GetComponent<Image>(); surface.sprite = game.authorCards[0].GetComponent<Image>().sprite;
            surface.type = Image.Type.Sliced; surface.color = new Color(1, .975f, .90f, .97f); surface.raycastTarget = false;
            var label = UnityEngine.Object.Instantiate(game.victorySource, panel.transform);
            label.name = "Text_RewardExplanation"; label.text = "Лёгкий · Основа 5 · без подсказок +2 · точность +2 · темп +1";
            label.fontSize = 27; label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 23; label.resizeTextMaxSize = 27;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, .5f);
            label.rectTransform.anchoredPosition = Vector2.zero; label.rectTransform.sizeDelta = new Vector2(860, 104);
            game.victoryRewardDetails = label;
            game.victorySource.resizeTextForBestFit = true; game.victorySource.resizeTextMinSize = 24;
            game.victorySource.resizeTextMaxSize = game.victorySource.fontSize;
            game.victorySource.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100);
            EditorSceneManager.SaveScene(scene);
            var gameplay = EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity");
            var board = UnityEngine.Object.FindFirstObjectByType<PuzzleBoard>(FindObjectsInactive.Include);
            var instruction = new SerializedObject(board).FindProperty("instructionText").objectReferenceValue as Text;
            instruction.fontSize = 29; instruction.resizeTextForBestFit = true; instruction.resizeTextMinSize = 24; instruction.resizeTextMaxSize = 29;
            instruction.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 88);
            EditorSceneManager.SaveScene(gameplay);
            AssetDatabase.SaveAssets();
            Debug.Log("VARIETY_SCENES_SAVED"); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
