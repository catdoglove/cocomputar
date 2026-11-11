using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    // 글꼴의 트럼프 기호 지원과 무관한 UI 벡터 도형.
    public sealed class CardSuitGraphic:MaskableGraphic
    {
        public int Kind;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(Kind==3){Polygon(vh,new[]{new Vector2(.5f,.95f),new Vector2(.94f,.5f),new Vector2(.5f,.05f),new Vector2(.06f,.5f)});return;}
            if(Kind==2){Circle(vh,new Vector2(.5f,.72f),.23f);Circle(vh,new Vector2(.28f,.45f),.23f);Circle(vh,new Vector2(.72f,.45f),.23f);}
            else
            {
                var points=new Vector2[48];for(int i=0;i<points.Length;i++){float t=i*Mathf.PI*2/points.Length;float x=16*Mathf.Pow(Mathf.Sin(t),3)/36;float y=(13*Mathf.Cos(t)-5*Mathf.Cos(2*t)-2*Mathf.Cos(3*t)-Mathf.Cos(4*t))/36;points[i]=new Vector2(.5f+x,.5f+(Kind==1?-y:y)*.85f);}Polygon(vh,points);
            }
            if(Kind==1||Kind==2)Polygon(vh,new[]{new Vector2(.5f,.45f),new Vector2(.33f,.04f),new Vector2(.67f,.04f)});
        }
        void Circle(VertexHelper vh,Vector2 center,float radius){var p=new Vector2[32];for(int i=0;i<p.Length;i++){float a=i*2*Mathf.PI/p.Length;p[i]=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;}Polygon(vh,p);}
        void Polygon(VertexHelper vh,Vector2[] points){int start=vh.currentVertCount;var center=Vector2.zero;foreach(var p in points)center+=p;center/=points.Length;Add(vh,center);foreach(var p in points)Add(vh,p);for(int i=0;i<points.Length;i++)vh.AddTriangle(start,start+i+1,start+(i+1)%points.Length+1);}
        void Add(VertexHelper vh,Vector2 p){var r=rectTransform.rect;vh.AddVert(new Vector3(r.x+p.x*r.width,r.y+p.y*r.height),color,Vector2.zero);}
    }
}
