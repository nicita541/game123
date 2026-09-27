using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ProjectReview
{
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        SaveStore.EditorTestSlot = "erudition.review.temporary";
        SaveStore.Reset();
        var game = UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        typeof(CryptogramGame).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, null);
        var output = Path.Combine(Directory.GetCurrentDirectory(), "Review");
        Directory.CreateDirectory(output);
        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        var target = new RenderTexture(1080, 2340, 24);
        target.Create();
        camera.targetTexture = target;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.rect = new Rect(0, 0, 1, 1);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 100;
        var safe = canvas.GetComponentsInChildren<RectTransform>(true).First(rect => rect.name == "SafeArea_1080x2340");
        safe.anchorMin = Vector2.zero;
        safe.anchorMax = Vector2.one;
        safe.offsetMin = new Vector2(24, 32);
        safe.offsetMax = new Vector2(-24, -32);
        var views = new[] { (UiActionKind.Home, 0, "Home"), (UiActionKind.Classic, 0, "Classic"), (UiActionKind.Turbo, 0, "Turbo"),
            (UiActionKind.Collections, 0, "Collections"), (UiActionKind.CollectionTab, 1, "Themes"), (UiActionKind.CollectionTab, 2, "Books"),
            (UiActionKind.Achievements, 0, "Achievements"), (UiActionKind.Shop, 0, "Shop"), (UiActionKind.Settings, 0, "Settings"),
            (UiActionKind.Statistics, 0, "Statistics"), (UiActionKind.Statistics, 0, "StatisticsProgress"),
            (UiActionKind.StatisticsPeriod, 1, "StatisticsMonth"), (UiActionKind.StatisticsPeriod, 2, "StatisticsYear"),
            (UiActionKind.CollectionDetails, 0, "CollectionDetails"), (UiActionKind.Debug, 0, "Debug"),
            (UiActionKind.DebugVictory, 0, "Victory"), (UiActionKind.DebugDefeat, 0, "Defeat"), (UiActionKind.DebugZeroFeathers, 0, "NoFeathers") };
        foreach (var view in views)
        {
            game.Execute(view.Item1, view.Item2);
            if (view.Item3 == "StatisticsProgress")
            {
                var example = new GameSave { bestStreak = 28 };
                var counts = new[] { 6, 10, 8, 12, 14, 9, 11 };
                for (var i = 0; i < counts.Length; i++) example.activityHistory.Add(new ActivityDay { date = DateTime.UtcNow.Date.AddDays(i - 6).Ticks, solved = counts[i] });
                example.dailySolved[6] = 11;
                game.presentation.Refresh(example, 6, null);
                game.statsSolved.text = "86"; game.statsAccuracy.text = "92%"; game.statsStreak.text = "12";
            }
            Canvas.ForceUpdateCanvases();
            foreach (var label in canvas.GetComponentsInChildren<Text>(false))
            {
                label.font.RequestCharactersInTexture(label.text, label.fontSize, label.fontStyle);
                label.SetAllDirty();
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(1080, 2340, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, 1080, 2340), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(output, view.Item3 + ".png"), texture.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(output, view.Item3 + ".jpg"), texture.EncodeToJPG(94));
            UnityEngine.Object.DestroyImmediate(texture);
        }
        camera.targetTexture = null;
        RenderTexture.active = null;
        target.Release();
        SaveStore.Reset();
        SaveStore.EditorTestSlot = null;
        foreach (var screen in game.screens)
        {
            Debug.Log("SCREEN_LAYOUT " + screen.name);
            foreach (var transform in screen.GetComponentsInChildren<RectTransform>(true))
            {
                var text = transform.GetComponent<Text>();
                if (transform.parent == screen.transform || (text != null && !transform.name.StartsWith("Text_Code") && !transform.name.StartsWith("Text_Letter")))
                    Debug.Log("LAYOUT " + screen.name + "/" + transform.parent.name + "/" + transform.name
                        + " pos=" + transform.anchoredPosition + " size=" + transform.sizeDelta
                        + (text == null ? "" : " text=" + text.text.Replace('\n', '|')));
            }
        }
        Debug.Log("CAPTURE_COMPLETE " + output);
    }
}
