using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class ContentCatalogRegression
{
    private static bool finished, oldEnabled;
    private static int frames, result;
    private static EnterPlayModeOptions oldOptions;
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Check(bool ok, string message)
    { if (!ok) throw new Exception("CONTENT_TEST_FAILED: " + message); Debug.Log("CONTENT_OK: " + message); }

    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            var game = UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
            Check(game.GetComponentsInChildren<CollectionCardView>(true).Length == 0, "Gallery template is in UI, not systems");
            Check(UnityEngine.Object.FindObjectsByType<CollectionCardView>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1, "Exactly one card is authored in scene");
            Check(game.gallery.template.gameObject.activeSelf && game.gallery.catalog.cards.Length == 17, "Visible template and seventeen definitions");
            Check(game.hintOffer != null && !game.hintOffer.activeSelf, "Hint offer serialized and initially closed");
            EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity", OpenSceneMode.Additive);
            var board = UnityEngine.Object.FindFirstObjectByType<PuzzleBoard>(FindObjectsInactive.Include);
            Check(new SerializedObject(board).FindProperty("hearts").arraySize == 3, "Three authored heart objects");
            var entries = PuzzleGenerator.Build(game.library, game.gallery.catalog);
            Check(entries.Select(e => e.id).Distinct().Count() == entries.Length, "Unique puzzle IDs");
            Check(entries.All(e => board.CanFit(e.text)), "Entire catalogue fits game grid");
            Check(entries.Length == PuzzleGenerator.Build(game.library).Length + 3, "Three example phrases appended without moving old entries");
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            SaveStore.EditorTestSlot = "erudition.content-catalog.temporary";
            SaveStore.Reset();
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

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || finished) return;
        var game = CryptogramGame.Current;
        if (++frames < 40 || game == null || game.classicBoard == null)
        { if (frames < 6000) return; }
        try { Test(game); Debug.Log("CONTENT_RUNTIME_PASS"); }
        catch (Exception e) { result = 1; Debug.LogException(e); }
        finished = true;
        EditorApplication.isPlaying = false;
    }

    private static void Changed(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode || !finished) return;
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= Changed;
        EditorSettings.enterPlayModeOptionsEnabled = oldEnabled;
        EditorSettings.enterPlayModeOptions = oldOptions;
        SaveStore.Reset(); SaveStore.EditorTestSlot = null;
        AdsBridge.EditorTestRewards = false; AdsBridge.EditorTestInterstitial = null;
        EditorApplication.Exit(result);
    }

    private static LetterCellView[] Cells(PuzzleBoard board) => board.GetComponentsInChildren<LetterCellView>(true)
        .Where(c => c.gameObject.activeSelf && c.Code > 0).ToArray();
    private static void NewPuzzle(CryptogramGame game, GameSave save)
    { save.activePuzzle = new PuzzleProgress(); save.feathers = 30; game.Execute(UiActionKind.Classic, 0); }

    private static void Shot(string name, int width = 1080, int height = 2340, bool checkHintInput = false)
    {
        var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        var target = new RenderTexture(width, height, 24); target.Create(); camera.targetTexture = target;
        camera.orthographic = true; camera.rect = new Rect(0, 0, 1, 1); camera.clearFlags = CameraClearFlags.SolidColor;
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas))
        { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 100; }
        Canvas.ForceUpdateCanvases();
        foreach (var safe in UnityEngine.Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            safe.FitNormalized(new Rect(0, 0, 1, 1));
        foreach (var background in UnityEngine.Object.FindObjectsByType<SceneBackdrop>(FindObjectsSortMode.None)) background.Fit();
        Canvas.ForceUpdateCanvases();
        foreach (var text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
        { text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle); text.SetAllDirty(); }
        Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
        if (checkHintInput)
        {
            var button = CryptogramGame.Current.hintOfferBuy;
            var hit = new List<RaycastResult>();
            var eventData = new PointerEventData(EventSystem.current) {
                position = RectTransformUtility.WorldToScreenPoint(camera, button.transform.position)
            };
            EventSystem.current.RaycastAll(eventData, hit);
            Check(hit.Count > 0 && hit[0].gameObject.GetComponentInParent<Button>() == button,
                "Hint purchase receives rendered pointer input: " + string.Join(",", hit.Select(h => h.gameObject.name)));
        }
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
        var output = Path.Combine(Directory.GetCurrentDirectory(), "Review", "ContentCatalogReview"); Directory.CreateDirectory(output);
        File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture); camera.targetTexture = null; RenderTexture.active = null;
        target.Release(); UnityEngine.Object.DestroyImmediate(target);
    }

    private static void Test(CryptogramGame game)
    {
        UnityEngine.Random.InitState(281026);
        var save = (GameSave)typeof(CryptogramGame).GetField("save", Private).GetValue(game);
        // The authored main camera is parented to the overlay canvas. Detach it
        // only for offscreen rendering so switching to camera mode is not circular.
        var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        camera.transform.SetParent(null);
        camera.transform.position = new Vector3(0, 0, -10);
        camera.transform.rotation = Quaternion.identity;
        var gallery = game.gallery;
        Check(gallery.Cards.Length == 17 && !gallery.template.gameObject.activeSelf, "All cards generated from hidden template");
        game.Execute(UiActionKind.Collections, 0);
        Canvas.ForceUpdateCanvases();
        Check(gallery.containers[0].rect.height > 1600, "Grid content grows for six author cards");
        Check(gallery.Cards[0].transform.localPosition != gallery.Cards[1].transform.localPosition, "Cards laid out into separate slots");
        Shot("Collections", 1080, 2340);
        game.Execute(UiActionKind.CollectionTab, 1);
        Shot("Themes_WithNewCard", 1080, 1920);
        for (var i = 0; i < gallery.Cards.Length; i++)
        {
            gallery.Cards[i].GetComponentInChildren<Button>(true).onClick.Invoke();
            Check(game.presentation.detailTitle.text == gallery.catalog.cards[i].title, "Card button opens definition " + i);
            Check(game.presentation.detailPicture.sprite == gallery.catalog.cards[i].picture, "Correct detail picture " + i);
        }
        var sample = gallery.catalog.cards.Last();
        var sampleIndex = Array.FindIndex(game.Entries, e => e.collectionId == sample.id);
        var duplicate = new PuzzleEntry { id = "same-text-other-id", text = sample.phrases[0].text, authorIndex = -1 };
        Check(sample.Contains(duplicate), "Matching text solved through another collection is counted");
        var legacy = new CollectionDefinition { id = "legacy-probe", includeLegacy = true,
            legacyGroup = CollectionGroup.Authors, legacyIndex = 0, group = CollectionGroup.Books };
        Check(legacy.Contains(new PuzzleEntry { id = "legacy-quote", authorIndex = 0, text = "Тест" }),
            "Moving legacy card to another tab retains membership");
        Check(PuzzleSelection.Choose(new[] { game.Entries[sampleIndex] }, PuzzleMode.Classic, 0,
            new List<string>(), game.classicBoard.CanFit, n => 0) == 0, "Added phrase enters selection");
        save.lastFailedPuzzleIndex = sampleIndex;
        game.Execute(UiActionKind.Retry, 0);
        game.CompletePuzzle(game.classicBoard);
        game.Execute(UiActionKind.CollectionDetails, gallery.Cards.Length - 1);
        Check(game.presentation.detailBody.text.Contains(game.Entries[sampleIndex].text), "Solved new phrase enters collection");
        Check(gallery.Cards.Last().progressText.text == "1/3", "New collection tracks progress without fixed arrays");
        Shot("NewCollection_Details");

        var board = game.classicBoard;
        var legacyProgress = new PuzzleProgress { remainingHearts = 3, mistakesInLevel = 2 };
        board.StartPuzzle(game.Entries[sampleIndex], sampleIndex, legacyProgress, 0, save.hints);
        Check(board.RemainingHearts == 1, "Old five-heart attempt accounts for previous mistakes");
        NewPuzzle(game, save);
        Check(board.RemainingHearts == 3, "New puzzle starts with three hearts");
        var slot = Cells(board).First(c => c.IsHiddenLetter); slot.Press();
        var wrong = board.GetComponentsInChildren<KeyboardKeyView>(true)
            .Where(k => !k.Used && k.Letter != slot.Answer).Select(k => k.Letter).Take(3).ToArray();
        var ads = 0;
        AdsBridge.EditorTestInterstitial = () => { ads++; return true; };
        save.winsUntilInterstitial = 4; save.lastInterstitialUtcTicks = DateTime.UtcNow.Ticks;
        board.Guess(wrong[0], null); board.Guess(wrong[0], null);
        Check(board.RemainingHearts == 2, "Repeated same wrong guess costs only one heart");
        board.Guess(wrong[1], null);
        Check(board.RemainingHearts == 1 && game.screens[2].activeSelf, "Two mistakes leave one heart");
        board.Guess(wrong[2], null);
        Check(board.RemainingHearts == 0 && game.screens[12].activeSelf && ads == 1, "Third mistake loses and requests ad despite cooldown");
        game.FailPuzzle(board);
        Check(ads == 1, "Duplicate defeat does not request another ad");
        game.Execute(UiActionKind.Retry, 0);
        Check(board.RemainingHearts == 3, "Retry restores three hearts");
        game.FailPuzzle(board);
        Check(ads == 2, "Consecutive defeat requests ad again");
        AdsBridge.EditorTestInterstitial = () => { ads++; return false; };
        game.Execute(UiActionKind.Retry, 0); game.FailPuzzle(board);
        Check(ads == 3 && game.screens[12].activeSelf, "Offline ad request leaves defeat usable");

        NewPuzzle(game, save); save.hints = 0; board.SetHintCount(0); save.coins = 149;
        var index = board.PuzzleIndex; var feathers = save.feathers;
        var before = board.ExportProgress().filledSlots;
        game.Execute(UiActionKind.Hint, 0);
        Check(game.hintOffer.activeSelf, "Empty hint opens offer over same puzzle");
        Check(game.hintOffer.activeInHierarchy, "Hint modal is active in hierarchy");
        Check(game.hintOffer.GetComponent<Canvas>().overrideSorting && game.hintOffer.GetComponent<Canvas>().sortingOrder > 10,
            "Hint modal sorts above gameplay canvas: " + game.hintOffer.GetComponent<Canvas>().sortingOrder);
        Shot("HintOffer_ClickCheck", 1080, 2340, true);
        game.hintOfferBuy.onClick.Invoke();
        Check(save.coins == 149 && save.hints == 0 && game.hintOffer.activeSelf, "Insufficient coins spend nothing");
        AdsBridge.EditorTestRewards = false;
        game.hintOfferAd.onClick.Invoke();
        Check(save.hints == 0 && board.ExportProgress().filledSlots == before && game.hintOffer.activeSelf, "Failed video reveals nothing");
        Shot("HintOffer_Unavailable");
        game.hintOfferClose.onClick.Invoke();
        Check(!game.hintOffer.activeSelf && board.PuzzleIndex == index && save.feathers == feathers, "Close returns to unchanged level for free");
        save.coins = 150; game.Execute(UiActionKind.Hint, 0);
        Shot("HintOffer", 1080, 1920);
        var hidden = Cells(board).Count(c => c.IsHiddenLetter);
        game.hintOfferBuy.onClick.Invoke(); game.hintOfferBuy.onClick.Invoke();
        Check(save.coins == 0 && save.hints == 4 && Cells(board).Count(c => c.IsHiddenLetter) == hidden - 1, "Purchase charges once and applies one of five hints");
        Check(board.PuzzleIndex == index && save.feathers == feathers && !game.hintOffer.activeSelf, "Purchase stays on same puzzle");
        save.hints = 0; game.Execute(UiActionKind.Hint, 0);
        AdsBridge.EditorTestRewards = true;
        hidden = Cells(board).Count(c => c.IsHiddenLetter);
        game.hintOfferAd.onClick.Invoke(); game.hintOfferAd.onClick.Invoke();
        Check(save.hints == 0 && Cells(board).Count(c => c.IsHiddenLetter) == hidden - 1, "Rewarded video applies exactly one hint");
        Check(!game.hintOffer.activeSelf && save.feathers == feathers, "Video returns to same puzzle for free");
        Shot("Gameplay_ThreeHearts");
        var saved = board.ExportProgress(); SaveStore.Save(save);
        var loaded = SaveStore.Load();
        Check(loaded.activePuzzle.puzzleId == board.Entry.id && loaded.activePuzzle.filledSlots == saved.filledSlots, "Stable puzzle ID and revealed cells persisted");
        var original = game.Entries;
        var puzzleId = board.Entry.id;
        typeof(CryptogramGame).GetField("puzzles", Private).SetValue(game, original.Reverse().ToArray());
        typeof(CryptogramGame).GetMethod("ResolveSavedPuzzles", Private).Invoke(game, null);
        Check(game.Entries[save.activePuzzle.puzzleIndex].id == puzzleId, "Reordered catalogue resumes by stable ID");
        typeof(CryptogramGame).GetField("puzzles", Private).SetValue(game, original);
        typeof(CryptogramGame).GetMethod("ResolveSavedPuzzles", Private).Invoke(game, null);
        game.Execute(UiActionKind.Home, 0); game.Execute(UiActionKind.Classic, 0);
        Check(board.ExportProgress().filledSlots == saved.filledSlots && save.feathers == feathers, "Progress resumes without spending a feather");
        Check(save.authorProgress.Length == 6 && save.themeProgress.Length == 3, "Legacy save arrays unchanged despite extra collection");
    }
}
