using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace CoComputar
{
    public sealed class RopeGameWindow:MiniGameWindow
    {
        public int Successes{get;private set;}public bool Running{get;private set;}public float Remaining{get;private set;}
        int limit;bool failed;float phase,jump;TMP_Text screen;RectTransform man,rope;
        public static RopeGameWindow Open(Stage06Controller stage,int seconds){var self=Create<RopeGameWindow>(stage,"rope","슈퍼 줄넘기",830,600);self.limit=seconds;self.Build();return self;}
        void Build()
        {
            var red=UIFactory.Panel(Window.Content,"휴대 게임기",0,0,810,540,new Color(.68f,.04f,.06f));var cream=UIFactory.Panel(red.transform,"크림 프레임",12,12,786,516,new Color(.9f,.87f,.75f));
            UIFactory.Text(cream.transform,"SUPER\nJUMP",12,15,130,110,24,Color.black);UIFactory.Text(cream.transform,"JUMP",650,15,120,40,25,Color.black);
            var lcd=UIFactory.Panel(cream.transform,"LCD",145,20,490,345,new Color(.62f,.67f,.4f));screen=UIFactory.Text(lcd.transform,"GAME A / SPACE",15,10,460,65,27,new Color(.12f,.16f,.07f));
            man=UIFactory.Rect(lcd.transform,"캐릭터",207,195,70,110);UIFactory.Panel(man,"머리",12,0,46,40,Color.black);UIFactory.Panel(man,"몸",22,42,26,44,Color.black);UIFactory.Panel(man,"다리1",14,85,14,24,Color.black);UIFactory.Panel(man,"다리2",42,85,14,24,Color.black);UIFactory.Panel(man,"눈1",20,9,6,8,Color.white);UIFactory.Panel(man,"눈2",44,9,6,8,Color.white);
            rope=UIFactory.Rect(lcd.transform,"회전 줄",95,145,300,160);var line=rope.gameObject.AddComponent<RopeLoopGraphic>();line.color=Color.black;line.raycastTarget=false;
            UIFactory.Button(cream.transform,"LEFT",14,255,115,80,Jump,Color.red);UIFactory.Button(cream.transform,"RIGHT",651,255,115,80,Jump,Color.red);
            UIFactory.Button(cream.transform,"GAME A",210,420,145,48,StartGame);UIFactory.Button(cream.transform,"GAME B",375,420,145,48,()=>{});UIFactory.Button(cream.transform,"TIME",540,420,145,48,()=>{});
        }
        public void StartGame(){if(!Stage.Ready)return;Running=true;failed=false;Successes=0;Remaining=limit;phase=jump=0;}
        public void Jump(){if(!Stage.Ready)return;if(!Running){StartGame();return;}if(jump<=0){jump=Stage.Data.jumpSeconds;App.Audio.Play("sfx_rope_jump");}}
        public void Tick(float dt)
        {
            if(!Running)return;Remaining=Mathf.Max(0,Remaining-dt);jump=Mathf.Max(0,jump-dt);phase+=dt;
            if(Remaining<=0){Fail();return;}
            if(phase>=Stage.Data.ropePeriod){phase-=Stage.Data.ropePeriod;if(jump<=0){Fail();return;}Successes++;App.Audio.Play("sfx_lcd_tick");if(Successes>=Stage.Data.ropeGoal){Running=false;Stage.Reward("B","rope");}}
        }
        void Fail(){Running=false;failed=true;App.Audio.Play("sfx_rope_miss");App.Audio.Play("sfx_game_over");}
        void Update()
        {
            if(InputReady&&Keyboard.current?.spaceKey.wasPressedThisFrame==true)Jump();if(InputReady)Tick(Time.unscaledDeltaTime);
            if(screen)screen.text=$"{Successes}/{Stage.Data.ropeGoal}     {Mathf.CeilToInt(Remaining):000}s\n"+(Running?"":Successes>=Stage.Data.ropeGoal?"CLEAR":failed?"GAME OVER":"GAME A / SPACE");
            if(man)man.anchoredPosition=new Vector2(207,-195+Mathf.Sin(Mathf.Clamp01(jump/Stage.Data.jumpSeconds)*Mathf.PI)*70);
            if(rope)rope.localScale=new Vector3(1,Mathf.Cos(phase/Stage.Data.ropePeriod*Mathf.PI*2),1);
        }
    }
    public sealed class RopeLoopGraphic:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper v){v.Clear();var r=rectTransform.rect;for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48,b=(i+1)*Mathf.PI*2/48;Vector2 p=r.center+new Vector2(Mathf.Cos(a)*r.width/2,Mathf.Sin(a)*r.height/2),q=r.center+new Vector2(Mathf.Cos(b)*r.width/2,Mathf.Sin(b)*r.height/2);Vector2 n=new Vector2(-(q-p).y,(q-p).x).normalized*2;int s=v.currentVertCount;foreach(var point in new[]{p+n,p-n,q-n,q+n})v.AddVert(point,color,Vector2.zero);v.AddTriangle(s,s+1,s+2);v.AddTriangle(s,s+2,s+3);}}
    }
    public sealed class SnakeGameWindow:MiniGameWindow
    {
        public readonly List<Vector2Int> Snake=new();public Vector2Int Apple{get;private set;}public int Apples{get;private set;}public bool Running{get;private set;}
        Vector2Int direction=Vector2Int.right,queued=Vector2Int.right;float tick;int best;bool animating;RectTransform board,viewport;GameObject flyingReward;TMP_Text score;readonly List<GameObject> pieces=new();
        public static SnakeGameWindow Open(Stage06Controller stage){var c=stage.Data;var self=Create<SnakeGameWindow>(stage,"snake","슈퍼 지렁이",c.snakeInitialCols*c.snakeCell+24,c.snakeInitialRows*c.snakeCell+160);self.Build();return self;}
        void Build()
        {
            Window.UseScreenSpaceContent();Window.Closed+=()=>{if(flyingReward)Destroy(flyingReward);if(Apples>=Stage.Data.snakeGoal)Stage.Reward("C","snake");};score=UIFactory.Text(Window.Toolbar,"사과 0 / 10",0,0,210,32,20,Color.black);
            viewport=UIFactory.Panel(Window.Content,"지렁이 판 보기",0,0,Window.Content.rect.width,Window.Content.rect.height,Color.black).rectTransform;viewport.gameObject.AddComponent<RectMask2D>();
            board=UIFactory.Rect(viewport,"논리 판",0,0,Stage.Data.snakeCols*Stage.Data.snakeCell,Stage.Data.snakeRows*Stage.Data.snakeCell);
            for(int y=0;y<Stage.Data.snakeRows;y++)for(int x=0;x<Stage.Data.snakeCols;x++)UIFactory.Panel(board,"잔디",x*Stage.Data.snakeCell,y*Stage.Data.snakeCell,Stage.Data.snakeCell,Stage.Data.snakeCell,(x+y)%2==0?new Color(.55f,.78f,.23f):new Color(.43f,.68f,.16f)).raycastTarget=false;
            var resize=gameObject.AddComponent<ResizableFakeWindow>();resize.Setup(Window,new Vector2(Stage.Data.snakeCols*Stage.Data.snakeCell+24,Stage.Data.snakeRows*Stage.Data.snakeCell+94));Restart();
        }
        public void Restart(){if(!Stage.Ready||animating)return;board.anchoredPosition=Vector2.zero;Snake.Clear();Snake.Add(new Vector2Int(3,3));Snake.Add(new Vector2Int(2,3));Snake.Add(new Vector2Int(1,3));direction=queued=Vector2Int.right;Apples=0;Running=true;tick=0;NextApple();Draw();}
        void NextApple()
        {
            if(Apples==Stage.Data.snakeGoal-1){Apple=new Vector2Int(Stage.Data.snakeCols-1,Stage.Data.snakeRows-1);return;}
            var empty=new List<Vector2Int>();for(int y=0;y<Stage.Data.snakeInitialRows;y++)for(int x=0;x<Stage.Data.snakeInitialCols;x++){var p=new Vector2Int(x,y);if(!Snake.Contains(p))empty.Add(p);}Apple=empty[UnityEngine.Random.Range(0,empty.Count)];
        }
        public void Turn(Vector2Int d){if(d!=-direction)queued=d;}
        public void Step()
        {
            if(!Running)return;direction=queued;var p=Snake[0]+direction;bool eat=p==Apple;int body=Snake.Count-(eat?0:1);
            if(p.x<0||p.y<0||p.x>=Stage.Data.snakeCols||p.y>=Stage.Data.snakeRows||Snake.GetRange(0,body).Contains(p)){Running=false;App.Audio.Play("sfx_snake_die");Draw();return;}
            Snake.Insert(0,p);if(eat){Apples++;best=Mathf.Max(best,Apples);App.Audio.Play("sfx_snake_eat");if(Apples>=Stage.Data.snakeGoal){Running=false;StartCoroutine(Spit());}else NextApple();}else Snake.RemoveAt(Snake.Count-1);Draw();
        }
        IEnumerator Spit()
        {
            animating=true;App.Audio.Play("sfx_spit");
            var mouth=UIFactory.Text(board,"○",Snake[0].x*Stage.Data.snakeCell,Snake[0].y*Stage.Data.snakeCell,30,30,25,Color.black);
            var reward=UIFactory.Sprite(viewport,App.Catalog.sprites.Get("s6_C"),Mathf.Min(viewport.rect.width-65,Snake[0].x*Stage.Data.snakeCell),Mathf.Min(viewport.rect.height-85,Snake[0].y*Stage.Data.snakeCell),65,85);
            for(float t=0;t<.3f;t+=Time.unscaledDeltaTime){reward.transform.localScale=Vector3.one*Mathf.Lerp(.1f,1,t/.3f);yield return null;}
            Destroy(mouth.gameObject);
            flyingReward=reward.gameObject;var rect=reward.rectTransform;rect.SetParent(App.DragLayer,true);
            var start=rect.position;var file=Stage.Files.Find("C");
            var target=App.Root.TransformPoint(new Vector3(file.x,file.y*-1-38,0));
            for(float t=0;t<.4f;t+=Time.unscaledDeltaTime){float f=Mathf.SmoothStep(0,1,t/.4f);rect.position=Vector3.Lerp(start,target,f);board.anchoredPosition+=Vector2.right*1200*Time.unscaledDeltaTime;yield return null;}
            Stage.Reward("C","snake");Destroy(reward.gameObject);flyingReward=null;animating=false;
        }
        void Draw()
        {
            foreach(var p in pieces)if(p)Destroy(p);pieces.Clear();float cell=Stage.Data.snakeCell;
            foreach(var p in Snake)pieces.Add(UIFactory.Panel(board,"파란 지렁이",p.x*cell+1,p.y*cell+1,cell-2,cell-2,new Color(.05f,.23f,.9f)).gameObject);
            var head=Snake[0];pieces.Add(UIFactory.Text(board,"••",head.x*cell+3,head.y*cell,cell,cell,18,Color.white).gameObject);
            if(Apples<Stage.Data.snakeGoal)pieces.Add(UIFactory.Panel(board,"사과",Apple.x*cell+3,Apple.y*cell+3,cell-6,cell-6,Color.red).gameObject);
            if(!Running&&Apples<Stage.Data.snakeGoal){var b=UIFactory.Button(viewport,"GAME OVER · 재시작",5,65,viewport.rect.width-10,65,()=>{Destroy(banner);board.anchoredPosition=Vector2.zero;Restart();});banner=b.gameObject;}
        }
        GameObject banner;
        void Update()
        {
            viewport.sizeDelta=Window.Content.rect.size;score.text=$"사과 {Apples}/{Stage.Data.snakeGoal}  최고 {best}";
            if(!InputReady)return;var k=Keyboard.current;if(k!=null){if(k.upArrowKey.wasPressedThisFrame||k.wKey.wasPressedThisFrame)Turn(Vector2Int.down);if(k.downArrowKey.wasPressedThisFrame||k.sKey.wasPressedThisFrame)Turn(Vector2Int.up);if(k.leftArrowKey.wasPressedThisFrame||k.aKey.wasPressedThisFrame)Turn(Vector2Int.left);if(k.rightArrowKey.wasPressedThisFrame||k.dKey.wasPressedThisFrame)Turn(Vector2Int.right);if(!Running&&k.spaceKey.wasPressedThisFrame){if(banner)Destroy(banner);Restart();}}
            if(Running){tick+=Time.unscaledDeltaTime;if(tick>=Stage.Data.snakeStep){tick-=Stage.Data.snakeStep;Step();}}
        }
    }
    public sealed class ResizableFakeWindow:MonoBehaviour
    {
        FakeWindow window;Vector2 min,max;readonly List<(RectTransform rect,Vector2 edge)> grips=new();
        public void Setup(FakeWindow w,Vector2 maximum)
        {
            window=w;min=w.Rect.sizeDelta;max=Vector2.Min(maximum,new Vector2(1000,700));
            foreach(var edge in new[]{new Vector2(1,0),new Vector2(0,1),new Vector2(1,1),new Vector2(-1,0),new Vector2(0,-1),new Vector2(-1,-1),new Vector2(1,-1),new Vector2(-1,1)}){var r=UIFactory.Panel(w.transform,"크기 조절",0,0,12,12,edge==Vector2.one?WindowSkin.Purple:Color.clear).rectTransform;var drag=r.gameObject.AddComponent<WindowResizeHandle>();drag.Owner=this;drag.Edge=edge;grips.Add((r,edge));if(edge==Vector2.one)UIFactory.Text(r,"◢",0,0,30,30,28,Color.white);}
        }
        public void Resize(Vector2 edge,Vector2 delta)
        {
            var old=window.Rect.sizeDelta;var size=new Vector2(Mathf.Clamp(old.x+delta.x*edge.x,min.x,max.x),Mathf.Clamp(old.y-delta.y*edge.y,min.y,max.y));window.Resize(size);window.UseScreenSpaceContent();if(edge.x<0)window.Rect.anchoredPosition+=new Vector2(old.x-size.x,0);if(edge.y<0)window.Rect.anchoredPosition+=new Vector2(0,size.y-old.y);var p=window.Rect.anchoredPosition;p.x=Mathf.Clamp(p.x,0,1024-size.x);p.y=Mathf.Clamp(p.y,-768+size.y,-38);window.Rect.anchoredPosition=p;
        }
        void LateUpdate()
        {
            if(!window)return;var s=window.Rect.sizeDelta;
            foreach(var g in grips)
            {
                bool corner=g.edge.x!=0&&g.edge.y!=0;
                float width=corner?20:g.edge.x!=0?9:s.x-40;
                float height=corner?20:g.edge.y!=0?9:s.y-40;
                if(g.edge==Vector2.one)width=height=30;
                g.rect.sizeDelta=new Vector2(width,height);
                g.rect.anchoredPosition=new Vector2(g.edge.x>0?s.x-width:g.edge.x<0?0:20,g.edge.y>0?-s.y+height:g.edge.y<0?0:-20);
            }
        }

    }
    public sealed class WindowResizeHandle:MonoBehaviour,IBeginDragHandler,IDragHandler,IPointerEnterHandler,IPointerExitHandler
    {
        public ResizableFakeWindow Owner;public Vector2 Edge;Vector2 last;
        public void OnBeginDrag(PointerEventData e){RectTransformUtility.ScreenPointToLocalPointInRectangle(GameApp.Instance.Root,e.position,e.pressEventCamera,out last);}
        public void OnDrag(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(GameApp.Instance.Root,e.position,e.pressEventCamera,out var p);Owner.Resize(Edge,p-last);last=p;}
        public void OnPointerEnter(PointerEventData e){GameApp.Instance.GetComponent<FakeCursor>().SetResizeHint(Edge.x!=0&&Edge.y!=0?"⤡":Edge.x!=0?"↔":"↕");}
        public void OnPointerExit(PointerEventData e){GameApp.Instance.GetComponent<FakeCursor>().SetResizeHint(null);}
        void OnDisable(){if(GameApp.Instance)GameApp.Instance.GetComponent<FakeCursor>()?.SetResizeHint(null);}
    }
}
