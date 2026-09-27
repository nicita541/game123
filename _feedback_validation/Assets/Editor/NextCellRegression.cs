using System;
using System.Collections;
using System.Linq;
using Erudition;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class NextCellRegression
{
    static void Check(bool condition, string message)
    { if (!condition) throw new Exception("NEXT_CELL_FAILED: " + message); }
    static LetterCellView[] Cells(PuzzleBoard board) => board.GetComponentsInChildren<LetterCellView>(true)
        .Where(c => c.gameObject.activeSelf && c.Code > 0).ToArray();
    static void NewPuzzle(CryptogramGame game, GameSave save)
    { save.activePuzzle = new PuzzleProgress(); save.feathers = 90; game.Execute(UiActionKind.Classic, 0); }
    public static void Run()
    {
        SeptemberFeedbackTests.ReviewFolder = "NextCellReview";
        SeptemberFeedbackTests.AdditionalChecks = Test;
        SeptemberFeedbackTests.AdditionalFlow = Ads;
        SeptemberFeedbackTests.Run();
    }
    static void Test(CryptogramGame game, GameSave save)
    {
        save.erudition = 600; save.hints = 10; NewPuzzle(game, save);
        var board = game.classicBoard;
        var cells = Cells(board);
        Check(cells.Count(c => !c.IsHiddenLetter) <= 3, "Long advanced text has at most three starter cells");
        Check(board.GetComponentsInChildren<KeyboardKeyView>(true).Count(k => k.Found) <= 3, "Long text does not reveal half the keyboard");
        var hidden = cells.Where(c => c.IsHiddenLetter).ToArray();
        var chosen = hidden[hidden.Length * 2 / 3];
        var expected = hidden[hidden.Length * 2 / 3 + 1];
        chosen.Press(); var scroll = board.GetComponentInChildren<ScrollRect>();
        Check(scroll.content.anchoredPosition.y > 0, "Lower row actually scrolled into view");
        board.Guess(chosen.Answer, null);
        Check(expected.PrimarySelected && hidden[0].IsHiddenLetter && !hidden[0].PrimarySelected, "Answer advances from current cell, not first gap");
        Check(scroll.content.anchoredPosition.y > 0, "Answer does not jump to top");
        var next = cells.SkipWhile(c => c != expected).Skip(1).First(c => c.IsHiddenLetter);
        board.UseHint(); Check(next.PrimarySelected, "Hint advances to the next hidden cell");
        SeptemberFeedbackTests.Shot("Long_NextCell");
        var last = Cells(board).Last(c => c.IsHiddenLetter); last.Press(); board.Guess(last.Answer, null);
        Check(hidden[0].PrimarySelected, "Wrap only after the final hidden cell");
        var progress = board.ExportProgress(); var featherBalance = save.feathers;
        game.Execute(UiActionKind.Home, 0); game.Execute(UiActionKind.Classic, 0);
        Check(progress.filledSlots == board.ExportProgress().filledSlots && progress.selectedSlot == board.ExportProgress().selectedSlot && save.feathers == featherBalance, "New selection survives free resume");
        foreach (var tier in new[] { 0, 60, 160, 320, 600 })
            foreach (var length in new[] { 2, 15, 30, 100, 300, 480 })
                Check(PuzzleGenerator.StartingClues(length, tier) <= (tier >= 560 ? 3 : tier >= 160 ? 4 : 5), "Starter budget capped regardless of text length");
        Check(!game.screens[4].GetComponentsInChildren<UiAction>(true).Any(a => new SerializedObject(a).FindProperty("action").intValue == (int)UiActionKind.RewardVictory), "No victory bonus ad control");
        var coins = save.coins; var points = save.erudition; var hints = save.hints;
        AdsBridge.EditorTestRewards = true; game.Execute(UiActionKind.RewardVictory, 0);
        Check(save.coins == coins && save.erudition == points && save.hints == hints, "Retired action cannot grant bonus");
        AdsBridge.EditorTestRewards = false;
        Check(game.soundPlayer.musicSource.clip.length > 180, "Full pianist performance assigned");
        foreach (var clip in new[] { game.soundPlayer.click, game.soundPlayer.correct, game.soundPlayer.mistake, game.soundPlayer.victory })
            Check(clip != null && clip.length > .1f && clip.samples > 8000, "Recorded sound effect assigned");
        game.Execute(UiActionKind.Settings, 0); SeptemberFeedbackTests.Shot("Settings_AudioCredits");
        game.Execute(UiActionKind.Home, 0); SeptemberFeedbackTests.Shot("Home_16x9", 1080, 1920); SeptemberFeedbackTests.Shot("Home_Notch", 1080, 2400, true);
        Debug.Log("NEXT_CELL_AND_AUDIO_PASS: forward selection, wrap, resume, bounded clues, no reward button, human recordings");
    }
    static IEnumerator Ads(CryptogramGame game, GameSave save)
    {
        var shown = 0; var ready = true;
        AdsBridge.EditorTestInterstitial = () => { if (!ready) return false; shown++; return true; };
        save.winsUntilInterstitial = 1; save.lastInterstitialUtcTicks = 0;
        NewPuzzle(game, save); game.CompletePuzzle(game.classicBoard);
        var points = save.erudition; var coins = save.coins; var hints = save.hints;
        yield return 1.15;
        Check(shown == 1, "Automatic interstitial shown on due victory");
        Check(save.winsUntilInterstitial >= 2 && save.winsUntilInterstitial <= 4, "Random interval is two to four wins");
        Check(save.erudition == points && save.coins == coins && save.hints == hints, "Interstitial never changes rewards");
        var loaded = SaveStore.Load();
        Check(loaded.winsUntilInterstitial == save.winsUntilInterstitial && loaded.lastInterstitialUtcTicks == save.lastInterstitialUtcTicks, "Ad interval persists");
        game.CompletePuzzle(game.classicBoard); yield return 1;
        Check(shown == 1, "Duplicate completion cannot show another ad");
        save.winsUntilInterstitial = 1;
        NewPuzzle(game, save); game.CompletePuzzle(game.classicBoard); yield return 1.15;
        Check(shown == 1, "Ninety-second cooldown respected");
        save.lastInterstitialUtcTicks = 0; save.winsUntilInterstitial = 1; ready = false;
        NewPuzzle(game, save); game.CompletePuzzle(game.classicBoard); yield return 1.15;
        Check(shown == 1 && game.screens[4].activeSelf, "Unavailable ad never blocks victory");
        ready = true; yield return 1;
        Check(shown == 1, "No late ad pops up after a load");
        save.winsUntilInterstitial = 1;
        NewPuzzle(game, save); game.CompletePuzzle(game.classicBoard); game.Execute(UiActionKind.Home, 0); yield return 1.15;
        Check(shown == 1 && game.screens[0].activeSelf, "Navigation cancels deferred show");
        save.winsUntilInterstitial = 1;
        NewPuzzle(game, save); game.CompletePuzzle(game.classicBoard); NewPuzzle(game, save); yield return 1.15;
        Check(shown == 1 && game.screens[2].activeSelf, "Ad never interrupts new gameplay");
        save.winsUntilInterstitial = 1;
        game.CompletePuzzle(game.classicBoard); yield return 1.15;
        Check(shown == 2, "Next eligible victory can show an ad");
        SeptemberFeedbackTests.Shot("Victory_NoBonus");
        AdsBridge.EditorTestInterstitial = null;
        Debug.Log("INTERSTITIAL_POLICY_PASS: delayed auto-show, random interval, cooldown, persistence, duplicate guard, offline fallback, navigation cancellation, unchanged rewards");
    }
}
