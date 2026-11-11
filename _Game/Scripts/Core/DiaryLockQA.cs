#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator DiaryLockChecks()
        {
            app.Save.NewGame("Stage04");app.Begin(false);yield return new WaitForSeconds(1.2f);yield return Hold(Key.LeftCtrl,8);var stage=app.ScreenRoot.GetComponentInChildren<Stage04Controller>();stage.Files.Reveal("important");stage.Open("important");yield return new WaitForSeconds(.3f);
            var home=stage.Files.Find("home");Check(!System.Text.RegularExpressions.Regex.IsMatch(home.content,"cipher|device|socket",System.Text.RegularExpressions.RegexOptions.IgnoreCase)&&System.Text.RegularExpressions.Regex.Replace(home.content,"[^a-z]","")=="money","집 본문 세 단어 제거·money 단서 유지");
            var icon=Icons().Single(i=>i.Id=="s4:home");FileRename.Begin(icon,home.displayName,name=>stage.Rename("home",name));yield return new WaitForSeconds(.1f);var input=icon.GetComponentInChildren<TMP_InputField>();var caret=input.GetComponent<InputCaret>();Check(input.isFocused&&caret.Visible,"이름 변경 자동 포커스·공용 커서 표시");
            Check(input.selectionStringAnchorPosition==input.text.Length&&input.selectionStringFocusPosition==input.text.Length,"이름 변경 시작 전체 이름 끝·선택 없음");
            input.text="집.txt";input.MoveTextEnd(false);yield return null;yield return null;Capture("rename-caret-visible");var before=caret.CaretRect.position;input.ProcessEvent(new Event{type=EventType.KeyDown,character='x',keyCode=KeyCode.X});input.ForceLabelUpdate();yield return null;yield return null;Check(caret.Visible&&caret.CaretRect.position.x>before.x,"문자 추가 시 끝 커서 이동: "+input.text+" "+before.x+" → "+caret.CaretRect.position.x);input.ProcessEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.Backspace});input.ForceLabelUpdate();yield return null;yield return null;Check(input.text=="집.txt","Backspace 정상 입력");yield return new WaitForSeconds(.6f);Check(!caret.Visible,"공통 1초 주기 커서 깜빡임");Capture("rename-caret");
            yield return MouseClick((RectTransform)app.Windows.GetComponentInChildren<FakeWindow>().transform.Find("제목 표시줄"));yield return null;Check(!icon.GetComponentInChildren<FileRename>(),"바깥 클릭 시 편집 취소·커서 제거");
            stage.Rename("diary","일기.dir");stage.Open("diary");yield return new WaitForSeconds(.3f);var diary=app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="s4:diary");input=diary.GetComponentsInChildren<TMP_InputField>()[0];yield return MouseClick((RectTransform)input.transform);input.ProcessEvent(new Event{type=EventType.KeyDown,character='A',keyCode=KeyCode.A});input.ForceLabelUpdate();yield return null;yield return null;caret=input.GetComponent<InputCaret>();Check(caret.Visible&&input.text=="A","일기 실제 입력·커서 표시");
            input.text=string.Join("\n",Enumerable.Range(1,40).Select(i=>"일기 기록 "+i));input.MoveTextEnd(false);yield return null;yield return null;yield return new WaitForSeconds(.1f);Canvas.ForceUpdateCanvases();
            var local=input.textViewport.InverseTransformPoint(caret.CaretRect.position);Check(local.y<=8&&local.y>=-input.textViewport.rect.height,"여러 줄 끝 입력 스크롤·커서 표시 영역 유지");Capture("diary-caret-scroll");input.readOnly=true;yield return null;yield return null;Check(!caret.Visible,"읽기 전용 커서 숨김");input.readOnly=false;
            stage.OpenFolder("trash");yield return new WaitForSeconds(.3f);diary.Focus();yield return S4Click("저장");Check(!caret.Visible||input.readOnly,"저장 이벤트 잠금 입력·커서 차단");yield return new WaitForSeconds(3.2f);
            var node=stage.Files.Find("diary");Check(node.unrestorable&&node.parentFolder=="trash"&&!app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.Key=="s4:diary"),"이벤트 후 창 닫기·일기 휴지통 잠금");
            icon=Icons().Single(i=>i.Id=="s4:diary");Check(!icon.Draggable&&icon.GetComponent<CanvasGroup>().alpha<1,"열린 휴지통 즉시 갱신·드래그 차단·흐림");var p=icon.Rect.anchoredPosition;yield return MouseDrag(Center(icon.Rect),Center(icon.Rect)+new Vector2(180,80));Check(icon.Rect.anchoredPosition==p&&!icon.DragVisual,"잠긴 일기 실제 드래그 시작 불가");
            yield return S4Right(icon);Check(Buttons().Any(b=>b.name=="복원 불가"&&!b.interactable),"잠긴 일기 복원 메뉴 비활성");stage.CloseMenu();yield return S4Click("모두 복원");Check(node.parentFolder=="trash","모두 복원도 잠긴 일기 제외");
            stage.Files.RestoreTrash(node);stage.Files.RestoreOriginal(node);stage.Files.Reveal("diary");Check(!stage.Files.Rename(node,"other.txt",out _)&&!stage.Files.CanUse(node)&&stage.Files.ActionFor(node)==null&&node.parentFolder=="trash","모델 경로 복원·이름 변경·열기·재생성 우회 차단");stage.Open("diary");Check(!app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.Key=="s4:diary"),"잠긴 일기 재실행 차단");
            stage.Delete("home");stage.Files.RestoreTrash(home);Check(home.parentFolder=="important"&&!home.unrestorable,"일반 파일 휴지통 복원 규칙 유지");Check(new SaveService().Progress.flags.Contains("stage04.diaryTrashed"),"일기 이동 상태 디스크 저장·재로드 유지");
            var fresh=new FakeFileSystem(JsonUtility.FromJson<FakeFileData>(Resources.Load<TextAsset>("Stage04Files").text));typeof(Stage04Controller).GetProperty("Files").SetValue(stage,fresh);stage.RestoreDiaryDisposition();Check(fresh.Find("diary").unrestorable&&fresh.Find("diary").parentFolder=="trash","저장 상태 적용 시 휴지통 잠금 복원");Capture("diary-locked-trash");
            app.Save.Flag("stage03.keep");app.Begin(false);yield return new WaitForSeconds(.2f);stage=app.ScreenRoot.GetComponentInChildren<Stage04Controller>();Check(!stage.Files.Find("diary").unrestorable&&stage.Files.Find("diary").parentFolder=="important"&&!new SaveService().Progress.flags.Contains("stage04.diaryTrashed")&&app.Save.Progress.flags.Contains("stage03.keep"),"이어하기 Stage04 잠금·이동 기록 초기화·다른 기록 유지");
        }
    }
}
#endif
