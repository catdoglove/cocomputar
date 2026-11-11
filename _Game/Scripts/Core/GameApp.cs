using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed class GameApp : MonoBehaviour
    {
        public static GameApp Instance { get; private set; }
        public GameCatalog Catalog;
        public SaveService Save { get; private set; }
        public AudioService Audio { get; private set; }
        public RectTransform Root, Windows, Modal, CursorLayer, DragLayer;
        public DialogueSystem Dialogue { get; private set; }
        public StageManager Stages { get; private set; }
        public Camera UICamera;
        public bool CinematicActive => GetComponent<DoorCaptureSequence>()?.Active==true;
        RectTransform screen;
        FakeCursor cursor;InputSystemUIInputModule uiInput;InputActionReference fileRightClick;
        bool title;
        public bool IsTitleScreen => title;
        int selected;
        TextMeshProUGUI[] menuLabels;
        float heartbeat;
        TextMeshProUGUI desktopClock;
        public System.Action ClockClicked, DesktopSettingsClicked;
        public System.Func<bool> ComputerBlocked;
        System.DateTime? displayDate;System.DateTime displayDateAnchor;
        public System.DateTime? DisplayDate
        {
            get=>displayDate.HasValue?displayDate.Value+(System.DateTime.UtcNow-displayDateAnchor):(System.DateTime?)null;
            set{displayDate=value;displayDateAnchor=System.DateTime.UtcNow;}
        }
        void Awake()
        {
            Instance=this; UIFactory.Theme=Catalog.theme;
            Save=new SaveService(); Audio=gameObject.AddComponent<AudioService>(); Audio.Setup(Catalog);
            var canvasGo=new GameObject("화면",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=canvasGo.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=UICamera; canvas.planeDistance=1;
            var scaler=canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1024,768); scaler.matchWidthOrHeight=.5f;
            Root=UIFactory.Rect(canvasGo.transform,"게임",0,0,1024,768);
            screen=UIFactory.Rect(Root,"배경과 게임",0,0,1024,768);
            Windows=UIFactory.Rect(Root,"창",0,0,1024,768);
            Modal=UIFactory.Rect(Root,"대화와 모달",0,0,1024,768);
            DragLayer=UIFactory.Rect(Root,"드래그 아이콘",0,38,1024,730);
            DragLayer.gameObject.AddComponent<RectMask2D>();
            CursorLayer=UIFactory.Rect(Root,"커서",0,0,1024,768);
            var eventGo=new GameObject("입력",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var input=eventGo.GetComponent<InputSystemUIInputModule>();input.AssignDefaultActions();uiInput=input;fileRightClick=input.rightClick;input.rightClick=null;
            Dialogue=gameObject.AddComponent<DialogueSystem>(); Stages=gameObject.AddComponent<StageManager>();
            cursor=gameObject.AddComponent<FakeCursor>(); cursor.Setup(CursorLayer);
        }
        void Start()
        {
            ApplyOptions(); ShowTitle();
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-coQA")>=0) gameObject.AddComponent<PlaythroughQA>();
            else if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-coEcgQA")>=0) gameObject.AddComponent<ECGQA>();
            #endif
        }
        public void ApplyOptions() { Audio.Apply(Save.Options); Catalog.crt.Apply(Save.Options.filter,Save.Options.curvature); }
        public void CloseAllWindows()
        {
            if(EventSystem.current && EventSystem.current.currentSelectedGameObject && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(Windows))
                EventSystem.current.SetSelectedGameObject(null);
            // 각 창의 종료 콜백으로 소유자의 참조와 실행 중인 작업도 정리한다.
            foreach(var window in Windows.GetComponentsInChildren<FakeWindow>())window.Close();
            foreach(var panel in Windows.GetComponentsInChildren<TitleMenuPanel>())panel.Close();
            UIFactory.Clear(Windows);UIFactory.Clear(DragLayer);
        }
        public void SetFileRightClick(bool enabled){uiInput.rightClick=enabled?fileRightClick:null;}
        public void ResetScreen()
        {
            CloseAllWindows();
            SetFileRightClick(false);ClockClicked=null;DesktopSettingsClicked=null;ComputerBlocked=null;DisplayDate=null;
            Dialogue.Cancel();
            GetComponent<StoryEffects>()?.ResetEffects();
            Audio.StopEffects();
            var exploration=GetComponent<PrologueController>();if(exploration) {exploration.enabled=false;Destroy(exploration);}
            title=false; StopAllCoroutines(); UIFactory.Clear(screen); UIFactory.Clear(Windows); UIFactory.Clear(Modal); UIFactory.Clear(DragLayer); cursor.SetHanging(false);
        }
        public void ShowTitle()
        {
            ResetScreen(); title=true; selected=0; Audio.Music("bgm_title");
            EventSystem.current.SetSelectedGameObject(null);
            UIFactory.Panel(screen,"검은 화면",0,0,1024,768,new Color(.009f,.02f,.012f));
            UIFactory.Text(screen,"PERSONAL SYSTEM  /  MEMORY TERMINAL",75,62,880,28,16,new Color(.28f,.55f,.33f));
            var logo=UIFactory.Text(screen,"<color=#FF4FD8>Co-</color><color=#4DFF4D>computar.</color>",115,180,850,100,78); logo.alignment=TextAlignmentOptions.Center;
            var ecg=UIFactory.Rect(screen,"심전도",50,327,924,80).gameObject.AddComponent<ECGGraphic>();
            ecg.Setup(Catalog.theme.green,Catalog.theme.ecgSweepSeconds);
            string[] labels={"Start","Option","Turn off"}; menuLabels=new TextMeshProUGUI[3];
            for(int i=0;i<3;i++)
            {
                int index=i; var button=UIFactory.Button(screen,labels[i],280,415+i*65,410,52,()=>TitleAction(index),Color.clear);
                button.navigation=new Navigation{mode=Navigation.Mode.None}; button.GetComponent<Outline>().enabled=false;
                var trigger=button.gameObject.AddComponent<EventTrigger>();
                var hover=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};
                hover.callback.AddListener(_=>{selected=index;UpdateSelection();});trigger.triggers.Add(hover);
                menuLabels[i]=button.GetComponentInChildren<TextMeshProUGUI>(); menuLabels[i].fontSize=34; menuLabels[i].alignment=TextAlignmentOptions.Left;
            }
            UIFactory.Text(screen,"↑ ↓  SELECT     ENTER  CONFIRM",75,692,700,28,16,new Color(.28f,.55f,.33f));
            UIFactory.Text(screen,"1989 / ONLINE",790,692,210,28,16,Catalog.theme.green); UpdateSelection();
        }
        void Update()
        {
            // 누적 시간이 아닌 시스템 현지 시각을 매번 읽는다.
            if(desktopClock) desktopClock.text=(DisplayDate??System.DateTime.Now).ToString("tt hh:mm",System.Globalization.CultureInfo.InvariantCulture);
            if(!title) return;
            heartbeat+=Time.unscaledDeltaTime;
            if(heartbeat>=2) { heartbeat=0; Audio.Play("sfx_heartbeat"); }
            if(Windows.childCount>0) return;
            var k=Keyboard.current; if(k==null) return;
            if(k.downArrowKey.wasPressedThisFrame) { selected=(selected+1)%3; UpdateSelection(); }
            if(k.upArrowKey.wasPressedThisFrame) { selected=(selected+2)%3; UpdateSelection(); }
            if(k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) TitleAction(selected);
        }
        void UpdateSelection()
        {
            string[] labels={"Start","Option","Turn off"};
            for(int i=0;i<3;i++) { menuLabels[i].text=(i==selected?"▶ ":"   ")+labels[i]; menuLabels[i].color=i==selected?Catalog.theme.pink:Catalog.theme.green; }
        }
        void TitleAction(int index)
        {
            selected=index; UpdateSelection();
            if(index==0)
            {
                var panel=TitleMenuPanel.Open(Windows,550,415,390,156);
                var first=UIFactory.Button(panel.Content,"게임 시작하기",18,16,354,52,()=>{
                    if(Save.Exists) ConfirmNewGame(); else Begin(true);
                });
                var b=UIFactory.Button(panel.Content,"이어하기",18,86,354,52,()=>Begin(false)); b.interactable=Save.Exists;
                panel.Style(first,showArrow:true);panel.Style(b,showArrow:true);
                panel.BindSpaceSelection(first,b);
                StartCoroutine(SelectNextFrame(first.gameObject));
            }
            else if(index==1) OpenOptions(); else Quit();
        }
        void ConfirmNewGame()
        {
            var panel=TitleMenuPanel.Open(Windows,550,395,430,240);
            UIFactory.Text(panel.Content,"NEW GAME",18,16,390,32,26,Catalog.theme.pink);
            UIFactory.Text(panel.Content,"기존 진행 기록을 지우고\n새로 시작하시겠습니까?",18,62,390,80,23,Catalog.theme.green);
            var yes=UIFactory.Button(panel.Content,"확인",18,165,170,48,()=>Begin(true));
            var no=UIFactory.Button(panel.Content,"취소",218,165,170,48,()=>TitleAction(0));
            panel.Style(yes);panel.Style(no);StartCoroutine(SelectNextFrame(no.gameObject));
        }
        public void OpenOptions()
        {
            TitleMenuPanel panel=null;Transform content;
            if(title) { panel=TitleMenuPanel.Open(Windows,550,480,430,250);content=panel.Content; }
            else { if(!FakeWindow.TryCreate(Windows,"options","Option",out var w,520,300,430,310)) return;content=w.Content; }
            string[] names={"배경음","효과음","화면 곡률","화면 효과"}; float[] values={Save.Options.music,Save.Options.effects,Save.Options.curvature,Save.Options.filter};
            for(int i=0;i<names.Length;i++)
            {
                int n=i; var label=UIFactory.Text(content,names[i],18,20+i*55,122,35,23);
                var slider=UIFactory.Slider(content,150,20+i*55,values[i],v=>{
                    if(n==0) Save.Options.music=v; else if(n==1) Save.Options.effects=v; else if(n==2) Save.Options.curvature=v; else Save.Options.filter=v;
                    ApplyOptions(); Save.SaveOptions();
                });
                if(panel) panel.Style(slider,label);
                if(i==0) StartCoroutine(SelectNextFrame(slider.gameObject));
            }
        }
        IEnumerator SelectNextFrame(GameObject target) {yield return null;if(target) EventSystem.current.SetSelectedGameObject(target);}
        public void Begin(bool fresh)
        {
            if(fresh) Save.NewGame(Catalog.stages[0].stageId);
            else {Save.UnlockDialogueSkip();Save.ResetStagePuzzles(Save.Progress.stageId);}
            Stages.Enter(Save.Progress.stageId,screen,cursor);
        }
        public void Desktop()
        {
            UIFactory.Clear(screen); var bg=UIFactory.Sprite(screen,Catalog.sprites.Get("desktop"),0,0,1024,768); bg.preserveAspect=false;
            if(Stages.Current && Stages.Current.desktopTheme)Stages.Current.desktopTheme.Draw(screen);
            UIFactory.Panel(screen,"메뉴바",0,0,1024,38,Catalog.theme.lavender);
            UIFactory.Button(screen,"열기",15,3,78,31,OpenComputer,Color.clear);
            UIFactory.Button(screen,"저장",98,3,78,31,()=>WindowApps.Alert("스테이지 진입·클리어 시 자동 저장됩니다."),Color.clear);
            UIFactory.Button(screen,"설정",181,3,78,31,()=>{if(DesktopSettingsClicked!=null)DesktopSettingsClicked();else Stages.ToggleDesktopHidden();},Color.clear);
            UIFactory.Button(screen,"옵션",264,3,78,31,OpenOptions,Color.clear);
            UIFactory.Button(screen,"도움말",347,3,100,31,()=>WindowApps.Note("도움말","프롤로그: WASD / 방향키 이동, SPACE 문 열기\n\n바탕화면: 클릭으로 열기, 드래그로 정리\n대화: 클릭 / SPACE\n\n폴더 속 파일을 창 밖의 작업 폴더로 드래그하면 꺼낼 수 있습니다. 바탕화면에는 놓을 수 없습니다."),Color.clear);
            desktopClock=UIFactory.Text(screen,System.DateTime.Now.ToString("tt hh:mm",System.Globalization.CultureInfo.InvariantCulture),842,2,177,36,25);
            desktopClock.name="시스템 시계";
            desktopClock.raycastTarget=true;
            desktopClock.gameObject.AddComponent<Button>().onClick.AddListener(()=>ClockClicked?.Invoke());
            FakeIcon.Create(screen,"computer","내 컴퓨터","computer",30,68,OpenComputer);
        }
        public void OpenComputer() { if(ComputerBlocked?.Invoke()==true)return; ShowComputer(); }
        void ShowComputer() => WindowApps.Note("내 컴퓨터","Co-computar.\nMemory terminal\n\n사용자: 알 수 없음\n접속: 유지됨");
        public RectTransform ScreenRoot => screen;
        public static bool Advance => (Keyboard.current!=null && Keyboard.current.spaceKey.wasPressedThisFrame) || (Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame);
        public static void Quit()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #else
            Application.Quit();
            #endif
        }
        void OnDestroy() { UnityEngine.Cursor.visible=true; }
    }
}
