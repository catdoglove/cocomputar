using System.IO;
using UnityEditor;
using UnityEngine;
// 교체 가능한 임시 도트 아트. 참고 원본 파일은 수정하지 않는다.
public static class Stage02Art
{
    static Texture2D image;static Color ink=new Color32(28,29,38,255);
    static void Begin(int w=128,int h=128){image=new Texture2D(w,h,TextureFormat.RGBA32,false);image.SetPixels(new Color[w*h]);}
    static void Rect(int x,int y,int w,int h,Color c){for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)if(xx>=0&&yy>=0&&xx<image.width&&yy<image.height)image.SetPixel(xx,image.height-1-yy,c);}
    static void Circle(int x,int y,int rx,int ry,Color c){for(int yy=-ry;yy<=ry;yy++)for(int xx=-rx;xx<=rx;xx++)if(xx*xx/(float)(rx*rx)+yy*yy/(float)(ry*ry)<=1)Rect(x+xx,y+yy,1,1,c);}
    static void Poly(Color c,params Vector2[] points){for(int y=0;y<image.height;y++)for(int x=0;x<image.width;x++){bool inside=false;for(int i=0,j=points.Length-1;i<points.Length;j=i++)if((points[i].y>y)!=(points[j].y>y)&&x<(points[j].x-points[i].x)*(y-points[i].y)/(points[j].y-points[i].y)+points[i].x)inside=!inside;if(inside)Rect(x,y,1,1,c);}}
    static void Save(string name){image.Apply();File.WriteAllBytes("Assets/_Game/Art/Stage02/"+name+".png",image.EncodeToPNG());Object.DestroyImmediate(image);}
    static void Cursor(bool white,int ox=0,int oy=0){Poly(ink,new(20+ox,10+oy),new(100+ox,56+oy),new(68+ox,71+oy),new(85+ox,102+oy),new(64+ox,113+oy),new(48+ox,80+oy),new(21+ox,102+oy));Poly(white?Color.white:new Color(.1f,.1f,.12f),new(25+ox,18+oy),new(89+ox,56+oy),new(62+ox,68+oy),new(78+ox,100+oy),new(66+ox,105+oy),new(50+ox,72+oy),new(26+ox,90+oy));}
    public static void Generate()
    {
        Directory.CreateDirectory("Assets/_Game/Art/Stage02");
        foreach(bool white in new[]{false,true})
        {
            string key=white?"twin_white":"twin_black";Begin();Cursor(white);Save(key);
            Begin(192,256);Poly(ink,new(48,110),new(132,110),new(173,153),new(184,255),new(8,255),new(17,153));Poly(new Color(.92f,.92f,.86f),new(50,116),new(130,116),new(166,156),new(176,255),new(16,255),new(24,156));
            Rect(91,137,3,118,ink);for(int i=0;i<4;i++)Circle(101,164+i*24,2,2,ink);Poly(Color.gray,new(48,115),new(90,139),new(68,157));Poly(Color.white,new(130,115),new(94,139),new(115,155));Rect(129,174,28,3,ink);Rect(147,164,4,22,ink);Cursor(white,24,0);Circle(42,226,22,11,white?Color.gray:ink);Save("portrait_"+key);
        }
        foreach(string name in new[]{"white_key","black_key"}){Begin();var c=name=="white_key"?Color.white:ink;Circle(63,28,23,23,Color.gray);Circle(63,28,19,19,c);Circle(63,28,10,10,Color.clear);Rect(57,47,14,67,c);Rect(70,90,18,9,c);Rect(70,106,18,8,c);Save(name);}
        Begin();Rect(20,18,88,102,ink);Rect(25,27,78,87,Color.gray);for(int i=0;i<4;i++)Rect(35+i*17,35,5,68,Color.white);Rect(14,18,100,8,ink);Rect(45,8,38,8,ink);Save("trash");
        Begin();Rect(20,20,88,88,Color.black);Save("black_square");
        foreach(string name in new[]{"cloud_drawn","cloud_drawn2","cloud_photo"})
        {Begin(192,100);bool photo=name=="cloud_photo";for(int i=0;i<7;i++){var c=photo?new Color(1-i*.025f,.85f-i*.02f,.9f-i*.015f):Color.white;Circle(30+i*22,55-(i%3)*12,30,photo?23+i%3*4:25,c);}Save(name);}
        foreach(string name in new[]{"sprout","bud","photo_bloom","tulip","bouquet"})
        {Begin();for(int n=0;n<(name=="bouquet"?3:1);n++){int x=64+(n-1)*20;Rect(x,45,5,70,new Color(.15f,.47f,.19f));Circle(x-12,85,15,6,Color.green);Circle(x+13,70,16,7,new Color(.25f,.65f,.25f));if(name!="sprout"){Circle(x,40,16,24,name=="tulip"?Color.yellow:new Color(1,.4f,.65f));if(name=="photo_bloom")Circle(x+5,39,10,17,new Color(1,.65f,.8f));}}Save(name);}
        Begin();Circle(57,69,32,31,new Color(.35f,.55f,.48f));Rect(25,40,64,52,new Color(.45f,.68f,.6f));Poly(new Color(.55f,.75f,.65f),new(85,58),new(119,32),new(125,40),new(88,89));Circle(22,48,20,28,Color.gray);Circle(22,48,13,20,Color.clear);Rect(31,42,48,44,new Color(.4f,.62f,.54f));Save("watering");
        for(int step=0;step<=5;step++)
        {Begin(256,192);float t=step/5f;Color c=Color.Lerp(new Color(.65f,.64f,.55f),new Color(1,.75f,.4f),t);int w=(int)Mathf.Lerp(24,176,t);Rect(128-w/2,65,w,100,c);Circle(128,50,(int)Mathf.Lerp(38,50,t),26,c);if(step<4)Circle(128,50,(int)Mathf.Lerp(22,2,t),12,Color.clear);Rect(128,130,(int)Mathf.Lerp(45,88,t),12,c);if(step>=3)Rect(42,42,75,34,c);Save("key_morph"+step);}
        AssetDatabase.Refresh();
    }
}
