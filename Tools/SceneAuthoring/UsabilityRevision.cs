using System;
using System.Linq;
using Erudition;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One-off authoring on the before-usability-pass backup. Never shipped inside Assets.
public static class UsabilityRevision
{
    static CryptogramGame game;
    static Font body, bold;
    static Sprite rounded;
    static readonly Color Ink = new Color(.19f,.14f,.27f);
    static readonly Color Cream = new Color(1,.975f,.90f);
    static Transform Find(Transform root,string name) => root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
    static Transform Maybe(Transform root,string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
    static Transform Screen(int i) => game.screens[i].transform;
    static void Place(Transform t,float x,float y,float w,float h)
    { var r=(RectTransform)t; r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f); r.anchoredPosition=new Vector2(x,y); r.sizeDelta=new Vector2(w,h); r.localScale=Vector3.one; }
    static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    { var go=new GameObject(name,typeof(RectTransform)); go.layer=5; go.transform.SetParent(parent,false); Place(go.transform,x,y,w,h); return (RectTransform)go.transform; }
    static void Surface(Image im,Color color,float radius=2.8f)
    { im.sprite=rounded; im.type=Image.Type.Sliced; im.preserveAspect=false; im.pixelsPerUnitMultiplier=radius; im.color=color; }
    static Image Card(Transform parent,string name,float x,float y,float w,float h,Color color)
    { var r=Rect(parent,name,x,y,w,h); var im=r.gameObject.AddComponent<Image>(); Surface(im,color); im.raycastTarget=false; return im; }
    static Text Text(Transform parent,string name,string text,float x,float y,float w,float h,int size,bool strong=false)
    { var r=Rect(parent,name,x,y,w,h); var t=r.gameObject.AddComponent<Text>(); t.font=strong?bold:body; t.fontSize=size; t.color=Ink; t.alignment=TextAnchor.MiddleCenter; t.text=text; t.raycastTarget=false; t.supportRichText=true; return t; }
    static void Remove(Transform root,string name) { var t=Maybe(root,name); if(t!=null) UnityEngine.Object.DestroyImmediate(t.gameObject); }
    static void Move(Transform root,string name,float x,float y,float w,float h) => Place(Find(root,name),x,y,w,h);
    static void FitText(Text t,int minimum,int maximum)
    { t.resizeTextForBestFit=true; t.resizeTextMinSize=minimum; t.resizeTextMaxSize=maximum; t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Truncate; }

    public static void Run()
    {
        SmoothAssets();
        var main=EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        game=UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        body=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Nunito-SemiBold.ttf");
        bold=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Nunito-Black.ttf");
        rounded=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/SpriteSheets/reference_controls_v2.png").OfType<Sprite>().Single(s=>s.name=="Capsule");
        Remove(Screen(0),"Button_StatisticsShortcut"); Remove(Screen(0),"Top_Sparkles");
        game.mainErudition.text="0";
        Text(Screen(0),"Text_Welcome","Каждая история — маленькое открытие",0,555,875,58,28);
        foreach(var n in new[]{"Button_Classic","Button_Turbo"})
        {
            var button=Find(Screen(0),n); var subtitle=Find(button,"Text_Subtitle").GetComponent<Text>();
            subtitle.text=n.Contains("Classic")?"Истории и цитаты\nот простого к сложному":"Короткая разминка\nна пару минут";
            subtitle.fontSize=39;
        }
        foreach(var board in new[]{game.classicBoard,game.turboBoard}) BuildBoard(board);
        FixRewards();
        CollectionReader();
        foreach(var label in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(label.font==null) label.font=body;
            if(label.name=="Text_Label" && label.GetComponentInParent<Button>()!=null) FitText(label,Mathf.Max(22,label.fontSize-6),label.fontSize);
            if(label.name=="Text_Letter") { label.horizontalOverflow=HorizontalWrapMode.Overflow; label.verticalOverflow=VerticalWrapMode.Overflow; }
        }
        foreach(var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(button.GetComponent<LetterCellView>()!=null) continue;
            if(button.GetComponent<UiWarmth>()==null) button.gameObject.AddComponent<UiWarmth>();
            var colors=button.colors; colors.fadeDuration=.12f; colors.highlightedColor=new Color(1,1,.99f); colors.pressedColor=new Color(.89f,.87f,.91f); colors.disabledColor=Color.white; button.colors=colors;
        }
        foreach(var name in new[]{"OwlMascot","OwlMascot_Happy","OwlMascot_Sad"})
        {
            var owl=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
            if(owl!=null) owl.gameObject.AddComponent<UiWarmth>().breathe=true;
        }
        var overlay=UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(i=>i.name=="Background_SoftOverlay");
        overlay.color=new Color(1,.96f,.87f,.12f);
        game.classicBoard.StartPuzzle(PuzzleGenerator.Generate(PuzzleMode.Classic,0,0),24,new PuzzleProgress{remainingHearts=5},0,2);
        game.turboBoard.StartPuzzle(PuzzleGenerator.Generate(PuzzleMode.Turbo,0,0),504,new PuzzleProgress{remainingHearts=3},0,2);
        foreach(var s in game.screens) s.SetActive(false); game.screens[0].SetActive(true);
        ReferencePolish.CreatePrefabs(game);
        PrefabUtility.SaveAsPrefabAsset(game.screens[1],"Assets/Prefabs/CollectionDetails.prefab");
        PrefabUtility.SaveAsPrefabAsset(game.classicBoard.GetComponentsInChildren<KeyboardKeyView>(true).First().gameObject,"Assets/Prefabs/KeyboardKey.prefab");
        SplitGameplay(main);
        AssetDatabase.SaveAssets();
        Debug.Log("USABILITY_AUTHORING_COMPLETE");
        UsabilityTests.Run();
    }

    static void SmoothAssets()
    {
        foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Art"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid); var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null || importer.textureType!=TextureImporterType.Sprite) continue;
            importer.filterMode=FilterMode.Trilinear; importer.mipmapEnabled=true; importer.mipMapsPreserveCoverage=false;
            importer.alphaIsTransparency=true; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=4096; importer.anisoLevel=2; importer.npotScale=TextureImporterNPOTScale.None;
            foreach(var platform in new[]{"Standalone","Android","iPhone"}) importer.ClearPlatformTextureSettings(platform);
            importer.SaveAndReimport();
        }
    }

    static void BuildBoard(PuzzleBoard board)
    {
        var s=board.transform; var classic=board==game.classicBoard;
        Move(s,"OwlMascot_Small",-345,647,300,300); Remove(s,"Decor_Lantern");
        Move(s,"HeartsContainer",0,860,classic?545:370,93);
        var bubble=Card(s,"Owl_Advice",140,652,685,235,Cream);
        var guide=Text(bubble.transform,"Text_OwlAdvice","Я Совушка. Давай начнём!\nОдно число — одна буква.\nБольшую часть я уже открыла.",0,0,615,193,31);
        var panel=Find(s,"PuzzlePanel"); Place(panel,0,-25,1010,1040);
        // Use the existing high-resolution paper; give the text a quiet, stable reading area.
        panel.GetComponent<Image>().pixelsPerUnitMultiplier=1.5f;
        var oldContent=Find(panel,"PuzzleContent");
        UnityEngine.Object.DestroyImmediate(oldContent.gameObject);
        Remove(panel,"Text_RevealFeedback");
        var instruction=Text(panel,"Text_Instruction","Найди 2 буквы",0,429,850,60,35,true);
        var progress=Text(panel,"Text_ReadingProgress","Открыто 0 букв",0,371,850,44,26);
        progress.color=new Color(.5f,.39f,.26f);
        var progressBack=Card(panel,"ReadingProgress_Back",0,332,840,10,new Color(.83f,.77f,.64f,.7f));
        var progressFill=Card(progressBack.transform,"ReadingProgress_Fill",0,0,840,10,new Color(.26f,.62f,.49f));
        progressBack.sprite=null; progressFill.sprite=null;
        progressFill.type=Image.Type.Filled; progressFill.fillMethod=Image.FillMethod.Horizontal; progressFill.fillOrigin=0;
        var viewport=Rect(panel,"ReadingViewport",0,-13,942,644); viewport.gameObject.AddComponent<RectMask2D>();
        var hit=viewport.gameObject.AddComponent<Image>(); hit.color=new Color(1,1,1,.001f); hit.raycastTarget=true;
        var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal=false; scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped; scroll.inertia=true; scroll.decelerationRate=.09f; scroll.scrollSensitivity=42; scroll.viewport=viewport;
        var content=Rect(viewport,"PuzzleContent",0,0,928,644); content.anchorMin=content.anchorMax=new Vector2(.5f,1); content.pivot=new Vector2(.5f,1); scroll.content=content;
        var scrollbarRoot=Card(panel,"ReadingScrollTrack",482,-13,8,644,new Color(.82f,.76f,.64f,.3f));
        var scrollbar=scrollbarRoot.gameObject.AddComponent<Scrollbar>(); scrollbar.direction=Scrollbar.Direction.BottomToTop;
        var slide=Rect(scrollbarRoot.transform,"SlidingArea",0,0,8,644); slide.anchorMin=Vector2.zero; slide.anchorMax=Vector2.one; slide.sizeDelta=Vector2.zero;
        var handle=Card(slide,"Handle",0,0,8,50,new Color(.55f,.43f,.3f,.65f)); scrollbar.handleRect=handle.rectTransform; scrollbar.targetGraphic=handle; scroll.verticalScrollbar=scrollbar; scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        Text(panel,"Text_ScrollTip","Длинный текст можно листать вверх и вниз",0,-358,840,42,23).color=new Color(.56f,.43f,.31f);
        var feedback=Text(panel,"Text_RevealFeedback","Выбери подсвеченное место, затем букву на клавиатуре",0,-431,870,116,29,true);
        var cells=new LetterCellView[classic?480:192];
        for(var i=0;i<cells.Length;i++)
        {
            var root=Rect(content,"Cell_"+i.ToString("000"),0,0,56,100);
            var click=root.gameObject.AddComponent<Image>(); click.color=new Color(1,1,1,.001f); click.raycastTarget=true;
            var button=root.gameObject.AddComponent<Button>(); button.targetGraphic=click; button.transition=Selectable.Transition.None;
            var glow=Card(root,"SelectionGlow",0,0,55,98,new Color(1,.78f,.3f,.6f)); glow.enabled=false; glow.pixelsPerUnitMultiplier=5;
            var letter=Text(root,"Text_Letter","",0,15,56,64,43,true);
            var number=Text(root,"Text_Code","",0,-34,56,38,24);
            var line=Card(root,"Underline",0,-12,39,4,new Color(.3f,.25f,.35f));
            line.sprite=null;
            var cell=root.gameObject.AddComponent<LetterCellView>(); cell.Configure(board,i,letter,number,line,button); cell.ConfigureSelection(glow);
            var so=new SerializedObject(cell); so.FindProperty("revealedColor").colorValue=Ink; so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(button.onClick,cell.Press); cells[i]=cell;
            root.gameObject.SetActive(false);
        }
        var keyboard=Find(s,"Keyboard"); Place(keyboard,0,-797,1030,480);
        var backdrop=Card(keyboard,"Keyboard_Backdrop",0,5,1060,486,new Color(.33f,.25f,.34f,.16f)); backdrop.transform.SetAsFirstSibling();
        var keys=board.GetComponentsInChildren<KeyboardKeyView>(true);
        foreach(var key in keys)
        {
            var bg=key.GetComponent<Image>(); Surface(bg,new Color(1,.99f,.96f),5);
            key.CaptureNormalColor(); var label=key.GetComponentInChildren<Text>(); label.fontSize=41;
            var strike=Card(key.transform,"Used_Strike",0,0,53,4,new Color(.43f,.4f,.43f,.75f)); strike.enabled=false;
            key.ConfigureUsage(strike,label,key.GetComponent<Button>()); key.SetUsed(false);
        }
        foreach(var control in new[]{"Button_Check","Button_ClearSelection"})
        { var t=Find(keyboard,control); Surface(t.GetComponent<Image>(),new Color(.58f,.54f,.68f),4); }
        Text(s,"Text_KeyboardLegend","Зачёркнутые буквы уже открыты в тексте",0,-1090,930,44,26).color=new Color(.23f,.16f,.25f);
        var hearts=Find(s,"HeartsContainer").Cast<Transform>().Select(t=>t.gameObject).ToArray();
        board.Configure(game,classic?PuzzleMode.Classic:PuzzleMode.Turbo,16,cells,keys,hearts,instruction,feedback,Find(Find(s,"Button_Hint"),"Text_Label").GetComponent<Text>());
        board.ConfigureReading(scroll,content,progress,progressFill,guide);
    }

    static void FixRewards()
    {
        var s=Screen(4);
        var rewards=Find(s,"RewardsPanel"); Place(rewards,0,-485,940,255);
        var left=Find(rewards,"Reward_EruditionCard"); var right=Find(rewards,"Reward_CollectionCard");
        Place(left,-320,0,296,250); Place(right,0,0,296,250);
        Move(left,"Icon_Bulb",0,65,95,100); Move(right,"Icon_Books",0,63,130,100); Move(right,"Icon_Star",70,97,47,47);
        Place(game.victoryEruditionReward.transform,0,-53,270,125); Place(game.victoryCollectionReward.transform,0,-53,270,125);
        game.victoryEruditionReward.fontSize=26; game.victoryCollectionReward.fontSize=26;
        var coinCard=Card(rewards,"Reward_CoinsCard",320,0,296,250,Cream);
        var coin=Find(s,"Reward_Coin"); coin.SetParent(coinCard.transform,false); Place(coin,0,65,88,88);
        game.presentation.coinsReward.transform.SetParent(coinCard.transform,false); Place(game.presentation.coinsReward.transform,0,-51,270,116); game.presentation.coinsReward.fontSize=34;
        var quote=Find(s,"QuoteCard"); Place(quote,0,75,940,600);
        Place(game.victoryQuote.transform,0,48,820,390); FitText(game.victoryQuote,28,47);
        Place(game.victorySource.transform,0,-205,830,75); FitText(game.victorySource,25,33);
        Move(quote,"AuthorDivider",0,-151,380,3); Move(quote,"QuoteMark",0,254,110,100);
        Move(quote,"Quote_Feather",419,-208,110,145);
        Move(quote,"Laurel_Left",-473,-60,90,245); Move(quote,"Laurel_Right",473,-60,90,245);
        Find(quote,"Laurel_Right").localScale=new Vector3(-1,1,1);
        Move(s,"OwlMascot_Happy",0,642,530,485);
        Move(s,"Celebration_Left",-320,655,135,180); Move(s,"Celebration_Right",331,610,135,180);
        Place(game.presentation.likeButton.transform,0,-668,430,65);
        Move(s,"Button_RewardedAd",0,-819,940,190); Place(game.victoryBonusNote.transform,0,-724,900,42);
        Move(s,"Button_Continue",0,-1044,940,146);
        // Failure must also accommodate the new long paragraphs.
        var defeat=Screen(12); Move(defeat,"OwlMascot_Defeat",0,590,465,420);
        Move(defeat,"DefeatCard",0,-30,940,740);
        var title=Find(defeat,"Text_Title").GetComponent<Text>(); title.text="Попробуем ещё?"; title.font=bold; title.fontSize=67;
        Place(game.defeatMessage.transform,0,280,840,95); game.defeatMessage.fontSize=30;
        game.defeatMessage.text="Сердечки закончились, но всё получится.\nТеперь история открыта — попробуем ещё?";
        FitText(game.defeatQuote,26,43); Place(game.defeatQuote.transform,0,-5,825,400);
        FitText(game.defeatSource,24,31); Place(game.defeatSource.transform,0,-278,820,75);
        Move(defeat,"Button_Retry",0,-570,840,140);
        var menu=defeat.GetComponentsInChildren<Button>(true).Single(b=>b.name!="Button_Retry"); Place(menu.transform,0,-770,790,130);
        Text(defeat,"Text_Encouragement","Ошибаться — часть любого открытия",0,-932,900,65,29);
    }

    static void CollectionReader()
    {
        var text=game.presentation.detailBody; var parent=text.transform.parent;
        var viewport=Rect(parent,"QuotesViewport",0,-302,820,718); viewport.gameObject.AddComponent<RectMask2D>();
        var hit=viewport.gameObject.AddComponent<Image>(); hit.color=new Color(1,1,1,.001f); hit.raycastTarget=true;
        var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.horizontal=false; scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=40;
        text.transform.SetParent(viewport,false); var rect=text.rectTransform; rect.anchorMin=new Vector2(0,1); rect.anchorMax=new Vector2(1,1); rect.pivot=new Vector2(.5f,1); rect.anchoredPosition=Vector2.zero; rect.sizeDelta=new Vector2(-24,718);
        text.fontSize=33; text.alignment=TextAnchor.UpperCenter;
        var fit=text.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize; scroll.content=rect; game.presentation.detailScroll=scroll;
        Text(parent,"Text_QuotesScrollHint","Собранные истории можно листать",0,-731,820,50,24).color=new Color(.55f,.44f,.35f);
    }

    static void SplitGameplay(Scene main)
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var root=new GameObject("GameplayCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(GameplaySceneLink)); root.layer=5;
        SceneManager.MoveGameObjectToScene(root,scene);
        var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=10; canvas.pixelPerfect=false;
        var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1080,2340); scaler.matchWidthOrHeight=.5f;
        var safe=Rect(root.transform,"GameplaySafeArea",0,0,1080,2340); safe.gameObject.AddComponent<SafeAreaFitter>();
        var link=root.GetComponent<GameplaySceneLink>(); link.classicBoard=game.classicBoard; link.turboBoard=game.turboBoard;
        link.classicErudition=game.classicErudition; link.turboErudition=game.turboErudition; link.classicErudition.text=link.turboErudition.text="0";
        link.classicScreen=game.screens[2]; link.turboScreen=game.screens[3];
        foreach(var screen in new[]{link.classicScreen,link.turboScreen})
        {
            screen.transform.SetParent(null,false); SceneManager.MoveGameObjectToScene(screen,scene); screen.transform.SetParent(safe,false);
            var r=screen.GetComponent<RectTransform>(); r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; r.localScale=Vector3.one; screen.SetActive(false);
        }
        link.actions=root.GetComponentsInChildren<UiAction>(true); foreach(var action in link.actions) action.SetGame(null);
        link.classicBoard.SetGame(null); link.turboBoard.SetGame(null);
        game.screens[2]=null; game.screens[3]=null; game.classicBoard=null; game.turboBoard=null; game.classicErudition=null; game.turboErudition=null;
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/GameplayScene.unity");
        EditorSceneManager.SaveScene(main);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity",true),new EditorBuildSettingsScene("Assets/Scenes/GameplayScene.unity",true)};
    }
}
