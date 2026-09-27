using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class VarietyRegression
{
    static bool done, oldEnabled;
    static int result, frames;
    static EnterPlayModeOptions oldOptions;
    static IEnumerator review;
    static double nextReview;
    static void Require(bool value, string message) { if (!value) throw new Exception("VARIETY_FAILED: " + message); }
    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            var library = AssetDatabase.LoadAssetAtPath<PuzzleLibrary>("Assets/Data/PuzzleLibrary.asset");
            var entries = PuzzleGenerator.Build(library);
            var random = new System.Random(77129);
            Require(entries.Length == 5364 && entries.Select(e => e.id).Distinct().Count() == entries.Length, "Stable catalogue IDs plus sixty new texts");
            Require(VarietyCatalog.Build().Select(e => e.text).Distinct().Count() == 60, "Sixty distinct authored texts");
            for (var tier = 0; tier < 5; tier++)
            {
                var seenTiers = new HashSet<int>(); var kinds = new HashSet<PuzzleKind>(); var recent = new List<string>();
                for (var n = 0; n < 250; n++)
                {
                    var i = PuzzleSelection.Choose(entries, PuzzleMode.Classic, PuzzleGenerator.Thresholds[tier], recent, s => true, random.Next);
                    Require(i >= 0 && entries[i].minimumErudition <= PuzzleGenerator.Thresholds[tier], "Only unlocked entries");
                    var key = PuzzleGenerator.TextKey(entries[i].text);
                    Require(!recent.Contains(key), "No text repeated within last ten starts");
                    recent.Add(key); if (recent.Count > 10) recent.RemoveAt(0);
                    seenTiers.Add(PuzzleGenerator.Tier(entries[i].minimumErudition)); kinds.Add(entries[i].kind);
                }
                Require(seenTiers.Count == tier + 1 && kinds.Count == 4, "Every unlocked difficulty and type sampled");
            }
            var tiny = new[] { new PuzzleEntry { text = "Первый" }, new PuzzleEntry { text = "Второй" } };
            Require(PuzzleSelection.Choose(tiny, PuzzleMode.Classic, 0, new[] { "ПЕРВЫЙ", "ВТОРОЙ" }, s => true, random.Next) == 0, "Exhausted pool relaxes oldest exclusion first");
            Require(PuzzleSelection.Choose(tiny, PuzzleMode.Classic, 0, new string[0], s => false, random.Next) == -1, "Unfittable texts rejected");
            var progress = new PuzzleProgress { scoringRevision = 1, initialHiddenLetters = 30, activeSeconds = 60 };
            Require(PuzzleReward.Calculate(0, progress).points == 10, "Perfect easy reward is ten");
            progress.playerTierAtStart = 1;
            Require(PuzzleReward.Calculate(0, progress).points == 5, "Easy at medium is halved");
            Require(PuzzleReward.Calculate(60, progress).points == 15, "Perfect medium reward is fifteen");
            progress.playerTierAtStart = 0; progress.hintsUsed = 3; progress.mistakesInLevel = 3; progress.activeSeconds = 1000;
            Require(PuzzleReward.Calculate(0, progress).points == 5, "Easy minimum is five");
            for (var player = 0; player < 5; player++)
                for (var puzzle = 0; puzzle <= player; puzzle++)
                    for (var hints = 0; hints < 4; hints++)
                        for (var errors = 0; errors < 5; errors++)
                        {
                            progress.playerTierAtStart = player; progress.hintsUsed = hints; progress.mistakesInLevel = errors;
                            var reward = PuzzleReward.Calculate(PuzzleGenerator.Thresholds[puzzle], progress);
                            Require(reward.points > 0 && reward.points <= 30, "Bounded positive rewards");
                        }
            var legacy = new XmlSerializer(typeof(GameSave)).Deserialize(new StringReader("<GameSave><version>3</version><erudition>90</erudition><authorProgress><int>7</int><int>0</int><int>0</int><int>0</int><int>0</int><int>0</int></authorProgress></GameSave>")) as GameSave;
            legacy.Normalize(); Require(legacy.erudition == 90 && legacy.authorProgress[0] == 7 && legacy.kindProgress.Length == 4, "Old saves retain earned progress");
            Debug.Log("VARIETY_LOGIC_PASS: 1250 random draws, exhaustion, fit rejection, reward matrix, legacy XML");
            SaveStore.EditorTestSlot = "erudition.variety.temporary"; SaveStore.Reset();
            oldEnabled = EditorSettings.enterPlayModeOptionsEnabled; oldOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true; EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged += Changed; EditorApplication.update += Tick; EditorApplication.isPlaying = true;
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || done) return;
        EditorApplication.QueuePlayerLoopUpdate();
        if (review != null)
        {
            if (EditorApplication.timeSinceStartup < nextReview) return;
            try
            {
                if (review.MoveNext()) { nextReview = EditorApplication.timeSinceStartup + 1; return; }
            }
            catch (Exception e) { result = 1; Debug.LogException(e); }
            done = true; EditorApplication.isPlaying = false; return;
        }
        var game = CryptogramGame.Current;
        if (++frames < 30 || game == null || game.classicBoard == null) { if (frames < 6000) return; }
        try
        {
            var state = (GameSave)typeof(CryptogramGame).GetField("save", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(game);
            var board = game.classicBoard;
            Require(game.kindCards.Length == 4 && game.collectionGroups.Length == 4 && game.presentation.collections.Length == 16, "Scene cards wired");
            foreach (var entry in VarietyCatalog.Build()) Require(board.CanFit(entry.text), "New text fits: " + entry.id);
            state.erudition = 60; state.hints = 5;
            state.lastFailedPuzzleIndex = Array.FindIndex(game.Entries, e => e.id == "variety_v1_2_0");
            game.Execute(UiActionKind.Retry, 0);
            var feathers = state.feathers;
            board.TickPlayTime(12); board.UseHint();
            var snapshot = board.ExportProgress();
            Require(snapshot.hintsUsed == 1 && snapshot.playerTierAtStart == 1 && snapshot.activeSeconds >= 12, "Hint and active time tracked");
            game.Execute(UiActionKind.Home, 0); game.Execute(UiActionKind.Classic, 0);
            Require(state.feathers == feathers && board.ExportProgress().hintsUsed == 1 && board.ExportProgress().activeSeconds >= 12, "Free resume retains scoring");
            var loaded = SaveStore.Load(); Require(loaded.activePuzzle.hintsUsed == 1 && loaded.activePuzzle.initialHiddenLetters == snapshot.initialHiddenLetters, "Scoring survives XML save/load");
            SeptemberFeedbackTests.ReviewFolder = "VarietyReview";
            SeptemberFeedbackTests.Shot("Gameplay_Riddle");
            var before = state.erudition;
            var expected = PuzzleReward.Calculate(board.Entry.minimumErudition, board.ExportProgress()).points;
            var safety = 0;
            while (game.screens[2].activeSelf && safety++ < 480)
            {
                var cell = board.GetComponentsInChildren<LetterCellView>().FirstOrDefault(c => c.IsHiddenLetter);
                if (cell == null) break; cell.Press(); board.Guess(cell.Answer, null);
            }
            Require(game.screens[4].activeSelf && state.erudition == before + expected && state.kindProgress[3] == 1, "Actual victory rewards correct difficulty and collection");
            Require(game.victorySource.text.Contains("Улитка") && game.victoryRewardDetails.text.Contains("×0"), "Answer and reduction explained after victory");
            game.CompletePuzzle(board); Require(state.erudition == before + expected, "Reward cannot be collected twice");
            SeptemberFeedbackTests.Shot("Victory_Scoring");
            game.Execute(UiActionKind.Collections, 0); game.Execute(UiActionKind.CollectionTab, 3);
            SeptemberFeedbackTests.Shot("TextKinds");
            SeptemberFeedbackTests.Shot("TextKinds_16x9", 1080, 1920);
            game.Execute(UiActionKind.ToggleSetting, 3); SeptemberFeedbackTests.Shot("TextKinds_LargeText", 1080, 2400, true);
            game.Execute(UiActionKind.ToggleSetting, 3);
            foreach (var card in game.kindCards)
                Require(card.GetComponentsInChildren<UiAction>(true).Any(a => new SerializedObject(a).FindProperty("parameter").intValue >= 12), "Detail arrow wired");
            game.Execute(UiActionKind.CollectionDetails, 15);
            Require(game.presentation.detailBody.text.Contains("Улитка"), "Solved riddle in collection");
            SeptemberFeedbackTests.Shot("RiddleCollection");
            Debug.Log("VARIETY_RUNTIME_PASS: 60 texts fit, resume, persisted scoring, real solution, reward guard, cards and detail navigation");
        }
        catch (Exception e) { result = 1; Debug.LogException(e); }
        if (result == 0) { review = Review(game); return; }
        done = true; EditorApplication.isPlaying = false;
    }
    static IEnumerator Review(CryptogramGame game)
    {
        var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        camera.orthographic = true;
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 100; }
        game.Execute(UiActionKind.Collections, 0); game.Execute(UiActionKind.CollectionTab, 3);
        yield return null; SeptemberFeedbackTests.Shot("TextKinds");
        yield return null; SeptemberFeedbackTests.Shot("TextKinds_16x9", 1080, 1920);
        game.Execute(UiActionKind.ToggleSetting, 3);
        yield return null; SeptemberFeedbackTests.Shot("TextKinds_LargeText", 1080, 2400, true);
        game.Execute(UiActionKind.ToggleSetting, 3); game.Execute(UiActionKind.CollectionDetails, 15);
        yield return null; SeptemberFeedbackTests.Shot("RiddleCollection");
        typeof(CryptogramGame).GetMethod("ShowScreen", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(game, new object[] { 4 });
        yield return null; SeptemberFeedbackTests.Shot("Victory_Scoring");
        game.Execute(UiActionKind.Classic, 0);
        yield return null; SeptemberFeedbackTests.Shot("Gameplay_Riddle");
    }
    static void Changed(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode || !done) return;
        EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= Changed;
        EditorSettings.enterPlayModeOptionsEnabled = oldEnabled; EditorSettings.enterPlayModeOptions = oldOptions;
        SaveStore.Reset(); SaveStore.EditorTestSlot = null; EditorApplication.Exit(result);
    }
}
