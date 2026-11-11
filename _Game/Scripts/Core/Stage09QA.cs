#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CoComputar
{
 public sealed partial class PlaythroughQA
 {
  IEnumerator Stage09Checks()
  {
   app.Save.NewGame("Stage09");app.Begin(false);yield return new WaitForSeconds(.4f);
   var s=app.ScreenRoot.GetComponentInChildren<Stage09Controller>();Check(s!=null,"실제 Stage09 진입");
   app.Dialogue.debugSkipOverride=DialogueSystem.SkipOverride.ForceOn;
   Check(new[]{"A","B","C"}.All(s.IsLocked),"세 폴더 잠금 초기값");
   Check(!s.Deliver("record3"),"D1 이전 전달 거부");
   s.Open("A");Check(!app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.name=="A폴더"),"잠긴 폴더 차단");app.CloseAllWindows();yield return new WaitForSeconds(.3f);
   Check(s.Extract()&&!s.Extract()&&s.Files.Find("zip")==null&&s.Files.Find("keys").available,"압축 1회 해제·같은 위치 폴더 전환");
   s.Talk();Check(app.Dialogue.CurrentSequence=="S9_D1","압축을 먼저 해도 D1 우선");yield return Hold(Key.LeftCtrl,9);Check(s.Progress.d1&&!app.Dialogue.Active,"D1 완료");
   s.Talk();Check(app.Dialogue.CurrentSequence=="S9_D2","압축 후에도 D2 우선");yield return Hold(Key.LeftCtrl,6);Check(s.Progress.d2,"D2 완료");
   s.Talk();Check(app.Dialogue.CurrentSequence=="S9_D3","D2 다음 D3");yield return Hold(Key.LeftCtrl,6);Check(s.Progress.d3,"D3 완료");
   Check(!s.Unlock("keyA","B")&&s.IsLocked("B"),"잘못된 열쇠 거부");app.CloseAllWindows();yield return new WaitForSeconds(.3f);
   s.Open("keys");yield return new WaitForSeconds(.3f);Canvas.ForceUpdateCanvases();
   var keyIcon=Icons().First(i=>i.Id=="s9:keyA");var folderIcon=Icons().First(i=>i.Id=="s9:A");
   yield return MouseDrag(RectTransformUtility.WorldToScreenPoint(app.UICamera,keyIcon.Rect.TransformPoint(keyIcon.Rect.rect.center)),RectTransformUtility.WorldToScreenPoint(app.UICamera,folderIcon.Rect.TransformPoint(folderIcon.Rect.rect.center)));
   Check(!s.IsLocked("A"),"실제 마우스 드래그로 폴더 해금");app.CloseAllWindows();yield return new WaitForSeconds(.3f);
   foreach(var id in new[]{"B","C"})Check(s.Unlock("key"+id,id)&&s.Files.Find("key"+id).available,"잠금 해제·열쇠 재사용 "+id);
   s.Open("A");s.Open("B");yield return new WaitForSeconds(.4f);var folders=app.Windows.GetComponentsInChildren<FakeWindow>();var a=folders.First(w=>w.Key=="s9:folder:A");var b=folders.First(w=>w.Key=="s9:folder:B");a.ToggleHidden();Check(a.ShowHidden&&!b.ShowHidden,"숨김 표시 창별 독립");Capture("stage09-folders");
   Check(s.Files.Find("record3").hidden&&!s.Files.Find("record3").IsVisible(false)&&s.Files.Find("record3").IsVisible(true),"정답 기존 숨김 판정");
   Check(NotepadEncodings.Options.Select(o=>Stage09Logic.Garble("record3",o.Id)).Distinct().Count()==6,"여섯 인코딩 모두 서로 다른 깨진 내용");
   Check(!s.Rename("record3","기록_01.txt"),"이름 중복 거부");app.CloseAllWindows();yield return new WaitForSeconds(.3f);
   Check(s.Rename("record1","기록_01.png"),"오답 파일 확장자 변경 가능");s.Open("record1");Check(!app.Windows.GetComponentInChildren<ImageViewer>(),"오답 이미지 손상 경고");app.CloseAllWindows();yield return new WaitForSeconds(.3f);
   Check(s.Rename("record1","기록_01.txt"),"오답 변경 복구");Check(s.Rename("record3","기록_03.png"),"정답 PNG 변경");
   s.Open("record3");yield return new WaitForSeconds(.3f);Check(app.Windows.GetComponentInChildren<ImageViewer>().Picture.preserveAspect,"가족사진 종횡비 유지");Capture("stage09-family");
   Check(s.Deliver("record3"),"사진 수락");Check(!s.Deliver("record3"),"전달 중 중복 거부");yield return new WaitForSeconds(.7f);
   Check(s.Progress.photoDelivered&&!s.Files.Find("record3").available&&!app.Dialogue.Active,"사진 소모 후 자동 대화 없음");
   Check(!app.Windows.GetComponentInChildren<ImageViewer>(),"전달한 사진 뷰어 닫힘");
   s.Talk();Check(app.Dialogue.CurrentSequence=="S9_FINAL","클릭 후 마지막 대화");yield return Hold(Key.LeftCtrl,12);
   Check(s.WaitingForHead&&app.Dialogue.Active&&app.Dialogue.Paused,"Ctrl 스킵에도 가져가렴 정지");Capture("stage09-head-wait");
   yield return Press(Key.Space);Check(s.WaitingForHead&&!s.Progress.keyObtained,"스페이스로 열쇠 획득 불가");
   yield return MouseClick((RectTransform)app.Modal.Find("열쇠 머리 클릭"));s.TakeHead();yield return new WaitForSeconds(1.2f);Check(s.Progress.keyObtained&&s.Progress.npcGone&&!app.Dialogue.Active&&!app.Modal.Find("대화"),"머리 1회 획득·NPC 및 대화 정리");
   s.Open("home");yield return new WaitForSeconds(.4f);Check(!s.Escape("finalKey"),"기본 배율 탈출 차단");s.HomeViewer.SetZoom(s.Rules.revealZoom);yield return new WaitForSeconds(.4f);Capture("stage09-home");
   Check(!s.Escape("keyC"),"폴더용 열쇠로 탈출 불가");
   var hint=s.Rules.hints.First(h=>h.answer);Check(s.HintText(hint).StartsWith("기록_03.txt"),"변경 후 힌트 원본 이름 유지");
   app.Save.SaveProgress();app.Stages.Enter("Stage09",app.ScreenRoot,UnityEngine.Object.FindFirstObjectByType<FakeCursor>());yield return new WaitForSeconds(.3f);s=app.ScreenRoot.GetComponentInChildren<Stage09Controller>();Check(s.Progress.npcGone&&s.Progress.keyObtained&&!s.Files.Find("record3").available,"체크포인트 상태 복원");Check(s.HintText(hint).StartsWith("기록_03.txt"),"불러온 뒤 힌트 원본 유지");
   s.Open("home");s.HomeViewer.SetZoom(s.Rules.revealZoom);yield return new WaitForSeconds(.3f);
   var home=s.HomeViewer;var door=home.GetComponent<Stage09Image>().Door;Canvas.ForceUpdateCanvases();
   // 확대 뒤 실제 패닝으로 현관문을 뷰포트 가운데에 놓는다.
   var viewport=(RectTransform)home.transform;var center=RectTransformUtility.WorldToScreenPoint(app.UICamera,viewport.TransformPoint(viewport.rect.center));
   var doorScreen=RectTransformUtility.WorldToScreenPoint(app.UICamera,door.TransformPoint(door.rect.center));
   yield return MouseDrag(center,center+center-doorScreen);yield return null;Canvas.ForceUpdateCanvases();Capture("stage09-door-panned");
   var finalIcon=Icons().First(i=>i.Id=="s9:finalKey");doorScreen=RectTransformUtility.WorldToScreenPoint(app.UICamera,door.TransformPoint(door.rect.center));
   yield return MouseDrag(RectTransformUtility.WorldToScreenPoint(app.UICamera,finalIcon.Rect.TransformPoint(finalIcon.Rect.rect.center)),doorScreen);
   Check(s.Busy,"확대·패닝 후 실제 열쇠 드롭으로 문 진입");yield return new WaitForSeconds(12);Check(app.Stages.Current.stageId=="Stage10","CMD 후 Stage10 자리 이동");
   foreach(var order in new[]{"ABC","CAB","BCA","CBA"}){app.Save.ResetStagePuzzles("Stage09");app.Save.Enter("Stage09");app.Begin(false);yield return new WaitForSeconds(.2f);s=app.ScreenRoot.GetComponentInChildren<Stage09Controller>();s.Extract();foreach(char id in order)Check(s.Unlock("key"+id,id.ToString()),"순서 자유 "+order+" / "+id);Check(Stage09Logic.Dialogue(s.Progress)=="S9_D1","탐색 먼저 해도 첫 대화 유지 "+order);}
   app.Save.ResetStagePuzzles("Stage09");app.Save.Enter("Stage09");app.Begin(false);yield return new WaitForSeconds(.3f);s=app.ScreenRoot.GetComponentInChildren<Stage09Controller>();Check(!s.Progress.d1&&!s.Progress.keyObtained&&!s.Progress.unpacked&&s.Files.Find("zip").available&&s.Files.Find("record3").extension=="txt", "이어하기 초기화");
  }
 }
}
#endif

