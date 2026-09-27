using System;
using System.Linq;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One-off migration of existing scene objects. Never included in the player.
public static class SceneLayoutOwnership
{
    static CryptogramGame game;
    static Transform F(Transform p,string name)=>p.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
    static Transform M(Transform p,string name)=>p.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
    static void R(RectTransform r,float x,float y,float w,float h)
    {r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
    static RectTransform Root(Transform parent,string name,float x,float y,float w,float h)
    {var o=new GameObject(name,typeof(RectTransform));o.layer=5;o.transform.SetParent(parent,false);var r=(RectTransform)o.transform;R(r,x,y,w,h);return r;}
    static void Remove(Transform p,string name){var t=M(p,name);if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);}
    static void ImageStyle(Image target,Image source)
    {if(target==null||source==null)return;target.sprite=source.sprite;target.type=source.type;target.color=source.color;target.preserveAspect=source.preserveAspect;target.pixelsPerUnitMultiplier=source.pixelsPerUnitMultiplier;}
    static void ButtonStyles(Transform current,Transform source)
    {
        foreach(var b in current.GetComponentsInChildren<Button>(true))
        {
            var relative=AnimationUtility.CalculateTransformPath(b.transform,current);var old=source.Find(relative);if(old==null)continue;
            ImageStyle(b.GetComponent<Image>(),old.GetComponent<Image>());
            var text=b.GetComponentInChildren<Text>(true);var oldText=old.GetComponentInChildren<Text>(true);if(text!=null&&oldText!=null)text.color=oldText.color;
        }
    }
    public static void Run()
    {
        var main=EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");game=UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        var legacy=EditorSceneManager.OpenScene("Assets/LayoutReference_Main.unity",OpenSceneMode.Additive);
        var previous=legacy.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CryptogramGame>(true)).Single();
        RestoreShop(previous,main);
        foreach(var i in new[]{4,5,10,12})ButtonStyles(game.screens[i].transform,previous.screens[i].transform);
        // Existing illustrated ornaments were hidden by the flat-button pass.
        var ad=game.victoryAdButton.transform;
        foreach(var child in ad.Cast<Transform>().Where(t=>t.name=="Video"||t.name=="Gift"))child.gameObject.SetActive(true);
        var adLabel=ad.GetComponentInChildren<Text>(true);R(adLabel.rectTransform,0,0,692,139);
        EditorSceneManager.CloseScene(legacy,true);
        ConfigureTracks();ConfigureGraphs();ConfigureSwitches();
        for(var i=0;i<game.achievementCards.Length;i++)
        {
            // Bake the former runtime layout once. Already-edited positions are retained.
            var r=(RectTransform)game.achievementCards[i].transform;
            if(Mathf.Abs(r.anchoredPosition.y-(413-i*204))<.1f)r.anchoredPosition=new Vector2(r.anchoredPosition.x,366-i*204);
        }
        NormalizeBackgrounds(main);EditorSceneManager.MarkSceneDirty(main);EditorSceneManager.SaveScene(main);
        var gameplay=EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity",OpenSceneMode.Additive);
        var link=gameplay.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameplaySceneLink>(true)).Single();
        var oldGameplay=EditorSceneManager.OpenScene("Assets/LayoutReference_Gameplay.unity",OpenSceneMode.Additive);
        var oldLink=oldGameplay.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameplaySceneLink>(true)).Single();
        foreach(var pair in new[]{(link.classicBoard,oldLink.classicBoard),(link.turboBoard,oldLink.turboBoard)})
        {
            ButtonStyles(pair.Item1.transform,pair.Item2.transform);
            var mode=F(pair.Item1.transform,"Label_Mode");ImageStyle(mode.GetComponent<Image>(),F(pair.Item2.transform,"Label_Mode").GetComponent<Image>());F(mode,"Text_Mode").GetComponent<Text>().color=Color.white;
            AuthorRows(pair.Item1);
        }
        EditorSceneManager.CloseScene(oldGameplay,true);NormalizeBackgrounds(gameplay);
        EditorSceneManager.MarkSceneDirty(gameplay);EditorSceneManager.SaveScene(gameplay);
        PlayerSettings.bundleVersion="0.1.2";PlayerSettings.Android.bundleVersionCode=3;
        AssetDatabase.SaveAssets();Debug.Log("SCENE_LAYOUT_OWNERSHIP_SAVED userHomeOffsetsPreserved=true oldShopRestored=true cellsAuthored=true");
    }
    public static void RunAndTest(){Run();LayoutOwnershipTests.Enabled=true;UsabilityTests.Run();}
    public static void ReviewAndBuild()
    {
        LayoutOwnershipTests.Enabled=true;UsabilityReview.Capture();
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");AndroidApkBuild.Run();
    }
    public static void PolishNoticeAndTest()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");game=UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        ShopNotice();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        LayoutOwnershipTests.Enabled=true;UsabilityTests.Run();
    }
    static void ShopNotice()
    {
        game.shopMessage.transform.SetParent(game.screens[9].transform,false);
        R(game.shopMessage.rectTransform,0,-595,880,65);game.shopMessage.fontSize=32;game.shopMessage.color=new Color(.23f,.12f,.24f);
    }
    static void RestoreShop(CryptogramGame old,Scene main)
    {
        var oldShop=old.screens[9];var parent=game.screens[9].transform.parent;var sibling=game.screens[9].transform.GetSiblingIndex();
        var copy=UnityEngine.Object.Instantiate(oldShop);copy.name=oldShop.name;copy.transform.SetParent(null,false);SceneManager.MoveGameObjectToScene(copy,main);copy.transform.SetParent(parent,false);copy.transform.SetSiblingIndex(sibling);
        Text Remap(Text text)=>copy.transform.Find(AnimationUtility.CalculateTransformPath(text.transform,oldShop.transform)).GetComponent<Text>();
        game.shopFeathers=Remap(old.shopFeathers);game.shopCoins=Remap(old.shopCoins);game.shopMessage=Remap(old.shopMessage);
        UnityEngine.Object.DestroyImmediate(game.screens[9]);game.screens[9]=copy;
        foreach(var action in copy.GetComponentsInChildren<UiAction>(true))action.SetGame(game);
        Remove(copy.transform,"Section_Special");Remove(F(copy.transform,"Section_Feathers"),"Text_Subtitle");Remove(F(copy.transform,"Section_Hints"),"Subtitle");
        var title=F(copy.transform,"Text_Title").GetComponent<Text>();title.fontSize=72;title.resizeTextMaxSize=72;title.resizeTextMinSize=60;
        game.shopFeathers.text="5";game.shopCoins.text="0";game.shopMessage.text="";
        var balance=F(copy.transform,"BalanceBar");
        var number=game.shopFeathers.rectTransform;number.anchoredPosition+=new Vector2(0,14);
        var cap=Root(number.parent,"Text_RegenLimit",number.anchoredPosition.x,-24,240,26).gameObject.AddComponent<Text>();cap.font=game.shopFeathers.font;cap.fontSize=19;cap.alignment=TextAnchor.MiddleCenter;cap.color=new Color(1,.95f,.79f);cap.text="восстановление до 5";cap.raycastTarget=false;
        ShopNotice();
        copy.SetActive(false);
    }
    static void ConfigureTracks()
    {
        foreach(var c in game.authorCards.Concat(game.themeCards).Concat(game.bookCards))c.progressTrack=(RectTransform)F(c.transform,"Progress_Back");
        foreach(var c in game.achievementCards)c.progressTrack=(RectTransform)F(c.transform,"Progress_Back");
        game.presentation.statsTrack=(RectTransform)F(game.presentation.statsFill.parent,"Progress_Back");
        game.presentation.detailTrack=(RectTransform)F(game.presentation.detailFill.parent,"Progress_Back");
    }
    static void ConfigureSwitches()
    {
        game.settingOnStops=new RectTransform[4];game.settingOffStops=new RectTransform[4];
        for(var i=0;i<4;i++)
        {
            var track=game.settingTracks[i].transform;
            game.settingOnStops[i]=Root(track,"OnPosition",43,0,0,0);game.settingOffStops[i]=Root(track,"OffPosition",-43,0,0,0);
        }
    }
    static void ConfigureGraphs()
    {
        var ui=game.presentation;var graph=ui.bars[0].parent;
        var weekly=Root(graph,"WeeklyChart_Editable",0,0,890,400);var yearly=Root(graph,"YearlyChart_Editable",0,0,890,400);
        ui.weeklyGraph=weekly.gameObject;ui.yearlyGraph=yearly.gameObject;
        ui.weekBars=new RectTransform[7];ui.weekDays=new Text[7];ui.weekAmounts=new Text[7];
        for(var i=0;i<7;i++)
        {
            ui.weekBars[i]=UnityEngine.Object.Instantiate(ui.bars[i],weekly,false);ui.weekBars[i].name="WeekBar_"+i;
            ui.weekDays[i]=UnityEngine.Object.Instantiate(ui.days[i],weekly,false);ui.weekDays[i].name="WeekDay_"+i;
            ui.weekAmounts[i]=UnityEngine.Object.Instantiate(ui.amounts[i],weekly,false);ui.weekAmounts[i].name="WeekCount_"+i;
            ChartSlot(ui.weekBars[i],ui.weekDays[i],ui.weekAmounts[i],i,7);
        }
        for(var i=0;i<12;i++)
        {
            ui.bars[i].SetParent(yearly,false);ui.days[i].transform.SetParent(yearly,false);ui.amounts[i].transform.SetParent(yearly,false);
            ChartSlot(ui.bars[i],ui.days[i],ui.amounts[i],i,12);
        }
        yearly.gameObject.SetActive(false);
    }
    static void ChartSlot(RectTransform bar,Text day,Text amount,int index,int count)
    {
        var step=890f/count;var x=(index-(count-1)/2f)*step;R(bar,x,-150,step*.7f,175);bar.pivot=new Vector2(.5f,0);
        var im=bar.GetComponent<Image>();im.type=Image.Type.Filled;im.fillMethod=Image.FillMethod.Vertical;im.fillOrigin=0;im.fillAmount=.025f;
        R(day.rectTransform,x,-180,step,45);day.fontSize=count==12?18:25;day.resizeTextForBestFit=false;
        R(amount.rectTransform,x,49,step,45);amount.text="0";
    }
    static void AuthorRows(PuzzleBoard board)
    {
        var serialized=new SerializedObject(board);var content=(RectTransform)serialized.FindProperty("content").objectReferenceValue;
        var cells=board.GetComponentsInChildren<LetterCellView>(true).OrderBy(c=>c.name).ToArray();
        var rows=cells.Length/16;
        for(var row=0;row<rows;row++)
        {
            var line=Root(content,"LetterRow_"+row.ToString("00"),0,-52-row*104,928,104);line.anchorMin=line.anchorMax=new Vector2(.5f,1);
            for(var column=0;column<16;column++)
            {
                var cell=(RectTransform)cells[row*16+column].transform;cell.SetParent(line,false);R(cell,(column-7.5f)*58,0,56,100);
            }
        }
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,rows*104);
        var fill=(Image)serialized.FindProperty("progressFill").objectReferenceValue;
        fill.rectTransform.anchorMin=fill.rectTransform.anchorMax=fill.rectTransform.pivot=new Vector2(0,.5f);fill.rectTransform.anchoredPosition=Vector2.zero;
        board.SetGame(null);board.StartPuzzle(PuzzleGenerator.Generate(board.Mode,0,0),board.Mode==PuzzleMode.Classic?24:504,new PuzzleProgress(),0,2);
    }
    static void NormalizeBackgrounds(Scene scene)
    {
        foreach(var root in scene.GetRootGameObjects())
        {
            foreach(var bg in root.GetComponentsInChildren<SceneBackdrop>(true))
            {
                bg.extendBeyondSafeArea=false;R(bg.picture.rectTransform,0,0,1080,2340);bg.picture.uvRect=new Rect(0,0,1,1);
                // Only a dedicated, non-interactive edge extension adapts to device
                // margins. The actual illustration remains fully scene-authored.
                var edge=Root(bg.transform.parent,"Background_EdgeExtension_AutoFit",0,0,1080,2340);
                edge.SetSiblingIndex(bg.transform.GetSiblingIndex());
                var picture=edge.gameObject.AddComponent<RawImage>();picture.texture=bg.picture.texture;picture.color=bg.picture.color;picture.raycastTarget=false;
                var fit=edge.gameObject.AddComponent<SceneBackdrop>();fit.artworkFrame=bg.picture.rectTransform;fit.picture=picture;fit.extendBeyondSafeArea=true;fit.Fit();
            }
            foreach(var warmth in root.GetComponentsInChildren<UiWarmth>(true))warmth.breathe=false;
        }
    }
}
