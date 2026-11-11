#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator SortingToolbarChecks()
        {
            app.Save.NewGame("Stage01");app.Save.Flag("stage01.wandered");app.Begin(false);yield return new WaitForSeconds(.3f);
            yield return Icon("npc");yield return FinishDialogue();Check(app.Stages.State==StageState.SortNormal,"첫 대화 후 일반 분류 시작");
            yield return MouseClick(Icons().Single(i=>i.Id=="sort").Rect);yield return new WaitForSeconds(.25f);
            foreach(var f in app.Stages.Sorting.Files.Where(f=>!f.hidden))yield return Drop(f.id,f.category);
            Check(app.Stages.Sorting.IsCorrect(false),"첫 퍼즐 전체 실제 드래그 완료");
            yield return Icon("npc");yield return FinishDialogue();Check(app.Stages.State==StageState.SortHidden,"검수 대화 완료 후 숨김 퍼즐 진입");
            for(int n=0;n<4;n++)
            {
                yield return MouseClick(Icons().Single(i=>i.Id=="sort").Rect);yield return new WaitForSeconds(.25f);
                var w=app.Windows.GetComponentsInChildren<FakeWindow>().Single(x=>x.Key=="sorting");
                Check(w.HasHiddenToggle&&w.Content.GetComponentsInChildren<FakeIcon>().Length>=3,"재개방 "+n+" 내용과 숨김 툴바 정상");
                var before=w.ShowHidden;yield return MouseClick((RectTransform)w.Toolbar.Find("설정 · 숨김파일 표시"));
                Check(w.ShowHidden!=before,"재개방 "+n+" 숨김 체크 실제 클릭");
                Check(w.Toolbar.GetComponentsInChildren<Button>().Length==1&&app.Windows.GetComponentsInChildren<FakeWindow>().Count(x=>x.Key=="sorting")==1,"토글·창 중복 생성 없음");
                Check(app.Audio.GetComponents<AudioSource>().Any(s=>s.loop&&s.isPlaying),"작업폴더 조작 중 BGM 재생 유지");
                if(n==3){if(!w.ShowHidden)yield return MouseClick((RectTransform)w.Toolbar.Find("설정 · 숨김파일 표시"));Capture("stage01-toolbar-fixed");}
                else {yield return MouseClick((RectTransform)w.transform.Find("제목 표시줄/X"));yield return null;}
            }
            foreach(var f in app.Stages.Sorting.Files.Where(f=>f.hidden))yield return Drop(f.id,f.category);
            Check(app.Stages.Sorting.IsCorrect(true),"숨김 파일 전체 실제 드래그 완료");
            yield return Icon("npc");yield return FinishDialogue();Check(app.Stages.State==StageState.Maze&&Icons().Any(i=>i.Id=="1989"),"두 번째 퍼즐 완료 후 1989년 폴더 해금");
            yield return Icon("sort");yield return new WaitForSeconds(.25f);Check(app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="sorting").HasHiddenToggle,"완료 후 작업폴더 재개방");Capture("stage01-toolbar-complete");
        }
    }
}
#endif
