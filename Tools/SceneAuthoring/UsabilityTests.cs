using System;
using System.Linq;
using System.Reflection;
using Erudition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UsabilityTests
{
    static int frames, result;
    static bool tested, oldEnabled;
    static EnterPlayModeOptions oldOptions;
    public static void Run()
    {
        Verify();
        if(LayoutOwnershipTests.Enabled) LayoutOwnershipTests.Capture();
        AdsBridge.EditorTestRewards=true;
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        SaveStore.EditorTestSlot="erudition.usability.temporary"; SaveStore.Reset();
        oldEnabled=EditorSettings.enterPlayModeOptionsEnabled; oldOptions=EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled=true; EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        EditorApplication.playModeStateChanged+=State; EditorApplication.update+=Tick; EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode || !tested) return;
        EditorApplication.update-=Tick; EditorApplication.playModeStateChanged-=State;
        EditorSettings.enterPlayModeOptionsEnabled=oldEnabled; EditorSettings.enterPlayModeOptions=oldOptions;
        SaveStore.Reset(); SaveStore.EditorTestSlot=null;
        if(result==0) try { UsabilityReview.Capture(); } catch(Exception e) { result=1; Debug.LogException(e); }
        EditorApplication.Exit(result);
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying || tested) return;
        frames++;
        var game=UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        if(frames<30 || game==null || game.classicBoard==null) { if(frames<6000)return; }
        tested=true;
        try { Test(game); Debug.Log("USABILITY_RUNTIME_PASS: additive scenes, zero erudition, migration, selection, manual letter placement in both modes, persistent green keys, exhausted keys, single-cell hints, partial and legacy saves, hearts, resume, rewards, long paragraphs, auto-scroll, settings, no runtime UI creation"); }
        catch(Exception e){ result=1; Debug.LogException(e); }
        EditorApplication.isPlaying=false;
    }
    public static void Verify()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity",OpenSceneMode.Additive);
        var game=UnityEngine.Object.FindFirstObjectByType<CryptogramGame>();
        var link=UnityEngine.Object.FindFirstObjectByType<GameplaySceneLink>();
        Require(game.screens[2]==null && game.screens[3]==null,"Main does not serialize cross-scene references");
        var all=Enumerable.Range(0,SceneManager.sceneCount).SelectMany(i=>SceneManager.GetSceneAt(i).GetRootGameObjects()).SelectMany(r=>r.GetComponentsInChildren<Component>(true)).ToArray();
        Require(all.All(c=>c!=null),"No missing scripts"); Require(all.OfType<Text>().All(t=>t.font!=null),"All fonts assigned");
        foreach(var button in all.OfType<Button>())
        {
            Require(button.onClick.GetPersistentEventCount()>0,"Saved action: "+button.name);
            for(var i=0;i<button.onClick.GetPersistentEventCount();i++) Require(button.onClick.GetPersistentTarget(i)!=null,"Action target: "+button.name);
        }
        var entries=PuzzleGenerator.Build(game.library);
        foreach(var entry in entries) Require((entry.mode==PuzzleMode.Classic?link.classicBoard:link.turboBoard).CanFit(entry.text),"Fits authored cells: "+entry.id);
        Require(entries.Where(e=>e.id.StartsWith("generated")&&e.mode==PuzzleMode.Classic&&e.minimumErudition>=320).All(e=>PuzzleGenerator.WordCount(e.text)>=20),"Advanced classic paragraphs contain twenty or more words");
        Require(all.OfType<KeyboardKeyView>().Count()==66,"Two full keyboards");
        Require(all.OfType<LetterCellView>().Count()==672,"672 authored cells");
        Require(all.OfType<RawImage>().Count(i=>i.name=="Illustration_Background"&&i.texture!=null)==11,"Eleven screens have an authored integrated background");
        Require(!all.OfType<Image>().Any(i=>i.name.StartsWith("OwlMascot")||i.name=="Owl_Settings"),"No detached owl images remain in scenes");
        Debug.Log("USABILITY_VERIFY_PASS scenes=2 cells=672 keys=66 catalogue="+entries.Length+" longestWords="+entries.Max(e=>PuzzleGenerator.WordCount(e.text))+" missing=0 unfonted=0");
    }
    static int Objects() => Enumerable.Range(0,SceneManager.sceneCount).SelectMany(i=>SceneManager.GetSceneAt(i).GetRootGameObjects()).Sum(r=>r.GetComponentsInChildren<Transform>(true).Length);
    static KeyboardKeyView Key(PuzzleBoard b,char c)=>b.GetComponentsInChildren<KeyboardKeyView>(true).Single(k=>k.Letter==c);
    static LetterCellView[] Cells(PuzzleBoard b)=>b.GetComponentsInChildren<LetterCellView>(true).Where(c=>c.gameObject.activeSelf && c.Code>0).ToArray();
    static void Solve(CryptogramGame g,PuzzleBoard b)
    {
        var guard=0; while(Cells(b).Any(c=>c.IsHiddenLetter) && guard++<700)
        { var cell=Cells(b).First(c=>c.IsHiddenLetter); cell.Press(); b.Guess(cell.Answer,Key(b,cell.Answer)); }
        Require(g.screens[4].activeSelf,"Victory screen");
    }
    static void Test(CryptogramGame game)
    {
        Require(game!=null && game.classicBoard!=null,"Gameplay scene loads and binds");
        Require(SceneManager.sceneCount==2,"Two loaded scenes");
        if(LayoutOwnershipTests.Enabled) LayoutOwnershipTests.AssertUnchanged("after Awake");
        var count=Objects(); var state=(GameSave)typeof(CryptogramGame).GetField("save",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
        Require(state.erudition==0 && state.feathers==5 && state.coins==0,"New player starts at zero");
        var legacy=new GameSave{version=1,erudition=161,solved=3}; legacy.Normalize(); Require(legacy.erudition==60 && legacy.solved==3,"Migration retains earned points");
        game.screens[0].GetComponentsInChildren<Button>(true).Single(b=>b.name=="Button_Classic").onClick.Invoke();
        var board=game.classicBoard; Require(state.feathers==4 && board.HiddenKinds>2,"Charge once at start; genuine beginner puzzle");
        Require(Cells(board).Count(c=>!c.IsHiddenLetter)==PuzzleGenerator.StartingClues(Cells(board).Length,0),"Only a few positional clues");
        var selected=Cells(board).First(c=>c.IsHiddenLetter); selected.Press();
        Require(selected.PrimarySelected && Cells(board).Where(c=>c.Code==selected.Code).All(c=>c.Selected),"Primary and matching cipher highlight");
        var hearts=board.RemainingHearts;
        var wrong=board.GetComponentsInChildren<KeyboardKeyView>(true).First(k=>!k.Used&&k.Letter!=selected.Answer);
        board.Guess(wrong.Letter,wrong); board.Guess(wrong.Letter,wrong); Require(board.RemainingHearts==hearts-1,"Repeated wrong guess costs only once");
        var hiddenBefore=Cells(board).Count(c=>c.IsHiddenLetter);
        board.UseHint(); Require(!selected.IsHiddenLetter&&Key(board,selected.Answer).Found&&state.hints==1&&Cells(board).Count(c=>c.IsHiddenLetter)==hiddenBefore-1,"Hint opens only selected cell");
        var selectedCode=board.ExportProgress().selectedCode; var index=board.PuzzleIndex;
        game.Execute(UiActionKind.Home,0); game.Execute(UiActionKind.Classic,0);
        Require(board.PuzzleIndex==index&&board.ExportProgress().selectedCode==selectedCode&&state.feathers==4,"Resume keeps selection and energy");
        Solve(game,board); Require(state.erudition==20&&state.solved==1&&state.coins==30&&state.feathers==4,"Classic rewards; no second energy charge");
        game.Execute(UiActionKind.RewardVictory,0); game.Execute(UiActionKind.RewardVictory,0); Require(state.erudition==30&&state.hints==2,"Bonus only once");
        game.Execute(UiActionKind.Turbo,0); board=game.turboBoard;
        selected=Cells(board).First(c=>c.IsHiddenLetter); selected.Press();
        foreach(var key in board.GetComponentsInChildren<KeyboardKeyView>(true).Where(k=>!k.Used&&k.Letter!=selected.Answer).Take(3)) board.Guess(key.Letter,key);
        Require(game.screens[12].activeSelf&&state.feathers==3,"Turbo defeat without extra charge"); game.Execute(UiActionKind.Retry,0); Require(board.RemainingHearts==3&&state.feathers==2,"Retry preserves generated entry");
        game.Execute(UiActionKind.DebugZeroFeathers,0); game.Execute(UiActionKind.Classic,0); Require(game.screens[5].activeSelf,"No energy");
        game.Execute(UiActionKind.RewardFeather,0); Require(state.feathers==1,"Reward restores energy");
        state.coins=1000;game.Execute(UiActionKind.BuyFifteenFeathers,0); Require(state.feathers==16&&state.reserveFeathers==0&&game.shopFeathers.text=="16"&&game.mainFeathers.text=="16","Purchased balance displayed without cap");
        state.activePuzzle=new PuzzleProgress(); state.erudition=600; state.classicSolved=1; game.Execute(UiActionKind.Classic,0); board=game.classicBoard;
        Require(PuzzleGenerator.WordCount(board.Entry.text)>=20&&board.HiddenKinds>12,"Long advanced generated paragraph");
        selected=Cells(board).Last(c=>c.IsHiddenLetter); selected.Press();
        var scroll=board.GetComponentInChildren<ScrollRect>(); Require(scroll.content.anchoredPosition.y>0,"Selecting a lower row scrolls it into view");
        var saved=board.ExportProgress(); SaveStore.Save(state); var loaded=SaveStore.Load();
        Require(loaded.activePuzzle.puzzleIndex==board.PuzzleIndex&&loaded.activePuzzle.selectedSlot==saved.selectedSlot,"Generated task and selection survive serialization");
        Solve(game,board); Require(game.presentation.coinsReward.transform.parent.name=="Reward_CoinsCard","Coins stay inside authored reward card");
        game.Execute(UiActionKind.Statistics,0); game.Execute(UiActionKind.StatisticsPeriod,2); Require(game.presentation.bars.Count(b=>b.gameObject.activeSelf)==12,"Year statistics");
        game.Execute(UiActionKind.CollectionDetails,6); Require(game.screens[1].activeSelf,"Generated stories are in collection details");
        game.Execute(UiActionKind.Back,0); Require(game.screens[7].activeSelf,"Collection back");
        game.Execute(UiActionKind.AchievementDetails,0); Require(game.presentation.popup.activeSelf,"Achievement popup"); game.Execute(UiActionKind.ClosePopup,0);
        game.Execute(UiActionKind.Settings,0);
        Require(game.settingTracks.Length==4&&game.settingThumbs.Length==4,"Four authored setting switches");
        for(var i=0;i<4;i++)
        {
            var original=game.settingValues[i].text;
            game.settingTracks[i].GetComponent<Button>().onClick.Invoke();
            Require(game.settingValues[i].text!=original,"Setting label updates "+i);
            var enabled=game.settingValues[i].text=="Включено";
            Require(game.settingThumbs[i].anchoredPosition.x==(enabled?43:-43),"Setting thumb updates "+i);
            game.settingTracks[i].GetComponent<Button>().onClick.Invoke();
            Require(game.settingValues[i].text==original,"Setting toggles back "+i);
        }
        var reloaded=SaveStore.Load();
        Require(reloaded.music==state.music&&reloaded.sound==state.sound&&reloaded.vibration==state.vibration&&reloaded.largeText==state.largeText,"Settings persist");
        TestManualPlacement(game,state,PuzzleMode.Classic);
        TestManualPlacement(game,state,PuzzleMode.Turbo);
        FeedbackTests.Run(game,state);
        if(LayoutOwnershipTests.Enabled) LayoutOwnershipTests.AssertUnchanged("after full game flow");
        Require(Objects()==count,"No objects created in runtime game flow");
    }

    static void TestManualPlacement(CryptogramGame game,GameSave state,PuzzleMode mode)
    {
        state.activePuzzle=new PuzzleProgress(); state.feathers=5; state.erudition=0; state.hints=2;
        game.Execute(mode==PuzzleMode.Classic?UiActionKind.Classic:UiActionKind.Turbo,0);
        var board=mode==PuzzleMode.Classic?game.classicBoard:game.turboBoard;
        var puzzle=new PuzzleEntry{id="manual-placement-regression",mode=mode,text="МАМА ПАПА ЛАЛА",source="Тест"};
        board.StartPuzzle(puzzle,board.PuzzleIndex,new PuzzleProgress(),0,2);
        var group=Cells(board).Where(c=>c.IsHiddenLetter).GroupBy(c=>c.Code).First(g=>g.Count()>1&&!Key(board,g.First().Answer).Found).ToArray();
        var selected=group[0]; var key=Key(board,selected.Answer); var hidden=Cells(board).Count(c=>c.IsHiddenLetter);
        Require(!key.Found&&!key.Used,"Unknown key starts neutral: "+mode);
        selected.Press(); key.Press();
        Require(!selected.IsHiddenLetter&&group.Skip(1).All(c=>c.IsHiddenLetter)&&Cells(board).Count(c=>c.IsHiddenLetter)==hidden-1,"Only chosen occurrence revealed: "+mode);
        Require(key.Found&&!key.Used&&key.GetComponent<Button>().interactable,"Discovered key stays usable: "+mode);
        key.ResetVisual(); var green=key.GetComponent<Image>().color;
        Require(green.g>green.r&&green.g>green.b,"Green persists after animation reset: "+mode);
        var hearts=board.RemainingHearts;
        var other=Cells(board).First(c=>c.IsHiddenLetter&&c.Code!=selected.Code); other.Press();
        key.Press(); key.Press();
        Require(board.RemainingHearts==hearts-1&&other.IsHiddenLetter,"Known key in wrong cell still validates, repeated mistake charged once: "+mode);
        key.ResetVisual(); Require(key.Found&&!key.Used&&key.GetComponent<Image>().color==green,"Wrong attempt does not erase discovery: "+mode);
        group[1].Press();
        var progress=board.ExportProgress(); SaveStore.Save(state); var roundtrip=SaveStore.Load();
        Require(roundtrip.activePuzzle.filledSlots==progress.filledSlots&&!string.IsNullOrEmpty(progress.filledSlots),"Partial slots serialize: "+mode);
        board.StartPuzzle(puzzle,board.PuzzleIndex,roundtrip.activePuzzle,0,state.hints);
        Require(!group[0].IsHiddenLetter&&group.Skip(1).All(c=>c.IsHiddenLetter)&&group[1].PrimarySelected&&key.Found&&!key.Used,"Resume retains only filled cell and green key: "+mode);
        var legacy=board.ExportProgress(); legacy.revealedCodes+=","+selected.Code; legacy.filledSlots="";
        board.StartPuzzle(puzzle,board.PuzzleIndex,legacy,0,state.hints);
        Require(group.All(c=>!c.IsHiddenLetter)&&key.Used,"Old code-based saves retain all already opened letters: "+mode);
        board.StartPuzzle(puzzle,board.PuzzleIndex,roundtrip.activePuzzle,0,state.hints);
        foreach(var cell in group.Skip(1)) { cell.Press(); key.Press(); }
        Require(group.All(c=>!c.IsHiddenLetter)&&key.Used&&!key.GetComponent<Button>().interactable,"Repeats require manual input, final occurrence disables key: "+mode);
        var strike=(Image)typeof(KeyboardKeyView).GetField("strike",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(key);
        Require(strike!=null&&strike.enabled,"Exhausted key is struck: "+mode);
        hearts=board.RemainingHearts; var guesses=state.guesses; key.Press();
        Require(board.RemainingHearts==hearts&&state.guesses==guesses,"Exhausted key is inert: "+mode);
        var hinted=Cells(board).First(c=>c.IsHiddenLetter); hinted.Press(); hidden=Cells(board).Count(c=>c.IsHiddenLetter);
        board.UseHint();
        Require(!hinted.IsHiddenLetter&&Cells(board).Count(c=>c.IsHiddenLetter)==hidden-1&&Key(board,hinted.Answer).Found&&!Key(board,hinted.Answer).Used,"Hint opens one occurrence and makes key green: "+mode);
        Solve(game,board);
        Debug.Log("MANUAL_LETTER_PLACEMENT_PASS mode="+mode+" singleCell=true greenPersists=true exhaustion=true partialSave=true legacySave=true hintSingleCell=true");
    }
    static void Require(bool condition,string message) { if(!condition) throw new Exception("USABILITY_CHECK_FAILED: "+message); }
}
