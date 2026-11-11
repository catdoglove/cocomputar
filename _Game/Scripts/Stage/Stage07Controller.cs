using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed class Stage07Controller:MonoBehaviour,IStageEntry
    {
        public Stage07Config Data{get;private set;}public Stage07Assets Assets{get;private set;}public Stage07Randomizer Randoms{get;private set;}
        public InstallStateService Installs{get;private set;}public FakeFileSystem Files{get;private set;}
        public bool AnimationBusy;public bool Ready=>!AnimationBusy&&!App.Dialogue.Active&&manager.State!=StageState.Transition;
        public bool WizardStarted,SecretUnlocked,SecretInstalled,Shears,Key,DoorUnlocked,RockMoved,DollBeheaded;
        public readonly HashSet<string> CutBushes=new();public readonly int[] RotationX=new int[3],RotationY=new int[3];
        public event Action<string> MapEvent;public GameApp App=>GameApp.Instance;
        StageManager manager;RectTransform desktop;bool talked,doorTalk;GameObject debug;
        public string Text(string key)=>Assets.Text(key);
        public void Enter(StageManager mgr,StageDefinition definition)
        {
            manager=mgr;Data=Stage07Config.Load();Assets=Resources.Load<Stage07Assets>("Stage07Assets");Randoms=new Stage07Randomizer(Data);Installs=new InstallStateService();
            Files=new FakeFileSystem(new FakeFileData{files=new[]{
                new FakeFileNode{id="important",displayName="중요 폴더",parentFolder="desktop",contentType="folder",iconKey="folder",available=true},
                new FakeFileNode{id="master",displayName="마스터키",parentFolder="important",contentType="folder",iconKey="folder",available=true},
                new FakeFileNode{id="keytext",displayName="KEY.txt",parentFolder="master",contentType="text",extension="txt",iconKey="document",content=Randoms.KeyText,available=true}}});
            App.SetFileRightClick(false);App.DesktopSettingsClicked=App.OpenOptions;App.Dialogue.Load(definition.dialogue);App.Audio.Music(Data.bgm);
            desktop=UIFactory.Rect(App.ScreenRoot,"Stage07 바탕화면",0,38,1024,730);RefreshDesktop();
        }
        public void RefreshDesktop()
        {
            UIFactory.Clear(desktop);
            FakeIcon.Create(desktop,"floppy","플로피디스크","icon_floppy",820,175,Talk);
            // Keep the first desktop column clear for My Computer and the important folder.
            for(int i=0;i<Data.programs.Length;i++){var p=Data.programs[i];FakeIcon.Create(desktop,"s7:setup:"+p.id,p.installer,"program",210+i*150,40,()=>StartInstaller(p.id));}
            FakeIcon.Create(desktop,"s7:important","중요 폴더","folder",40,195,()=>OpenFolder("important"));
            for(int i=0;i<Data.programs.Length;i++){var p=Data.programs[i];if(p.id!="internet"&&Installs.IsInstalled(p.id))FakeIcon.Create(desktop,"s7:"+p.id,p.name,"program",210+i*150,220,()=>Launch(p.id));}
        }
        public void Talk()
        {
            if(!Ready)return;
            bool eligible=Data.doorEventTrigger=="unlock"?DoorUnlocked:Data.doorEventTrigger=="rockMoved"?RockMoved:Installs.IsInstalled("map");
            string id=doorTalk?"S7_AFTER_DOOR":eligible?"S7_DOOR":WizardStarted?"S7_TALK_3":talked?"S7_TALK_2":"S7_TALK_1";
            App.Dialogue.Play(()=>{talked=true;if(id=="S7_DOOR")doorTalk=true;},id);
        }
        public void OpenFolder(string id)
        {
            if(!Ready)return;var node=Files.Find(id);if(node==null)return;
            if(!FakeWindow.TryCreate(App.Windows,"s7:folder:"+id,node.displayName,out var w,180,120,650,450))return;
            w.SetFileBackground();void Draw(){UIFactory.Clear(w.Content);int i=0;foreach(var n in Files.Children(id))FakeIcon.Create(w.Content,"s7:"+n.id,n.displayName,n.iconKey,20+i++*130,20,()=>{if(!Ready)return;if(n.contentType=="folder")OpenFolder(n.id);else WindowApps.Note(n.displayName,n.content,"s7:keytext");});}
            w.AddHiddenToggle(Draw);Draw();
        }
        public void StartInstaller(string id)
        {
            if(!Ready)return;
            if(Installs.IsInstalled(id)){FileUI.Notice(Text("이미_설치됨"),"s7:installed",title:"경고");return;}
            var running=App.Windows.GetComponentsInChildren<SetupWizardWindow>().FirstOrDefault();if(running){running.Window.Focus();return;}
            var serial=App.Windows.GetComponentsInChildren<SerialDialog>().FirstOrDefault();if(serial){serial.Window.Focus();return;}
            var data=Data.programs.Single(p=>p.id==id);if(data.serial)SerialDialog.Open(this,data);else SetupWizardWindow.Open(this,data);
        }
        public void Installed(SetupWizardData data,bool secret)
        {
            if(!Installs.Complete(data.id))return;
            if(data.id=="internet"&&secret){SecretInstalled=true;App.Save.Flag("stage07.secretBrowserInstalled");}
            App.Audio.Play("sfx_install_done");RefreshDesktop();
        }
        public void Launch(string id)
        {
            if(!Ready)return;
            if(id=="secret"&&!SecretInstalled||id!="secret"&&!Installs.IsInstalled(id))return;
            if(id=="viewer"){ShapeViewerWindow.Open(this);return;}
            if(id=="map")
            {
                if(Data.requireInternet&&!Installs.IsInstalled("internet")){FileUI.Notice(Text("인터넷_없음"),"s7:offline",title:"경고");return;}
                Map07Window.Open(this);return;
            }
            if(!FakeWindow.TryCreate(App.Windows,"s7:browser:"+id,id=="secret"?"시크릿 브라우저.exe":"인터넷.exe",out var w,200,130,650,440))return;
            UIFactory.Text(w.Toolbar,Data.browserAddress,5,2,620,30,19);UIFactory.Text(w.Content,id=="secret"&&Data.secretBrowserPages.Length>0?Data.secretBrowserPages[0]:Text("브라우저_본문"),20,40,570,170,24);
            if(id=="internet")UIFactory.Button(w.Content,"map.exe",20,230,200,40,()=>Launch("map"));
        }
        public void EmitMapEvent(string id)=>MapEvent?.Invoke(id);
        public void ExitDoor(){if(Ready&&DoorUnlocked)manager.RequestClear("door.exe");else App.Audio.Play("sfx_door_locked");}
        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(Ready&&Keyboard.current?.f1Key.wasPressedThisFrame==true){if(debug)Destroy(debug);else debug=UIFactory.Text(App.Modal,"map: "+Randoms.Serial("map")+"  인터넷: "+Randoms.Serial("internet")+"\n"+string.Join(" / ",Randoms.Letters.Where(l=>l.Color=="blue"||l.Color=="green").Select(l=>$"{l.Shape+1}:{l.Letter}({l.X},{l.Y})")),70,40,920,100,18,Color.magenta).gameObject;}
#endif
        }
        void OnDestroy(){if(debug)Destroy(debug);}
    }
    // 기존 흰 셔츠 하단을 UI에서 재사용하는 임시 합성 CG. 최종 portrait_floppy로 교체 가능.
    public sealed class FloppyPortrait:MonoBehaviour
    {
        Image source;RawImage body;Image head;
        public void Setup(Image image)
        {
            source=image;var tex=image.sprite.texture;
            body=UIFactory.Rect(transform,"셔츠",0,128,335,262).gameObject.AddComponent<RawImage>();body.texture=tex;body.uvRect=new Rect(0,0,1,.64f);body.raycastTarget=false;
            head=UIFactory.Sprite(transform,GameApp.Instance.Catalog.sprites.Get("icon_floppy"),90,0,160,145);
        }
        void LateUpdate(){if(!source)return;source.enabled=false;body.color=head.color=source.color;}
    }
}
