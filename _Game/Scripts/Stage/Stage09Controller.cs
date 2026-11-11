using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace CoComputar
{
    // 실제 스테이지9 전용. Stage08SpecialProgress/복제 장면과 어떤 상태도 공유하지 않는다.
    public sealed class Stage09Controller:MonoBehaviour,IStageEntry
    {
        public FakeFileSystem Files{get;private set;}public Stage09Rules Rules{get;private set;}
        public Stage09Progress Progress{get;private set;}public bool Busy{get;private set;}
        public bool WaitingForHead{get;private set;}public ImageViewer HomeViewer{get;private set;}
        GameApp app;StageManager manager;RectTransform desktop;TextAsset dialogue;FakeIcon npc;
        string answerOriginalName;FilePopup menu;Image headTarget;GameObject pause;float waiting;bool debugVisible;
        readonly Dictionary<string,Action> refresh=new();
        public bool Ready=>!Busy&&!app.Dialogue.Active&&!pause;
        string T(string id)=>Rules.Text(id);
        public void Enter(StageManager mgr,StageDefinition definition)
        {
            app=GameApp.Instance;manager=mgr;var assets=Resources.Load<Stage09Assets>("Stage09Assets");
            Rules=JsonUtility.FromJson<Stage09Rules>(assets.rules.text);dialogue=assets.dialogue;
            answerOriginalName=JsonUtility.FromJson<FakeFileData>(assets.files.text).files.First(n=>n.id==Rules.answerFileId).displayName;
            Progress=app.Save.Progress.stage09??new Stage09Progress();app.Save.Progress.stage09=Progress;
            Files=new FakeFileSystem(Progress.files==null?JsonUtility.FromJson<FakeFileData>(assets.files.text):new FakeFileData{files=Progress.files});
            if(Files.Find(Rules.answerFileId)==null||Rules.revealZoom>Rules.maxZoom)throw new InvalidOperationException("Stage09 정답/확대 데이터 오류");
            // 획득 직후 페이드 도중 세션을 복원해도 NPC가 다시 남지 않는다.
            if(Progress.keyObtained){Progress.npcGone=true;Files.Find("finalKey").available=true;}
            app.SetFileRightClick(true);app.DesktopSettingsClicked=app.OpenOptions;
            desktop=UIFactory.Rect(app.ScreenRoot,"Stage09 바탕화면",0,38,1024,730);
            Files.Changed+=Changed;app.Audio.Music("bgm_stage09");RefreshDesktop();Save();
        }
        void Save(){Progress.files=Files.Nodes.ToArray();app.Save.SaveProgress();}
        void Changed(){Save();RefreshDesktop();foreach(var draw in refresh.Values.ToArray())draw();}
        public void RefreshDesktop()
        {
            UIFactory.Clear(desktop);foreach(var n in Files.Children("desktop").Where(n=>!n.hidden))Icon(desktop,n,n.x,n.y);
            if(!Progress.npcGone){npc=FakeIcon.Create(desktop,"s9:npc",Rules.npcLabel,Rules.npcIcon,Rules.npcX,Rules.npcY,Talk);npc.gameObject.AddComponent<DropTarget>().Category="s9:npc";}
        }
        FakeIcon Icon(Transform parent,FakeFileNode n,float x,float y)
        {
            var icon=FakeIcon.Create(parent,"s9:"+n.id,n.displayName,n.iconKey,x,y,()=>Open(n.id),true,n.hidden);
            icon.gameObject.AddComponent<Stage09DragSound>().Key=n.contentType=="key";icon.ScreenBoundDrag=true;icon.DroppedOutside=e=>Drop(n.id,e,icon);
            if(n.contentType=="folder"){
                icon.gameObject.AddComponent<DropTarget>().Category="s9:"+n.id;
                if(IsLocked(n.id))UIFactory.Text(icon.transform,"▣",58,32,28,30,24,Color.black);
            }
            icon.gameObject.AddComponent<FileRightClick>().Click=e=>{if(Ready)Context(icon,n,e);};return icon;
        }
        public bool IsLocked(string id)=>new[]{"A","B","C"}.Contains(id)&&!Progress.unlocked.Contains(id);
        public void Open(string id)
        {
            if(!Ready)return;var n=Files.Find(id);if(n==null||!Files.CanUse(n))return;
            if(IsLocked(id)){Notice("locked");return;}
            if(n.contentType=="folder"){Folder(n);return;}
            if(n.extension=="zip"){Notice("zip");return;}
            if(n.contentType=="key")return;
            if(n.contentType=="record")
            {
                if(n.extension=="txt"){TextViewer(n);return;}
                if(n.id!=Rules.answerFileId||!FakeFileSystem.Eq(n.extension,Rules.answerExtension)){Notice("corrupt","sfx_image_corrupt");return;}
            }
            Picture(n);
        }
        void Folder(FakeFileNode folder)
        {
            if(!FakeWindow.TryCreate(app.Windows,"s9:folder:"+folder.id,folder.displayName,out var w,230,120,610,500))return;
            w.SetFileBackground();var area=UIFactory.Rect(w.Content,"파일 영역",0,0,586,395);
            void Draw(){if(!w||w.IsClosing)return;UIFactory.Clear(area);int i=0;foreach(var n in Files.Children(folder.id).Where(n=>n.IsVisible(w.ShowHidden))){Icon(area,n,15+i%4*140,15+i/4*110);i++;}}
            refresh[folder.id]=Draw;w.Closed+=()=>refresh.Remove(folder.id);w.AddHiddenToggle(Draw,true);Draw();
        }
        void Context(FakeIcon icon,FakeFileNode n,PointerEventData e)
        {
            CloseMenu();var block=UIFactory.Panel(app.Modal,"파일 메뉴",0,0,1024,768,Color.clear);menu=block.gameObject.AddComponent<FilePopup>();block.gameObject.AddComponent<Button>().onClick.AddListener(CloseMenu);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(app.Modal,e.position,e.pressEventCamera,out var pos);
            var panel=UIFactory.Panel(block.transform,"메뉴",Mathf.Clamp(pos.x,0,810),Mathf.Clamp(-pos.y,38,585),205,160,WindowSkin.Pink);
            string[] ids={"extract","rename","delete","restore"};for(int i=0;i<ids.Length;i++){
                string id=ids[i];var b=UIFactory.Button(panel.transform,T(id),3,3+38*i,199,35,()=>{CloseMenu();if(id=="extract")Extract();else if(id=="rename")FileRename.Begin(icon,n.displayName,name=>ConfirmRename(n,name));});
                b.interactable=id=="extract"?n.id=="zip":id=="rename"&&n.contentType=="record";
            }
        }
        void CloseMenu(){if(menu)menu.Close();menu=null;}
        public bool Extract()
        {
            if(!Ready||Progress.unpacked)return false;
            if(!Files.ExtractArchive("zip","keys"))return false;Progress.unpacked=true;Save();app.Audio.Play("sfx_unzip");StartCoroutine(Flash("keys"));return true;
        }
        void ConfirmRename(FakeFileNode n,string name)
        {
            if(!Ready)return;string ext=System.IO.Path.GetExtension(name);
            if(FakeFileSystem.Eq(ext,"."+n.extension)){Rename(n.id,name);return;}
            if(!FakeWindow.TryCreate(app.Windows,"s9:extension",T("warning"),out var w,260,240,510,240))return;
            UIFactory.Text(w.Content,T("extension"),15,8,465,80,20,Color.black);
            UIFactory.Button(w.Content,T("confirm"),100,96,130,35,()=>{w.Close();Rename(n.id,name);});
            UIFactory.Button(w.Content,T("cancel"),255,96,130,35,w.Close);
        }
        public bool Rename(string id,string name)
        {
            if(!Ready)return false;var n=Files.Find(id);if(n==null||n.contentType!="record")return false;
            if(Files.Children(n.parentFolder).Any(other=>other!=n&&FakeFileSystem.Eq(other.displayName,name.Trim()))){Notice("duplicate");return false;}
            if(!Files.Rename(n,name,out var error)){FileUI.Notice(error);return false;}
            CloseRelated(id);app.Audio.Play("sfx_rename_ok");return true;
        }
        void CloseRelated(string id){foreach(var w in app.Windows.GetComponentsInChildren<FakeWindow>().ToArray())if(w.GetComponent<FileOwner>()?.Id==id)w.Close();}
        void TextViewer(FakeFileNode n)
        {
            if(!FakeWindow.TryCreate(app.Windows,"s9:text:"+n.id,n.displayName,out var w,250,110,630,520))return;
            w.gameObject.AddComponent<FileOwner>().Id=n.id;string encoding=NotepadEncodings.Default;GameObject choices=null;
            var text=FileUI.ScrollText(w.Content,Stage09Logic.Garble(n.id,encoding),0,0,600,410,Color.black);Button selector=null;
            selector=UIFactory.Button(w.Toolbar,T("encoding")+": ANSI",4,0,215,32,()=>{
                if(choices){Destroy(choices);return;}
                var panel=UIFactory.Panel(w.transform,"인코딩 목록",15,78,215,198,WindowSkin.Pink);choices=panel.gameObject;
                for(int i=0;i<NotepadEncodings.Options.Length;i++){var option=NotepadEncodings.Options[i];UIFactory.Button(panel.transform,option.Label,2,2+i*32,211,31,()=>{
                    encoding=option.Id;text.text=Stage09Logic.Garble(n.id,encoding);selector.GetComponentInChildren<TMPro.TMP_Text>().text=T("encoding")+": "+option.Label;Destroy(choices);
                });}
            });
        }
        void Picture(FakeFileNode n)
        {
            var imageKey=n.contentType=="record"?"s9_family":n.imageKey??n.iconKey;
            if(string.IsNullOrEmpty(imageKey))imageKey=n.iconKey;
            var viewer=ImageViewer.Open(new ViewerDefinition{id="s9:"+n.id,title=n.displayName,imageKey=imageKey,zoomInLevels=Rules.maxZoom,zoomStep=Rules.zoomStep,fitImage=true});
            if(viewer.GetComponent<Stage09Image>())return;
            viewer.Window.gameObject.AddComponent<FileOwner>().Id=n.id;
            var binding=viewer.gameObject.AddComponent<Stage09Image>();binding.Setup(this,viewer,n);
            if(n.id=="home"){HomeViewer=viewer;viewer.Window.Closed+=()=>HomeViewer=null;}
        }
        public string HintText(Stage09Hint hint)=>hint.answer?answerOriginalName+" → ."+Rules.answerExtension:hint.text;
        public void Notice(string key,string sound="sfx_wrong_key"){app.Audio.Play(sound);FileUI.Notice(T(key),"s9:notice",false,T("warning"));}
        public bool Unlock(string key,string folder)
        {
            if(!Ready||!IsLocked(folder))return false;
            if(key!="key"+folder){Notice("wrong");return false;}
            Progress.unlocked.Add(folder);app.Audio.Play("sfx_unlock");Changed();StartCoroutine(Flash(folder));return true;
        }
        IEnumerator Flash(string id)
        {
            var item=desktop.GetComponentsInChildren<FakeIcon>().FirstOrDefault(i=>i.Id=="s9:"+id);if(!item)yield break;
            var art=item.Artwork;for(float t=0;t<.35f;t+=Time.unscaledDeltaTime){if(!art)yield break;art.color=Color.Lerp(new Color(1,.8f,.3f),Color.white,t/.35f);yield return null;}if(art)art.color=Color.white;
        }
        void Drop(string id,PointerEventData e,FakeIcon source)
        {
            if(!Ready)return;
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
            var hit=hits.FirstOrDefault(h=>h.gameObject!=source.gameObject&&!h.gameObject.transform.IsChildOf(source.transform));
            if(!hit.gameObject){app.Audio.Play("sfx_wrong_key");return;}
            var target=hit.gameObject.GetComponentInParent<DropTarget>();var picture=hit.gameObject.GetComponentInParent<ImageViewer>();
            if(picture&&picture==HomeViewer){var binding=picture.GetComponent<Stage09Image>();if(binding.HitDoor(e.position,e.pressEventCamera)&&Escape(id))return;}
            if(target&&target.Category.StartsWith("s9:")){
                string destination=target.Category.Substring(3);
                if(destination=="npc"){Deliver(id,source);return;}
                if(IsLocked(destination)){Unlock(id,destination);return;}
                if(new[]{"A","B","C"}.Contains(destination)&&id=="key"+destination)return;
            }
            app.Audio.Play("sfx_wrong_key");
        }
        public bool Deliver(string id,FakeIcon source=null)
        {
            if(!Ready||!Stage09Logic.Photo(Progress,Files.Find(id),Rules)){app.Audio.Play("sfx_wrong_key");return false;}
            Busy=true;StartCoroutine(Delivery(id,source));return true;
        }
        IEnumerator Delivery(string id,FakeIcon source)
        {
            var floating=UIFactory.Sprite(app.DragLayer,app.Catalog.sprites.Get("picture"),0,0,60,64);
            floating.raycastTarget=false;floating.transform.position=source?source.transform.position:npc.transform.position;
            Vector3 from=floating.transform.position,to=npc.transform.position;app.Audio.Play("sfx_photo_deliver");
            for(float t=0;t<.4f;t+=Time.unscaledDeltaTime){floating.transform.position=Vector3.Lerp(from,to,t/.4f);floating.transform.localScale=Vector3.one*(1-t/.4f);yield return null;}
            Destroy(floating.gameObject);CloseRelated(id);Files.Find(id).available=false;Progress.photoDelivered=true;Busy=false;Changed();
        }
        public void Talk()
        {
            if(!Ready||Progress.npcGone)return;string id=Stage09Logic.Dialogue(Progress);CloseMenu();app.Dialogue.Load(dialogue);
            app.Dialogue.LineRevealed=line=>{if(id=="S9_FINAL"&&line==T("headLine")&&!WaitingForHead)WaitForHead();};
            app.Dialogue.Play(()=>{if(id=="S9_D1")Progress.d1=true;if(id=="S9_D2")Progress.d2=true;if(id=="S9_D3")Progress.d3=true;app.Dialogue.LineRevealed=null;Save();},id);
        }
        void WaitForHead()
        {
            WaitingForHead=true;app.Dialogue.Paused=true;waiting=0;var r=Rules.headRect;
            headTarget=UIFactory.Panel(app.Modal,"열쇠 머리 클릭",r.x,r.y,r.width,r.height,Color.clear);
            headTarget.gameObject.AddComponent<BrowserLink>();headTarget.gameObject.AddComponent<Button>().onClick.AddListener(TakeHead);
        }
        public void TakeHead()
        {
            if(!WaitingForHead||Busy||pause)return;WaitingForHead=false;Busy=true;headTarget.raycastTarget=false;StartCoroutine(Head());
        }
        IEnumerator Head()
        {
            var portrait=app.Modal.Find("대화/왼쪽 NPC");
            if(portrait){var original=portrait.GetComponent<Image>();var body=UIFactory.Rect(portrait.parent,"머리가 분리된 몸",32,340,335,240);body.gameObject.AddComponent<RectMask2D>();var copy=UIFactory.Sprite(body,original.sprite,0,-150,335,390);copy.color=original.color;copy.preserveAspect=original.preserveAspect;original.enabled=false;}
            var r=Rules.headRect;var head=UIFactory.Sprite(app.Modal,app.Catalog.sprites.Get("s9_head"),r.x,r.y,r.width,r.height);head.raycastTarget=false;app.Audio.Play("sfx_key_click");
            var reward=Files.Find("finalKey");Vector3 from=head.transform.position,to=desktop.TransformPoint(new Vector3(reward.x,-reward.y,0));
            for(float t=0;t<.45f;t+=Time.unscaledDeltaTime){float k=t/.45f;head.transform.position=Vector3.Lerp(from,to,k);head.transform.localScale=Vector3.one*Mathf.Lerp(1,.2f,k);head.color=new Color(1,1,1,1-k);yield return null;}
            Destroy(head.gameObject);Destroy(headTarget.gameObject);app.Dialogue.CloseCurrent();
            Progress.keyObtained=true;Files.Find("finalKey").available=true;Save();app.Audio.Play("sfx_file_appear");
            if(npc){var group=npc.GetComponent<CanvasGroup>()??npc.gameObject.AddComponent<CanvasGroup>();app.Audio.Play("sfx_npc_fade");for(float t=0;t<.5f;t+=Time.unscaledDeltaTime){group.alpha=1-t/.5f;yield return null;}}
            Progress.npcGone=true;Busy=false;Changed();
        }
        public bool Escape(string id)
        {
            if(!Ready||id!="finalKey"||!Progress.keyObtained||IsLocked("C")||!HomeViewer||HomeViewer.Level<Rules.revealZoom)return false;
            Busy=true;app.Audio.Play("sfx_door_open");app.CloseAllWindows();manager.RequestClear("homeDoor");return true;
        }
        void Update()
        {
            if(WaitingForHead&&!pause){waiting+=Time.unscaledDeltaTime;if(headTarget&&waiting>=Rules.headWaitSeconds)headTarget.color=new Color(1,1,.4f,.12f+.08f*Mathf.Sin(waiting*3));}
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true){if(pause)Resume();else{
                pause=UIFactory.Panel(app.Modal,T("pause"),0,0,1024,768,new Color(0,0,0,.8f)).gameObject;app.Dialogue.Paused=true;
                UIFactory.Button(pause.transform,T("resume"),340,290,340,45,Resume);UIFactory.Button(pause.transform,T("title"),340,360,340,45,app.ShowTitle);UIFactory.Button(pause.transform,T("quit"),340,430,340,45,GameApp.Quit);
            }}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(Keyboard.current?.f1Key.wasPressedThisFrame==true)debugVisible=!debugVisible;
#endif
        }
        void Resume(){if(pause)Destroy(pause);pause=null;app.Dialogue.Paused=WaitingForHead;}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI(){if(debugVisible)GUI.Box(new Rect(15,15,630,125),$"Stage09 | D1:{Progress.d1} D2:{Progress.d2} D3:{Progress.d3}\nZip:{Progress.unpacked} Photo:{Progress.photoDelivered} Key:{Progress.keyObtained} NPC gone:{Progress.npcGone}\nUnlocked:{string.Join(",",Progress.unlocked)} Answer:{Rules.answerFileId}.{Rules.answerExtension}\nZoom:{(HomeViewer?HomeViewer.Level:0)} Head waiting:{WaitingForHead}");}
#endif
        void OnDestroy(){if(Files!=null)Files.Changed-=Changed;CloseMenu();if(headTarget)Destroy(headTarget.gameObject);if(pause)Destroy(pause);if(app){app.Dialogue.LineRevealed=null;app.Dialogue.Paused=false;}}
    }
    public sealed class Stage09DragSound:MonoBehaviour,IBeginDragHandler
    {
        public bool Key;public void OnBeginDrag(PointerEventData e){if(Key&&e.button==PointerEventData.InputButton.Left&&GetComponent<FakeIcon>().Draggable)GameApp.Instance.Audio.Play("sfx_key_drop");}
    }
    // 이미지의 실제 레터박스 영역을 기준으로 정규화 좌표를 변환한다. 부모 줌/팬/창 이동을 그대로 따른다.
    public sealed class Stage09Image:MonoBehaviour
    {
        Stage09Controller stage;ImageViewer viewer;CanvasGroup reveal;RectTransform door;bool sounded;
        public void Setup(Stage09Controller controller,ImageViewer image,FakeFileNode file)
        {
            stage=controller;viewer=image;var sprite=viewer.Picture.sprite;float w=590,h=410;float ratio=sprite.rect.width/sprite.rect.height;
            if(w/h>ratio)w=h*ratio;else h=w/ratio;float ox=(590-w)/2,oy=(410-h)/2;
            var hint=stage.Rules.hints.FirstOrDefault(x=>x.fileId==file.id);
            if(hint!=null){
                UIFactory.Text(viewer.PictureRoot,"·",ox+hint.x*w,oy+hint.y*h,20,20,12,Color.black);
                var label=UIFactory.Text(viewer.PictureRoot,stage.HintText(hint),ox+hint.x*w-55,oy+hint.y*h,160,32,10,Color.black);
                reveal=label.gameObject.AddComponent<CanvasGroup>();reveal.alpha=0;
            }
            if(file.id=="home"){
                var r=stage.Rules.doorRect;door=UIFactory.Rect(viewer.PictureRoot,"현관문 판정",ox+(r.x-r.width/2)*w,oy+(r.y-r.height/2)*h,r.width*w,r.height*h);
                var icon=UIFactory.Sprite(door,GameApp.Instance.Catalog.sprites.Get(stage.Rules.doorSprite),r.width*w*.25f,r.height*h*.2f,r.width*w*.5f,r.height*h*.6f);
                reveal=icon.gameObject.AddComponent<CanvasGroup>();reveal.alpha=0;
            }
        }
        void Update(){if(!reveal)return;bool visible=viewer.Level>=stage.Rules.revealZoom;reveal.alpha=Mathf.MoveTowards(reveal.alpha,visible?1:0,Time.unscaledDeltaTime*4);if(visible&&!sounded){sounded=true;GameApp.Instance.Audio.Play("sfx_zoom_reveal");}}
        public bool HitDoor(Vector2 screen,Camera camera)=>door&&viewer.Level>=stage.Rules.revealZoom&&RectTransformUtility.RectangleContainsScreenPoint(door,screen,camera);
        public RectTransform Door=>door;
    }
}
