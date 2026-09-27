using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Erudition;
using UnityEngine;

public static class FeedbackTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("FEEDBACK_TEST_FAILED: "+message);}
    static object Call(CryptogramGame game,string name,params object[] args)=>typeof(CryptogramGame).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,args);
    public static void Run(CryptogramGame game,GameSave state)
    {
        var old=new GameSave{version=2,feathers=5,reserveFeathers=85,nextFeatherUtcTicks=1};old.Normalize();old.Normalize();
        Check(old.feathers==90&&old.reserveFeathers==0&&old.nextFeatherUtcTicks==0,"90 legacy feathers preserved exactly once");
        state.feathers=90;state.nextFeatherUtcTicks=DateTime.UtcNow.AddDays(-1).Ticks;Call(game,"RestoreEnergy");
        Check(state.feathers==90&&state.nextFeatherUtcTicks==0,"No regeneration above 5");
        Call(game,"SpendFeather");Check(state.feathers==89&&state.nextFeatherUtcTicks==0,"Spend high balance");
        state.feathers=5;Call(game,"SpendFeather");Check(state.feathers==4&&state.nextFeatherUtcTicks>DateTime.UtcNow.Ticks,"Recovery starts below 5");
        state.nextFeatherUtcTicks=DateTime.UtcNow.AddMinutes(-45).Ticks;Call(game,"RestoreEnergy");Check(state.feathers==5&&state.nextFeatherUtcTicks==0,"Regeneration stops at 5");
        state.feathers=0;state.nextFeatherUtcTicks=DateTime.UtcNow.AddMinutes(-21).Ticks;Call(game,"RestoreEnergy");Check(state.feathers==2,"Offline recovery grants correct amount");
        var choose=typeof(CryptogramGame).GetMethod("ChoosePuzzle",BindingFlags.Instance|BindingFlags.NonPublic);
        foreach(var points in new[]{0,60,160,320,600})
        {
            state.erudition=points;state.recentTexts.Clear();var last=new Queue<string>();
            for(var attempt=0;attempt<120;attempt++)
            {
                state.classicSolved=attempt/2;var mode=(PuzzleMode)(attempt%2);var board=mode==PuzzleMode.Classic?game.classicBoard:game.turboBoard;
                var index=(int)choose.Invoke(game,new object[]{mode,board});Check(index>=0,"Eligible unique text");
                var key=PuzzleGenerator.TextKey(game.Entries[index].text);Check(!last.Contains(key),"No repeat inside ten attempts, tier "+points);
                last.Enqueue(key);if(last.Count>10)last.Dequeue();state.recentTexts.Add(key);if(state.recentTexts.Count>10)state.recentTexts.RemoveAt(0);
            }
        }
        SaveStore.Save(state);var loaded=SaveStore.Load();Check(loaded.recentTexts.SequenceEqual(state.recentTexts),"Ten-text memory survives restart");
        state.erudition=0;state.classicSolved=0;state.feathers=90;state.activePuzzle=new PuzzleProgress();game.Execute(UiActionKind.Classic,0);
        Check(state.feathers==89&&game.mainFeathers.text=="89","New level charges total once");var history=state.recentTexts.ToArray();
        game.Execute(UiActionKind.Home,0);game.Execute(UiActionKind.Classic,0);
        Check(state.feathers==89&&state.recentTexts.SequenceEqual(history),"Resume has no charge or extra history entry");
        game.FailPuzzle(game.classicBoard);game.FailPuzzle(game.classicBoard);Check(state.feathers==89,"Failure cannot charge again");
        state.feathers=0;AdsBridge.EditorTestRewards=false;var hints=state.hints;game.Execute(UiActionKind.RewardFeather,0);Check(state.hints==hints&&state.feathers==0,"Ordinary editor never grants fake rewards");AdsBridge.EditorTestRewards=true;
        foreach(var group in game.collectionGroups)
        {
            var scroll=group.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            var first=scroll.content.GetChild(0) as RectTransform;
            Check(first.anchorMin.y==1&&first.anchorMax.y==1&&Mathf.Abs(first.anchoredPosition.y+first.rect.height/2)<20,"Collection starts at viewport top");
        }
        var unique=game.Entries.Select(e=>PuzzleGenerator.TextKey(e.text)).Distinct().Count();
        Debug.Log("FEEDBACK_TEST_PASS migration90=true regenCap5=true purchaseTotal=true energyOnStartOnly=true noRepeat600Selections=true persistedHistory=true uniqueTexts="+unique);
    }
}
