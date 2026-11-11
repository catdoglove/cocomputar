using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace CoComputar
{
    public sealed class ShapeViewerWindow:MonoBehaviour
    {
        public int Shape{get;private set;}public Slider X,Y;Stage07Controller stage;RectTransform canvas;bool changing;
        public static ShapeViewerWindow Open(Stage07Controller stage)
        {
            if(!FakeWindow.TryCreate(stage.App.Windows,"s7:viewer","3D뷰어98",out var w,125,100,775,530))return w.GetComponent<ShapeViewerWindow>();
            var self=w.gameObject.AddComponent<ShapeViewerWindow>();self.stage=stage;
            for(int i=0;i<3;i++){int s=i;UIFactory.Button(w.Toolbar,new[]{"○","△","□"}[i],5+i*70,0,60,32,()=>self.Select(s));}
            self.canvas=UIFactory.Panel(w.Content,"도형 표시 영역",10,10,405,425,new Color(.95f,.96f,.98f)).rectTransform;
            UIFactory.Text(w.Content,"X · 좌우 회전",450,70,270,35,23);self.X=UIFactory.Slider(w.Content,450,115,0,_=>self.Rotate(),false);
            UIFactory.Text(w.Content,"Y · 상하 회전",450,210,270,35,23);self.Y=UIFactory.Slider(w.Content,450,255,0,_=>self.Rotate(),false);
            foreach(var slider in new[]{self.X,self.Y}){slider.maxValue=stage.Data.rotationMax;slider.wholeNumbers=true;}
            UIFactory.Text(w.Content,"○ → △ → □\n도형 순서대로 확인하세요.\n\nX / Y: 0 ~ "+stage.Data.rotationMax,450,325,280,120,18);self.Select(0);return self;
        }
        public void Select(int shape)
        {
            if(!stage.Ready)return;changing=true;Shape=Mathf.Clamp(shape,0,2);X.value=stage.RotationX[Shape];Y.value=stage.RotationY[Shape];changing=false;Draw();
        }
        void Rotate(){if(changing||!stage.Ready)return;stage.RotationX[Shape]=(int)X.value;stage.RotationY[Shape]=(int)Y.value;stage.App.Audio.Play("sfx_viewer_rotate");Draw();}
        void Update()
        {
            if(!stage.Ready||transform.GetSiblingIndex()!=transform.parent.childCount-1)return;var k=Keyboard.current;if(k==null)return;
            if(k.leftArrowKey.wasPressedThisFrame)X.value--;if(k.rightArrowKey.wasPressedThisFrame)X.value++;
            if(k.downArrowKey.wasPressedThisFrame)Y.value--;if(k.upArrowKey.wasPressedThisFrame)Y.value++;
        }
        void Draw()
        {
            UIFactory.Clear(canvas);int x=stage.RotationX[Shape],y=stage.RotationY[Shape];
            var shape=UIFactory.Rect(canvas,"2D 도형 면",35,50,335,300).gameObject.AddComponent<Shape07Graphic>();shape.Kind=Shape;shape.TurnX=x;shape.TurnY=y;shape.raycastTarget=false;
            foreach(var l in stage.Randoms.Visible(Shape,x,y,stage.Data.visibleDistance))
            {
                Color c=l.Color=="blue"?new Color(.05f,.2f,.85f):l.Color=="green"?new Color(.03f,.5f,.12f):l.Color=="red"?new Color(.8f,.08f,.08f):new Color(.85f,.38f,0);
                int row=l.Slot/3,col=l.Slot%3;float u=(col-1)*(Shape==1?.24f+row*.14f:.4f),v=Shape==1?.3f-row*.3f:(1-row)*.4f;
                float px=202.5f+u*335*.48f*(1-.035f*x)+v*335*Mathf.Sin(x*Mathf.PI/3)*.12f,py=200-v*300*.48f*(1-.03f*y);
                var label=UIFactory.Text(canvas,l.Letter.ToString(),px-17,py-19,34,38,27,c);label.alignment=TMPro.TextAlignmentOptions.Center;label.name="letter:"+l.Letter;
            }
            UIFactory.Text(canvas,$"X: {x}    Y: {y}",25,375,350,35,22);
        }
    }
    // 실제 3D 대신 회전값에 따라 윤곽 폭/기울기를 바꾸는 2D 면 표현.
    public sealed class Shape07Graphic:MaskableGraphic
    {
        public int Kind,TurnX,TurnY;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();int count=Kind==0?40:Kind==1?3:4;var r=rectTransform.rect;var center=r.center;
            float phase=Kind==1?Mathf.PI/2:Mathf.PI/4;float skew=Mathf.Sin(TurnX*Mathf.PI/3)*.12f;
            var points=new Vector2[count];
            for(int i=0;i<count;i++){float a=phase+i*Mathf.PI*2/count;points[i]=center+new Vector2(Mathf.Cos(a)*r.width*.48f*(1-.035f*TurnX)+Mathf.Sin(a)*r.width*skew,Mathf.Sin(a)*r.height*.48f*(1-.03f*TurnY));}
            for(int i=0;i<count;i++)
            {
                int n=vh.currentVertCount;vh.AddVert(center,new Color(.83f,.86f,.94f),Vector2.zero);vh.AddVert(points[i],new Color(.9f,.92f,.98f),Vector2.zero);vh.AddVert(points[(i+1)%count],new Color(.73f,.77f,.86f),Vector2.zero);vh.AddTriangle(n,n+1,n+2);
                Line(vh,points[i],points[(i+1)%count],2,new Color(.14f,.18f,.3f));
            }
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color c)
        {var d=(b-a).normalized;var p=new Vector2(-d.y,d.x)*width;int n=vh.currentVertCount;vh.AddVert(a-p,c,Vector2.zero);vh.AddVert(a+p,c,Vector2.zero);vh.AddVert(b+p,c,Vector2.zero);vh.AddVert(b-p,c,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    }
}
