using UnityEngine;
namespace CoComputar
{
    [CreateAssetMenu(menuName="Co-computar/Desktop Theme")]
    public sealed class DesktopTheme : ScriptableObject
    {
        public Color top=new Color32(200,245,154,255), bottom=new Color32(139,224,78,255);
        public void Draw(Transform root)
        {
            var texture=new Texture2D(256,192,TextureFormat.RGB24,false){filterMode=FilterMode.Point};
            for(int y=0;y<192;y++)for(int x=0;x<256;x++)
            {var c=Color.Lerp(bottom,top,y/191f);float d=((x+y)%2==0?1:-1)*.018f;texture.SetPixel(x,y,c+new Color(d,d,d,0));}
            texture.Apply();var sprite=Sprite.Create(texture,new Rect(0,0,256,192),Vector2.one*.5f);
            var image=UIFactory.Sprite(root,sprite,0,38,1024,730);image.preserveAspect=false;
            var cleanup=image.gameObject.AddComponent<DesktopThemeTexture>();cleanup.Texture=texture;cleanup.Sprite=sprite;
        }
    }
    public sealed class DesktopThemeTexture : MonoBehaviour
    {
        public Texture2D Texture;public Sprite Sprite;
        void OnDestroy(){Destroy(Sprite);Destroy(Texture);}
    }
}
