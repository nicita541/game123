using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SeptemberFeedbackTests
{
    static bool tested, oldEnabled;
    static bool checkedCore;
    static IEnumerator extraFlow;
    static double nextStep;
    public static Action<CryptogramGame, GameSave> AdditionalChecks;
    public static Func<CryptogramGame, GameSave, IEnumerator> AdditionalFlow;
    public static string ReviewFolder = "SeptemberReview";
    static int frames, result;
    static EnterPlayModeOptions oldOptions;
    static CryptogramGame game;
    static GameSave state;
    const string Slot = "erudition.september-review.temporary";

    public static void Run()
    {
        try
        {
            VerifyScenes();
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            SaveStore.EditorTestSlot = Slot; SaveStore.Reset();
            oldEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            oldOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged += Changed;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    static void Require(bool condition, string message)
    { if (!condition) throw new Exception("SEPTEMBER_TEST_FAILED: " + message); }
    static Component[] Components() => Enumerable.Range(0, SceneManager.sceneCount)
        .SelectMany(i => SceneManager.GetSceneAt(i).GetRootGameObjects())
        .SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
    static LetterCellView[] Cells(PuzzleBoard board) => board.GetComponentsInChildren<LetterCellView>(true)
        .Where(c => c.gameObject.activeSelf && c.Code > 0).ToArray();
    static KeyboardKeyView Key(PuzzleBoard board, char letter) => board.GetComponentsInChildren<KeyboardKeyView>(true).Single(k => k.Letter == letter);

    static void VerifyScenes()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity", OpenSceneMode.Additive);
        var all = Components();
        Require(all.All(c => c != null), "No missing scripts");
        Require(all.OfType<PuzzleBoard>().Count() == 1 && all.OfType<KeyboardKeyView>().Count() == 33, "Only one playable mode and keyboard");
        Require(!all.OfType<Transform>().Any(t => t.name.Contains("Turbo")), "Turbo scene and buttons removed");
        Require(!all.OfType<Text>().Any(t => t.text.Contains("Турбо")), "No Turbo labels remain");
        foreach (var button in all.OfType<Button>())
        {
            Require(button.onClick.GetPersistentEventCount() > 0, "Saved button action: " + button.name);
            for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                Require(button.onClick.GetPersistentTarget(i) != null, "Button target: " + button.name);
        }
        var shell = UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        var board = all.OfType<PuzzleBoard>().Single();
        foreach (var entry in PuzzleGenerator.Build(shell.library)) Require(board.CanFit(entry.text), "Catalogue fits remaining board: " + entry.id);
        foreach (var card in shell.authorCards)
        {
            var viewport = card.transform.Find("PortraitViewport").GetComponent<RectTransform>();
            Require(card.GetComponent<Mask>() != null && viewport.GetComponent<RectMask2D>() != null, "Portrait confined by card and viewport masks");
            Require(viewport.rect.width < ((RectTransform)card.transform).rect.width, "Inset portrait frame");
            Require(viewport.GetComponentsInChildren<Image>().All(i => i.maskable), "Backgrounds obey clipping");
        }
        Require(!shell.defeatQuote.gameObject.activeSelf && !shell.defeatSource.gameObject.activeSelf, "Answer hidden in authored defeat scene");
        Debug.Log("SEPTEMBER_SCENE_PASS catalogue=" + PuzzleGenerator.Build(shell.library).Length + " cells=" + all.OfType<LetterCellView>().Count() + " keys=33");
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || tested) return;
        frames++; game = CryptogramGame.Current;
        if (frames < 30 || game == null || game.classicBoard == null) { if (frames < 6000) return; }
        try
        {
            if (!checkedCore)
            {
                Test(); AdditionalChecks?.Invoke(game, state);
                checkedCore = true; extraFlow = AdditionalFlow?.Invoke(game, state);
            }
            if (EditorApplication.timeSinceStartup < nextStep) return;
            if (extraFlow != null && extraFlow.MoveNext())
            { nextStep = EditorApplication.timeSinceStartup + Convert.ToDouble(extraFlow.Current); return; }
            Debug.Log("SEPTEMBER_RUNTIME_PASS: scattered clues, individual guesses, hints, save/resume, legacy mode, defeat secrecy, retry cost, victory and no UI creation");
        }
        catch (Exception e) { result = 1; Debug.LogException(e); }
        tested = true; EditorApplication.isPlaying = false;
    }

    static void Changed(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode || !tested) return;
        EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= Changed;
        EditorSettings.enterPlayModeOptionsEnabled = oldEnabled; EditorSettings.enterPlayModeOptions = oldOptions;
        SaveStore.Reset(); SaveStore.EditorTestSlot = null;
        AdsBridge.EditorTestInterstitial = null;
        EditorApplication.Exit(result);
    }

    static void Test()
    {
        Require(game != null && game.classicBoard != null, "Additive gameplay scene registered");
        var objectCount = Components().OfType<Transform>().Count();
        state = (GameSave)typeof(CryptogramGame).GetField("save", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
        Shot("Home");
        game.screens[0].GetComponentsInChildren<Button>(true).Single(b => b.name == "Button_Classic").onClick.Invoke();
        var board = game.classicBoard;
        Require(state.feathers == 4 && game.screens[2].activeSelf, "Single feather charged for new game");
        var cells = Cells(board);
        Require(cells.Count(c => !c.IsHiddenLetter) >= 3 && cells.Count(c => !c.IsHiddenLetter) <= cells.Length / 4 + 1, "Sparse but useful starter clues");
        Shot("Classic_Beginner");

        // Repeated letters remain partially hidden, even after a correct guess.
        var probe = new PuzzleEntry { id = "september-repeats", mode = PuzzleMode.Classic, text = "МАМА МЫЛА РАМУ. ПАПА ЧИТАЛ КНИГУ.", source = "Тест" };
        board.StartPuzzle(probe, board.PuzzleIndex, new PuzzleProgress(), 0, state.hints);
        cells = Cells(board);
        var start = board.ExportProgress();
        var opened = cells.Where(c => !c.IsHiddenLetter).ToArray();
        Require(opened.Length == PuzzleGenerator.StartingClues(cells.Length, 0), "Expected clue budget");
        Require(opened.Select(c => c.Answer).Distinct().Count() >= 3, "Varied clue letters");
        Require(cells.GroupBy(c => c.Code).Any(g => g.Any(c => !c.IsHiddenLetter) && g.Any(c => c.IsHiddenLetter)), "Repeated cipher is only partially given");
        var repeated = cells.Where(c => c.IsHiddenLetter).GroupBy(c => c.Code).First(g => g.Count() > 1).ToArray();
        var hidden = cells.Count(c => c.IsHiddenLetter);
        repeated[0].Press(); Key(board, repeated[0].Answer).Press();
        Require(Cells(board).Count(c => c.IsHiddenLetter) == hidden - 1 && repeated.Skip(1).All(c => c.IsHiddenLetter), "Correct input opens exactly one occurrence");
        Require(Key(board, repeated[0].Answer).Found && !Key(board, repeated[0].Answer).Used, "Known key stays available for repeats");
        repeated[1].Press(); board.UseHint();
        Require(Cells(board).Count(c => c.IsHiddenLetter) == hidden - 2 && state.hints == 1, "Hint opens exactly one cell");
        var progress = board.ExportProgress(); SaveStore.Save(state);
        var loaded = SaveStore.Load();
        board.StartPuzzle(probe, board.PuzzleIndex, loaded.activePuzzle, 600, state.hints);
        Require(board.ExportProgress().filledSlots == progress.filledSlots, "Exact opened cells preserved even if difficulty changes");
        board.StartPuzzle(probe, board.PuzzleIndex, new PuzzleProgress(), 0, state.hints);
        Require(board.ExportProgress().filledSlots == start.filledSlots, "Retry uses deterministic initial clues");
        Shot("Partial_Letters");

        foreach (var points in new[] { 0, 60, 160, 320, 600 })
            for (var variant = 0; variant < 12; variant++)
            {
                var puzzle = PuzzleGenerator.Generate(PuzzleMode.Classic, PuzzleGenerator.Tier(points), variant);
                board.StartPuzzle(puzzle, board.PuzzleIndex, new PuzzleProgress(), points, state.hints);
                cells = Cells(board); opened = cells.Where(c => !c.IsHiddenLetter).ToArray();
                Require(opened.Length > 0 && opened.Length <= Math.Ceiling(cells.Length * .22), "Clues leave the phrase mostly hidden");
                var words = board.GetComponentsInChildren<LetterCellView>(true).Where(c => c.gameObject.activeSelf).ToArray();
                // Every authored row consists of complete words; grouping by row
                // and spaces lets us check that no word was completely given away.
                foreach (var row in words.GroupBy(c => c.transform.parent))
                {
                    var current = new List<LetterCellView>();
                    foreach (var cell in row.Concat(new LetterCellView[] { null }))
                    {
                        if (cell != null && cell.Code > 0) { current.Add(cell); continue; }
                        if (current.Count > 0) Require(current.Any(c => c.IsHiddenLetter), "No complete word disclosed at start");
                        current.Clear();
                    }
                }
            }

        // Catalogue indices are unchanged; an old Turbo attempt resumes on Classic.
        var oldIndex = Array.FindIndex(game.Entries, e => e.mode == PuzzleMode.Turbo);
        board.StartPuzzle(game.Entries[oldIndex], oldIndex, new PuzzleProgress { remainingHearts = 2 }, 0, state.hints);
        progress = board.ExportProgress(); var feathers = state.feathers;
        game.Execute(UiActionKind.Home, 0); game.Execute(UiActionKind.Classic, 0);
        Require(board.PuzzleIndex == oldIndex && board.ExportProgress().filledSlots == progress.filledSlots && board.RemainingHearts == 2 && state.feathers == feathers, "Legacy attempt resumes without loss or extra feather");

        state.activePuzzle = new PuzzleProgress(); state.erudition = 0;
        game.Execute(UiActionKind.Classic, 0);
        var index = board.PuzzleIndex; feathers = state.feathers;
        progress = board.ExportProgress();
        game.Execute(UiActionKind.Home, 0); game.Execute(UiActionKind.Classic, 0);
        Require(state.feathers == feathers && board.ExportProgress().filledSlots == progress.filledSlots, "Normal resume is free");
        board.SetRemainingHeartsForDebug(1);
        var target = Cells(board).First(c => c.IsHiddenLetter); target.Press();
        var wrong = board.GetComponentsInChildren<KeyboardKeyView>(true).First(k => !k.Used && k.Letter != target.Answer);
        wrong.Press();
        Require(game.screens[12].activeSelf && state.feathers == feathers, "Defeat has no extra charge");
        Require(string.IsNullOrEmpty(game.defeatQuote.text) && string.IsNullOrEmpty(game.defeatSource.text) && !game.defeatQuote.gameObject.activeSelf, "Unsolved answer and author never shown");
        Require(!game.screens[12].GetComponentsInChildren<Text>().Any(t => t.text.Contains(board.Entry.text)), "No answer elsewhere on defeat screen");
        Shot("Defeat");
        game.Execute(UiActionKind.Retry, 0);
        Require(board.PuzzleIndex == index && board.RemainingHearts == 5 && state.feathers == feathers - 1, "Retry same puzzle costs exactly one feather");
        var guard = 0;
        while (Cells(board).Any(c => c.IsHiddenLetter) && guard++ < 500)
        { target = Cells(board).First(c => c.IsHiddenLetter); target.Press(); Key(board, target.Answer).Press(); }
        Require(game.screens[4].activeSelf && game.victoryQuote.text.Contains(board.Entry.text), "Solved phrase visible on victory");
        Require(state.erudition == 20 && state.coins == 30, "Classic rewards preserved");
        Shot("Victory");
        game.Execute(UiActionKind.Collections, 0); Shot("Collections"); Shot("Collections_16x9", 1080, 1920); Shot("Collections_Notch", 1080, 2400, true);
        var scroll = game.collectionGroups[0].GetComponentInChildren<ScrollRect>(); scroll.verticalNormalizedPosition = 0; Shot("Collections_Scrolled");
        game.Execute(UiActionKind.ToggleSetting, 3); Shot("Collections_LargeText"); game.Execute(UiActionKind.ToggleSetting, 3);
        state.activePuzzle = new PuzzleProgress(); state.feathers = 0; state.nextFeatherUtcTicks = DateTime.UtcNow.AddMinutes(20).Ticks;
        game.Execute(UiActionKind.Retry, 0);
        Require(game.screens[5].activeSelf && state.feathers == 0, "Retry without energy shows restoration screen");
        Require(Components().OfType<Transform>().Count() == objectCount, "Runtime did not create UI objects");
    }

    public static void Shot(string name, int width = 1080, int height = 2340, bool notch = false)
    {
        var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        var target = new RenderTexture(width, height, 24); target.Create(); camera.targetTexture = target;
        camera.orthographic = true; camera.rect = new Rect(0, 0, 1, 1); camera.clearFlags = CameraClearFlags.SolidColor;
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 100; }
        Canvas.ForceUpdateCanvases();
        foreach (var safe in UnityEngine.Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            safe.FitNormalized(notch ? new Rect(0, .028f, 1, .925f) : new Rect(0, 0, 1, 1));
        foreach (var background in UnityEngine.Object.FindObjectsByType<SceneBackdrop>(FindObjectsSortMode.None)) background.Fit();
        Canvas.ForceUpdateCanvases();
        foreach (var text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
        { text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle); text.SetAllDirty(); }
        Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
        var output = Path.Combine(Directory.GetCurrentDirectory(), "Review", ReviewFolder); Directory.CreateDirectory(output);
        File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture); camera.targetTexture = null; RenderTexture.active = null;
        target.Release(); UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("SEPTEMBER_SHOT " + name);
    }
}
