using System;
using System.IO;
using System.Linq;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-time editing tool. All controls are serialized in the two delivered scenes.
public static class FeedbackPass
{
    static CryptogramGame game;
    static Sprite rounded;
    static readonly Color Ink=new Color(.16f,.10f,.27f), Paper=new Color(1,.983f,.935f), Blue=new Color(.13f,.48f,.91f);
    static Transform S(int i)=>game.screens[i].transform;
    static Transform M(Transform p,string n)=>p.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==n);
    static Transform F(Transform p,string n)=>M(p,n)??throw new Exception("Missing "+p.name+"/"+n);
    static void R(Transform t,float x,float y,float w,float h)
    {var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;}
    static void Move(Transform p,string n,float x,float y,float w,float h)=>R(F(p,n),x,y,w,h);
    static void Remove(Transform p,params string[] names)
    {foreach(var name in names){var t=M(p,name);if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);}}
    static void Flat(Image im,Color color,float multiplier=1)
    {im.sprite=rounded;im.type=Image.Type.Sliced;im.preserveAspect=false;im.pixelsPerUnitMultiplier=multiplier;im.color=color;}
    static void Font(Text t,int size,TextAnchor align=TextAnchor.MiddleCenter)
    {t.fontSize=size;t.resizeTextForBestFit=true;t.resizeTextMinSize=size-5;t.resizeTextMaxSize=size;t.color=Ink;t.alignment=align;t.verticalOverflow=VerticalWrapMode.Truncate;}
    static RectTransform Root(Transform p,string name,float x,float y,float w,float h)
    {var o=new GameObject(name,typeof(RectTransform));o.layer=5;o.transform.SetParent(p,false);R(o.transform,x,y,w,h);return (RectTransform)o.transform;}
    static Text Label(Transform p,string name,string value,float x,float y,float w,float h,int size)
    {var t=Root(p,name,x,y,w,h).gameObject.AddComponent<Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Nunito-SemiBold.ttf");Font(t,size);t.text=value;t.raycastTarget=false;return t;}
    static void Title(int i,float x,float y,float w,float h,int size,string value=null)
    {var t=F(S(i),"Text_Title").GetComponent<Text>();R(t.transform,x,y,w,h);Font(t,size);if(value!=null)t.text=value;}
    static void ButtonStyle(Transform t,float x,float y,float w,float h,string caption=null)
    {
        R(t,x,y,w,h);Flat(t.GetComponent<Image>(),Blue,.8f);
        var label=t.GetComponentInChildren<Text>(true);if(label!=null){R(label.transform,0,0,w-(w<150?12:50),h-12);Font(label,42);label.color=Color.white;if(caption!=null)label.text=caption;}
    }
    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        game=UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();rounded=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/rounded_panel.png");
        Dump("Before");Home();Settings();Collections();Shop();Rewards();Other();
        game.mainFeathers.text=game.shopFeathers.text="5";game.shopCoins.text=game.mainErudition.text="0";
        game.noFeathersCounter.text=game.noFeathersCounterRestored.text="0";
        game.presentation.statsLevel.text="0";game.presentation.statsNext.text="До следующего уровня: 50";
        foreach(var s in game.screens.Where(s=>s!=null))s.SetActive(false);game.screens[0].SetActive(true);
        PrefabUtility.SaveAsPrefabAsset(S(10).gameObject,"Assets/Prefabs/SettingsScreen.prefab");
        PrefabUtility.SaveAsPrefabAsset(game.authorCards[0].gameObject,"Assets/Prefabs/AuthorCard.prefab");
        PrefabUtility.SaveAsPrefabAsset(game.themeCards[0].gameObject,"Assets/Prefabs/CollectionCard.prefab");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Gameplay();PlayerSettings.bundleVersion="0.1.1";PlayerSettings.Android.bundleVersionCode=2;
        AssetDatabase.SaveAssets();Debug.Log("FEEDBACK_SCENES_AUTHORED");
    }
    public static void RunAndTest(){Run();UsabilityTests.Run();}
    static void Home()
    {
        var s=S(0);
        foreach(var b in s.GetComponentsInChildren<Button>(true).Where(b=>b.name=="Button_Classic"||b.name=="Button_Turbo"))
        {
            foreach(var name in new[]{"Icon_Arrow","ArrowCircle"}){var t=M(b.transform,name);if(t!=null){var r=(RectTransform)t;r.anchoredPosition=new Vector2(365,0);}}
            foreach(var label in b.GetComponentsInChildren<Text>(true).Where(t=>t.name.Contains("Subtitle"))) label.rectTransform.anchoredPosition+=new Vector2(0,17);
        }
        var energy=F(s,"FeatherEnergy");R(energy,0,1025,390,116);
        Move(energy,"Icon_Feather",-145,1,95,129);R(game.mainFeathers.transform,-5,3,150,85);Font(game.mainFeathers,53);game.mainFeathers.color=Color.white;
        var plus=energy.GetComponentInChildren<Button>(true);if(plus!=null){R(plus.transform,137,0,70,70);var label=plus.GetComponentInChildren<Text>(true);R(label.transform,0,0,64,68);Font(label,42);label.text="+";}
        Label(s,"Text_RegenLimit","восстановление до 5",0,943,460,46,25).color=new Color(.33f,.23f,.26f);
    }
    static void Settings()
    {
        var s=S(10);Remove(s,"Settings_Subtitle","Text_Autosave","ReadingSample");Title(10,0,977,585,100,65);
        for(var i=0;i<4;i++)
        {
            var row=F(s,"SettingRow_"+i);R(row,0,405-i*254,954,210);Flat(row.GetComponent<Image>(),Paper,.5f);
            Remove(row,"Setting_Description");game.settingValues[i].gameObject.SetActive(false);
            Move(row,"Icon_Back",-361,0,148,148);Move(row,"Icon",-361,0,107,107);
            var t=F(row,"Setting_Title").GetComponent<Text>();R(t.transform,-43,0,445,102);Font(t,44,TextAnchor.MiddleLeft);if(i==1)t.text="Звук";
            R(game.settingTracks[i].transform,330,0,182,88);
        }
        ButtonStyle(F(s,"Button_SettingsHome"),0,-828,910,135,"В библиотеку");
        var back=F(s,"Button_Back");ButtonStyle(back,-451,1093,85,85,"‹");
    }
    static void Collections()
    {
        var s=S(7);Title(7,89,923,555,110,75);Remove(s,"ThemePreview_0","ThemePreview_1");
        var sets=new[]{game.authorCards,game.themeCards,game.bookCards};
        for(var tab=0;tab<3;tab++)
        {
            var group=game.collectionGroups[tab].transform;R(group,0,0,1080,2340);
            var viewport=Root(group,"CardsViewport",0,-227,1010,1492);viewport.gameObject.AddComponent<RectMask2D>();
            var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            var content=Root(viewport,"CardsContent",0,0,1000,Mathf.Max(1492,Mathf.CeilToInt(sets[tab].Length/2f)*575+20));
            content.anchorMin=content.anchorMax=new Vector2(.5f,1);content.pivot=new Vector2(.5f,1);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=55;scroll.decelerationRate=.12f;
            for(var i=0;i<sets[tab].Length;i++)
            {
                var card=sets[tab][i];card.transform.SetParent(content,false);
                R(card.transform,i%2==0?-253:253,-280-i/2*575,484,548);Flat(card.GetComponent<Image>(),Paper,.6f);
                ((RectTransform)card.transform).anchorMin=((RectTransform)card.transform).anchorMax=new Vector2(.5f,1);
                var portrait=card.GetComponentsInChildren<Image>(true).First(im=>im.name.StartsWith("Portrait_")||im.name.StartsWith("Illustration_"));
                var mask=M(card.transform,"PortraitViewport");
                if(mask==null)
                {
                    mask=Root(card.transform,"PortraitViewport",0,85,484,378);var im=mask.gameObject.AddComponent<Image>();Flat(im,Color.white,.6f);mask.gameObject.AddComponent<Mask>().showMaskGraphic=false;
                    Remove(card.transform,"PortraitScenery");portrait.transform.SetParent(mask,false);mask.SetAsFirstSibling();
                }
                R(mask,0,85,484,378);var bg=M(mask,"IllustratedBackground");if(bg!=null)R(bg,0,0,1100,390);
                R(portrait.transform,0,0,484,378);portrait.preserveAspect=true;
                Move(card.transform,"InfoPanel",0,-187,484,174);Flat(F(card.transform,"InfoPanel").GetComponent<Image>(),Paper,.6f);
                R(card.title.transform,-20,-139,399,65);Font(card.title,34,TextAnchor.MiddleLeft);
                R(card.progressText.transform,-135,-198,165,45);Font(card.progressText,29,TextAnchor.MiddleLeft);card.progressText.color=Blue;
                Move(card.transform,"Progress_Back",-40,-241,350,17);Flat(F(card.transform,"Progress_Back").GetComponent<Image>(),new Color(.84f,.85f,.92f),2);
                R(card.progressFill,-215,-241,0,17);card.progressFill.pivot=new Vector2(0,.5f);card.fullProgressWidth=350;Flat(card.progressFill.GetComponent<Image>(),Blue,2);
                ButtonStyle(F(card.transform,"Button_OpenDetails"),182,-199,70,70,"›");
            }
        }
    }
    static void Shop()
    {
        var s=S(9);Remove(s,"Section_Special");Title(9,0,1010,490,110,72);
        var balance=F(s,"BalanceBar");R(balance,0,819,1000,108);
        foreach(var pair in new[]{("Energy_Backing",-240f),("Coin_Backing",240f)}){Move(balance,pair.Item1,pair.Item2,0,455,141);Flat(F(balance,pair.Item1).GetComponent<Image>(),Paper,.7f);}
        R(game.shopFeathers.transform,-234,20,170,73);R(game.shopCoins.transform,244,0,170,83);Font(game.shopFeathers,47);Font(game.shopCoins,47);
        Move(balance,"Icon_Feather",-405,2,74,96);Move(balance,"Icon_Coin",83,0,80,80);Move(balance,"Button_FeatherInfo",-66,0,66,66);Move(balance,"Button_CoinInfo",413,0,66,66);
        Label(balance,"Text_RegenLimit","восстановление до 5",-240,-40,396,43,25);
        var feathers=F(s,"Section_Feathers");R(feathers,0,0,1000,1000);feathers.GetComponent<Image>().enabled=false;Remove(feathers,"Text_Title","Text_Subtitle","Heading_Feather");
        for(var i=0;i<2;i++)Product(F(feathers,"ShopItem_Feathers_"+(i+1)),243-i*335,i==0?"5 перьев":"15 перьев");
        Product(F(s,"Section_Hints"),-427,"5 подсказок");
        R(game.shopMessage.transform,0,-654,950,80);Font(game.shopMessage,32);game.shopMessage.text="";
    }
    static void Product(Transform p,float y,string name)
    {
        R(p,0,y,974,290);Flat(p.GetComponent<Image>(),Paper,.5f);Remove(p,"Text_Detail","Subtitle");
        Move(p,"Icon",-342,0,191,228);var title=F(p,"Text_Name").GetComponent<Text>();R(title.transform,-30,0,350,120);Font(title,43);title.text=name;
        var buy=F(p,"Button_Buy");ButtonStyle(buy,329,0,237,106);Move(buy,"Coin",-65,0,54,54);
        var price=F(buy,"Text_Label").GetComponent<Text>();R(price.transform,35,0,125,91);Font(price,40);price.color=Color.white;
    }
    static void Rewards()
    {
        var s=S(4);if(game.presentation.likeButton!=null)UnityEngine.Object.DestroyImmediate(game.presentation.likeButton.gameObject);game.presentation.likeButton=null;game.presentation.likeLabel=null;
        var rewards=F(s,"RewardsPanel");R(rewards,0,-469,986,298);
        var names=new[]{"Reward_EruditionCard","Reward_CollectionCard","Reward_CoinsCard"};
        var texts=new[]{game.victoryEruditionReward,game.victoryCollectionReward,game.presentation.coinsReward};
        for(var i=0;i<3;i++)
        {
            var c=F(rewards,names[i]);R(c,(i-1)*332,0,314,286);Flat(c.GetComponent<Image>(),Paper,.65f);
            foreach(var image in c.GetComponentsInChildren<Image>(true).Where(im=>im.transform!=c))R(image.transform,0,69,106,106);
            var star=M(c,"Icon_Star");if(star!=null)R(star,61,106,44,44);
            R(texts[i].transform,0,-57,286,132);Font(texts[i],31);texts[i].supportRichText=true;
        }
        ButtonStyle(F(s,"Button_Continue"),0,-701,963,134,"Продолжить");
        var ad=F(s,"Button_RewardedAd");ButtonStyle(ad,0,-882,963,155,"Видео за бонус");
        foreach(var child in ad.Cast<Transform>().Where(t=>t.GetComponent<Image>()!=null)) child.gameObject.SetActive(false);
        R(game.victoryBonusNote.transform,0,-1021,950,79);Font(game.victoryBonusNote,29);
    }
    static void Other()
    {
        Title(6,0,1032,554,103,70);
        Title(12,0,760,719,170,64,"Попробуем\nещё");Remove(S(12),"Text_Encouragement");
        Title(5,0,759,697,172,63,"Перья\nзакончились");
        foreach(var t in S(5).GetComponentsInChildren<Text>(true).Where(t=>t.text.StartsWith("Перья постепенно")))t.text="Восстановление\nдо 5 перьев";
        Title(8,0,720,716,110,77);Move(S(8),"Tabs",0,531,985,113);
        foreach(var c in game.achievementCards)
        {
            var back=F(c.transform,"Progress_Back").GetComponent<Image>();Flat(back,new Color(.83f,.84f,.92f),1.8f);Flat(c.progressFill.GetComponent<Image>(),Blue,1.8f);
        }
        F(game.achievementCards[4].transform,"Text_Title").GetComponent<Text>().text="Коллекционер";
        F(game.achievementCards[4].transform,"Text_Description").GetComponent<Text>().text="Собери 20 разных историй";
        var details=S(1);Title(1,0,1003,692,111,61);
        foreach(var t in details.GetComponentsInChildren<Text>(true).Where(t=>t.text.Contains("Собранные истории можно")))t.gameObject.SetActive(false);
        ButtonStyle(F(details,"Button_Classic"),0,-977,917,140,"Играть в классику");ButtonStyle(F(details,"Button_Back"),-455,1093,85,85,"‹");
        var pic=game.presentation.detailPicture;R(pic.transform,0,537,690,384);
        R(game.presentation.detailProgress.transform,0,268,810,80);Font(game.presentation.detailProgress,38);
        var fill=game.presentation.detailFill;fill.anchoredPosition=new Vector2(-370,194);Move(fill.parent,"Progress_Back",0,194,740,24);Flat(F(fill.parent,"Progress_Back").GetComponent<Image>(),new Color(.84f,.85f,.92f),1.8f);Flat(fill.GetComponent<Image>(),Blue,1.8f);
        if(game.presentation.detailScroll!=null){R(game.presentation.detailScroll.viewport,0,-246,832,751);}
    }
    static void Gameplay()
    {
        const string path="Assets/Art/Backgrounds/gameplay_compact_v2.png";AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.wrapMode=TextureWrapMode.Mirror;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity",OpenSceneMode.Additive);var link=UnityEngine.Object.FindFirstObjectByType<GameplaySceneLink>();
        foreach(var board in new[]{link.classicBoard,link.turboBoard})
        {
            var s=board.transform;F(s,"Illustration_Background").GetComponent<RawImage>().texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            ButtonStyle(F(s,"Button_BackOrPause"),-449,1073,99,99,"‹");
            Move(s,"HeartsContainer",0,935,board.Mode==PuzzleMode.Classic?545:370,93);
            var mode=F(s,"Label_Mode");R(mode,-211,1073,327,102);Flat(mode.GetComponent<Image>(),Paper,.7f);
            var modeLabel=F(mode,"Text_Mode").GetComponent<Text>();R(modeLabel.transform,20,0,227,88);Font(modeLabel,40);
            R(modeLabel.transform,33,0,242,88);
            foreach(var im in mode.GetComponentsInChildren<Image>(true).Where(im=>im.transform!=mode))R(im.transform,-121,0,68,75);
            var crown=board.Mode==PuzzleMode.Classic?link.classicErudition:link.turboErudition;
            var badge=crown.transform.parent as RectTransform;badge.anchoredPosition=new Vector2(badge.anchoredPosition.x,1073);
            var top=F(s,"TopBar");var hint=F(top,"Button_Hint");R(hint,440,1073,112,112);
            Remove(s,"Text_KeyboardLegend","Text_ScrollTip");var panel=F(s,"PuzzlePanel");R(panel,0,32,1010,1110);
            Move(panel,"Text_Instruction",0,489,870,60);Move(panel,"Text_ReadingProgress",0,434,870,44);Move(panel,"ReadingProgress_Back",0,403,840,10);
            Move(panel,"ReadingViewport",0,-29,942,806);Move(panel,"ReadingScrollTrack",482,-29,8,806);Move(panel,"Text_RevealFeedback",0,-489,870,71);
            board.SetGame(null);board.StartPuzzle(PuzzleGenerator.Generate(board.Mode,0,0),board.Mode==PuzzleMode.Classic?24:504,new PuzzleProgress(),0,2);
        }
        Dump("After");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    static void Dump(string suffix)
    {
        var lines=UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.GetComponent<Text>()!=null||r.GetComponent<Button>()!=null).Select(r=>r.parent.name+"/"+r.name+" pos="+r.anchoredPosition+" size="+r.sizeDelta+" text="+r.GetComponent<Text>()?.text);
        Directory.CreateDirectory("Review/FeedbackPass");File.WriteAllLines("Review/FeedbackPass/Hierarchy"+suffix+".txt",lines);
    }
}
