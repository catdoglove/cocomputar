using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    // 경로는 창 생성 시 고정되어 뒤로/앞으로 이동해도 바뀌지 않는다.
    public sealed class FakeBrowserWindow:MonoBehaviour
    {
        public string Route{get;private set;} public string Location=>history[index].Address; public FakeWindow Window{get;private set;}
        sealed class Visit{public string Address;public float Scroll=1;public int Fault=-2;}
        readonly List<Visit> history=new();int index=-1;Stage08Controller stage;SiteData site;PageData page;
        RectTransform sheet,rootSheet;ScrollRect scroll;TMP_Text status,address,faultText;Button back,forward;float y;Coroutine delayed;string category="";
        public static FakeBrowserWindow Open(Stage08Controller stage,string route)
        {
            if(!FakeWindow.TryCreate(stage.App.Windows,"s8:browser:"+route,stage.Text("browserTitle"),out var w,65,65,900,660))return w.GetComponent<FakeBrowserWindow>();
            var self=w.gameObject.AddComponent<FakeBrowserWindow>();self.stage=stage;self.Route=route;self.Window=w;
            self.back=UIFactory.Button(w.Toolbar,"Back",0,1,90,30,()=>self.Travel(-1));self.forward=UIFactory.Button(w.Toolbar,"Forward",94,1,100,30,()=>self.Travel(1));
            UIFactory.Button(w.Toolbar,"Home",198,1,90,30,()=>self.Navigate(route+"_home/home"));UIFactory.Button(w.Toolbar,"Favorites",292,1,110,30,self.Favorites);
            foreach(var button in w.Toolbar.GetComponentsInChildren<Button>()) {var label=button.GetComponentInChildren<TMP_Text>();label.fontSize=17;label.textWrappingMode=TextWrappingModes.NoWrap;button.gameObject.AddComponent<BrowserLink>();}
            self.address=UIFactory.Text(w.Toolbar,"",412,3,465,28,17,Color.black);
            stage.Changed+=self.Refresh;stage.DateChanged+=self.Refresh;w.Closed+=()=>{stage.Changed-=self.Refresh;stage.DateChanged-=self.Refresh;};
            self.Navigate(route+"_home/home");return self;
        }
        public void Navigate(string target)
        {
            if(!stage.Ready)return;if(!target.Contains('/'))target=site.id+"/"+target;
            string id=target.Split('/')[0];var next=stage.Sites.FirstOrDefault(s=>s.id==id);
            if(next==null||(next.yourOnly&&Route!="your"))target="notfound/home";
            if(target.StartsWith("eye/")&&stage.EyeDone)target="notfound/home";
            Remember();if(index<history.Count-1)history.RemoveRange(index+1,history.Count-index-1);
            stage.App.Audio.Play("sfx_browser_nav");history.Add(new Visit{Address=target});index=history.Count-1;category="";Render();
        }
        void Remember(){if(index>=0&&scroll)history[index].Scroll=scroll.verticalNormalizedPosition;}
        public void Travel(int direction){if(!stage.Ready||index+direction<0||index+direction>=history.Count)return;Remember();index+=direction;category="";Render();}
        void Refresh(){if(!Window||Window.IsClosing||index<0)return;Remember();Render();}
        void Favorites()
        {
            if(!FakeWindow.TryCreate(stage.App.Windows,"s8:favorites:"+Route,stage.Text("favoritesTitle"),out var w,320,200,360,250))return;w.SetParentWindow(Window);
            var names=stage.Text("favorites").Split('|');var targets=new[]{"mart/home",Route+"_home/home","recipe/home"};
            for(int i=0;i<3;i++){int n=i;UIFactory.Button(w.Content,names[i],15,8+i*43,310,37,()=>{w.Close();Navigate(targets[n]);});}
        }
        void Render()
        {
            if(delayed!=null){StopCoroutine(delayed);delayed=null;}UIFactory.Clear(Window.Content);
            string[] parts=Location.Split('/');site=stage.Sites.First(s=>s.id==parts[0]);page=site.pages.FirstOrDefault(p=>p.id==parts[1])??site.pages[0];
            Window.SetTitle(site.title);address.text=site.domain+"/"+parts[1]+".html";back.interactable=index>0;forward.interactable=index<history.Count-1;
            var viewport=UIFactory.Panel(Window.Content,"사이트 스크롤",0,0,876,535,Color.white).rectTransform;viewport.gameObject.AddComponent<RectMask2D>();
            sheet=UIFactory.Rect(viewport,"사이트 본문",0,0,852,535);rootSheet=sheet;scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=sheet;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;
            var track=UIFactory.Panel(Window.Content,"스크롤바",856,0,20,535,new Color(.85f,.85f,.88f));var handle=UIFactory.Panel(track.transform,"손잡이",0,0,20,70,WindowSkin.Shadow);handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.sizeDelta=Vector2.zero;
            var bar=track.gameObject.AddComponent<Scrollbar>();bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;bar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=bar;
            status=UIFactory.Text(Window.Content,stage.Text("ready"),5,540,site.id=="candy"?460:860,25,16);
            if(site.id=="candy")UIFactory.Text(Window.Content,stage.Text("budget")+" "+(stage.Ordered?0:stage.Rules.budget).ToString("N0")+" / "+stage.Text("total")+" "+stage.CartTotal("candy").ToString("N0"),470,540,395,25,16);ColorUtility.TryParseHtmlString(site.color,out var color);
            UIFactory.Panel(sheet,"사이트 제목",6,6,840,65,color);Link(site.title,16,16,820,45,()=>Navigate(site.id+"/home"),Color.yellow,29);
            bool mini=site.id.EndsWith("_home");int count=site.menu.Length+(site.id=="your_home"?1:0);
            if(mini)
            {
                UIFactory.Panel(sheet,"왼쪽 메뉴",6,82,174,430,color);
                for(int i=0;i<site.menu.Length;i++){var item=site.menu[i];Link(item.label,18,100+i*45,153,36,()=>Navigate(item.target),Color.cyan,18);}
                if(site.id=="your_home")Link(" ",151,100+(site.menu.Length-1)*45,25,36,()=>Navigate("eye/home"));
                sheet=UIFactory.Rect(rootSheet,"페이지",192,88,852,535);sheet.localScale=Vector3.one*.76f;y=10;
            }
            else {for(int i=0;i<site.menu.Length;i++){var item=site.menu[i];Link(item.label,10+i*(830f/Math.Max(1,count)),78,825f/Math.Max(1,count),34,()=>Navigate(item.target));}y=126;}
            Heading(page.title);if(site.id=="candy"&&page.id=="home"&&stage.Ordered)Paragraph(stage.Text("orderOK"));
            if(site.id=="hospital"&&Route=="your")
            {
                LinkRow("ABOUT",()=>Navigate("hospital/about"));
                if(stage.Special.Progress.adminLoggedIn)LinkRow("관리자",()=>Navigate("hospital/admin"));
                if(parts[1]=="about")AdminLogin();
                else if(parts[1]=="admin"&&stage.Special.Progress.adminLoggedIn)AdminLibrary();
            }
            bool adminPage=site.id=="hospital"&&Route=="your"&&(parts[1]=="about"||parts[1]=="admin");
            var widgets=(adminPage?Array.Empty<WebWidget>():page.widgets??Array.Empty<WebWidget>()).Where(w=>!w.yourOnly||Route=="your").ToArray();if(widgets.Any(w=>w.kind=="album"))AlbumGrid(widgets.Where(w=>w.kind=="album").ToArray());foreach(var widget in widgets.Where(w=>w.kind!="album"))Widget(widget);
            if(site.id.EndsWith("_home")){LinkRow(stage.Text("homeBanner"),()=>Navigate("recipe/home"));LinkRow(stage.Text("diaryBanner"),()=>Navigate("plant/home"));LinkRow(stage.Text("mailBanner"),()=>Navigate("lost/home"));}
            if(site.id=="plant"&&Route=="your")LinkRow(stage.Text("weatherBanner"),()=>Navigate("weather/home"));
            if(site.id=="weather"&&Route=="your")LinkRow(stage.Text("newsAd"),()=>Navigate("news/home"));
            sheet.sizeDelta=new Vector2(852,Mathf.Max(535,y+30));if(mini)rootSheet.sizeDelta=new Vector2(852,Mathf.Max(535,88+(y+30)*.76f));Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=history[index].Scroll;foreach(var button in Window.GetComponentsInChildren<Button>())if(!button.GetComponent<BrowserLink>())button.gameObject.AddComponent<BrowserLink>();
        }
        void Heading(string text){UIFactory.Text(sheet,text,22,y,810,45,27,new Color(.1f,.08f,.45f));y+=54;}
        void AdminLogin()
        {
            Heading("관리자 로그인");
            UIFactory.Text(sheet,"아이디",28,y,130,36,21);var id=FileUI.Input(sheet,"",165,y,400,38);y+=50;
            UIFactory.Text(sheet,"비밀번호",28,y,130,36,21);var password=FileUI.Input(sheet,"",165,y,400,38);password.contentType=TMP_InputField.ContentType.Password;y+=50;
            var error=UIFactory.Text(sheet,"",28,y,790,60,20,Color.red);y+=70;
            LinkRow("로그인",()=>{if(stage.Special.Login(id.text,password.text))Navigate("hospital/admin");else error.text=stage.Special.Rules.loginFailure;});
        }
        void AdminLibrary()
        {
            Heading("환자 기록");
            foreach(var record in stage.Special.Rules.records){Heading(record.title);Paragraph(record.body);}
            Heading("자료실");foreach(var file in stage.Special.Rules.library)Paragraph(file+"  (열 수 없는 파일)");
            LinkRow("설치관리자.exe 다운로드",stage.Special.Download);
        }
        void Paragraph(string text)
        {
            var t=UIFactory.Text(sheet,text,25,y,800,50,20,Color.black);t.richText=false;float h=Mathf.Max(36,t.GetPreferredValues(text,800,100000).y+16);t.rectTransform.sizeDelta=new Vector2(800,h);y+=h+9;
        }
        Button Link(string text,float x,float yy,float width,float height,Action action,Color? color=null,int size=20)
        {
            var b=UIFactory.Button(sheet,text,x,yy,width,height,()=>{if(stage.Ready)action();},Color.clear);var label=b.GetComponentInChildren<TMP_Text>();label.fontSize=size;label.color=color??new Color(.05f,.1f,.65f);label.fontStyle=FontStyles.Bold|FontStyles.Underline;label.alignment=TextAlignmentOptions.MidlineLeft;b.gameObject.AddComponent<BrowserLink>();return b;
        }
        void LinkRow(string text,Action action){Link(text,28,y,790,38,action);y+=48;}
        void Picture(string key,float height=240){UIFactory.Sprite(sheet,stage.App.Catalog.sprites.Get(key),60,y,730,height);y+=height+15;}
        void Widget(WebWidget w)
        {
            switch(w.kind)
            {
                case "text":Paragraph(Stage08Validation.Diary(w));break;
                case "link":case "banner":LinkRow(w.text,()=>Activate(w));break;
                case "image":Picture(w.image);break;
                case "album":break;
                case "products":Products(w.target,w.limit);break;
                case "cart":Cart(w.target);break;
                case "reward":
                    if(w.target=="plank"&&!stage.Ordered){Paragraph(stage.Text("orderWait"));var locked=Link(w.text,28,y,790,38,()=>{});locked.interactable=false;locked.GetComponentInChildren<TMP_Text>().color=Color.gray;y+=48;break;}
                    Picture(w.image,140);LinkRow(w.text,()=>{if(!stage.Grant(w.target))Status(stage.Text("duplicate"));else Status(stage.Files.Find(w.target).displayName+" "+stage.Text("savedDesktop"));});break;
                case "globe":Globe();break;
                case "doll":Paragraph(stage.Text("dollIntro"));Paragraph(stage.Text("tokens")+" "+stage.Tokens);LinkRow(stage.Text("buy"),()=>Navigate("buy"));break;
                case "news":News();break;
                case "hospital":FileUI.Notice(stage.Text("hospital"),"s8:hospital-notice",false,stage.Text("notice"));Paragraph(stage.Text("hospital"));LinkRow(stage.Text("hospitalAd"),()=>stage.Notice("hospital"));break;
                case "missing":Paragraph(stage.Text("missingBody"));LinkRow(stage.Text("homeTitle"),()=>Navigate("notfound/home"));break;
                case "eye":Eye();break;
                case "notfound":NotFound();break;
            }
        }
        void AlbumGrid(WebWidget[] widgets)
        {
            float start=y;for(int i=0;i<widgets.Length;i++)
            {
                var widget=widgets[i];float x=20+i%3*275,yy=start+i/3*190;
                var image=UIFactory.Sprite(sheet,stage.App.Catalog.sprites.Get(widget.image),x,yy,255,140);image.raycastTarget=true;
                var button=image.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>{
                    var overlay=UIFactory.Panel(Window.Content,"확대 사진",0,0,876,535,new Color(.05f,.04f,.1f,.96f));
                    UIFactory.Sprite(overlay.transform,stage.App.Catalog.sprites.Get(widget.image),25,20,826,440);
                    UIFactory.Text(overlay.transform,widget.text,25,465,650,45,20,Color.white);
                    UIFactory.Button(overlay.transform,stage.Text("close"),735,475,110,35,()=>Destroy(overlay.gameObject));
                });image.gameObject.AddComponent<BrowserLink>();UIFactory.Text(sheet,widget.text,x,yy+145,255,40,17);
            }y=start+Mathf.Ceil(widgets.Length/3f)*190;
        }
        void Globe()
        {
            if(Route!="your"||stage.GlobeTaken)return;
            var globe=UIFactory.Sprite(sheet,stage.App.Catalog.sprites.Get("s8_globe"),30,y,110,110);globe.raycastTarget=true;globe.gameObject.AddComponent<Stage08Globe>();
            var button=globe.gameObject.AddComponent<Button>();globe.gameObject.AddComponent<BrowserLink>();
            button.onClick.AddListener(()=>stage.Confirm(stage.Text("download")+"\n"+stage.Files.Find("head").displayName,()=>stage.TakeGlobe(globe),stage.Text("downloadButton")));
            UIFactory.Text(sheet,"1989",160,y+35,200,45,30);y+=130;
        }
        void Activate(WebWidget w){if(w.action=="mail")Stage08MailWindow.Open(stage);else Navigate(w.target);}
        void Products(string shop,int limit)
        {
            if(shop=="candy")Paragraph(stage.Text("budget")+" "+(stage.Ordered?0:stage.Rules.budget).ToString("N0")+" / "+stage.Text("total")+" "+stage.CartTotal(shop).ToString("N0"));
            var available=stage.Products.Where(p=>p.shop==shop&&(!p.yourOnly||Route=="your")).ToArray();
            var categories=available.Select(p=>p.category).Distinct().ToArray();if(categories.Length>1){LinkRow(stage.Text("allProducts"),()=>{category="";Refresh();});foreach(var cat in categories){var c=cat;LinkRow(c,()=>{category=c;Refresh();});}}
            var list=available.Where(p=>string.IsNullOrEmpty(category)||p.category==category);if(limit>0)list=list.Take(limit);
            int i=0;float start=y;foreach(var product in list)
            {
                var p=product;float x=20+i%2*410,yy=start+i/2*220;UIFactory.Panel(sheet,"상품 "+p.id,x,yy,398,208,new Color(1,.98f,.88f));
                UIFactory.Sprite(sheet,stage.App.Catalog.sprites.Get(p.image),x+10,yy+10,100,100);
                Link(p.name,x+120,yy+8,265,48,()=>Detail(p));UIFactory.Text(sheet,p.price.ToString("N0"),x+120,yy+65,250,30,22,Color.magenta);
                UIFactory.Text(sheet,Route=="your"&&!string.IsNullOrEmpty(p.yourDescription)?p.yourDescription:p.description,x+12,yy+112,374,48,17,Color.black);
                var purchase=Link(shop=="doll"?stage.Text("buy"):shop=="bread"?stage.Text("details"):stage.Text("addCart"),x+15,yy+164,365,32,()=>{if(shop=="doll")stage.Buy(p);else if(shop=="bread")Detail(p);else if(stage.AddCart(p,1))Status(stage.Text("cartAdded"));});if(shop=="doll"&&p.id=="body"&&stage.Acquired.Contains("body")){purchase.interactable=false;purchase.GetComponentInChildren<TMP_Text>().text=stage.Text("soldOut");}i++;
            }
            y=start+Mathf.Ceil(i/2f)*220+10;
        }
        void Detail(WebProduct p)
        {
            if(p.shop=="doll"){stage.Buy(p);return;}
            if(!FakeWindow.TryCreate(stage.App.Windows,"s8:detail:"+Route+":"+p.id,p.name,out var w,245,170,540,430))return;w.SetParentWindow(Window);
            UIFactory.Sprite(w.Content,stage.App.Catalog.sprites.Get(p.image),15,10,180,170);
            UIFactory.Text(w.Content,Route=="your"&&!string.IsNullOrEmpty(p.yourDescription)?p.yourDescription:p.description,210,12,290,220,21,Color.black);
            UIFactory.Text(w.Content,p.name+" · "+p.price.ToString("N0"),20,195,480,40,20);
            if(p.action=="paint")UIFactory.Button(w.Content,stage.Text("colorful"),20,250,450,38,()=>stage.Confirm(stage.Text("download")+"\n"+stage.Files.Find("paint").displayName,()=>stage.Grant("paint"),stage.Text("downloadButton")));
            else if(p.action=="doll"&&Route=="your")UIFactory.Button(w.Content,stage.Text("eyeBread"),20,250,450,38,()=>{w.Close();Navigate("doll/home");});
        }
        void Cart(string shop)
        {
            Heading(stage.Text("cart"));foreach(var p in stage.Products.Where(p=>p.shop==shop&&stage.Cart.ContainsKey(p.id)).ToArray())
            {var product=p;int count=stage.Cart[p.id];Paragraph(p.name+" · "+count+" · "+(p.price*count).ToString("N0"));LinkRow("−",()=>stage.AddCart(product,-1));LinkRow("+",()=>stage.AddCart(product,1));}
            Paragraph(stage.Text("total")+" "+stage.CartTotal(shop).ToString("N0"));LinkRow(stage.Text("clearCart"),()=>stage.ClearCart(shop));LinkRow(stage.Text("order"),()=>stage.Order(shop));
        }
        void News()
        {
            var date=stage.App.DisplayDate??DateTime.Now;string special=date.Date==new DateTime(1989,6,6)?"1989":date.Date==new DateTime(1990,2,12)?"1990":null;
            if(special!=null)stage.App.Save.Flag("stage08.news"+special+"Seen");
            var articles=stage.Rules.news.Where(n=>special==null?n.id.StartsWith("normal"):n.id==special).ToArray();
            string requested=Location.Split('/')[1];var selected=articles.FirstOrDefault(n=>"article-"+n.id==requested);
            if(selected!=null){Heading(selected.title);Paragraph(selected.body);LinkRow(stage.Text("articleList"),()=>Navigate("news/home"));}
            else foreach(var article in articles){var item=article;Heading(item.title);LinkRow(stage.Text("readArticle"),()=>Navigate("news/article-"+item.id));}
            LinkRow(stage.Text("hospitalAd"),()=>Navigate("hospital/home"));
        }
        void NotFound()
        {
            Heading(stage.Text("notfound"));faultText=UIFactory.Text(sheet,stage.Text("notfoundBody"),25,y,800,100,20,Color.black);y+=115;
            LinkRow(stage.Text("homeTitle"),()=>Navigate(Route+"_home/home"));
            var visit=history[index];if(visit.Fault==-2)visit.Fault=UnityEngine.Random.value<stage.Rules.notFoundChance?UnityEngine.Random.Range(0,4):-1;if(visit.Fault>=0)delayed=StartCoroutine(Fault(visit.Fault));
        }
        IEnumerator Fault(int kind)
        {
            yield return new WaitForSecondsRealtime(stage.Rules.glitchDelay);
            if(kind==1)faultText.text=stage.Text("notfoundChanged");
            else if(kind==2)
            {
                for(int i=0;i<40;i++){var silhouette=UIFactory.Sprite(sheet,stage.App.Catalog.sprites.Get("portrait_npc_stage8"),15+i%10*83,y+i/10*100,75,95);silhouette.color=new Color(.12f,.12f,.15f,.7f);}y+=400;
            }
            else if(kind==3)Paragraph(string.Join("\n",Enumerable.Repeat(stage.Text("notfoundBody"),24)));
            else
            {
                var origin=faultText.rectTransform.anchoredPosition;string text=stage.Text("notfoundBody");
                for(int i=0;i<12;i++){var chars=text.ToCharArray();for(int j=i%3;j<chars.Length;j+=3)chars[j]='ㅁ';faultText.text=new string(chars);faultText.color=i%2==0?new Color(.4f,.05f,.2f):Color.black;faultText.rectTransform.anchoredPosition=origin+Vector2.right*(i%2==0?3:-3);yield return new WaitForSecondsRealtime(.12f);}
                faultText.text=text.Replace("페이지","페ㅁ지");faultText.color=Color.black;faultText.rectTransform.anchoredPosition=origin;
            }
            sheet.sizeDelta=new Vector2(852,Mathf.Max(535,y+20));
        }

        void Eye()
        {
            int correct=UnityEngine.Random.Range(0,stage.Rules.eyeCount);float start=y;
            for(int i=0;i<stage.Rules.eyeCount;i++){int n=i;float x=16+i%8*103,yy=start+i/8*67;var button=Link("",x+UnityEngine.Random.Range(-5,5),yy+UnityEngine.Random.Range(-4,4),85,54,()=>{if(n==correct&&!stage.EyeDone)StartCoroutine(EyeEvent());},Color.red,40);var eye=UIFactory.Rect(button.transform,"눈 그래픽",0,0,85,54).gameObject.AddComponent<Stage08Eye>();eye.raycastTarget=false;eye.Trembles=i==correct;}y+=Mathf.Ceil(stage.Rules.eyeCount/8f)*67;
        }
        IEnumerator EyeEvent()
        {
            stage.BeginEyeEvent(Window);yield break;
        }
        public void Status(string text){if(status)status.text=text;StartCoroutine(ClearStatus(text));}
        IEnumerator ClearStatus(string text){yield return new WaitForSecondsRealtime(stage.Rules.statusSeconds);if(status&&status.text==text)status.text=stage.Text("ready");}
    }
    public sealed class BrowserLink:MonoBehaviour{}
    public sealed class Stage08EyeJitter:MonoBehaviour
    {Vector3 origin;void Start(){origin=transform.localPosition;}void Update(){transform.localPosition=origin+new Vector3(Mathf.Sin(Time.unscaledTime*31)*1.5f,Mathf.Cos(Time.unscaledTime*23),0);}}
}
