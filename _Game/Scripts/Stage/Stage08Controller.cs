using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CoComputar
{
    // 스테이지 진입마다 새 인스턴스. 선택 이력만 SaveService에 남긴다.
    public sealed class Stage08Controller:MonoBehaviour,IStageEntry
    {
        public Stage08Rules Rules{get;private set;} public SiteData[] Sites{get;private set;} public WebProduct[] Products{get;private set;}
        public FakeFileSystem Files{get;private set;} public readonly List<MailMessage> Messages=new();
        public readonly Dictionary<string,int> Cart=new(); public readonly HashSet<string> Acquired=new();
        public bool Ordered,EyeDone,GlobeTaken,NpcCreated,Busy; public int Tokens{get;private set;}
        public Stage08SpecialRoute Special{get;private set;}
        public void RestoreTokens(int value){Tokens=Mathf.Max(0,value);}
        public event Action Changed,MailChanged,DateChanged; StageManager manager;RectTransform desktop;FilePopup menu;UnityEngine.UI.Image eventBlock;
        public GameApp App=>GameApp.Instance; public bool Ready=>!Busy&&!App.Dialogue.Active&&manager.State!=StageState.Transition;
        public string Text(string id)=>Rules.Text(id);
        public void Enter(StageManager mgr,StageDefinition definition)
        {
            manager=mgr;var a=Resources.Load<Stage08Assets>("Stage08Assets");
            Rules=JsonUtility.FromJson<Stage08Rules>(a.rules.text);Sites=JsonUtility.FromJson<SiteDocument>(a.sites.text).sites;
            Products=JsonUtility.FromJson<ProductDocument>(a.products.text).products;Files=new FakeFileSystem(JsonUtility.FromJson<FakeFileData>(a.files.text));
            Messages.AddRange(JsonUtility.FromJson<MailDocument>(a.mail.text).messages);Tokens=Rules.startTokens;
            if(Stage08Validation.CandyWays(Products,Rules.budget,Rules.maxQuantity)==0||!Products.Where(p=>p.shop=="doll").All(p=>Stage08Validation.DollValid(p,Tokens)))throw new InvalidOperationException("Stage08 상품 가격 데이터 검증 실패");
            App.SetFileRightClick(true);App.DesktopSettingsClicked=App.OpenOptions;
            App.ClockClicked=()=>{if(Ready)DateTimeWindow.Open(d=>{App.DisplayDate=d;DateChanged?.Invoke();},App.DisplayDate??DateTime.Now);};
            App.Audio.Music("bgm_stage08");desktop=UIFactory.Rect(App.ScreenRoot,"Stage08 바탕화면",0,38,1024,730);
            Special=gameObject.AddComponent<Stage08SpecialRoute>();Special.Setup(this,desktop,a);
            Files.Changed+=RefreshDesktop;RefreshDesktop();
        }
        public void RefreshDesktop()
        {
            UIFactory.Clear(desktop);foreach(var n in Files.Children("desktop").ToArray())DrawIcon(desktop,n,n.x,n.y);
            Special.Draw();if(Special.Locked)return;
            // 테스트용 이동 버튼. 스테이지8 바탕화면에서만 표시한다.
            var back=UIFactory.Button(desktop,"테스트: 스테이지7로 돌아가기",0,0,310,36,()=>
            {
                if(!Ready)return;
                CloseMenu();App.Save.Enter("Stage07");App.Begin(false);
            });
            var rect=(RectTransform)back.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);
            rect.anchoredPosition=new Vector2(-24,24);
        }
        public FakeIcon DrawIcon(Transform parent,FakeFileNode n,float x,float y)
        {
            var icon=FakeIcon.Create(parent,"s8:"+n.id,n.displayName,n.iconKey,x,y,()=>Open(n.id),true,n.hidden);
            icon.ScreenBoundDrag=true;icon.DropWithoutTarget=e=>{if(n.parentFolder!="desktop")return;RectTransformUtility.ScreenPointToLocalPointInRectangle(desktop,e.position,e.pressEventCamera,out var point);n.x=Mathf.Clamp(point.x-46,0,932);n.y=Mathf.Clamp(-point.y-44,0,625);icon.Rect.anchoredPosition=new Vector2(n.x,-n.y);};
            var target=icon.gameObject.AddComponent<DropTarget>();target.Category="s8:"+n.id;
            icon.Dropped=(source,destination)=>{if(Ready)Merge(n.id,destination.Category.Replace("s8:",""));};
            icon.gameObject.AddComponent<FileRightClick>().Click=e=>{if(Ready)Context(n,e);};return icon;
        }
        void Context(FakeFileNode n,UnityEngine.EventSystems.PointerEventData e)
        {
            if(Special.Locked){Special.Denied();return;}
            CloseMenu();var block=UIFactory.Panel(App.Modal,"파일 메뉴",0,0,1024,768,Color.clear);menu=block.gameObject.AddComponent<FilePopup>();block.gameObject.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(CloseMenu);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(App.Modal,e.position,e.pressEventCamera,out var pos);
            var panel=UIFactory.Panel(block.transform,"메뉴",Mathf.Clamp(pos.x,0,815),Mathf.Clamp(-pos.y,35,565),205,158,new Color(.76f,.76f,.78f));
            var ids=new[]{"extract","rename","delete","restore"};for(int i=0;i<ids.Length;i++){string key=ids[i];var b=UIFactory.Button(panel.transform,Text(key),3,3+i*38,199,35,()=>{CloseMenu();Extract(n.id);});b.interactable=i==0&&n.extension=="zip";}
        }
        void CloseMenu(){if(menu)menu.Close();menu=null;}
        public void Open(string id)
        {
            if(!Ready)return;CloseMenu();var n=Files.Find(id);if(n==null||!Files.CanUse(n))return;
            if(Special.Open(id))return;
            if(id=="my"||id=="your")
            {
                if(id=="your"&&!App.Save.Progress.flags.Contains("stage07.secretBrowserInstalled")){Notice("noBrowser");return;}
                FakeBrowserWindow.Open(this,id);return;
            }
            if(id=="mail"){Stage08MailWindow.Open(this);return;}
            if(id=="door"){manager.RequestClear("door.exe");return;}
            if(n.contentType=="folder"){OpenFolder(id);return;}
            if(n.extension=="zip"){Notice("zip");return;}
            if(n.contentType=="image")WindowApps.Picture(n.displayName,n.imageKey);else WindowApps.Note(n.displayName,n.content,"s8:"+id);
        }
        public void OpenFolder(string id)
        {
            var node=Files.Find(id);if(node==null||!Files.CanUse(node))return;
            if(!FakeWindow.TryCreate(App.Windows,"s8:folder:"+id,node.displayName,out var w,180,140,650,450))return;
            w.SetFileBackground();void Draw(){if(!w)return;UIFactory.Clear(w.Content);int i=0;foreach(var n in Files.Children(id).Where(n=>n.IsVisible(w.ShowHidden)))DrawIcon(w.Content,n,15+i%4*145,15+i++/4*125);}
            Files.Changed+=Draw;w.Closed+=()=>Files.Changed-=Draw;w.AddHiddenToggle(Draw);Draw();
        }
        public void Extract(string id){if(id=="paint"&&Files.ExtractArchive(id,"colors"))App.Audio.Play("sfx_key_get");}
        public bool Grant(string id)
        {
            var n=Files.Find(id);if(n==null||!Acquired.Add(id))return false;Files.Reveal(id);App.Audio.Play("sfx_download");Changed?.Invoke();return true;
        }
        public void Notice(string id)=>FileUI.Notice(Text(id),"s8:notice",title:Text("warning"));
        public void Confirm(string text,Action accept,string acceptLabel=null)
        {
            if(App.Windows.GetComponentsInChildren<FakeWindow>().Any(x=>x.Key=="s8:confirm"&&!x.IsClosing))return;
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
            var blocker=UIFactory.Panel(App.Windows,"확인 입력 잠금",0,0,1024,768,Color.clear);
            FakeWindow.TryCreate(blocker.transform,"s8:confirm",Text("warning"),out var w,280,240,470,240);w.Closed+=()=>Destroy(blocker.gameObject);
            UIFactory.Text(w.Content,text,18,10,420,75,20);UIFactory.Button(w.Content,acceptLabel??Text("confirm"),70,90,130,34,()=>{w.Close();if(Ready)accept();});
            UIFactory.Button(w.Content,Text("cancel"),250,90,130,34,w.Close);
        }
        public int CartTotal(string shop)=>Products.Where(p=>p.shop==shop&&Cart.ContainsKey(p.id)).Sum(p=>p.price*Cart[p.id]);
        public bool AddCart(WebProduct p,int delta)
        {
            if(p.shop=="candy"&&Ordered){Notice("orderDone");return false;}
            int count=Cart.TryGetValue(p.id,out int v)?v:0,next=Mathf.Clamp(count+delta,0,Rules.maxQuantity);
            if(delta>0&&count==next){Notice("maxQuantity");return false;}
            if(p.shop=="candy"&&CartTotal("candy")+(next-count)*p.price>Rules.budget){Notice("over");return false;}
            if(next==0)Cart.Remove(p.id);else Cart[p.id]=next;if(delta>0)App.Audio.Play("sfx_cart_add");Changed?.Invoke();return true;
        }
        public void ClearCart(string shop){foreach(var p in Products.Where(p=>p.shop==shop))Cart.Remove(p.id);Changed?.Invoke();}
        public void Order(string shop)
        {
            if(shop!="candy"){Notice("martDenied");return;}if(Ordered){Notice("orderDone");return;}
            if(CartTotal(shop)!=Rules.budget){Notice(CartTotal(shop)<Rules.budget?"under":"over");return;}
            Ordered=true;ClearCart(shop);App.Audio.Play("sfx_order_ok");Notice("orderOK");Changed?.Invoke();
        }
        public void Buy(WebProduct p)
        {
            if(p.freeBlocked){Notice("ribbon");return;}
            if(p.id=="body"&&Acquired.Contains("body")){Notice("soldOut");return;}
            int cost=Stage08Validation.Total(p);
            if(p.category=="인형 몸"&&p.id!="body"){Notice("tokens");return;}
            Confirm(Text("confirmBuy")+"\n"+p.name+" · "+p.price+" + "+Text("tax")+" "+(cost-p.price)+" = "+cost,()=>{if(cost>Tokens||Tokens==0){Notice("tokens");return;}if(p.id=="body"&&!Acquired.Contains("body")){Tokens-=cost;Grant("body");}});
        }
        public void Merge(string a,string b)
        {
            if(!Ready||Special.Locked||NpcCreated||!(a=="head"&&b=="body"||a=="body"&&b=="head"))return;
            if(!Files.CanUse(Files.Find("head"))||!Files.CanUse(Files.Find("body")))return;
            StartCoroutine(MergeParts());
        }
        IEnumerator MergeParts()
        {
            Busy=true;var block=UIFactory.Panel(App.Modal,"결합 입력 잠금",0,0,1024,768,Color.clear);
            Files.Find("head").available=Files.Find("body").available=false;Files.Notify();
            yield return IconMerge.Play(block.transform,App.Catalog.sprites.Get("s8_globe"),App.Catalog.sprites.Get("s8_doll"),App.Audio);
            NpcCreated=true;App.Save.Flag("stage08.npcCreated");Files.Notify();Destroy(block.gameObject);Busy=false;
        }
        public void BeginEyeEvent(FakeWindow owner)
        {
            if(!Ready||EyeDone)return;Busy=true;EyeDone=true;App.Save.Flag("stage08.eyeEventDone");StartCoroutine(EyeEvent(owner));
        }
        IEnumerator EyeEvent(FakeWindow owner)
        {
            App.Audio.PauseEventMusic();eventBlock=UIFactory.Panel(App.Modal,"눈 이벤트 입력 잠금",0,0,1024,768,Color.black);
            var pill=UIFactory.Sprite(eventBlock.transform,App.Catalog.sprites.Get("s8_pill"),320,240,384,280);
            for(float t=0;t<Rules.eyeSeconds;t+=Time.unscaledDeltaTime){pill.color=new Color(1,1,1,Mathf.Sin(t/Rules.eyeSeconds*Mathf.PI));yield return null;}
            Destroy(eventBlock.gameObject);eventBlock=null;Busy=false;App.Audio.ResumeEventMusic();if(owner&&!owner.IsClosing)owner.Close();
        }
        public bool Send(string address,string body,IEnumerable<string> attachments)
        {
            if(!string.Equals(address.Trim(),Rules.replyAddress,StringComparison.OrdinalIgnoreCase)){Notice("badAddress");return false;}
            if(body!=Rules.autoTypeText){Notice("emptyBody");return false;}
            var ids=attachments.Distinct().ToArray();Messages.Add(new MailMessage{id=Guid.NewGuid().ToString(),folder="sent",sender=Text("mailSelf"),recipient=address,subject=Text("replyTitle"),body=body,attachments=ids,date=(App.DisplayDate??DateTime.Now).ToString("yyyy.MM.dd HH:mm"),read=true});
            App.Audio.Play("sfx_mail_send");MailChanged?.Invoke();StartCoroutine(Reply(ids));return true;
        }
        IEnumerator Reply(string[] ids)
        {
            yield return new WaitForSecondsRealtime(Rules.replySeconds);bool correct=Stage08Validation.CorrectAttachments(ids);
            Messages.Add(new MailMessage{id=Guid.NewGuid().ToString(),folder="inbox",sender=Rules.replyAddress,recipient=Text("mailSelf"),subject=Text("replyTitle"),body=Text(correct?"replyOK":"wrongFiles"),attachment=correct?"door":null,date=DateTime.Now.ToString("yyyy.MM.dd HH:mm")});
            App.Audio.Play("sfx_mail_new");MailChanged?.Invoke();FileUI.Notice(Text("newMail"),"s8:newmail",false,Text("mail"));
        }
        public void TakeGlobe(UnityEngine.UI.Image globe)
        {
            if(GlobeTaken)return;GlobeTaken=true;StartCoroutine(EraseGlobe(globe));
        }
        IEnumerator EraseGlobe(UnityEngine.UI.Image globe)
        {
            if(globe){globe.type=UnityEngine.UI.Image.Type.Filled;globe.fillMethod=UnityEngine.UI.Image.FillMethod.Vertical;globe.fillOrigin=0;globe.raycastTarget=false;}
            for(float t=0;t<Rules.globeEraseSeconds;t+=Time.unscaledDeltaTime){if(globe)globe.fillAmount=1-t/Rules.globeEraseSeconds;yield return null;}
            if(globe)Destroy(globe.gameObject);Grant("head");
        }
        public void NotifyChange()=>Changed?.Invoke();
        void OnDestroy(){CloseMenu();if(eventBlock){Destroy(eventBlock.gameObject);App.Audio.ResumeEventMusic();}if(Files!=null)Files.Changed-=RefreshDesktop;}
    }
}
