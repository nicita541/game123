using System;
using System.Linq;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-time scene authoring, outside Assets in the delivered project.
public static class IntegratedReferenceBackgrounds
{
    static CryptogramGame game;
    static Sprite rounded;
    static readonly Color Ink=new Color(.12f,.06f,.23f);
    static readonly Color Paper=new Color(1,.987f,.962f);
    static Transform F(Transform p,string n)=>p.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
    static Transform M(Transform p,string n)=>p.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==n);
    static Transform S(int n)=>game.screens[n].transform;
    static void R(Transform t,float x,float y,float w,float h)
    {var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;}
    static void Move(Transform p,string n,float x,float y,float w,float h)=>R(F(p,n),x,y,w,h);
    static void Remove(Transform p,params string[] names)
    {foreach(var n in names){var t=M(p,n);if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);}}
    static void Flat(Image image,Color color,float radius=.75f)
    {image.sprite=rounded;image.type=Image.Type.Sliced;image.preserveAspect=false;image.pixelsPerUnitMultiplier=radius;image.color=color;}
    static void Flat(Transform p,string name,Color color,float radius=.75f)=>Flat(F(p,name).GetComponent<Image>(),color,radius);
    static void Title(Transform s,float x,float y,float w,float h,int size,string value=null)
    {
        var t=F(s,"Text_Title").GetComponent<Text>();R(t.transform,x,y,w,h);t.fontSize=size;t.color=Ink;
        t.resizeTextForBestFit=true;t.resizeTextMinSize=size-8;t.resizeTextMaxSize=size;
        if(value!=null)t.text=value;
    }
    static RawImage Background(Transform screen,string name)
    {
        var o=new GameObject("Illustration_Background",typeof(RectTransform),typeof(RawImage));o.layer=5;o.transform.SetParent(screen,false);
        R(o.transform,0,0,1080,2340);o.transform.SetAsFirstSibling();var im=o.GetComponent<RawImage>();
        im.texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Backgrounds/"+name+"_reference_v1.png");im.raycastTarget=false;
        if(im.texture==null)throw new Exception("Missing integrated background: "+name);
        var fit=o.AddComponent<SceneBackdrop>();fit.artworkFrame=(RectTransform)screen;fit.picture=im;fit.Fit();
        return im;
    }
    public static void Apply(CryptogramGame owner)
    {
        game=owner;rounded=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/rounded_panel.png");
        foreach(var name in new[]{"home","gameplay","victory","energy","stats","collections","achievements","shop","settings"})
        {
            var path="Assets/Art/Backgrounds/"+name+"_reference_v1.png";AssetDatabase.ImportAsset(path);
            var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=100;
            ti.maxTextureSize=4096;ti.npotScale=TextureImporterNPOTScale.None;ti.mipmapEnabled=true;ti.filterMode=FilterMode.Trilinear;ti.wrapMode=TextureWrapMode.Mirror;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
        }
        foreach(var pair in new[]{(0,"home"),(4,"victory"),(5,"energy"),(6,"stats"),(7,"collections"),(8,"achievements"),(9,"shop"),(10,"settings"),(12,"energy")})
        {
            var s=S(pair.Item1);Background(s,pair.Item2);
            foreach(var child in s.Cast<Transform>().ToArray())
                if(child.name.StartsWith("OwlMascot")||child.name.StartsWith("Decor_")||child.name.StartsWith("Celebration_")||child.name=="Owl_Settings"||child.name=="Trophy_Decor"||child.name=="Books_Decor")
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            Remove(s,"TitleParchment","Header_Crown");
        }
        Home();Statistics();Collections();Victory();Energy();OtherScreens();
        foreach(var screen in game.screens.Where(s=>s!=null))
        {
            var nav=M(screen.transform,"BottomNavigation");if(nav!=null)Flat(nav.GetComponent<Image>(),Paper,.25f);
        }
        PrefabUtility.SaveAsPrefabAsset(game.authorCards[0].gameObject,"Assets/Prefabs/AuthorCard.prefab");
        PrefabUtility.SaveAsPrefabAsset(game.themeCards[0].gameObject,"Assets/Prefabs/CollectionCard.prefab");
        PrefabUtility.SaveAsPrefabAsset(F(S(0),"BottomNavigation").gameObject,"Assets/Prefabs/BottomNavigation.prefab");
        Gameplay();Debug.Log("INTEGRATED_BACKGROUNDS_AUTHORED screens=11 uniqueArt=9 detachedOwls=0");
    }
    static void Home()
    {
        var s=S(0);var block=F(s,"EruditionBlock");var backdrop=block.GetComponent<Image>();if(backdrop!=null)backdrop.enabled=false;
        Remove(block,"Icon_Crown","Laurel_Left","Laurel_Right");Remove(s,"Text_Welcome");
        R(block,0,735,840,230);Title(block,0,0,720,140,113);
        Move(block,"ValueBadge",0,-130,260,112);R(game.mainErudition.transform,0,-130,245,105);game.mainErudition.fontSize=85;
        var badge=F(block,"ValueBadge").GetComponent<Image>();badge.enabled=true;badge.sprite=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/SpriteSheets/reference_controls_v2.png").OfType<Sprite>().Single(s=>s.name=="Capsule");badge.color=Color.white;
    }
    static void Statistics()
    {
        var s=S(6);Title(s,0,1015,590,125,92);
        var level=F(s,"EruditionCard");R(level,0,283,1000,370);Flat(level.GetComponent<Image>(),new Color(1,.976f,.897f),.32f);
        Move(level,"Text_Heading",-165,122,560,78);F(level,"Text_Heading").GetComponent<Text>().color=Ink;
        Move(level,"Text_NextLevel",210,-115,435,60);
        foreach(var n in new[]{"LevelLaurel_Left","LevelLaurel_Right"}){var leaf=F(level,n);leaf.localEulerAngles=new Vector3(0,0,n.EndsWith("Left")?-36:36);}
        Flat(level,"Progress_Back",new Color(.78f,.8f,.87f),1);
        Flat(game.statsProgressFill.GetComponent<Image>(),new Color(.17f,.57f,1),1);
        var names=new[]{"Card_Solved","Card_Accuracy","Card_Streak","Card_Best"};
        var colors=new[]{new Color(1,.969f,.918f),new Color(.925f,.987f,.889f),new Color(.951f,.910f,1),new Color(.906f,.955f,1)};
        for(var i=0;i<4;i++)
        {
            var card=F(s,names[i]);R(card,i%2==0?-254:254,i<2?-45:-302,490,238);Flat(card.GetComponent<Image>(),colors[i],.38f);
            foreach(var n in new[]{"Text_Label","Text_Value"}) F(card,n).GetComponent<Text>().color=Ink;
        }
        var graph=F(s,"ActivityGraph");R(graph,0,-697,1000,500);Flat(graph.GetComponent<Image>(),Paper,.32f);
        foreach(var b in game.presentation.periods)Flat(b.GetComponent<Image>(),Color.white,.7f);
        foreach(var b in game.presentation.bars)Flat(b.GetComponent<Image>(),new Color(.23f,.61f,1),1.2f);
    }
    static void Collections()
    {
        var s=S(7);Title(s,89,912,555,138,91);
        var tabs=F(s,"Tabs");R(tabs,0,586,1005,128);Flat(tabs.GetComponent<Image>(),Paper,.55f);
        foreach(var b in game.collectionTabs){Flat(b.GetComponent<Image>(),Color.white,.6f);var r=(RectTransform)b.transform;r.sizeDelta=new Vector2(321,104);}
        var all=game.authorCards.Concat(game.themeCards).Concat(game.bookCards).ToArray();
        for(var i=0;i<all.Length;i++)
        {
            var c=all[i];var local=i<6?i:(i-6)%3;R(c.transform,local%2==0?-253:253,303-local/2*400,490,390);Flat(c.GetComponent<Image>(),Paper,.4f);
            var viewport=M(c.transform,"PortraitViewport");if(viewport!=null){R(viewport,0,50,490,290);Flat(viewport.GetComponent<Image>(),Color.white,.4f);Move(viewport,"IllustratedBackground",0,0,880,310);}
            var foot=F(c.transform,"InfoPanel");R(foot,0,-125,490,140);Flat(foot.GetComponent<Image>(),Paper,.4f);
            R(c.title.transform,-14,-90,428,52);c.title.color=Ink;c.title.fontSize=32;c.title.resizeTextForBestFit=true;c.title.resizeTextMinSize=28;c.title.resizeTextMaxSize=32;
            R(c.progressText.transform,-138,-130,160,39);c.progressText.fontSize=27;
            Move(c.transform,"Progress_Back",-46,-170,332,18);Flat(c.transform,"Progress_Back",new Color(.85f,.86f,.93f),1);
            R(c.progressFill,-212,-170,70,18);c.progressFill.pivot=new Vector2(0,.5f);c.fullProgressWidth=332;Flat(c.progressFill.GetComponent<Image>(),new Color(.15f,.53f,1),1);
            var arrow=F(c.transform,"Button_OpenDetails");R(arrow,184,-123,70,70);Flat(arrow.GetComponent<Image>(),new Color(.925f,.928f,1),.6f);
        }
        foreach(var item in s.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("ThemePreview_")).ToArray())
        {
            var index=item.name.EndsWith("0")?0:1;R(item,index==0?-253:253,-837,490,232);Flat(item.GetComponent<Image>(),Paper,.65f);
            Move(item,"Landscape",0,34,487,162);var info=F(item,"Info");R(info,0,-65,490,102);Flat(info.GetComponent<Image>(),Paper,.65f);
        }
    }
    static void Victory()
    {
        var s=S(4);Title(s,0,885,790,170,132);
        var quote=F(s,"QuoteCard");R(quote,0,-55,940,490);Flat(quote.GetComponent<Image>(),new Color(1,.966f,.863f),.55f);
        R(game.victoryQuote.transform,0,35,820,282);R(game.victorySource.transform,0,-179,830,61);
        Move(quote,"AuthorDivider",0,-135,380,3);Move(quote,"QuoteMark",0,205,110,88);Move(quote,"Quote_Feather",419,-179,110,130);
        foreach(var n in new[]{"Laurel_Left","Laurel_Right"}){var t=F(quote,n);var r=(RectTransform)t;r.anchoredPosition=new Vector2(r.anchoredPosition.x,-35);r.sizeDelta=new Vector2(85,220);}
        foreach(var child in F(s,"RewardsPanel").Cast<Transform>())if(child.name.StartsWith("Reward_"))Flat(child.GetComponent<Image>(),Paper,.6f);
    }
    static void Energy()
    {
        var s=S(5);Title(s,0,750,740,246,85,"Перья\nзакончились");Remove(s,"Text_RestCaption");
        Move(s,"RestoreCard",0,-230,930,380);
    }
    static void OtherScreens()
    {
        var a=S(8);Title(a,0,712,720,145,92);Remove(a,"List_Backdrop");
        foreach(var card in game.achievementCards)Flat(card.GetComponent<Image>(),Paper,.7f);
        foreach(var b in game.achievementTabs)Flat(b.GetComponent<Image>(),Color.white,.7f);
        Title(S(9),0,1000,490,110,84);
        foreach(var n in new[]{"Section_Feathers","Section_Hints","Section_Special"})Flat(S(9),n,Paper,.6f);
        Title(S(10),0,957,660,115,76);
        var defeat=S(12);Title(defeat,0,750,770,200,78);
        Move(defeat,"DefeatCard",0,-360,940,610);R(game.defeatMessage.transform,0,230,840,100);R(game.defeatQuote.transform,0,-5,825,330);R(game.defeatSource.transform,0,-244,820,70);
        Move(defeat,"Button_Retry",0,-766,840,140);
        var back=defeat.GetComponentsInChildren<Button>(true).Single(b=>b.name!="Button_Retry");R(back.transform,0,-949,790,130);
        Move(defeat,"Text_Encouragement",0,-1096,900,56);
    }
    static void Gameplay()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity",OpenSceneMode.Additive);
        var link=UnityEngine.Object.FindFirstObjectByType<GameplaySceneLink>();
        foreach(var board in new[]{link.classicBoard,link.turboBoard})
        {
            var s=board.transform;Background(s,"gameplay");Remove(s,"OwlMascot_Small","Decor_Lantern");F(s,"Owl_Advice").gameObject.SetActive(false);
            var panel=F(s,"PuzzlePanel");R(panel,0,-70,1010,900);
            Move(panel,"Text_Instruction",0,368,850,60);Move(panel,"Text_ReadingProgress",0,313,850,44);Move(panel,"ReadingProgress_Back",0,280,840,10);
            Move(panel,"ReadingViewport",0,-12,942,544);Move(panel,"ReadingScrollTrack",482,-12,8,544);
            Move(panel,"Text_ScrollTip",0,-305,840,40);Move(panel,"Text_RevealFeedback",0,-376,870,104);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);EditorSceneManager.CloseScene(scene,true);
    }
}
