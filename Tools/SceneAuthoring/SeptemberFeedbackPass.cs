using System;
using System.Linq;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Run only in the validation copy. Saves focused edits to existing scene objects.
public static class SeptemberFeedbackPass
{
    public static void Run()
    {
        try
        {
            var main = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            var game = UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
            foreach (var action in game.GetComponentsInChildren<UiAction>(true)) RemoveTurbo(action);
            foreach (var root in main.GetRootGameObjects())
                foreach (var action in root.GetComponentsInChildren<UiAction>(true)) RemoveTurbo(action);
            game.screens[3] = null; // Keep all other serialized screen indices stable.
            foreach (var card in game.authorCards.Concat(game.themeCards).Concat(game.bookCards)) ClipPortrait(card);
            game.defeatQuote.text = ""; game.defeatQuote.gameObject.SetActive(false);
            game.defeatSource.text = ""; game.defeatSource.gameObject.SetActive(false);
            game.defeatMessage.text = "Сердечки закончились.\nПопробуй ещё раз — у тебя получится!";
            var panel = (RectTransform)game.defeatMessage.transform.parent;
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, 260);
            game.defeatMessage.rectTransform.anchoredPosition = Vector2.zero;
            game.defeatMessage.rectTransform.sizeDelta = new Vector2(840, 190);
            game.defeatMessage.alignment = TextAnchor.MiddleCenter;
            EditorSceneManager.SaveScene(main);

            var gameplay = EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity");
            var link = UnityEngine.Object.FindFirstObjectByType<GameplaySceneLink>();
            var retired = link.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Screen_GameplayTurbo");
            if (retired != null) UnityEngine.Object.DestroyImmediate(retired.gameObject);
            link.actions = link.GetComponentsInChildren<UiAction>(true);
            EditorSceneManager.SaveScene(gameplay);

            foreach (var path in new[] { "Assets/Prefabs/AuthorCard.prefab", "Assets/Prefabs/CollectionCard.prefab" })
            {
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var card in prefab.GetComponentsInChildren<CollectionCardView>(true)) ClipPortrait(card);
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("SEPTEMBER_SCENES_SAVED: turbo removed, portrait masks fixed, defeat answer hidden");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    static void RemoveTurbo(UiAction action)
    {
        if (action == null) return;
        var serialized = new SerializedObject(action);
        if (serialized.FindProperty("action").intValue == (int)UiActionKind.Turbo)
            UnityEngine.Object.DestroyImmediate(action.gameObject);
    }

    static void ClipPortrait(CollectionCardView card)
    {
        var viewport = card.transform.Find("PortraitViewport") as RectTransform;
        if (viewport == null) return;
        // The outer mask follows the rounded card; the rectangular inner clip
        // also contains the panoramic backdrop while the list is scrolled.
        var outer = card.GetComponent<Mask>() ?? card.gameObject.AddComponent<Mask>();
        outer.showMaskGraphic = true;
        if (viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();
        var shape = viewport.GetComponent<Mask>();
        if (shape != null) shape.showMaskGraphic = false;
        var frame = (RectTransform)card.transform;
        var width = frame.rect.width - 16;
        viewport.sizeDelta = new Vector2(width, viewport.sizeDelta.y - 8);
        viewport.anchoredPosition += new Vector2(0, -4);
        foreach (var image in viewport.GetComponentsInChildren<Image>(true))
        {
            image.maskable = true;
            image.raycastTarget = false;
            if (image.transform == viewport) continue;
            var rect = image.rectTransform;
            rect.anchoredPosition = Vector2.zero;
            if (image.name == "IllustratedBackground")
            {
                // Cover the viewport with the source's true aspect ratio;
                // the masks crop both edges instead of leaking into neighbours.
                var ratio = image.sprite.rect.width / image.sprite.rect.height;
                var height = Mathf.Max(viewport.rect.height, width / ratio);
                rect.sizeDelta = new Vector2(height * ratio, height);
            }
            else rect.sizeDelta = viewport.sizeDelta;
        }
    }
}
