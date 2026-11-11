using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    // 좌표는 1024x768 논리 화면 기준. owner가 있으면 그 이미지 내부 좌표를 따른다.
    public sealed class HiddenObject
    {
        public string id,text,sprite;public LensFilter filter;public RectTransform owner;public Rect bounds;public int size=24;public float rotation;
        public Color tint=new Color(.16f,.1f,.22f);public Func<float> visibility;public Action clicked;
        public bool hadOwner=>!ReferenceEquals(owner,null);
        public Rect ScreenBounds
        {
            get{if(!owner)return bounds;var a=GameApp.Instance.Root.InverseTransformPoint(owner.TransformPoint(new Vector3(bounds.x,-bounds.y,0)));var b=GameApp.Instance.Root.InverseTransformPoint(owner.TransformPoint(new Vector3(bounds.xMax,-bounds.yMax,0)));return new Rect(a.x,-a.y,b.x-a.x,a.y-b.y);}
        }
    }
    public sealed class FilmLensWindow:MonoBehaviour
    {
        public FakeWindow Window{get;private set;}public LensFilter Filter{get;private set;}
        public RectTransform Area{get;private set;}Stage03Controller stage;Image tint;Vector2 last;bool initialized;
        readonly List<RectTransform> fragments=new();int used;
        public static FilmLensWindow Open(Stage03Controller stage)
        {
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"s3:lens",stage.Updated?stage.Data.updatedFilmName:stage.Data.filmName,out var window,450,260,330,275))return window.GetComponent<FilmLensWindow>();
            var self=window.gameObject.AddComponent<FilmLensWindow>();self.Window=window;self.stage=stage;
            window.GetComponent<Image>().color=Color.clear;window.GetComponent<Image>().raycastTarget=false;
            var background=window.transform.Find("배경").GetComponent<Image>();background.color=Color.clear;background.raycastTarget=false;
            window.UseScreenSpaceContent();
            float top=0;float width=window.Content.rect.width,height=window.Content.rect.height;self.Area=UIFactory.Rect(window.Content,"렌즈 영역",0,top,width,height);self.Area.gameObject.AddComponent<RectMask2D>();
            self.tint=UIFactory.Panel(self.Area,"반투명 필름",0,0,width,height,Color.clear);self.tint.raycastTarget=false;
            foreach(var edge in new[]{new Rect(0,0,width,2),new Rect(0,height-2,width,2),new Rect(0,0,2,height),new Rect(width-2,0,2,height)})UIFactory.Panel(self.Area,"렌즈 테두리",edge.x,edge.y,edge.width,edge.height,Color.black).raycastTarget=false;
            if(stage.Updated)
            {
                LensFilter[] filters={LensFilter.Red,LensFilter.Blue,LensFilter.Yellow,LensFilter.Green};
                for(int i=0;i<4;i++){var f=filters[i];var b=UIFactory.Button(window.Toolbar,f.ToString(),i*77.5f,0,77,32,()=>self.Select(f),self.ColorFor(f));b.GetComponentInChildren<TMP_Text>().fontSize=17;}
            }
            self.Select(stage.Updated?LensFilter.Red:LensFilter.Neutral,false);return self;
        }
        public void Select(LensFilter filter,bool sound=true){Filter=filter;var color=ColorFor(filter);color.a=.5f;tint.color=color;if(sound)GameApp.Instance.Audio.Play(stage.Data.filterSound);}
        public Color ColorFor(LensFilter filter)
        {
            var entry=Array.Find(stage.Data.filterColors,x=>x.filter==filter.ToString());return entry!=null&&ColorUtility.TryParseHtmlString(entry.color,out var color)?color:Color.white;
        }

        public static Rect Bounds(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);var root=GameApp.Instance.Root;var a=root.InverseTransformPoint(corners[1]);var b=root.InverseTransformPoint(corners[3]);return new Rect(a.x,-a.y,b.x-a.x,a.y-b.y);
        }
        public bool Contains(Vector2 point)=>Bounds(Area).Contains(point);
        public static Rect Intersect(Rect a,Rect b){float x=Mathf.Max(a.x,b.x),y=Mathf.Max(a.y,b.y);return new Rect(x,y,Mathf.Max(0,Mathf.Min(a.xMax,b.xMax)-x),Mathf.Max(0,Mathf.Min(a.yMax,b.yMax)-y));}
        static List<Rect> Subtract(List<Rect> input,Rect cover)
        {
            var result=new List<Rect>();foreach(var r in input){var c=Intersect(r,cover);if(c.width<=0||c.height<=0){result.Add(r);continue;}
                foreach(var piece in new[]{new Rect(r.x,r.y,r.width,c.y-r.y),new Rect(r.x,c.yMax,r.width,r.yMax-c.yMax),new Rect(r.x,c.y,c.x-r.x,c.height),new Rect(c.xMax,c.y,r.xMax-c.xMax,c.height)})if(piece.width>0&&piece.height>0)result.Add(piece);
            }return result;
        }
        void LateUpdate()
        {
            if(!stage||!Window)return;
            // 일반 창보다 위, 시스템 알림/암호 창보다 아래. 대화는 기존 Modal 레이어가 담당한다.
            Window.transform.SetAsLastSibling();foreach(var w in GameApp.Instance.Windows.GetComponentsInChildren<FakeWindow>())if(w.Key.StartsWith("alert:")||w.Key=="s3:password")w.transform.SetAsLastSibling();
            var p=Window.Rect.anchoredPosition;p.x=Mathf.Clamp(p.x,0,1024-Window.Rect.rect.width);p.y=Mathf.Clamp(p.y,-768+Window.Rect.rect.height,-38);Window.Rect.anchoredPosition=p;
            bool still=initialized&&(p-last).magnitude/Mathf.Max(.001f,Time.unscaledDeltaTime)<stage.Data.trail.stillSpeed&&Window.transform.localScale.x>.99f;
            stage.Track(this,still,Time.unscaledDeltaTime);last=p;initialized=true;
            RenderHidden();
        }
        public bool Shows(string id){foreach(var r in fragments)if(r.gameObject.activeSelf&&r.name==id)return true;return false;}
        void RenderHidden()
        {
            used=0;var lens=Bounds(Area);var windows=GameApp.Instance.Windows.GetComponentsInChildren<FakeWindow>();
            foreach(var h in stage.Hidden)
            {
                float alpha=h.visibility?.Invoke()??1;if(alpha<=0||h.filter!=Filter||h.hadOwner&&(!h.owner||!h.owner.gameObject.activeInHierarchy))continue;
                Rect full=h.ScreenBounds;var clipped=Intersect(full,lens);var owner=h.owner?h.owner.GetComponentInParent<FakeWindow>():null;
                if(h.owner){clipped=Intersect(clipped,Bounds(h.owner));foreach(var mask in h.owner.GetComponentsInParent<RectMask2D>())clipped=Intersect(clipped,Bounds((RectTransform)mask.transform));}if(clipped.width<=0||clipped.height<=0)continue;
                var pieces=new List<Rect>{clipped};foreach(var w in windows)if(w!=Window&&w!=owner&&(owner==null||w.transform.GetSiblingIndex()>owner.transform.GetSiblingIndex()))pieces=Subtract(pieces,Bounds(w.Rect));
                foreach(var piece in pieces)Draw(h,full,piece,lens,alpha);
            }
            for(int i=used;i<fragments.Count;i++)fragments[i].gameObject.SetActive(false);
        }
        void Draw(HiddenObject h,Rect full,Rect clipped,Rect lens,float alpha)
        {
            RectTransform clip;if(used==fragments.Count)
            {
                clip=UIFactory.Rect(Area,h.id,0,0,1,1);clip.gameObject.AddComponent<RectMask2D>();
                var hit=clip.gameObject.AddComponent<Image>();hit.color=Color.clear;
                var button=clip.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;
                UIFactory.Sprite(clip,null,0,0,1,1).name="숨김 그림";UIFactory.Text(clip,"",0,0,1,1).name="숨김 글자";fragments.Add(clip);
            }else clip=fragments[used];used++;clip.name=h.id;clip.gameObject.SetActive(true);
            clip.anchoredPosition=new Vector2(clipped.x-lens.x,-(clipped.y-lens.y));clip.sizeDelta=clipped.size;
            var button2=clip.GetComponent<Button>();button2.onClick.RemoveAllListeners();clip.GetComponent<Image>().raycastTarget=h.clicked!=null;button2.interactable=h.clicked!=null;if(h.clicked!=null)button2.onClick.AddListener(()=>h.clicked());
            var image=clip.Find("숨김 그림").GetComponent<Image>();image.enabled=!string.IsNullOrEmpty(h.sprite);image.sprite=string.IsNullOrEmpty(h.sprite)?null:GameApp.Instance.Catalog.sprites.Get(h.sprite);image.color=new Color(1,1,1,alpha);
            var text=clip.Find("숨김 글자").GetComponent<TMP_Text>();text.text=h.text??"";text.fontSize=h.size*(h.owner&&h.bounds.width>0?full.width/h.bounds.width:1);text.color=new Color(h.tint.r,h.tint.g,h.tint.b,alpha);text.textWrappingMode=TextWrappingModes.NoWrap;
            foreach(var child in new[]{image.rectTransform,text.rectTransform}){child.anchoredPosition=new Vector2(full.x-clipped.x,-(full.y-clipped.y));child.sizeDelta=full.size;}
            image.rectTransform.localRotation=Quaternion.identity;
            if(h.id.StartsWith("trail:")&&!string.IsNullOrEmpty(h.sprite))
            {
                image.rectTransform.pivot=new Vector2(.5f,.5f);image.rectTransform.sizeDelta=new Vector2(24,40);image.rectTransform.anchoredPosition=new Vector2(full.center.x-clipped.x,-(full.center.y-clipped.y));image.rectTransform.localRotation=Quaternion.Euler(0,0,h.rotation);
            }
            else image.rectTransform.pivot=new Vector2(0,1);
            if(h.clicked!=null){image.rectTransform.sizeDelta=new Vector2(full.width,40);text.rectTransform.anchoredPosition+=Vector2.down*40;text.rectTransform.sizeDelta=new Vector2(full.width,25);}
        }
    }
}
