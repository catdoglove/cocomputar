#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator ExplorerChecks()
        {
            app.Save.NewGame("Stage05");app.Begin(false);yield return new WaitForSeconds(.3f);
            var stage=app.ScreenRoot.GetComponentInChildren<Stage05Controller>();
            stage.Open("look");yield return new WaitForSeconds(.3f);var explorer=app.Windows.GetComponentInChildren<Stage05ExplorerWindow>();var window=explorer.Window;
            Check(explorer.CurrentFolderId=="look"&&explorer.Expanded.Contains("look"),"돌아보기 탐색기 시작·루트 펼침");
            var rootTree=window.Content.Find("폴더 트리");
            var rootRow=rootTree.GetComponentsInChildren<Button>().Single(b=>b.name=="tree:desktop");
            var lookRow=rootTree.GetComponentsInChildren<Button>().Single(b=>b.name=="tree:look");
            Check(((RectTransform)lookRow.transform).anchoredPosition.x>((RectTransform)rootRow.transform).anchoredPosition.x,"바탕화면 상위·돌아보기 하위 들여쓰기");
            yield return MouseClick((RectTransform)rootRow.transform);yield return null;
            Check(explorer.CurrentFolderId=="desktop"&&window.Content.GetComponentsInChildren<FakeIcon>().Select(i=>i.Id).SequenceEqual(new[]{"s5:look","s5:soft"}),"바탕화면 클릭 동일 창에 돌아보기·서포트 표시");
            yield return MouseClick((RectTransform)rootTree.GetComponentsInChildren<Button>().Single(b=>b.name=="toggle:desktop").transform);yield return null;
            Check(rootTree.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("tree:"))==1,"바탕화면 접기 하위 트리 숨김");
            yield return MouseClick(window.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s5:look").Rect);yield return null;
            Check(explorer.CurrentFolderId=="look"&&explorer.Expanded.Contains("desktop"),"오른쪽 폴더 열기 바탕화면 자동 펼침");
            Check(window.Content.GetComponentsInChildren<ScrollRect>().Length==2,"트리와 내용 독립 스크롤 2개");
            yield return MouseClick(window.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s5:past").Rect);yield return null;
            Check(explorer.CurrentFolderId=="past"&&app.Windows.GetComponentsInChildren<Stage05ExplorerWindow>().Length==1,"오른쪽 폴더 클릭 같은 창 이동");
            Check(window.transform.Find("제목 표시줄").GetComponentInChildren<TMP_Text>().text=="과거"&&window.GetComponent<FileOwner>().Id=="past","현재 폴더 제목·소유자 갱신");
            var tree=window.Content.Find("폴더 트리");var row=tree.GetComponentsInChildren<Button>().Single(b=>b.name=="tree:past");Check(row.GetComponent<Image>().color.b==.5f&&row.GetComponentInChildren<TMP_Text>().color==Color.white,"현재 선택 진한 파랑·흰 글씨");
            Check(!tree.GetComponentsInChildren<Button>().Any(b=>b.name=="toggle:memory1"),"하위 폴더 없는 항목 토글 없음");
            yield return MouseClick((RectTransform)tree.GetComponentsInChildren<Button>().Single(b=>b.name=="toggle:past").transform);yield return null;
            Check(!explorer.Expanded.Contains("past")&&!tree.GetComponentsInChildren<Button>().Any(b=>b.name=="tree:memory1")&&explorer.CurrentFolderId=="past","마이너스 접기·현재 내용 유지");
            yield return MouseClick(window.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s5:memory1").Rect);yield return null;
            Check(explorer.CurrentFolderId=="memory1"&&explorer.Expanded.Contains("past")&&tree.GetComponentsInChildren<Button>().Any(b=>b.name=="tree:memory1"),"오른쪽 이동 시 부모 자동 펼침");
            Check(!tree.GetComponentsInChildren<Button>().Any(b=>b.name=="tree:apple1"),"트리 파일 제외");
            yield return MouseClick(window.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s5:apple1").Rect);yield return new WaitForSeconds(.2f);var viewer=app.Windows.GetComponentInChildren<ImageViewer>();
            Check(viewer&&explorer.CurrentFolderId=="memory1","파일은 별도 뷰어 유지");viewer.Window.Close();yield return null;
            explorer.Navigate("nowof");yield return null;var scrolls=window.Content.GetComponentsInChildren<ScrollRect>();var right=scrolls.Single(s=>s.name=="폴더 내용");var left=scrolls.Single(s=>s.name=="폴더 트리");
            Check(right.content.rect.height>right.viewport.rect.height,"숫자 PNG 목록 세로 스크롤");var leftPos=left.content.anchoredPosition;right.verticalNormalizedPosition=0;yield return null;Check(left.content.anchoredPosition==leftPos,"오른쪽 스크롤 왼쪽 위치 독립");
            var split=(RectTransform)window.Content.Find("트리 구분선");float before=explorer.TreeWidth;yield return MouseDrag(Center(split),Center(split)+new Vector2(60,0));Check(explorer.TreeWidth>before+20,"구분선 실제 드래그 폭 변경");
            explorer.SetTreeWidth(0);Check(explorer.TreeWidth==Stage05ExplorerWindow.TreeMinWidth,"트리 최소 폭 제한");explorer.SetTreeWidth(10000);Check(window.Content.rect.width-explorer.TreeWidth-Stage05ExplorerWindow.DividerWidth>=Stage05ExplorerWindow.FilesMinWidth,"오른쪽 최소 폭 제한");explorer.SetTreeWidth(255);
            // Many folders exercise the independent tree scroll without changing production layout.
            for(int i=0;i<30;i++)stage.Files.Nodes.Add(new FakeFileNode{id="qaFolder"+i,displayName="검사폴더"+i,parentFolder="look",contentType="folder",iconKey="folder"});stage.Files.Notify();yield return null;
            Check(left.content.rect.height>left.viewport.rect.height,"긴 폴더 트리 독립 세로 스크롤");stage.Files.Nodes.RemoveAll(n=>n.id.StartsWith("qaFolder"));stage.Files.Notify();yield return null;
            right.StopMovement();right.verticalNormalizedPosition=1;yield return null;Capture("stage05-explorer");
            explorer.Navigate("memory1");stage.Open("look");yield return new WaitForSeconds(.3f);var second=app.Windows.GetComponentsInChildren<Stage05ExplorerWindow>().Single(e=>e!=explorer);
            Check(second.CurrentFolderId=="look"&&!second.Expanded.Contains("past")&&explorer.CurrentFolderId=="memory1","바탕화면 재실행 새 창·펼침/현재 폴더 독립");
            second.Navigate("memory2");window.Rect.anchoredPosition=new Vector2(5,-70);second.Window.Rect.anchoredPosition=new Vector2(510,-160);window.Resize(new Vector2(490,510));second.Window.Resize(new Vector2(490,510));yield return new WaitForSeconds(.3f);
            explorer.SetTreeWidth(180);second.SetTreeWidth(180);var apple=window.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s5:apple1");var target=second.Window.Content.Find("폴더 내용/보기 영역").GetComponent<DropTarget>();
            yield return MouseDrag(Center(apple.Rect),Center((RectTransform)target.transform));yield return null;Check(stage.Files.Find("apple1").parentFolder=="memory2","탐색 창 사이 파일 드래그 이동");
            Check(!window.Content.GetComponentsInChildren<FakeIcon>().Any(i=>i.Id=="s5:apple1")&&second.Window.Content.GetComponentsInChildren<FakeIcon>().Any(i=>i.Id=="s5:apple1"),"두 창 목록 즉시 갱신");
            stage.OpenFolder("trash");yield return new WaitForSeconds(.2f);var trash=app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="s5:folder:trash");Check(!trash.GetComponent<Stage05ExplorerWindow>(),"휴지통 기존 창 유지");trash.Close();yield return null;
            app.OpenComputer();yield return new WaitForSeconds(.2f);Check(app.Windows.GetComponentsInChildren<Stage05ExplorerWindow>().Length==2,"내 컴퓨터 탐색기로 교체 안 함");
            app.CloseAllWindows();yield return null;stage.Open("look");yield return new WaitForSeconds(.3f);explorer=app.Windows.GetComponentInChildren<Stage05ExplorerWindow>();explorer.Navigate("memory1");
            var hidden=new FakeFileNode{id="hiddenExplorerQA",displayName="hidden.png",parentFolder="memory1",contentType="image",iconKey="picture",imageKey="s5_photo",hidden=true};stage.Files.Nodes.Add(hidden);stage.Files.Notify();yield return null;
            Check(!explorer.Window.Content.GetComponentsInChildren<FakeIcon>().Any(i=>i.Id=="s5:hiddenExplorerQA"),"탐색기 숨김 파일 기본 제외");
            yield return MouseClick((RectTransform)explorer.Window.Toolbar.Find("설정 · 숨김파일 표시"));yield return null;Check(explorer.Window.Content.GetComponentsInChildren<FakeIcon>().Any(i=>i.Id=="s5:hiddenExplorerQA")&&stage.IsFileVisible(hidden),"기존 숨김 표시·파일 열기 판정 연결");
            explorer.Navigate("memory2");Check(!explorer.Window.ShowHidden,"폴더 이동 시 숨김 옵션 대상 변경");explorer.Navigate("memory1");Check(explorer.Window.ShowHidden,"돌아오면 해당 폴더 숨김 설정 유지");
            stage.Files.Nodes.Remove(hidden);stage.Files.Notify();app.CloseAllWindows();yield return null;stage.Open("soft");yield return new WaitForSeconds(.3f);explorer=app.Windows.GetComponentInChildren<Stage05ExplorerWindow>();Check(explorer.CurrentFolderId=="soft"&&!explorer.Expanded.Contains("past"),"재열기 창 상태 초기화·서포트 지원");
            var photo=stage.Files.Find("photo");explorer.Navigate(photo.parentFolder);yield return null;yield return MouseClick(explorer.Window.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s5:photo").Rect);yield return new WaitForSeconds(.2f);Check(app.Windows.GetComponentInChildren<PhotoCorrectionRenderer>(),"프로그램 파일 별도 창 실행");
            stage.Talk();yield return null;Check(app.Dialogue.Active&&app.Windows.GetComponentsInChildren<FakeWindow>().Length==0,"NPC 대화 시작 모든 탐색기·프로그램 창 정상 닫기");
            yield return Hold(UnityEngine.InputSystem.Key.LeftCtrl,8);stage.Open("look");yield return new WaitForSeconds(.3f);Check(app.Windows.GetComponentInChildren<Stage05ExplorerWindow>(),"대화 종료 후 다시 탐색 가능");
        }
    }
}
#endif
