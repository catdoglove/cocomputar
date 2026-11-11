using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace CoComputar
{
    public sealed partial class Stage04Controller:MonoBehaviour,IStageEntry
    {
        GameApp app;StageManager manager;RectTransform icons;FilePopup menu;bool busy;
        public FakeFileData Data{get;private set;}public FakeFileSystem Files{get;private set;}public string Password{get;private set;}
        public bool RestoreUnlocked{get;private set;}public bool IntroDone{get;private set;}public bool Puzzle1{get;private set;}public bool Puzzle2=>Files.Find("mydream").available;
        public bool Ready=>IntroDone&&!busy&&!app.Dialogue.Active;
        readonly Dictionary<string,FakeWindow> folders=new();
        public void Enter(StageManager mgr,StageDefinition definition)
        {
            app=GameApp.Instance;app.SetFileRightClick(true);manager=mgr;Data=JsonUtility.FromJson<FakeFileData>(Resources.Load<TextAsset>("Stage04Files").text);Files=new FakeFileSystem(Data);RestoreDiaryDisposition();Password=UnityEngine.Random.Range(0,10000).ToString("D4");Files.Find("to_document").content=Files.Find("to_document").content.Replace("{PASSWORD}",Password);
            app.Dialogue.Load(definition.dialogue);app.Dialogue.FlagCommand=flag=>{if(flag=="restoreUnlocked")RestoreUnlocked=true;};app.Audio.Music("bgm_stage04");
            icons=UIFactory.Rect(app.ScreenRoot,"Stage04 파일",0,0,1024,768);Files.Changed+=Refresh;Refresh();StartCoroutine(Intro());
        }
        IEnumerator Intro(){yield return new WaitForSeconds(1);var npc=icons.GetComponentsInChildren<FakeIcon>().First(i=>i.Id=="memo");npc.gameObject.AddComponent<Stage03NpcPop>();StartCoroutine(Startle());app.Dialogue.Play(()=>IntroDone=true,"S4_INTRO");}
        IEnumerator Startle(){var root=app.ScreenRoot;var p=root.anchoredPosition;for(float t=0;t<.25f;t+=Time.unscaledDeltaTime){root.anchoredPosition=p+Vector2.right*Mathf.Sin(t*120)*4;yield return null;}root.anchoredPosition=p;}
        public void Talk(){if(Ready){CloseMenu();app.Dialogue.Play(()=>{},Puzzle2?"S4_LOOP_3":Puzzle1?"S4_LOOP_2":"S4_LOOP_1");}}
        void Refresh()
        {
            UIFactory.Clear(icons);FakeIcon.Create(icons,"memo","메모","icon_memo",780,190,Talk);
            var trash=FakeIcon.Create(icons,"s4:trash","휴지통","trash",865,615,()=>{if(Ready)OpenFolder(FakeFileSystem.Trash);});trash.gameObject.AddComponent<DropTarget>().Category=FakeFileSystem.Trash;
            foreach(var n in Files.Children(FakeFileSystem.Desktop))AddIcon(icons,n,n.x,n.y);
            foreach(var pair in folders.ToArray()){if(!pair.Value){folders.Remove(pair.Key);continue;}DrawFolder(pair.Value,pair.Key);}
        }
        void AddIcon(Transform parent,FakeFileNode n,float x,float y)
        {
            var icon=FakeIcon.Create(parent,"s4:"+n.id,n.displayName,n.iconKey,x,y,()=>Open(n.id),!n.unrestorable,n.unrestorable||n.hidden);if(n.unrestorable)UIFactory.Text(icon.transform,"복원 불가",0,88,92,20,12,Color.gray);icon.AllowWindowDrops=true;icon.ScreenBoundDrag=true;icon.gameObject.AddComponent<FileRightClick>().Click=e=>Context(n,icon,e);
            icon.Dropped=(i,t)=>{if(Ready&&t.Category==FakeFileSystem.Trash)Delete(n.id);};
        }
        public void OpenFolder(string id)
        {
            if(!FakeWindow.TryCreate(app.Windows,"s4:folder:"+id,id=="desktop"?"내 컴퓨터":id=="trash"?"휴지통":Files.Find(id).displayName,out var w))return;
            w.SetFileBackground(id=="trash"?42:0);w.gameObject.AddComponent<FileOwner>().Id=id;folders[id]=w;w.Closed+=()=>folders.Remove(id);w.AddHiddenToggle(()=>DrawFolder(w,id));if(id=="trash")w.AddToolbarRightButton("모두 복원",155,32,4,()=>{if(Ready){foreach(var n in Files.Children("trash").ToArray())Files.RestoreTrash(n);app.Audio.Play("sfx_restore");}});DrawFolder(w,id);
        }
        void DrawFolder(FakeWindow w,string id)
        {
            UIFactory.Clear(w.Content);
            var area=UIFactory.Panel(w.Content,"파일 목록",0,id=="trash"?44:0,w.Content.rect.width,w.Content.rect.height-(id=="trash"?44:0),Color.white);if(id=="trash"){var drop=area.gameObject.AddComponent<DropTarget>();drop.Category=id;drop.IsFolderWindow=true;}
            int i=0;foreach(var n in Files.Children(id).Where(n=>n.IsVisible(w.ShowHidden))){AddIcon(area.transform,n,20+i%4*145,12+i/4*112);i++;}
        }
        public void CloseMenu(){if(menu)menu.Close();menu=null;}
        void Context(FakeFileNode n,FakeIcon icon,PointerEventData e)
        {
            if(!Ready)return;CloseMenu();app.Audio.Play("sfx_menu_open");var block=UIFactory.Panel(app.Modal,"파일 메뉴",0,0,1024,768,Color.clear);menu=block.gameObject.AddComponent<FilePopup>();block.gameObject.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(CloseMenu);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(app.Modal,e.position,e.pressEventCamera,out var pos);var panel=UIFactory.Panel(block.transform,"메뉴",Mathf.Clamp(pos.x,0,815),Mathf.Clamp(-pos.y,35,565),205,n.parentFolder=="trash"||n.extension=="zip"?40:160,new Color(.76f,.76f,.78f));int row=0;
            void Item(string label,Action action,bool enabled=true){var b=UIFactory.Button(panel.transform,label,3,3+row++*38,199,35,()=>{CloseMenu();action();});b.interactable=enabled;}
            if(n.parentFolder=="trash"){Item(n.unrestorable?"복원 불가":"복원",()=>{Files.RestoreTrash(n);app.Audio.Play("sfx_restore");},!n.unrestorable);return;}
            if(n.extension=="zip"){Item("압축해제",Extract);return;}
            Item("열기",()=>Open(n.id));Item("복원",()=>Restore(n.id),RestoreUnlocked&&n.isBroken);Item("이름 바꾸기",()=>{if(!n.renamable){FileUI.Notice("이 파일의 이름은 바꿀 수 없습니다.");return;}FileRename.Begin(icon,n.displayName,name=>Rename(n.id,name));});Item("삭제",()=>Delete(n.id));
        }
        void CloseRelated(string id){foreach(var owner in app.Windows.GetComponentsInChildren<FileOwner>())if(owner.Id==id||Files.IsWithin(Files.Find(owner.Id),id))owner.GetComponent<FakeWindow>()?.Close();}
        public bool Rename(string id,string name){if(!Ready)return false;var n=Files.Find(id);if(!Files.Rename(n,name,out var error)){FileUI.Notice(error);return false;}CloseRelated(id);app.Audio.Play(n.isBroken?"sfx_file_broken":"sfx_rename");return true;}
        public void Restore(string id){if(!Ready)return;var n=Files.Find(id);if(!RestoreUnlocked||!n.isBroken)return;CloseRelated(id);Files.RestoreOriginal(n);app.Audio.Play("sfx_restore");}
        public void Delete(string id){if(!Ready)return;CloseRelated(id);Files.Delete(Files.Find(id));app.Audio.Play("sfx_trash");}
        public void Open(string id)
        {
            if(!Ready)return;CloseMenu();var n=Files.Find(id);if(n==null||!Files.CanUse(n))return;
            if(n.contentType=="folder"){OpenFolder(id);return;}
            switch(Files.ActionFor(n))
            {
                case "notepad":NotepadWindow.Open(this,n);break;
                case "zip":FileUI.Notice("마우스를 사용해 압축된 파일을 먼저 해제하시오.");break;
                case "image":var viewer=ImageViewer.Open(new ViewerDefinition{id="s4:"+id,title=n.displayName,imageKey=n.imageKey,zoomInLevels=4,overlays=new[]{new ViewerRegion{id="dir",text=".dir",x=330,y=230,width=35,height=20,minZoom=Data.diaryHintZoom}}});if(!viewer.Window.GetComponent<FileOwner>())viewer.Window.gameObject.AddComponent<FileOwner>().Id=id;break;
                case "diary":OpenDiary(n);break;
                case "install":if(!Puzzle2)StartCoroutine(Install());else FileUI.Notice("이미 설치되어 있습니다.",sound:false);break;
                case "dream":EnterDream();break;
                default:FileUI.Notice("파일을 열 수 없습니다. 형식이 올바르지 않거나 손상된 파일입니다.");break;
            }
        }
        public void RestoreDiaryDisposition(){if(app.Save.Progress.flags.Contains("stage04.diaryTrashed")){var n=Files.Find("diary");Files.Rename(n,"일기.dir",out _);Files.LockInTrash(n);}}
        public void ReadText(FakeFileNode n){if(n.id=="to_document"&&NotepadEncodings.IsCorrect(n.encoding))Puzzle1=true;}
        public void Extract(){if(!Ready||Files.Find("zip")==null||!Files.CanUse(Files.Find("zip")))return;PasswordDialog.Open("s4:password",value=>Ready&&value==Password&&Files.ExtractArchive("zip","important"),()=>{app.Audio.Play("sfx_unzip");});var w=app.Windows.GetComponentsInChildren<FakeWindow>().FirstOrDefault(w=>w.Key=="s4:password");if(w&&!w.GetComponent<FileOwner>())w.gameObject.AddComponent<FileOwner>().Id="zip";}
        IEnumerator Install(){busy=true;yield return manager.InstallProgram("money.exe",Data.installSeconds,"sfx_ding","sfx_ding");Files.Reveal("mydream");busy=false;}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void Update(){if(Keyboard.current?.f1Key.wasPressedThisFrame==true){var old=app.Windows.GetComponentsInChildren<FakeWindow>().FirstOrDefault(w=>w.Key=="s4:debug");if(old){old.Close();return;}FakeWindow.TryCreate(app.Windows,"s4:debug","개발 파일 상태",out var w,220,150,620,440);FileUI.ScrollText(w.Content,"암호: "+Password+"\n"+string.Join("\n",Files.Nodes.Select(n=>n.displayName+" / "+n.extension+" / broken="+n.isBroken)),0,0,600,380,Color.black);}}
#endif
    }
}
