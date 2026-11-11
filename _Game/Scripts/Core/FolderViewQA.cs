#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator FolderViewChecks()
        {
            app.Save.NewGame("Stage06");app.Begin(false);yield return new WaitForSeconds(.5f);
            var stage=app.ScreenRoot.GetComponentInChildren<Stage06Controller>();stage.Open("games");yield return new WaitForSeconds(.3f);
            var explorer=app.Windows.GetComponentInChildren<Stage05ExplorerWindow>();var view=explorer.GetComponent<FolderView>();
            Check(view&&view.ViewMode==FolderViewMode.Large&&!explorer.Window.ShowHidden,"기본 크게 보기·숨김 꺼짐");
            Check(FolderView.ViewMenuItems.Select(i=>i.Label).SequenceEqual(new[]{"자세히 보기","크게 보기","숨김파일 표시"}),"메뉴 이름·순서 데이터");
            yield return MouseClick((RectTransform)explorer.Window.Toolbar.Find("보기"));Check(view.MenuOpen,"보기 메뉴 실제 열기");Capture("folder-view-menu");
            yield return MouseClick((RectTransform)app.Modal.GetComponentsInChildren<Button>().Single(b=>b.name=="자세히 보기").transform);yield return null;
            Check(view.ViewMode==FolderViewMode.Details&&!view.MenuOpen,"자세히 선택·메뉴 닫기");
            var icons=explorer.Window.Content.GetComponentsInChildren<FakeIcon>();Check(icons.Length==3&&icons.All(i=>i.IsDetails)&&icons.All(i=>i.Rect.rect.height==34),"기존 아이콘을 목록 행으로 표시");
            var hover=icons[0];var hoverPoint=Center(hover.Rect);
            InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=hoverPoint});yield return null;yield return null;
            Check(hover.RowHovered&&hover.transform.Find("행 호버").GetComponent<RectTransform>().rect.width==hover.Rect.rect.width,"행 빈 공간 호버·이름/유형 전체 너비");Capture("folder-view-hover");
            hover.GetComponent<Image>().color=Color.blue;yield return null;yield return null;Check(!hover.RowHovered&&hover.GetComponent<Image>().color==Color.blue,"선택·드롭 배경색 우선");hover.GetComponent<Image>().color=Color.clear;
            var alpha=hover.gameObject.AddComponent<CanvasGroup>();alpha.alpha=.5f;yield return null;yield return null;Check(hover.RowHovered&&alpha.alpha==.5f,"숨김 행 반투명 유지·호버 표시");alpha.alpha=1;
            var other=icons[1];other.Draggable=true;var start=Center(other.Rect);
            InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=start,buttons=1});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=hoverPoint,buttons=1});yield return null;yield return null;yield return null;
            Check(other.IsDragging&&hover.RowHovered&&!other.RowHovered,"다른 파일 드래그 중 대상 행 호버·원본 제외");
            InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=new Vector2(40,40)});yield return null;yield return null;Check(!hover.RowHovered,"포인터 이탈 호버 해제");
            hover.OnPointerEnter(new UnityEngine.InputSystem.UI.ExtendedPointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerType=UnityEngine.InputSystem.UI.UIPointerType.Touch});yield return null;yield return null;Check(!hover.RowHovered,"터치 호버 잔류 방지");
            Check(explorer.Window.Content.Find("폴더 트리")&&explorer.Window.Content.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="유형"),"트리 유지·오른쪽 유형 헤더");Capture("folder-view-stage06-details");
            icons=explorer.Window.Content.GetComponentsInChildren<FakeIcon>();
            var card=icons.Single(i=>i.Id=="s6:card");yield return MouseClick(card.Rect);yield return null;Check(explorer.CurrentFolderId=="card","목록 폴더 한 번 클릭 같은 창 이동");
            var missing="card_data"+stage.Randoms.CardMissing.ToString("00");explorer.Navigate(stage.Randoms.CardLocation);yield return null;
            var source=explorer.Window.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s6:"+missing);var target=(RectTransform)explorer.Window.Content.Find("폴더 트리/보기 영역/목록/tree:card");
            yield return MouseDrag(Center(source.Rect),Center(target));yield return null;Check(stage.Files.Find(missing).parentFolder=="card","자세히 보기 행에서 트리로 드래그 이동");
            explorer.Navigate("rope_system");yield return null;Check(!explorer.Window.Content.GetComponentsInChildren<FakeIcon>().Any(i=>i.Id=="s6:time"),"목록 숨김 파일 제외");
            yield return MouseClick((RectTransform)explorer.Window.Toolbar.Find("설정 · 숨김파일 표시"));yield return null;
            var time=explorer.Window.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s6:time");Check(time.GetComponent<CanvasGroup>().alpha==.5f&&time.IsDetails,"숨김 파일 목록 반투명");
            var scroll=time.GetComponentInParent<ScrollRect>();scroll.verticalNormalizedPosition=0;yield return null;
            yield return MouseClick(time.Rect);yield return new WaitForSeconds(.3f);Check(app.Windows.GetComponentsInChildren<NotepadWindow>().Any(n=>n.File.id=="time"),"목록 문서 한 번 클릭 기존 뷰어");
            app.Windows.GetComponentsInChildren<NotepadWindow>().Single(n=>n.File.id=="time").GetComponent<FakeWindow>().Close();yield return null;
            view.Select("large");yield return null;Check(!explorer.Window.Content.GetComponentsInChildren<FakeIcon>().Any(i=>i.IsDetails),"크게 보기 복원");
            stage.Open("games");yield return new WaitForSeconds(.2f);var second=app.Windows.GetComponentsInChildren<Stage05ExplorerWindow>().Single(e=>e!=explorer);Check(second.GetComponent<FolderView>().ViewMode==FolderViewMode.Large&&!second.Window.ShowHidden,"새 탐색기 창별 기본 상태");
            app.CloseAllWindows();yield return null;Check(!app.Modal.GetComponentsInChildren<Button>().Any(b=>b.name=="자세히 보기"),"전체 창 닫기 메뉴 정리");
            app.Save.NewGame("Stage05");app.Begin(false);yield return new WaitForSeconds(.5f);var s5=app.ScreenRoot.GetComponentInChildren<Stage05Controller>();
            s5.OpenFolder("memory1");s5.OpenFolder("memory2");yield return new WaitForSeconds(.3f);
            var left=app.Windows.GetComponentsInChildren<Stage05ExplorerWindow>().Single(e=>e.CurrentFolderId=="memory1");var right=app.Windows.GetComponentsInChildren<Stage05ExplorerWindow>().Single(e=>e.CurrentFolderId=="memory2");
            left.Window.Rect.anchoredPosition=new Vector2(5,-70);right.Window.Rect.anchoredPosition=new Vector2(510,-160);left.Window.Resize(new Vector2(490,510));right.Window.Resize(new Vector2(490,510));yield return null;
            left.SetTreeWidth(180);right.SetTreeWidth(180);left.GetComponent<FolderView>().Select("details");right.GetComponent<FolderView>().Select("details");yield return null;
            var apple=left.Window.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s5:apple1");var destination=(RectTransform)right.Window.Content.Find("폴더 내용/보기 영역");
            yield return MouseDrag(Center(apple.Rect),Center(destination));yield return null;
            Check(s5.Files.Find("apple1").parentFolder=="memory2"&&!left.Window.Content.GetComponentsInChildren<FakeIcon>().Any(i=>i.Id=="s5:apple1")&&right.Window.Content.GetComponentsInChildren<FakeIcon>().Any(i=>i.Id=="s5:apple1"&&i.IsDetails),"자세히 보기 창 사이 드래그·양쪽 목록 갱신");Capture("folder-view-stage05-transfer");
            var row=right.Window.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s5:apple1");Check(FileTypeLabels.For(row)=="이미지 파일","확장자 유형 매핑");
            right.GetComponent<FolderView>().ToggleMenu();yield return null;yield return MouseClick((RectTransform)app.Modal.Find("폴더 보기 메뉴"));Check(!right.GetComponent<FolderView>().MenuOpen,"메뉴 바깥 클릭 닫기");
            app.CloseAllWindows();yield return null;
            app.Save.NewGame("Stage01");app.Save.Flag("stage01.wandered");app.Begin(false);yield return new WaitForSeconds(.3f);yield return Icon("npc");yield return FinishDialogue();
            app.Stages.OpenSorting();yield return new WaitForSeconds(.2f);var sorting=app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="sorting");sorting.GetComponent<FolderView>().Select("details");yield return null;
            foreach(var file in app.Stages.Sorting.Files.Where(f=>!f.hidden))yield return Drop(file.id,file.category);
            Check(app.Stages.Sorting.IsCorrect(false),"Stage01 자세히 보기 파일 분류 드래그");app.CloseAllWindows();yield return null;
            app.ScreenRoot.gameObject.AddComponent<MailPuzzle>().Open();yield return new WaitForSeconds(.2f);var mail=app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="mail");mail.ToggleHidden();mail.GetComponent<FolderView>().Select("details");yield return null;
            yield return Drop("5.txt","mail");yield return new WaitForSeconds(3.6f);Check(app.Save.Progress.flags.Contains("stage01.mail")&&mail.Content.GetComponentsInChildren<FakeIcon>().Length==1,"목록 우체통 퍼즐·파일 입자 삭제·목록 정리");Capture("folder-view-mail-complete");app.CloseAllWindows();yield return null;
            app.Save.NewGame("Stage04");app.Begin(false);yield return new WaitForSeconds(1.3f);yield return Hold(Key.LeftCtrl,8);
            var s4=app.ScreenRoot.GetComponentInChildren<Stage04Controller>();s4.OpenFolder("desktop");yield return new WaitForSeconds(.2f);
            var folder=app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="s4:folder:desktop");folder.GetComponent<FolderView>().Select("details");yield return null;
            var doc=folder.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s4:to_document");FileRename.Begin(doc,s4.Files.Find("to_document").displayName,_=>{});yield return null;yield return null;yield return null;
            var field=doc.GetComponentInChildren<TMP_InputField>();Check(field&&field.isFocused&&field.GetComponent<RectTransform>().anchoredPosition.y==0&&field.selectionStringFocusPosition==field.text.Length,"목록 이름 변경 행 내부·끝 커서");yield return Press(Key.Escape);
            s4.Delete("to_document");s4.OpenFolder("trash");yield return new WaitForSeconds(.2f);var trash=app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="s4:folder:trash");trash.GetComponent<FolderView>().Select("details");yield return null;
            var trashed=trash.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s4:to_document");yield return MouseClick(trashed.Rect);yield return MouseClick(trashed.Rect);Check(!app.Windows.GetComponentsInChildren<NotepadWindow>().Any(),"휴지통 목록 문서 실행 금지");Capture("folder-view-trash");
            s4.Files.RestoreTrash(s4.Files.Find("to_document"));yield return null;Check(!trash.Content.GetComponentsInChildren<FakeIcon>().Any(),"복원 후 열린 목록 즉시 갱신");
            app.CloseAllWindows();app.Save.Enter("Stage06");app.Begin(false);yield return new WaitForSeconds(.3f);stage=app.ScreenRoot.GetComponentInChildren<Stage06Controller>();stage.Open("games");yield return new WaitForSeconds(.2f);Check(app.Windows.GetComponentInChildren<FolderView>().ViewMode==FolderViewMode.Large&&!app.Windows.GetComponentInChildren<FakeWindow>().ShowHidden,"이어하기 보기·숨김 초기화");
        }
    }
}
#endif
