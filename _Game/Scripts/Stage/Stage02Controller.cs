using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace CoComputar
{
    [Serializable] public class Stage02Data
    {
        public ViewerDefinition sunset,key,watering;
        public string outdoorNightImage="outdoor_night";
        public string outdoorImage="outdoor",music="bgm_stage02";
        public string[] junkImages,clouds;public int photoCloud=2;public float cloudCycleSeconds=9,rainSeconds=1.8f;
        public int secretYear=1989,secretMonth=6,secretDay=6;
        public string wateringFailureSound="sfx_water_fail",rainSound="sfx_rain";
    }
    // 퍼즐 진행은 진입마다 초기화하고, 첫 전달의 대사 분기는 저장 데이터로 관리한다.
    public sealed class Stage02Controller : MonoBehaviour,IStageEntry
    {
        GameApp app;StageManager manager;public Stage02Data Data{get;private set;}
        public string FirstKey=>app.Save.Progress.stage02FirstKey??"";
        public bool TalkedBeforeFirstKey=>app.Save.Progress.stage02TalkedBeforeFirstKey;
        bool npcTalked;
        public int BlackFlowerIndex{get;private set;}
        public int Phase{get;private set;}public bool WhiteTaken,BlackTaken,Rained,BouquetTaken,Sepia,ExitTaken;
        public bool OutdoorNight{get;private set;}
        public bool DateLocked=>Sepia||manager.State==StageState.Transition;
        public bool KeysDialogueCompleted{get;private set;}
        readonly List<GameObject> npcs=new();
        public bool KeysDelivered{get;private set;}bool dateEventStarted;
        public readonly bool[] TulipsPicked=new bool[4];
        public readonly bool[] Buds=new bool[4];public readonly DoorAssembly Assembly=new();
        public bool OptionalSolved=>app.Save.Progress.flags.Contains("stage02.tulipSolved");
        public bool PlantsGone=>BouquetTaken||(OptionalSolved&&Phase>=3);
        public RecycleFiles FileStore {get;}=new RecycleFiles();
        readonly Dictionary<string,Action> fileViews=new();RectTransform desktopFiles;DropTarget desktopDrop;
        readonly HashSet<string> consumed=new();readonly Dictionary<string,FakeIcon> items=new();
        public void Enter(StageManager stageManager,StageDefinition definition)
        {
            app=GameApp.Instance;manager=stageManager;BlackFlowerIndex=UnityEngine.Random.Range(0,Buds.Length);Data=JsonUtility.FromJson<Stage02Data>(Resources.Load<TextAsset>("Stage02Config").text);
            if(FirstKey!=""){npcTalked=true;Phase=2;WhiteTaken=FirstKey=="white";BlackTaken=FirstKey=="black";}
            desktopFiles=UIFactory.Rect(app.ScreenRoot,"복원된 파일",0,38,1024,730);
            var desktopSurface=UIFactory.Panel(app.ScreenRoot,"바탕화면 파일 드롭",0,38,1024,730,Color.clear);
            desktopSurface.transform.SetSiblingIndex(app.ScreenRoot.Find("메뉴바").GetSiblingIndex());
            desktopDrop=desktopSurface.gameObject.AddComponent<DropTarget>();desktopDrop.Category=RecycleFiles.Desktop;
            FileStore.Changed=RefreshFiles;
            FileStore.IsPast=()=>Sepia;
            Register("sunset","석양.jpg","picture",RecycleFiles.Collection,OpenSunset,false,"picture:"+Data.sunset.id);
            Register("outdoor","야외.gif","picture",RecycleFiles.Collection,OpenOutdoor,true,"s2:outdoor");
            for(int i=0;i<Data.junkImages.Length;i++){string key=Data.junkImages[i];Register("junk"+i,$"낙서{i+1}.jpg","picture",RecycleFiles.Trash,()=>WindowApps.Picture(key,key),false,"picture:"+key);}
            Register("watering","물뿌리개.bmp","watering",RecycleFiles.Trash,()=>ImageViewer.Open(Data.watering),true,"picture:"+Data.watering.id);
            app.Dialogue.Load(definition.dialogue);app.Audio.Music(Data.music);StoryEffects.Get();
            FakeIcon.Create(app.ScreenRoot,"landscapes","풍경수집","folder",30,190,OpenCollection).gameObject.AddComponent<DropTarget>().Category=RecycleFiles.Collection;
            FakeIcon.Create(app.ScreenRoot,"trash","휴지통","trash",30,312,OpenTrash).gameObject.AddComponent<DropTarget>().Category=RecycleFiles.Trash;
            foreach(var pair in new[]{("twin_black","흑",420f),("twin_white","백",510f)})
            {var npc=FakeIcon.Create(app.ScreenRoot,pair.Item1,pair.Item2,pair.Item1,pair.Item3,230,Talk);npc.gameObject.AddComponent<DropTarget>().Category="twins";npcs.Add(npc.gameObject);}
            app.ClockClicked=()=>DateTimeWindow.Open(ApplyDate,app.DisplayDate??DateTime.Now,()=>DateLocked);
            app.DesktopSettingsClicked=()=>{if(FakeWindow.TryCreate(app.Windows,"s2:desktop-settings","바탕화면 설정",out var w,180,48,370,155))w.AddHiddenToggle(()=>{},true);};
            var computer=Array.Find(app.ScreenRoot.GetComponentsInChildren<FakeIcon>(),i=>i.Id=="computer");computer.Clicked=()=>WindowApps.Alert("접근 허가 필요");
        }
        public void Talk()
        {
            if(app.Dialogue.Active||Sepia)return;
            app.Dialogue.Play(()=>{npcTalked=true;if(Phase==0)Phase=1;},!npcTalked?"S2_INTRO":RepeatDialogue());
        }
        public string RepeatDialogue()
        {
            if(FirstKey==""||TalkedBeforeFirstKey)return Phase<=1?"S2_P1_PRE":"S2_P2_PRE";
            return BranchDialogue(KeysDelivered?"repeatAfter":"repeatBefore");
        }
        string BranchDialogue(string moment)=>"dialogue.stage2.firstKey."+FirstKey+"."+(TalkedBeforeFirstKey?"afterTalk":"beforeTalk")+"."+moment;
        bool DeliverKey(string key)
        {
            if(KeysDelivered||FirstKey==key)return false;
            bool first=FirstKey=="";
            if(first){app.Save.Progress.stage02FirstKey=key;app.Save.Progress.stage02TalkedBeforeFirstKey=npcTalked;app.Save.SaveProgress();}
            Consume(key+".key");npcTalked=true;Phase=first?2:3;KeysDelivered=!first;
            string dialogue=FirstKey=="white"&&TalkedBeforeFirstKey?(first?"S2_P1_OK":"S2_P2_OK"):BranchDialogue(first?"deliver":"secondDeliver");
            app.Dialogue.Play(()=>{if(!first){SpawnItem("key.png","열쇠.png","picture",650,250,OpenKey,false);KeysDialogueCompleted=true;TryDateEvent();}},dialogue);return true;
        }
        void OnDestroy(){if(desktopDrop)Destroy(desktopDrop);FileStore.Changed=null;}
        void Register(string id,string name,string sprite,string location,Action open,bool hidden=false,params string[] windows)
        {FileStore.Add(new RecycleFile{Id=id,Name=name,Sprite=sprite,Location=location,Open=open,Hidden=hidden,WindowKeys=windows,BlockedInPast=id=="door.exe"});}
        void RefreshFiles()
        {
            if(desktopFiles)DrawFiles(desktopFiles,RecycleFiles.Desktop,true);
            foreach(var draw in new List<Action>(fileViews.Values))draw();
        }
        void DrawFiles(RectTransform area,string location,bool hidden)
        {
            if(!area)return;UIFactory.Clear(area);int n=0;
            foreach(var file in FileStore.Files)
            {
                if(file.Location!=location||(file.Hidden&&!hidden))continue;
                string id=file.Id;bool desktop=location==RecycleFiles.Desktop;
                var icon=FakeIcon.Create(area,id,file.Name,file.Sprite,desktop?650+n%3*110:20+n%3*125,desktop?140+n/3*115:10+n/3*105,()=>FileStore.Open(id),true);
                icon.DoubleClicked=()=>FileStore.Open(id);
                var group=icon.gameObject.AddComponent<CanvasGroup>();group.alpha=file.Hidden?(location==RecycleFiles.Trash?.3f:.5f):1;
                BindDrag(icon);n++;
            }
        }
        void OpenCollection(){OpenFileFolder(RecycleFiles.Collection,"풍경수집",120,95);}
        void OpenTrash(){OpenFileFolder(RecycleFiles.Trash,"휴지통",75,350);}
        void OpenFileFolder(string location,string title,float x,float y)
        {
            if(!FakeWindow.TryCreate(app.Windows,location,title,out var w,x,y,440,385))return;
            w.SetFileBackground(35);
            var surface=UIFactory.Panel(w.Content,"드롭 영역",0,35,420,290,Color.clear);
            surface.gameObject.AddComponent<DropTarget>().Category=location;
            var area=UIFactory.Rect(surface.transform,"파일",0,0,420,280);
            void Draw(){DrawFiles(area,location,w.ShowHidden);}
            fileViews[location]=Draw;w.Closed=()=>fileViews.Remove(location);w.AddHiddenToggle(Draw,true);Draw();
        }
        void OpenSunset()
        {
            if(!FileStore.CanUse("sunset"))return;
            var viewer=ImageViewer.Open(Data.sunset);if(WhiteTaken)viewer.RemoveRegion("white-key");
            viewer.HotspotClicked=id=>{if(id!="white-key"||WhiteTaken||!FileStore.CanUse("sunset"))return;WhiteTaken=true;viewer.RemoveRegion(id);SpawnItem("white.key","white.key","white_key",680,180);app.Audio.Play("sfx_key_get");};
        }
        public void OpenOutdoor(){if(FileStore.CanUse("outdoor"))OutdoorWindow.Open(this);}
        void OpenKey()
        {
            if(!FileStore.CanUse("key.png"))return;
            var viewer=ImageViewer.Open(Data.key);if(ExitTaken){viewer.Picture.enabled=false;viewer.RemoveRegion("exit-folder");}
            viewer.HotspotClicked=id=>{if(viewer.Level!=-Data.key.zoomOutLevels||ExitTaken)return;ExitTaken=true;viewer.Picture.enabled=false;viewer.RemoveRegion(id);SpawnItem("exit","나가는길","folder",820,320,OpenExit,false);};
        }
        public void TakeBlack()
        {
            if(BlackTaken||!Buds[BlackFlowerIndex]||!Rained||!FileStore.CanUse("outdoor"))return;BlackTaken=true;SpawnItem("black.key","black.key","black_key",790,180);app.Audio.Play("sfx_key_get");
        }
        public void PickTulip(int index)
        {
            if(index<0||index>=TulipsPicked.Length||TulipsPicked[index]||!Sepia||PlantsGone||!FileStore.CanUse("outdoor"))return;
            TulipsPicked[index]=true;app.Audio.Play("sfx_key_get");
            if(Array.Exists(TulipsPicked,picked=>!picked))return;
            BouquetTaken=true;SpawnItem("bouquet","꽃다발.pub","bouquet",680,450);ReturnFromPast();
        }
        // 복귀는 네 튤립 수집 완료 경로에서만 호출한다.
        void ReturnFromPast()
        {
            app.CloseAllWindows();
            app.DisplayDate=null;
            Sepia=false;foreach(var npc in npcs)if(npc)npc.SetActive(true);
            StoryEffects.Get().Sepia(false);app.Audio.StopDateEffect();app.Audio.ResumeEventMusic();OutdoorNight=true;
        }
        public void ApplyDate(DateTime value)
        {
            if(DateLocked)return;
            app.DisplayDate=value;TryDateEvent();
        }
        void TryDateEvent()
        {
            if(manager.State==StageState.Transition)return;
            var value=app.DisplayDate??DateTime.Now;
            if(!KeysDelivered||!KeysDialogueCompleted||app.Dialogue.Active||dateEventStarted||PlantsGone||value.Year!=Data.secretYear||value.Month!=Data.secretMonth||value.Day!=Data.secretDay)return;
            dateEventStarted=true;Sepia=true;foreach(var npc in npcs)if(npc)npc.SetActive(false);app.Audio.PauseEventMusic();StoryEffects.Get().Sepia(true);
        }
        void SpawnItem(string id,string name,string sprite,float x,float y,Action click=null,bool drag=true)
        {
            if(id!="exit")
            {
                Register(id,name,sprite,RecycleFiles.Desktop,click,false,id=="key.png"?"picture:"+Data.key.id:"item:"+id);return;
            }
            if(items.TryGetValue(id,out var existing)&&existing)return;
            var icon=FakeIcon.Create(app.ScreenRoot,id,name,sprite,x,y,click,drag);items[id]=icon;if(drag)BindDrag(icon);
        }
        void Consume(string id){FileStore.Remove(id);if(items.TryGetValue(id,out var icon)&&icon){icon.gameObject.SetActive(false);Destroy(icon.gameObject);}items.Remove(id);}
        public bool Receive(string id,string target)
        {
            if(app.Dialogue.Active||manager.State==StageState.Transition)return false;
            if(FileStore.Find(id)!=null&&(target==RecycleFiles.Desktop||target==RecycleFiles.Collection||target==RecycleFiles.Trash))return FileStore.Move(id,target);
            if(!FileStore.CanUse(id))return false;
            if(target=="photo-cloud"&&id=="watering"&&!Rained&&!OutdoorNight){var outdoor=app.Windows.GetComponentInChildren<OutdoorWindow>();if(outdoor)return outdoor.Water();return false;}
            if(target=="twins"&&!Sepia)
            {
                if(id=="white.key"&&WhiteTaken)return DeliverKey("white");
                if(id=="black.key"&&BlackTaken)return DeliverKey("black");
                if(id=="bouquet"&&BouquetTaken&&!OptionalSolved){Consume(id);app.Save.Flag("stage02.tulipSolved");app.Dialogue.Play(()=>{},"S2_TULIP");return true;}
            }
            if(target.StartsWith("part:",StringComparison.Ordinal)&&exitWindow&&Assembly.Merge(id,target.Substring(5)))
            {app.Audio.Play("sfx_file_merge");DrawExit();return true;}
            return false;
        }
        void BindDrag(FakeIcon icon)
        {
            icon.ScreenBoundDrag=true;
            icon.DroppedOutside=e=>{
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
                var hit=hits.Find(h=>h.gameObject!=icon.gameObject&&!h.gameObject.transform.IsChildOf(icon.transform));
                var target=hit.gameObject?hit.gameObject.GetComponentInParent<DropTarget>():null;
                DropFile(icon.Id,target?target.Category:"");
            };
        }
        FakeWindow exitWindow;
        public bool DropFile(string id,string target)
        {
            bool accepted=Receive(id,target);
            if(!accepted)
            {
                if(id=="watering")app.Audio.PlayPuzzle(Data.wateringFailureSound);
                else app.Audio.Play("sfx_drag_return");
            }
            return accepted;
        }
        void OpenExit(){if(FakeWindow.TryCreate(app.Windows,"s2:exit","나가는길",out exitWindow,260,180,570,330)){exitWindow.SetFileBackground();exitWindow.Closed=()=>{exitWindow=null;fileViews.Remove(RecycleFiles.Exit);};DrawExit();}}
        void DrawExit()
        {
            if(!exitWindow)return;UIFactory.Clear(exitWindow.Content);
            if(Assembly.Complete)
            {
                if(FileStore.Find("door.exe")==null)Register("door.exe","door.exe","door",RecycleFiles.Exit,()=>manager.RequestClear("door.exe"));
                fileViews[RecycleFiles.Exit]=()=>{if(exitWindow)DrawExit();};
                DrawFiles(exitWindow.Content,RecycleFiles.Exit,true);foreach(var icon in exitWindow.Content.GetComponentsInChildren<FakeIcon>()) {icon.DroppedOutside=null;icon.RejectOutsideDrop=true;}return;
            }
            int n=0;foreach(var group in Assembly.Groups)
            {
                var icon=FakeIcon.Create(exitWindow.Content,group.Key,Assembly.Label(group.Key)+".?","black_square",20+n++*125,60,null,true);
                icon.gameObject.AddComponent<DropTarget>().Category="part:"+group.Key;
                icon.RejectOutsideDrop=true;icon.Dropped=(source,target)=>DropFile(source.Id,target.Category);
            }
        }
    }
}
