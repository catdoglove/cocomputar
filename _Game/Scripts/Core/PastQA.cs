#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator PastChecks()
        {
            app.Save.NewGame("Stage02");app.Begin(false);yield return null;
            var stage=app.ScreenRoot.GetComponentInChildren<Stage02Controller>();var black=S2Icon("twin_black");var white=S2Icon("twin_white");var pos=black.Rect.anchoredPosition;
            stage.ApplyDate(new DateTime(1989,6,6));Check(!stage.Sepia,"미리 날짜 설정: 키 전달 전에는 미발동");
            yield return Icon("twin_black");yield return Hold(Key.LeftCtrl,3);stage.WhiteTaken=true;stage.Receive("white.key","twins");yield return Hold(Key.LeftCtrl,3);
            int eerie=0,keys=0;Action<string> heard=k=>{if(k=="sfx_sepia_on")eerie++;if(k=="sfx_key_get")keys++;};app.Audio.Played+=heard;
            stage.BlackTaken=true;stage.Receive("black.key","twins");
            Check(stage.KeysDelivered&&app.Dialogue.Active&&!stage.KeysDialogueCompleted&&!stage.Sepia&&black.gameObject.activeSelf&&app.Audio.MusicPlaying&&eerie==0,"두 키 전달 직후: 대화 유지·NPC/BGM 유지·과거 미진입");
            stage.ApplyDate(new DateTime(1989,6,6));Check(!stage.Sepia,"대화 중 재적용도 과거 진입 차단");
            yield return Hold(Key.LeftCtrl,6);
            Check(!app.Dialogue.Active&&stage.KeysDialogueCompleted&&stage.Sepia&&stage.DateLocked&&!black.gameObject.activeSelf&&!white.gameObject.activeSelf&&eerie==1&&!app.Audio.MusicPlaying,"NPC 대화 완전 종료 후 과거 1회 진입");
            app.ClockClicked();yield return new WaitForSeconds(.3f);var clock=app.Windows.GetComponentInChildren<DateTimeWindow>();
            Check(!clock.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.text.Contains("현재 날짜")),"수동 현재 복귀 버튼 제거");
            Check(clock.GetComponentsInChildren<Button>().Where(b=>b.GetComponentInChildren<TextMeshProUGUI>()?.text!="X"&&b.GetComponentInChildren<TextMeshProUGUI>()?.text!="취소").All(b=>!b.interactable),"과거 날짜·시간 조작 잠금");
            stage.ApplyDate(DateTime.Now);Check(stage.Sepia&&app.DisplayDate.Value.Year==1989,"직접 현재 날짜 적용으로도 복귀 불가");
            yield return Icon("landscapes");yield return Icon("trash");WindowApps.Note("검증 문서","내용 보존");
            var files=stage.FileStore.Files.ToDictionary(f=>f.Id,f=>f.Location);
            int closed=0;bool order=true;var windows=app.Windows.GetComponentsInChildren<FakeWindow>();
            foreach(var w in windows){var previous=w.Closed;w.Closed=()=>{order&=stage.Sepia&&app.DisplayDate?.Year==1989&&!black.gameObject.activeSelf;closed++;previous?.Invoke();};}
            stage.OpenOutdoor();yield return new WaitForSeconds(.3f);
            for(int i=0;i<3;i++){var plant=app.Windows.GetComponentInChildren<OutdoorWindow>().transform.Find("식물/식물 슬롯 "+i).GetComponentInChildren<Button>();yield return MouseClick((RectTransform)plant.transform);}
            Check(stage.Sepia&&!stage.BouquetTaken&&closed==0,"3개 수집까지 과거·열린 창 유지");
            // 빠른 완료에도 불안 효과음만 정지하고 획득음 경로는 유지한다.
            app.Audio.Play("sfx_sepia_on");var last=app.Windows.GetComponentInChildren<OutdoorWindow>().transform.Find("식물/식물 슬롯 3").GetComponentInChildren<Button>();yield return MouseClick((RectTransform)last.transform);
            Check(stage.BouquetTaken&&stage.FileStore.Find("bouquet")!=null&&keys==4,"네 번째 튤립 클릭: 꽃다발 1개·획득음 각 1회");
            Check(closed==windows.Length&&order&&app.Windows.GetComponentsInChildren<FakeWindow>().Length==0,"모든 창 정상 종료가 날짜·NPC 복원보다 먼저 실행");
            Check(app.DisplayDate==null&&!stage.Sepia&&!stage.DateLocked&&stage.OutdoorNight&&black.gameObject.activeSelf&&white.gameObject.activeSelf&&black.Rect.anchoredPosition==pos,"동일 완료 처리에서 현재 시각·NPC·밤 상태 자동 복원");
            Check(app.Audio.MusicPlaying&&!app.Audio.DateEffectPlaying,"BGM 재개·불안 효과음 종료");
            Check(files.All(p=>stage.FileStore.Find(p.Key)?.Location==p.Value),"창 닫기 후 모든 기존 파일과 폴더 위치 보존");
            yield return new WaitForSeconds(1.2f);Check(app.Catalog.crt.sepiaAmount==0,"세피아 해제 완료");
            yield return Icon("landscapes");Check(app.Windows.GetComponentInChildren<FakeWindow>().Content.GetComponentsInChildren<FakeIcon>().Any(i=>i.Id=="sunset"),"폴더 다시 열기: 기존 아이콘 남아 있음");
            stage.OpenOutdoor();yield return new WaitForSeconds(.3f);var outdoor=app.Windows.GetComponentInChildren<OutdoorWindow>();Check(outdoor.transform.Find("밤 배경")&&!outdoor.GetComponentInChildren<DropTarget>()&&!outdoor.Water(),"밤 배경·구름 제거·물뿌리개 차단 유지");Capture("outdoor-night");
            app.ClockClicked();yield return new WaitForSeconds(.3f);Check(app.Windows.GetComponentInChildren<DateTimeWindow>().GetComponentsInChildren<Button>().All(b=>b.interactable),"자동 복귀 후 날짜 조작 활성");
            stage.Receive("bouquet","twins");yield return Hold(Key.LeftCtrl,6);Check(stage.OptionalSolved,"복원 NPC에게 꽃다발 전달 가능");
            app.Audio.Played-=heard;app.Begin(false);yield return null;stage=app.ScreenRoot.GetComponentInChildren<Stage02Controller>();Check(!stage.KeysDialogueCompleted&&!stage.OutdoorNight&&!stage.Sepia,"이어하기 새 대화완료·과거 상태 초기화");
        }
    }
}
#endif
