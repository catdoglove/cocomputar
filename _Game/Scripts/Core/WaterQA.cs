#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator WaterChecks()
        {
            app.Save.NewGame("Stage02");app.Begin(false);yield return new WaitForSeconds(.2f);
            var stage=app.ScreenRoot.GetComponentInChildren<Stage02Controller>();yield return Icon("twin_black");yield return Hold(Key.LeftCtrl,3);
            stage.WhiteTaken=true;stage.Receive("white.key","twins");yield return Hold(Key.LeftCtrl,3);
            stage.FileStore.Move("watering",RecycleFiles.Desktop);stage.OpenOutdoor();yield return new WaitForSeconds(.3f);
            var outdoor=app.Windows.GetComponentInChildren<OutdoorWindow>();outdoor.GetComponentInParent<FakeWindow>().Rect.anchoredPosition=new Vector2(20,-60);
            int fails=0,rains=0;Action<string> heard=key=>{if(key==stage.Data.wateringFailureSound)fails++;if(key==stage.Data.rainSound)rains++;};app.Audio.Played+=heard;
            var origin=S2Icon("watering").Rect.anchoredPosition;
            yield return MouseDrag(Center(S2Icon("watering").Rect),RectTransformUtility.WorldToScreenPoint(app.UICamera,app.ScreenRoot.TransformPoint(new Vector3(150,-500,0))));
            Check(fails==1&&!stage.Rained&&S2Icon("watering").Rect.anchoredPosition==origin,"엉뚱한 곳 실제 드롭: 실패음·원위치·상태 유지");
            for(int i=0;i<20;i++)stage.DropFile("watering","drawn-cloud");
            Check(fails==1&&rains==0,"빠른 실패 20회 중복 재생 차단");
            yield return new WaitForSeconds(.35f);stage.DropFile("watering","");Check(fails==2,"실패음 종료 후 재시도 피드백");
            var cloud=outdoor.GetComponentInChildren<DropTarget>();float timeout=Time.realtimeSinceStartup+12;
            while(cloud.Category!="photo-cloud"&&Time.realtimeSinceStartup<timeout)yield return null;
            stage.DropFile("watering","");float began=Time.realtimeSinceStartup;Check(stage.DropFile("watering","photo-cloud"),"정답 구름 성공");
            int failsBefore=fails;
            Check(rains==1&&app.Audio.PuzzlePlaying(stage.Data.rainSound)&&!app.Audio.PuzzlePlaying(stage.Data.wateringFailureSound)&&outdoor.transform.Find("비"),"성공 시 실패음 교체 및 빗소리·빗방울 동시 시작");
            for(int i=0;i<20;i++)stage.DropFile("watering","photo-cloud");
            Check(rains==1&&fails==failsBefore,"비가 내리는 중 연속 드롭: 효과음 겹침 없음");
            while(!stage.Rained&&Time.realtimeSinceStartup-began<5)yield return null;yield return null;
            Check(stage.Rained&&Mathf.Abs(Time.realtimeSinceStartup-began-stage.Data.rainSeconds)<.2f,"설정된 강우 시간과 연출 종료 일치");
            Check(!app.Audio.PuzzlePlaying(stage.Data.rainSound)&&!outdoor.transform.Find("비"),"비 연출과 소리 동시 종료");
            for(int i=0;i<20;i++)stage.DropFile("watering","photo-cloud");Check(rains==1&&stage.Rained,"완료 후 비 중복 재생·퍼즐 재실행 없음");
            app.CloseAllWindows();yield return null;stage.OpenOutdoor();yield return new WaitForSeconds(.2f);Check(!stage.DropFile("watering","photo-cloud")&&rains==1,"창 재열기 후에도 비 재실행 차단");
            // 진행 중 창을 닫으면 비 소리가 남지 않으며 재시도할 수 있다.
            stage.Rained=false;outdoor=app.Windows.GetComponentInChildren<OutdoorWindow>();cloud=outdoor.GetComponentInChildren<DropTarget>();timeout=Time.realtimeSinceStartup+12;
            while(cloud.Category!="photo-cloud"&&Time.realtimeSinceStartup<timeout)yield return null;
            stage.DropFile("watering","photo-cloud");yield return new WaitForSeconds(.2f);outdoor.GetComponentInParent<FakeWindow>().Close();yield return null;
            Check(!app.Audio.PuzzlePlaying(stage.Data.rainSound)&&!stage.Rained,"강우 도중 창 닫기: 소리 즉시 정지·미완료 상태 유지");
            Check(app.Catalog.audio.Get(stage.Data.rainSound)!=app.Catalog.audio.Get("sfx_ding")&&app.Catalog.audio.Get(stage.Data.wateringFailureSound),"전용 비·실패 음원 데이터 등록");
            app.Audio.Played-=heard;
        }
    }
}
#endif
