using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace CoComputar
{
    public sealed partial class Stage05Controller:MonoBehaviour,IStageEntry
    {
        GameApp app;StageManager manager;RectTransform icons;FilePopup menu;bool busy,finalStarted,finalDone,bgmTalk;int talkStep,interactions;float firstTalkTime;
        readonly Dictionary<string,FakeWindow> folders=new();
        public FakeFileSystem Files{get;private set;}public Stage05Config Data{get;private set;}public Stage05Randomizer Randoms{get;private set;}
        public bool RadioSolved{get;private set;}public bool PhotoSolved{get;private set;}public bool PhotoRewardClaimed{get;private set;}public bool PlaylistSolved{get;private set;}public bool PlaylistRewardClaimed{get;private set;}public bool ClassicRegistered{get;private set;}
        public List<string> Playlist{get;private set;}public bool Ready=>!busy&&!app.Dialogue.Active;public bool FinalDone=>finalDone;
        public event Action SpeakerFix;public Vector3 PhotoValues;public string PhotoFile;
        public void Enter(StageManager mgr,StageDefinition definition)
        {
            app=GameApp.Instance;manager=mgr;app.SetFileRightClick(true);app.DesktopSettingsClicked=app.OpenOptions;
            Data=JsonUtility.FromJson<Stage05Config>(Resources.Load<TextAsset>("Stage05Config").text);Data.musicByDistance=new AnimationCurve(Data.radioDistances.Select((d,i)=>new Keyframe(d,Data.radioGains[i])).ToArray());PhotoValues=Vector3.zero;Randoms=new Stage05Randomizer(Data);Playlist=Randoms.Initial.ToList();
            Files=new FakeFileSystem(JsonUtility.FromJson<FakeFileData>(Resources.Load<TextAsset>("Stage05FileLayout").text));
            for(int i=0;i<Randoms.Photos.Length;i++){int n=Randoms.Photos[i];Files.Nodes.Add(new FakeFileNode{id="photo"+n,displayName=n+".png",extension="png",parentFolder="nowof",contentType="image",iconKey="picture",imageKey="s5_photo"});}
            Files.Find("hint").content=string.Join("\n",Randoms.Hint.Select((s,i)=>(i+1)+". "+Stage05Randomizer.Prefix(s)));
            RestoreProgress();
            app.Dialogue.Load(definition.dialogue);app.Dialogue.FlagCommand=flag=>{if(flag=="S5_SPEAKER_FIX")SpeakerFix?.Invoke();};app.Audio.Music("bgm_stage05");app.Audio.BgmMutedChanged+=OnMutedChanged;
            icons=UIFactory.Rect(app.ScreenRoot,"Stage05 파일",0,38,1024,730);Files.Changed+=FilesChanged;Refresh();SaveStageProgress();
        }
        void OnMutedChanged(bool muted){if(!muted&&app.Dialogue.Active&&(app.Dialogue.CurrentSequence=="S5_BGM_1"||app.Dialogue.CurrentSequence=="S5_BGM_2"))app.Dialogue.CloseCurrent();}
        void Count()=>interactions++;
        void Refresh()
        {
            UIFactory.Clear(icons);FakeIcon.Create(icons,"speaker","스피커","icon_speaker",780,150,Talk);
            var trash=FakeIcon.Create(icons,"s5:trash","휴지통","trash",865,565,()=>OpenFolder("trash"));var target=trash.gameObject.AddComponent<DropTarget>();target.Category="s5trash";target.IsFolderWindow=true;
            foreach(var n in Files.Children("desktop").Where(IsFileVisible))AddIcon(icons,n,n.x,n.y);
            foreach(var pair in folders.ToArray())if(pair.Value)DrawFolder(pair.Value,pair.Key);else folders.Remove(pair.Key);
            foreach(var explorer in app.Windows.GetComponentsInChildren<Stage05ExplorerWindow>())explorer.Refresh();
        }
        internal void AddIcon(Transform parent,FakeFileNode n,float x,float y,Action open=null)
        {
            var icon=FakeIcon.Create(parent,"s5:"+n.id,n.displayName,n.iconKey,x,y,open??(()=>Open(n.id)),true,n.hidden);icon.AllowWindowDrops=true;icon.ScreenBoundDrag=true;
            if(n.contentType=="half"||n.contentType=="folder"){var drop=icon.gameObject.AddComponent<DropTarget>();drop.Category="s5:"+n.id;drop.IsFolderWindow=true;}
            icon.Dropped=(i,t)=>Drop(n.id,t.Category);icon.gameObject.AddComponent<FileRightClick>().Click=e=>Context(n,icon,e,open);
        }
        public void Drop(string id,string target)
        {
            if(!Ready)return;var n=Files.Find(id);if(n==null)return;Count();
            if(n.parentFolder=="trash")
            {
                if(target.StartsWith("s5:")){var dest=target.Substring(3);var folder=Files.Find(dest);if(folder?.contentType=="folder"&&Files.CanUse(folder)&&!Files.IsWithin(folder,id)){RestoreFile(n.id,dest);}}return;
            }
            if(!Files.CanUse(n)||!IsFileVisible(n))return;
            if(target=="s5trash"){DeleteFile(id);return;}
            if(target=="s5:slot"){RegisterClassic(id);return;}
            if(target=="s5:photoeditor"){var editor=app.Windows.GetComponentInChildren<PhotoCorrectionRenderer>();if(editor)editor.Load(id);return;}
            if((id=="iconA"&&target=="s5:iconB")||(id=="iconB"&&target=="s5:iconA")){if(PhotoSolved&&PhotoRewardClaimed&&PlaylistSolved&&PlaylistRewardClaimed&&Files.CanUse(Files.Find("iconA"))&&Files.CanUse(Files.Find("iconB")))StartCoroutine(Merge());return;}
            // 반쪽 아이콘과 보상 음악은 잘못된 드롭으로 이동하지 않는다.
            if(n.contentType=="half"||id=="classic")return;
            if(target.StartsWith("s5:")){var dest=Files.Find(target.Substring(3));if(dest?.contentType=="folder"&&Files.CanUse(dest)&&!Files.IsWithin(dest,n.id)){CloseRelated(n.id);n.parentFolder=dest.id;Files.Notify();}}
        }
        public bool IsFileVisible(FakeFileNode n)=>n!=null&&n.IsVisible(app.Save.Hidden("s5:folder:"+n.parentFolder));
        public bool IsExplorerFolder(string id){var n=Files.Find(id);return n!=null&&n.contentType=="folder"&&Files.CanUse(n)&&(Files.IsWithin(n,"look")||Files.IsWithin(n,"soft"));}
        public void OpenFolder(string id)
        {
            if(!Ready)return;Count();if(IsExplorerFolder(id)){Stage05ExplorerWindow.Open(this,id);return;}if(!FakeWindow.TryCreate(app.Windows,"s5:folder:"+id,id=="trash"?"휴지통":Files.Find(id).displayName,out var w,180,80,790,600))return;
            w.SetFileBackground(38);w.gameObject.AddComponent<FileOwner>().Id=id;folders[id]=w;w.Closed+=()=>folders.Remove(id);UIFactory.Rect(w.Content,"폴더 파일",0,38,w.Content.rect.width,w.Content.rect.height-38);w.AddHiddenToggle(()=>DrawFolder(w,id));if(id=="trash")w.AddToolbarRightButton("모두 복원",140,30,2,RestoreAllFiles);DrawFolder(w,id);
        }
        void DrawFolder(FakeWindow w,string id)
        {
            var body=(RectTransform)w.Content.Find("폴더 파일");UIFactory.Clear(body);float top=0;
            var area=UIFactory.Panel(body,"파일 목록",0,top,body.rect.width,body.rect.height-top,Color.white);area.gameObject.AddComponent<RectMask2D>();var target=area.gameObject.AddComponent<DropTarget>();target.Category=id=="trash"?"s5trash":"s5:"+id;target.IsFolderWindow=true;
            var nodes=Files.Children(id).Where(n=>n.IsVisible(w.ShowHidden)).ToArray();var content=UIFactory.Rect(area.transform,"아이콘",0,0,760,Mathf.Max(area.rectTransform.rect.height,Mathf.Ceil(nodes.Length/6f)*112));var scroll=area.gameObject.AddComponent<ScrollRect>();scroll.viewport=area.rectTransform;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;
            for(int i=0;i<nodes.Length;i++)AddIcon(content,nodes[i],10+i%6*124,8+i/6*112);
        }
        public bool DeleteFile(string id)
        {
            var n=Files.Find(id);if(!Ready||n==null||!Files.CanUse(n)||!IsFileVisible(n))return false;
            CloseRelated(id);Files.Delete(n);app.Audio.PlayPuzzle("sfx_s5_trash",replace:true);return true;
        }
        public bool RestoreFile(string id,string destination=null,bool sound=true)
        {
            var n=Files.Find(id);if(!Ready||n==null||n.parentFolder!="trash"||n.unrestorable)return false;
            if(destination!=null){var folder=Files.Find(destination);if(folder?.contentType!="folder"||!Files.CanUse(folder)||Files.IsWithin(folder,id))return false;n.parentFolder=destination;Files.Notify();}
            else Files.RestoreTrash(n);
            if(sound)app.Audio.PlayPuzzle("sfx_s5_restore",replace:true);return true;
        }
        void RestoreAllFiles(){bool changed=false;foreach(var n in Files.Children("trash").ToArray())changed|=RestoreFile(n.id,sound:false);if(changed)app.Audio.PlayPuzzle("sfx_s5_restore",replace:true);}
        void CloseRelated(string id){foreach(var editor in app.Windows.GetComponentsInChildren<PhotoCorrectionRenderer>())if(Files.IsWithin(Files.Find(editor.Loaded),id))editor.GetComponent<FakeWindow>().Close();foreach(var owner in app.Windows.GetComponentsInChildren<FileOwner>())if(owner.Id==id||Files.IsWithin(Files.Find(owner.Id),id))owner.GetComponent<FakeWindow>()?.Close();}
        void CloseMenu(){if(menu)menu.Close();menu=null;}
        void Context(FakeFileNode n,FakeIcon icon,PointerEventData e,Action open=null)
        {
            if(!Ready)return;CloseMenu();var block=UIFactory.Panel(app.Modal,"파일 메뉴",0,0,1024,768,Color.clear);menu=block.gameObject.AddComponent<FilePopup>();block.gameObject.AddComponent<Button>().onClick.AddListener(CloseMenu);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(app.Modal,e.position,e.pressEventCamera,out var p);var panel=UIFactory.Panel(block.transform,"메뉴",Mathf.Clamp(p.x,0,815),Mathf.Clamp(-p.y,38,565),205,n.parentFolder=="trash"?42:160,new Color(.76f,.76f,.78f));int row=0;
            void Item(string label,Action action,bool enabled=true){var b=UIFactory.Button(panel.transform,label,3,3+row++*38,199,35,()=>{CloseMenu();action();});b.interactable=enabled;}
            if(n.parentFolder=="trash"){Item("복원",()=>RestoreFile(n.id));return;}
            Item("열기",open??(()=>Open(n.id)));Item("복원",()=>{CloseRelated(n.id);Files.RestoreOriginal(n);},n.isBroken&&n.renamable);Item("이름 바꾸기",()=>FileRename.Begin(icon,n.displayName,name=>Rename(n.id,name)),n.renamable);Item("삭제",()=>DeleteFile(n.id));
        }
        public bool Rename(string id,string name)
        {
            if(!Ready)return false;var n=Files.Find(id);if(n==null||!Files.CanUse(n)||!Files.Rename(n,name,out var error))return false;CloseRelated(id);
            if(id=="door"&&FakeFileSystem.Eq(n.displayName,"door.exe")&&!finalStarted){finalStarted=true;app.Audio.SetMusicFade(0);CloseMenu();app.Dialogue.Play(()=>{finalDone=true;SaveStageProgress();app.Audio.SetMusicFade(0);},"S5_FINAL");}return true;
        }
        public void Open(string id)
        {
            if(!Ready)return;CloseMenu();var n=Files.Find(id);if(n==null||!Files.CanUse(n)||!IsFileVisible(n))return;Count();
            if(n.contentType=="folder"){OpenFolder(id);return;}
            if(n.contentType=="half")return;
            if(n.isBroken){FileUI.Notice("파일이 손상되었습니다. 복원 메뉴를 사용하세요.");return;}
            if(id=="radio"){OpenRadio();return;}if(id=="photo"){OpenPhoto();return;}if(id=="sound"){OpenPlaylist();return;}
            if(id=="door")
            {
                if(n.extension=="exe"){if(finalDone&&FakeFileSystem.Eq(n.displayName,"door.exe")){busy=true;app.CloseAllWindows();manager.RequestClear("door.exe");}else FileUI.Notice("실행할 수 없는 파일입니다.");return;}
                if(n.extension=="txt"){FileUI.Notice("내용을 읽을 수 없습니다.");return;}
            }
            if(n.contentType=="text")
            {
                if(FakeWindow.TryCreate(app.Windows,"s5:file:"+id,n.displayName+" - 메모장",out var w,210,100,600,550)){w.gameObject.AddComponent<FileOwner>().Id=id;FileUI.ScrollText(w.Content,n.content,0,0,580,495,Color.black);}return;
            }
            if(id=="classic")return;
            var viewer=ImageViewer.Open(new ViewerDefinition{id="s5:"+id,title=n.displayName,imageKey=id=="door"?"door":n.imageKey});if(!viewer.Window.GetComponent<FileOwner>())viewer.Window.gameObject.AddComponent<FileOwner>().Id=id;
            if(id=="photo"+Randoms.Frequency&&!PhotoSolved){var image=viewer.PictureRoot.GetComponentInChildren<Image>();if(image)image.color=new Color(.4f,.55f,.3f,.55f);}
        }
        public void Talk()
        {
            if(!Ready)return;CloseMenu();string seq;
            if(finalStarted)seq="S5_AFTER_FINAL";
            else if(app.Audio.BgmMuted){seq=bgmTalk?"S5_BGM_2":"S5_BGM_1";bgmTalk=true;}
            else if(talkStep==0){seq="S5_TALK_1";talkStep=1;app.Dialogue.Play(()=>{firstTalkTime=Time.unscaledTime;interactions=0;},seq);return;}
            else if(talkStep==1&&interactions>=Data.secondTalkInteractions&&Time.unscaledTime-firstTalkTime>=Data.secondTalkSeconds){seq="S5_TALK_2";talkStep=2;}
            else seq=talkStep==2?"S5_TALK_3":"S5_AFTER_FINAL";
            app.Dialogue.Play(()=>{},seq);
        }
        public void RadioSuccess(){if(RadioSolved)return;RadioSolved=true;Files.Reveal("classic");app.Audio.Play("sfx_radio_lock");app.Audio.Play("sfx_number_lock");}
        public void PhotoSuccess(){if(PhotoSolved||!RadioSolved)return;PhotoSolved=true;ConfirmHalfPuzzleClear();}
        // Called only on the first solved transition; restoring state never emits this sound.
        void ConfirmHalfPuzzleClear(){SaveStageProgress();app.Audio.Play(Data.puzzleClearSfx);}
        public bool ClaimPhotoReward(){if(!Ready||!PhotoSolved||PhotoRewardClaimed)return false;PhotoRewardClaimed=true;Files.Reveal("iconA");app.Audio.Play("sfx_photo_success");return true;}
        public bool ClaimPlaylistReward(){if(!Ready||!PlaylistSolved||PlaylistRewardClaimed)return false;PlaylistRewardClaimed=true;Files.Reveal("iconB");app.Audio.Play("sfx_playlist_success");return true;}
        public bool RegisterClassic(string id){if(!Ready||id!="classic"||!RadioSolved||ClassicRegistered||!Files.CanUse(Files.Find(id)))return false;ClassicRegistered=true;CloseRelated(id);Files.Nodes.Remove(Files.Find(id));Files.Notify();app.Audio.Play("sfx_playlist_drop");app.Windows.GetComponentInChildren<PlaylistView>()?.Draw();return true;}
        public void MoveSong(int index,int direction)
        {
            if(!Ready||PlaylistSolved||index<0||index>=Playlist.Count)return;int next=index+direction;if(next<0||next>=Playlist.Count)return;(Playlist[index],Playlist[next])=(Playlist[next],Playlist[index]);app.Audio.Play("sfx_playlist_move");SaveStageProgress();
            if(ClassicRegistered&&Playlist.SequenceEqual(Randoms.Hint)){PlaylistSolved=true;ConfirmHalfPuzzleClear();}
        }
        IEnumerator Merge()
        {
            busy=true;var blocker=UIFactory.Panel(app.Modal,"결합 입력 잠금",0,0,1024,768,Color.clear);Files.Find("iconA").available=Files.Find("iconB").available=false;Files.Notify();yield return IconMerge.Play(blocker.transform,app.Catalog.sprites.Get("s5_half_a"),app.Catalog.sprites.Get("s5_half_b"),app.Audio);Files.Reveal("door");Destroy(blocker.gameObject);busy=false;SaveStageProgress();
        }
        void Update()
        {
            if(finalStarted)app.Audio.SetMusicFade(0);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(Keyboard.current?.f1Key.wasPressedThisFrame==true){if(FakeWindow.TryCreate(app.Windows,"s5:debug","개발용 정답",out var w))FileUI.ScrollText(w.Content,"F = "+Randoms.Frequency+"\n사진 = "+Randoms.Frequency+".png\n보정 = "+Randoms.PhotoTarget+"\n"+string.Join("\n",Randoms.Hint),0,0,620,400,Color.black);}
#endif
        }
        void OnDestroy(){if(app)app.Audio.BgmMutedChanged-=OnMutedChanged;if(Files!=null)Files.Changed-=FilesChanged;CloseMenu();}
    }
}
