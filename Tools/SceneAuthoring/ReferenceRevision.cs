using System;
using System.Linq;
using Erudition;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-off editor authoring, run only against the backed-up pre-revision scene.
public static class ReferenceRevision
{
    static CryptogramGame game;
    static ReferenceUiPresenter ui;
    static Font body, heavy;
    static readonly Color Ink = new Color(.105f,.055f,.23f);
    static readonly Color Muted = new Color(.43f,.44f,.59f);
    static Sprite Round => Sheet("Assets/Art/SpriteSheets/reference_controls_v2.png","Capsule");
    static Sprite Circle => Sheet("Assets/Art/SpriteSheets/reference_controls_v2.png","CreamCircle");
    static Transform S(int index) => game.screens[index].transform;
    static Sprite Asset(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    static Sprite Sheet(string path, string name) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single(s => s.name == name);
    static Sprite Icon(string name) => Sheet("Assets/Art/SpriteSheets/ui_icons_reference.png", name);
    static Sprite Extra(string name) => Sheet("Assets/Art/SpriteSheets/reference_icons_v2.png", name);
    static Sprite Decor(string name) => Sheet("Assets/Art/SpriteSheets/reference_decor_v2.png", name);
    static Sprite Surface(string name) => Sheet("Assets/Art/SpriteSheets/ui_surfaces_reference.png", name);
    static Transform F(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    static Transform Maybe(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
    static Text T(Transform root, string name) => F(root, name).GetComponent<Text>();
    static void Delete(Transform root, string name) { var t=Maybe(root,name); if(t!=null) UnityEngine.Object.DestroyImmediate(t.gameObject); }
    static void R(Transform t,float x,float y,float w,float h)
    { var r=t.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f); r.anchoredPosition=new Vector2(x,y); r.sizeDelta=new Vector2(w,h); }
    static void Style(Image image, Color color, float round=.45f)
    { image.sprite=Round; image.type=Image.Type.Sliced; image.preserveAspect=false; image.pixelsPerUnitMultiplier=round*6.25f; image.color=color; }
    static Image Pic(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)); go.layer=5; go.transform.SetParent(parent,false);
        R(go.transform,x,y,w,h); var im=go.GetComponent<Image>(); im.sprite=sprite; im.preserveAspect=true; im.raycastTarget=false; return im;
    }
    static Image Card(Transform parent,string name,float x,float y,float w,float h,Color? color=null)
    { var im=Pic(parent,name,Round,x,y,w,h); Style(im,color??new Color(1,.985f,.955f)); return im; }
    static Text Label(Transform parent,string name,string text,float x,float y,float w,float h,int size,bool bold=false,Color? color=null)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)); go.layer=5; go.transform.SetParent(parent,false); R(go.transform,x,y,w,h);
        var t=go.GetComponent<Text>(); t.text=text; t.font=bold?heavy:body; t.fontSize=size; t.color=color??Ink; t.alignment=TextAnchor.MiddleCenter;
        t.raycastTarget=false; t.supportRichText=true; t.verticalOverflow=VerticalWrapMode.Truncate; return t;
    }
    static Button Action(Transform target,UiActionKind kind,int value=0)
    {
        var image=target.GetComponent<Image>(); image.raycastTarget=true;
        var button=target.GetComponent<Button>()??target.gameObject.AddComponent<Button>(); button.targetGraphic=image;
        var action=target.GetComponent<UiAction>()??target.gameObject.AddComponent<UiAction>(); action.Configure(game,kind,value);
        button.onClick=new Button.ButtonClickedEvent(); UnityEventTools.AddPersistentListener(button.onClick,action.Press);
        var colors=ColorBlock.defaultColorBlock; colors.pressedColor=new Color(.8f,.85f,.96f); button.colors=colors; return button;
    }
    static Button Button(Transform parent,string name,string label,UiActionKind kind,int parameter,float x,float y,float w,float h,Sprite sprite=null)
    {
        var im=Card(parent,name,x,y,w,h); if(sprite!=null){im.sprite=sprite;im.pixelsPerUnitMultiplier=2;}
        var b=Action(im.transform,kind,parameter); var text=Label(im.transform,"Text_Label",label,0,0,w-20,h-6,36,true,sprite==null?Ink:Color.white); text.verticalOverflow=VerticalWrapMode.Overflow; return b;
    }
    static void Leaves(Transform parent,float x,float y,float w,float h)
    {
        Pic(parent,"Laurel_Left",Decor("Laurel"),-x,y,w,h);
        var right=Pic(parent,"Laurel_Right",Decor("Laurel"),x,y,w,h); right.transform.localScale=new Vector3(-1,1,1);
    }
    static void Header(int index,string title,float y=980,float width=870,int size=76,bool crown=true)
    {
        var s=S(index); var banner=F(s,"TitleParchment"); R(banner,0,y,width,210);
        var text=T(s,"Text_Title"); text.text=title; text.font=heavy; text.fontSize=size; text.color=Ink; R(text.transform,0,y+12,width-90,150);
        Leaves(banner, width*.39f,-42,105,125);
        if(crown) Pic(s,"Header_Crown",Icon("Crown"),0,Mathf.Min(y+130,1070),125,112);
    }
    static void CircleButton(Transform button)
    { var im=button.GetComponent<Image>(); im.sprite=button.name.Contains("Details")?Sheet("Assets/Art/SpriteSheets/reference_controls_v2.png","LavenderCircle"):Circle; im.type=Image.Type.Simple; im.color=Color.white; im.preserveAspect=true; }

    public static void Run()
    {
        Import(); EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity"); game=UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        body=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Nunito-SemiBold.ttf"); heavy=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Nunito-Black.ttf");
        if(body==null||heavy==null) throw new Exception("Missing bundled Nunito fonts");
        ui=game.gameObject.AddComponent<ReferenceUiPresenter>(); game.presentation=ui;
        foreach(var text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        { text.font=text.fontStyle==FontStyle.Bold?heavy:body; text.fontStyle=FontStyle.Normal; text.supportRichText=true; }
        Main(); Boards(); Victory(); NoFeathers(); Statistics(); Levels(); Achievements(); Shop(); Details(); Navigation(); Popup();
        foreach(var action in S(11).GetComponentsInChildren<UiAction>(true))
            if(action.name.Contains("Mode")) { action.Configure(game,UiActionKind.Levels); action.GetComponentInChildren<Text>().text="Уровни"; }
        Header(10,"Настройки",985,820,76,false);
        game.classicStartLabel.text="Классика";
        game.turboStartLabel.text="Турбо";
        ui.Refresh(new GameSave(),6,null);
        game.classicBoard.StartPuzzle(game.library.entries[0],0,new PuzzleProgress { remainingHearts=5 },101,2);
        var turboIndex=Array.FindIndex(game.library.entries,e=>e.mode==PuzzleMode.Turbo);
        game.turboBoard.StartPuzzle(game.library.entries[turboIndex],turboIndex,new PuzzleProgress { remainingHearts=3 },101,2);
        foreach(var screen in game.screens) screen.SetActive(false); game.screens[0].SetActive(true);
        ReferencePolish.CreatePrefabs(game);
        PrefabUtility.SaveAsPrefabAsset(S(1).gameObject,"Assets/Prefabs/CollectionDetails.prefab");
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene); EditorSceneManager.SaveScene(game.gameObject.scene); AssetDatabase.SaveAssets();
        Debug.Log("REFERENCE_REVISION_SAVED objects="+game.gameObject.scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Transform>(true).Length));
        RuntimeSmoke.RunWithCapture();
    }

    static void Main()
    {
        var s=S(0); Delete(s,"Button_ModeSelection");
        var b=F(s,"EruditionBlock"); R(b,0,755,840,230); R(F(b,"Text_Title"),0,25,770,125); T(b,"Text_Title").font=heavy; T(b,"Text_Title").fontSize=98;
        R(F(b,"Icon_Crown"),0,165,145,135); var badge=F(b,"ValueBadge"); R(badge,0,-100,270,115); Style(badge.GetComponent<Image>(),new Color(1,.94f,.76f));
        R(F(b,"Text_Value"),0,-100,245,108); game.mainErudition.font=heavy; game.mainErudition.fontSize=90; Leaves(b,220,-108,145,180);
        var owl=F(s,"OwlMascot"); R(owl,0,190,670,665);
        var books=Pic(s,"Decor_Books",Decor("Books"),0,-95,750,285); books.transform.SetSiblingIndex(owl.GetSiblingIndex());
        Pic(s,"Decor_Cat",Decor("Cat"),362,-68,330,250);
        Pic(s,"Decor_Inkwell",Decor("Ink"),-395,-45,220,340);
        foreach(var spec in new[]{("Button_Classic",-390f),("Button_Turbo",-720f)})
        {
            var button=F(s,spec.Item1); R(button,0,spec.Item2,960,305);
            var text=T(button,"Text_Label"); text.font=heavy; text.fontSize=72; R(text.transform,78,67,590,125);
            var subtitle=Label(button,"Text_Subtitle",spec.Item1.Contains("Classic")?"Длинные цитаты,\nстихи и тексты":"Короткие фразы\nи выражения",78,-58,590,137,44,false,Color.white); subtitle.alignment=TextAnchor.MiddleLeft;
            var arrow=F(button,"Icon_Arrow"); R(arrow,414,0,70,100);
            var bubble=Pic(button,"ArrowCircle",Circle,414,0,115,115); bubble.color=new Color(.13f,.16f,.5f,.22f); bubble.transform.SetSiblingIndex(arrow.GetSiblingIndex());
        }
        var energy=F(s,"FeatherEnergy"); R(energy,0,1010,440,110); Style(energy.GetComponent<Image>(),new Color(.27f,.2f,.31f),.35f);
        R(F(energy,"Icon_Feather"),-202,12,130,180); R(game.mainFeathers.transform,5,0,210,105); game.mainFeathers.fontSize=60;
        var plus=F(energy,"Button_AddEnergy"); CircleButton(plus); R(plus,170,0,94,94); T(plus,"Text_Label").fontSize=70;
        foreach(var name in new[]{"Button_Settings","Button_StatisticsShortcut"}) { var t=F(s,name); CircleButton(t); R(t,name.Contains("Settings")?-419:419,990,133,133); }
        Pic(s,"Top_Sparkles",Extra("Sparkles"),270,1010,110,150);
    }

    static void Boards()
    {
        for(var n=2;n<=3;n++)
        {
            var s=S(n); R(F(s,"OwlMascot_Small"),0,565,640,570);
            var owl=F(s,"OwlMascot_Small"); var lantern=Pic(s,"Decor_Lantern",Decor("Lantern"),-395,410,260,315); lantern.transform.SetSiblingIndex(owl.GetSiblingIndex());
            var panel=F(s,"PuzzlePanel"); R(panel,0,-15,1000,950); panel.GetComponent<Image>().pixelsPerUnitMultiplier=.9f;
            var instruction=Maybe(panel,"Text_Instruction"); if(instruction!=null) instruction.gameObject.SetActive(false);
            var feedback=F(panel,"Text_RevealFeedback"); R(feedback,0,-395,870,50); feedback.GetComponent<Text>().fontSize=26;
            var content=F(panel,"PuzzleContent"); R(content,0,60,940,750);
            foreach(var cell in s.GetComponentsInChildren<LetterCellView>(true))
            {
                var letter=T(cell.transform,"Text_Letter"); letter.font=heavy; letter.fontSize=43;
                var code=T(cell.transform,"Text_Code"); code.fontSize=25; code.color=new Color(.31f,.4f,.53f);
                var so=new SerializedObject(cell); so.FindProperty("revealedColor").colorValue=new Color(.08f,.12f,.31f); so.ApplyModifiedPropertiesWithoutUndo();
            }
            var hearts=F(s,"HeartsContainer"); R(hearts,0,860,n==2?545:370,93); Style(hearts.GetComponent<Image>(),new Color(.34f,.19f,.12f,.6f),.25f);
            for(var i=0;i<hearts.childCount;i++) R(hearts.GetChild(i),(i-(hearts.childCount-1)/2f)*88,0,82,82);
            var back=F(s,"Button_BackOrPause"); CircleButton(back); R(back,-435,1010,116,116);
            var hint=F(s,"Button_Hint"); CircleButton(hint); R(hint,425,1000,130,130);
            var bubble=Pic(hint,"Hint_CountBadge",Circle,43,48,61,61); bubble.color=new Color(.1f,.48f,1); F(hint,"Text_Label").SetAsLastSibling(); T(hint,"Text_Label").color=Color.white; T(hint,"Text_Label").font=heavy; R(F(hint,"Text_Label"),43,49,55,50);
            var keyboard=F(s,"Keyboard"); R(keyboard,0,-780,1030,480);
            for(var row=0;row<3;row++)
            {
                var root=F(keyboard,"Row_0"+(row+1)); R(root,0,155-row*141,1015,126);
                var keys=root.GetComponentsInChildren<KeyboardKeyView>(true);
                for(var i=0;i<keys.Length;i++) { R(keys[i].transform,(i-(keys.Length-1)/2f)*84,0,78,125); keys[i].GetComponentInChildren<Text>().font=heavy; keys[i].GetComponentInChildren<Text>().fontSize=42; }
            }
            var check=F(keyboard,"Button_Check"); check.SetParent(F(keyboard,"Row_03"),false); R(check,455,0,107,125); Style(check.GetComponent<Image>(),new Color(.35f,.4f,.62f));
            T(check,"Text_Label").text="✓"; T(check,"Text_Label").fontSize=68; R(F(check,"Text_Label"),0,0,95,110);
            var clear=Button(F(keyboard,"Row_03"),"Button_ClearSelection","⌫",UiActionKind.ClearSelection,0,-455,0,107,125); Style(clear.GetComponent<Image>(),new Color(.35f,.4f,.62f)); clear.GetComponentInChildren<Text>().fontSize=56; clear.GetComponentInChildren<Text>().color=Color.white;
        }
    }

    static void Victory()
    {
        var s=S(4); Header(4,"Отлично!",940,940,106);
        R(F(s,"OwlMascot_Happy"),0,565,750,680);
        Pic(s,"Celebration_Left",Extra("Sparkles"),-360,650,160,270); Pic(s,"Celebration_Right",Extra("Sparkles"),375,475,150,260);
        var quote=F(s,"QuoteCard"); R(quote,0,30,900,475); Style(quote.GetComponent<Image>(),new Color(1,.965f,.83f),.25f);
        game.victoryQuote.font=heavy; game.victoryQuote.fontSize=50; R(game.victoryQuote.transform,0,65,760,265);
        game.victorySource.fontSize=35; game.victorySource.alignment=TextAnchor.MiddleCenter; game.victorySource.color=new Color(.6f,.34f,.12f); R(game.victorySource.transform,0,-132,780,65);
        var quoteMark=Label(quote,"QuoteMark","“",0,188,110,130,95,true,new Color(1,.72f,.25f)); quoteMark.verticalOverflow=VerticalWrapMode.Overflow;
        Card(quote,"AuthorDivider",0,-76,380,3,new Color(.92f,.75f,.47f)); Leaves(quote,445,0,130,380); Pic(quote,"Quote_Feather",Icon("Feather"),390,-155,170,225);
        var rewards=F(s,"RewardsPanel"); R(rewards,0,-405,830,295); rewards.GetComponent<Image>().color=Color.clear;
        var left=Card(rewards,"Reward_EruditionCard",-212,0,395,285); var right=Card(rewards,"Reward_CollectionCard",212,0,395,285);
        game.victoryEruditionReward.transform.SetParent(left.transform,false); R(game.victoryEruditionReward.transform,0,-63,360,135); game.victoryEruditionReward.font=heavy; game.victoryEruditionReward.fontSize=30; game.victoryEruditionReward.color=Ink;
        game.victoryCollectionReward.transform.SetParent(right.transform,false); R(game.victoryCollectionReward.transform,0,-63,360,135); game.victoryCollectionReward.font=heavy; game.victoryCollectionReward.fontSize=30; game.victoryCollectionReward.color=Ink;
        Pic(left.transform,"Icon_Bulb",Icon("Bulb"),0,68,135,135); Pic(right.transform,"Icon_Books",Decor("Books"),0,67,185,135); Pic(right.transform,"Icon_Star",Extra("Star"),85,112,65,65);
        ui.coinsReward=Label(s,"Reward_Coins","+30 монет",-255,-580,310,52,29,true); Pic(s,"Reward_Coin",Icon("Coin"),-415,-580,50,50);
        ui.likeButton=Button(s,"Button_LikeQuote","Мне нравится",UiActionKind.LikeQuote,0,252,-580,350,64); ui.likeLabel=ui.likeButton.GetComponentInChildren<Text>(); ui.likeLabel.fontSize=29; Pic(ui.likeButton.transform,"Heart",Icon("Heart"),-150,0,38,38);
        var ad=F(s,"Button_RewardedAd"); R(ad,0,-745,970,205); var text=T(ad,"Text_Label"); text.text="Посмотреть рекламу\nи получить бонус"; text.font=heavy; text.fontSize=41; R(text.transform,15,0,700,150);
        Pic(ad,"Video",Extra("Video"),-390,0,120,125); Pic(ad,"Gift",Extra("Gift"),398,0,125,135);
        R(F(s,"Button_Continue"),0,-970,950,145); T(F(s,"Button_Continue"),"Text_Label").font=heavy; T(F(s,"Button_Continue"),"Text_Label").fontSize=55;
        R(game.victoryBonusNote.transform,0,-650,950,47); game.victoryBonusNote.fontSize=27;
    }

    static void NoFeathers()
    {
        var s=S(5); Header(5,"Перья\nзакончились",800,920,79,false);
        R(F(s,"TitleParchment"),0,800,960,305); R(F(s,"Text_Title"),0,808,850,240);
        var bg=Card(s,"FeatherCounter_Back",0,1040,440,108,new Color(.27f,.2f,.31f)); bg.transform.SetAsFirstSibling();
        R(game.noFeathersCounter.transform,5,1040,300,100); game.noFeathersCounter.color=Color.white; game.noFeathersCounter.fontSize=60; game.noFeathersCounterRestored.gameObject.SetActive(false);
        R(F(s,"Icon_EnergyFeather"),-200,1045,125,165); Button(s,"Button_EnergyInfo","+",UiActionKind.Shop,0,169,1040,90,90);
        var owl=F(s,"OwlMascot_Sad"); R(owl,0,335,620,570); var books=Pic(s,"Decor_Books",Decor("Books"),0,85,830,285); books.transform.SetSiblingIndex(owl.GetSiblingIndex()); Pic(s,"Decor_Lantern",Decor("Lantern"),-385,100,240,260);
        var card=F(s,"RestoreCard"); R(card,0,-245,930,400); card.GetComponent<Image>().sprite=Surface("Parchment"); card.GetComponent<Image>().type=Image.Type.Sliced;
        var info=T(card,"Text_Info"); info.font=heavy; info.fontSize=43; info.text="Перья постепенно\nвосстанавливаются."; R(info.transform,0,97,800,140);
        R(F(card,"Text_TimerCaption"),0,-15,700,56); T(card,"Text_TimerCaption").fontSize=32;
        var timer=Card(card,"TimerBadge",0,-112,530,103,new Color(1,.94f,.77f)); timer.transform.SetSiblingIndex(0); Pic(card,"TimerFeather",Icon("Feather"),-170,-112,80,100); R(game.noFeathersTimer.transform,50,-112,335,105); game.noFeathersTimer.font=heavy; game.noFeathersTimer.fontSize=66; Leaves(card,388,-60,110,180);
        var ad=F(s,"Button_RewardedAd"); R(ad,0,-632,970,275); ad.GetComponent<Image>().sprite=Surface("PurpleButton"); var label=T(ad,"Text_Label"); label.text="<size=65>+1 перо</size>\nПосмотреть\nрекламу"; label.font=heavy; label.fontSize=40; R(label.transform,95,0,570,248);
        Pic(ad,"Video",Extra("Video"),-370,0,155,170); Pic(ad,"Feather",Icon("Feather"),-205,0,180,225);
        var shop=F(s,"Button_OpenShop"); R(shop,0,-875,960,165); shop.GetComponent<Image>().sprite=Surface("BlueButton"); T(shop,"Text_Label").fontSize=45; Pic(shop,"Shop",Icon("Shop"),-380,0,100,100);
        var back=F(s,"Button_ReturnMenu"); R(back,0,-1060,650,100); Style(back.GetComponent<Image>(),new Color(.49f,.39f,.34f,.82f)); T(back,"Text_Label").fontSize=34;
        R(game.noFeathersNote.transform,0,-469,900,50); game.noFeathersNote.fontSize=29;
    }

    static void Statistics()
    {
        var s=S(6); Header(6,"Статистика",985,850,83);
        Pic(s,"OwlMascot_Statistics",Asset("Assets/Art/owl_mascot.png"),0,745,480,445);
        Pic(s,"Decor_Books",Decor("Books"),0,565,790,180); Pic(s,"Decor_Cat",Decor("Cat"),358,615,250,200); Pic(s,"Decor_Ink",Decor("Ink"),-387,650,160,250);
        var level=F(s,"EruditionCard"); R(level,0,345,1000,365); Style(level.GetComponent<Image>(),new Color(1,.98f,.9f),.28f);
        Delete(level,"Icon_Crown"); Label(level,"Text_Heading","Уровень эрудиции",-185,115,520,75,44,true);
        ui.statsLevel=game.statsLevel; R(ui.statsLevel.transform,-230,-20,325,150); ui.statsLevel.font=heavy; ui.statsLevel.fontSize=123;
        Pic(level,"LevelLaurel_Left",Decor("Laurel"),-405,-33,130,180); var levelLeaf=Pic(level,"LevelLaurel_Right",Decor("Laurel"),-60,-33,130,180); levelLeaf.transform.localScale=new Vector3(-1,1,1);
        Pic(level,"Crown",Icon("Crown"),225,65,195,145);
        var back=F(level,"Progress_Back"); R(back,215,-65,390,48); Style(back.GetComponent<Image>(),new Color(.77f,.79f,.87f));
        var fill=game.statsProgressFill; R(fill,20,-65,210,48); fill.pivot=new Vector2(0,.5f); Style(fill.GetComponent<Image>(),new Color(.13f,.58f,1)); ui.statsFill=fill;
        ui.statsNext=Label(level,"Text_NextLevel","До следующего уровня: 49",210,-126,425,55,28,false,Muted);
        var names=new[]{"Card_Solved","Card_Accuracy","Card_Streak","Card_Best"}; var titles=new[]{"Решено задач","Верных ответов","Серия побед","Решено сегодня"}; var icons=new[]{"Fire","Target","Star","Calendar"}; var hints=new[]{"Всего заданий","Точность","Лучший результат: 0","Ваша активность"};
        for(var i=0;i<4;i++)
        {
            var card=F(s,names[i]); R(card,i%2==0?-254:254,20-i/2*265,490,245); Style(card.GetComponent<Image>(),new[]{new Color(1,.95f,.88f),new Color(.94f,1,.9f),new Color(.96f,.91f,1),new Color(.9f,.96f,1)}[i]);
            Pic(card,"MetricIcon",Extra(icons[i]),-157,8,136,160);
            var title=T(card,"Text_Label"); title.text=titles[i]; title.font=heavy; title.fontSize=30; R(title.transform,65,70,320,65);
            var value=T(card,"Text_Value"); value.font=heavy; value.fontSize=77; R(value.transform,65,-3,280,100);
            var caption=Label(card,"Text_Caption",hints[i],60,-82,320,60,25,false,Muted);
            if(i==2) ui.statsBest=caption; if(i==3) ui.statsToday=value;
        }
        var graph=F(s,"ActivityGraph"); foreach(Transform child in graph.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        R(graph,0,-705,1000,580); Style(graph.GetComponent<Image>(),new Color(1,.995f,.98f),.3f);
        Label(graph,"Text_Title","Активность",-278,222,390,73,49,true); Label(graph,"Text_Subtitle","Решённые задачи по дням",-198,162,565,55,27,false,Muted);
        ui.periods=new Button[3]; for(var i=0;i<3;i++) { ui.periods[i]=Button(graph,"Period_"+i,new[]{"Неделя","Месяц","Год"}[i],UiActionKind.StatisticsPeriod,i,95+i*145,220,144,67); ui.periods[i].GetComponentInChildren<Text>().fontSize=28; }
        ui.bars=new RectTransform[12]; ui.days=new Text[12]; ui.amounts=new Text[12];
        for(var i=0;i<12;i++) { ui.bars[i]=Card(graph,"ActivityBar_"+i,0,-150,85,5,new Color(.3f,.64f,1)).rectTransform; ui.days[i]=Label(graph,"Day_"+i,"Пн",0,-180,85,45,25,false,Muted); ui.amounts[i]=Label(graph,"Count_"+i,"0",0,-120,85,45,26,true); }
        game.activityBars=Array.Empty<RectTransform>(); game.activityDayLabels=Array.Empty<Text>();
    }

    static void Levels()
    {
        var s=S(7); Header(7,"Уровни",915,840,77); R(F(s,"TitleParchment"),110,915,820,220); R(F(s,"Text_Title"),105,930,730,125); R(F(s,"Header_Crown"),100,1065,115,95);
        Pic(s,"OwlMascot_Collections",Asset("Assets/Art/owl_mascot.png"),-380,905,300,305); Pic(s,"Decor_Books",Decor("Books"),-270,752,480,130); Pic(s,"Decor_Hourglass",Decor("Hourglass"),389,817,210,265);
        var tabs=F(s,"Tabs"); R(tabs,0,625,990,120); Style(tabs.GetComponent<Image>(),new Color(1,1,1,.97f),.3f);
        for(var i=0;i<game.collectionTabs.Length;i++) { var b=game.collectionTabs[i]; R(b.transform,(i-1)*323,0,320,98); Style(b.GetComponent<Image>(),Color.white,.3f); b.GetComponentInChildren<Text>().font=heavy; b.GetComponentInChildren<Text>().fontSize=40; }
        var all=game.authorCards.Concat(game.themeCards).Concat(game.bookCards).ToArray(); ui.collections=all; ui.collectionPictures=new Sprite[all.Length];
        for(var i=0;i<all.Length;i++)
        {
            var c=all[i]; var local=i<6?i:(i-6)%3; var portrait=c.GetComponentsInChildren<Image>(true).First(im=>im.name.StartsWith("Portrait_")||im.name.StartsWith("Illustration_")); ui.collectionPictures[i]=portrait.sprite;
            R(c.transform,local%2==0?-251:251,352-local/2*435,480,412); Style(c.GetComponent<Image>(),new Color(1,.988f,.97f),.32f);
            var scenery=Maybe(c.transform,"PortraitScenery"); if(scenery!=null) R(scenery,0,77,461,240);
            R(portrait.transform,0,82,460,260); portrait.preserveAspect=true;
            if(i<6)
            {
                var viewport=Card(c.transform,"PortraitViewport",0,80,460,255); viewport.transform.SetSiblingIndex(1); viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
                if(scenery!=null) UnityEngine.Object.DestroyImmediate(scenery.gameObject);
                Pic(viewport.transform,"IllustratedBackground",Sheet("Assets/Art/SpriteSheets/reference_collection_scenes_v2.png","Scene_"+i),0,0,795,265);
                portrait.transform.SetParent(viewport.transform,false); R(portrait.transform,0,-14,460,295);
            }
            var foot=Card(c.transform,"InfoPanel",0,-116,478,178); foot.transform.SetSiblingIndex(c.title.transform.GetSiblingIndex());
            R(c.title.transform,-18,-72,410,65); c.title.font=heavy; c.title.fontSize=32; c.title.alignment=TextAnchor.MiddleLeft;
            R(c.progressText.transform,-138,-122,160,47); c.progressText.fontSize=29; c.progressText.color=new Color(.1f,.47f,1);
            var back=F(c.transform,"Progress_Back"); R(back,-56,-162,310,19); Style(back.GetComponent<Image>(),new Color(.85f,.86f,.93f));
            R(c.progressFill,-211,-162,70,19); c.progressFill.pivot=new Vector2(0,.5f); c.fullProgressWidth=310; Style(c.progressFill.GetComponent<Image>(),new Color(.17f,.52f,1));
            var arrow=Button(c.transform,"Button_OpenDetails","›",UiActionKind.CollectionDetails,i,178,-116,70,70); CircleButton(arrow.transform); arrow.GetComponent<Image>().color=new Color(.91f,.92f,1); arrow.GetComponentInChildren<Text>().fontSize=61; arrow.GetComponentInChildren<Text>().color=new Color(.35f,.4f,.64f);
        }
        // The two compact theme cards at the bottom of the Authors tab also exist in the reference.
        for(var i=0;i<2;i++)
        {
            var card=Card(game.collectionGroups[0].transform,"ThemePreview_"+i,i==0?-251:251,-842,480,185);
            Pic(card.transform,"Landscape",Sheet("Assets/Art/SpriteSheets/reference_collection_scenes_v2.png",i==0?"Scene_1":"Scene_3"),0,28,461,149);
            var foot=Card(card.transform,"Info",0,-48,478,84); Label(foot.transform,"Title",i==0?"Природа":"Любовь",-65,0,290,64,32,true);
            Button(foot.transform,"Button_Open","›",UiActionKind.CollectionDetails,6+i,181,0,65,65);
        }
    }

    static void Achievements()
    {
        var s=S(8); Header(8,"Достижения",790,1000,83,false);
        var trophyOwl=Pic(s,"OwlMascot_Achievements",Asset("Assets/Art/owl_mascot.png"),75,955,400,355); trophyOwl.transform.SetAsFirstSibling();
        Pic(s,"Trophy_Decor",Icon("Trophy"),-114,1010,170,175); var stack=Pic(s,"Books_Decor",Decor("Books"),-325,905,270,140); stack.transform.SetSiblingIndex(F(s,"TitleParchment").GetSiblingIndex());
        var wash=Card(s,"List_Backdrop",0,-204,1060,1610,new Color(.925f,.935f,1,.95f)); wash.transform.SetSiblingIndex(0);
        var tabs=F(s,"Tabs"); R(tabs,0,599,985,113); if(tabs.GetComponent<Image>()!=null) tabs.GetComponent<Image>().color=Color.clear;
        for(var i=0;i<3;i++) { R(game.achievementTabs[i].transform,(i-1)*322,0,311,88); Style(game.achievementTabs[i].GetComponent<Image>(),Color.white,.35f); game.achievementTabs[i].GetComponentInChildren<Text>().fontSize=38; }
        foreach(var old in game.achievementCards) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var titles=new[]{"Первые шаги","Любознательный","Эрудит","Ценитель классики","Доброе сердце","Мастер точности","Ночной гений"};
        var descriptions=new[]{"Реши 10 задач","Реши 50 задач","Достигни 5 уровня","Прочитай 10 произведений","Полюби 20 разных цитат","Реши 30 задач без ошибок","Реши 5 задач подряд вечером"};
        var icons=new[]{Extra("Star"),Icon("Book"),Icon("Trophy"),Extra("Cap"),Icon("Heart"),Extra("Target"),Extra("Moon")}; ui.achievementIcons=icons;
        var targets=new[]{10,50,5,10,20,30,5}; game.achievementCards=new AchievementCardView[7];
        for(var i=0;i<7;i++)
        {
            var card=Card(s,"AchievementCard_"+(i+1),0,413-i*204,978,188); var c=card.gameObject.AddComponent<AchievementCardView>(); game.achievementCards[i]=c; c.target=targets[i]; c.fullProgressWidth=445;
            var tile=Card(card.transform,"IconTile",-378,0,180,165,new[]{new Color(1,.93f,.55f),new Color(1,.92f,.65f),new Color(.83f,.65f,1),new Color(.61f,.85f,1),new Color(1,.7f,.76f),new Color(.65f,.95f,.79f),new Color(.3f,.29f,.62f)}[i]);
            Pic(tile.transform,"Icon",icons[i],0,0,148,142);
            var title=Label(card.transform,"Text_Title",titles[i],25,48,550,60,38,true); title.alignment=TextAnchor.MiddleLeft;
            var desc=Label(card.transform,"Text_Description",descriptions[i],37,-3,580,57,31,false,Muted); desc.alignment=TextAnchor.MiddleLeft;
            Card(card.transform,"Progress_Back",-32,-57,445,23,new Color(.83f,.84f,.93f)); var fill=Card(card.transform,"Progress_Fill",-254,-57,0,23,new Color(.16f,.52f,1)); fill.rectTransform.pivot=new Vector2(0,.5f); c.progressFill=fill.rectTransform;
            c.progressText=Label(card.transform,"Text_Progress","0/"+targets[i],286,-56,136,46,30,false,new Color(.28f,.36f,.64f));
            var arrow=Button(card.transform,"Button_Details","›",UiActionKind.AchievementDetails,i,409,10,77,77); CircleButton(arrow.transform); arrow.GetComponent<Image>().color=new Color(.91f,.93f,1); arrow.GetComponentInChildren<Text>().fontSize=58;
        }
    }

    static void Shop()
    {
        var s=S(9); Header(9,"Магазин",1010,740,83,false); CircleButton(F(s,"Button_Back"));
        var balance=F(s,"BalanceBar"); R(balance,0,840,820,95); balance.GetComponent<Image>().color=Color.clear;
        foreach(var entry in new[]{("Energy_Backing",-217f),("Coin_Backing",220f)}) {var im=Card(balance,entry.Item1,entry.Item2,0,390,85,new Color(.27f,.2f,.31f));im.transform.SetAsFirstSibling();}
        R(game.shopFeathers.transform,-202,0,235,80); R(game.shopCoins.transform,240,0,190,80); game.shopFeathers.color=game.shopCoins.color=Color.white; game.shopFeathers.fontSize=game.shopCoins.fontSize=44;
        R(F(balance,"Icon_Feather"),-408,5,100,142); R(F(balance,"Icon_Coin"),48,0,93,93); R(F(balance,"Button_FeatherInfo"),-54,0,72,72); R(F(balance,"Button_CoinInfo"),374,0,72,72); CircleButton(F(balance,"Button_FeatherInfo")); CircleButton(F(balance,"Button_CoinInfo"));
        R(F(s,"OwlMascot_Shop"),0,607,465,411); var books=Pic(s,"Decor_Books",Decor("Books"),-50,430,720,180); books.transform.SetSiblingIndex(F(s,"OwlMascot_Shop").GetSiblingIndex()); Pic(s,"Decor_Ink",Decor("Ink"),-380,516,160,250);
        var feathers=F(s,"Section_Feathers"); R(feathers,0,86,1000,605); Style(feathers.GetComponent<Image>(),new Color(1,.98f,.9f),.28f);
        R(F(feathers,"Text_Title"),-88,229,550,70); T(feathers,"Text_Title").text="Перья"; T(feathers,"Text_Title").font=heavy; T(feathers,"Text_Title").fontSize=53; T(feathers,"Text_Title").alignment=TextAnchor.MiddleLeft;
        R(F(feathers,"Text_Subtitle"),20,171,775,56); T(feathers,"Text_Subtitle").fontSize=29; Pic(feathers,"Heading_Feather",Icon("Feather"),-415,226,80,100);
        for(var i=0;i<2;i++)
        {
            var item=F(feathers,"ShopItem_Feathers_"+(i+1)); R(item,i==0?-242:242,-61,461,419); Style(item.GetComponent<Image>(),new Color(1,.95f,.8f),.3f);
            var icon=F(item,"Icon").GetComponent<Image>(); icon.sprite=Extra(i==0?"FeatherBundle":"FeatherBag"); R(icon.transform,0,101,375,230);
            R(F(item,"Text_Name"),0,-66,420,61); T(item,"Text_Name").font=heavy; T(item,"Text_Name").fontSize=36;
            R(F(item,"Text_Detail"),0,-114,370,50); T(item,"Text_Detail").fontSize=29;
            Price(F(item,"Button_Buy"),i==0?100:250,0,-167,390,87);
        }
        var hints=F(s,"Section_Hints"); R(hints,0,-388,1000,288); Style(hints.GetComponent<Image>(),new Color(1,.98f,.9f),.28f);
        R(F(hints,"Text_Name"),-96,81,680,70); T(hints,"Text_Name").font=heavy; T(hints,"Text_Name").fontSize=48;
        R(F(hints,"Text_Detail"),-50,-63,560,65); T(hints,"Text_Detail").text="5 подсказок"; T(hints,"Text_Detail").fontSize=31;
        Label(hints,"Subtitle","Используйте, когда нужна помощь",10,29,775,52,29,false,Muted);
        R(F(hints,"Icon"),-373,-51,128,152); Price(F(hints,"Button_Buy"),150,300,-69,300,93);
        var special=F(s,"Section_Special"); R(special,0,-748,1000,386); Style(special.GetComponent<Image>(),new Color(1,.98f,.9f),.28f);
        R(F(special,"Text_Title"),-50,140,590,65); T(special,"Text_Title").text="Особое"; T(special,"Text_Title").font=heavy; T(special,"Text_Title").fontSize=49; Pic(special,"Heading_Star",Extra("Star"),-405,140,75,80);
        for(var i=0;i<2;i++)
        {
            var item=F(special,"ShopItem_Special_"+(i+1)); R(item,0,28-i*138,936,130); Style(item.GetComponent<Image>(),new Color(.94f,.91f,1));
            var glyph=F(item,"Icon"); UnityEngine.Object.DestroyImmediate(glyph.GetComponent<Text>()); var im=glyph.gameObject.AddComponent<Image>(); im.sprite=Extra(i==0?"NoAds":"Moon"); im.preserveAspect=true; im.raycastTarget=false; R(glyph,-367,0,153,115);
            R(F(item,"Text_Name"),-38,23,440,56); T(item,"Text_Name").font=heavy; T(item,"Text_Name").fontSize=35;
            R(F(item,"Text_Detail"),-40,-30,440,47); T(item,"Text_Detail").fontSize=28;
            var buy=F(item,"Button_Buy"); R(buy,321,0,236,83); buy.GetComponent<Image>().sprite=Surface("PurpleButton"); T(buy,"Text_Label").fontSize=33;
        }
        R(game.shopMessage.transform,0,397,940,45); game.shopMessage.fontSize=27;
    }
    static void Price(Transform button,int price,float x,float y,float w,float h)
    { R(button,x,y,w,h); button.GetComponent<Image>().sprite=Surface("GreenButton"); button.GetComponent<Image>().type=Image.Type.Sliced; var label=T(button,"Text_Label"); label.text=price.ToString(); label.font=heavy; label.fontSize=41; R(label.transform,26,0,w-100,h-10); Pic(button,"Coin",Icon("Coin"),-78,0,53,53); }

    static void Details()
    {
        var s=S(1); foreach(Transform child in s.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject); s.name="Screen_LevelDetails";
        var parchment=Pic(s,"TitleParchment",Surface("TitleRibbon"),0,978,900,205); ui.detailTitle=Label(s,"Text_Title","А. С. Пушкин",0,984,800,115,65,true);
        Button(s,"Button_Back","‹",UiActionKind.Back,0,-441,1090,92,92);
        var panel=Card(s,"CollectionDetailCard",0,-5,960,1610);
        ui.detailPicture=Pic(panel.transform,"CollectionPortrait",ui.collectionPictures[0],0,505,690,440);
        ui.detailProgress=Label(panel.transform,"Text_Progress","0 / 20 пройдено",0,207,810,80,42,true);
        Card(panel.transform,"Progress_Back",0,130,740,31,new Color(.83f,.85f,.94f)); ui.detailFill=Card(panel.transform,"Progress_Fill",-370,130,0,31,new Color(.14f,.52f,1)).rectTransform; ui.detailFill.pivot=new Vector2(0,.5f);
        ui.detailBody=Label(panel.transform,"Text_UnlockedQuotes","Проходите уровни, чтобы открывать разгаданные тексты.",0,-256,800,625,36); ui.detailBody.alignment=TextAnchor.UpperCenter;
        Button(s,"Button_Classic","Играть в Классику",UiActionKind.Classic,0,0,-969,840,135,Surface("GreenButton"));
    }
    static void Navigation()
    {
        foreach(var screen in game.screens)
        {
            var nav=Maybe(screen.transform,"BottomNavigation"); if(nav==null)continue;
            R(nav,0,-1050,1020,170); Style(nav.GetComponent<Image>(),new Color(1,.994f,.98f),.25f);
            foreach(var button in nav.GetComponentsInChildren<Button>(true))
            {
                var label=button.GetComponentInChildren<Text>(); label.font=body; label.fontSize=28; R(label.transform,0,-47,198,53);
                var icon=button.GetComponentsInChildren<Image>().FirstOrDefault(im=>im.name.StartsWith("Icon_")); if(icon!=null) R(icon.transform,0,25,78,82);
            }
            nav.SetAsLastSibling();
        }
    }
    static void Popup()
    {
        var canvas=game.screens[0].GetComponentInParent<Canvas>(); var parent=F(canvas.transform,"GlobalPopupLayer"); parent.gameObject.SetActive(true);
        var dim=Card(parent,"AchievementInfoPopup",0,0,1080,2340,new Color(.08f,.055f,.18f,.62f)); dim.raycastTarget=true; ui.popup=dim.gameObject;
        var card=Card(dim.transform,"PopupCard",0,0,880,750); ui.popupIcon=Pic(card.transform,"Icon",Icon("Trophy"),0,190,185,185);
        ui.popupTitle=Label(card.transform,"Text_Title","Достижение",0,40,785,106,52,true); ui.popupBody=Label(card.transform,"Text_Body","Прогресс",0,-110,770,150,36);
        Button(card.transform,"Button_Close","Понятно",UiActionKind.ClosePopup,0,0,-277,580,105,Surface("BlueButton")); dim.gameObject.SetActive(false);
    }

    static void Import()
    {
        Slice("Assets/Art/SpriteSheets/reference_controls_v2.png",new[]{"CreamCircle","LavenderCircle","BlueCircle","Capsule"},new[]{new Rect(50,60,550,540),new Rect(640,60,580,540),new Rect(45,650,550,545),new Rect(590,740,640,370)},1280,1280);
        var scenes=new Rect[6]; for(var i=0;i<6;i++) scenes[i]=new Rect(i%2==0?18:902,i/2==0?16:i/2==1?307:599,850,270);
        Slice("Assets/Art/SpriteSheets/reference_collection_scenes_v2.png",new[]{"Scene_0","Scene_1","Scene_2","Scene_3","Scene_4","Scene_5"},scenes,1768,884);
        Slice("Assets/Art/SpriteSheets/reference_decor_v2.png",new[]{"Books","Cat","Ink","Lantern","Laurel","Hourglass"},new[]{new Rect(35,80,602,337),new Rect(680,15,563,403),new Rect(155,421,312,433),new Rect(690,425,545,423),new Rect(132,873,460,365),new Rect(738,850,425,390)},1280,1280);
        var rects=new Rect[12]; for(var i=0;i<12;i++) rects[i]=new Rect((i%4)*362,i/4==0?0:i/4==1?383:718,362,i/4==0?383:i/4==1?335:368);
        Slice("Assets/Art/SpriteSheets/reference_icons_v2.png",new[]{"Fire","Target","Star","Calendar","Cap","Moon","Gift","FeatherBundle","FeatherBag","Video","NoAds","Sparkles"},rects,1448,1086);
    }
    static void Slice(string path,string[] names,Rect[] sourceRects,float sourceWidth,float sourceHeight)
    {
        AssetDatabase.ImportAsset(path); var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Multiple; importer.alphaIsTransparency=true; importer.isReadable=true; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=4096; importer.SaveAndReimport();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path); var meta=new SpriteMetaData[names.Length];
        for(var i=0;i<names.Length;i++)
        {
            var r=sourceRects[i]; var x=Mathf.RoundToInt(r.x/sourceWidth*tex.width); var y=Mathf.RoundToInt((sourceHeight-r.y-r.height)/sourceHeight*tex.height); var w=Mathf.RoundToInt(r.width/sourceWidth*tex.width); var h=Mathf.RoundToInt(r.height/sourceHeight*tex.height);
            int minX=x+w,minY=y+h,maxX=x,maxY=y; var pixels=tex.GetPixels32();
            for(var yy=Mathf.Max(0,y);yy<Mathf.Min(tex.height,y+h);yy++) for(var xx=x;xx<Mathf.Min(tex.width,x+w);xx++) if(pixels[yy*tex.width+xx].a>25){minX=Math.Min(minX,xx);maxX=Math.Max(maxX,xx);minY=Math.Min(minY,yy);maxY=Math.Max(maxY,yy);}
            meta[i]=new SpriteMetaData{name=names[i],rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),pivot=new Vector2(.5f,.5f),alignment=(int)SpriteAlignment.Center,border=names[i]=="Capsule"?new Vector4(150,140,150,140):Vector4.zero};
        }
        importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.spritesheet=meta; EditorUtility.SetDirty(importer); importer.SaveAndReimport();
        Debug.Log("REVISION_SLICES " + path + "=" + AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Count());
    }
}
