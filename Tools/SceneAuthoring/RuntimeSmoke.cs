using System;
using System.Linq;
using System.Reflection;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RuntimeSmoke
{
    private static int frames;
    private static int result;
    private static bool tested;
    private static bool previousOptionsEnabled;
    private static EnterPlayModeOptions previousOptions;
    private static bool captureAfter;

    public static void RunWithCapture()
    {
        captureAfter = true;
        Run();
    }

    public static void Run()
    {
        FinishGameProject.Verify();
        SaveStore.EditorTestSlot = "erudition.runtime-smoke.temporary";
        SaveStore.Reset();
        previousOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        previousOptions = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.playModeStateChanged += OnState;
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    private static void OnState(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode || !tested) return;
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnState;
        EditorSettings.enterPlayModeOptionsEnabled = previousOptionsEnabled;
        EditorSettings.enterPlayModeOptions = previousOptions;
        SaveStore.Reset();
        SaveStore.EditorTestSlot = null;
        if (captureAfter && result == 0)
        {
            try { ProjectReview.Capture(); }
            catch (Exception exception) { result = 1; Debug.LogException(exception); }
        }
        EditorApplication.Exit(result);
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || tested || ++frames < 20) return;
        tested = true;
        try { Test(); Debug.Log("RUNTIME_SMOKE_PASS: start, cipher, mistakes, duplicate guess, hint, resume, victory, reward, defeat, retry, energy, shop, persistence"); }
        catch (Exception exception) { result = 1; Debug.LogException(exception); }
        EditorApplication.isPlaying = false;
    }

    private static void Test()
    {
        var game = UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        var state = (GameSave)typeof(CryptogramGame).GetField("save", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(game);
        var objectCount = game.gameObject.scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length);
        Require(game.screens[0].activeSelf && state.feathers == 5, "Initial state");
        game.screens[0].GetComponentsInChildren<Button>(true).Single(button => button.name == "Button_Classic").onClick.Invoke();
        var board = game.classicBoard;
        Require(state.feathers == 4 && board.RemainingHearts == 5 && game.screens[2].activeSelf, "Classic costs one feather");
        var cells = board.GetComponentsInChildren<LetterCellView>(true);
        foreach (var group in cells.Where(cell => cell.gameObject.activeSelf && cell.Code > 0).GroupBy(cell => cell.Code))
            Require(group.Select(cell => cell.Answer).Distinct().Count() == 1, "Cipher maps one code to one letter");
        var selected = cells.First(cell => cell.gameObject.activeSelf && cell.IsHiddenLetter);
        selected.Press();
        var wrong = selected.Answer == 'А' ? 'Б' : 'А';
        board.Guess(wrong, Key(board, wrong));
        Require(board.RemainingHearts == 4, "Wrong letter loses heart");
        board.Guess(wrong, Key(board, wrong));
        Require(board.RemainingHearts == 4, "Repeated wrong letter does not lose another heart");
        var hintsBefore = state.hints;
        board.UseHint();
        Require(state.hints == hintsBefore - 1 && !selected.IsHiddenLetter, "Hint reveals selected cipher");
        var index = board.PuzzleIndex;
        game.Execute(UiActionKind.Home, 0);
        game.Execute(UiActionKind.Classic, 0);
        Require(state.feathers == 4 && board.PuzzleIndex == index && board.RemainingHearts == 4, "Resume preserves state without charging");
        var guard = 0;
        while (game.screens[2].activeSelf && guard++ < 40)
        {
            selected = cells.First(cell => cell.gameObject.activeSelf && cell.IsHiddenLetter);
            selected.Press();
            board.Guess(selected.Answer, Key(board, selected.Answer));
        }
        Require(game.screens[4].activeSelf && state.solved == 1 && state.erudition == 121, "Solved puzzle reaches victory and grants reward");
        game.Execute(UiActionKind.RewardVictory, 0);
        game.Execute(UiActionKind.RewardVictory, 0);
        Require(state.erudition == 131 && state.hints == hintsBefore, "Rewarded bonus is granted only once");
        game.Execute(UiActionKind.Turbo, 0);
        Require(state.feathers == 3 && game.turboBoard.RemainingHearts == 3, "Turbo starts with three hearts");
        board = game.turboBoard;
        selected = board.GetComponentsInChildren<LetterCellView>(true).First(cell => cell.gameObject.activeSelf && cell.IsHiddenLetter);
        selected.Press();
        foreach (var letter in "АБВГДЕ".Where(letter => letter != selected.Answer).Take(3)) board.Guess(letter, Key(board, letter));
        Require(game.screens[12].activeSelf && state.activePuzzle.puzzleIndex == -1, "Three mistakes lead to defeat");
        game.Execute(UiActionKind.Retry, 0);
        Require(game.screens[3].activeSelf && state.feathers == 2 && board.RemainingHearts == 3, "Retry starts fresh and costs one feather");
        game.Execute(UiActionKind.DebugZeroFeathers, 0);
        game.Execute(UiActionKind.Classic, 0);
        Require(game.screens[5].activeSelf && state.feathers == 0, "Zero energy blocks a new level");
        game.Execute(UiActionKind.RewardFeather, 0);
        Require(state.feathers == 1, "Optional reward restores one feather");
        var coins = state.coins;
        game.Execute(UiActionKind.BuyFifteenFeathers, 0);
        Require(state.feathers == 5 && state.reserveFeathers == 11 && state.coins == coins - 250, "Full purchased bundle is retained");
        state.feathers = 0;
        state.reserveFeathers = 0;
        state.nextFeatherUtcTicks = DateTime.UtcNow.AddMinutes(-41).Ticks;
        typeof(CryptogramGame).GetMethod("RestoreEnergy", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(game, null);
        Require(state.feathers == 3, "Offline energy recovery follows twenty-minute intervals");
        SaveStore.Save(state);
        var loaded = SaveStore.Load();
        Require(loaded.solved == 1 && loaded.feathers == 3 && loaded.erudition == 131 && loaded.guesses == state.guesses, "Save round-trip");
        state.erudition = 500;
        state.classicSolved = 3;
        var reviewIndex = (int)typeof(CryptogramGame).GetMethod("ChoosePuzzle", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(game, new object[] { PuzzleMode.Classic, game.classicBoard });
        Require(game.library.entries[reviewIndex].authorIndex >= 0, "Author collections remain attainable at high erudition");
        game.Execute(UiActionKind.CollectionDetails, 0);
        Require(game.screens[1].activeSelf && game.screens[1].name == "Screen_CollectionDetails", "Collection details replace redundant mode screen");
        game.Execute(UiActionKind.Back, 0);
        Require(game.screens[7].activeSelf, "Details back returns to collections");
        game.Execute(UiActionKind.Statistics, 0);
        game.Execute(UiActionKind.StatisticsPeriod, 2);
        Require(game.presentation.bars.Count(bar => bar.gameObject.activeSelf) == 12, "Year graph uses twelve authored bars");
        game.Execute(UiActionKind.StatisticsPeriod, 0);
        Require(game.presentation.bars.Count(bar => bar.gameObject.activeSelf) == 7, "Week graph uses seven authored bars");
        Require(state.activityHistory.Sum(day => day.solved) == 1, "Activity history records actual victory");
        game.Execute(UiActionKind.LikeQuote, 0);
        game.Execute(UiActionKind.LikeQuote, 0);
        Require(state.likedPuzzleIds.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Length == 1, "Quote like is unique");
        game.Execute(UiActionKind.AchievementDetails, 0);
        Require(game.presentation.popup.activeSelf, "Achievement details opens saved popup");
        game.Execute(UiActionKind.ClosePopup, 0);
        Require(!game.presentation.popup.activeSelf, "Achievement popup closes");
        Require(game.gameObject.scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length) == objectCount,
            "UI objects remain pre-authored in the scene during the entire game loop");
    }

    private static KeyboardKeyView Key(PuzzleBoard board, char value)
        => board.GetComponentsInChildren<KeyboardKeyView>(true).Single(key => key.name == "Key_" + value);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("Runtime check failed: " + message);
    }
}
