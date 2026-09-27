using System;
using System.Linq;
using Erudition;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HeaderSettingsPolish
{
    static CryptogramGame game;
    static Font body,bold;
    static Sprite capsule,circle;
    static readonly Color Ink=new Color(.18f,.12f,.28f);
    static readonly Color Cream=new Color(1,.981f,.923f);
    static Transform S(int i)=>game.screens[i].transform;
    static Transform F(Transform r,string n)=>r.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
    static Sprite Sprite(string path,string n)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single(s=>s.name==n);
    static void R(Transform t,float x,float y,float w,float h)
    {var r=(RectTransform)t; r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
    static void Move(Transform p,string n,float x,float y,float w,float h)=>R(F(p,n),x,y,w,h);
    static RectTransform Root(Transform p,string n,float x,float y,float w,float h)
    {var o=new GameObject(n,typeof(RectTransform));o.layer=5;o.transform.SetParent(p,false);R(o.transform,x,y,w,h);return (RectTransform)o.transform;}
    static Image Pic(Transform p,string n,Sprite sprite,float x,float y,float w,float h)
    {var t=Root(p,n,x,y,w,h);var im=t.gameObject.AddComponent<Image>();im.sprite=sprite;im.preserveAspect=true;im.raycastTarget=false;return im;}
    static Image Card(Transform p,string n,float x,float y,float w,float h,Color color)
    {var im=Pic(p,n,capsule,x,y,w,h);im.type=Image.Type.Sliced;im.preserveAspect=false;im.pixelsPerUnitMultiplier=3;im.color=color;return im;}
    static Text Label(Transform p,string n,string value,float x,float y,float w,float h,int size,bool heavy=false)
    {var r=Root(p,n,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=heavy?bold:body;t.fontSize=size;t.color=Ink;t.text=value;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
    static Button Action(Image image,UiActionKind kind,int parameter=0)
    {image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;var action=image.gameObject.AddComponent<UiAction>();action.Configure(game,kind,parameter);UnityEventTools.AddPersistentListener(button.onClick,action.Press);image.gameObject.AddComponent<UiWarmth>();return button;}
    static void Header(int index,float y,float width,int size)
    {
        var s=S(index);Move(s,"TitleParchment",0,y,width,192);Move(s,"Text_Title",0,y+8,width-125,108);var title=F(s,"Text_Title").GetComponent<Text>();title.fontSize=size;title.resizeTextForBestFit=true;title.resizeTextMinSize=size-5;title.resizeTextMaxSize=size;
        var crown=s.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Header_Crown");if(crown!=null)R(crown,0,y+132,105,90);
        var banner=F(s,"TitleParchment");Move(banner,"Laurel_Left",-width*.40f,-53,90,110);Move(banner,"Laurel_Right",width*.40f,-53,90,110);
    }
    public static void Run()
    {
        ImportIcons();var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");game=UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        body=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Nunito-SemiBold.ttf");bold=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Nunito-Black.ttf");
        capsule=Sprite("Assets/Art/SpriteSheets/reference_controls_v2.png","Capsule");circle=Sprite("Assets/Art/SpriteSheets/reference_controls_v2.png","CreamCircle");
        Collections();Statistics();Energy();Settings();
        IntegratedReferenceBackgrounds.Apply(game);
        foreach(var s in game.screens)if(s!=null)s.SetActive(false);game.screens[0].SetActive(true);
        PrefabUtility.SaveAsPrefabAsset(S(10).gameObject,"Assets/Prefabs/SettingsScreen.prefab");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("HEADER_SETTINGS_LAYOUT_SAVED");
        UsabilityTests.Run();
    }
    static void Collections()
    {
        var s=S(7);Header(7,992,835,72);
        // Compact illustrations sit in one hero band, below the title and above the tabs.
        Move(s,"OwlMascot_Collections",-319,801,247,235);Move(s,"Decor_Books",-300,688,338,110);Move(s,"Decor_Hourglass",341,748,177,225);
        Move(s,"Tabs",0,558,990,106);
        foreach(var button in game.collectionTabs) {var r=(RectTransform)button.transform;r.sizeDelta=new Vector2(320,88);}
        // Preserve readable card content, but reserve enough space for the title/hero band.
        foreach(var card in game.authorCards.Concat(game.themeCards).Concat(game.bookCards))
        {
            var r=(RectTransform)card.transform;
            var old=r.anchoredPosition;r.anchoredPosition=new Vector2(old.x,old.y-72);
            r.localScale=new Vector3(.97f,.97f,1);
        }
        // Extra preview themes repeat the next tab and crowd the bottom; keep them in Themes.
        // The reference's compact theme previews are restyled by IntegratedReferenceBackgrounds.
    }
    static void Statistics()
    {
        var s=S(6);Header(6,955,850,75);
        Move(s,"OwlMascot_Statistics",0,661,418,388);
        Move(s,"Decor_Books",0,497,610,134);
        Move(s,"Decor_Cat",355,523,225,175);Move(s,"Decor_Ink",-367,548,145,225);
        Move(s,"EruditionCard",0,255,1000,320);
        R(game.presentation.statsNext.transform,210,-117,425,45);
        var names=new[]{"Card_Solved","Card_Accuracy","Card_Streak","Card_Best"};
        for(var i=0;i<4;i++)
        {
            var card=F(s,names[i]);R(card,i%2==0?-254:254,i<2?-40:-288,490,220);
            Move(card,"MetricIcon",-157,2,116,133);Move(card,"Text_Label",65,64,320,58);Move(card,"Text_Value",65,-5,280,92);Move(card,"Text_Caption",60,-76,320,44);
        }
        var graph=F(s,"ActivityGraph");R(graph,0,-687,1000,480);Move(graph,"Text_Title",-260,184,430,76);Move(graph,"Text_Subtitle",-198,122,565,48);
        var graphTitle=F(graph,"Text_Title").GetComponent<Text>();graphTitle.fontSize=45;graphTitle.resizeTextForBestFit=true;graphTitle.resizeTextMinSize=41;graphTitle.resizeTextMaxSize=45;
        for(var i=0;i<game.presentation.periods.Length;i++)R(game.presentation.periods[i].transform,95+i*145,190,144,60);
    }
    static void Energy()
    {
        var s=S(5);Header(5,812,940,63);F(s,"Text_Title").GetComponent<Text>().text="Перья закончились";
        Label(s,"Text_RestCaption","Немного отдыха — и снова в игру",0,668,875,52,29).color=new Color(.34f,.23f,.24f);
    }
    static void Settings()
    {
        var s=S(10);var title=F(s,"Text_Title");var banner=F(s,"TitleParchment");var debug=game.debugOpenButton.transform;
        var back=s.GetComponentsInChildren<Button>(true).First(b=>b.name.Contains("Back")).transform;
        debug.SetParent(s,false);back.SetParent(s,false);
        foreach(var child in s.Cast<Transform>().ToArray())if(child!=title&&child!=banner&&child!=debug&&child!=back)UnityEngine.Object.DestroyImmediate(child.gameObject);
        Header(10,996,810,75);R(back,-455,1101,85,85);var backImage=back.GetComponent<Image>();backImage.sprite=circle;backImage.type=Image.Type.Simple;backImage.preserveAspect=true;backImage.color=Color.white;
        Label(s,"Settings_Subtitle","Пусть в библиотеке будет уютно",0,836,875,55,31);
        Pic(s,"Owl_Settings",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/owl_mascot.png"),0,671,275,270);
        var headings=new[]{"Музыка","Звуки","Вибрация","Крупный текст"};
        var captions=new[]{"Тихая мелодия библиотеки","Отклик клавиш и маленьких побед","Мягкий отклик на ошибку","Чуть больше букв — легче читать"};
        var icons=new[]{"Music","Sound","Vibration","Text"};
        game.settingValues=new Text[4];game.settingTracks=new Image[4];game.settingThumbs=new RectTransform[4];
        for(var i=0;i<4;i++)
        {
            var row=Card(s,"SettingRow_"+i,0,425-i*243,960,202,Cream);
            var iconBack=Pic(row.transform,"Icon_Back",circle,-362,0,158,158);iconBack.color=i%2==0?new Color(.94f,.91f,1):new Color(.88f,.94f,1);
            Pic(row.transform,"Icon",Sprite("Assets/Art/SpriteSheets/settings_icons_v1.png",icons[i]),-362,0,111,111);
            var heading=Label(row.transform,"Setting_Title",headings[i],-65,40,440,62,39,true);heading.alignment=TextAnchor.MiddleLeft;
            var caption=Label(row.transform,"Setting_Description",captions[i],-65,-44,440,98,27);caption.alignment=TextAnchor.MiddleLeft;caption.color=new Color(.50f,.43f,.48f);
            var track=Card(row.transform,"Toggle_"+headings[i],326,16,182,88,i<3?new Color(.3f,.64f,.47f):new Color(.74f,.71f,.77f));track.pixelsPerUnitMultiplier=3.6f;Action(track,UiActionKind.ToggleSetting,i);
            var thumb=Pic(track.transform,"SwitchThumb",circle,i<3?43:-43,0,74,74);game.settingThumbs[i]=thumb.rectTransform;game.settingTracks[i]=track;
            game.settingValues[i]=Label(row.transform,"Text_State",i<3?"Включено":"Выключено",326,-65,220,45,25);
            game.settingValues[i].color=i<3?new Color(.2f,.46f,.34f):new Color(.47f,.43f,.5f);
        }
        Label(s,"Text_Autosave","Изменения сохраняются автоматически",0,-516,910,52,26).color=new Color(.42f,.32f,.32f);
        var sample=Card(s,"ReadingSample",0,-653,960,150,new Color(1,.963f,.848f));
        Pic(sample.transform,"Feather",Sprite("Assets/Art/SpriteSheets/ui_icons_reference.png","Feather"),-385,0,92,115);
        Label(sample.transform,"Sample_Text","Уютно читать.\nЛегко разгадывать.",20,0,660,123,35,true);
        var home=Card(s,"Button_SettingsHome",0,-872,940,135,Color.white);home.sprite=Sprite("Assets/Art/SpriteSheets/ui_surfaces_reference.png","BlueButton");home.pixelsPerUnitMultiplier=1.2f;Action(home,UiActionKind.Home);
        Label(home.transform,"Text_Label","Вернуться в библиотеку",0,0,830,113,43,true).color=Color.white;
        R(debug,0,-1070,720,65);var dim=debug.GetComponent<Image>();dim.sprite=null;dim.color=Color.clear;
        var debugText=debug.GetComponentInChildren<Text>();R(debugText.transform,0,0,710,60);debugText.text="Инструменты разработчика";debugText.font=body;debugText.fontSize=24;debugText.resizeTextForBestFit=false;debugText.color=new Color(.46f,.38f,.34f);
    }
    static void ImportIcons()
    {
        const string path="Assets/Art/SpriteSheets/settings_icons_v1.png";AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.alphaIsTransparency=true;importer.isReadable=true;importer.maxTextureSize=4096;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();var w=texture.width;var h=texture.height;var names=new[]{"Music","Sound","Vibration","Text"};var sprites=new SpriteMetaData[4];
        for(var i=0;i<4;i++)
        {
            var x0=i%2*w/2;var y0=(1-i/2)*h/2;var xmin=x0+w/2;var xmax=x0;var ymin=y0+h/2;var ymax=y0;
            for(var y=y0;y<y0+h/2;y++)for(var x=x0;x<x0+w/2;x++)if(pixels[y*w+x].a>75){xmin=Math.Min(xmin,x);xmax=Math.Max(xmax,x);ymin=Math.Min(ymin,y);ymax=Math.Max(ymax,y);}
            xmin=Math.Max(x0,xmin-4);ymin=Math.Max(y0,ymin-4);xmax=Math.Min(x0+w/2-1,xmax+4);ymax=Math.Min(y0+h/2-1,ymax+4);
            sprites[i]=new SpriteMetaData{name=names[i],rect=new Rect(xmin,ymin,xmax-xmin+1,ymax-ymin+1),pivot=new Vector2(.5f,.5f)};
        }
        importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.spritesheet=sprites;importer.isReadable=false;importer.SaveAndReimport();
    }
}
