using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CoComputar
{
    [Serializable] public class ViewerRegion
    {
        public string id,spriteKey,text;public float x,y,width=30,height=30;
        public int minZoom=-99,maxZoom=99;public string color="#FFFFFF";
    }
    [Serializable] public class ViewerDefinition
    {
        public bool fitImage;public string id,title,imageKey;public int zoomInLevels=4,zoomOutLevels;
        public float zoomStep=.5f;public ViewerRegion[] hotspots,overlays;public string[] shrinkSprites;
    }
    public sealed class ImageViewer:MonoBehaviour,IBeginDragHandler,IDragHandler
    {
        public int Level{get;private set;}public event Action<int> OnZoomChanged;
        public Action<string> HotspotClicked;public RectTransform PictureRoot{get;private set;}
        public ViewerDefinition Definition;public FakeWindow Window;public Image Picture;
        Slider slider;Vector2 last;RectTransform viewport;
        readonly Dictionary<string,GameObject> regions=new();readonly HashSet<string> removed=new();
        public static ImageViewer Open(ViewerDefinition data)
        {
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"picture:"+data.id,data.title+" - 이미지 뷰어",out var w,230,110,610,520))return w.GetComponentInChildren<ImageViewer>();
            var panel=UIFactory.Panel(w.Content,"이미지 영역",0,0,590,410,new Color(.92f,.92f,.9f));panel.gameObject.AddComponent<RectMask2D>();
            var self=panel.gameObject.AddComponent<ImageViewer>();self.Window=w;self.Definition=data;self.viewport=panel.rectTransform;
            self.PictureRoot=UIFactory.Rect(panel.transform,"확대 이미지",0,0,590,410);
            self.Picture=UIFactory.Sprite(self.PictureRoot,GameApp.Instance.Catalog.sprites.Get(data.imageKey),0,0,590,410);self.Picture.preserveAspect=data.fitImage;
            self.AddRegions(data.hotspots,true);self.AddRegions(data.overlays,false);
            UIFactory.Button(w.Content,"−",110,420,42,32,()=>self.SetZoom(self.Level-1));
            self.slider=UIFactory.Slider(w.Content,170,420,0,v=>self.SetZoom(Mathf.RoundToInt(v)));self.slider.minValue=-data.zoomOutLevels;self.slider.maxValue=data.zoomInLevels;self.slider.wholeNumbers=true;self.slider.SetValueWithoutNotify(0);
            UIFactory.Button(w.Content,"+",420,420,42,32,()=>self.SetZoom(self.Level+1));self.Refresh();return self;
        }
        void AddRegions(ViewerRegion[] list,bool clickable)
        {
            if(list==null)return;foreach(var region in list)
            {
                var r=region;var area=UIFactory.Panel(PictureRoot,r.id,r.x,r.y,r.width,r.height,Color.clear);regions[r.id]=area.gameObject;
                if(!string.IsNullOrEmpty(r.spriteKey)){var image=UIFactory.Sprite(area.transform,GameApp.Instance.Catalog.sprites.Get(r.spriteKey),0,0,r.width,r.height);if(ColorUtility.TryParseHtmlString(r.color,out var c))image.color=c;}
                if(!string.IsNullOrEmpty(r.text))UIFactory.Text(area.transform,r.text,0,0,r.width,r.height,12,Color.black);
                if(clickable)area.gameObject.AddComponent<Button>().onClick.AddListener(()=>HotspotClicked?.Invoke(r.id));else area.raycastTarget=false;
            }
        }
        public void RemoveRegion(string id){removed.Add(id);Refresh();}
        public void SetZoom(int value)
        {
            value=Mathf.Clamp(value,-Definition.zoomOutLevels,Definition.zoomInLevels);if(value==Level)return;
            Level=value;slider.SetValueWithoutNotify(value);GameApp.Instance.Audio.Play("sfx_zoom");Refresh();OnZoomChanged?.Invoke(Level);
        }
        void Refresh()
        {
            float scale=Mathf.Pow(1+Definition.zoomStep,Level);PictureRoot.localScale=Vector3.one*scale;
            if(Definition.shrinkSprites!=null&&Definition.shrinkSprites.Length>0)Picture.sprite=GameApp.Instance.Catalog.sprites.Get(Definition.shrinkSprites[Mathf.Clamp(-Level,0,Definition.shrinkSprites.Length-1)]);
            foreach(var list in new[]{Definition.hotspots,Definition.overlays})if(list!=null)foreach(var r in list)regions[r.id].SetActive(!removed.Contains(r.id)&&Level>=r.minZoom&&Level<=r.maxZoom);
            Clamp();
        }
        void Clamp()
        {
            var size=PictureRoot.sizeDelta*PictureRoot.localScale.x;var p=PictureRoot.anchoredPosition;
            p.x=size.x<590?(590-size.x)/2:Mathf.Clamp(p.x,590-size.x,0);
            p.y=size.y<410?-(410-size.y)/2:Mathf.Clamp(p.y,0,size.y-410);PictureRoot.anchoredPosition=p;
        }
        public void OnBeginDrag(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,e.position,e.pressEventCamera,out last);}
        public void OnDrag(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,e.position,e.pressEventCamera,out var now);PictureRoot.anchoredPosition+=now-last;last=now;Clamp();}
    }
}
