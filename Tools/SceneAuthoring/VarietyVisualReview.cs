using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class VarietyVisualReview
{
    public static void Run()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity", OpenSceneMode.Additive);
            SaveStore.EditorTestSlot = "erudition.variety-visual.temporary"; SaveStore.Reset();
            var game = UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
            typeof(CryptogramGame).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(game, null);
            game.RegisterGameplay(UnityEngine.Object.FindFirstObjectByType<GameplaySceneLink>());
            var save = (GameSave)typeof(CryptogramGame).GetField("save", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(game);
            save.erudition = 60;
            save.lastFailedPuzzleIndex = Array.FindIndex(game.Entries, e => e.id == "variety_v1_2_0");
            game.Execute(UiActionKind.Retry, 0); Shot("Gameplay_Riddle");
            game.CompletePuzzle(game.classicBoard); Shot("Victory_Scoring");
            game.Execute(UiActionKind.Collections, 0); game.Execute(UiActionKind.CollectionTab, 3); Shot("TextKinds");
            Shot("TextKinds_16x9", 1080, 1920);
            game.Execute(UiActionKind.ToggleSetting, 3); Shot("TextKinds_LargeText", 1080, 2400);
            game.Execute(UiActionKind.ToggleSetting, 3); game.Execute(UiActionKind.CollectionDetails, 15); Shot("RiddleCollection");
            SaveStore.Reset(); SaveStore.EditorTestSlot = null; EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void Shot(string name, int width = 1080, int height = 2340)
    {
        var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        camera.transform.SetParent(null, true);
        camera.transform.position = new Vector3(0, 0, -1000); camera.transform.rotation = Quaternion.identity;
        camera.orthographic = true; camera.orthographicSize = height / 2f;
        camera.nearClipPlane = .1f; camera.farClipPlane = 5000; camera.cullingMask = -1;
        camera.rect = new Rect(0, 0, 1, 1); camera.clearFlags = CameraClearFlags.SolidColor;
        var target = new RenderTexture(width, height, 24); target.Create(); camera.targetTexture = target;
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas) continue;
            var scaler = canvas.GetComponent<CanvasScaler>(); if (scaler != null) scaler.enabled = false;
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            var rect = (RectTransform)canvas.transform;
            rect.pivot = new Vector2(.5f, .5f);
            rect.position = Vector3.zero; rect.rotation = Quaternion.identity; rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(width, height);
        }
        foreach (var safe in UnityEngine.Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            safe.FitNormalized(new Rect(0, 0, 1, 1));
        foreach (var backdrop in UnityEngine.Object.FindObjectsByType<SceneBackdrop>(FindObjectsSortMode.None)) backdrop.Fit();
        foreach (var graphic in UnityEngine.Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None)) graphic.SetAllDirty();
        Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Review/VarietyReview"); Directory.CreateDirectory(path);
        File.WriteAllBytes(Path.Combine(path, name + ".png"), texture.EncodeToPNG());
        Debug.Log("VARIETY_VISUAL " + name + " graphics=" + UnityEngine.Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None).Length);
        UnityEngine.Object.DestroyImmediate(texture); camera.targetTexture = null; RenderTexture.active = null; target.Release(); UnityEngine.Object.DestroyImmediate(target);
    }
}
