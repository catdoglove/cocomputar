using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    public enum CapturePhase { None, Blackout, Morph, Recoil, Run, Lunge, Drag, FinalBlack, Complete }
    public sealed class DoorCaptureSequence : MonoBehaviour
    {
        public CapturePhase Phase { get; private set; }
        public bool Active => Phase!=CapturePhase.None && Phase!=CapturePhase.Complete;
        public int BackSteps { get; private set; }
        public RectTransform Actor { get; private set; }
        public RectTransform Pursuer { get; private set; }
        public RectTransform Layer { get; private set; }
        public float MorphProgress { get; private set; }
        bool restoreCursor,cursorWasVisible;
        public IEnumerator Run(RectTransform door,RectTransform actor,Image actorImage)
        {
            if(Active) yield break;
            var app=GameApp.Instance;Phase=CapturePhase.Blackout;
            cursorWasVisible=app.CursorLayer.gameObject.activeSelf;restoreCursor=true;app.CursorLayer.gameObject.SetActive(false);
            Layer=UIFactory.Panel(app.Modal,"문 포획 암전",0,0,1024,768,Color.black).rectTransform;
            // 같은 화면 좌표를 유지하며 문과 주인공만 완전 불투명한 입력 차단막 위로 옮긴다.
            door.SetParent(Layer,true);actor.SetParent(Layer,true);Actor=actor;
            try
            {
                yield return new WaitForSecondsRealtime(.35f);
                Phase=CapturePhase.Morph;
                var doorImage=door.GetComponentInChildren<Image>();
                var handle=(RectTransform)door.Find("하얀 손잡이");
                Vector3 handleWorld=handle.position;handle.gameObject.SetActive(false);
                Pursuer=UIFactory.Rect(Layer,"변형 커서",0,0,4,5);Pursuer.position=handleWorld;
                var morph=Pursuer.gameObject.AddComponent<DoorHandleMorph>();morph.raycastTarget=false;
                var cursor=UIFactory.Sprite(Pursuer,app.Catalog.sprites.Get("cursor"),0,0,42,57);cursor.color=new Color(1,1,1,0);
                Vector2 origin=Pursuer.anchoredPosition;
                for(float t=0;t<1.25f;t+=Time.unscaledDeltaTime)
                {
                    MorphProgress=Mathf.SmoothStep(0,1,t/1.25f);morph.Amount=MorphProgress;morph.SetVerticesDirty();
                    Pursuer.sizeDelta=Vector2.Lerp(new Vector2(4,5),new Vector2(42,57),MorphProgress);
                    Pursuer.anchoredPosition=origin+new Vector2(-18,24)*MorphProgress;
                    doorImage.color=new Color(1,1,1,1-MorphProgress);
                    float reveal=Mathf.InverseLerp(.75f,1,MorphProgress);cursor.color=new Color(1,1,1,reveal);morph.color=new Color(1,1,1,1-reveal);
                    yield return null;
                }
                MorphProgress=1;cursor.color=Color.white;morph.enabled=false;door.gameObject.SetActive(false);
                Pursuer.sizeDelta=new Vector2(42,57);
                // 화면 안에 도주 여백이 남는 방향을 선택하고, 처음에는 커서를 바라본다.
                Vector2 away=(Actor.anchoredPosition-Pursuer.anchoredPosition).normalized;
                if(Mathf.Abs(away.x)<.45f) away=Actor.anchoredPosition.x>512?Vector2.left:Vector2.right;
                if(Actor.anchoredPosition.x>760 && away.x>0 || Actor.anchoredPosition.x<220 && away.x<0) away.x=-away.x;
                away.Normalize();
                Phase=CapturePhase.Recoil;
                for(int step=0;step<3;step++)
                {
                    var start=Actor.anchoredPosition;
                    for(float t=0;t<.24f;t+=Time.unscaledDeltaTime)
                    {
                        float p=Mathf.Clamp01(t/.24f);Actor.anchoredPosition=start+away*15*p+Vector2.up*Mathf.Sin(p*Mathf.PI)*3;
                        Pose(actorImage,-away,t,true);yield return null;
                    }
                    Actor.anchoredPosition=start+away*15;BackSteps++;yield return new WaitForSecondsRealtime(.09f);
                }
                Phase=CapturePhase.Run;var runStart=Actor.anchoredPosition;
                for(float t=0;t<.42f;t+=Time.unscaledDeltaTime)
                {Actor.anchoredPosition=runStart+away*150*t;Pose(actorImage,away,t,true);yield return null;}
                Phase=CapturePhase.Lunge;app.Audio.Play("sfx_door_run");var from=Pursuer.anchoredPosition;var victim=Actor.anchoredPosition;
                for(float t=0;t<.22f;t+=Time.unscaledDeltaTime)
                {
                    float p=Mathf.Clamp01(t/.22f);Actor.anchoredPosition=victim+away*50*t;
                    Pursuer.anchoredPosition=Vector2.Lerp(from,Actor.anchoredPosition+new Vector2(8,22),p*p);Pose(actorImage,away,t,true);yield return null;
                }
                Pursuer.anchoredPosition=Actor.anchoredPosition+new Vector2(8,22);
                Actor.SetParent(Pursuer,true);Phase=CapturePhase.Drag;
                var grab=Pursuer.anchoredPosition;var offscreen=new Vector2(1160,220);
                for(float t=0;t<.75f;t+=Time.unscaledDeltaTime)
                {float p=Mathf.Clamp01(t/.75f);Pursuer.anchoredPosition=Vector2.Lerp(grab,offscreen,p*p);yield return null;}
                Pursuer.anchoredPosition=offscreen;Phase=CapturePhase.FinalBlack;
                yield return new WaitForSecondsRealtime(.35f);Phase=CapturePhase.Complete;
            }
            finally {Cleanup();}
        }
        void Pose(Image image,Vector2 direction,float time,bool stepping)
        {
            string key=Mathf.Abs(direction.x)>.45f?"player_side":direction.y>0?"player_back":"player_front";
            image.sprite=GameApp.Instance.Catalog.sprites.Get(key+(stepping && (int)(time*14)%2==1?"_step":""));
            // 좌우 반전 시 그림의 중심을 보존한다.
            image.rectTransform.pivot=new Vector2(.5f,.5f);image.rectTransform.anchoredPosition=new Vector2(20,-31);
            image.rectTransform.localScale=new Vector3(direction.x<0?-1:1,1,1);
        }
        void Cleanup()
        {
            if(Layer) {Layer.gameObject.SetActive(false);Destroy(Layer.gameObject);}
            if(restoreCursor && GameApp.Instance && GameApp.Instance.CursorLayer) GameApp.Instance.CursorLayer.gameObject.SetActive(cursorWasVisible);
            restoreCursor=false;
        }
        void OnDestroy() {Cleanup();}
    }
    // 흰 사각형의 아래 꼭짓점들을 펼쳐 화살표와 꼬리를 만든 뒤 기존 커서 그림으로 연결한다.
    public sealed class DoorHandleMorph : MaskableGraphic
    {
        public float Amount;
        static readonly Vector2[] square={new(0,0),new(0,1),new(.3f,1),new(.55f,1),new(.75f,1),new(1,1),new(1,0)};
        static readonly Vector2[] arrow={new(0,0),new(0,.91f),new(.28f,.67f),new(.48f,1),new(.66f,.91f),new(.46f,.59f),new(.86f,.59f)};
        static readonly int[] triangles={0,1,2,0,2,5,0,5,6,2,3,4,2,4,5};
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var size=rectTransform.rect.size;
            for(int i=0;i<square.Length;i++) {var p=Vector2.Lerp(square[i],arrow[i],Amount);mesh.AddVert(new Vector3(p.x*size.x,-p.y*size.y),color,Vector2.zero);}
            for(int i=0;i<triangles.Length;i+=3) mesh.AddTriangle(triangles[i],triangles[i+1],triangles[i+2]);
        }
    }
}
