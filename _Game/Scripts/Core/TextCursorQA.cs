#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator TextCursorChecks()
        {
            app.Save.NewGame("Stage07");app.Begin(false);yield return new WaitForSeconds(.3f);
            FakeWindow.TryCreate(app.Windows,"qa:text-cursor","입력 테스트",out var w,200,150,600,400);
            var input=FileUI.Input(w.Content,"입력",20,20,450,100,true);yield return new WaitForSeconds(.3f);
            var cursor=app.GetComponent<FakeCursor>();
            InputSystem.QueueStateEvent(mouse,new MouseState{position=Center((RectTransform)input.transform)});yield return null;yield return null;
            Check(cursor.TextCursorActive,"입력 영역 위 I-beam 커서");
            input.readOnly=true;yield return null;yield return null;Check(!cursor.TextCursorActive,"읽기 전용 기본 커서");
            input.readOnly=false;input.interactable=false;yield return null;yield return null;Check(!cursor.TextCursorActive,"비활성 입력 기본 커서");
            input.interactable=true;input.enabled=false;yield return null;yield return null;Check(!cursor.TextCursorActive,"입력 컴포넌트 비활성 기본 커서");
            input.enabled=true;var group=w.gameObject.AddComponent<CanvasGroup>();group.interactable=false;yield return null;yield return null;Check(!cursor.TextCursorActive,"부모 입력 잠금 기본 커서");
            group.interactable=true;yield return null;yield return null;Check(cursor.TextCursorActive,"잠금 해제 후 I-beam 복원");
            var blocker=UIFactory.Panel(app.Modal,"차단",0,0,1024,768,Color.clear);yield return null;yield return null;Check(!cursor.TextCursorActive,"위에 덮인 팝업 입력 차단 시 기본 커서");
            Destroy(blocker.gameObject);yield return null;yield return null;Check(cursor.TextCursorActive,"차단 해제 후 커서 복원");Capture("text-cursor-hover");
            InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(950,700)});yield return null;yield return null;Check(!cursor.TextCursorActive,"입력 영역 이탈 기본 커서");
            InputSystem.QueueStateEvent(mouse,new MouseState{position=Center((RectTransform)input.transform)});yield return null;yield return null;w.Close();yield return null;yield return null;Check(!cursor.TextCursorActive,"입력 창 닫기 커서 잔상 없음");
        }
    }
}
#endif
