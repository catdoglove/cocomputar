using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace CoComputar
{
    public sealed partial class Stage06Controller:MonoBehaviour,IStageEntry
    {
        public Stage06Config Data{get;private set;}public FakeFileSystem Files{get;private set;}public Stage06Randomizer Randoms{get;private set;}
        public bool Ready=>!busy&&!app.Dialogue.Active&&manager.State!=StageState.Transition;
        public bool BookComplete=>stuck.Count==Data.slots.Length;public bool BlockSeen{get;private set;}public bool PlayedGame{get;private set;}
        public readonly HashSet<string> stuck=new();public event Action<string> OnMiniGameStarted,OnMiniGameCleared;
        GameApp app;StageManager manager;RectTransform desktop;bool busy,talk1,talk2;GameObject debug;FilePopup menu;
        
        public void Enter(StageManager mgr,StageDefinition definition)
        {
            app=GameApp.Instance;manager=mgr;Data=JsonUtility.FromJson<Stage06Config>(Resources.Load<TextAsset>("Stage06Config").text);Files=new FakeFileSystem(JsonUtility.FromJson<FakeFileData>(Resources.Load<TextAsset>("Stage06FileLayout").text));Randoms=new Stage06Randomizer(Data);Randoms.Apply(Files);
            app.SetFileRightClick(true);app.DesktopSettingsClicked=app.OpenOptions;app.Dialogue.Load(definition.dialogue);app.Audio.Music(Data.bgm);
            desktop=UIFactory.Rect(app.ScreenRoot,"Stage06 파일",0,38,1024,730);Files.Changed+=Refresh;Refresh();
        }
        public void Talk(){if(!Ready)return;string seq=BlockSeen?"S6_AFTER_BLOCK":PlayedGame&&!talk2?"S6_TALK_2":talk1?"S6_TALK_BASE":"S6_TALK_1";app.Dialogue.Play(()=>{if(seq=="S6_TALK_1")talk1=true;if(seq=="S6_TALK_2")talk2=true;},seq);}
        void Refresh()
        {
            UIFactory.Clear(desktop);FakeIcon.Create(desktop,"joystick","조이스틱","icon_joystick",800,170,Talk);
            foreach(var n in Files.Children("desktop").Where(n=>!n.hidden))AddIcon(desktop,n,n.x,n.y);
            foreach(var explorer in app.Windows.GetComponentsInChildren<Stage05ExplorerWindow>())explorer.Refresh();
        }
        public void AddIcon(Transform parent,FakeFileNode n,float x,float y,Action open=null)
        {
            var icon=FakeIcon.Create(parent,"s6:"+n.id,n.displayName,n.iconKey,x,y,open??(()=>Open(n.id)),true,n.hidden);icon.AllowWindowDrops=true;icon.ScreenBoundDrag=true;
            if(n.contentType=="folder"){var drop=icon.gameObject.AddComponent<DropTarget>();drop.Category="s6:"+n.id;drop.IsFolderWindow=true;}
            icon.Dropped=(i,t)=>Drop(n.id,t.Category);icon.DropWithoutTarget=e=>
            {
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
                if(hits.Any(h=>h.gameObject.transform.IsChildOf(app.Windows)||h.gameObject.transform.IsChildOf(app.Modal))){app.Audio.Play("sfx_drag_return");return;}
                if(n.contentType=="sticker"){app.Audio.Play("sfx_drag_return");return;}
                RectTransformUtility.ScreenPointToLocalPointInRectangle(desktop,e.position,e.pressEventCamera,out var p);n.x=Mathf.Clamp(p.x,0,932);n.y=Mathf.Clamp(-p.y,0,620);Move(n.id,"desktop");
            };
            icon.gameObject.AddComponent<FileRightClick>().Click=e=>Context(n,e,open);
        }
        void Context(FakeFileNode n,PointerEventData e,Action open=null)
        {
            if(!Ready)return;if(menu)menu.Close();var blocker=UIFactory.Panel(app.Modal,"파일 메뉴",0,0,1024,768,Color.clear);menu=blocker.gameObject.AddComponent<FilePopup>();blocker.gameObject.AddComponent<Button>().onClick.AddListener(()=>menu.Close());
            RectTransformUtility.ScreenPointToLocalPointInRectangle(app.Modal,e.position,e.pressEventCamera,out var p);var panel=UIFactory.Panel(blocker.transform,"메뉴",Mathf.Clamp(p.x,0,810),Mathf.Clamp(-p.y,38,550),200,158,WindowSkin.Pink);
            string[] labels={"열기","복원","이름 바꾸기","삭제"};for(int i=0;i<labels.Length;i++){var b=UIFactory.Button(panel.transform,labels[i],3,3+i*38,194,34,()=>{menu.Close();if(open!=null)open();else Open(n.id);});b.interactable=i==0;}
        }
        public void Move(string id,string destination)
        {
            if(!Ready)return;var n=Files.Find(id);var folder=Files.Find(destination);if(n==null||n.contentType=="sticker"||destination!="desktop"&&(folder?.contentType!="folder"||!Files.CanUse(folder)||Files.IsWithin(folder,id))){app.Audio.Play("sfx_drag_return");return;}
            n.parentFolder=destination;Files.Notify();
        }
        public void Drop(string id,string destination)
        {
            if(!Ready)return;if(destination.StartsWith("s6slot:")){Stick(id,destination.Substring(7));return;}if(destination.StartsWith("s6:"))Move(id,destination.Substring(3));else app.Audio.Play("sfx_drag_return");
        }
        public void OpenFolder(string id)
        {
            if(!Ready)return;var n=Files.Find(id);if(n?.contentType!="folder"||!Files.CanUse(n))return;
            Stage05ExplorerWindow.Open(this,id);
        }
        public void Open(string id)
        {
            if(!Ready)return;var n=Files.Find(id);if(n==null||!Files.CanUse(n))return;
            if(n.contentType=="folder"){OpenFolder(id);return;}if(n.contentType=="binary"){Warning("열 수 없는 파일입니다.");return;}
            if(n.contentType=="text"){NotepadWindow.Open(this,n);return;}
            if(id=="book"){if(!BookComplete)StickerBookWindow.Open(this);else RunBook();return;}
            if(n.contentType=="sticker"){ImageViewer.Open(new ViewerDefinition{id="s6:"+id,title=n.displayName,imageKey=n.iconKey});return;}
            if(n.contentType!="game")return;string game=id.Replace("_exe","");var existing=app.Windows.GetComponentsInChildren<MiniGameWindow>().FirstOrDefault(w=>w.GameId==game);if(existing){existing.Window.Focus();return;}
            var errors=GameIntegrityChecker.Check(Files,Data,game);if(errors.Length>0){Warning(errors);return;}
            if(game=="card")CardGameWindow.Open(this);
            else if(game=="snake")SnakeGameWindow.Open(this);
            else
            {
                var file=Files.Find("time");bool valid=TimePropertyParser.Parse(file.content,Data.schema,out var values,out var normalized);file.content=normalized;if(!valid){Warning("데이터가 손상되었습니다.");return;}
                if(!app.Save.Stage06SpecialSeen&&TimePropertyParser.Special(values,Data.easterConditions)){StartCoroutine(SpecialImage());return;}RopeGameWindow.Open(this,int.Parse(values["play_time"]));
            }
        }
        public void Started(string game){PlayedGame=true;OnMiniGameStarted?.Invoke(game);}
        public void Reward(string id,string game)
        {
            var file=Files.Find(id);if(file.available||stuck.Contains(id))return;file.available=true;app.Audio.Play("sfx_sticker_get");Files.Notify();OnMiniGameCleared?.Invoke(game);
            if(id=="star")app.Save.ObtainStage06Star();
        }
        public void SaveText(FakeFileNode file,string text){TimePropertyParser.Parse(text,Data.schema,out _,out var normalized);file.content=normalized;app.Audio.Play("sfx_save_ok");FileUI.Notice("저장되었습니다.","s6:saved",false);}
        public void Warning(params string[] messages)
        {
            if(!FakeWindow.TryCreate(app.Windows,"s6:warning","경고",out var w,230,190,570,250+messages.Length*32))return;
            UIFactory.Text(w.Content,"X",18,45,70,70,50,Color.red);UIFactory.Text(w.Content,"실행불가",105,12,400,46,30,Color.black);UIFactory.Text(w.Content,string.Join("\n",messages),105,67,425,messages.Length*32+30,20,Color.black);UIFactory.Button(w.Content,"확인",205,110+messages.Length*32,130,38,w.Close);app.Audio.Play("sfx_error");
        }
        UnityEngine.EventSystems.EventSystem specialInput;
        bool specialInputWasEnabled,specialActive;
        void EndSpecialImage()
        {
            if(!specialActive)return;specialActive=false;
            if(specialInput)specialInput.enabled=specialInputWasEnabled;
            if(app&&app.Audio)app.Audio.ResumeEventMusic();
            specialInput=null;busy=false;
        }
        IEnumerator SpecialImage()
        {
            app.Save.MarkStage06SpecialSeen();
            busy=specialActive=true;
            app.Audio.PauseEventMusic();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
            specialInput=UnityEngine.EventSystems.EventSystem.current;
            specialInputWasEnabled=specialInput&&specialInput.enabled;
            if(specialInput)specialInput.enabled=false;
            var viewer=ImageViewer.Open(new ViewerDefinition{id="s6:special",title="...",imageKey="s6_glitch",zoomInLevels=0});
            try
            {
                yield return new WaitForSecondsRealtime(Data.easterStillSeconds);
                if(!viewer)yield break;
                app.Audio.Play("sfx_glitch_static");StoryEffects.Get().Glitch(Data.easterSeconds);
                for(float t=0;t<Data.easterSeconds&&viewer;t+=Time.unscaledDeltaTime)
                {
                    viewer.Picture.color=new Color(1,1,1,UnityEngine.Random.Range(.65f,1));yield return null;
                }
                if(viewer)viewer.Window.Close();
                var timeFile=Files.Find("time");timeFile.content=Data.initialText;
                if(app.Windows.GetComponentsInChildren<NotepadWindow>().Any(n=>n.File==timeFile))NotepadWindow.Open(this,timeFile,true);
            }
            finally{EndSpecialImage();}
        }
        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(!specialActive&&Keyboard.current?.f1Key.wasPressedThisFrame==true){if(debug){Destroy(debug);debug=null;}else debug=UIFactory.Text(app.Modal,$"카드: {Randoms.CardMissing} / {Randoms.CardLocation}\n줄넘기: {Randoms.RopeMissing} / {Randoms.RopeLocation}\n정답 창: {Randoms.DoorIndex+1}",170,40,700,110,20,Color.magenta).gameObject;}
#endif
        }
        void OnDestroy(){EndSpecialImage();if(Files!=null)Files.Changed-=Refresh;if(debug)Destroy(debug);if(menu)menu.Close();}
    }
}
