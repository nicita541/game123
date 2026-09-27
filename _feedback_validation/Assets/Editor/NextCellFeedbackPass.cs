using System;
using System.Linq;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Focused authoring pass in the validation copy. Never runs in the player.
public static class NextCellFeedbackPass
{
    static bool testAfterSave;
    public static void RunAndTest() { testAfterSave = true; Run(); }
    public static void Run()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            var game = UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
            // Restore the complete original green button, then change only its position.
            var reference = EditorSceneManager.OpenScene("Assets/RestoreReference_Main.unity", OpenSceneMode.Additive);
            var originalGame = reference.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CryptogramGame>(true)).Single();
            var original = originalGame.screens[0].GetComponentsInChildren<Button>(true).Single(b => b.name == "Button_Classic");
            var current = game.screens[0].GetComponentsInChildren<Button>(true).Single(b => b.name == "Button_Classic");
            var restored = UnityEngine.Object.Instantiate(original.gameObject, current.transform.parent, false);
            restored.name = original.name; restored.transform.SetSiblingIndex(current.transform.GetSiblingIndex());
            var labelPath = AnimationUtility.CalculateTransformPath(originalGame.classicStartLabel.transform, original.transform);
            game.classicStartLabel = restored.transform.Find(labelPath).GetComponent<Text>();
            restored.GetComponent<UiAction>().Configure(game, UiActionKind.Classic);
            UnityEngine.Object.DestroyImmediate(current.gameObject);
            var frame = (RectTransform)restored.transform;
            frame.anchoredPosition = new Vector2(frame.anchoredPosition.x, -540);
            EditorSceneManager.CloseScene(reference, true);

            var victory = game.screens[4];
            foreach (var action in victory.GetComponentsInChildren<UiAction>(true))
                if (new SerializedObject(action).FindProperty("action").intValue == (int)UiActionKind.RewardVictory)
                    UnityEngine.Object.DestroyImmediate(action.gameObject);
            foreach (var text in victory.GetComponentsInChildren<Text>(true).Where(t => t.name.Contains("Bonus")).ToArray())
                UnityEngine.Object.DestroyImmediate(text.gameObject);
            var continueButton = victory.GetComponentsInChildren<UiAction>(true).Single(a => new SerializedObject(a).FindProperty("action").intValue == (int)UiActionKind.Continue);
            var continueRect = (RectTransform)continueButton.transform;
            continueRect.anchoredPosition = new Vector2(0, -820);

            var audio = game.soundPlayer;
            audio.musicSource.volume = .24f;
            audio.effectsSource.volume = .42f;
            audio.musicSource.loop = true;
            audio.musicSource.playOnAwake = false;
            foreach (var path in new[] { "Click", "Correct", "Mistake", "Victory", "LibraryMusic" })
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath("Assets/Audio/" + path + ".wav");
                var settings = importer.defaultSampleSettings;
                settings.loadType = path == "LibraryMusic" ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = path == "LibraryMusic" ? .8f : 1;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            // Human credit is part of the shipped settings screen as well as the source file.
            var settingsScreen = game.screens[10].transform;
            var previousCredits = settingsScreen.Find("Text_AudioCredits");
            var credits = previousCredits != null ? previousCredits.gameObject : new GameObject("Text_AudioCredits", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            if (previousCredits == null) credits.transform.SetParent(settingsScreen, false);
            var label = credits.GetComponent<Text>();
            label.font = game.settingValues[0].font;
            label.text = "Музыка: Э. Сати · Robin Alciatore / Musopen\nЗвуки: Kenney · CC0";
            label.fontSize = 24; label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(.29f, .20f, .16f); label.raycastTarget = false;
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, -920); rect.sizeDelta = new Vector2(880, 100);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("NEXT_CELL_SCENE_PASS: original green entry repositioned, no victory ad button, recorded audio configured");
            if (testAfterSave) NextCellRegression.Run(); else EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
