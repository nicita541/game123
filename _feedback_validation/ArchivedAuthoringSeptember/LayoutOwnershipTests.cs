using System;
using System.Collections.Generic;
using System.Linq;
using Erudition;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Compares the saved edit-time geometry with the same objects after Awake and
// after the full gameplay regression suite. No player data or source scenes changed.
public static class LayoutOwnershipTests
{
    public static bool Enabled;
    struct Geometry
    {
        public Vector2 position,size,min,max,pivot;
        public Vector3 scale;
        public Quaternion rotation;
        public Geometry(RectTransform r)
        {position=r.anchoredPosition;size=r.sizeDelta;min=r.anchorMin;max=r.anchorMax;pivot=r.pivot;scale=r.localScale;rotation=r.localRotation;}
        public bool Matches(RectTransform r)=>Near(position,r.anchoredPosition)&&Near(size,r.sizeDelta)&&Near(min,r.anchorMin)&&Near(max,r.anchorMax)&&Near(pivot,r.pivot)
            &&Vector3.Distance(scale,r.localScale)<.01f&&Quaternion.Angle(rotation,r.localRotation)<.01f;
        static bool Near(Vector2 a,Vector2 b)=>Vector2.Distance(a,b)<.05f;
    }
    static readonly Dictionary<string,Geometry> saved=new Dictionary<string,Geometry>();
    static string Path(Transform t)=>t.parent==null?t.gameObject.scene.name+"/"+t.name:Path(t.parent)+"/"+t.name;
    static RectTransform[] Tracked()=>Enumerable.Range(0,SceneManager.sceneCount).SelectMany(i=>SceneManager.GetSceneAt(i).GetRootGameObjects())
        .SelectMany(r=>r.GetComponentsInChildren<RectTransform>(true)).Where(r=>
            r.GetComponent<Button>()!=null||r.GetComponent<LetterCellView>()!=null||r.GetComponent<CollectionCardView>()!=null||r.GetComponent<AchievementCardView>()!=null
            ||r.name.StartsWith("LetterRow_")||r.name=="Illustration_Background"||r.name=="EruditionBlock"||r.name=="FeatherEnergy"
            ||r.parent!=null&&(r.parent.name=="WeeklyChart_Editable"||r.parent.name=="YearlyChart_Editable")).ToArray();
    public static void Capture()
    {
        saved.Clear();foreach(var r in Tracked())saved.Add(Path(r),new Geometry(r));
        if(saved.Count<850)throw new Exception("Not enough scene geometry tracked: "+saved.Count);
        Debug.Log("SCENE_GEOMETRY_CAPTURE count="+saved.Count);
    }
    public static void AssertUnchanged(string stage)
    {
        var actual=Tracked().ToDictionary(Path,r=>r);
        foreach(var pair in saved)
        {
            if(!actual.TryGetValue(pair.Key,out var r))throw new Exception("Authored object missing "+stage+": "+pair.Key);
            if(!pair.Value.Matches(r))throw new Exception("Authored geometry changed "+stage+": "+pair.Key+" position="+r.anchoredPosition+" size="+r.sizeDelta);
        }
        var game=UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        var menu=game.screens[0].transform;
        var objects=menu.GetComponentsInChildren<RectTransform>(true);
        if(objects.Single(r=>r.name=="FeatherEnergy").anchoredPosition.y!=1000||objects.Single(r=>r.name=="EruditionBlock").anchoredPosition.y!=695
            ||objects.Single(r=>r.name=="Button_Settings").anchoredPosition.y!=1000||objects.Any(r=>r.name=="Text_RegenLimit"))
            throw new Exception("User's saved main-menu edits were overwritten");
        foreach(var bg in UnityEngine.Object.FindObjectsByType<SceneBackdrop>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(bg.extendBeyondSafeArea)continue; // Dedicated device-margin underlay, not authored artwork.
            var before=new Geometry(bg.picture.rectTransform);bg.Fit();
            if(!before.Matches(bg.picture.rectTransform))throw new Exception("Background Fit overwrites scene geometry: "+bg.name);
        }
        Debug.Log("SCENE_GEOMETRY_UNCHANGED_PASS stage="+stage+" tracked="+saved.Count+" userMenuEdits=true backgroundsEditable=true");
    }
}
