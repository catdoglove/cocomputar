#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator RightClickAt(RectTransform rect)
        {
            var point=Center(rect);InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=2});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;
        }
        IEnumerator InputCRTChecks()
        {
            app.Save.NewGame("Stage02");app.Begin(false);yield return new WaitForSeconds(.3f);
            var stage=app.ScreenRoot.GetComponentInChildren<Stage02Controller>();
            yield return MouseClick(S2Icon("landscapes").Rect);yield return new WaitForSeconds(.2f);
            Check(stage.Phase==0&&app.Windows.GetComponentInChildren<FakeWindow>(),"NPC 대화 전 풍경수집 왼쪽 클릭으로 열기");app.CloseAllWindows();yield return null;
            yield return MouseClick(S2Icon("trash").Rect);yield return new WaitForSeconds(.2f);
            Check(stage.Phase==0&&app.Windows.GetComponentInChildren<FakeWindow>(),"NPC 대화 전 휴지통 왼쪽 클릭으로 열기");app.CloseAllWindows();yield return null;
            int clicks=0;System.Action<string> heard=key=>{if(key=="sfx_icon_press")clicks++;};app.Audio.Played+=heard;
            yield return RightClickAt(S2Icon("landscapes").Rect);yield return RightClickAt(S2Icon("twin_black").Rect);
            Check(app.Windows.childCount==0&&!app.Dialogue.Active&&clicks==0,"실제 우클릭: 창·대화·눌림 효과음 없음");
            yield return Icon("landscapes");yield return new WaitForSeconds(.2f);var icon=S2Icon("sunset");
            var origin=icon.Rect.anchoredPosition;var e=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Right,position=Center(icon.Rect),clickCount=2};
            icon.OnPointerDown(e);icon.OnPointerClick(e);icon.OnBeginDrag(e);e.position+=new Vector2(150,100);icon.OnDrag(e);icon.OnEndDrag(e);icon.OnPointerUp(e);
            Check(icon.Rect.anchoredPosition==origin&&!icon.DragVisual&&app.Windows.GetComponentsInChildren<FakeWindow>().Length==1&&clicks==0,"아이콘 우클릭·더블클릭·드래그 직접 호출도 무효");
            app.Audio.Played-=heard;app.CloseAllWindows();yield return null;
            app.Catalog.crt.Apply(.7f,.23f);var material=app.Catalog.crt.material;float bend=material.GetVector("_Shape").y;
            app.Catalog.crt.Apply(0,.23f);Check(material.GetFloat("_Intensity")==0&&material.GetVector("_Shape").y==bend,"화면효과 0: 곡률 독립 유지");
            app.Save.Options.filter=.73f;app.Save.Options.curvature=.26f;app.Save.SaveOptions();var saved=new SaveService().Options;
            Check(saved.filter==.73f&&saved.curvature==.26f,"화면효과·곡률 저장 파일 재로딩 유지");
            UIFactory.Panel(app.ScreenRoot,"스캔라인 검사면",260,230,500,330,Color.white);
            app.Catalog.crt.Apply(0,0);yield return null;Capture("scanlines-off");
            app.Catalog.crt.Apply(1,0);yield return null;Capture("scanlines-on");
            Check(material.GetFloat("_Intensity")==1&&material.GetVector("_Shape").y==0,"화면효과 1·곡률 0 독립 적용");
            app.ApplyOptions();
        }
    }
}
#endif
