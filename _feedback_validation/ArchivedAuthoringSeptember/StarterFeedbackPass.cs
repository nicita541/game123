using System;
using System.Linq;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Focused, edit-time scene migration. Never part of the player or a runtime layout.
public static class StarterFeedbackPass
{
    public static void Inspect()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        var game=UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        foreach(var t in game.authorCards[0].GetComponentsInChildren<RectTransform>(true))Describe(t);
        foreach(var t in game.screens[12].GetComponentsInChildren<RectTransform>(true))Describe(t);
        Debug.Log("FEEDBACK_INSPECT_REFS quote="+game.defeatQuote.name+" source="+game.defeatSource.name+" message="+game.defeatMessage.name);
        EditorApplication.Exit(0);
    }
    static void Describe(RectTransform r)
    {
        var im=r.GetComponent<Image>();var text=r.GetComponent<Text>();
        Debug.Log("FEEDBACK_INSPECT "+AnimationUtility.CalculateTransformPath(r,null)+" position="+r.anchoredPosition+" size="+r.sizeDelta+" anchor="+r.anchorMin+"/"+r.anchorMax+" pivot="+r.pivot
            +" components="+string.Join(",",r.GetComponents<Component>().Select(c=>c.GetType().Name))
            +(im==null?"":" sprite="+(im.sprite==null?"none":AssetDatabase.GetAssetPath(im.sprite)+":"+im.sprite.name)+" type="+im.type+" border="+(im.sprite==null?Vector4.zero:im.sprite.border))
            +(text==null?"":" text="+text.text));
    }
}
