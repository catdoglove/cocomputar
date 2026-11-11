#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator KeyDialogueVisual()
        {
            app.Save.NewGame("Stage02");app.Begin(false);yield return null;
            app.Dialogue.Play(()=>{},"dialogue.stage2.firstKey.white.beforeTalk.deliver");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftCtrl));
            DialogueTextEcho echo=null;float deadline=Time.time+10;
            while(!echo&&Time.time<deadline){echo=app.Modal.GetComponentInChildren<DialogueTextEcho>();yield return null;}
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            yield return new WaitForSeconds(.1f);
            Check(echo&&echo.isActiveAndEnabled,"동시 대사 화면 유지");
            var text=echo.GetComponent<TMP_Text>();
            Check(text.text==echo.Source.text&&text.maxVisibleCharacters==echo.Source.maxVisibleCharacters,"흑백 동시 타이핑 표시 동기화");
            Capture("key-dialogue-overlap");
        }
        IEnumerator KeyDialogueFinish(bool overlapExpected,Stage02Controller stage,bool final)
        {
            bool overlapSeen=false,quietReply=false,resumed=false,mutterSeen=false,earlyPast=false;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftCtrl));
            float deadline=Time.time+20;
            while(app.Dialogue.Active&&Time.time<deadline)
            {
                earlyPast|=stage.Sepia;
                var echo=app.Modal.GetComponentInChildren<DialogueTextEcho>();
                if(echo&&!overlapSeen)
                {
                    overlapSeen=true;
                    Check(!app.Audio.MusicPlaying,"동시 대사에서 BGM 정지");
                    var text=echo.GetComponent<TMP_Text>();
                    Check(echo.Source.color==Color.black&&text.color==Color.white,"흑/백 두 글자색");
                    Capture("key-dialogue-overlap");
                    if(text.text==echo.Source.text&&text.maxVisibleCharacters==echo.Source.maxVisibleCharacters)
                    {
                        if(!results.Any(r=>r.Contains("동시 타이핑")))Check(true,"동시 타이핑·Ctrl 표시 수 일치");
                    }
                }
                var texts=app.Modal.GetComponentsInChildren<TMP_Text>();
                if(texts.Any(t=>t.text=="..네"))quietReply|=!app.Audio.MusicPlaying;
                if(overlapSeen&&!echo&&!texts.Any(t=>t.text=="..네"))resumed|=app.Audio.MusicPlaying;
                mutterSeen|=texts.Any(t=>t.text.StartsWith("(중얼)")&&t.fontSize==20);
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            Check(!app.Dialogue.Active&&!earlyPast,"Ctrl 종료·대화 중 과거 진입 없음");
            if(overlapExpected)Check(overlapSeen&&quietReply&&resumed,"겹침 → V양 답변까지 무음 → 다음 NPC 발화에서 재생");
            if(final&&!stage.TalkedBeforeFirstKey)Check(mutterSeen,"중얼 대사 작은 글씨");
        }
        IEnumerator AcquireDialogueKey(Stage02Controller stage,string key)
        {
            if(key=="white")
            {
                stage.FileStore.Open("sunset");yield return new WaitForSeconds(.3f);
                var viewer=app.Windows.GetComponentInChildren<ImageViewer>();
                yield return MouseClick((RectTransform)viewer.PictureRoot.Find("white-key"));
                Check(stage.WhiteTaken,"풍경 퍼즐 열쇠 획득");
            }
            else
            {
                Check(stage.FileStore.Move("watering",RecycleFiles.Desktop),"물뿌리개 휴지통 복원");
                stage.OpenOutdoor();yield return new WaitForSeconds(.3f);
                var outdoor=app.Windows.GetComponentInChildren<OutdoorWindow>();
                float end=Time.time+12;
                while(Time.time<end&&!outdoor.GetComponentsInChildren<DropTarget>().Any(t=>t.Category=="photo-cloud"))yield return null;
                Check(stage.Receive("watering","photo-cloud"),"첫 열쇠 전달 전에도 올바른 구름에 물뿌리개 사용");
                yield return new WaitForSeconds(2);
                var plant=outdoor.transform.Find("식물/식물 슬롯 "+stage.BlackFlowerIndex);
                yield return MouseClick((RectTransform)plant.GetComponentInChildren<Button>().transform);
                yield return null;
                yield return MouseClick((RectTransform)outdoor.transform.Find("식물/식물 슬롯 "+stage.BlackFlowerIndex+"/검은 열쇠"));
                Check(stage.BlackTaken,"야외 퍼즐 검은 열쇠 획득");
            }
            app.CloseAllWindows();yield return null;
        }
        IEnumerator KeyDialogueChecks()
        {
            foreach(bool talked in new[]{true,false})foreach(string first in new[]{"white","black"})
            {
                string label=first+"/"+talked;
                app.Save.NewGame("Stage02");app.Save.Flag("stage01.keep");app.Begin(false);yield return null;
                var stage=app.ScreenRoot.GetComponentInChildren<Stage02Controller>();
                if(talked){stage.Talk();Check(app.Dialogue.CurrentSequence=="S2_INTRO","일반 인트로");yield return KeyDialogueFinish(false,stage,false);}
                yield return AcquireDialogueKey(stage,first);
                yield return Icon("landscapes");yield return new WaitForSeconds(.3f);
                app.Windows.GetComponentInChildren<FakeWindow>().Rect.anchoredPosition=new Vector2(20,-400);
                yield return S2Drag(first+".key",S2Icon("twin_white").Rect);
                yield return null;
                string prefix="dialogue.stage2.firstKey."+first+"."+(talked?"afterTalk":"beforeTalk")+".";
                Check(stage.FirstKey==first&&stage.TalkedBeforeFirstKey==talked&&stage.Phase==2,label+": 첫 전달 분기 상태");
                Check(app.Dialogue.CurrentSequence==(talked&&first=="white"?"S2_P1_OK":prefix+"deliver"),label+": 전달 대사 선택");
                Check(!app.Windows.GetComponentsInChildren<FakeWindow>().Any(),label+": 대화 시 모든 창 닫힘");
                var saved=new SaveService();Check(saved.Progress.stage02FirstKey==first&&saved.Progress.stage02TalkedBeforeFirstKey==talked,label+": 실제 JSON 저장·로드");
                yield return KeyDialogueFinish(!talked,stage,false);
                stage.Talk();Check(app.Dialogue.CurrentSequence==(talked?"S2_P2_PRE":prefix+"repeatBefore"),label+": 첫 전달 후 반복·인트로 중복 없음");
                yield return KeyDialogueFinish(false,stage,false);
                yield return AcquireDialogueKey(stage,first=="white"?"black":"white");
                stage.ApplyDate(new DateTime(1989,6,6));Check(!stage.Sepia,label+": 열쇠 두 개 전 과거 차단");
                yield return S2Drag(first=="white"?"black.key":"white.key",S2Icon("twin_black").Rect);
                Check(stage.KeysDelivered&&!stage.KeysDialogueCompleted&&!stage.Sepia,label+": 두 열쇠 전달 직후 대화 우선");
                Check(app.Dialogue.CurrentSequence==(talked&&first=="white"?"S2_P2_OK":prefix+"secondDeliver"),label+": 두 번째 전달 대사");
                yield return KeyDialogueFinish(false,stage,true);
                Check(stage.Sepia&&stage.KeysDialogueCompleted&&stage.FileStore.Find("key.png")!=null,label+": 대화 끝에서만 출구 단서·과거 진입");
                for(int i=0;i<4;i++)stage.PickTulip(i);yield return null;
                Check(!stage.Sepia&&stage.OutdoorNight,label+": 꽃다발 자동 복귀 유지");
                stage.Talk();Check(app.Dialogue.CurrentSequence==(talked?"S2_P2_PRE":prefix+"repeatAfter"),label+": 두 전달 후 반복");
                yield return KeyDialogueFinish(false,stage,false);
                float music=app.Save.Options.music;
                app.Begin(false);yield return null;stage=app.ScreenRoot.GetComponentInChildren<Stage02Controller>();
                Check(stage.FirstKey==""&&!stage.TalkedBeforeFirstKey&&stage.Phase==0&&app.Save.Progress.flags.Contains("stage01.keep")&&app.Save.Options.music==music,label+": 이어하기 분기 초기화·다른 기록/옵션 보존");
            }
        }
    }
}
#endif
