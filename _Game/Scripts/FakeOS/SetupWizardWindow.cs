using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace CoComputar
{
    public sealed class SerialDialog:MonoBehaviour
    {
        public FakeWindow Window;public TMP_InputField Input;Stage07Controller stage;SetupWizardData data;
        public static string Normalize(string value)
        {
            var letters=new string(value.ToUpperInvariant().Where(c=>c>='A'&&c<='Z').Take(3).ToArray());
            var digits=new string(value.Where(c=>c>='0'&&c<='9').Take(3).ToArray());return letters+(letters.Length==3?"-"+digits:"");
        }
        public static void Open(Stage07Controller stage,SetupWizardData data)
        {
            if(!FakeWindow.TryCreate(stage.App.Windows,"s7:serial","시리얼 코드 입력",out var w,250,200,525,315))return;
            var self=w.gameObject.AddComponent<SerialDialog>();self.Window=w;self.stage=stage;self.data=data;
            UIFactory.Text(w.Content,$"<color={data.serialColor}>시리얼 코드</color>를 입력하세요.",20,5,465,38,23);
            UIFactory.Text(w.Content,"대상: "+data.name,20,48,465,28,18);
            self.Input=FileUI.Input(w.Content,"",30,90,440,40);self.Input.characterLimit=7;
            self.Input.onValidateInput=(text,pos,c)=>(c>='A'&&c<='Z'||c>='a'&&c<='z')?char.ToUpperInvariant(c):c>='0'&&c<='9'||c=='-'?c:'\0';
            string previous="";self.Input.onValueChanged.AddListener(value=>{if(previous.Length==4&&previous.EndsWith("-")&&value==previous.Substring(0,3))value=value.Substring(0,2);var s=Normalize(value);previous=s;if(value!=s){self.Input.SetTextWithoutNotify(s);self.Input.caretPosition=s.Length;}});
            var placeholder=UIFactory.Text(self.Input.textViewport,"AAA-000",0,0,420,34,20,Color.gray);self.Input.placeholder=placeholder;
            UIFactory.Button(w.Content,"확인",185,160,130,38,self.Submit);self.Input.ActivateInputField();
        }
        public void Submit()
        {
            if(!stage.Ready||Window.IsClosing)return;
            if(Input.text!=stage.Randoms.Serial(data.id)){stage.App.Audio.Play("sfx_serial_wrong");var notice=FileUI.Notice(stage.Text("시리얼_오답"),"s7:serial-error",false,"경고");notice.SetParentWindow(Window);notice.Closed+=()=>{if(Input)Input.ActivateInputField();};return;}
            Window.Close();SetupWizardWindow.Open(stage,data);
        }
        void Update(){if(!stage.Ready||Window.transform.GetSiblingIndex()!=Window.transform.parent.childCount-1)return;var k=Keyboard.current;if(k?.enterKey.wasPressedThisFrame==true)Submit();else if(k?.escapeKey.wasPressedThisFrame==true)Window.Close();}
    }
    public sealed class SetupWizardWindow:MonoBehaviour
    {
        public FakeWindow Window;public SetupWizardData Data;public int Step{get;private set;}=1;
        public bool Agree,SecretOption,RunAfter=true;public readonly HashSet<string> Components=new();
        public bool ChoosingComponents{get;private set;}
        public bool DefaultInstall=true,Shortcut=true,StartMenu=true;public float Progress{get;private set;}
        Stage07Controller stage;RectTransform page;Button previous,next,cancel;TMP_Text progressText,currentFile;Image[] blocks;bool modal,failed;float elapsed;string lastFile;
        Stage08SpecialRoute special;
        public static void OpenSpecial(Stage08SpecialRoute route)
        {
            var app=GameApp.Instance;
            if(!FakeWindow.TryCreate(app.Windows,"deep:wizard","설치 관리자",out var w,70,55,885,655))return;
            var self=w.gameObject.AddComponent<SetupWizardWindow>();self.Window=w;self.special=route;
            self.BuildFrame(app);self.DrawSpecial();
        }
        public static void Open(Stage07Controller stage,SetupWizardData data)
        {
            if(!stage.Ready)return;
            if(!FakeWindow.TryCreate(stage.App.Windows,"s7:wizard",data.installer,out var w,70,55,885,655))return;
            var self=w.gameObject.AddComponent<SetupWizardWindow>();self.Window=w;self.stage=stage;self.Data=data;stage.WizardStarted=true;
            self.BuildFrame(stage.App);self.Draw();
        }
        void BuildFrame(GameApp app)
        {
            var w=Window;var self=this;
            // Custom map installation starts with every component unchecked.
            // 공통 창의 이동/열기/종료는 재사용하고 설치 마법사만 고전 파란색 외형을 적용한다.
            var title=w.transform.Find("제목 표시줄");var titleImage=title.GetComponent<RawImage>();titleImage.texture=Texture2D.whiteTexture;titleImage.color=new Color(0,0,.5f);
            foreach(var image in w.transform.Find("배경").GetComponentsInChildren<Image>())image.color=new Color(.76f,.76f,.76f);
            w.Toolbar.gameObject.SetActive(false);w.Content.localScale=Vector3.one;w.Content.anchoredPosition=new Vector2(12,-48);w.Content.sizeDelta=new Vector2(861,593);
            var left=UIFactory.Panel(w.Content,"설치 일러스트",0,0,230,530,new Color(.02f,.15f,.49f));
            UIFactory.Sprite(left.transform,app.Catalog.sprites.Get("computer"),35,90,160,150);UIFactory.Text(left.transform,"Co-computar.",20,245,200,45,23,Color.white);
            UIFactory.Sprite(left.transform,app.Catalog.sprites.Get("icon_floppy"),95,350,95,95);UIFactory.Text(left.transform,special?"MEMORY / 08":"MEMORY / 07",22,470,205,30,18,Color.white);
            self.page=UIFactory.Panel(w.Content,"페이지",232,0,629,530,Color.white).rectTransform;
            self.previous=UIFactory.Button(w.Content,"< 이전",355,547,160,38,self.Previous,new Color(.76f,.76f,.76f));
            self.next=UIFactory.Button(w.Content,"다음 >",520,547,160,38,self.Next,new Color(.76f,.76f,.76f));
            self.cancel=UIFactory.Button(w.Content,"취소",700,547,150,38,self.Cancel,new Color(.76f,.76f,.76f));
            var x=title.Find("X").GetComponent<Button>();x.onClick.RemoveAllListeners();x.onClick.AddListener(self.Cancel);
        }
        void DrawSpecial()
        {
            UIFactory.Clear(page);previous.gameObject.SetActive(Step<5);previous.interactable=Step>1;
            cancel.gameObject.SetActive(Step<6);next.interactable=Step!=5&&(Step!=2||Agree);
            next.GetComponentInChildren<TMP_Text>().text=Step==6?"마침":Step>=2&&Step<=4?"동의":"다음 >";
            Window.transform.Find("제목 표시줄/X").gameObject.SetActive(Step<6);
            Text(Step==1?"설치 시작":Step==4?"경고!":Step<5?"사용권 계약":Step==5?"설치 진행":"설치 완료",28,15,565,60,35);
            if(Step==1)Text("설치 관리자를 시작합니다.\n\n계속하려면 다음을 누르십시오.",28,105,565,300,25);
            else if(Step<5)
            {
                var warning=Text(special.Rules.warnings[Step-2],28,105,565,300,25);if(Step==4)warning.color=Color.red;
                if(Step==2)Choice("동의합니다",Agree,430,()=>{Agree=!Agree;DrawSpecial();});
            }
            else if(Step==5)
            {
                var bar=UIFactory.Panel(page,"진행 바",28,175,560,37,new Color(.8f,.8f,.8f));blocks=new Image[25];
                for(int i=0;i<blocks.Length;i++){blocks[i]=UIFactory.Panel(bar.transform,"블록 "+i,3+i*22.2f,3,19,31,new Color(0,0,.55f));blocks[i].enabled=false;}
                progressText=Text("0%",28,225,565,50,28);currentFile=Text("",28,290,565,120);
            }
            else Text("설치가 완료되었습니다.",28,105,565,300,25);
        }
        TMP_Text Text(string text,float x,float y,float width,float height,float size=21)=>UIFactory.Text(page,text,x,y,width,height,size,Color.black);
        Button Choice(string label,bool chosen,float y,Action click,float x=25,float width=570,bool radio=true)
        {var button=UIFactory.Button(page,(radio?(chosen?"● ":"○ "):(chosen?"[x] ":"[ ] "))+label,x,y,width,32,click,Color.white);var text=button.GetComponentInChildren<TMP_Text>();text.alignment=TextAlignmentOptions.MidlineLeft;text.fontSize=width<400?18:20;return button;}
        void Draw()
        {
            UIFactory.Clear(page);previous.interactable=Step==2||Step==3;next.interactable=Step==1||Step==3||Step==5||Step==2&&Agree;cancel.gameObject.SetActive(Step!=5);
            next.GetComponentInChildren<TMP_Text>().text=Step==5?"마침":"다음 >";
            string key=new[]{"설치_시작_제목","사용권_제목","설정_제목","진행_제목","완료_제목"}[Step-1];Text(ChoosingComponents?"설치 유형":stage.Text(key),28,15,565,60,35);
            if(Step==1)Text(stage.Text("설치_시작_본문").Replace("{프로그램명}",Data.name),28,105,565,375,25);
            else if(Step==2)Contract();
            else if(Step==3){if(ChoosingComponents)ComponentSettings();else Settings();}
            else if(Step==4)
            {
                Text(stage.Text("진행_상태"),28,110,560,42,25);var bar=UIFactory.Panel(page,"진행 바",28,175,560,37,new Color(.8f,.8f,.8f));blocks=new Image[25];
                for(int i=0;i<blocks.Length;i++)blocks[i]=UIFactory.Panel(bar.transform,"블록 "+i,3+i*22.2f,3,19,31,new Color(0,0,.55f));
                progressText=Text("",28,225,560,35);currentFile=Text("",28,280,560,80);Text(stage.Text("진행_안내"),28,405,560,90);RefreshProgress();
            }
            else
            {
                Text(stage.Text("완료_본문").Replace("{프로그램명}",Data.name),28,105,565,280,23);
                if(Data.id!="internet")Choice(stage.Text("완료_실행"),RunAfter,420,()=>{RunAfter=!RunAfter;Draw();},25,570,false);
            }
        }
        void Contract()
        {
            Text(stage.Text("사용권_안내"),25,77,580,40,18);
            bool internet=Data.id=="internet";string contract=internet?(stage.SecretUnlocked?stage.Assets.secretContract.text:stage.Assets.longContract.text):stage.Assets.shortContract.text;
            var body=FileUI.ScrollText(page,contract,25,123,580,295,Color.black);body.fontSize=19;
            var scroll=body.GetComponentInParent<ScrollRect>();var content=UIFactory.Rect(scroll.viewport,"계약 내용",0,0,540,1);body.rectTransform.SetParent(content,false);body.rectTransform.anchoredPosition=new Vector2(8,-4);
            float height=body.GetPreferredValues(contract,530,100000).y+22;body.rectTransform.sizeDelta=new Vector2(530,height);content.sizeDelta=new Vector2(540,Mathf.Max(295,height+(internet?40:0)));scroll.content=content;scroll.verticalNormalizedPosition=1;
            if(internet&&!stage.SecretUnlocked)
            {
                var more=UIFactory.Text(content,"더보기",8,height,180,34,19,Color.black);more.raycastTarget=true;
                var click=more.gameObject.AddComponent<Button>();click.transition=Selectable.Transition.None;click.onClick.AddListener(()=>{if(!stage.Ready)return;stage.SecretUnlocked=true;Draw();});
                more.gameObject.AddComponent<WizardLinkCursor>();
            }
            if(internet&&stage.SecretUnlocked)
            {
                Button option=null;
                option=UIFactory.Button(content,(SecretOption?"[x] ":"[ ] ")+"시크릿 브라우저 설치(S)",8,height,520,32,()=>
                {
                    if(!stage.Ready)return;SecretOption=!SecretOption;
                    option.GetComponentInChildren<TMP_Text>().text=(SecretOption?"[x] ":"[ ] ")+"시크릿 브라우저 설치(S)";
                },Color.white);
                option.name="시크릿 브라우저 설치";
                var label=option.GetComponentInChildren<TMP_Text>();label.alignment=TextAlignmentOptions.MidlineLeft;label.fontSize=19;
            }
            UIFactory.Button(page,"▲",574,123,30,27,()=>scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition+.1f));
            UIFactory.Button(page,"▼",574,391,30,27,()=>scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition-.1f));
            Choice(stage.Text("사용권_동의"),Agree,439,()=>{Agree=true;Draw();});Choice(stage.Text("사용권_비동의"),!Agree,479,()=>{Agree=false;Draw();});
        }
        void Settings()
        {
            Text(stage.Text("설정_안내"),25,76,580,43,19);Text("설치 폴더(D)",25,119,500,30);
            Text("C:\\Program Files\\"+Data.name,35,158,390,37,20);UIFactory.Button(page,"찾아보기(B)...",430,155,170,35,()=>Notice(stage.Text("찾아보기_경고")));
            Text("설치 종류",25,210,560,32,23);Choice("기본 설치(T)",DefaultInstall,250,()=>{DefaultInstall=true;Draw();});Text("일반적인 구성 요소를 설치합니다.",55,285,530,30,18);
            if(Data.id!="viewer"){Choice("사용자 지정 설치(C)",!DefaultInstall,325,()=>{DefaultInstall=false;Draw();});Text("설치할 구성 요소를 선택할 수 있습니다.",55,360,530,30,18);}
            Choice("바탕 화면 바로 가기 만들기(S)",Shortcut,443,()=>{Shortcut=!Shortcut;Draw();},20,370,false);
            Choice("시작 메뉴 등록(M)",StartMenu,480,()=>{StartMenu=!StartMenu;Draw();},20,270,false);
        }
        void ComponentSettings()
        {
            Text("프로그램을 설치할 항목을 선택하십시오.",25,77,580,43,19);
            var description=Text("항목을 선택하여 설명을 확인하세요.",30,345,570,65,18);
            for(int i=0;i<stage.Data.components.Length;i++)
            {
                var c=stage.Data.components[i];Button button=null;
                button=UIFactory.Button(page,(Components.Contains(c.id)?"[✓] ":"[ ] ")+c.label,30,145+i*36,560,32,()=>{if(!Components.Remove(c.id))Components.Add(c.id);button.GetComponentInChildren<TMP_Text>().text=(Components.Contains(c.id)?"[✓] ":"[ ] ")+c.label;description.text=c.description;},Color.white);
            }
        }
        public void Next()
        {
            if(special)
            {
                if(Window.IsClosing||Step==5||Step==2&&!Agree)return;
                if(Step==6){Window.Close();GameApp.Instance.CloseAllWindows();return;}
                Step++;elapsed=0;GameApp.Instance.Audio.PlayCapped(Step>=2&&Step<=4?"sfx_install_warn":"sfx_wizard_next",special.Rules.sfxCap);DrawSpecial();return;
            }
            if(!stage.Ready||modal||Window.IsClosing||Step==4)return;
            if(Step==2&&!Agree)return;
            if(ChoosingComponents&&Components.Count==0){Notice(stage.Text("구성요소_없음"));return;}
            stage.App.Audio.Play("sfx_wizard_next");
            if(Step==5){bool run=RunAfter&&Data.id!="internet";Window.Close();if(run)stage.Launch(Data.id);return;}
            if(Step==3&&Data.id=="map")
            {
                if(!DefaultInstall&&!ChoosingComponents){ChoosingComponents=true;Draw();return;}
                // Basic installation always uses the original all-components preset.
                if(DefaultInstall){Components.Clear();foreach(var c in stage.Data.components)Components.Add(c.id);}
            }
            ChoosingComponents=false;
            Step++;if(Step==4){elapsed=0;failed=false;Progress=0;}Draw();
        }
        public void Previous(){if(special){if(Step>1&&Step<5){Step--;DrawSpecial();}return;}if(!stage.Ready||modal||Step<2||Step>3)return;if(ChoosingComponents)ChoosingComponents=false;else Step--;Draw();}
        void RefreshProgress()
        {
            for(int i=0;i<blocks.Length;i++)blocks[i].enabled=i<Mathf.CeilToInt(Progress*.25f);
            progressText.text=stage.Text("진행_퍼센트").Replace("{퍼센트}",Mathf.FloorToInt(Progress).ToString());
            string file=Data.files[Mathf.Min(Data.files.Length-1,Mathf.FloorToInt(Progress/100*Data.files.Length))];currentFile.text="현재 파일:\nC:\\Program Files\\"+Data.name+"\\"+file;
            if(lastFile!=file){lastFile=file;stage.App.Audio.Play("sfx_install_tick");}
        }
        void Notice(string message,Action after=null)
        {
            if(modal)return;modal=true;stage.App.Audio.Play("sfx_wizard_error");var w=FileUI.Notice(message,"s7:wizard-notice",false,"경고");w.SetParentWindow(Window);
            w.Closed+=()=>{modal=false;if(!Window.IsClosing)after?.Invoke();};
        }
        public void Cancel()
        {
            if(special){if(Step<6)Window.Close();return;}
            if(!stage.Ready||modal)return;
            if(Step==5){Window.Close();return;}
            if(!FakeWindow.TryCreate(stage.App.Windows,"s7:cancel","설치 마법사",out var w,290,240,450,235))return;modal=true;w.SetParentWindow(Window);w.Closed+=()=>modal=false;
            UIFactory.Text(w.Content,stage.Text("설치_취소_확인"),20,10,390,65,23);
            UIFactory.Button(w.Content,"예",90,105,110,35,()=>{stage.App.Audio.Play("sfx_wizard_cancel");Window.Close();});UIFactory.Button(w.Content,"아니오",230,105,110,35,w.Close);
        }
        void Update()
        {
            if(special)
            {
                if(Step==5&&!Window.IsClosing)
                {
                    elapsed+=Time.unscaledDeltaTime;Progress=Mathf.Min(100,elapsed/Mathf.Max(.1f,special.Rules.installSeconds)*100);
                    progressText.text=Mathf.FloorToInt(Progress)+"%";
                    for(int i=0;i<blocks.Length;i++)blocks[i].enabled=i<Mathf.CeilToInt(Progress*.25f);
                    var files=special.Rules.installFiles;currentFile.text=files[Mathf.Min(files.Length-1,Mathf.FloorToInt(Progress/100*files.Length))];
                    if(Progress>=100){Step=6;special.Installed();foreach(var other in GameApp.Instance.Windows.GetComponentsInChildren<FakeWindow>())if(other!=Window)other.Close();DrawSpecial();}
                }
                return;
            }
            if(!stage.Ready||modal)return;
            if(Step==4&&!failed)
            {
                elapsed+=Time.unscaledDeltaTime;string error=Data.id=="map"?stage.Data.ComponentError(Components):null;
                Progress=Mathf.Min(error==null?100:stage.Data.failPercent,elapsed/Mathf.Max(.1f,stage.Data.installSeconds)*100);RefreshProgress();
                if(error!=null&&Progress>=stage.Data.failPercent){failed=true;Notice(stage.Text(error),Window.Close);}
                else if(Progress>=100){stage.Installed(Data,SecretOption);Step=5;Draw();}
            }
            if(Window.transform.GetSiblingIndex()!=Window.transform.parent.childCount-1)return;
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)Cancel();else if(Keyboard.current?.enterKey.wasPressedThisFrame==true)Next();
        }
    }
    public sealed class WizardLinkCursor:MonoBehaviour,UnityEngine.EventSystems.IPointerEnterHandler,UnityEngine.EventSystems.IPointerExitHandler
    {
        bool over;
        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e){over=true;GameApp.Instance.GetComponent<FakeCursor>().SetResizeHint("☞");}
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e){Clear();}
        void Clear(){if(over&&GameApp.Instance)GameApp.Instance.GetComponent<FakeCursor>().SetResizeHint(null);over=false;}void OnDisable()=>Clear();
    }
}
