#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator DateChecks()
        {
            app.Save.NewGame("Stage02");app.Begin(false);yield return new WaitForSeconds(.2f);
            var stage=app.ScreenRoot.GetComponentInChildren<Stage02Controller>();
            int eerie=0;Action<string> heard=key=>{if(key=="sfx_sepia_on")eerie++;};app.Audio.Played+=heard;
            app.ClockClicked();yield return new WaitForSeconds(.3f);var window=app.Windows.GetComponentInChildren<DateTimeWindow>();
            var year=window.GetComponentsInChildren<TextMeshProUGUI>().First(t=>t.text.EndsWith("년 ▼"));yield return MouseClick((RectTransform)year.GetComponentInParent<Button>().transform);
            Check(window.transform.Find("내용/년도 선택")||window.GetComponentInChildren<ScrollRect>(),"년도 스크롤 목록 열림");
            Capture("date-year-dropdown");
            var choice=window.GetComponentsInChildren<TextMeshProUGUI>().First(t=>t.text=="1989년");var scroll=window.GetComponentInChildren<ScrollRect>();scroll.verticalNormalizedPosition=1-(2100-1989)*29f/(201*29-204);yield return null;yield return MouseClick((RectTransform)choice.GetComponentInParent<Button>().transform);
            Check(year.text=="1989년 ▼","년도 목록 선택 반영");
            var hands=window.GetComponentsInChildren<RectTransform>();var hour=hands.First(t=>t.name=="시침");var minute=hands.First(t=>t.name=="분침");var second=hands.First(t=>t.name=="초침");
            float h=hour.localEulerAngles.z,m=minute.localEulerAngles.z,sec=second.localEulerAngles.z;var began=DateTime.UtcNow;
            yield return new WaitForSecondsRealtime(2.2f);float elapsed=(float)(DateTime.UtcNow-began).TotalSeconds;
            Check(Mathf.Abs(Mathf.DeltaAngle(h,hour.localEulerAngles.z)+elapsed/120)<.01f&&Mathf.Abs(Mathf.DeltaAngle(m,minute.localEulerAngles.z)+elapsed/10)<.03f,"시침·분침 경과 시간에 따라 부드러운 이동");
            Check(Mathf.Abs(Mathf.DeltaAngle(sec,second.localEulerAngles.z))>=12&&Mathf.Abs(Mathf.DeltaAngle(sec,second.localEulerAngles.z))<=18,"초침 1초 단위 이동");
            Check(!stage.Sepia&&eerie==0,"날짜 UI만 조작할 때 이벤트 없음");app.CloseAllWindows();
            var chosen=new DateTime(1989,6,6,13,24,35);stage.ApplyDate(chosen);var before=app.DisplayDate.Value;
            yield return new WaitForSecondsRealtime(1.2f);Check((app.DisplayDate.Value-before).TotalSeconds>=1.15,"설정 날짜도 실시간 진행");
            app.ClockClicked();yield return new WaitForSeconds(.2f);window=app.Windows.GetComponentInChildren<DateTimeWindow>();
            var digital=window.GetComponentsInChildren<TextMeshProUGUI>().First(t=>System.Text.RegularExpressions.Regex.IsMatch(t.text,"^[0-9]{2}:[0-9]{2}:[0-9]{2}$"));
            Check(Math.Abs((TimeSpan.Parse(digital.text)-app.DisplayDate.Value.TimeOfDay).TotalSeconds)<1.1,"창 재열기 시 현재 설정 시각 복원");app.CloseAllWindows();
            Check(!stage.KeysDelivered&&!stage.Sepia&&eerie==0,"전달 전 정답 날짜: 필터·효과음 없음");
            yield return Icon("twin_black");yield return Hold(Key.LeftCtrl,3);
            stage.WhiteTaken=true;stage.Receive("white.key","twins");yield return Hold(Key.LeftCtrl,3);
            Check(!stage.KeysDelivered&&!stage.Sepia&&eerie==0,"흰 열쇠만 전달: 이벤트 차단");
            stage.BlackTaken=true;stage.Receive("black.key","twins");
            Check(stage.KeysDelivered&&!stage.Sepia&&eerie==0,"키 전달 대화 중 과거 진입 대기");yield return Hold(Key.LeftCtrl,5);
            Check(stage.KeysDialogueCompleted&&stage.Sepia&&eerie==1,"키 전달 대화 종료 후 과거 1회 진입");
            for(int i=0;i<20;i++)stage.ApplyDate(chosen);
            Check(eerie==1,"적용 연타에도 기괴한 효과음 중복 없음");
            yield return Hold(Key.LeftCtrl,3);stage.OpenOutdoor();yield return new WaitForSeconds(.2f);Capture("date-event-active");
            Check(app.Catalog.audio.Get("sfx_sepia_on")!=app.Catalog.audio.Get("sfx_ding")&&app.Catalog.audio.Get("sfx_sepia_on").length>2,"전용 불안 효과음 등록");
            app.Audio.Played-=heard;
        }
    }
}
#endif
