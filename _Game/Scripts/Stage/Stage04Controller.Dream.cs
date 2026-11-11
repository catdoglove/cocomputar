using System.Collections;
using UnityEngine;
namespace CoComputar
{
    public sealed partial class Stage04Controller
    {
        void OpenDiary(FakeFileNode n)
        {
            if(!FakeWindow.TryCreate(app.Windows,"s4:diary",n.displayName,out var w,160,95,700,575))return;w.gameObject.AddComponent<FileOwner>().Id=n.id;app.Save.Flag("stage04.diaryOpened");
            UIFactory.Sprite(w.Content,app.Catalog.sprites.Get("s4_diary"),0,0,680,470);
            var left=FileUI.Input(w.Content,"",65,55,245,355,true);var right=FileUI.Input(w.Content,"",355,55,245,355,true);
            var paper=new Color(.91f,.77f,.55f);left.GetComponent<UnityEngine.UI.Image>().color=paper;right.GetComponent<UnityEngine.UI.Image>().color=paper;
            UIFactory.Button(w.Content,"저장",545,478,120,35,()=>{if(!busy)StartCoroutine(DiaryWarning(w));});
        }
        IEnumerator DiaryWarning(FakeWindow diary)
        {
            busy=true;foreach(var input in diary.GetComponentsInChildren<TMPro.TMP_InputField>()){input.readOnly=true;input.DeactivateInputField();}app.Audio.PauseEventMusic();var blocker=UIFactory.Panel(app.Modal,"일기 입력 잠금",0,0,1024,768,new Color(0,0,0,.12f));
            FakeWindow.TryCreate(blocker.transform,"s4:diary:warning","경고",out var warning,280,280,470,170);UIFactory.Text(warning.Content,"이건 너의 일기장이 아니야",35,28,400,65,25,Color.black);
            // 경고창의 X를 포함해 연출 중 입력을 막는다.
            UIFactory.Panel(blocker.transform,"입력 차단",0,0,1024,768,Color.clear);app.Audio.Play("sfx_error");yield return new WaitForSecondsRealtime(3);
            if(warning)warning.Close();CloseRelated("diary");app.Save.Flag("stage04.diaryTrashed");Files.LockInTrash(Files.Find("diary"));Destroy(blocker.gameObject);app.Audio.ResumeEventMusic();busy=false;
        }
        void EnterDream()
        {
            DreamWindow.Open(app.ScreenRoot,Data.dreamWindowRatio,()=>
            {
                busy=true;app.CloseAllWindows();UIFactory.Panel(app.Modal,"꿈의 암전",0,0,1024,768,Color.black);manager.RequestClear("door.exe");
            });
        }
        void OnDestroy(){if(Files!=null)Files.Changed-=Refresh;if(app){app.CursorLayer.gameObject.SetActive(true);app.Audio.ResumeEventMusic();}CloseMenu();}
    }
}
