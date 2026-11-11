using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CoComputar
{
    public sealed class NotepadWindow:MonoBehaviour
    {
        public FakeFileNode File{get;private set;}public TMP_Text Body{get;private set;}
        Stage04Controller stage;Stage06Controller stage06;TMP_InputField editor;FakeWindow window;
        FakeFileSystem Files=>stage06?stage06.Files:stage.Files;public string HelpSpecialRoute=>stage?stage.Data.helpSpecialRoute:"";public string HelpWarningKey=>stage?stage.Data.helpWarningKey:"";
        public static NotepadWindow Open(Stage04Controller stage,FakeFileNode file,bool reload=false)
        {
            bool created=FakeWindow.TryCreate(GameApp.Instance.Windows,"s4:text:"+file.id,file.displayName+" - 메모장",out var w,185,100,660,540);
            if(!created){var existing=w.GetComponent<NotepadWindow>();if(reload)existing.Display();return existing;}
            var self=w.gameObject.AddComponent<NotepadWindow>();self.window=w;self.stage=stage;self.File=file;w.gameObject.AddComponent<FileOwner>().Id=file.id;
            UIFactory.Button(w.Toolbar,stage.Data.openLabel,2,0,95,30,self.OpenDialog);UIFactory.Button(w.Toolbar,stage.Data.saveLabel,99,0,95,30,self.SaveDialog);UIFactory.Button(w.Toolbar,stage.Data.helpLabel,196,0,95,30,self.Help);self.Display();return self;
        }
        public static NotepadWindow Open(Stage06Controller stage,FakeFileNode file,bool reload=false)
        {
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"s6:text:"+file.id,file.displayName+" - 메모장",out var w,185,100,660,540)){var existing=w.GetComponent<NotepadWindow>();if(reload)existing.Display();return existing;}
            var self=w.gameObject.AddComponent<NotepadWindow>();self.stage06=stage;self.File=file;self.window=w;w.gameObject.AddComponent<FileOwner>().Id=file.id;
            UIFactory.Button(w.Toolbar,"열기",2,0,95,30,self.OpenDialog);UIFactory.Button(w.Toolbar,"저장",99,0,95,30,self.SaveDialog);UIFactory.Button(w.Toolbar,"복원",196,0,95,30,()=>{if(self.editor)self.editor.text=stage.Data.initialText;});UIFactory.Button(w.Toolbar,"도움말",293,0,95,30,()=>FileUI.Notice("play_time은 초 단위입니다. 저장한 뒤 게임을 다시 실행하세요.\n초기 내용은 복원 버튼으로 되돌릴 수 있습니다.","s6:help",false));self.Display();return self;
        }
        void Display()
        {
            if(stage06&&File.editable){if(editor)Destroy(editor.gameObject);editor=FileUI.Input(window.Content,File.content,0,0,640,480,true);editor.ActivateInputField();return;}
            if(Body)Destroy(Body.transform.parent.parent.gameObject);Body=FileUI.ScrollText(window.Content,Files.DisplayText(File),0,35,640,445,File.id=="home"?new Color(.08f,.40f,.19f):Color.black);if(stage)stage.ReadText(File);
        }
        void Help()
        {
            var b=UIFactory.Button(window.Content,"정보",196,32,150,34,()=>{FileUI.Notice("Co-computar 메모장\nVersion 0.98\nCopyright © Co-computar Studio.","s4:about",false);});b.onClick.AddListener(()=>Destroy(b.gameObject));
        }
        public void SaveEncoding(string encoding){File.encoding=NotepadEncodings.Normalize(encoding);if(!NotepadEncodings.IsCorrect(File.encoding))File.garbledContent=FakeFileSystem.GarbleFallback(File.id+":"+File.encoding,File.content??"");GameApp.Instance.Audio.Play("sfx_save_ok");FileUI.Notice("저장되었습니다.","s4:saved",false);}
        public void SaveDialog()
        {
            if(stage06){if(editor){stage06.SaveText(File,editor.text);editor.text=File.content;}return;}
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"s4:save:"+File.id,"Save",out var w,250,180,540,365))return;w.gameObject.AddComponent<FileOwner>().Id=File.id;
            UIFactory.Text(w.Content,"Save in:  "+File.parentFolder,10,8,470,30,21);var list=UIFactory.Panel(w.Content,"파일 목록",10,45,490,90,Color.white);UIFactory.Text(list.transform,"Name                     Size          Type\n"+File.displayName+"          —             Text",8,6,470,80,19,Color.black);
            UIFactory.Text(w.Content,"File name:",10,155,130,35,20);var name=FileUI.Input(w.Content,File.displayName,140,150,360,35);name.interactable=false;
            UIFactory.Text(w.Content,"Encoding:",10,200,130,35,20);string chosen=NotepadEncodings.Normalize(File.encoding);GameObject options=null;Button selector=null;
            selector=UIFactory.Button(w.Content,NotepadEncodings.Label(chosen)+" ▼",140,197,225,34,()=>
            {
                if(options){Destroy(options);return;}
                const int rowHeight=32;int height=NotepadEncodings.Options.Length*rowHeight;
                // Expand upward inside the existing content area: all six rows remain visible.
                var panel=UIFactory.Panel(w.Content,"인코딩 선택",140,197-height,225,height,Color.white);options=panel.gameObject;
                for(int i=0;i<NotepadEncodings.Options.Length;i++)
                {
                    var option=NotepadEncodings.Options[i];
                    UIFactory.Button(panel.transform,option.Label,0,i*rowHeight,225,rowHeight,()=>{chosen=option.Id;selector.GetComponentInChildren<TMP_Text>().text=option.Label+" ▼";Destroy(options);});
                }
            });
            UIFactory.Button(w.Content,"Save",260,265,110,35,()=>{SaveEncoding(chosen);w.Close();});UIFactory.Button(w.Content,"Cancel",385,265,115,35,w.Close);
        }
        public void OpenDialog()
        {
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"s4:open","Open",out var w,215,150,600,440))return;w.gameObject.AddComponent<FileOwner>().Id=File.id;
            string path=stage06?"desktop":File.parentFolder;FakeFileNode selected=null;TMP_InputField filename=null;Button open=null;
            void Draw()
            {
                UIFactory.Clear(w.Content);selected=null;UIFactory.Text(w.Content,"Look in:  "+path,10,6,450,32,21);UIFactory.Button(w.Content,"↑",520,4,42,30,()=>{var p=Files.Find(path);path=p?.parentFolder??"desktop";Draw();});
                var panel=UIFactory.Panel(w.Content,"파일 목록",10,48,552,230,Color.white);UIFactory.Text(panel.transform,"Name                         Size       Type",8,3,530,28,20,Color.black);int row=0;
                foreach(var n in Files.Children(path))
                {
                    bool folder=n.contentType=="folder";bool allowed=Files.CanUse(n)&&(folder||FakeFileSystem.Eq(n.extension,"txt"));var node=n;
                    var b=UIFactory.Button(panel.transform,n.displayName+"          —       "+(folder?"Folder":n.extension),4,34+row++*38,542,34,()=>{selected=node;filename.text=node.displayName;open.interactable=!folder;},Color.white);b.interactable=allowed;
                    if(folder)b.gameObject.AddComponent<FileListDoubleClick>().Clicked=()=>{path=node.id;Draw();};
                }
                UIFactory.Text(w.Content,"File name:",10,298,130,35,20);filename=FileUI.Input(w.Content,"",140,295,422,35);filename.readOnly=true;
                open=UIFactory.Button(w.Content,"Open",315,345,110,35,()=>{if(selected==null||!Files.CanUse(selected))return;var n=selected;w.Close();if(stage06?n.contentType=="text":Files.ActionFor(n)=="notepad"){if(stage06)NotepadWindow.Open(stage06,n,true);else NotepadWindow.Open(stage,n,true);}else FileUI.Notice("파일을 열 수 없습니다. 형식이 올바르지 않거나 손상된 파일입니다.");});open.interactable=false;UIFactory.Button(w.Content,"Cancel",440,345,120,35,w.Close);
            }
            Draw();
        }
    }
    public sealed class FileListDoubleClick:MonoBehaviour,IPointerClickHandler
    {
        public System.Action Clicked;public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&e.clickCount==2)Clicked?.Invoke();}
    }
}
