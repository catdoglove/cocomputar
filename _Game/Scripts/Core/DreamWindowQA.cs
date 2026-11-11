#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        void CheckDreamLayout(DreamWindow dream,string label)
        {
            var bounds=dream.DesktopBounds;var size=dream.Window.Rect.rect.size;var position=dream.Window.Rect.anchoredPosition;
            Check(Vector2.Distance(size,bounds.size*.7f)<.1f,label+" 바탕화면 메뉴 제외 70% 크기");
            Check(Vector2.Distance(position,new Vector2((bounds.width-size.x)/2,-(bounds.y+(bounds.height-size.y)/2)))<.1f,label+" 중앙 배치");
            var view=dream.View;var content=dream.Window.Content.rect.size;Check(Mathf.Abs(view.localScale.x-view.localScale.y)<.0001f&&view.sizeDelta.x*view.localScale.x<=content.x+.1f&&view.sizeDelta.y*view.localScale.y<=content.y+.1f,label+" 4:3 화면 전체 비율 유지·내용 영역 안에 맞춤");
        }
        IEnumerator DreamWindowChecks()
        {
            app.Save.NewGame("Stage04");app.Begin(false);yield return new WaitForSeconds(1.2f);yield return Hold(Key.LeftCtrl,8);var stage=app.ScreenRoot.GetComponentInChildren<Stage04Controller>();stage.Files.Reveal("mydream");
            yield return MouseClick(Icons().Single(i=>i.Id=="s4:mydream").Rect);yield return new WaitForSeconds(.3f);var dream=app.Windows.GetComponentInChildren<DreamWindow>();Check(dream&&dream.Window.Key=="s4:dream","기존 FakeWindow 경로로 프로그램 열기");CheckDreamLayout(dream,"기본 화면");
            Check(Icons().Any(i=>i.Id=="memo")&&Icons().Any(i=>i.Id=="s4:to_document")&&app.CursorLayer.gameObject.activeSelf,"바탕화면 아이콘·마우스 커서 유지");Check(app.Windows.GetSiblingIndex()<app.DragLayer.GetSiblingIndex(),"창 위 드래그 아이콘 기존 레이어 유지");Capture("dream-window-desktop");
            var before=dream.Map.Position;yield return Hold(Key.D,.15f);Check(dream.Map.Position!=before,"창 안 WASD 이동");
            WindowApps.Note("꿈 뒤의 창","다른 창을 여는 검사");yield return new WaitForSeconds(.25f);before=dream.Map.Position;yield return Hold(Key.D,.15f);Check(dream.Map.Position==before,"다른 창 포커스 중 이동 중지");
            stage.Open("mydream");stage.Open("mydream");Check(app.Windows.GetComponentsInChildren<DreamWindow>().Length==1&&dream.Window.transform.GetSiblingIndex()==app.Windows.childCount-1,"여러 번 실행 시 기존 창 앞으로·중복 없음");
            foreach(var size in new[]{new Vector2Int(800,600),new Vector2Int(1440,900),new Vector2Int(900,1000)}){Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSeconds(.4f);CheckDreamLayout(dream,size.ToString());}
            Screen.SetResolution(1024,768,FullScreenMode.Windowed);yield return new WaitForSeconds(.3f);
            var original=app.ScreenRoot.sizeDelta;app.ScreenRoot.sizeDelta=new Vector2(920,680);yield return null;yield return null;CheckDreamLayout(dream,"컨테이너 자체 변경");app.ScreenRoot.sizeDelta=original;yield return null;yield return null;
            var bar=(RectTransform)dream.Window.transform.Find("제목 표시줄");var from=Center(bar);var end=RectTransformUtility.WorldToScreenPoint(app.UICamera,bar.TransformPoint(bar.rect.center)-app.Root.TransformVector(new Vector3(160,0,0)));yield return MouseDrag(from,end);yield return MouseClick(Icons().Single(i=>i.Id=="memo").Rect);yield return null;
            Check(app.Dialogue.Active&&!app.Windows.GetComponentInChildren<DreamWindow>()&&!app.Windows.GetComponentInChildren<PrologueController>(),"실제 NPC 클릭 시 꿈 창·진행 컴포넌트 정리");yield return Hold(Key.LeftCtrl,5);
            stage.Open("mydream");yield return new WaitForSeconds(.3f);dream=app.Windows.GetComponentInChildren<DreamWindow>();Check(dream.Map.Cell==new Vector2Int(1,1),"NPC 대화 후 다시 실행 시 시작점 초기화");
            yield return MouseClick((RectTransform)dream.Window.transform.Find("제목 표시줄/X"));yield return null;Check(!app.Windows.GetComponentInChildren<DreamWindow>()&&!app.Save.Progress.cleared.Contains("Stage04")&&stage.Puzzle2,"X 닫기 시 종료·클리어 오발동 없음·설치 해금 유지");
            stage.Open("mydream");yield return new WaitForSeconds(.3f);dream=app.Windows.GetComponentInChildren<DreamWindow>();Check(dream&&dream.Map.Cell==new Vector2Int(1,1),"닫은 후 재실행 정상");Capture("dream-window-reopened");app.CloseAllWindows();yield return null;
        }
    }
}
#endif
