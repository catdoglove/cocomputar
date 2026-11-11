using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    // 원본 스프라이트의 UV를 작은 사각형으로 나눠 별도 텍스처 복사 없이 흩날린다.
    public sealed class PixelDriftGraphic : MaskableGraphic
    {
        struct Grain { public Vector2 origin;public Rect uv;public float delay,life,speed,sway,phase; }
        Grain[] grains;Texture texture;float elapsed;Vector2 cell;
        public const float Duration=3.4f;
        public override Texture mainTexture=>texture?texture:s_WhiteTexture;
        public float Elapsed=>elapsed;
        public static PixelDriftGraphic Create(Image source,int seed,float opacity)
        {
            var rect=UIFactory.Rect(source.transform.parent,"떠오르는 편지 픽셀",source.rectTransform.anchoredPosition.x,-source.rectTransform.anchoredPosition.y,source.rectTransform.rect.width,source.rectTransform.rect.height);
            var effect=rect.gameObject.AddComponent<PixelDriftGraphic>();effect.raycastTarget=false;
            effect.color=new Color(1,1,1,opacity);effect.Setup(source.sprite,seed);source.enabled=false;return effect;
        }
        void Setup(Sprite sprite,int seed)
        {
            texture=sprite.texture;var uv=sprite.textureRect;var rect=rectTransform.rect;
            float scale=Mathf.Min(rect.width/uv.width,rect.height/uv.height);
            float width=uv.width*scale,height=uv.height*scale;
            int cols=Mathf.CeilToInt(width/2),rows=Mathf.CeilToInt(height/2);cell=new Vector2(width/cols,height/rows);
            grains=new Grain[cols*rows];var random=new System.Random(seed);
            float Next()=> (float)random.NextDouble();
            for(int y=0;y<rows;y++) for(int x=0;x<cols;x++)
            {
                grains[y*cols+x]=new Grain{
                    origin=new Vector2(rect.x+(rect.width-width)*.5f+x*cell.x,rect.y+(rect.height-height)*.5f+y*cell.y),
                    uv=new Rect((uv.x+x*uv.width/cols)/texture.width,(uv.y+y*uv.height/rows)/texture.height,uv.width/cols/texture.width,uv.height/rows/texture.height),
                    delay=(1-y/(float)(rows-1))*.8f+Next()*.18f,life=1.5f+Next()*.8f,speed=28+Next()*35,sway=5+Next()*13,phase=Next()*Mathf.PI*2};
            }
            SetAllDirty();
        }
        void Update() {elapsed+=Time.deltaTime;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(grains==null)return;
            foreach(var g in grains)
            {
                float age=Mathf.Max(0,elapsed-g.delay),p=Mathf.Clamp01(age/g.life);if(p>=1)continue;
                var offset=new Vector2((Mathf.Sin(age*2+g.phase)-Mathf.Sin(g.phase))*g.sway+Mathf.Sin(g.phase)*age*7,age*g.speed);
                var pos=g.origin+offset;var tint=color;tint.a*=1-Mathf.SmoothStep(0,1,p);
                int i=vh.currentVertCount;
                vh.AddVert(pos,tint,new Vector2(g.uv.xMin,g.uv.yMin));vh.AddVert(pos+new Vector2(0,cell.y),tint,new Vector2(g.uv.xMin,g.uv.yMax));
                vh.AddVert(pos+cell,tint,new Vector2(g.uv.xMax,g.uv.yMax));vh.AddVert(pos+new Vector2(cell.x,0),tint,new Vector2(g.uv.xMax,g.uv.yMin));
                vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
            }
        }
    }
}
