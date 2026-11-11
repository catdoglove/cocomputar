using UnityEngine;
namespace CoComputar
{
    public static class WindowApps
    {
        public static void Note(string name,string content,string documentId=null)
        {
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"note:"+(documentId ?? name),name+" - 메모장",out var w,220,160,565,360)) return;
            UIFactory.Panel(w.Content,"종이",0,0,545,305,UIFactory.Theme.paper);
            UIFactory.Text(w.Content,content,15,20,515,265,23);
        }
        public static void Picture(string name,string imageKey)
        {
            ImageViewer.Open(new ViewerDefinition{id=name,title=name,imageKey=imageKey});
        }
        public static void Alert(string message,System.Action confirmed=null)
        {
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"alert:"+message,"알림",out var w,300,255,425,215)) return;
            UIFactory.Text(w.Content,message,20,15,365,85,22);
            UIFactory.Button(w.Content,"확인",145,105,105,36,()=>{w.Close();confirmed?.Invoke();});
            GameApp.Instance.Audio.Play("sfx_error");
        }
    }
}
