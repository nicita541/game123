using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Erudition;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UsabilityReview
{
    static CryptogramGame game;
    static GameSave save;
    static Camera camera;
    static Canvas[] canvases;
    static string output;
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity",OpenSceneMode.Additive);
        SaveStore.EditorTestSlot="erudition.usability-review.temporary"; SaveStore.Reset();
        game=UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        typeof(CryptogramGame).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(game,null);
        game.RegisterGameplay(UnityEngine.Object.FindFirstObjectByType<GameplaySceneLink>());
        save=(GameSave)typeof(CryptogramGame).GetField("save",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
        camera=UnityEngine.Object.FindFirstObjectByType<Camera>(); canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        output=Path.Combine(Directory.GetCurrentDirectory(),"Review",LayoutOwnershipTests.Enabled?"SceneEditable":"FeedbackPass"); Directory.CreateDirectory(output);
        Shot("Home"); game.Execute(UiActionKind.Classic,0); Shot("Classic_Beginner");
        game.Execute(UiActionKind.Turbo,0); Shot("Turbo_Beginner");
        foreach(var item in new[]{(UiActionKind.Collections,"Collections"),(UiActionKind.Achievements,"Achievements"),(UiActionKind.Shop,"Shop"),(UiActionKind.Settings,"Settings"),(UiActionKind.Statistics,"Statistics")})
        {
            game.Execute(item.Item1,0); Shot(item.Item2);
            if(item.Item1==UiActionKind.Collections||item.Item1==UiActionKind.Settings||item.Item1==UiActionKind.Statistics)
            {Shot(item.Item2+"_16x9",1080,1920);Shot(item.Item2+"_Notch",1080,2400,true);}
        }
        game.Execute(UiActionKind.Collections,0);var authorScroll=game.collectionGroups[0].GetComponentInChildren<ScrollRect>();authorScroll.verticalNormalizedPosition=0;Shot("Collections_Scrolled");
        game.Execute(UiActionKind.Statistics,0);game.Execute(UiActionKind.StatisticsPeriod,2);Shot("Statistics_Year");game.Execute(UiActionKind.StatisticsPeriod,0);
        game.Execute(UiActionKind.CollectionTab,1);Shot("Collections_Themes");game.Execute(UiActionKind.CollectionTab,2);Shot("Collections_Books");
        save.coins=300;save.feathers=0;game.Execute(UiActionKind.BuyFifteenFeathers,0);game.Execute(UiActionKind.Shop,0);Shot("Shop_15Feathers");
        save.feathers=90;game.Execute(UiActionKind.Home,0);Shot("Home_90Feathers");
        save.activePuzzle=new PuzzleProgress(); save.erudition=600; save.feathers=5;
        game.Execute(UiActionKind.Classic,0); Shot("Classic_Long");
        var board=game.classicBoard;
        board.GetComponentsInChildren<LetterCellView>(true).Last(c=>c.gameObject.activeSelf&&c.IsHiddenLetter).Press(); Shot("Classic_Long_Scrolled");
        game.CompletePuzzle(board); Shot("Victory_Long"); Shot("Victory_16x9",1080,1920); Shot("Victory_Notch",1080,2400,true);
        game.Execute(UiActionKind.CollectionDetails,6); Shot("Collection_Story");
        save.activePuzzle=new PuzzleProgress(); game.Execute(UiActionKind.Classic,0); game.FailPuzzle(game.classicBoard); Shot("Defeat_Long");
        game.Execute(UiActionKind.DebugZeroFeathers,0); Shot("NoFeathers"); Shot("NoFeathers_16x9",1080,1920); Shot("NoFeathers_Notch",1080,2400,true);
        game.Execute(UiActionKind.Home,0); Shot("Home_16x9",1080,1920); Shot("Home_Notch",1080,2400,true);
        save.erudition=0; save.feathers=5; save.activePuzzle=new PuzzleProgress(); game.Execute(UiActionKind.Classic,0); Shot("Classic_16x9",1080,1920); Shot("Classic_Notch",1080,2400,true);
        game.Execute(UiActionKind.ToggleSetting,3); Shot("Classic_LargeText");
        game.Execute(UiActionKind.Settings,0); Shot("Settings_LargeText");
        game.Execute(UiActionKind.Statistics,0); Shot("Statistics_LargeText");
        game.Execute(UiActionKind.Collections,0); Shot("Collections_LargeText");
        game.Execute(UiActionKind.DebugZeroFeathers,0); Shot("NoFeathers_LargeText");
        game.Execute(UiActionKind.ToggleSetting,3); save.erudition=0; save.feathers=5; save.activePuzzle=new PuzzleProgress();
        game.Execute(UiActionKind.Classic,0); board=game.classicBoard;
        board.StartPuzzle(new PuzzleEntry{id="manual-placement-preview",mode=PuzzleMode.Classic,text="МАМА ПАПА ЛАЛА",source="Проверка повторяющихся букв"},board.PuzzleIndex,new PuzzleProgress(),0,save.hints);
        var repeats=board.GetComponentsInChildren<LetterCellView>(true).Where(c=>c.gameObject.activeSelf&&c.IsHiddenLetter).GroupBy(c=>c.Code).First(g=>g.Count()>1).ToArray();
        var key=board.GetComponentsInChildren<KeyboardKeyView>(true).Single(k=>k.Letter==repeats[0].Answer);
        repeats[0].Press(); key.Press(); key.ResetVisual(); repeats[1].Press(); Shot("Keyboard_FoundGreen");
        foreach(var cell in repeats.Skip(1)) { cell.Press(); key.Press(); } key.ResetVisual(); Shot("Keyboard_Exhausted");
        camera.targetTexture=null; RenderTexture.active=null; SaveStore.Reset(); SaveStore.EditorTestSlot=null;
        Debug.Log("USABILITY_CAPTURE_COMPLETE "+output);
    }
    static void Shot(string name,int width=1080,int height=2340,bool notch=false)
    {
        var target=new RenderTexture(width,height,24); target.Create(); camera.targetTexture=target; camera.orthographic=true; camera.rect=new Rect(0,0,1,1); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
        foreach(var canvas in canvases) { canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=100; }
        Canvas.ForceUpdateCanvases();
        foreach(var safe in UnityEngine.Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include,FindObjectsSortMode.None)) safe.FitNormalized(notch?new Rect(0,.028f,1,.925f):new Rect(0,0,1,1));
        foreach(var bg in UnityEngine.Object.FindObjectsByType<SceneBackdrop>(FindObjectsSortMode.None)) bg.Fit();
        Canvas.ForceUpdateCanvases();
        foreach(var text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)) { text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle); text.SetAllDirty(); }
        Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=target;
        if(name.StartsWith("Settings")||name.StartsWith("Statistics")||name.StartsWith("Collections")||name.StartsWith("NoFeathers"))
            foreach(var text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t=>t.isActiveAndEnabled&&!string.IsNullOrWhiteSpace(t.text)))
            {
                if(text.canvasRenderer.cull) continue; // Text outside a scroll viewport is intentionally clipped.
                if(text.cachedTextGenerator.vertexCount==0) throw new Exception("Missing rendered text in "+name+": "+text.transform.parent.name+"/"+text.name+" = "+text.text);
                if(!text.resizeTextForBestFit&&text.verticalOverflow==VerticalWrapMode.Truncate&&text.preferredHeight>text.rectTransform.rect.height+1)
                    throw new Exception("Partially clipped text in "+name+": "+text.name+" = "+text.text+" (required "+text.preferredHeight+", available "+text.rectTransform.rect.height+")");
            }
        if(name=="Classic_LargeText")
            foreach(var text in game.classicBoard.GetComponentsInChildren<Text>().Where(t=>t.name=="Text_Letter"&&!string.IsNullOrWhiteSpace(t.text)))
                if(text.cachedTextGenerator.vertexCount==0) throw new Exception("Visible letter clipped by large-text setting: "+text.text);
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,false); texture.ReadPixels(new Rect(0,0,width,height),0,0); texture.Apply();
        File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG()); File.WriteAllBytes(Path.Combine(output,name+".jpg"),texture.EncodeToJPG(96));
        UnityEngine.Object.DestroyImmediate(texture); camera.targetTexture=null; RenderTexture.active=null; target.Release(); UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("USABILITY_SHOT "+name+" "+width+"x"+height);
    }
}
