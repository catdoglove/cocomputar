using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace CoComputar
{
    public sealed class Stage03Controller:MonoBehaviour,IStageEntry
    {
        public Stage03Data Data{get;private set;}public Stage03Randomizer Puzzle{get;private set;}
        public bool IntroDone{get;private set;}public bool Updated{get;private set;}public bool SecretUnlocked{get;private set;}
        public bool PastPngOpened{get;private set;}
        public bool Past{get;private set;}public bool PastDone{get;private set;}public bool Busy{get;private set;}
        public bool PatchFound{get;private set;}public int TrailStep{get;private set;}public float TrailProgress{get;private set;}
        public readonly List<HiddenObject> Hidden=new();
        readonly Dictionary<string,FakeIcon> icons=new();GameApp app;StageManager manager;bool p1Talk,pastTalk;
        GameObject lockMark;FilmLensWindow lens;TMP_Text debug;Material visionMaterial;
        bool Blocked=>Busy||app.Dialogue.Active||manager.State==StageState.Transition;
        public void Enter(StageManager mgr,StageDefinition definition)
        {
            app=GameApp.Instance;manager=mgr;Data=JsonUtility.FromJson<Stage03Data>(Resources.Load<TextAsset>("Stage03Config").text);Data.trail.Build();Puzzle=new Stage03Randomizer(Data);p1Talk=app.Save.Progress.flags.Contains("stage03.p1Talk");PastPngOpened=app.Save.Progress.flags.Contains("stage03.pastPngOpened");
            app.Dialogue.Load(definition.dialogue);app.Audio.Music(Data.music);
            foreach(var item in Data.layout)
            {
                if(item.id=="computer"){var computer=Array.Find(app.ScreenRoot.GetComponentsInChildren<FakeIcon>(),i=>i.Id=="computer");computer.Rect.anchoredPosition=new Vector2(item.x,-item.y);computer.Clicked=()=>WindowApps.Alert(Data.deniedMessage);continue;}
                string id=item.id;icons[id]=FakeIcon.Create(app.ScreenRoot,id,item.name,item.sprite,item.x,item.y,()=>Click(id));
            }
            var padlock=UIFactory.Rect(icons["secret"].transform,"자물쇠",58,28,26,32);lockMark=padlock.gameObject;
            UIFactory.Panel(padlock,"고리",5,0,16,17,Color.black).raycastTarget=false;UIFactory.Panel(padlock,"고리 안",9,4,8,10,new Color(.76f,.76f,.78f)).raycastTarget=false;
            UIFactory.Panel(padlock,"잠금",0,13,26,19,new Color(.3f,.25f,.13f)).raycastTarget=false;UIFactory.Panel(padlock,"열쇠구멍",11,18,4,9,Color.white).raycastTarget=false;
            app.ClockClicked=()=>{if(!Blocked)DateTimeWindow.Open(ApplyDate,app.DisplayDate??DateTime.Now,()=>Past||Busy);};
            SetupHidden();
        }
        public void Click(string id)
        {
            if(Blocked||hiddenInPast.ContainsKey(id))return;
            if(id=="geurim"){Talk();return;}if(id=="pieces"){OpenPieces();return;}if(id=="secret"){OpenSecret();return;}
            if(id=="film"||id=="bad1"||id=="bad2")
            {
                if(!IntroDone){app.Audio.Play(Data.pop);icons["geurim"].gameObject.AddComponent<Stage03NpcPop>();app.Dialogue.Play(()=>IntroDone=true,"S3_INTRO_A","S3_INTRO_COMMON");return;}
                if(id!="film"){app.Audio.Play(Data.denied);WindowApps.Alert(Data.deniedMessage);return;}OpenFilm();return;
            }
            if(id=="patch")StartCoroutine(Install());
            if(id=="memory"&&Past&&!PastDone)StartCoroutine(Memory());
        }
        public void Talk()
        {
            if(Blocked)return;
            if(Past){app.Dialogue.Play(()=>{if(!pastTalk){pastTalk=true;var p=Data.layout[Array.FindIndex(Data.layout,x=>x.id=="geurim")];icons["memory"]=FakeIcon.Create(app.ScreenRoot,"memory","d#or.e?e","door",p.x-120,p.y+120,()=>Click("memory"));}},pastTalk?"S3_PAST_LOOP":"S3_PAST");return;}
            if(!IntroDone){app.Dialogue.Play(()=>IntroDone=true,"S3_INTRO_B","S3_INTRO_COMMON");return;}
            string sequence=SecretUnlocked?"S3_LOOP_C":Updated?(p1Talk?"S3_LOOP_B":"S3_P1_OK"):"S3_LOOP_A";
            app.Dialogue.Play(()=>{if(sequence=="S3_P1_OK")CompleteP1Talk();},sequence);
        }
        public void OpenPieces()
        {
            if(Past||Blocked)return;if(!FakeWindow.TryCreate(app.Windows,"s3:pieces","조각들",out var w,100,190,610,235))return;w.SetFileBackground();
            for(int i=0;i<Data.cards.Length;i++){int index=Data.cardDisplayOrder[i];var c=Data.cards[index];FakeIcon.Create(w.Content,"s3:card:"+c.id,c.displayName,"picture",15+i*140,15,()=>OpenCard(index));}
        }
        public void OpenCard(int index)
        {
            var c=Data.cards[index];string key="s3:card:"+c.realName;
            if(!FakeWindow.TryCreate(app.Windows,key,c.displayName,out var w,245,160,610,470))return;
            var picture=UIFactory.Panel(w.Content,"무늬 그림",0,0,590,410,new Color(.98f,.96f,.9f));
            var suit=UIFactory.Rect(picture.transform,c.symbol,120,65,230,250).gameObject.AddComponent<CardSuitGraphic>();suit.Kind=index;suit.color=index==0||index==3?new Color(.7f,.05f,.08f):Color.black;suit.raycastTarget=false;
            Hidden.Add(new HiddenObject{id=key,filter=Parse(c.filter),owner=picture.rectTransform,bounds=new Rect(c.x,c.y,90,70),text=Puzzle.Password[index].ToString(),size=52});
        }
        void CompleteP1Talk(){p1Talk=true;app.Save.Flag("stage03.p1Talk");}
        public void OpenFilm()
        {
            if(Past||Blocked||!IntroDone)return;
            if(Updated&&!p1Talk){app.Dialogue.Play(()=>{CompleteP1Talk();if(!Past)lens=FilmLensWindow.Open(this);},"S3_FILM_FIRST","S3_P1_OK");return;}
            lens=FilmLensWindow.Open(this);
        }
        public void OpenSecret()
        {
            if(Past||Blocked)return;if(!SecretUnlocked){PasswordDialog.Open(this);return;}
            if(!FakeWindow.TryCreate(app.Windows,"s3:secret","비밀",out var w,220,200,610,245))return;w.SetFileBackground(35);
            var area=UIFactory.Rect(w.Content,"파일 목록",0,35,590,155);
            void Draw()
            {
                UIFactory.Clear(area);int index=0;
                foreach(var file in Data.secretFiles)
                {
                    if(!file.IsVisible(w.ShowHidden))continue;
                    var record=file;FakeIcon.Create(area,file.id,file.name,file.spriteKey,30+index*155,20,()=>OpenSecretFile(record),false,file.hidden);index++;
                }
            }
            w.AddHiddenToggle(Draw);Draw();
        }
        void OpenSecretFile(FileRecord file)
        {
            if(Past||Blocked||!SecretUnlocked||!file.IsVisible(app.Save.Hidden("s3:secret")))return;
            if(file.id=="s3:door")manager.RequestClear("door.exe");
            else if(file.id=="s3:past")OpenPastImage();
        }

        public bool SubmitPassword(string value)
        {
            if(Blocked||value!=Puzzle.Password||!Updated)return false;SecretUnlocked=true;if(lockMark)lockMark.SetActive(false);app.Audio.Play(Data.unlock);return true;
        }
        public void OpenPastImage()
        {
            if(Past||Blocked||!SecretUnlocked)return;
            var file=Array.Find(Data.secretFiles,f=>f.id=="s3:past");if(file==null||!file.IsVisible(app.Save.Hidden("s3:secret")))return;
            var viewer=ImageViewer.Open(new ViewerDefinition{id="s3:past",title="과거.png",imageKey=Data.pastImage,zoomInLevels=4,zoomOutLevels=0});
            if(!viewer)return;
            if(!PastPngOpened){PastPngOpened=true;app.Save.Flag("stage03.pastPngOpened");}
            TryPast();
            if(Hidden.Exists(h=>h.owner==viewer.PictureRoot))return;
            foreach(var clue in Data.dateParts)Hidden.Add(new HiddenObject{id="date:"+clue.filter,owner=viewer.PictureRoot,filter=Parse(clue.filter),bounds=new Rect(clue.x,clue.y,125,50),text=clue.text,size=12});
        }
        public void ApplyDate(DateTime date){if(Blocked||Past)return;app.DisplayDate=date;TryPast();}
        void TryPast()
        {
            var date=app.DisplayDate??DateTime.Now;
            if(Blocked||Past||PastDone||!PastPngOpened||date.Year!=Data.secretYear||date.Month!=Data.secretMonth||date.Day!=Data.secretDay)return;
            Past=true;
            // Keep the date-clue viewer usable while closing windows owned by hidden desktop icons.
            var viewer=Array.Find(app.Windows.GetComponentsInChildren<ImageViewer>(),v=>v.Definition.id=="s3:past");
            if(viewer){foreach(var window in app.Windows.GetComponentsInChildren<FakeWindow>())if(window!=viewer.Window)window.Close();UIFactory.Clear(app.DragLayer);}
            else app.CloseAllWindows();
            SetPastDesktop(true);StoryEffects.Get().Sepia(true,Data.warp);ReplaceNpc(true);
        }
        // Runtime-only state: recreated on stage restart; icons and folder contents are never deleted.
        readonly Dictionary<string,(FakeIcon icon,float alpha,bool interactable,bool raycasts)> hiddenInPast=new();
        public int HiddenInPastCount=>hiddenInPast.Count;
        void SetPastDesktop(bool past)
        {
            if(past)
            {
                foreach(var item in Data.layout)
                {
                    if((item.kind!="folder"&&item.kind!="executable")||Array.IndexOf(Data.pastIconExceptions,item.id)>=0||!icons.TryGetValue(item.id,out var icon)||!icon.gameObject.activeSelf)continue;
                    var group=icon.GetComponent<CanvasGroup>()??icon.gameObject.AddComponent<CanvasGroup>();
                    hiddenInPast[item.id]=(icon,group.alpha,group.interactable,group.blocksRaycasts);
                    group.interactable=false;group.blocksRaycasts=false;StartCoroutine(FadeDesktopIcon(icon,group,group.alpha,0,true));
                }
            }
            else
            {
                foreach(var saved in hiddenInPast.Values)
                {
                    if(!saved.icon)continue;var group=saved.icon.GetComponent<CanvasGroup>();saved.icon.gameObject.SetActive(true);
                    group.interactable=saved.interactable;group.blocksRaycasts=saved.raycasts;StartCoroutine(FadeDesktopIcon(saved.icon,group,0,saved.alpha,false));
                }
                hiddenInPast.Clear();
            }
        }
        IEnumerator FadeDesktopIcon(FakeIcon icon,CanvasGroup group,float from,float to,bool hide)
        {
            for(float t=0;t<.2f;t+=Time.unscaledDeltaTime){if(!icon)yield break;group.alpha=Mathf.Lerp(from,to,t/.2f);yield return null;}
            if(!icon)yield break;group.alpha=to;if(hide)icon.gameObject.SetActive(false);
        }
        void ReplaceNpc(bool young)
        {
            var old=icons["geurim"];var p=old.Rect.anchoredPosition;old.gameObject.SetActive(false);Destroy(old.gameObject);
            icons["geurim"]=FakeIcon.Create(app.ScreenRoot,"geurim",young?"어린그림":"그림",young?Data.youngNpc:Data.npc,p.x,-p.y,Talk);
        }
        void SetupHidden()
        {
            for(int i=0;i<Data.trail.points.Length;i++)
            {
                int n=i;var p=Data.trail.points[i];Hidden.Add(new HiddenObject{id="trail:"+i,filter=LensFilter.Neutral,bounds=new Rect(p.x-30,p.y-30,60,60),rotation=Data.trail.Angle(i),text=i==0?"×":null,sprite=i==0?null:(i%2==0?"s3_foot_right":"s3_foot_left"),size=42,tint=Color.red,visibility=()=>n<=TrailStep?1:n==TrailStep+1?TrailProgress:0});
            }
            var end=Data.trail.goal;
            Hidden.Add(new HiddenObject{id="patch",filter=LensFilter.Neutral,bounds=new Rect(end.x-45,end.y-25,90,65),sprite="door",text="F_patch.exe",size=15,visibility=()=>TrailStep>=Data.trail.points.Length&&!PatchFound?1:0,clicked=RevealPatch});
            for(int i=0;i<4;i++){int n=i;Hidden.Add(new HiddenObject{id="name:"+i,bounds=Puzzle.Names[i],text=Data.cards[i].realName,filter=Parse(Data.cards[i].filter),visibility=()=>Updated?1:0});}
        }
        public static LensFilter Parse(string value)=>(LensFilter)Enum.Parse(typeof(LensFilter),value);
        public void Track(FilmLensWindow current,bool still,float dt)
        {
            if(Blocked||Updated||TrailStep>=Data.trail.points.Length)return;
            var target=Data.trail.points[TrailStep];bool held=still&&current.Filter==LensFilter.Neutral&&current.Contains(target);
            float hold=TrailStep==0?Data.trail.startHold:Data.trail.holdSeconds;
            TrailProgress=Mathf.Clamp01(TrailProgress+(held?dt/hold:-dt*Data.trail.decayPerSecond));
            if(TrailProgress>=1){TrailStep++;TrailProgress=0;app.Audio.Play(Data.footstep);}
        }
        public void RevealPatch()
        {
            if(Blocked||PatchFound||TrailStep<Data.trail.points.Length)return;PatchFound=true;var p=Data.trail.goal;
            icons["patch"]=FakeIcon.Create(app.ScreenRoot,"patch","F_patch.exe","door",p.x-45,p.y-25,()=>Click("patch"));
        }
        IEnumerator Install()
        {
            if(!PatchFound||Updated)yield break;Busy=true;
            yield return manager.InstallProgram("F_patch.exe",Data.installSeconds,Data.installTick,Data.ding);
            if(lens)lens.Window.Close();icons["patch"].gameObject.SetActive(false);Destroy(icons["patch"].gameObject);icons.Remove("patch");
            var old=icons["film"];var p=old.Rect.anchoredPosition;old.gameObject.SetActive(false);Destroy(old.gameObject);
            Updated=true;icons["film"]=FakeIcon.Create(app.ScreenRoot,"film",Data.updatedFilmName,"s3_program",p.x,-p.y,()=>Click("film"));Busy=false;
        }
        bool cursorHidden,cursorWasActive;
        void RestoreCursor(){if(cursorHidden&&app&&app.CursorLayer)app.CursorLayer.gameObject.SetActive(cursorWasActive);cursorHidden=false;}
        IEnumerator Memory()
        {
            Busy=true;cursorWasActive=app.CursorLayer.gameObject.activeSelf;cursorHidden=true;app.CursorLayer.gameObject.SetActive(false);app.CloseAllWindows();app.Audio.PauseEventMusic();
            var blackout=UIFactory.Panel(app.Modal,"기억 입력 잠금",0,0,1024,768,Color.black);
            app.Audio.Music(Data.murmur);app.Audio.SetMusicFade(0);
            var image=UIFactory.Sprite(blackout.transform,app.Catalog.sprites.Get(Data.visionImage),100,90,824,588);image.color=new Color(1,1,1,0);
            visionMaterial=new Material(Resources.Load<Material>("MemoryBlur"));image.material=visionMaterial;
            float duration=Mathf.Max(15,Data.visionSeconds);
            for(float t=0;t<duration;t+=Time.unscaledDeltaTime)
            {
                float progress=t/duration;app.Audio.SetMusicFade(Mathf.Min(1,t/5)*Mathf.Clamp01((duration-t)/2));
                image.color=new Color(1,1,1,Mathf.Clamp01(t/4)*Mathf.Clamp01((duration-t)/2));image.transform.localScale=Vector3.one*Mathf.Lerp(.7f,1.2f,progress);visionMaterial.SetFloat("_Blur",Mathf.Lerp(18,0,progress));yield return null;
            }
            app.DisplayDate=null;app.Catalog.crt.sepiaAmount=0;app.Catalog.crt.UpdateStory();app.Audio.StopDateEffect();Past=false;PastDone=true;ReplaceNpc(false);
            if(icons.TryGetValue("memory",out var memory)){memory.gameObject.SetActive(false);Destroy(memory.gameObject);icons.Remove("memory");}
            app.Save.Flag("stage03.pastSolved");Destroy(visionMaterial);visionMaterial=null;app.Audio.Music(Data.music);app.Audio.SetMusicFade(0);
            for(float t=0;t<1;t+=Time.unscaledDeltaTime){app.Audio.SetMusicFade(t);yield return null;}app.Audio.SetMusicFade(1);Destroy(blackout.gameObject);Busy=false;RestoreCursor();SetPastDesktop(false);
        }
        void Update()
        {
            Hidden.RemoveAll(h=>h.hadOwner&&!h.owner);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(Keyboard.current!=null&&Keyboard.current.f1Key.wasPressedThisFrame&&!Blocked)
            {if(debug){Destroy(debug.gameObject);debug=null;}else{debug=UIFactory.Text(app.Modal,"QA 암호: "+Puzzle.Password+"\n숨김 이름: "+string.Join(" / ",Puzzle.Names),120,44,830,100,18,Color.magenta);}}
            #endif
        }
        void OnDestroy(){RestoreCursor();if(visionMaterial)Destroy(visionMaterial);if(debug)Destroy(debug.gameObject);}
    }
    public sealed class Stage03NpcPop:MonoBehaviour
    {
        IEnumerator Start(){for(float t=0;t<.35f;t+=Time.unscaledDeltaTime){transform.localScale=Vector3.one*(1+Mathf.Sin(t/.35f*Mathf.PI)*.15f);yield return null;}transform.localScale=Vector3.one;Destroy(this);}
    }
}
