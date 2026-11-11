using System;
using System.Collections;
using UnityEngine;
namespace CoComputar
{
    public sealed class StageManager : MonoBehaviour
    {
        public StageDefinition Current { get; private set; }
        public StageState State { get; private set; }
        public event Action<StageState> StateChanged;
        public SortingModel Sorting { get; private set; }
        RectTransform root; FakeCursor cursor; FakeIcon puzzleIcon, mazeIcon, toxicIcon;
        bool desktopHidden => GameApp.Instance.Save.Hidden("desktop");
        bool sideUnlocked;
        FolderDocument tree;
        public void Enter(string id,RectTransform screen,FakeCursor pointer)
        {
            var app=GameApp.Instance; Current=app.Catalog.Stage(id) ?? app.Catalog.stages[0]; root=screen; cursor=pointer;
            app.Save.Enter(Current.stageId); app.ResetScreen(); sideUnlocked=false;
            SetState(StageState.Intro);
            if(Current.entryPrefab)
            {
                app.Desktop();cursor.SetHanging(true);
                var entry=Instantiate(Current.entryPrefab,root);
                foreach(var behaviour in entry.GetComponentsInChildren<MonoBehaviour>()) if(behaviour is IStageEntry stage) stage.Enter(this,Current);
                return;
            }
            if(!Current.dialogue)
            {
                app.Desktop(); UIFactory.Text(root,Current.title,300,300,650,90,42);
                UIFactory.Text(root,"연결되었습니다.\n다음 기억은 아직 복구되지 않았습니다.",300,410,650,100,25);
                UIFactory.Button(root,"타이틀로",385,580,250,50,app.ShowTitle); cursor.SetHanging(true); return;
            }
            app.Dialogue.Load(Current.dialogue); Sorting=new SortingModel(JsonUtility.FromJson<FileDocument>(Current.files.text).files);
            tree=JsonUtility.FromJson<FolderDocument>(Current.folderTree.text);
            if(Current.hasPrologue && !app.Save.Progress.flags.Contains("stage01.wandered"))
                gameObject.AddComponent<PrologueController>().Setup(root,()=>{app.Save.Flag("stage01.wandered");StartDesktop();});
            else StartDesktop();
        }
        void SetState(StageState state) { State=state; StateChanged?.Invoke(state); }
        void StartDesktop()
        {
            var app=GameApp.Instance; app.Desktop(); app.Audio.Music("bgm_stage01"); cursor.SetHanging(true); SetState(StageState.Intro);
            FakeIcon.Create(root,"npc","폴더군","folder",140,204,Talk);
            puzzleIcon=FakeIcon.Create(root,"sort","작업 폴더","folder",265,204,()=>{if(State!=StageState.Intro) OpenSorting();});
            puzzleIcon.gameObject.AddComponent<CanvasGroup>().alpha=.4f;
            UIFactory.Text(root,"MEMORY / 01",720,684,280,36,21,new Color(.12f,.35f,.56f));
            UIFactory.Text(root,"폴더군이 당신을 기다리고 있습니다.",265,340,640,36,22,new Color(.1f,.32f,.5f));
        }
        public void Talk()
        {
            var dialogue=GameApp.Instance.Dialogue; if(dialogue.Active) return;
            switch(State)
            {
                case StageState.Intro:
                    dialogue.Play(()=>{SetState(StageState.SortNormal); puzzleIcon.GetComponent<CanvasGroup>().alpha=1;},"S1_INTRO"); break;
                case StageState.SortNormal:
                    bool first=Sorting.IsCorrect(false);
                    dialogue.Play(()=>{if(first) {SetState(StageState.SortHidden); RefreshSorting();}},"S1_CHECK1",first?"S1_CHECK1_OK":"S1_CHECK1_FAIL"); break;
                case StageState.SortHidden:
                    bool second=Sorting.IsCorrect(true);
                    if(second) dialogue.Play(()=>{SetState(StageState.Maze); sideUnlocked=true; RestoreDesktopHidden(); mazeIcon=FakeIcon.Create(root,"1989","1989년","folder",390,204,()=>OpenMaze(tree.root,0));},"S1_CHECK2","S1_CHECK2_OK","S1_OUTRO");
                    else dialogue.Play(()=>{},"S1_CHECK2","S1_CHECK2_FAIL"); break;
                case StageState.Maze: WindowApps.Note("폴더군","오래된 폴더를 차근차근 살펴보시지요."); break;
            }
        }
        FakeWindow sortingWindow;
        RectTransform sortingArea;
        public void OpenSorting()
        {
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"sorting","작업 폴더 — File Explorer",out sortingWindow,225,80,780,625)) return;
            sortingWindow.SetFileBackground(38);
            sortingArea=UIFactory.Rect(sortingWindow.Content,"파일 영역",0,38,760,525);
            if(State==StageState.SortHidden || State==StageState.Maze) sortingWindow.AddHiddenToggle(RefreshSorting);
            sortingWindow.Closed=()=>{sortingWindow=null;sortingArea=null;}; RefreshSorting();
        }
        void RefreshSorting()
        {
            if(!sortingWindow) return;
            // Toolbar placement must not affect puzzle state. Add the toggle in place;
            // recreating the window here recursively re-entered RefreshSorting.
            if((State==StageState.SortHidden || State==StageState.Maze) && !sortingWindow.HasHiddenToggle)
                sortingWindow.AddHiddenToggle(RefreshSorting);
            UIFactory.Clear(sortingArea);
            UIFactory.Panel(sortingArea,"탐색기 배경",0,0,760,525,GameApp.Instance.Catalog.theme.paper);
            string[] categories={"document","picture","music"}, names={"문서함","사진첩","음악"};
            for(int i=0;i<3;i++)
            {
                string category=categories[i]; string name=names[i];
                var folder=FakeIcon.Create(sortingArea,category,name,"folder",30+i*210,10,()=>OpenCategory(category,name));
                var folderDrop=folder.gameObject.AddComponent<DropTarget>();folderDrop.Category=category;
                folderDrop.Received=()=>{
                    foreach(var opened in GameApp.Instance.Windows.GetComponentsInChildren<FakeWindow>())
                        if(opened.Key=="category:"+category)opened.Content.GetComponent<DropTarget>()?.Received?.Invoke();
                };
            }
            int n=0;
            foreach(var file in Sorting.Files)
            {
                if(Sorting.Location(file.id)!="" || !file.IsVisible(sortingWindow.ShowHidden)) continue;
                var record=file;
                var icon=FakeIcon.Create(sortingArea,file.id,file.name,file.spriteKey,22+(n%5)*140,135+(n/5)*115,()=>Preview(record),true,file.hidden);
                icon.AllowWindowDrops=true;
                icon.Dropped=(item,target)=>{Sorting.Move(item.Id,target.Category); RefreshSorting();target.Received?.Invoke();}; n++;
            }
            UIFactory.Text(sortingArea,"파일을 폴더 아이콘 또는 열린 폴더 창 안으로 드래그하세요.",10,487,740,32,16);
            UIFactory.Sprite(sortingArea,GameApp.Instance.Catalog.sprites.Get("tree"),615,390,50,70);
            UIFactory.Sprite(sortingArea,GameApp.Instance.Catalog.sprites.Get("tree"),680,420,50,70);
        }
        void OpenCategory(string category,string name)
        {
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"category:"+category,name,out var w,285,150,640,480)) return;
            w.SetFileBackground(45);
            var dropSurface=w.Content.gameObject.AddComponent<UnityEngine.UI.Image>();dropSurface.color=Color.clear;
            var windowDrop=w.Content.gameObject.AddComponent<DropTarget>();windowDrop.Category=category;windowDrop.IsFolderWindow=true;
            var area=UIFactory.Rect(w.Content,"저장된 파일",0,45,620,365);
            void Draw()
            {
                UIFactory.Clear(area);int n=0;
                UIFactory.Text(area,"다른 폴더 창 또는 작업 폴더로 드래그하여 이동",15,0,580,35,20);
                foreach(var file in Sorting.Files)
                {
                    if(Sorting.Location(file.id)!=category || !file.IsVisible(w.ShowHidden)) continue;
                    var record=file;
                    var icon=FakeIcon.Create(area,file.id,file.name,file.spriteKey,20+n%5*115,45+n/5*105,()=>Preview(record),true,file.hidden);
                    icon.DroppedOutside=e=>{
                        var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                        UnityEngine.EventSystems.EventSystem.current.RaycastAll(e,hits);
                        var top=hits.Find(h=>h.gameObject!=icon.gameObject && !h.gameObject.transform.IsChildOf(icon.transform));
                        if(!top.gameObject)return;
                        var target=top.gameObject.GetComponentInParent<DropTarget>();
                        bool folderWindow=target && target.IsFolderWindow;
                        bool folderIcon=target && sortingArea && target.transform.IsChildOf(sortingArea);
                        if(folderWindow || folderIcon)
                        {
                            if(target.Category==category)return;
                            Sorting.Move(record.id,target.Category);Draw();RefreshSorting();target.Received?.Invoke();return;
                        }
                        if(!sortingWindow || !sortingArea || !top.gameObject.transform.IsChildOf(sortingArea))return;
                        Sorting.Move(record.id,"");Draw();RefreshSorting();
                    }; n++;
                }
            }
            windowDrop.Received=Draw;
            if(State==StageState.SortHidden || State==StageState.Maze) w.AddHiddenToggle(Draw);Draw();
        }
        public static void Preview(FileRecord record)
        {
            if(record.category=="document") WindowApps.Note(record.name,record.content);
            else if(record.category=="picture") WindowApps.Picture(record.name,record.imageKey);
            else WindowApps.Alert("재생할 수 없습니다");
        }
        public void ToggleDesktopHidden()
        {
            if(!sideUnlocked) { WindowApps.Note("설정","아직 사용할 수 있는 설정이 없습니다."); return; }
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"desktop-settings","바탕화면 설정",out var w,180,48,370,155)) return;
            var toggle=UIFactory.Button(w.Content,"숨김파일 표시",15,20,320,45,()=>{
                GameApp.Instance.Save.SetHidden("desktop",!desktopHidden); w.Close(); RestoreDesktopHidden();
            });
            if(desktopHidden) UIFactory.CheckMark(toggle.transform,275,8);
        }
        void OpenToxic()
        {
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"toxic","유해물질",out var w,170,115,500,320)) return;
            w.SetFileBackground();
            FakeIcon.Create(w.Content,"inside","남겨진 것","folder",35,40,()=>gameObject.AddComponent<MailPuzzle>().Open());
        }
        void RestoreDesktopHidden()
        {
            if(toxicIcon) {toxicIcon.gameObject.SetActive(false);Destroy(toxicIcon.gameObject);}
            if(desktopHidden) toxicIcon=FakeIcon.Create(root,"toxic","유해물질","folder",30,350,()=>OpenToxic(),false,true);
        }
        void OpenMaze(string id,int depth)
        {
            var node=Array.Find(tree.nodes,n=>n.id==id); if(node==null) return;
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"maze:"+id,node.name,out var w,210+depth*28%180,90+depth*35%210,580,365)) return;
            w.SetFileBackground();
            if(node.exit) FakeIcon.Create(w.Content,"door.exe","door.exe","door",230,55,()=>RequestClear("door.exe"));
            else if(node.children!=null && node.children.Length>0)
            {
                for(int i=0;i<node.children.Length;i++)
                {
                    string child=node.children[i]; var next=Array.Find(tree.nodes,n=>n.id==child);
                    FakeIcon.Create(w.Content,child,next.name,"folder",35+i%4*125,45+i/4*110,()=>OpenMaze(child,depth+1));
                }
            }
            else UIFactory.Text(w.Content,"이 폴더는 비어 있습니다.",40,80,470,60,24);
        }
        public void RequestClear(string condition)
        {
            if(Current && condition==Current.clearCondition && State!=StageState.Transition && !string.IsNullOrEmpty(Current.nextStageId)) StartCoroutine(Transition());
        }
        public IEnumerator InstallProgram(string title,float seconds,string tick,string done)
        {
            var app=GameApp.Instance;var blocker=UIFactory.Panel(app.Modal,"설치 입력 잠금",0,0,1024,768,Color.clear);
            var console=ConsolePanel(blocker.transform,title,260,230,510,240);
            var text=UIFactory.Text(console,"설치중… 0%",20,60,460,130,25,Color.white);
            for(int i=0;i<=20;i++){text.text="설치중… "+i*5+"%";app.Audio.PlayPuzzle(tick);yield return new WaitForSecondsRealtime(seconds/20);}
            text.text="설치 완료";app.Audio.Play(done);yield return new WaitForSecondsRealtime(.65f);Destroy(blocker.gameObject);
        }
        static RectTransform ConsolePanel(Transform parent,string title,float x,float y,float width,float height)
        {
            var w=UIFactory.Panel(parent,"CMD",x,y,width,height,Color.black);
            w.gameObject.AddComponent<NeoFontScope>();
            UIFactory.Panel(w.transform,"표시줄",0,0,width,30,new Color(.15f,.05f,.19f));
            UIFactory.Text(w.transform,title,12,1,width-30,30,18,Color.white);return w.rectTransform;
        }
        public static float LoadingInterval(int line) => .28f*Mathf.Pow(.025f/.28f,Mathf.Clamp01(line/41f));
        public IEnumerator ConsoleSequence(Transform parent,bool reverse,float interval)
        {
            var app=GameApp.Instance;
            var panel=ConsolePanel(parent,"명령 프롬프트",40,65,944,590);
            var text=UIFactory.Text(panel,"",18,40,900,540,22,Color.white);
            if(reverse)
            {
                for(int remaining=42;remaining>=0;remaining--)
                {
                    text.text=string.Join("\n",System.Linq.Enumerable.Repeat("system loading",remaining));
                    yield return new WaitForSecondsRealtime(Mathf.Max(.001f,interval));
                }
            }
            else
            {
                for(int i=0;i<=100;i++){text.text=$"loading...  {i}%";yield return new WaitForSecondsRealtime(.022f);}
                text.text="system load success";app.Audio.Play("sfx_ding");yield return new WaitForSecondsRealtime(.45f);
                for(int i=0;i<42;i++){text.text+="\nsystem loading";app.Audio.Play("sfx_ding");yield return new WaitForSecondsRealtime(LoadingInterval(i));}
            }
            Destroy(panel.gameObject);
        }
        IEnumerator Transition()
        {
            SetState(StageState.Transition); var app=GameApp.Instance;
            var blocker=UIFactory.Panel(app.Modal,"전환 입력 잠금",0,0,1024,768,Color.clear);
            blocker.gameObject.AddComponent<NeoFontScope>();
            var w=ConsolePanel(blocker.transform,"명령 프롬프트",40,65,944,590);
            var progress=UIFactory.Text(w.transform,"loading...  00%",18,40,870,40,24,Color.white);
            for(int i=0;i<=100;i++) {progress.text=$"loading...  {i}%";yield return new WaitForSeconds(.022f);}
            var text=UIFactory.Text(blocker.transform,"system load success",58,155,940,1100,22,Color.white);
            app.Audio.Play("sfx_ding"); yield return new WaitForSeconds(.45f);
            for(int i=0;i<42;i++) {text.text+="\nsystem loading";app.Audio.Play("sfx_ding");yield return new WaitForSeconds(LoadingInterval(i));}
            UIFactory.Panel(blocker.transform,"暗",0,0,1024,768,Color.black);
            app.Save.Complete(Current.stageId,Current.nextStageId); yield return new WaitForSeconds(1);
            Enter(Current.nextStageId,root,cursor);
        }
    }
    public interface IStageEntry { void Enter(StageManager manager,StageDefinition definition); }
}
