#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator RestartChecks()
        {
            app.Save.NewGame("Stage02");app.Save.Flag("stage01.mail");app.Save.Flag("stage03.marker");app.Save.Progress.cleared.AddRange(new[]{"Stage01","Stage02"});app.Begin(false);yield return null;
            var stage=app.ScreenRoot.GetComponentInChildren<Stage02Controller>();yield return Icon("twin_black");yield return Hold(Key.LeftCtrl,3);
            stage.WhiteTaken=true;stage.Receive("white.key","twins");yield return Hold(Key.LeftCtrl,3);
            stage.BlackTaken=true;stage.Receive("black.key","twins");yield return Hold(Key.LeftCtrl,3);
            stage.ApplyDate(new DateTime(1989,6,6));for(int i=0;i<4;i++)stage.PickTulip(i);
            stage.Receive("bouquet","twins");yield return Hold(Key.LeftCtrl,3);
            Check(stage.OptionalSolved&&stage.TulipsPicked.All(v=>v)&&stage.KeysDelivered,"완료한 실제 튤립·키 진행 준비");
            app.Save.Options.music=.21f;app.Save.Options.effects=.43f;app.Save.Options.filter=.34f;app.Save.Options.curvature=.76f;app.Save.SetHidden("desktop",true);app.Save.SaveOptions();
            string options=JsonUtility.ToJson(app.Save.Options);var cleared=app.Save.Progress.cleared.ToArray();
            stage.Rained=true;stage.ExitTaken=true;for(int i=0;i<4;i++)stage.Buds[i]=true;stage.Assembly.Merge("d","o1");
            app.DisplayDate=new DateTime(1989,6,6);StoryEffects.Get().Sepia(true);app.Audio.PlayPuzzle(stage.Data.rainSound,true,true);
            Check(app.Audio.EffectsPlaying&&app.Catalog.crt.sepiaAmount==1,"초기화 전 필터·효과음 재생 상태");
            app.Begin(false);yield return null;stage=app.ScreenRoot.GetComponentInChildren<Stage02Controller>();
            Check(!stage.OptionalSolved&&!new CoComputar.SaveService().Progress.flags.Contains("stage02.tulipSolved"),"이어하기: 메모리·저장 파일의 현재 튤립 기록 삭제");
            Check(app.Save.Progress.flags.Contains("stage01.mail")&&app.Save.Progress.flags.Contains("stage03.marker"),"다른 스테이지 기록 보존");
            Check(app.Save.Progress.cleared.SequenceEqual(cleared)&&app.Save.Progress.stageId=="Stage02","클리어·해금 및 재시작 스테이지 보존");
            Check(JsonUtility.ToJson(app.Save.Options)==options&&JsonUtility.ToJson(new CoComputar.SaveService().Options)==options,"볼륨·CRT·숨김 옵션 메모리와 저장 파일 보존");
            Check(stage.Phase==0&&!stage.WhiteTaken&&!stage.BlackTaken&&!stage.KeysDelivered&&!stage.BouquetTaken&&!stage.ExitTaken&&!stage.Rained,"키 획득·전달 및 나머지 퍼즐 상태 초기화");
            Check(stage.TulipsPicked.All(v=>!v)&&stage.Buds.All(v=>!v)&&stage.Assembly.Groups.Count==4,"튤립 4개·새싹 4개·door 조각 초기화");
            Check(!app.Dialogue.Active,"재시작 전 대화·완료 콜백 취소");
            Check(app.DisplayDate==null&&!stage.Sepia&&app.Catalog.crt.sepiaAmount==0&&app.Catalog.crt.glitchAmount==0&&!app.Audio.EffectsPlaying,"게임 날짜·이벤트·세피아·잔류 효과음 초기화");
            Check(!stage.FileStore.Files.Any(f=>new[]{"white.key","black.key","bouquet","key.png","door.exe"}.Contains(f.Id)),"이전 획득 파일 제거");
            stage.ApplyDate(new DateTime(1989,6,6));Check(!stage.Sepia,"재시작 직후 정답 날짜도 키 전달 전에는 무효");
            yield return Icon("twin_black");yield return Hold(Key.LeftCtrl,3);stage.WhiteTaken=true;stage.Receive("white.key","twins");yield return Hold(Key.LeftCtrl,3);
            int count=0;Action<string> heard=key=>{if(key=="sfx_sepia_on")count++;};app.Audio.Played+=heard;
            stage.BlackTaken=true;stage.Receive("black.key","twins");yield return Hold(Key.LeftCtrl,5);Check(stage.KeysDelivered&&stage.Sepia&&count==1,"재시작 후 두 키 전달: 이미 발동함 상태 초기화·날짜 이벤트 재발동");
            stage.ApplyDate(new DateTime(1989,6,6));Check(count==1,"새 실행 내 중복 발동 방지 유지");app.Audio.Played-=heard;yield return Hold(Key.LeftCtrl,3);
            app.Save.Flag("stage02.tulipSolved");app.Save.Enter("Stage03");app.Begin(false);yield return null;
            Check(app.Save.Progress.flags.Contains("stage02.tulipSolved"),"다른 스테이지 이어하기는 Stage02 기록을 지우지 않음");
        }
    }
}
#endif
