using System;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    // 기존 FakeWindow 안에 고정 논리 해상도의 게임 화면을 종횡비 유지로 배치한다.
    public sealed class DreamWindow:MonoBehaviour
    {
        public const float DREAM_WINDOW_RATIO=.7f;
        public FakeWindow Window{get;private set;}
        public PrologueController Map{get;private set;}
        public RectTransform View{get;private set;}
        public Rect DesktopBounds{get;private set;}
        RectTransform desktop,viewport;Vector2 previousSize;Vector2Int previousResolution;float previousTop=-1,ratio;
        public static DreamWindow Open(RectTransform desktop,float ratio,Action completed)
        {
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"s4:dream","mydream.exe — 접힌 꿈",out var window))return window.GetComponent<DreamWindow>();
            var self=window.gameObject.AddComponent<DreamWindow>();self.Window=window;self.desktop=desktop;self.ratio=Mathf.Clamp(ratio,.3f,.95f);
            window.gameObject.AddComponent<FileOwner>().Id="mydream";
            self.viewport=UIFactory.Panel(window.Content,"꿈 화면 여백",0,0,1,1,new Color(.12f,.12f,.14f)).rectTransform;self.viewport.gameObject.AddComponent<RectMask2D>();
            self.View=UIFactory.Rect(self.viewport,"꿈 논리 화면",0,0,1024,768);self.View.gameObject.AddComponent<RectMask2D>();self.Layout();
            self.Map=window.gameObject.AddComponent<PrologueController>();self.Map.InputAllowed=()=>window.gameObject.activeInHierarchy&&!GameApp.Instance.Dialogue.Active&&window.transform.GetSiblingIndex()==window.transform.parent.childCount-1&&GameApp.Instance.Modal.childCount==0;
            self.Map.Setup(self.View,completed,JsonUtility.FromJson<WanderingMap>(Resources.Load<TextAsset>("DreamMap01").text),true);return self;
        }
        void LateUpdate(){Layout();}
        void Layout()
        {
            var menu=desktop.Find("메뉴바") as RectTransform;float top=menu?menu.rect.height:0;var size=desktop.rect.size;
            if(size!=previousSize||!Mathf.Approximately(top,previousTop)||previousResolution!=new Vector2Int(Screen.width,Screen.height))
            {
                previousSize=size;previousTop=top;previousResolution=new Vector2Int(Screen.width,Screen.height);DesktopBounds=new Rect(0,top,size.x,Mathf.Max(1,size.y-top));var windowSize=DesktopBounds.size*ratio;
                Window.Resize(windowSize);Window.Rect.anchoredPosition=new Vector2((size.x-windowSize.x)*.5f,-(top+(DesktopBounds.height-windowSize.y)*.5f));
            }
            var content=Window.Content.rect.size;viewport.sizeDelta=content;float scale=Mathf.Min(content.x/1024,content.y/768);
            View.localScale=Vector3.one*scale;View.anchoredPosition=new Vector2((content.x-1024*scale)*.5f,-(content.y-768*scale)*.5f);
        }
    }
}
