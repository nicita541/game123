using System;
using System.Reflection;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplayEntrySmoke
{
    static int frames, result;
    static bool tested, oldEnabled;
    static EnterPlayModeOptions oldOptions;
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity");
        SaveStore.EditorTestSlot="erudition.direct-scene.temporary"; SaveStore.Reset();
        oldEnabled=EditorSettings.enterPlayModeOptionsEnabled; oldOptions=EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled=true; EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.update+=Tick; EditorApplication.playModeStateChanged+=State; EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying||tested) return;
        frames++; var game=CryptogramGame.Current;
        if(frames<30 || game==null || game.classicBoard==null || game.classicBoard.Entry==null) { if(frames<6000)return; }
        tested=true;
        try
        {
            if(game==null||game.classicBoard==null||!game.screens[2].activeSelf||SceneManager.sceneCount!=2) throw new Exception("Direct GameplayScene did not boot");
            var save=(GameSave)typeof(CryptogramGame).GetField("save",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
            if(save.feathers!=4||save.erudition!=0||game.classicBoard.HiddenKinds!=2) throw new Exception("Direct scene initialization state incorrect");
            game.Execute(UiActionKind.Home,0); if(!game.screens[0].activeSelf) throw new Exception("Home from direct gameplay failed");
            Debug.Log("DIRECT_GAMEPLAY_ENTRY_PASS: shell loaded once, correct initial puzzle, navigation works");
        }
        catch(Exception e){result=1; Debug.LogException(e);}
        EditorApplication.isPlaying=false;
    }
    static void State(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode||!tested)return;
        EditorApplication.update-=Tick; EditorApplication.playModeStateChanged-=State;
        EditorSettings.enterPlayModeOptionsEnabled=oldEnabled; EditorSettings.enterPlayModeOptions=oldOptions;
        SaveStore.Reset(); SaveStore.EditorTestSlot=null; EditorApplication.Exit(result);
    }
}
