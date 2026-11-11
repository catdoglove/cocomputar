#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator TrashChecks()
        {
            app.Save.NewGame("Stage02");app.Begin(false);yield return new WaitForSeconds(.3f);var stage=app.ScreenRoot.GetComponentInChildren<Stage02Controller>();
            yield return Icon("twin_black");float began=Time.realtimeSinceStartup;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftCtrl));
            while(app.Dialogue.Active&&Time.realtimeSinceStartup-began<4)yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            Check(!app.Dialogue.Active&&Time.realtimeSinceStartup-began<2.5f,"Ctrl 33줄 도입 대화 2.5초 이내 완료");
            yield return Icon("trash");yield return new WaitForSeconds(.3f);
            var trash=app.Windows.GetComponentInChildren<FakeWindow>();trash.Content.GetComponentInChildren<Button>().onClick.Invoke();yield return null;
            yield return MouseClick(S2Icon("watering").Rect);S2Icon("watering").OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){clickCount=2});yield return null;
            Check(!app.Windows.GetComponentInChildren<ImageViewer>(),"휴지통 클릭·더블클릭 열기 차단");Check(S2Icon("watering").GetComponent<CanvasGroup>().alpha<.5f,"휴지통 숨김 파일 흐린 표시");
            int executions=0;
            foreach(string extension in new[]{"txt","jpg","mp3","exe"})
            {
                string id="test."+extension;stage.FileStore.Add(new RecycleFile{Id=id,Name=id,Sprite="document",Location=RecycleFiles.Trash,Open=()=>executions++});yield return null;
                yield return Icon(id);S2Icon(id).OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){clickCount=2});Check(executions==0,extension+" 휴지통 실행 차단");
                stage.FileStore.Move(id,RecycleFiles.Desktop);stage.FileStore.Open(id);Check(executions==1,extension+" 복원 후 기존 실행 복구");executions=0;stage.FileStore.Remove(id);yield return null;
            }
            var position=RectTransformUtility.WorldToScreenPoint(app.UICamera,app.ScreenRoot.TransformPoint(new Vector3(900,-650,0)));
            yield return MouseDrag(Center(S2Icon("watering").Rect),position);Check(stage.FileStore.CanUse("watering"),"실제 드래그로 휴지통에서 바탕화면 꺼내기");
            yield return Icon("watering");yield return new WaitForSeconds(.3f);Check(app.Windows.GetComponentInChildren<ImageViewer>(),"복원 물뿌리개 뷰어 열기");
            WindowApps.Note("무관한 문서","유지");yield return new WaitForSeconds(.2f);
            // 파일 아이콘을 가리지 않도록 창을 옮긴 뒤 실제 마우스로 휴지통에 넣는다.
            foreach(var w in app.Windows.GetComponentsInChildren<FakeWindow>())if(w.Key!=RecycleFiles.Trash)w.Rect.anchoredPosition=new Vector2(350,-420);
            trash.Close();yield return null;
            yield return S2Drag("watering",S2Icon("trash").Rect);yield return null;
            Check(!app.Windows.GetComponentInChildren<ImageViewer>()&&app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.Key=="note:무관한 문서"),"휴지통 이동 시 해당 창만 닫기");
            app.CloseAllWindows();yield return null;
            yield return Icon("trash");yield return new WaitForSeconds(.2f);trash=app.Windows.GetComponentInChildren<FakeWindow>();trash.Content.GetComponentInChildren<Button>().onClick.Invoke();yield return null;
            yield return S2Drag("watering",S2Icon("landscapes").Rect);Check(stage.FileStore.Find("watering").Location==RecycleFiles.Collection,"폴더 아이콘으로 복원");
            app.CloseAllWindows();yield return null;yield return Icon("landscapes");yield return new WaitForSeconds(.2f);var folder=app.Windows.GetComponentInChildren<FakeWindow>();folder.Content.GetComponentInChildren<Button>().onClick.Invoke();yield return null;
            yield return Icon("watering");yield return new WaitForSeconds(.2f);Check(app.Windows.GetComponentInChildren<ImageViewer>(),"복원 폴더 안 파일 열기");app.CloseAllWindows();yield return null;
            Check(app.Catalog.audio.Get("sfx_key_get")!=app.Catalog.audio.Get("sfx_ding"),"열쇠 전용 음원 별도 등록");
            int sounds=0;Action<string> heard=key=>{if(key=="sfx_key_get")sounds++;};app.Audio.Played+=heard;
            yield return Icon("landscapes");yield return new WaitForSeconds(.2f);yield return Icon("sunset");yield return new WaitForSeconds(.2f);var viewer=app.Windows.GetComponentInChildren<ImageViewer>();viewer.HotspotClicked("white-key");viewer.HotspotClicked("white-key");
            stage.Rained=true;stage.Buds[stage.BlackFlowerIndex]=true;stage.TakeBlack();stage.TakeBlack();Check(sounds==2,"흰·검은 열쇠 획득당 전용음 1회");app.Audio.Played-=heard;
            float saved=app.Save.Options.effects;app.Save.Options.effects=0;app.ApplyOptions();app.Catalog.mixer.GetFloat("EffectsVolume",out float muted);app.Save.Options.effects=.5f;app.ApplyOptions();app.Catalog.mixer.GetFloat("EffectsVolume",out float half);
            Check(muted<=-79&&Mathf.Abs(half+6.0206f)<.1f,"효과음 음소거·50% 믹서 볼륨 적용");app.Save.Options.effects=saved;app.ApplyOptions();
        }
    }
}
#endif
