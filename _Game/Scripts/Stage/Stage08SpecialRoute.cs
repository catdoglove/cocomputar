using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace CoComputar
{
    // Replica scenes own only visuals. They never enter Stage01/Stage09 or run their puzzle controllers.
    public sealed class Stage08SpecialRoute:MonoBehaviour
    {
        public Stage08SpecialProgress Progress{get;private set;}
        public Stage08SpecialRules Rules{get;private set;}
        public bool Locked=>Progress.warpInstalled;
        public bool Replica=>Progress.state>=SpecialRouteState.StageReturn1;
        Stage08Controller stage;GameApp app;GameApp App=>app;RectTransform desktop;Image overlay;
        TextAsset dialogue;int repeat;bool running,paused,cursorArrived,accelerateCursor;Coroutine cursorMotion;AudioSource heart;
        FakeIcon npc;float idle;GameObject pausePanel;
        bool HasFocus
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // 숨긴 자동 검사 창만 포커스 검사를 생략한다. 실제 플레이의 포커스 일시정지는 유지.
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"-coSpecialQA")>=0)return true;
#endif
                return Application.isFocused;
            }
        }
        public void Setup(Stage08Controller controller,RectTransform root,Stage08Assets assets)
        {
            stage=controller;app=controller.App;desktop=root;dialogue=assets.specialDialogue;
            Rules=JsonUtility.FromJson<Stage08SpecialRules>(assets.specialRules.text);
            Progress=App.Save.Progress.stage08Special??new Stage08SpecialProgress();
            App.Save.Progress.stage08Special=Progress;
            if(Progress.state==SpecialRouteState.Ending) {Progress=new Stage08SpecialProgress();App.Save.Progress.stage08Special=Progress;}
            if(Progress.files!=null)
            {
                stage.Files.Nodes.Clear();stage.Files.Nodes.AddRange(JsonUtility.FromJson<FakeFileData>(JsonUtility.ToJson(new FakeFileData{files=Progress.files})).files);
                stage.Acquired.UnionWith(Progress.acquired??Array.Empty<string>());stage.Ordered=Progress.ordered;stage.EyeDone=Progress.eyeDone;stage.GlobeTaken=Progress.globeTaken;stage.RestoreTokens(Progress.tokens);stage.NpcCreated=true;
            }
            if(Progress.state==SpecialRouteState.GameOver||Progress.state==SpecialRouteState.FakeDoorGiven)Progress.state=SpecialRouteState.Offered;
            if(Progress.state>SpecialRouteState.WarpInstalled)Progress.state=Progress.checkpoint;
            stage.Files.Nodes.RemoveAll(n=>n.id=="deep:fake"&&Progress.state==SpecialRouteState.Offered);
            App.Dialogue.Load(dialogue);App.Dialogue.FlagCommand=Command;
            App.ComputerBlocked=()=>{if(!stage.Ready)return true;if(!Locked)return false;if(!Replica||Progress.state<SpecialRouteState.Stage9Replica)Denied();return true;};
            if(Locked)BindLocks();
            stage.Files.Changed+=SaveActive;
        }
        void SaveActive(){if(Progress.state!=SpecialRouteState.NotStarted)Save();}
        void BindLocks(){App.ClockClicked=Denied;App.DesktopSettingsClicked=Denied;}
        public void Save()
        {
            Progress.files=JsonUtility.FromJson<FakeFileData>(JsonUtility.ToJson(new FakeFileData{files=stage.Files.Nodes.ToArray()})).files;
            Progress.acquired=stage.Acquired.ToArray();Progress.ordered=stage.Ordered;Progress.eyeDone=stage.EyeDone;Progress.globeTaken=stage.GlobeTaken;Progress.tokens=stage.Tokens;
            App.Save.SaveProgress();
        }
        void State(SpecialRouteState value,bool checkpoint=false){Progress.state=value;if(checkpoint)Progress.checkpoint=value;Save();}
        void Sound(string key)=>App.Audio.PlayCapped(key,Rules.sfxCap);
        void AddFile(string id,string name,string icon,string content="",float x=530,float y=345)
        {
            if(stage.Files.Find(id)!=null)return;
            stage.Files.Nodes.Add(new FakeFileNode{id=id,displayName=name,parentFolder="desktop",iconKey=icon,extension=name.Split('.').Last(),contentType="document",content=content,x=x,y=y});stage.Files.Notify();
        }
        public void Denied(){Sound("sfx_lock_denied");FileUI.Notice(Rules.lockWarning,"deep:locked",false,"경고");}
        public bool Open(string id)
        {
            if(Locked){Denied();return true;}
            if(id=="deep:fake"){StartCoroutine(FakeDoor());return true;}
            if(id=="deep:installer"){SetupWizardWindow.OpenSpecial(this);return true;}
            return false;
        }
        public void Draw()
        {
            if(Replica){DrawReplica();return;}
            if(stage.NpcCreated&&Progress.state!=SpecialRouteState.FakeDoorGiven&&Progress.state!=SpecialRouteState.GameOver)
                npc=FakeIcon.Create(desktop,"s8:npc",stage.Text("globe"),"icon_npc_stage8",820,210,Talk);
            if(Locked)foreach(var icon in desktop.GetComponentsInChildren<FakeIcon>())if(icon!=npc)UIFactory.Text(icon.transform,"▣",58,38,25,26,20,Color.yellow).raycastTarget=false;
        }
        public void Talk()
        {
            if(!stage.Ready||running)return;idle=0;
            switch(Progress.state)
            {
                case SpecialRouteState.NotStarted:Play("DEEP_1",()=>State(SpecialRouteState.Talked1));break;
                case SpecialRouteState.Talked1:case SpecialRouteState.Offered:
                    Play("DEEP_2",()=>{State(SpecialRouteState.Offered,true);Offer();});break;
                case SpecialRouteState.WarpInstalled:
                    Play("DEEP_7",()=>{State(SpecialRouteState.Talked3);StartCoroutine(Travel(true));});break;
                default:
                    if(!Locked)Play("DEEP_REPEAT_"+(repeat++%2),()=>{});break;
            }
        }
        void Play(string id,Action done,bool noPortraits=false,bool reunion=false,bool joy=false)
        {
            running=true;stage.Busy=true;
            App.Dialogue.Load(dialogue);App.Dialogue.FlagCommand=Command;
            App.Dialogue.SpecialPresentation=noPortraits?"hidden":reunion?"reunion":"deep";
            App.Dialogue.Play(()=>{App.Dialogue.SpecialPresentation=null;stage.Busy=false;running=false;done?.Invoke();},id);
        }
        void Command(string command)
        {
            if(command=="special_account")GiveAccount();
            if(command=="special_cursor"&&cursorMotion==null)cursorMotion=StartCoroutine(ForceCursor(new Vector2(690,-295)));
        }
        void Offer()
        {
            stage.Busy=true;overlay=UIFactory.Panel(App.Modal,"선택 입력 잠금",0,0,1024,768,new Color(0,0,0,.2f));
            FakeWindow.TryCreate(overlay.transform,"deep:offer","선택",out var window,290,230,450,250);
            window.transform.Find("제목 표시줄/X").gameObject.SetActive(false);
            UIFactory.Text(window.Content,Rules.choice,20,20,390,60,24);
            void Choose(bool yes){if(!stage.Busy)return;Destroy(overlay.gameObject);overlay=null;stage.Busy=false;if(yes)Play("DEEP_4",()=>{if(string.IsNullOrEmpty(Progress.adminPw))GiveAccount();Save();});else Play("DEEP_3",()=>StartCoroutine(VanishNpc()));}
            UIFactory.Button(window.Content,"예",60,110,130,38,()=>Choose(true));UIFactory.Button(window.Content,"아니오",240,110,130,38,()=>Choose(false));
        }
        IEnumerator VanishNpc()
        {
            stage.Busy=true;Sound("sfx_npc_vanish");if(npc){var group=npc.gameObject.AddComponent<CanvasGroup>();for(float t=0;t<.6f;t+=Time.unscaledDeltaTime){group.alpha=1-t/.6f;yield return null;}}
            AddFile("deep:fake","door.exe","door",x:700,y:360);State(SpecialRouteState.FakeDoorGiven);stage.RefreshDesktop();stage.Busy=false;
        }
        void GiveAccount()
        {
            if(!string.IsNullOrEmpty(Progress.adminPw))return;
            Progress.adminPw=Rules.GeneratePassword();AddFile("deep:account","관리자계정.txt","document",Rules.accountTemplate.Replace("{adminPw}",Progress.adminPw));
            State(SpecialRouteState.AdminFileGiven);Sound("sfx_file_appear");
        }
        public bool Login(string id,string password)
        {
            if(string.IsNullOrEmpty(Progress.adminPw)||id!=Progress.adminId||password!=Progress.adminPw){Sound("sfx_login_fail");return false;}
            Progress.adminLoggedIn=true;if(Progress.state<SpecialRouteState.LoggedIn)State(SpecialRouteState.LoggedIn);else Save();return true;
        }
        public void Download()
        {
            if(!Progress.adminLoggedIn||Locked)return;
            if(stage.Files.Find("deep:installer")!=null){FileUI.Notice("이미 다운로드한 파일입니다.","deep:download",false,"알림");return;}
            stage.Confirm("설치관리자.exe를 바탕화면에 다운로드하시겠습니까?",()=>{AddFile("deep:installer","설치관리자.exe","program",x:650,y:345);State(SpecialRouteState.InstallerDownloaded);Sound("sfx_file_appear");},"다운로드");
        }
        public void Installed()
        {
            if(Locked)return;Progress.warpInstalled=true;AddFile("deep:warp","워프.exe","program",x:760,y:345);State(SpecialRouteState.WarpInstalled,true);Sound("sfx_warp_install");stage.RefreshDesktop();
            BindLocks();
        }
        IEnumerator FakeDoor()
        {
            stage.Busy=true;App.CloseAllWindows();App.Audio.PauseEventMusic();overlay=UIFactory.Panel(App.Modal,"가짜 오류 연출",0,0,1024,768,Color.clear);
            for(int i=0;i<Rules.errorCount;i++)
            {
                FakeWindow.TryCreate(overlay.transform,"deep:error:"+i,"경고",out var window,55+i*70,65+i*65,520,235);
                UIFactory.Text(window.Content,Rules.errorMessages[i%Rules.errorMessages.Length],20,25,460,100,23);window.transform.Find("제목 표시줄/X").gameObject.SetActive(false);
                Sound("sfx_fake_error");StoryEffects.Get().Glitch(.15f);yield return new WaitForSecondsRealtime(.35f);
            }
            UIFactory.Clear(overlay.transform);overlay.color=Color.black;Sound("sfx_gameover");State(SpecialRouteState.GameOver);
            UIFactory.Text(overlay.transform,"GAME OVER",300,230,500,75,46,Color.white);UIFactory.Text(overlay.transform,Rules.gameover,190,340,700,60,22,Color.white);
            UIFactory.Button(overlay.transform,"다시 시도",295,470,200,45,()=>{Destroy(overlay.gameObject);overlay=null;stage.Busy=false;stage.Files.Nodes.RemoveAll(n=>n.id=="deep:fake");State(SpecialRouteState.Offered,true);App.Audio.ResumeEventMusic();stage.RefreshDesktop();Talk();});
            UIFactory.Button(overlay.transform,"타이틀",525,470,200,45,App.ShowTitle);
        }
        IEnumerator Travel(bool reverse)
        {
            stage.Busy=true;App.CloseAllWindows();overlay=UIFactory.Panel(App.Modal,"특수 전환",0,0,1024,768,Color.clear);
            yield return App.Stages.ConsoleSequence(overlay.transform,reverse,Rules.removeLineSeconds);
            overlay.color=Color.black;UIFactory.Clear(overlay.transform);
            State(reverse?SpecialRouteState.StageReturn1:SpecialRouteState.Stage9Replica,true);
            stage.RefreshDesktop();App.Audio.Music(reverse?"bgm_special_stage1":"bgm_special_stage9");yield return new WaitForSecondsRealtime(.6f);
            Destroy(overlay.gameObject);overlay=null;stage.Busy=false;
        }
        void DrawReplica()
        {
            UIFactory.Clear(desktop);
            bool first=Progress.state<SpecialRouteState.Stage9Replica;
            var theme=App.Catalog.stages.FirstOrDefault(s=>s.stageId==(first?"Stage01":"Stage09"))?.desktopTheme;
            UIFactory.Panel(desktop,"복제 배경",0,0,1024,730,theme?theme.top:first?new Color(.1f,.65f,.7f):new Color(.25f,.28f,.38f));
            Action blocked=first?Denied:()=>{};
            LockedIcon("내 컴퓨터","computer",30,25,blocked);LockedIcon("작업 폴더","folder",265,204,blocked);
            if(first)
            {
                if(Progress.state==SpecialRouteState.StageReturn1)npc=FakeIcon.Create(desktop,"deep:folder","폴더군","folder",140,204,()=>{if(!stage.Ready)return;State(SpecialRouteState.FolderTalked);Play("DEEP_8",()=>StartCoroutine(FolderVanish()),reunion:true);});
                else LockedIcon("폴더군","folder",140,204,Denied);
                FakeIcon.Create(desktop,"deep:internet","인터넷","icon_npc_stage8",850,545,()=>{});
            }
            else
            {
                LockedIcon("기록","document",160,25,blocked);LockedIcon("door.exe","door",290,25,blocked);
                if(Progress.state==SpecialRouteState.Stage9Replica)
                    npc=FakeIcon.Create(desktop,"deep:keynpc","열쇠","icon_key_special",650,220,()=>{if(!stage.Ready)return;State(SpecialRouteState.KeyTalking);cursorArrived=false;Play("DEEP_10",()=>StartCoroutine(KeyDrop()));});
                if(Progress.state==SpecialRouteState.KeyObtained)
                    FakeIcon.Create(desktop,"deep:key","열쇠.key","icon_key_special",650,345,()=>{if(!stage.Ready)return;Play("DEEP_11",()=>StartCoroutine(ShowForbidden()));});
                if(Progress.state>=SpecialRouteState.ForbiddenFolderShown)
                {
                    var forbidden=FakeIcon.Create(desktop,"deep:forbidden","","folder",95,550,Final);
                    foreach(var image in forbidden.GetComponentsInChildren<Image>())if(image.sprite)image.color=new Color(.06f,.04f,.08f);UIFactory.Text(desktop,"▣",150,585,32,30,22,Color.yellow);
                    FakeIcon.Create(desktop,"deep:internet","인터넷","icon_npc_stage8",210,550,Final);
                }
                else FakeIcon.Create(desktop,"deep:internet","인터넷","icon_npc_stage8",850,545,()=>{});
            }
        }
        void LockedIcon(string label,string icon,float x,float y,Action action){var item=FakeIcon.Create(desktop,"deep:locked:"+label,label,icon,x,y,action);UIFactory.Text(item.transform,"▣",58,38,25,26,20,Color.yellow).raycastTarget=false;}
        IEnumerator FolderVanish()
        {
            stage.Busy=true;overlay=UIFactory.Panel(App.Modal,"폴더 소멸",0,0,1024,768,Color.clear);
            for(float t=0;t<1;t+=Time.unscaledDeltaTime){overlay.color=new Color(0,0,0,t);yield return null;}
            var silhouette=UIFactory.Sprite(overlay.transform,App.Catalog.sprites.Get("special_silhouette"),0,0,1024,560);
            bool done=false;int revealed=0;
            App.Dialogue.LineRevealed=line=>{revealed++;silhouette.rectTransform.anchoredPosition+=Vector2.left*24;};
            Play("DEEP_9_SPLIT",()=>done=true,true);
            while(revealed<3&&!done)yield return null;
            App.Dialogue.LineRevealed=null;
            stage.Busy=true;Sound("sfx_head_split");var head=UIFactory.Sprite(overlay.transform,App.Catalog.sprites.Get("folder"),140,145,135,110);
            for(float t=0;t<.4f;t+=Time.unscaledDeltaTime){head.rectTransform.anchoredPosition+=Vector2.up*Time.unscaledDeltaTime*1800;yield return null;}
            Destroy(head.gameObject);while(!done)yield return null;Destroy(silhouette.gameObject);State(SpecialRouteState.FolderGone);stage.RefreshDesktop();
            for(float t=0;t<.7f;t+=Time.unscaledDeltaTime){overlay.color=new Color(0,0,0,1-t/.7f);yield return null;}
            Destroy(overlay.gameObject);overlay=null;Play("DEEP_9_EMPTY",()=>StartCoroutine(Joy()),true);
        }
        IEnumerator Joy()
        {
            stage.Busy=true;heart=gameObject.AddComponent<AudioSource>();heart.clip=App.Catalog.audio.Get("sfx_heartbeat");heart.outputAudioMixerGroup=App.Catalog.effectsGroup;heart.loop=true;heart.volume=0;heart.Play();
            for(float t=0;t<2;t+=Time.unscaledDeltaTime){heart.volume=Mathf.Lerp(0,Rules.sfxCap,t/2);heart.pitch=Mathf.Lerp(.8f,1.3f,t/2);yield return null;}
            Play("DEEP_9_JOY",()=>{heart.Stop();StartCoroutine(Travel(false));},joy:true);
        }
        IEnumerator ForceCursor(Vector2 target)
        {
            var cursor=App.GetComponent<FakeCursor>();Vector2 start=cursor.Position;cursor.Controlled=true;float elapsed=0;
            while(elapsed<Rules.cursorSeconds)
            {
                if(!paused&&HasFocus){elapsed+=Time.unscaledDeltaTime*(accelerateCursor?5:1);cursor.ControlledPosition=Vector2.Lerp(start,target,Mathf.SmoothStep(0,1,elapsed/Rules.cursorSeconds));}
                yield return null;
            }
            cursor.ControlledPosition=target;cursorArrived=true;cursorMotion=null;
        }
        IEnumerator KeyDrop()
        {
            stage.Busy=true;accelerateCursor=true;if(cursorMotion==null&&!cursorArrived)cursorMotion=StartCoroutine(ForceCursor(new Vector2(690,-295)));
            while(!cursorArrived)yield return null;
            Sound("sfx_key_drop");if(npc){var group=npc.gameObject.AddComponent<CanvasGroup>();for(float t=0;t<.7f;t+=Time.unscaledDeltaTime){group.alpha=1-t/.7f;yield return null;}}
            App.GetComponent<FakeCursor>().Controlled=false;accelerateCursor=false;State(SpecialRouteState.KeyObtained,true);stage.RefreshDesktop();stage.Busy=false;
        }
        IEnumerator ShowForbidden()
        {
            stage.Busy=true;State(SpecialRouteState.KeyTalked);
            foreach(var icon in desktop.GetComponentsInChildren<FakeIcon>())if(icon.Id=="deep:internet")icon.gameObject.SetActive(false);
            var actor=UIFactory.Sprite(desktop,App.Catalog.sprites.Get("icon_npc_stage8"),850,545,80,90);
            for(float t=0;t<1.4f;t+=Time.unscaledDeltaTime){actor.rectTransform.anchoredPosition=Vector2.Lerp(new Vector2(850,-545),new Vector2(210,-550),t/1.4f);yield return null;}
            Destroy(actor.gameObject);State(SpecialRouteState.ForbiddenFolderShown);stage.RefreshDesktop();Sound("sfx_forbidden_folder");StoryEffects.Get().Glitch(.2f);stage.Busy=false;
        }
        void Final(){if(!stage.Ready||running)return;Play("DEEP_12",()=>{State(SpecialRouteState.FinalTalked);StartCoroutine(Ending());});}
        IEnumerator Ending()
        {
            stage.Busy=true;App.CloseAllWindows();overlay=UIFactory.Panel(App.Modal,"폴더 진입",0,0,1024,768,Color.clear);
            var key=UIFactory.Sprite(overlay.transform,App.Catalog.sprites.Get("icon_key_special"),650,383,70,60);
            for(float t=0;t<.7f;t+=Time.unscaledDeltaTime){key.rectTransform.anchoredPosition=Vector2.Lerp(new Vector2(650,-383),new Vector2(130,-610),t/.7f);yield return null;}Destroy(key.gameObject);Sound("sfx_deep_enter");
            foreach(var icon in desktop.GetComponentsInChildren<FakeIcon>())if(icon.Id=="deep:internet")icon.gameObject.SetActive(false);
            var actor=UIFactory.Sprite(overlay.transform,App.Catalog.sprites.Get("icon_npc_stage8"),210,588,80,90);
            for(float t=0;t<1;t+=Time.unscaledDeltaTime){actor.rectTransform.anchoredPosition=Vector2.Lerp(new Vector2(210,-588),new Vector2(130,-610),t);actor.transform.localScale=Vector3.one*(1-t);yield return null;}Destroy(actor.gameObject);
            var cursor=App.GetComponent<FakeCursor>();cursorArrived=false;cursorMotion=StartCoroutine(ForceCursor(new Vector2(130,-610)));while(!cursorArrived)yield return null;cursor.SetVisible(false);
            overlay.color=Color.black;App.Audio.PauseEventMusic();var title=UIFactory.Text(overlay.transform,"「더 깊고 깊게...」",150,320,724,100,38,Color.clear);title.alignment=TextAlignmentOptions.Center;
            for(float t=0;t<1;t+=Time.unscaledDeltaTime){title.color=new Color(1,1,1,t);yield return null;}
            State(SpecialRouteState.Ending);App.Save.CompleteDeepEnding();yield return new WaitForSecondsRealtime(Rules.endingSeconds);cursor.Controlled=false;cursor.SetVisible(true);App.ShowTitle();
        }
        void Update()
        {
            if(Progress==null)return;
            if(App.GetComponent<FakeCursor>().Controlled){App.Dialogue.Paused=paused||!HasFocus;App.GetComponent<FakeCursor>().ControlPaused=paused||!HasFocus;}
            if(App.GetComponent<FakeCursor>().Controlled&&Keyboard.current?.escapeKey.wasPressedThisFrame==true)
            {
                paused=!paused;App.Dialogue.Paused=paused;
                if(paused){pausePanel=UIFactory.Panel(App.Modal,"일시정지",0,0,1024,768,new Color(0,0,0,.8f)).gameObject;UIFactory.Button(pausePanel.transform,"계속",340,290,340,45,Resume);UIFactory.Button(pausePanel.transform,"타이틀",340,360,340,45,App.ShowTitle);UIFactory.Button(pausePanel.transform,"종료",340,430,340,45,GameApp.Quit);}
                else Resume();
            }
            if(Locked&&!Replica&&stage.Ready&&npc){idle+=Time.unscaledDeltaTime;if(Mouse.current?.leftButton.wasPressedThisFrame==true)idle=0;npc.transform.localRotation=Quaternion.Euler(0,0,idle>Rules.idleSeconds?Mathf.Sin(Time.unscaledTime*5)*3:0);}
        }
        void Resume(){App.GetComponent<FakeCursor>().ControlPaused=false;paused=false;App.Dialogue.Paused=false;if(pausePanel)Destroy(pausePanel);}
        void OnDestroy(){if(stage&&stage.Files!=null)stage.Files.Changed-=SaveActive;if(!app)return;App.Dialogue.LineRevealed=null;App.Dialogue.SpecialPresentation=null;App.Dialogue.Paused=false;var cursor=App.GetComponent<FakeCursor>();cursor.Controlled=false;cursor.ControlPaused=false;cursor.SetVisible(true);if(heart)heart.Stop();App.Audio.ResumeEventMusic();if(overlay)Destroy(overlay.gameObject);if(pausePanel)Destroy(pausePanel);}
    }
}
