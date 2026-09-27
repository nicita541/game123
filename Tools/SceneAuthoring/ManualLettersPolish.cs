using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Run once in the authoring copy after HeaderSettingsPolish. Edits existing scene objects only.
public static class ManualLettersPolish
{
    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity");
        var legends=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Text>(true))
            .Where(t=>t.name=="Text_KeyboardLegend").ToArray();
        if(legends.Length!=2) throw new Exception("Expected one keyboard legend per game mode");
        foreach(var text in legends)
        {
            text.text="Зелёная — буква найдена\nСерая и зачёркнутая — все повторения на месте";
            text.fontSize=27;
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,84);
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("MANUAL_LETTER_LEGENDS_SAVED count=2");
        UsabilityTests.Run();
    }
}
