using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Erudition;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class FinishGameProject
{
    private static Font font;
    private static Sprite rounded;
    private static CryptogramGame game;
    private static Transform safe;
    private static GameObject[] screens;

    public static void RunAndTest()
    {
        Run();
        RuntimeSmoke.Run();
    }

    public static void FinalReview()
    {
        Run();
        RuntimeSmoke.RunWithCapture();
    }

    public static void Verify()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        var all = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
        var missing = all.Count(component => component == null);
        var texts = all.OfType<Text>().ToArray();
        var gameController = all.OfType<CryptogramGame>().Single();
        var actions = all.OfType<UiAction>().ToArray();
        var cells = all.OfType<LetterCellView>().ToArray();
        var keys = all.OfType<KeyboardKeyView>().ToArray();
        if (missing != 0 || texts.Any(label => label.font == null)) throw new Exception("Missing components or fonts: " + missing);
        foreach (var button in all.OfType<Button>())
        {
            if (button.onClick.GetPersistentEventCount() == 0) throw new Exception("Button has no saved action: " + button.name);
            for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentTarget(i) == null || string.IsNullOrEmpty(button.onClick.GetPersistentMethodName(i)))
                    throw new Exception("Broken saved button action: " + button.name);
        }
        const string alphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
        foreach (var board in new[] { gameController.classicBoard, gameController.turboBoard })
            if (!board.GetComponentsInChildren<KeyboardKeyView>(true).Select(key => key.name[4]).OrderBy(letter => letter)
                .SequenceEqual(alphabet.OrderBy(letter => letter))) throw new Exception("Keyboard alphabet incomplete");
        if (gameController.screens.Length != 13 || cells.Length != 117 || keys.Length != 66 || actions.Length < 80)
            throw new Exception("Incomplete UI wiring: screens=" + gameController.screens.Length + " cells=" + cells.Length
                + " keys=" + keys.Length + " actions=" + actions.Length);
        if (gameController.library == null || gameController.library.entries.Length != 24) throw new Exception("Puzzle library not wired");
        foreach (var entry in gameController.library.entries)
            if (!(entry.mode == PuzzleMode.Classic ? gameController.classicBoard : gameController.turboBoard).CanFit(entry.text))
                throw new Exception("Puzzle no longer fits: " + entry.id);
        var serializer = new XmlSerializer(typeof(GameSave));
        var testSave = new GameSave { reserveFeathers = 10, erudition = 255 };
        string xml;
        using (var writer = new StringWriter()) { serializer.Serialize(writer, testSave); xml = writer.ToString(); }
        GameSave roundTrip;
        using (var reader = new StringReader(xml)) roundTrip = (GameSave)serializer.Deserialize(reader);
        if (roundTrip.reserveFeathers != 10 || roundTrip.erudition != 255) throw new Exception("Save serialization failed");
        Debug.Log("VERIFY_FINISHED screens=" + gameController.screens.Length + " cells=" + cells.Length
            + " keys=" + keys.Length + " buttons=" + actions.Length + " puzzles=" + gameController.library.entries.Length
            + " missing=" + missing + " unfonted=" + texts.Count(label => label.font == null));
    }

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Inter-Regular.ttf");
        rounded = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/rounded_panel.png");
        if (font == null || rounded == null) throw new Exception("Core UI assets are missing");
        safe = Root("SafeArea_1080x2340");
        screens = new[]
        {
            "MainMenu", "ModeSelection", "GameplayClassic", "GameplayTurbo", "Victory",
            "NoFeathers", "Statistics", "Collections", "Achievements", "Shop", "Settings", "DebugPanel"
        }.Select(name => Find(safe, "Screen_" + name).gameObject).ToArray();

        var systems = new GameObject("GameSystems");
        game = systems.AddComponent<CryptogramGame>();
        game.ads = systems.AddComponent<AdsBridge>();
        game.library = BuildLibrary();
        safe.gameObject.AddComponent<SafeAreaFitter>();

        SetupGameplay();
        SetupCollections();
        SetupAchievements();
        SetupSettings();
        SetupDebug();
        SetupShop();
        SetupMainMenu();
        SetupDefeat();
        BindGameReferences();
        ReferencePolish.Apply(game);
        AudioAuthoring.Apply(game);
        WireButtons();
        game.classicBoard.StartPuzzle(game.library.entries[0], 0, new PuzzleProgress { remainingHearts = 5 }, 101, 2);
        var turboPreview = Array.FindIndex(game.library.entries, entry => entry.mode == PuzzleMode.Turbo);
        game.turboBoard.StartPuzzle(game.library.entries[turboPreview], turboPreview, new PuzzleProgress { remainingHearts = 3 }, 101, 2);
        ReferencePolish.CreatePrefabs(game);

        EditorUtility.SetDirty(game);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("FINISH_GAME scene objects=" + scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length)
            + " cells=" + (game.classicBoard.GetComponentsInChildren<LetterCellView>(true).Length + game.turboBoard.GetComponentsInChildren<LetterCellView>(true).Length)
            + " puzzles=" + game.library.entries.Length);
    }

    private static PuzzleLibrary BuildLibrary()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        const string path = "Assets/Data/PuzzleLibrary.asset";
        var library = AssetDatabase.LoadAssetAtPath<PuzzleLibrary>(path);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<PuzzleLibrary>();
            AssetDatabase.CreateAsset(library, path);
        }
        if (library.entries != null && library.entries.Length > 0) return library;
        library.entries = new[]
        {
            Entry("c_pushkin_01", PuzzleMode.Classic, "Любви все возрасты покорны.", "А. С. Пушкин", 0, 0, 1, 0),
            Entry("c_tolstoy_01", PuzzleMode.Classic, "Все счастливые семьи похожи друг на друга.", "Л. Н. Толстой", 0, 1, 1, 1),
            Entry("c_dostoevsky_01", PuzzleMode.Classic, "Красота спасёт мир.", "Ф. М. Достоевский", 0, 2, 2, 1),
            Entry("c_chekhov_01", PuzzleMode.Classic, "В человеке всё должно быть прекрасно.", "А. П. Чехов", 100, 3, 2, 1),
            Entry("c_gogol_01", PuzzleMode.Classic, "Русь, куда ж несёшься ты?", "Н. В. Гоголь", 100, 4, 0, 2),
            Entry("c_turgenev_01", PuzzleMode.Classic, "Утро туманное, утро седое.", "И. С. Тургенев", 100, 5, 0, 0),
            Entry("c_original_01", PuzzleMode.Classic, "Чтение открывает новые дороги для мысли.", "Эрудиция", 180, -1, 2, 0),
            Entry("c_original_02", PuzzleMode.Classic, "Добрая мысль освещает самый длинный путь.", "Эрудиция", 180, -1, 2, 2),
            Entry("c_original_03", PuzzleMode.Classic, "Слово пишется пером, а остаётся в памяти.", "Эрудиция", 260, -1, 2, 1),
            Entry("c_original_04", PuzzleMode.Classic, "В каждой книге живёт целый неизведанный мир.", "Эрудиция", 260, -1, 2, 2),
            Entry("c_original_05", PuzzleMode.Classic, "Человек учится, пока ищет ответы на вопросы.", "Эрудиция", 350, -1, 2, 1),
            Entry("c_original_06", PuzzleMode.Classic, "Когда мы читаем, наш внутренний мир становится богаче.", "Эрудиция", 350, -1, 2, 0),
            Entry("t_01", PuzzleMode.Turbo, "Знание — сила.", "Народная мудрость", 0, -1, 2, 1),
            Entry("t_02", PuzzleMode.Turbo, "Книга — друг.", "Народная мудрость", 0, -1, 2, 0),
            Entry("t_03", PuzzleMode.Turbo, "Смело думай.", "Эрудиция", 0, -1, 2, 1),
            Entry("t_04", PuzzleMode.Turbo, "Слово лечит.", "Эрудиция", 100, -1, 1, 0),
            Entry("t_05", PuzzleMode.Turbo, "Век живи — век учись.", "Народная мудрость", 100, -1, 2, 1),
            Entry("t_06", PuzzleMode.Turbo, "Мечтай и читай.", "Эрудиция", 100, -1, 1, 2),
            Entry("t_07", PuzzleMode.Turbo, "Ищи свой ответ.", "Эрудиция", 180, -1, 2, 1),
            Entry("t_08", PuzzleMode.Turbo, "Мысли шире.", "Эрудиция", 180, -1, 2, 1),
            Entry("t_09", PuzzleMode.Turbo, "Добро возвращается.", "Эрудиция", 260, -1, 1, 2),
            Entry("t_10", PuzzleMode.Turbo, "В словах есть сила.", "Эрудиция", 260, -1, 2, 0),
            Entry("t_11", PuzzleMode.Turbo, "Читай каждый день.", "Эрудиция", 350, -1, 2, 0),
            Entry("t_12", PuzzleMode.Turbo, "Ум растёт от вопросов.", "Эрудиция", 350, -1, 2, 1)
        };
        EditorUtility.SetDirty(library);
        return library;
    }

    private static PuzzleEntry Entry(string id, PuzzleMode mode, string text, string source, int min, int author, int theme, int book)
    {
        return new PuzzleEntry { id = id, mode = mode, text = text, source = source,
            minimumErudition = min, authorIndex = author, themeIndex = theme, bookIndex = book };
    }

    private static void SetupGameplay()
    {
        game.classicBoard = SetupBoard(screens[2].transform, PuzzleMode.Classic, 18, 4, 49f, 145f);
        game.turboBoard = SetupBoard(screens[3].transform, PuzzleMode.Turbo, 15, 3, 54f, 105f);
        foreach (var entry in game.library.entries)
        {
            var board = entry.mode == PuzzleMode.Classic ? game.classicBoard : game.turboBoard;
            if (!board.CanFit(entry.text)) throw new Exception("Puzzle exceeds its grid: " + entry.id + " / " + entry.text);
        }
    }

    private static PuzzleBoard SetupBoard(Transform screen, PuzzleMode mode, int columns, int rows, float spacing, float firstY)
    {
        var board = screen.gameObject.AddComponent<PuzzleBoard>();
        var content = Find(screen, "PuzzleContent");
        foreach (var child in content.Cast<Transform>().Where(child => child.name.StartsWith("Puzzle_Row_") || child.name.StartsWith("Reveal_")).ToArray())
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        var instruction = Find(content, "Text_Instruction").GetComponent<Text>();
        var feedback = Find(content, "Text_RevealFeedback").GetComponent<Text>();
        feedback.gameObject.SetActive(true);
        feedback.text = "Нажмите на скрытую букву, затем выберите клавишу";
        feedback.fontSize = 25;
        feedback.rectTransform.anchoredPosition = new Vector2(0, mode == PuzzleMode.Classic ? 245 : 170);
        feedback.rectTransform.sizeDelta = new Vector2(900, 58);

        var cells = new LetterCellView[columns * rows];
        for (var row = 0; row < rows; row++)
        for (var col = 0; col < columns; col++)
        {
            var index = row * columns + col;
            var x = (col - (columns - 1) / 2f) * spacing;
            var y = firstY - row * (mode == PuzzleMode.Classic ? 155 : 165);
            var root = UiRect(content, $"LetterCell_R{row + 1:00}_C{col + 1:00}", new Vector2(x, y), new Vector2(spacing - 3, 106));
            var image = root.gameObject.AddComponent<Image>();
            image.color = new Color(1, 1, 1, 0.001f);
            image.raycastTarget = true;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var letter = UiText(root, "Text_Letter", "", new Vector2(0, 26), new Vector2(spacing - 3, 53),
                mode == PuzzleMode.Classic ? 36 : 40, new Color(0.1f, 0.16f, 0.34f), TextAnchor.MiddleCenter);
            var underline = UiImage(root, "Underline", new Vector2(0, -8), new Vector2(spacing - 9, 4),
                new Color(0.14f, 0.23f, 0.43f));
            var number = UiText(root, "Text_Code", "", new Vector2(0, -40), new Vector2(spacing - 3, 30),
                19, new Color(0.34f, 0.42f, 0.58f), TextAnchor.MiddleCenter);
            var cell = root.gameObject.AddComponent<LetterCellView>();
            cell.Configure(board, index, letter, number, underline, button);
            UnityEventTools.AddPersistentListener(button.onClick, cell.Press);
            root.gameObject.SetActive(false);
            cells[index] = cell;
        }

        var keyboard = Find(screen, "Keyboard");
        var firstRow = Find(keyboard, "Row_01");
        var secondRow = Find(keyboard, "Row_02");
        var keyTemplate = Find(firstRow, "Key_Й");
        foreach (var added in new[] { (parent: firstRow, letter: "Ъ"), (parent: secondRow, letter: "Ё") })
        {
            var extraKey = UnityEngine.Object.Instantiate(keyTemplate.gameObject, added.parent);
            extraKey.name = "Key_" + added.letter;
            Find(extraKey.transform, "Text_Label").GetComponent<Text>().text = added.letter;
        }
        foreach (var row in new[] { firstRow, secondRow })
        {
            var rowKeys = row.Cast<Transform>().Where(child => child.name.StartsWith("Key_")).ToArray();
            for (var i = 0; i < rowKeys.Length; i++)
            {
                rowKeys[i].GetComponent<RectTransform>().anchoredPosition = new Vector2((i - (rowKeys.Length - 1) / 2f) * 77, 0);
                rowKeys[i].GetComponent<RectTransform>().sizeDelta = new Vector2(72, 125);
            }
        }
        var keys = keyboard.GetComponentsInChildren<Button>(true).Where(button => button.name.StartsWith("Key_")).ToArray();
        var keyViews = new List<KeyboardKeyView>();
        foreach (var key in keys)
        {
            var letter = key.name.Substring(4);
            ClearButton(key);
            var view = key.gameObject.AddComponent<KeyboardKeyView>();
            view.Configure(board, letter, key.GetComponent<Image>());
            UnityEventTools.AddPersistentListener(key.onClick, view.Press);
            keyViews.Add(view);
        }
        var hearts = Find(screen, "HeartsContainer").Cast<Transform>().Where(child => child.name.StartsWith("Heart_")).Select(child => child.gameObject).ToArray();
        var hintText = Find(Find(screen, "Button_Hint"), "Text_Label").GetComponent<Text>();
        board.Configure(game, mode, columns, cells, keyViews.ToArray(), hearts, instruction, feedback, hintText);
        return board;
    }

    private static void SetupCollections()
    {
        var screen = screens[7].transform;
        var authors = Group(screen, "Group_Authors");
        var themes = Group(screen, "Group_Themes");
        var books = Group(screen, "Group_Books");
        var authorCards = new CollectionCardView[6];
        var targets = new[] { 20, 30, 18, 25, 20, 24 };
        for (var i = 0; i < 6; i++)
        {
            var card = Find(screen, "CollectionCard_" + (i + 1));
            card.SetParent(authors, false);
            authorCards[i] = ConfigureCard(card, targets[i]);
        }
        game.authorCards = authorCards;
        var names = new[] { "Природа", "Любовь", "Мудрость", "Поэзия", "Романы", "Сказки" };
        var spriteNames = new[] { "Theme_Nature", "Theme_Love", "Theme_Wisdom", "Book_Poetry", "Book_Novels", "Book_FairyTales" };
        var themeCards = new CollectionCardView[3];
        var bookCards = new CollectionCardView[3];
        for (var i = 0; i < 6; i++)
        {
            var parent = i < 3 ? themes : books;
            var local = i % 3;
            var clone = UnityEngine.Object.Instantiate(authorCards[0].gameObject, parent);
            clone.name = (i < 3 ? "ThemeCard_" : "BookCard_") + (local + 1);
            clone.GetComponent<RectTransform>().anchoredPosition = local == 2 ? new Vector2(0, 160) : new Vector2(local == 0 ? -240 : 240, 545);
            Find(clone.transform, "Text_Name").GetComponent<Text>().text = names[i];
            var portrait = clone.GetComponentsInChildren<Image>(true).First(image => image.name.StartsWith("Portrait_"));
            portrait.name = "Illustration_" + spriteNames[i];
            portrait.sprite = SpriteFromSheet("Assets/Art/SpriteSheets/collections_missing.png", spriteNames[i]);
            var rect = portrait.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(390, 185);
            rect.anchoredPosition = new Vector2(0, 77);
            var card = clone.GetComponent<CollectionCardView>();
            card.target = i < 3 ? new[] { 30, 15, 20 }[i] : new[] { 25, 30, 20 }[i - 3];
            if (i < 3) themeCards[i] = card; else bookCards[i - 3] = card;
        }
        game.themeCards = themeCards;
        game.bookCards = bookCards;
        game.collectionGroups = new[] { authors.gameObject, themes.gameObject, books.gameObject };
        themes.gameObject.SetActive(false);
        books.gameObject.SetActive(false);
        game.collectionTabs = new[] { "Tab_Авторы", "Tab_Темы", "Tab_Книги" }
            .Select(name => Find(screen, name).GetComponent<Button>()).ToArray();
    }

    private static CollectionCardView ConfigureCard(Transform card, int target)
    {
        var view = card.GetComponent<CollectionCardView>();
        if (view == null) view = card.gameObject.AddComponent<CollectionCardView>();
        view.title = Find(card, "Text_Name").GetComponent<Text>();
        view.progressText = Find(card, "Text_Progress").GetComponent<Text>();
        view.progressFill = Find(card, "Progress_Fill").GetComponent<RectTransform>();
        view.target = target;
        view.fullProgressWidth = 210;
        return view;
    }

    private static void SetupAchievements()
    {
        var screen = screens[8].transform;
        game.achievementTabs = new[] { "Tab_Все", "Tab_Прогресс", "Tab_Особые" }
            .Select(name => Find(screen, name).GetComponent<Button>()).ToArray();
        game.achievementCards = new AchievementCardView[5];
        var targets = new[] { 1, 25, 200, 20, 10 };
        for (var i = 0; i < 5; i++)
        {
            var root = Find(screen, "AchievementCard_" + (i + 1));
            var view = root.gameObject.AddComponent<AchievementCardView>();
            view.progressText = Find(root, "Text_Progress").GetComponent<Text>();
            view.progressFill = Find(root, "Progress_Fill").GetComponent<RectTransform>();
            view.target = targets[i];
            game.achievementCards[i] = view;
        }
    }

    private static void SetupSettings()
    {
        var screen = screens[10].transform;
        var names = new[] { "Музыка", "Звуки", "Вибрация", "Крупный текст" };
        game.settingValues = new Text[names.Length];
        for (var i = 0; i < names.Length; i++)
        {
            var toggle = Find(screen, "Toggle_" + names[i]);
            var image = toggle.GetComponent<Image>();
            var button = toggle.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            game.settingValues[i] = Find(toggle, "Knob").GetComponent<Text>();
        }
        game.debugOpenButton = Find(screen, "Button_OpenDebug").gameObject;
    }

    private static void SetupDebug()
    {
        var screen = screens[11].transform;
        var panel = Find(screen, "DebugControls");
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(940, 1900);
        panel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);
        var labels = new[]
        {
            ("Button_AddErudition", "+10 эрудиции"),
            ("Button_RemoveFeather", "−1 перо"),
            ("Button_ZeroFeathers", "0/5 перьев"),
            ("Button_RefillFeathers", "Восстановить перья"),
            ("Button_AddHint", "+1 подсказка"),
            ("Button_Reset", "Сброс прогресса"),
            ("Button_ForceDefeat", "Поражение"),
            ("Button_OneHeart", "Оставить 1 сердце"),
            ("Button_UnlockCollections", "Все коллекции"),
            ("Button_UnlockAchievements", "Все достижения")
        };
        for (var i = 0; i < labels.Length; i++)
        {
            var row = i / 2;
            var col = i % 2;
            var x = col == 0 ? -235 : 235;
            var y = -550 - row * 145;
            UiButton(panel, labels[i].Item1, labels[i].Item2, new Vector2(x, y), new Vector2(420, 125),
                i == 5 ? new Color(0.82f, 0.32f, 0.37f) : new Color(0.33f, 0.45f, 0.76f), 28);
        }
        var values = Find(panel, "DebugValues").GetComponent<Text>();
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(940, 1790);
        panel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
        var debugButtons = panel.GetComponentsInChildren<Button>(true);
        for (var i = 0; i < debugButtons.Length; i++)
        {
            var rect = debugButtons[i].GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(i % 2 == 0 ? -230 : 230, 760 - i / 2 * 145);
            rect.sizeDelta = new Vector2(420, 115);
            var label = debugButtons[i].GetComponentInChildren<Text>(true);
            label.fontSize = 28;
        }
        values.rectTransform.anchoredPosition = new Vector2(0, -830);
        game.debugValues = values;
    }

    private static void SetupShop()
    {
        var screen = screens[9].transform;
        game.shopMessage = UiText(screen, "Text_ShopMessage", "", new Vector2(0, -790), new Vector2(900, 70),
            30, new Color(0.15f, 0.35f, 0.3f), TextAnchor.MiddleCenter);
    }

    private static void SetupMainMenu()
    {
        var screen = screens[0].transform;
        UiButton(screen, "Button_ModeSelection", "Выбор режима", new Vector2(0, -745), new Vector2(560, 105),
            new Color(0.24f, 0.52f, 0.91f), 34);
    }

    private static void SetupDefeat()
    {
        var screen = Group(safe, "Screen_Defeat");
        screens = screens.Concat(new[] { screen.gameObject }).ToArray();
        UiText(screen, "Text_Title", "Не получилось", new Vector2(0, 865), new Vector2(850, 120),
            72, new Color(0.18f, 0.1f, 0.28f), TextAnchor.MiddleCenter);
        var owl = UiImage(screen, "OwlMascot_Defeat", new Vector2(0, 475), new Vector2(530, 490), Color.white);
        owl.sprite = SpriteFromSheet("Assets/Art/SpriteSheets/owl_states_missing.png", "Owl_NoFeathers");
        owl.preserveAspect = true;
        var card = UiRect(screen, "DefeatCard", new Vector2(0, 10), new Vector2(920, 470));
        var cardImage = card.gameObject.AddComponent<Image>();
        cardImage.sprite = rounded;
        cardImage.type = Image.Type.Sliced;
        cardImage.color = new Color(1f, 0.98f, 0.91f, 0.98f);
        game.defeatMessage = UiText(card, "Text_Message", "Сердечки закончились", new Vector2(0, 160), new Vector2(830, 80),
            39, new Color(0.27f, 0.2f, 0.34f), TextAnchor.MiddleCenter);
        game.defeatQuote = UiText(card, "Text_Quote", "", new Vector2(0, 20), new Vector2(820, 200),
            38, new Color(0.13f, 0.2f, 0.37f), TextAnchor.MiddleCenter);
        game.defeatSource = UiText(card, "Text_Source", "", new Vector2(0, -155), new Vector2(780, 65),
            28, new Color(0.47f, 0.42f, 0.48f), TextAnchor.MiddleCenter);
        UiButton(screen, "Button_Retry", "Повторить за 1 перо", new Vector2(0, -450), new Vector2(790, 140),
            new Color(0.1f, 0.7f, 0.46f), 39);
        UiButton(screen, "Button_ReturnMenu", "Вернуться в меню", new Vector2(0, -675), new Vector2(650, 120),
            new Color(0.4f, 0.47f, 0.67f), 34);
        screen.gameObject.SetActive(false);
    }

    private static void BindGameReferences()
    {
        game.screens = screens;
        game.mainErudition = TextAt(0, "EruditionBlock", "Text_Value");
        game.mainFeathers = TextAt(0, "FeatherEnergy", "Text_Energy");
        game.classicErudition = TextAt(2, "EruditionBadge", "Text_Value");
        game.turboErudition = TextAt(3, "EruditionBadge", "Text_Value");
        game.shopFeathers = TextAt(9, "BalanceBar", "Feathers");
        game.shopCoins = TextAt(9, "BalanceBar", "Coins");
        game.noFeathersCounter = Find(screens[5].transform, "FeatherCounter_Empty").GetComponent<Text>();
        game.noFeathersCounterRestored = Find(screens[5].transform, "FeatherCounter_Restored").GetComponent<Text>();
        game.noFeathersTimer = Find(screens[5].transform, "Text_Timer").GetComponent<Text>();
        game.noFeathersNote = Find(screens[5].transform, "Text_AdFeatherGranted").GetComponent<Text>();
        game.classicStartLabel = TextAt(0, "Button_Classic", "Text_Label");
        game.turboStartLabel = TextAt(0, "Button_Turbo", "Text_Label");
        game.victoryQuote = TextAt(4, "QuoteCard", "Text_Quote");
        game.victorySource = TextAt(4, "QuoteCard", "Text_Author");
        game.victoryEruditionReward = Find(screens[4].transform, "Reward_Erudition").GetComponent<Text>();
        game.victoryCollectionReward = Find(screens[4].transform, "Reward_Collection").GetComponent<Text>();
        game.victoryBonusNote = Find(screens[4].transform, "Text_AdBonusGranted").GetComponent<Text>();
        game.victoryAdButton = Find(screens[4].transform, "Button_RewardedAd").GetComponent<Button>();
        game.noFeathersAdButton = Find(screens[5].transform, "Button_RewardedAd").GetComponent<Button>();
        game.statsLevel = TextAt(6, "EruditionCard", "Text_Level");
        game.statsSolved = TextAt(6, "Card_Solved", "Text_Value");
        game.statsAccuracy = TextAt(6, "Card_Accuracy", "Text_Value");
        game.statsStreak = TextAt(6, "Card_Streak", "Text_Value");
        game.statsBest = TextAt(6, "Card_Best", "Text_Value");
        game.statsProgressFill = Find(Find(screens[6].transform, "EruditionCard"), "Progress_Fill").GetComponent<RectTransform>();
        game.activityBars = Enumerable.Range(1, 7).Select(i => Find(screens[6].transform, "Bar_" + i).GetComponent<RectTransform>()).ToArray();
    }

    private static void WireButtons()
    {
        var count = 0;
        for (var screenIndex = 0; screenIndex < screens.Length; screenIndex++)
        {
            foreach (var button in screens[screenIndex].GetComponentsInChildren<Button>(true))
            {
                if (button.name.StartsWith("Key_") || button.name.StartsWith("LetterCell_")) continue;
                var mapping = GetAction(screenIndex, button);
                if (mapping == null) throw new Exception("Unmapped button on Screen_" + screenIndex + ": " + button.name);
                var action = button.GetComponent<UiAction>();
                if (action == null) action = button.gameObject.AddComponent<UiAction>();
                action.Configure(game, mapping.Item1, mapping.Item2);
                ClearButton(button);
                UnityEventTools.AddPersistentListener(button.onClick, action.Press);
                count++;
            }
        }
        Debug.Log("WIRED buttons=" + count);
    }

    private static Tuple<UiActionKind, int> GetAction(int screen, Button button)
    {
        var name = button.name;
        UiActionKind action;
        var parameter = 0;
        if (name == "Button_Home" || name == "Button_ReturnMenu") action = UiActionKind.Home;
        else if (name == "Button_Statistics" || name == "Button_StatisticsShortcut") action = UiActionKind.Statistics;
        else if (name == "Button_Collections") action = UiActionKind.Collections;
        else if (name == "Button_Achievements") action = UiActionKind.Achievements;
        else if (name == "Button_Shop" || name == "Button_OpenShop" || name == "Button_AddEnergy") action = UiActionKind.Shop;
        else if (name == "Button_Settings") action = UiActionKind.Settings;
        else if (name == "Button_ModeSelection") action = UiActionKind.Home;
        else if (name == "Button_FeatherInfo") action = UiActionKind.FeatherInfo;
        else if (name == "Button_CoinInfo") action = UiActionKind.CoinInfo;
        else if (name == "Button_Classic" || name == "Button_PlayClassic") action = UiActionKind.Classic;
        else if (name == "Button_Turbo" || name == "Button_PlayTurbo") action = UiActionKind.Turbo;
        else if (name == "Button_Back" || name == "Button_BackOrPause") action = UiActionKind.Back;
        else if (name == "Button_Continue") action = UiActionKind.Continue;
        else if (name == "Button_Hint") action = UiActionKind.Hint;
        else if (name == "Button_Check") action = UiActionKind.Check;
        else if (name == "Button_RewardedAd") action = screen == 4 ? UiActionKind.RewardVictory : UiActionKind.RewardFeather;
        else if (name == "Button_Buy")
        {
            var parent = button.transform.parent.name;
            action = parent == "ShopItem_Feathers_1" ? UiActionKind.BuyFiveFeathers
                : parent == "ShopItem_Feathers_2" ? UiActionKind.BuyFifteenFeathers
                : parent == "Section_Hints" ? UiActionKind.BuyFiveHints : UiActionKind.PremiumUnavailable;
        }
        else if (name.StartsWith("Tab_") && screen == 7)
        {
            action = UiActionKind.CollectionTab;
            parameter = name == "Tab_Авторы" ? 0 : name == "Tab_Темы" ? 1 : 2;
        }
        else if (name.StartsWith("Tab_") && screen == 8)
        {
            action = UiActionKind.AchievementTab;
            parameter = name == "Tab_Все" ? 0 : name == "Tab_Прогресс" ? 1 : 2;
        }
        else if (name.StartsWith("Toggle_") && screen == 10)
        {
            action = UiActionKind.ToggleSetting;
            parameter = name == "Toggle_Музыка" ? 0 : name == "Toggle_Звуки" ? 1 : name == "Toggle_Вибрация" ? 2 : 3;
        }
        else if (name == "Button_OpenDebug") action = UiActionKind.Debug;
        else if (screen == 11)
        {
            action = name == "Button_MainMenu" ? UiActionKind.Home
                : name == "Button_AddErudition" ? UiActionKind.DebugAddErudition
                : name == "Button_RemoveFeather" ? UiActionKind.DebugRemoveFeather
                : name == "Button_ZeroFeathers" || name == "Button_Defeat" ? UiActionKind.DebugZeroFeathers
                : name == "Button_RefillFeathers" ? UiActionKind.DebugRefillFeathers
                : name == "Button_AddHint" ? UiActionKind.DebugAddHint
                : name == "Button_Victory" ? UiActionKind.DebugVictory
                : name == "Button_ForceDefeat" ? UiActionKind.DebugDefeat
                : name == "Button_Reset" ? UiActionKind.DebugReset
                : name == "Button_OneHeart" ? UiActionKind.DebugOneHeart
                : name == "Button_UnlockCollections" ? UiActionKind.DebugUnlockCollections
                : name == "Button_UnlockAchievements" ? UiActionKind.DebugUnlockAchievements
                : name == "Button_Statistics" ? UiActionKind.Statistics
                : name == "Button_Collections" ? UiActionKind.Collections
                : name == "Button_Achievements" ? UiActionKind.Achievements
                : name == "Button_Shop" ? UiActionKind.Shop
                : name == "Button_Classic" ? UiActionKind.Classic
                : name == "Button_Turbo" ? UiActionKind.Turbo
                : UiActionKind.Back;
        }
        else if (name == "Button_Retry") action = UiActionKind.Retry;
        else return null;
        return Tuple.Create(action, parameter);
    }

    private static void ClearButton(Button button)
    {
        for (var i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
    }

    private static Text TextAt(int screenIndex, string parentName, string textName)
    {
        return Find(Find(screens[screenIndex].transform, parentName), textName).GetComponent<Text>();
    }

    private static Sprite SpriteFromSheet(string path, string name)
    {
        var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().SingleOrDefault(asset => asset.name == name);
        if (sprite == null) throw new Exception("Sprite missing: " + path + " / " + name);
        return sprite;
    }

    private static Transform Root(string name)
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        return roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Single(transform => transform.name == name);
    }

    private static Transform Find(Transform parent, string name)
    {
        var item = parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(transform => transform.name == name);
        if (item == null) throw new Exception("Missing UI object under " + parent.name + ": " + name);
        return item;
    }

    private static RectTransform Group(Transform parent, string name)
    {
        var rect = UiRect(parent, name, Vector2.zero, Vector2.zero);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static RectTransform UiRect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.layer = 5;
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Text UiText(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor alignment)
    {
        var rect = UiRect(parent, name, position, size);
        rect.gameObject.AddComponent<CanvasRenderer>();
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.text = value;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Image UiImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var rect = UiRect(parent, name, position, size);
        rect.gameObject.AddComponent<CanvasRenderer>();
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Button UiButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color, int fontSize)
    {
        var image = UiImage(parent, name, position, size, color);
        image.sprite = rounded;
        image.type = Image.Type.Sliced;
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var text = UiText(image.transform, "Text_Label", label, Vector2.zero, size - new Vector2(20, 12), fontSize, Color.white, TextAnchor.MiddleCenter);
        text.fontStyle = FontStyle.Bold;
        return button;
    }
}
