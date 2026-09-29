using System;
using System.Linq;
using Erudition;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ReferencePolish
{
    private const string Surfaces = "Assets/Art/SpriteSheets/ui_surfaces_reference.png";
    private const string Icons = "Assets/Art/SpriteSheets/ui_icons_reference.png";
    private static Font font;

    public static void Apply(CryptogramGame game)
    {
        Import();
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Inter-Regular.ttf");
        UnityEngine.Object.FindFirstObjectByType<Camera>().rect = new Rect(0, 0, 1, 1);
        foreach (var screen in game.screens)
        {
            foreach (var image in screen.GetComponentsInChildren<Image>(true))
            {
                var name = image.name;
                if (name == "PuzzlePanel") Skin(image, "Parchment");
                else if (name.Contains("Card") || name.StartsWith("Section_") || name == "RewardsPanel"
                    || name == "BottomNavigation" || name == "BalanceBar") Skin(image, "CreamCard");
                else if (name.StartsWith("Key_")) { Skin(image, "KeyboardKey"); image.pixelsPerUnitMultiplier = 7; }
            }
            foreach (var button in screen.GetComponentsInChildren<Button>(true))
            {
                if (button.name.StartsWith("Key_") || button.name.StartsWith("LetterCell_")
                    || button.name.StartsWith("Tab_") || button.name.StartsWith("Toggle_")) continue;
                var image = button.GetComponent<Image>();
                if (image == null) continue;
                if (button.transform.parent.name == "BottomNavigation") continue;
                var sprite = button.name.Contains("Turbo") || button.name == "Button_RewardedAd" ? "PurpleButton"
                    : button.name.Contains("Buy") || button.name.Contains("Shop") ? "GoldButton"
                    : button.name.Contains("Classic") || button.name == "Button_Continue" || button.name == "Button_Retry" ? "GreenButton"
                    : button.GetComponent<RectTransform>().sizeDelta.x < 160 ? "KeyboardKey" : "BlueButton";
                Skin(image, sprite);
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.pressedColor = new Color(.82f, .82f, .9f);
                colors.disabledColor = new Color(.6f, .6f, .65f, .8f);
                button.colors = colors;
            }
            PolishNavigation(screen.transform);
            var title = screen.transform.Cast<Transform>().FirstOrDefault(child => child.name == "Text_Title");
            if (title != null)
            {
                var rt = title.GetComponent<RectTransform>();
                var banner = AddImage(screen.transform, "TitleParchment", Sprite("TitleRibbon"), rt.anchoredPosition,
                    new Vector2(Mathf.Min(900, rt.sizeDelta.x + 100), 190));
                banner.preserveAspect = false;
                banner.transform.SetSiblingIndex(title.GetSiblingIndex());
                title.GetComponent<Text>().color = new Color(.16f, .08f, .23f);
            }
        }
        PolishMain(game.screens[0].transform);
        PolishBoard(game.screens[2].transform, false);
        PolishBoard(game.screens[3].transform, true);
        ReplaceGlyph(Find(game.screens[1].transform, "Icon_BooksAndFeather"), "Book");
        ReplaceGlyph(Find(game.screens[1].transform, "Icon_Lightning"), "Lightning");
        foreach (var card in game.achievementCards) ReplaceGlyph(Find(card.transform, "Icon"), "Trophy");
        foreach (var card in game.authorCards)
        {
            var index = Array.IndexOf(game.authorCards, card);
            Rect(card.transform, new Vector2(index % 2 == 0 ? -240 : 240, 560 - index / 2 * 465), new Vector2(450, 420));
            var portrait = card.GetComponentsInChildren<Image>(true).First(image => image.name.StartsWith("Portrait_"));
            var nature = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/SpriteSheets/collections_missing.png")
                .OfType<Sprite>().Single(sprite => sprite.name == "Theme_Nature");
            var scenery = AddImage(card.transform, "PortraitScenery", nature, new Vector2(0, 88), new Vector2(410, 230));
            scenery.preserveAspect = false;
            scenery.color = new Color(1, 1, 1, .82f);
            scenery.transform.SetSiblingIndex(portrait.transform.GetSiblingIndex());
            Rect(portrait.transform, new Vector2(0, 90), new Vector2(414, 220));
            Rect(card.title.transform, new Vector2(0, -70), new Vector2(410, 88));
            card.title.fontSize = 31;
            Rect(card.progressText.transform, new Vector2(-125, -140), new Vector2(130, 50));
            foreach (var rect in card.GetComponentsInChildren<RectTransform>(true).Where(rect => rect.name.StartsWith("Progress_")))
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -145);
        }
        var shop = game.screens[9].transform;
        ReplaceGlyph(Find(Find(shop, "ShopItem_Feathers_1"), "Icon"), "Feather");
        ReplaceGlyph(Find(Find(shop, "ShopItem_Feathers_2"), "Icon"), "Feather");
        ReplaceGlyph(Find(Find(shop, "Section_Hints"), "Icon"), "Bulb");
        foreach (var premium in new[] { "ShopItem_Special_1", "ShopItem_Special_2" })
            Find(Find(Find(shop, premium), "Button_Buy"), "Text_Label").GetComponent<Text>().text = "Скоро";
        PolishSupportScreens(game);
        var rootCanvas = game.screens[0].GetComponentInParent<Canvas>();
        foreach (var label in rootCanvas.GetComponentsInChildren<Text>(true)) label.supportRichText = true;
        foreach (var back in rootCanvas.GetComponentsInChildren<RectTransform>(true).Where(rect => rect.name == "Progress_Back"))
        {
            var fill = back.parent.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(rect => rect.name == "Progress_Fill");
            if (fill == null) continue;
            fill.anchorMin = back.anchorMin;
            fill.anchorMax = back.anchorMax;
            fill.pivot = new Vector2(0, .5f);
            fill.anchoredPosition = back.anchoredPosition - new Vector2(back.sizeDelta.x * back.pivot.x, 0);
        }
    }

    private static void PolishSupportScreens(CryptogramGame game)
    {
        var victoryOwl = Find(game.screens[4].transform, "OwlMascot_Happy").GetComponent<Image>();
        victoryOwl.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/owl_victory_v2.png");
        Rect(victoryOwl.transform, new Vector2(0, 570), new Vector2(650, 590));
        var sadSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/owl_sad_v2.png");
        var sadOwl = Find(game.screens[5].transform, "OwlMascot_Sad").GetComponent<Image>();
        sadOwl.sprite = sadSprite;
        Rect(sadOwl.transform, new Vector2(0, 390), new Vector2(690, 620));
        Find(game.screens[12].transform, "OwlMascot_Defeat").GetComponent<Image>().sprite = sadSprite;
        var shopOwl = Find(game.screens[9].transform, "OwlMascot_Shop").GetComponent<Image>();
        shopOwl.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/owl_shop_v2.png");
        Rect(shopOwl.transform, new Vector2(0, 615), new Vector2(410, 350));
        foreach (var card in game.achievementCards)
        {
            var root = card.transform;
            Rect(Find(root, "Text_Title"), new Vector2(-5, 62), new Vector2(450, 68));
            Find(root, "Text_Title").GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            Rect(Find(root, "Text_Description"), new Vector2(40, -3), new Vector2(540, 58));
            Find(root, "Text_Description").GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            Rect(Find(root, "Progress_Back"), new Vector2(65, -75), new Vector2(600, 24));
            card.fullProgressWidth = 600;
        }
        var settings = Find(game.screens[10].transform, "SettingsCard");
        foreach (var label in settings.GetComponentsInChildren<Text>(true).Where(label => label.transform.parent == settings && label.name.StartsWith("Text_")))
        {
            var y = label.rectTransform.anchoredPosition.y;
            Rect(label.transform, new Vector2(-120, y), new Vector2(500, 75));
            label.alignment = TextAnchor.MiddleLeft;
        }
        var shop = game.screens[9].transform;
        var hints = Find(shop, "Section_Hints");
        Rect(Find(hints, "Text_Name"), new Vector2(-20, 45), new Vector2(470, 65));
        Rect(Find(hints, "Text_Detail"), new Vector2(-70, -25), new Vector2(370, 55));
        foreach (var name in new[] { "Text_Name", "Text_Detail" }) Find(hints, name).GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        Rect(Find(hints, "Button_Buy"), new Vector2(310, 0), new Vector2(250, 95));
        foreach (var name in new[] { "ShopItem_Special_1", "ShopItem_Special_2" })
        {
            var item = Find(shop, name);
            Rect(Find(item, "Text_Name"), new Vector2(-15, 26), new Vector2(480, 60));
            Rect(Find(item, "Text_Detail"), new Vector2(-45, -30), new Vector2(420, 52));
            Find(item, "Text_Name").GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            Find(item, "Text_Detail").GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        }
        foreach (var button in shop.GetComponentsInChildren<Button>(true).Where(button => button.name == "Button_Buy"))
            button.GetComponent<Image>().pixelsPerUnitMultiplier = 4;
        var balance = Find(shop, "BalanceBar");
        Rect(game.shopFeathers.transform, new Vector2(-165, 0), new Vector2(255, 75));
        Rect(game.shopCoins.transform, new Vector2(215, 0), new Vector2(180, 75));
        game.shopFeathers.color = game.shopCoins.color = new Color(.18f, .14f, .3f);
        game.shopFeathers.text = "5/5";
        game.shopCoins.text = "1200";
        AddImage(balance, "Icon_Feather", Icon("Feather"), new Vector2(-330, 0), new Vector2(70, 78));
        AddImage(balance, "Icon_Coin", Icon("Coin"), new Vector2(55, 0), new Vector2(67, 67));
        SmallPlus(balance, "Button_FeatherInfo", new Vector2(-12, 0));
        SmallPlus(balance, "Button_CoinInfo", new Vector2(365, 0));
        AddImage(game.screens[5].transform, "Icon_EnergyFeather", Icon("Feather"), new Vector2(-135, 1000), new Vector2(90, 95));
        game.noFeathersCounter.text = "0/5";
        game.noFeathersCounterRestored.text = "1/5";
        var stats = game.screens[6].transform;
        var graph = Find(stats, "ActivityGraph");
        Rect(graph, new Vector2(0, -330), new Vector2(920, 500));
        Skin(graph.GetComponent<Image>(), "CreamCard");
        Find(graph, "Text_Days").gameObject.SetActive(false);
        game.activityDayLabels = new Text[7];
        for (var i = 0; i < 7; i++)
        {
            AddText(graph, "Day_" + (i + 1), new[] { "ВС", "ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ" }[i],
                new Vector2(game.activityBars[i].anchoredPosition.x, -185), new Vector2(90, 45), 24);
            game.activityDayLabels[i] = Find(graph, "Day_" + (i + 1)).GetComponent<Text>();
            game.activityDayLabels[i].color = new Color(.38f, .38f, .5f);
        }
        Rect(game.statsLevel.transform, new Vector2(55, 55), new Vector2(680, 90));
        game.statsLevel.text = "Эрудиция 101";
        AddImage(game.statsLevel.transform.parent, "Icon_Crown", Icon("Crown"), new Vector2(-325, 55), new Vector2(100, 85));
        game.victoryCollectionReward.fontSize = 31;
        game.victoryCollectionReward.rectTransform.sizeDelta = new Vector2(360, 135);
        var debugSubtitle = game.screens[11].GetComponentsInChildren<Text>(true).FirstOrDefault(text => text.text.Contains("no-code"));
        if (debugSubtitle != null) debugSubtitle.text = "Проверка ресурсов, экранов и игровых состояний";
    }

    private static void SmallPlus(Transform parent, string name, Vector2 position)
    {
        var image = AddImage(parent, name, Sprite("KeyboardKey"), position, new Vector2(48, 48));
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        AddText(image.transform, "Text_Label", "+", Vector2.zero, new Vector2(46, 46), 37);
        image.GetComponentInChildren<Text>().color = new Color(.18f, .15f, .35f);
    }

    private static void PolishMain(Transform screen)
    {
        var block = Find(screen, "EruditionBlock");
        Rect(block, new Vector2(0, 750), new Vector2(760, 260));
        Skin(block.GetComponent<Image>(), "TitleRibbon");
        ReplaceGlyph(Find(block, "Icon_Crown"), "Crown");
        Rect(Find(block, "Icon_Crown"), new Vector2(0, 150), new Vector2(130, 110));
        var title = Find(block, "Text_Title").GetComponent<Text>();
        Rect(title.transform, new Vector2(0, 30), new Vector2(700, 110));
        title.fontSize = 88;
        title.fontStyle = FontStyle.Bold;
        var badge = AddImage(block, "ValueBadge", Sprite("CreamCard"), new Vector2(0, -85), new Vector2(250, 106));
        badge.transform.SetSiblingIndex(0);
        Rect(Find(block, "Text_Value"), new Vector2(0, -85), new Vector2(230, 96));
        Find(block, "Text_Value").GetComponent<Text>().fontSize = 76;
        Rect(Find(screen, "OwlMascot"), new Vector2(0, 165), new Vector2(795, 775));
        MainButton(Find(screen, "Button_Classic"), -385, "Book");
        MainButton(Find(screen, "Button_Turbo"), -715, "Lightning");
        var modes = Find(screen, "Button_ModeSelection");
        Rect(modes, new Vector2(0, -923), new Vector2(310, 68));
        modes.GetComponent<Image>().color = new Color(1, 1, 1, .85f);
        var modeLabel = Find(modes, "Text_Label").GetComponent<Text>();
        modeLabel.text = "Все режимы ›";
        modeLabel.fontSize = 26;
        Rect(modeLabel.transform, Vector2.zero, new Vector2(300, 60));
        ReplaceGlyph(Find(Find(screen, "FeatherEnergy"), "Icon_Feather"), "Feather");
        ButtonIcon(Find(screen, "Button_Settings"), "Gear");
        ButtonIcon(Find(screen, "Button_StatisticsShortcut"), "Statistics");
    }

    private static void MainButton(Transform button, float y, string icon)
    {
        Rect(button, new Vector2(0, y), new Vector2(960, 300));
        var text = Find(button, "Text_Label").GetComponent<Text>();
        Rect(text.transform, new Vector2(85, 0), new Vector2(600, 246));
        text.alignment = TextAnchor.MiddleLeft;
        text.fontSize = 44;
        text.lineSpacing = 1.08f;
        text.resizeTextForBestFit = false;
        text.text = icon == "Book" ? "<size=56>Классика</size>\n<size=34>Длинные цитаты,\nстихи и тексты</size>"
            : "<size=56>Турбо</size>\n<size=34>Короткие фразы\nи выражения</size>";
        AddImage(button, "Icon_" + icon, Icon(icon), new Vector2(-335, 0), new Vector2(185, 190));
        AddText(button, "Icon_Arrow", "›", new Vector2(414, 0), new Vector2(65, 100), 86);
    }

    private static void PolishBoard(Transform screen, bool turbo)
    {
        var hearts = Find(screen, "HeartsContainer");
        foreach (Transform heart in hearts) if (heart.name.StartsWith("Heart_")) ReplaceGlyph(heart, "Heart");
        Rect(Find(screen, "OwlMascot_Small"), new Vector2(0, 570), new Vector2(570, 515));
        var panel = Find(screen, "PuzzlePanel");
        Rect(panel, new Vector2(0, -65), new Vector2(990, 930));
        var keyboard = Find(screen, "Keyboard");
        Rect(keyboard, new Vector2(0, -810), new Vector2(1020, 610));
        if (keyboard.GetComponent<Image>() != null) keyboard.GetComponent<Image>().color = Color.clear;
        Rect(Find(keyboard, "Row_01"), new Vector2(0, 175), new Vector2(980, 130));
        Rect(Find(keyboard, "Row_02"), new Vector2(0, 20), new Vector2(980, 130));
        Rect(Find(keyboard, "Row_03"), new Vector2(0, -135), new Vector2(980, 130));
        Rect(Find(keyboard, "Button_Check"), new Vector2(0, -270), new Vector2(560, 82));
        Find(keyboard, "Button_Check").GetComponent<Image>().pixelsPerUnitMultiplier = 4;
        foreach (var key in keyboard.GetComponentsInChildren<KeyboardKeyView>(true))
        {
            var text = key.GetComponentInChildren<Text>(true);
            text.fontStyle = FontStyle.Bold;
        }
        foreach (var cell in screen.GetComponentsInChildren<LetterCellView>(true))
        {
            var letter = Find(cell.transform, "Text_Letter").GetComponent<Text>();
            letter.fontStyle = FontStyle.Bold;
            letter.fontSize = 38;
        }
        Skin(Find(screen, "Label_Mode").GetComponent<Image>(), turbo ? "PurpleButton" : "BlueButton");
        var label = Find(Find(screen, "Label_Mode"), "Text_Mode").GetComponent<Text>();
        label.text = turbo ? "Турбо" : "Классика";
        Rect(label.transform, new Vector2(30, 0), new Vector2(218, 88));
        label.fontSize = 33;
        AddImage(label.transform.parent, "Icon_Mode", Icon(turbo ? "Lightning" : "Book"), new Vector2(-105, 0), new Vector2(75, 78));
        var badge = Find(screen, "EruditionBadge");
        Skin(badge.GetComponent<Image>(), "CreamCard");
        var value = Find(badge, "Text_Value");
        value.GetComponent<Text>().text = "101";
        Rect(value, new Vector2(38, 0), new Vector2(150, 88));
        AddImage(badge, "Icon_Crown", Icon("Crown"), new Vector2(-73, 0), new Vector2(76, 70));
        var hint = Find(screen, "Button_Hint");
        AddImage(hint, "Icon_Bulb", Icon("Bulb"), new Vector2(-6, -6), new Vector2(78, 87));
        var hintText = Find(hint, "Text_Label").GetComponent<Text>();
        Rect(hintText.transform, new Vector2(35, 40), new Vector2(55, 50));
        hintText.fontSize = 30;
        hintText.text = "2";
        hintText.color = new Color(.12f, .26f, .64f);
        hintText.transform.SetAsLastSibling();
        foreach (var key in keyboard.GetComponentsInChildren<KeyboardKeyView>(true)) key.CaptureNormalColor();
        var top = new GameObject("TopBar", typeof(RectTransform));
        top.layer = 5;
        var topRect = top.GetComponent<RectTransform>();
        topRect.SetParent(screen, false);
        topRect.anchorMin = Vector2.zero;
        topRect.anchorMax = Vector2.one;
        topRect.offsetMin = topRect.offsetMax = Vector2.zero;
        foreach (var name in new[] { "Button_BackOrPause", "Label_Mode", "EruditionBadge", "Button_Hint", "HeartsContainer" })
            Find(screen, name).SetParent(topRect, false);
    }

    private static void PolishNavigation(Transform screen)
    {
        var nav = screen.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == "BottomNavigation");
        if (nav == null) return;
        var names = new[] { "Home", "Statistics", "Levels", "Achievements", "Shop" };
        var titles = new[] { "Главная", "Статистика", "Уровни", "Достижения", "Магазин" };
        var icons = new[] { "Home", "Statistics", "Book", "Trophy", "Shop" };
        for (var i = 0; i < names.Length; i++)
        {
            var button = Find(nav, "Button_" + names[i]);
            button.GetComponent<Image>().color = Color.clear;
            var label = Find(button, "Text_Label").GetComponent<Text>();
            label.text = titles[i];
            label.fontSize = 25;
            var selected = screen.name == "Screen_" + (i == 0 ? "MainMenu" : names[i]);
            label.color = selected ? new Color(.05f, .38f, .95f) : new Color(.4f, .4f, .57f);
            Rect(label.transform, new Vector2(0, -44), new Vector2(190, 47));
            var image = AddImage(button, "Icon_" + icons[i], Icon(icons[i]), new Vector2(0, 25), new Vector2(78, 80));
            if (!selected) image.color = new Color(.73f, .75f, .85f, .85f);
        }
    }

    private static void ButtonIcon(Transform button, string icon)
    {
        Find(button, "Text_Label").gameObject.SetActive(false);
        AddImage(button, "Icon_" + icon, Icon(icon), Vector2.zero, new Vector2(82, 82));
    }

    private static void ReplaceGlyph(Transform transform, string icon)
    {
        var text = transform.GetComponent<Text>();
        if (text != null) UnityEngine.Object.DestroyImmediate(text);
        var image = transform.GetComponent<Image>();
        if (image == null) image = transform.gameObject.AddComponent<Image>();
        image.sprite = Icon(icon);
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private static void Skin(Image image, string sprite)
    {
        if (image == null) return;
        image.sprite = Sprite(sprite);
        image.type = sprite == "TitleRibbon" ? Image.Type.Simple : Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1.8f;
        image.color = Color.white;
    }

    private static Sprite Sprite(string name) => AssetDatabase.LoadAllAssetsAtPath(Surfaces).OfType<Sprite>().Single(sprite => sprite.name == name);
    private static Sprite Icon(string name) => AssetDatabase.LoadAllAssetsAtPath(Icons).OfType<Sprite>().Single(sprite => sprite.name == name);
    private static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).First(item => item.name == name);
    private static void Rect(Transform transform, Vector2 position, Vector2 size)
    {
        var rect = transform.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Image AddImage(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.layer = 5;
        obj.transform.SetParent(parent, false);
        Rect(obj.transform, position, size);
        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static void AddText(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        obj.layer = 5;
        obj.transform.SetParent(parent, false);
        Rect(obj.transform, position, size);
        var text = obj.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private static void Import()
    {
        foreach (var name in new[] { "victory", "sad", "shop" })
        {
            var path = "Assets/Art/owl_" + name + "_v2.png";
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        AssetDatabase.ImportAsset(Surfaces);
        AssetDatabase.ImportAsset(Icons);
        var surfaceRects = new[] {
            Slice("GreenButton", 5, 70, 535, 240, new Vector4(100, 65, 100, 65)),
            Slice("PurpleButton", 542, 70, 544, 240, new Vector4(100, 65, 100, 65)),
            Slice("BlueButton", 5, 338, 535, 234, new Vector4(100, 65, 100, 65)),
            Slice("GoldButton", 542, 338, 544, 234, new Vector4(100, 65, 100, 65)),
            Slice("Parchment", 5, 600, 535, 470, new Vector4(95, 100, 95, 100)),
            Slice("CreamCard", 542, 644, 544, 398, new Vector4(75, 75, 75, 75)),
            Slice("KeyboardKey", 115, 1100, 306, 317, new Vector4(60, 60, 60, 60)),
            Slice("TitleRibbon", 484, 1130, 602, 240, Vector4.zero)
        };
        SetImporter(Surfaces, surfaceRects);
        SetImporter(Icons, new[] {
            IconSlice("Feather", 45, 20, 320, 375), IconSlice("Crown", 387, 62, 319, 283),
            IconSlice("Heart", 741, 80, 320, 280), IconSlice("Bulb", 1058, 0, 390, 380),
            IconSlice("Book", 35, 410, 358, 315), IconSlice("Lightning", 440, 389, 255, 338),
            IconSlice("Gear", 742, 405, 321, 314), IconSlice("Statistics", 1100, 420, 317, 284),
            IconSlice("Home", 39, 743, 330, 310), IconSlice("Trophy", 382, 738, 344, 332),
            IconSlice("Shop", 738, 738, 333, 320), IconSlice("Coin", 1109, 738, 304, 324)
        });
    }

    private static SpriteMetaData Slice(string name, float x, float top, float width, float height, Vector4 border)
        => new SpriteMetaData { name = name, rect = new Rect(x, 1448 - top - height, width, height), pivot = new Vector2(.5f, .5f), border = border };

    private static SpriteMetaData IconSlice(string name, float x, float top, float width, float height)
        => new SpriteMetaData { name = name, rect = new Rect(x, 1086 - top - height, width, height), pivot = new Vector2(.5f, .5f) };

    private static void SetImporter(string path, SpriteMetaData[] slices)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 100;
        importer.spritesheet = slices;
        importer.SaveAndReimport();
    }

    public static void CreatePrefabs(CryptogramGame game)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        var entries = new[] {
            ("BottomNavigation", Find(game.screens[0].transform, "BottomNavigation")),
            ("TopBar", Find(game.screens[2].transform, "TopBar")),
            ("FeatherEnergyWidget", Find(game.screens[0].transform, "FeatherEnergy")),
            ("EruditionBadge", Find(game.screens[2].transform, "EruditionBadge")),
            ("HeartIndicator", Find(game.screens[2].transform, "Heart_01")),
            ("HintButton", Find(game.screens[2].transform, "Button_Hint")),
            ("LetterCell", game.classicBoard.GetComponentsInChildren<LetterCellView>(true).First(cell => cell.gameObject.activeSelf && cell.Code > 0).transform),
            ("AuthorCard", game.authorCards[0].transform), ("CollectionCard", game.themeCards[0].transform),
            ("AchievementCard", game.achievementCards[0].transform),
            ("ShopItemCard", Find(game.screens[9].transform, "ShopItem_Feathers_1")),
            ("RewardCard", Find(game.screens[4].transform, "RewardsPanel")),
            ("OwlMascotBlock", Find(game.screens[0].transform, "OwlMascot"))
        };
        foreach (var entry in entries) PrefabUtility.SaveAsPrefabAsset(entry.Item2.gameObject, "Assets/Prefabs/" + entry.Item1 + ".prefab");
    }
}
