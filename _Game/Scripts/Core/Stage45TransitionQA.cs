#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using UnityEngine;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator Stage45TransitionChecks()
        {
            app.Save.NewGame("Stage04");app.Save.Progress.stage05=new SaveService().Progress.stage05;Debug.Log("RELOADED STAGE05 "+JsonUtility.ToJson(app.Save.Progress.stage05));app.Begin(false);yield return new WaitForSeconds(1.3f);yield return FinishDialogue();
            var previous=app.ScreenRoot.GetComponentInChildren<Stage04Controller>();previous.Files.Reveal("mydream");previous.Open("mydream");yield return new WaitForSeconds(.3f);
            yield return S4WalkDream(app.Windows.GetComponentInChildren<PrologueController>());
            float deadline=Time.realtimeSinceStartup+25;
            while(app.Stages.Current.stageId!="Stage05"&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return new WaitForSeconds(.5f);
            var stage=app.ScreenRoot.GetComponentInChildren<Stage05Controller>();Check(stage,"4 → 5 실제 로딩 전환 완료");
            foreach(var id in new[]{"look","soft"})
            {
                var file=stage.Files.Find(id);Debug.Log("ENTRY FILE "+id+" "+JsonUtility.ToJson(file));
                Check(file!=null&&file.available&&file.parentFolder=="desktop"&&!file.hidden,"전환 직후 기본 폴더 데이터 "+id);
                var icon=Icons().SingleOrDefault(i=>i.Id=="s5:"+id);Check(icon&&icon.gameObject.activeInHierarchy,"재시작 없이 바탕화면 폴더 표시 "+id);
                yield return MouseClick(icon.Rect);yield return new WaitForSeconds(.25f);
                Check(app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.GetComponent<FileOwner>()?.Id==id),"전환 직후 폴더 클릭 열기 "+id);
                app.CloseAllWindows();yield return null;
            }
            Capture("stage45-transition-folders");
            var disk=new SaveService().Progress.stage05;
            Check(disk.files.Any(n=>n.id=="look")&&disk.files.Any(n=>n.id=="soft"),"전환 후 정상 파일 목록 디스크 저장");
            var target=stage.Randoms.PhotoTarget;stage.DeleteFile("look");disk=new SaveService().Progress.stage05;
            app.Save.Progress.stage05=disk;app.Stages.Enter("Stage05",app.ScreenRoot,app.GetComponent<FakeCursor>());yield return new WaitForSeconds(.3f);
            stage=app.ScreenRoot.GetComponentInChildren<Stage05Controller>();
            Check(stage.Files.Find("look").parentFolder=="trash"&&stage.Randoms.PhotoTarget==target,"정상 저장의 폴더 이동과 랜덤 정답 보존");
            stage.OpenFolder("trash");yield return new WaitForSeconds(.2f);Check(Icons().Any(i=>i.Id=="s5:look"),"정상 저장 복원 후 휴지통 폴더 접근");
            app.Begin(false);yield return new WaitForSeconds(.3f);Check(Icons().Any(i=>i.Id=="s5:look")&&Icons().Any(i=>i.Id=="s5:soft"),"이어하기 재시작 초기화 규칙 유지");
        }
    }
}
#endif
