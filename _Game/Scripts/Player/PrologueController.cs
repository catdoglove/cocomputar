using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
namespace CoComputar
{
    [Serializable] public class WanderingMap { public string start; public int startX,startY; public WanderingZone[] zones; }
    [Serializable] public class WanderingZone { public string id,name,subtitle; public int theme; public string[] rows; public WanderingPortal[] portals; }
    [Serializable] public class WanderingPortal { public string id,label,target,arrival; public int x,y,spawnX,spawnY; }
    public sealed class PrologueController : MonoBehaviour
    {
        public const float Tile=42;
        public Func<bool> InputAllowed;
        [Range(1.5f,3f)] public float ViewScale=1.8f;
        public Vector2 CameraOffset=>world?new Vector2(world.anchoredPosition.x,-world.anchoredPosition.y):Vector2.zero;
        public Vector2 PlayerScreenCenter=>CameraOffset+new Vector2(Position.x,Position.y-21)*ViewScale;
        public static readonly Vector2 Origin=new(71,125);
        public WanderingMap Map { get; private set; }
        public WanderingZone Zone { get; private set; }
        public Vector2 Position { get; private set; }
        public bool Transitioning { get; private set; }
        public float FadeAlpha { get; private set; }
        public DoorCaptureSequence CaptureSequence { get; private set; }
        RectTransform exitDoor;
        RectTransform root,world,player,hud; Image playerImage,fade;
        TextMeshProUGUI prompt; Action finished;bool touchExit;
        float walk; string facing="player_front";
        public static Vector2 Center(int x,int y) => Origin+new Vector2((x+.5f)*Tile,(y+.5f)*Tile);
        public Vector2Int Cell => new(Mathf.FloorToInt((Position.x-Origin.x)/Tile),Mathf.FloorToInt((Position.y-Origin.y)/Tile));
        public void Setup(RectTransform parent,Action onFinished,WanderingMap map=null,bool touch=false)
        {
            finished=onFinished;touchExit=touch;Map=map??JsonUtility.FromJson<WanderingMap>(Resources.Load<TextAsset>("WanderingMaze").text);
            root=UIFactory.Rect(parent,"프롤로그",0,0,1024,768);
            Draw(Array.Find(Map.zones,z=>z.id==Map.start),Center(Map.startX,Map.startY));
            if(!touchExit)GameApp.Instance.Audio.Music("bgm_prologue");
        }
        void Draw(WanderingZone zone,Vector2 spawn)
        {
            if(world) {world.gameObject.SetActive(false);Destroy(world.gameObject);}
            if(hud) {hud.gameObject.SetActive(false);Destroy(hud.gameObject);}
            Zone=zone;Position=spawn;walk=0;facing="player_front";
            world=UIFactory.Rect(root,zone.name,0,0,1024,768);
            Color bg=zone.theme==3?new Color(.94f,.93f,.9f):zone.theme==0?new Color(.12f,.14f,.19f):zone.theme==1?new Color(.025f,.10f,.14f):new Color(.12f,.035f,.07f);
            Color accent=zone.theme==3?new Color(.35f,.32f,.3f):zone.theme==0?new Color(.8f,.79f,.72f):zone.theme==1?new Color(.38f,.85f,.77f):new Color(1,.40f,.45f);
            UIFactory.Panel(world,"먼 어둠",0,0,1024,768,bg);
            hud=UIFactory.Rect(root,"미로 안내",0,0,1024,768);
            UIFactory.Panel(hud,"상단 안내 배경",0,0,1024,112,new Color(bg.r,bg.g,bg.b,1));
            UIFactory.Panel(hud,"하단 안내 배경",0,696,1024,72,new Color(bg.r,bg.g,bg.b,1));
            UIFactory.Text(hud,zone.name,71,32,700,47,31,accent);
            UIFactory.Text(hud,zone.subtitle,73,82,800,30,17,Color.Lerp(bg,accent,.65f));
            for(int y=0;y<zone.rows.Length;y++) for(int x=0;x<zone.rows[y].Length;x++) DrawTile(x,y,zone.rows[y][x]=='#',zone.theme,bg,accent);
            foreach(var portal in zone.portals)
            {
                var p=Center(portal.x,portal.y);bool exit=string.IsNullOrEmpty(portal.target);
                if(exit)
                {
                    exitDoor=UIFactory.Rect(world,"기억의 문",p.x-26,p.y-100,52,64);
                    UIFactory.Sprite(exitDoor,GameApp.Instance.Catalog.sprites.Get("door"),0,0,52,64);
                    UIFactory.Panel(exitDoor,"하얀 손잡이",41,36,4,5,Color.white);
                    continue;
                }
                var gate=UIFactory.Panel(world,portal.label,p.x-16,p.y-30,32,44,exit?new Color(.87f,.92f,.74f):accent);
                UIFactory.Panel(gate.transform,"틈",4,4,24,36,bg);
                UIFactory.Panel(gate.transform,"빛",7,6,3,30,exit?Color.white:accent);
                UIFactory.Panel(world,"문턱",p.x-18,p.y+12,36,3,accent);
            }
            player=UIFactory.Rect(world,"V양",spawn.x-20,spawn.y-52,40,62);
            UIFactory.Panel(player,"발 그림자",6,51,28,9,new Color(0,0,0,.3f));
            playerImage=UIFactory.Sprite(player,GameApp.Instance.Catalog.sprites.Get(facing),0,0,40,62);
            prompt=UIFactory.Text(hud,"WASD / 방향키  이동     빛나는 틈에서 SPACE",71,707,900,34,20,accent);
            UIFactory.Text(hud,"되돌아가도 괜찮아. 어딘가에는 출구가 있어.",71,746,880,20,13,Color.Lerp(bg,accent,.6f));
            FollowPlayer();
            if(fade) fade.transform.SetAsLastSibling();
        }
        void FollowPlayer()
        {
            // UI로 만든 미로의 월드만 이동한다. 충돌 좌표와 고정 안내 UI는 그대로 둔다.
            world.localScale=Vector3.one*ViewScale;
            float width=Zone.rows[0].Length*Tile,height=Zone.rows.Length*Tile;
            float ClampAxis(float desired,float start,float length,float viewport)
            {
                if(length*ViewScale<=viewport) return (viewport-length*ViewScale)*.5f-start*ViewScale;
                return Mathf.Clamp(desired,viewport-(start+length)*ViewScale,-start*ViewScale);
            }
            float x=ClampAxis(512-Position.x*ViewScale,Origin.x,width,1024);
            // 상하 고정 안내 영역을 제외한 플레이 화면(112~696)에 맵을 맞춘다.
            float y=112+ClampAxis(384-112-(Position.y-21)*ViewScale,Origin.y,height,584);
            world.anchoredPosition=new Vector2(x,-y);
        }
        void DrawTile(int x,int y,bool wall,int theme,Color bg,Color accent)
        {
            float px=Origin.x+x*Tile,py=Origin.y+y*Tile;int hash=(x*37+y*71)%17;
            if(theme==3)
            {
                UIFactory.Panel(world,wall?"종이 더미":"흰 바닥",px,py,Tile,Tile,wall?new Color(.72f,.70f,.65f):Color.white);
                if(wall){for(int i=0;i<4;i++)UIFactory.Panel(world,"접힌 종이",px+3+i%2*2,py+4+i*8,34,6,new Color(.92f,.90f,.83f));}
                return;
            }
            if(theme==0)
            {
                UIFactory.Panel(world,wall?"접힌 벽":"먼지 길",px,py,Tile,Tile,wall?new Color(.38f,.39f,.42f):Color.Lerp(bg,accent,.12f));
                if(wall)
                {
                    UIFactory.Panel(world,"종이 그림자",px+5,py+6,35,34,new Color(.08f,.10f,.14f));
                    UIFactory.Panel(world,"접힌 종이",px+2,py+2,32,32,Color.Lerp(new Color(.6f,.62f,.65f),accent,hash/20f));
                    UIFactory.Panel(world,"접힘",px+22,py+2,2,28,new Color(.43f,.45f,.5f));
                }
                else UIFactory.Panel(world,"가루",px+5+hash,py+9+hash,2,2,new Color(.4f,.43f,.48f));
            }
            else if(theme==1)
            {
                UIFactory.Panel(world,wall?"깊은 물":"나무 다리",px,py,Tile,Tile,wall?new Color(.025f,.19f,.24f):new Color(.37f,.41f,.33f));
                if(wall)
                {
                    UIFactory.Panel(world,"물결",px+3+hash/2,py+12,24,2,new Color(.07f,.34f,.38f));
                    if(hash<7) {UIFactory.Panel(world,"마른 줄기",px+25,py+8,3,28,new Color(.16f,.45f,.39f));UIFactory.Panel(world,"잎",px+20,py+11,12,3,new Color(.20f,.5f,.42f));}
                }
                else for(int i=0;i<3;i++) UIFactory.Panel(world,"널빤지 틈",px+2,py+3+i*13,38,2,new Color(.13f,.24f,.23f));
            }
            else
            {
                UIFactory.Panel(world,wall?"신호 기둥":"금속 길",px,py,Tile,Tile,wall?new Color(.28f,.055f,.10f):new Color(.21f,.16f,.20f));
                if(wall)
                {
                    UIFactory.Panel(world,"폐쇄 패널",px+4,py+4,34,32,new Color(.11f,.04f,.08f));
                    UIFactory.Panel(world,"잔류 신호",px+8,py+8,hash<6?23:7,3,Color.Lerp(bg,accent,.7f));
                    UIFactory.Panel(world,"패널 하단",px+7,py+30,28,2,new Color(.45f,.12f,.20f));
                }
                else {UIFactory.Panel(world,"이음매",px,py+40,42,2,new Color(.1f,.08f,.12f));UIFactory.Panel(world,"볼트",px+5,py+5,2,2,accent);}
            }
        }
        public bool Walkable(int x,int y) => y>=0 && y<Zone.rows.Length && x>=0 && x<Zone.rows[y].Length && Zone.rows[y][x]!='#';
        public bool Blocked(Vector2 point)
        {
            // 발 주변 사각형의 네 모서리가 걸치는 모든 타일을 검사한다.
            int left=Mathf.FloorToInt((point.x-10-Origin.x)/Tile),right=Mathf.FloorToInt((point.x+10-Origin.x)/Tile);
            int top=Mathf.FloorToInt((point.y-8-Origin.y)/Tile),bottom=Mathf.FloorToInt((point.y+8-Origin.y)/Tile);
            for(int y=top;y<=bottom;y++) for(int x=left;x<=right;x++) if(!Walkable(x,y)) return true;
            return false;
        }
        void Update()
        {
            if(Transitioning || !root || Keyboard.current==null || (InputAllowed!=null&&!InputAllowed())) return;
            var k=Keyboard.current;
            Vector2 d=new((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),(k.sKey.isPressed||k.downArrowKey.isPressed?1:0)-(k.wKey.isPressed||k.upArrowKey.isPressed?1:0));
            var move=d.normalized*180*Time.deltaTime;
            // 긴 프레임에서도 벽을 건너뛰지 않도록 작은 보폭으로 나눈다.
            int steps=Mathf.Max(1,Mathf.CeilToInt(move.magnitude/5));move/=steps;
            for(int i=0;i<steps;i++)
            {
                var next=Position+new Vector2(move.x,0);if(!Blocked(next)) Position=next;
                next=Position+new Vector2(0,move.y);if(!Blocked(next)) Position=next;
            }
            player.anchoredPosition=new Vector2(Position.x-20,-Position.y+52);
            if(d!=Vector2.zero)
            {
                facing=Mathf.Abs(d.x)>0?"player_side":d.y<0?"player_back":"player_front";
                playerImage.sprite=GameApp.Instance.Catalog.sprites.Get(facing+(((int)(walk/Mathf.PI)%2==1)?"_step":""));
                playerImage.rectTransform.localScale=new Vector3(d.x<0?-1:1,1,1);walk+=Time.deltaTime*12;
            }
            else {walk=0;playerImage.sprite=GameApp.Instance.Catalog.sprites.Get(facing);}
            FollowPlayer();
            var portal=Array.Find(Zone.portals,p=>Vector2.Distance(Position,Center(p.x,p.y))<23);
            prompt.text=portal==null?"WASD / 방향키  이동     빛나는 틈에서 SPACE":"SPACE  ·  "+portal.label;
            if(touchExit)prompt.text="WASD / 방향키 이동 · 문에 닿으면 진입";
            if(portal!=null && (touchExit||k.spaceKey.wasPressedThisFrame)) StartCoroutine(Travel(portal));
        }
        IEnumerator Fade(float from,float to)
        {
            for(float t=0;t<.32f;t+=Time.unscaledDeltaTime)
            {FadeAlpha=Mathf.Lerp(from,to,t/.32f);fade.color=new Color(0,0,0,FadeAlpha);yield return null;}
            FadeAlpha=to;fade.color=new Color(0,0,0,to);
        }
        IEnumerator Travel(WanderingPortal portal)
        {
            if(Transitioning) yield break;
            if(string.IsNullOrEmpty(portal.target))
            {
                if(touchExit){Transitioning=true;fade=UIFactory.Panel(root,"꿈 페이드",0,0,1024,768,Color.clear);yield return Fade(0,1);finished();root.gameObject.SetActive(false);Destroy(root.gameObject);Destroy(this);yield break;}
                Transitioning=true;CaptureSequence=gameObject.AddComponent<DoorCaptureSequence>();
                yield return CaptureSequence.Run(exitDoor,player,playerImage);
                root.gameObject.SetActive(false);Destroy(root.gameObject);finished();Destroy(this);yield break;
            }
            Transitioning=true;fade=UIFactory.Panel(root,"구역 페이드",0,0,1024,768,Color.clear);
            GameApp.Instance.Audio.Play("sfx_window_close");yield return Fade(0,1);
            var next=Array.Find(Map.zones,z=>z.id==portal.target);var arrival=Array.Find(next.portals,p=>p.id==portal.arrival);
            Draw(next,Center(arrival.spawnX,arrival.spawnY));yield return Fade(1,0);
            Destroy(fade.gameObject);fade=null;Transitioning=false;
        }
        void OnDestroy() {if(CaptureSequence) Destroy(CaptureSequence);if(root) Destroy(root.gameObject);}
    }
}
