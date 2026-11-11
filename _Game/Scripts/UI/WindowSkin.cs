using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    // Shared, resolution independent window chrome. Content keeps its original logical coordinates.
    public static class WindowSkin
    {
        public static readonly Color Pink=new Color32(249,207,242,255);
        public static readonly Color Purple=new Color32(143,92,226,255);
        public static readonly Color Light=new Color32(251,240,255,255);
        public static readonly Color Shadow=new Color32(66,43,96,255);
        static Texture2D titleTexture;
        public static void Title(RectTransform rect)
        {
            if(!titleTexture)
            {
                titleTexture=new Texture2D(512,32,TextureFormat.RGBA32,false){name="창 제목 디더",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
                for(int y=0;y<32;y++)for(int x=0;x<512;x++)
                {
                    float fade=Mathf.Clamp01(x/380f);
                    var c=Color.Lerp(new Color32(126,67,236,255),new Color32(215,171,240,255),fade);
                    if((x+y)%2==0)c=Color.Lerp(c,Light,.65f*(1-fade));
                    titleTexture.SetPixel(x,y,c);
                }
                titleTexture.Apply(false,true);
            }
            var image=rect.gameObject.AddComponent<RawImage>();image.texture=titleTexture;
        }
        public static void Bevel(RectTransform rect,bool inset=false)
        {
            var edge=UIFactory.Rect(rect,"입체 테두리",0,0,0,0);edge.anchorMin=Vector2.zero;edge.anchorMax=Vector2.one;edge.offsetMin=edge.offsetMax=Vector2.zero;
            var bevel=edge.gameObject.AddComponent<WindowBevel>();bevel.Inset=inset;bevel.raycastTarget=false;
        }
        public static void FrameFill(RectTransform rect)
        {
            // Keep chrome pink even in a lens window with a transparent client area.
            foreach(var spec in new[]{new Vector4(0,0,1,1),new Vector4(0,0,0,1),new Vector4(1,0,1,1),new Vector4(0,0,1,0)})
            {
                var strip=UIFactory.Panel(rect,"분홍 프레임",0,0,0,0,Pink);strip.raycastTarget=false;
                var r=strip.rectTransform;r.anchorMin=new Vector2(spec.x,spec.y);r.anchorMax=new Vector2(spec.z,spec.w);
                r.offsetMin=r.offsetMax=Vector2.zero;
                if(spec==new Vector4(0,0,1,1)){r.anchorMin=new Vector2(0,1);r.offsetMin=new Vector2(0,-77);}
                else if(spec.x==spec.z){r.sizeDelta=new Vector2(7,0);r.pivot=new Vector2(spec.x,.5f);}
                else {r.sizeDelta=new Vector2(0,7);r.pivot=new Vector2(.5f,0);}
            }
        }
    }
    public sealed class WindowBevel : MaskableGraphic
    {
        public bool Inset;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;var a=Inset?WindowSkin.Purple:WindowSkin.Light;var b=Inset?WindowSkin.Light:WindowSkin.Shadow;
            Quad(vh,r.xMin,r.yMax-2,r.width,2,a);Quad(vh,r.xMin,r.yMin,2,r.height,a);
            Quad(vh,r.xMin,r.yMin,r.width,2,b);Quad(vh,r.xMax-2,r.yMin,2,r.height,b);
        }
        static void Quad(VertexHelper vh,float x,float y,float w,float h,Color color)
        {
            int n=vh.currentVertCount;vh.AddVert(new Vector3(x,y),color,Vector2.zero);vh.AddVert(new Vector3(x,y+h),color,Vector2.zero);vh.AddVert(new Vector3(x+w,y+h),color,Vector2.zero);vh.AddVert(new Vector3(x+w,y),color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
}
