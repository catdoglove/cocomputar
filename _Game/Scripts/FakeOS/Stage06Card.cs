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
    // FakeWindow는 sealed이므로 상속 대신 기존 창을 소유하는 공통 컴포넌트로 재사용한다.
    public class MiniGameWindow:MonoBehaviour
    {
        public FakeWindow Window;public Stage06Controller Stage;public string GameId;
        protected GameApp App=>GameApp.Instance;
        protected bool InputReady=>Stage.Ready&&Window.transform.GetSiblingIndex()==Window.transform.parent.childCount-1;
        protected static T Create<T>(Stage06Controller stage,string id,string title,float width,float height) where T:MiniGameWindow
        {
            if(id=="card"||id=="rope")title=stage.Files.Find(id+"_exe").displayName;
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"s6:game:"+id,title,out var w,(1024-width)/2,65,width,height))return w.GetComponent<T>();
            var self=w.gameObject.AddComponent<T>();self.Window=w;self.Stage=stage;self.GameId=id;stage.Started(id);return self;
        }
    }
    public sealed class HighScoreBoard
    {
        public sealed class Record{public string Name,Date;public float Seconds;}
        readonly List<Record>[] rows={new(),new(),new()};
        public IReadOnlyList<Record> Rows(int difficulty)=>rows[difficulty];
        public bool Qualifies(int difficulty,float time)=>rows[difficulty].Count<4||rows[difficulty].Last().Seconds>time;
        public void Add(int difficulty,string name,float time,DateTime date){rows[difficulty].Add(new Record{Name=name.ToUpperInvariant(),Seconds=time,Date=date.ToString("yyyy-MM-dd")});rows[difficulty].Sort((a,b)=>a.Seconds.CompareTo(b.Seconds));if(rows[difficulty].Count>4)rows[difficulty].RemoveAt(4);}
        public string Text(int difficulty)
        {
            string text="순위  이니셜     기록       달성 날짜\n1     ZZZ     00:00:01   1989-06-06\n";
            for(int i=0;i<4;i++){var r=i<rows[difficulty].Count?rows[difficulty][i]:null;text+=(i+2)+"     "+(r?.Name??"???")+"     "+(r==null?"--:--:--":TimeSpan.FromSeconds(r.Seconds).ToString(@"hh\:mm\:ss"))+"   "+(r?.Date??"0000-00-00")+"\n";}return text;
        }
    }
    public sealed partial class Stage06Controller{public readonly HighScoreBoard Scores=new();}
    public sealed class CardGameWindow:MiniGameWindow
    {
        public int[] Cards{get;private set;}public bool[] Matched{get;private set;}public int Difficulty{get;private set;}public bool Cleared{get;private set;}
        public bool Previewing{get;private set;}public bool AcceptsCards{get;private set;}
        Button startButton;Button[] buttons;RectTransform table;TMP_Text timer;int first=-1;bool checking;float started=-1,elapsed;
        public static CardGameWindow Open(Stage06Controller stage)
        {
            var self=Create<CardGameWindow>(stage,"card","슈퍼 카드",820,650);self.Build();self.Window.Closed+=()=>{self.StopAllCoroutines();self.Previewing=self.AcceptsCards=false;};return self;
        }
        void Build()
        {
            UIFactory.Button(Window.Toolbar,"게임(G)",0,0,120,30,Menu);UIFactory.Button(Window.Toolbar,"도움말(H)",126,0,135,30,()=>FileUI.Notice("카드 두 장을 뒤집어 같은 그림을 찾으세요.\nHARD를 완료하면 스티커를 받습니다.","s6:cardHelp",false));
            table=UIFactory.Panel(Window.Content,"초록 펠트",0,0,800,590,new Color(0,.38f,.12f)).rectTransform;StartGame(0);
        }
        void Menu()
        {
            if(Window.Content.Find("게임 메뉴")){Destroy(Window.Content.Find("게임 메뉴").gameObject);return;}
            var panel=UIFactory.Panel(Window.Content,"게임 메뉴",0,0,190,190,WindowSkin.Pink);
            for(int i=0;i<3;i++){int d=i;UIFactory.Button(panel.transform,new[]{"EASY","NORMAL","HARD"}[i],0,i*36,190,34,()=>{Destroy(panel.gameObject);StartGame(d);});}
            UIFactory.Button(panel.transform,"순위표",0,108,190,34,()=>{Destroy(panel.gameObject);ShowScores();});UIFactory.Button(panel.transform,"종료",0,144,190,34,Window.Close);
        }
        public void StartGame(int difficulty)
        {
            StopAllCoroutines();Previewing=AcceptsCards=false;Difficulty=difficulty;Cleared=checking=false;first=-1;started=-1;elapsed=0;UIFactory.Clear(table);
            Cards=Enumerable.Range(0,Stage.Data.cardPairs[difficulty]).SelectMany(i=>new[]{i,i}).OrderBy(_=>UnityEngine.Random.value).ToArray();Matched=new bool[Cards.Length];buttons=new Button[Cards.Length];timer=UIFactory.Text(table,"00:00:00",550,5,230,32,23,Color.white);
            int cols=difficulty==0?3:5;float cardWidth=130,cardHeight=108;
            for(int i=0;i<Cards.Length;i++){int index=i;buttons[i]=UIFactory.Button(table,"★",20+i%cols*150,45+i/cols*130,cardWidth,cardHeight,()=>Flip(index),new Color(.05f,.18f,.55f));buttons[i].onClick.RemoveAllListeners();buttons[i].onClick.AddListener(()=>Flip(index));buttons[i].navigation=new Navigation{mode=Navigation.Mode.None};buttons[i].interactable=false;buttons[i].name="card:"+i;buttons[i].GetComponentInChildren<TMP_Text>().color=Color.white;}
            startButton=UIFactory.Button(table,"게임 시작",290,265,220,55,BeginPreview);startButton.name="게임 시작";RefreshCardInput();
        }
        public void BeginPreview()
        {
            if(!Stage.Ready||Previewing||AcceptsCards||Cleared)return;
            startButton.gameObject.SetActive(false);Previewing=true;StartCoroutine(Preview());
        }
        IEnumerator Preview()
        {
            for(int i=0;i<Cards.Length;i++)Face(i,true);
            yield return new WaitForSecondsRealtime(Mathf.Max(0,Stage.Data.cardPreviewSeconds));
            for(int i=0;i<Cards.Length;i++)Face(i,false);
            Previewing=false;AcceptsCards=true;RefreshCardInput();
        }
        void Face(int i,bool face){buttons[i].GetComponent<Image>().color=face?Color.white:new Color(.05f,.18f,.55f);var label=buttons[i].GetComponentInChildren<TMP_Text>();label.text=face?((char)('A'+Cards[i])).ToString()+"\n"+new[]{"♠","♥","♦","♣"}[Cards[i]%4]:"★";label.color=face?(Cards[i]%2==0?Color.black:Color.red):Color.white;}
        public void Flip(int index)
        {
            if(!Stage.Ready||!AcceptsCards||checking||Cleared||index<0||index>=Cards.Length||index==first||Matched[index])return;if(started<0)started=Time.unscaledTime;Face(index,true);App.Audio.Play("sfx_card_flip");if(first<0){first=index;RefreshCardInput();return;}checking=true;RefreshCardInput();StartCoroutine(Compare(first,index));
        }
        IEnumerator Compare(int a,int b)
        {
            if(Cards[a]==Cards[b]){Matched[a]=Matched[b]=true;App.Audio.Play("sfx_card_match");}
            else{App.Audio.Play("sfx_card_mismatch");yield return new WaitForSecondsRealtime(Stage.Data.cardMismatchSeconds);Face(a,false);Face(b,false);}
            first=-1;checking=false;if(Matched.All(v=>v))
            {
                Cleared=true;elapsed=Time.unscaledTime-started;App.Audio.Play("sfx_card_clear");if(Difficulty==2){Stage.Reward("A","card");if(elapsed<=Stage.Data.starTimeLimitSec)Stage.Reward("star","card");}
                if(Stage.Scores.Qualifies(Difficulty,elapsed))Initials();else ShowScores();
            }
            RefreshCardInput();
        }
        void RefreshCardInput()
        {
            for(int i=0;i<buttons.Length;i++)
            {
                bool enabled=AcceptsCards&&!checking&&!Cleared&&!Matched[i]&&i!=first;
                // Change input state without first tinting the whole board as disabled.
                // Re-enable ColorTint only after interactable has its final value.
                var button=buttons[i];button.transition=Selectable.Transition.None;
                button.interactable=enabled;button.GetComponent<Image>().raycastTarget=enabled;
                if(enabled)button.transition=Selectable.Transition.ColorTint;
                if(!enabled)
                {
                    button.targetGraphic.CrossFadeColor(Color.white,0,true,true);
                    var events=UnityEngine.EventSystems.EventSystem.current;
                    if(events&&events.currentSelectedGameObject==button.gameObject)events.SetSelectedGameObject(null);
                }
            }
        }
        void Initials()
        {
            if(!FakeWindow.TryCreate(App.Windows,"s6:initials","기록 등록",out var w,300,230,430,280))return;
            w.SetParentWindow(Window);
            UIFactory.Text(w.Content,"이니셜 3글자 (A–Z)",20,10,360,35,22);var input=FileUI.Input(w.Content,"AAA",55,55,260,38);input.characterLimit=3;input.characterValidation=TMP_InputField.CharacterValidation.Alphanumeric;input.ActivateInputField();
            var edit=w.gameObject.AddComponent<ScoreInitialsInput>();edit.Input=input;
            void Confirm(){if(!w||w.IsClosing||!Window||Window.IsClosing)return;string value=input.text.ToUpperInvariant();if(value.Length!=3||value.Any(c=>c<'A'||c>'Z'))return;Stage.Scores.Add(Difficulty,value,elapsed,App.DisplayDate??DateTime.Now);App.Audio.Play("sfx_highscore_enter");w.Close();ShowScores();}
            edit.Confirm=Confirm;UIFactory.Button(w.Content,"확인",135,150,120,34,Confirm);UIFactory.Button(w.Content,"▲",320,52,45,32,()=>edit.Change(1));UIFactory.Button(w.Content,"▼",320,88,45,32,()=>edit.Change(-1));
        }
        public void ShowScores()
        {
            if(!FakeWindow.TryCreate(App.Windows,"s6:scores","HIGH SCORE",out var w,160,160,700,330))return;
            var text=UIFactory.Text(w.Content,Stage.Scores.Text(Difficulty),12,45,660,220,21,Color.black);
            for(int i=0;i<3;i++){int d=i;UIFactory.Button(w.Content,new[]{"EASY","NORMAL","HARD"}[i],10+i*210,2,200,34,()=>text.text=Stage.Scores.Text(d));}
        }
        void Update(){if(started>=0&&!Cleared)elapsed=Time.unscaledTime-started;if(timer)timer.text=TimeSpan.FromSeconds(elapsed).ToString(@"hh\:mm\:ss");}
    }
    public sealed class ScoreInitialsInput:MonoBehaviour
    {
        public TMP_InputField Input;public Action Confirm;
        public void Change(int delta){string s=(Input.text.ToUpperInvariant()+"AAA").Substring(0,3);int i=Mathf.Clamp(Input.caretPosition,0,2);var chars=s.ToCharArray();chars[i]=(char)('A'+(Mathf.Clamp(chars[i]-'A',0,25)+delta+26)%26);Input.text=new string(chars);Input.caretPosition=i;}
        void Update(){if(!UnityEngine.EventSystems.EventSystem.current||!UnityEngine.EventSystems.EventSystem.current.currentInputModule||!UnityEngine.EventSystems.EventSystem.current.currentInputModule.isActiveAndEnabled)return;var k=Keyboard.current;if(k==null)return;if(k.upArrowKey.wasPressedThisFrame)Change(1);if(k.downArrowKey.wasPressedThisFrame)Change(-1);if(k.enterKey.wasPressedThisFrame)Confirm?.Invoke();}
    }
}
