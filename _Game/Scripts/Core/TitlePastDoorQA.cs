#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator TitlePastDoorChecks()
        {
            Check(app.Catalog.theme.titleFont&&app.Catalog.theme.titleFont!=app.Catalog.theme.font,"타이틀 Neo와 게임 Galmuri 별도 연결");
            yield return FontPreview();
            app.Save.NewGame("Stage02");app.Begin(false);yield return null;
            var stage=app.ScreenRoot.GetComponentInChildren<Stage02Controller>();stage.WhiteTaken=true;stage.Receive("white.key","twins");yield return FinishDialogue();
            stage.BlackTaken=true;stage.Receive("black.key","twins");yield return FinishDialogue();
            yield return Icon("key.png");yield return new WaitForSeconds(.25f);var viewer=app.Windows.GetComponentInChildren<ImageViewer>();viewer.SetZoom(-5);yield return MouseClick((RectTransform)viewer.PictureRoot.Find("exit-folder"));app.CloseAllWindows();yield return null;
            stage.Assembly.Merge("r","o2");stage.Assembly.Merge("d","o1");stage.Assembly.Merge("o1","o2");yield return Icon("exit");yield return new WaitForSeconds(.25f);
            Check(stage.FileStore.Find("door.exe").BlockedInPast,"door.exe 데이터에 과거 실행 제한 등록");
            stage.ApplyDate(new DateTime(1989,6,6));Check(stage.Sepia,"키 대화 후 실제 과거 진입");yield return new WaitForSeconds(.25f);
            int errors=0;Action<string> heard=k=>{if(k=="sfx_error")errors++;};app.Audio.Played+=heard;
            yield return MouseClick(Icons().Single(i=>i.Id=="door.exe").Rect);Check(errors==1&&app.Stages.State!=StageState.Transition,"과거 door 실제 클릭 거부음·로딩 없음");
            foreach(var location in new[]{RecycleFiles.Desktop,RecycleFiles.Collection,RecycleFiles.Exit})
            {stage.FileStore.Move("door.exe",location);for(int i=0;i<3;i++)stage.FileStore.Open("door.exe");Check(app.Stages.State!=StageState.Transition,"위치 변경·연속 실행 공통 차단 "+location);}
            Check(errors==10,"거부 효과음 공통 경로");
            stage.FileStore.Open("sunset");yield return new WaitForSeconds(.2f);Check(app.Windows.GetComponentInChildren<ImageViewer>(),"다른 파일은 과거에도 열림");
            for(int i=0;i<4;i++)stage.PickTulip(i);
            Check(stage.BouquetTaken&&!stage.Sepia,"꽃다발 완료 후 자동 현재 복귀");
            app.Audio.Played-=heard;stage.FileStore.Open("door.exe");Check(app.Stages.State==StageState.Transition,"현재 복귀 즉시 door 실행 가능");
            var date=app.DisplayDate;stage.ApplyDate(new DateTime(1989,6,6));Check(stage.DateLocked&&!stage.Sepia&&app.DisplayDate==date,"로딩 시작 후 날짜 변경·과거 진입 차단");
            float deadline=Time.realtimeSinceStartup+20;while(app.Stages.Current.stageId!="Stage03"&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(app.Stages.Current.stageId=="Stage03","현재 door 실행 후 다음 스테이지 전환");
        }
    }
}
#endif
