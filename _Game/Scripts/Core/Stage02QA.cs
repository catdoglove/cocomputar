#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator FastDialogue(){yield return Hold(Key.LeftCtrl,app.Dialogue.Active?9:0);Check(!app.Dialogue.Active,"Ctrl 대화 완료");}
        FakeIcon S2Icon(string id)=>Icons().First(i=>i.Id==id);
        Vector2 Center(RectTransform rect)=>RectTransformUtility.WorldToScreenPoint(app.UICamera,rect.TransformPoint(rect.rect.center));
        IEnumerator S2Drag(string id,RectTransform target){yield return MouseDrag(Center(S2Icon(id).Rect),Center(target));}
        IEnumerator Stage02Checks(bool preserveFlow=false)
        {
            Check(DoorAssemblyOrders()==144,"144가지 방향·조립 순서 모두 완성, 자기 자신·소모 조각 거부");
            if(!preserveFlow){app.Save.NewGame("Stage02");app.Save.Flag("stage01.mail");app.Begin(false);}yield return new WaitForSeconds(.3f);
            var stage=app.ScreenRoot.GetComponentInChildren<Stage02Controller>();Check(stage&&stage.Phase==0,"Stage02 데이터 진입");
            yield return Icon("landscapes");Check(app.Windows.GetComponentsInChildren<FakeWindow>().Length==1,"도입 전 폴더 접근 허용");
            yield return Icon("twin_black");yield return new WaitForSeconds(.3f);Capture("stage02-twins");yield return FastDialogue();Check(stage.Phase==1,"도입 종료 해금");
            yield return Icon("landscapes");yield return Icon("landscapes");yield return new WaitForSeconds(.3f);
            Check(app.Windows.GetComponentsInChildren<FakeWindow>().Length==1,"폴더 싱글 인스턴스");
            var collection=app.Windows.GetComponentInChildren<FakeWindow>();yield return MouseClick((RectTransform)collection.Toolbar.Find("설정 · 숨김파일 표시"));yield return null;
            yield return Icon("trash");yield return new WaitForSeconds(.3f);var trash=app.Windows.GetComponentsInChildren<FakeWindow>().First(w=>w.Key=="s2:trash");
            Check(collection.ShowHidden&&!trash.ShowHidden,"숨김 토글 창별 독립");collection.Close();yield return null;yield return Icon("landscapes");yield return new WaitForSeconds(.2f);collection=app.Windows.GetComponentsInChildren<FakeWindow>().First(w=>w.Key=="s2:collection");Check(!collection.ShowHidden,"재열기 숨김 해제");
            yield return Icon("sunset");yield return new WaitForSeconds(.3f);var viewer=app.Windows.GetComponentInChildren<ImageViewer>();
            viewer.SetZoom(-1);Check(viewer.Level==0,"석양 원본 이하 제한");viewer.SetZoom(9);Check(viewer.Level==4,"석양 최대 4단계");viewer.SetZoom(2);
            Check(viewer.PictureRoot.Find("date").gameObject.activeSelf,"확대 날짜 단서 표시");Capture("stage02-sunset");
            viewer.SetZoom(0);yield return MouseClick((RectTransform)viewer.PictureRoot.Find("white-key"));Check(stage.WhiteTaken,"그림 열쇠 실제 클릭 획득");app.CloseAllWindows();yield return null;
            yield return S2Drag("white.key",S2Icon("twin_white").Rect);Check(stage.Phase==2,"흰 열쇠 실제 드래그 전달");yield return FastDialogue();
            stage.ApplyDate(new DateTime(1989,6,6));Check(!stage.Sepia,"필수 퍼즐2 전 선택 이벤트 차단");
            yield return Icon("trash");yield return new WaitForSeconds(.3f);trash=app.Windows.GetComponentInChildren<FakeWindow>();yield return MouseClick((RectTransform)trash.Toolbar.Find("설정 · 숨김파일 표시"));yield return null;
            Check(!stage.Receive("watering","photo-cloud"),"휴지통 물뿌리개 퍼즐 사용 차단");
            yield return MouseDrag(Center(S2Icon("watering").Rect),RectTransformUtility.WorldToScreenPoint(app.UICamera,app.ScreenRoot.TransformPoint(new Vector3(900,-650,0))));
            Check(stage.FileStore.Find("watering").Location==RecycleFiles.Desktop,"물뿌리개 바탕화면 복원");
            stage.OpenOutdoor();yield return new WaitForSeconds(.3f);Check(!stage.Receive("watering","drawn-cloud"),"그림 구름 잘못된 드롭 거부");
            var outdoor=app.Windows.GetComponentInChildren<OutdoorWindow>();DropTarget cloud=null;
            // 복원된 바탕화면 아이콘이 뷰어 뒤에 가리지 않도록 창을 이동한다.
            outdoor.GetComponentInParent<FakeWindow>().Rect.anchoredPosition=new Vector2(20,-60);
            float deadline=Time.time+12;
            while(Time.time<deadline){cloud=outdoor.GetComponentInChildren<DropTarget>();if(cloud.Category=="photo-cloud"&&((RectTransform)cloud.transform).anchoredPosition.x>100&&((RectTransform)cloud.transform).anchoredPosition.x<300)break;yield return null;}
            yield return S2Drag("watering",(RectTransform)cloud.transform);yield return new WaitForSeconds(2.2f);Check(stage.Rained,"물뿌리개 실제 창간 드래그 → 비");Capture("stage02-rain-grown");
            var plants=outdoor.transform.Find("식물");var buds=plants.GetComponentsInChildren<Button>();foreach(var b in buds.ToArray())b.onClick.Invoke();yield return null;
            Check(stage.Buds.All(v=>v),"네 새싹 클릭 성장");var black=(RectTransform)plants.Find("식물 슬롯 "+stage.BlackFlowerIndex+"/검은 열쇠");yield return MouseClick(black);Check(stage.BlackTaken,"꽃 안 검은 열쇠 클릭");app.CloseAllWindows();yield return null;
            yield return S2Drag("black.key",S2Icon("twin_black").Rect);Check(stage.Phase==3,"검은 열쇠 전달");yield return FastDialogue();Check(S2Icon("key.png"),"결말 후 열쇠.png 생성");
            app.ClockClicked();yield return new WaitForSeconds(.3f);Check(app.Windows.GetComponentInChildren<DateTimeWindow>(),"시계 클릭 날짜 창");Capture("stage02-calendar");app.CloseAllWindows();yield return null;
            app.Catalog.crt.Apply(0,0);stage.ApplyDate(new DateTime(1989,6,6));yield return new WaitForSeconds(.2f);Check(stage.Sepia&&app.Catalog.crt.sepiaAmount==1,"필터 0에서 세피아 동작");
            stage.OpenOutdoor();yield return new WaitForSeconds(.3f);outdoor=app.Windows.GetComponentInChildren<OutdoorWindow>();Capture("stage02-tulips");foreach(var tulip in outdoor.transform.Find("식물").GetComponentsInChildren<Button>())tulip.onClick.Invoke();yield return null;
            yield return null;Check(stage.BouquetTaken&&!stage.Sepia&&app.DisplayDate==null,"튤립 네 개 개별 클릭 → 꽃다발·실제 시간 복귀");stage.ApplyDate(new DateTime(1989,6,6));Check(!stage.Sepia&&stage.PlantsGone,"튤립 재발 방지");app.CloseAllWindows();yield return null;
            yield return S2Drag("bouquet",S2Icon("twin_white").Rect);yield return FastDialogue();Check(stage.OptionalSolved,"꽃다발 전달 플래그 저장");
            yield return Icon("key.png");yield return new WaitForSeconds(.3f);viewer=app.Windows.GetComponentInChildren<ImageViewer>();viewer.SetZoom(1);Check(viewer.Level==0,"열쇠 확대 제한");viewer.SetZoom(-99);Check(viewer.Level==-5,"열쇠 5단계 축소 제한");Capture("stage02-morph");yield return MouseClick((RectTransform)viewer.PictureRoot.Find("exit-folder"));Check(stage.ExitTaken,"축소 폴더 실제 클릭");app.CloseAllWindows();yield return null;
            yield return Icon("exit");yield return new WaitForSeconds(.3f);var piece=S2Icon("r");var origin=piece.Rect.anchoredPosition;yield return MouseDrag(Center(piece.Rect),new Vector2(10,10));Check(stage.Assembly.Groups.Count==4&&piece.Rect.anchoredPosition==origin&&piece.transform.IsChildOf(app.Windows),"폴더 밖 드롭: 이동·합성 없이 복귀");yield return S2Drag("r",S2Icon("o2").Rect);Check(stage.Assembly.Label("o2")=="or","r과 o 먼저 합치기 허용");
            yield return S2Drag("d",S2Icon("o1").Rect);yield return S2Drag("o1",S2Icon("o2").Rect);yield return null;
            Check(stage.Assembly.Complete,"do와 or 합치기 → door");yield return Icon("door.exe");yield return new WaitForSeconds(10);
            Check(app.Stages.Current.stageId=="Stage03"&&app.Save.Progress.cleared.Contains("Stage02"),"기존 CMD → Stage03 및 클리어 저장");
            if(preserveFlow)yield break;
            app.Save.Enter("Stage02");app.Begin(false);yield return new WaitForSeconds(.2f);stage=app.ScreenRoot.GetComponentInChildren<Stage02Controller>();
            Check(stage.Phase==0&&!stage.WhiteTaken&&!stage.BlackTaken&&!stage.Rained&&stage.Assembly.Groups.Count==4,"이어하기 내부 상태 초기화");Check(!stage.OptionalSolved&&app.Save.Progress.flags.Contains("stage01.mail"),"현재 선택 퍼즐 초기화·이전 스테이지 기록 유지");Check(!stage.PlantsGone,"이어하기 필수 검은 열쇠 경로 유지");
            app.ApplyOptions();Capture("stage02-desktop");
        }
    }
}
#endif
