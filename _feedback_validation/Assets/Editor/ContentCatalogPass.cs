using System;
using System.IO;
using System.Linq;
using Erudition;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ContentCatalogPass
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        var game = UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        if (game.gallery != null) throw new Exception("Migration already applied");
        var presenter = game.presentation;
        var catalog = ScriptableObject.CreateInstance<CollectionCatalog>();
        catalog.cards = presenter.collections.Select((card, i) => new CollectionDefinition {
            id = "legacy_" + i, title = card.title.text, picture = presenter.collectionPictures[i],
            group = i < 6 ? CollectionGroup.Authors : i < 9 ? CollectionGroup.Themes : i < 12 ? CollectionGroup.Books : CollectionGroup.Kinds,
            legacyGroup = i < 6 ? CollectionGroup.Authors : i < 9 ? CollectionGroup.Themes : i < 12 ? CollectionGroup.Books : CollectionGroup.Kinds,
            includeLegacy = true, legacyIndex = i < 6 ? i : i < 9 ? i - 6 : i < 12 ? i - 9 : i - 12,
            legacyTarget = card.target
        }).ToArray();
        catalog.cards = catalog.cards.Concat(new[] { new CollectionDefinition {
            title = "Маленькие открытия", picture = presenter.collectionPictures[6], group = CollectionGroup.Themes,
            phrases = new[] {
                new CollectionPhrase { text = "Любопытство превращает прогулку в открытие.", source = "Мысли Совушки" },
                new CollectionPhrase { text = "Новый вопрос помогает заметить знакомый мир.", source = "Мысли Совушки" },
                new CollectionPhrase { text = "Большое открытие начинается с маленького наблюдения.", source = "Мысли Совушки" }
            }
        } }).ToArray();
        catalog.EnsureIds();
        AssetDatabase.CreateAsset(catalog, "Assets/Data/CollectionCatalog.asset");
        var gallery = game.gameObject.AddComponent<CollectionGallery>();
        gallery.catalog = catalog;
        gallery.template = presenter.collections[0];
        gallery.template.name = "CollectionCard_Template";
        gallery.template.picture = gallery.template.transform.Find("PortraitViewport/Portrait_Pushkin").GetComponent<Image>();
        gallery.template.picture.name = "CollectionPicture";
        UnityEngine.Object.DestroyImmediate(gallery.template.transform.Find("PortraitViewport/IllustratedBackground").gameObject);
        gallery.template.progressText.text = "0/20";
        gallery.containers = game.collectionGroups.Select(g => g.GetComponentInChildren<ScrollRect>(true).content).ToArray();
        foreach (var card in presenter.collections.Skip(1)) UnityEngine.Object.DestroyImmediate(card.gameObject);
        foreach (var container in gallery.containers)
        {
            var grid = container.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = ((RectTransform)gallery.template.transform).sizeDelta;
            grid.spacing = new Vector2(22, 24);
            grid.padding = new RectOffset(6, 6, 6, 24);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;
            var fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        for (var i = 0; i < game.collectionGroups.Length; i++) game.collectionGroups[i].SetActive(i == 0);
        game.gallery = presenter.gallery = gallery;
        game.authorCards = game.themeCards = game.bookCards = game.kindCards = Array.Empty<CollectionCardView>();
        presenter.collections = Array.Empty<CollectionCardView>();
        presenter.collectionPictures = Array.Empty<Sprite>();
        CreateHintOffer(game);
        EditorUtility.SetDirty(game); EditorUtility.SetDirty(presenter); EditorUtility.SetDirty(gallery);
        EditorSceneManager.SaveScene(game.gameObject.scene);

        var gameplay = EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity");
        var board = UnityEngine.Object.FindFirstObjectByType<PuzzleBoard>(FindObjectsInactive.Include);
        var serialized = new SerializedObject(board);
        var hearts = serialized.FindProperty("hearts");
        for (var i = hearts.arraySize - 1; i >= PuzzleBoard.MaxHearts; i--)
            UnityEngine.Object.DestroyImmediate(((GameObject)hearts.GetArrayElementAtIndex(i).objectReferenceValue));
        hearts.arraySize = PuzzleBoard.MaxHearts;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(gameplay);
        AssetDatabase.SaveAssets();
        Debug.Log("CONTENT_SCENE_MIGRATION_PASS: 17 data cards, one template, automatic grids, hint popup, three hearts");
    }

    private static void CreateHintOffer(CryptogramGame game)
    {
        var popup = UnityEngine.Object.Instantiate(game.presentation.popup, game.presentation.popup.transform.parent);
        popup.name = "HintOfferPopup";
        popup.SetActive(true);
        var popupCanvas = popup.AddComponent<Canvas>();
        popupCanvas.overrideSorting = true;
        popupCanvas.sortingOrder = 100;
        popup.AddComponent<GraphicRaycaster>();
        popup.SetActive(false);
        game.hintOffer = popup;
        var panel = popup.transform.Find("PopupCard").GetComponent<RectTransform>();
        panel.sizeDelta = new Vector2(880, 920);
        UnityEngine.Object.DestroyImmediate(panel.Find("Icon").gameObject);
        var title = panel.Find("Text_Title").GetComponent<Text>();
        title.text = "Подсказки закончились";
        title.rectTransform.anchoredPosition = new Vector2(0, 335);
        var body = panel.Find("Text_Body").GetComponent<Text>();
        body.rectTransform.anchoredPosition = new Vector2(0, 140);
        body.rectTransform.sizeDelta = new Vector2(770, 230);
        body.fontSize = 34;
        body.text = "Купить 5 подсказок за 150 монет\nили посмотреть видео за 1 подсказку.";
        game.hintOfferMessage = body;
        var close = panel.Find("Button_Close").GetComponent<Button>();
        game.hintOfferBuy = UnityEngine.Object.Instantiate(close, panel);
        game.hintOfferAd = UnityEngine.Object.Instantiate(close, panel);
        game.hintOfferClose = close;
        ConfigureButton(game.hintOfferBuy, "Button_BuyHints", "5 подсказок · 150 монет", -55, UiActionKind.BuyHintOffer, game);
        ConfigureButton(game.hintOfferAd, "Button_RewardHint", "Смотреть видео · +1", -195, UiActionKind.RewardHint, game);
        ConfigureButton(close, "Button_CloseHintOffer", "Продолжить без подсказки", -335, UiActionKind.CloseHintOffer, game);
    }

    private static void ConfigureButton(Button button, string name, string label, float y, UiActionKind action, CryptogramGame game)
    {
        button.name = name;
        var rect = (RectTransform)button.transform;
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(730, 110);
        var text = button.GetComponentInChildren<Text>();
        text.text = label; text.fontSize = 32;
        text.rectTransform.sizeDelta = new Vector2(700, 104);
        button.GetComponent<UiAction>().Configure(game, action);
    }

    public static void FinishLayout()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        var game = UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        game.hintOffer.SetActive(true);
        foreach (var card in game.gallery.catalog.cards.Where(c => c.includeLegacy)) card.legacyGroup = card.group;
        EditorUtility.SetDirty(game.gallery.catalog); AssetDatabase.SaveAssets();
        var canvas = game.hintOffer.GetComponent<Canvas>();
        if (canvas == null) canvas = game.hintOffer.AddComponent<Canvas>();
        canvas.overrideSorting = true; canvas.sortingOrder = 100;
        var serializedCanvas = new SerializedObject(canvas);
        serializedCanvas.FindProperty("m_OverrideSorting").boolValue = true;
        serializedCanvas.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(canvas);
        if (game.hintOffer.GetComponent<GraphicRaycaster>() == null) game.hintOffer.AddComponent<GraphicRaycaster>();
        game.hintOffer.SetActive(false);
        EditorSceneManager.SaveScene(game.gameObject.scene);
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity");
        var board = UnityEngine.Object.FindFirstObjectByType<PuzzleBoard>(FindObjectsInactive.Include);
        var hearts = new SerializedObject(board).FindProperty("hearts");
        for (var i = 0; i < hearts.arraySize; i++)
        {
            var rect = (RectTransform)((GameObject)hearts.GetArrayElementAtIndex(i).objectReferenceValue).transform;
            rect.anchoredPosition = new Vector2((i - 1) * 86, rect.anchoredPosition.y);
        }
        EditorSceneManager.SaveScene(scene);
    }

    public static void FinishAndTest()
    {
        FinishLayout();
        ContentCatalogRegression.Run();
    }

    public static void Inspect()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        var game = UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        foreach (var card in game.presentation.collections.Take(1))
            foreach (var t in card.GetComponentsInChildren<RectTransform>(true))
                Debug.Log("CARD " + t.name + " pos=" + t.anchoredPosition + " size=" + t.sizeDelta + " sprite=" + t.GetComponent<Image>()?.sprite?.name);
        foreach (var group in game.collectionGroups)
            foreach (var t in group.GetComponentsInChildren<RectTransform>(true).Take(4))
                Debug.Log("GROUP " + t.name + " pos=" + t.anchoredPosition + " size=" + t.sizeDelta + " anchors=" + t.anchorMin + "/" + t.anchorMax);
        foreach (var t in game.presentation.popup.GetComponentsInChildren<RectTransform>(true))
            Debug.Log("POPUP " + t.name + " pos=" + t.anchoredPosition + " size=" + t.sizeDelta);
    }
}
