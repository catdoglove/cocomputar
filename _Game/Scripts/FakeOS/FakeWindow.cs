using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed class FakeWindow : MonoBehaviour, IPointerDownHandler
    {
        public RectTransform Content { get; private set; }
        public RectTransform Toolbar { get; private set; }
        RectTransform well;
        string hiddenViewOverride;
        string HiddenView => hiddenViewOverride ?? ( Key=="sorting" || Key.StartsWith("category:") ? "explorer" : Key);
        bool localHidden,independentHidden;
        public bool ShowHidden => independentHidden?localHidden:GameApp.Instance.Save.Hidden(HiddenView);
        RectTransform hiddenMark;
        public bool HasHiddenToggle => hiddenMark;
        Action hiddenChanged;
        public Action Closed;
        public string Key { get; private set; }
        public FakeWindow ParentWindow { get; private set; }
        public bool IsClosing => closing;
        public void SetParentWindow(FakeWindow parent)
        {
            if(closing || (parent && parent.closing))return;
            for(var ancestor=parent;ancestor;ancestor=ancestor.ParentWindow)
                if(ancestor==this)return;
            ParentWindow=parent;
        }
        GameObject lastSelection;
        bool closing;
        public RectTransform Rect => (RectTransform)transform;
        // 모든 열기 경로는 종류/문서 ID로 식별한다. 기존 창이면 내용 생성도 건너뛴다.
        public static bool TryCreate(Transform parent, string key, string title, out FakeWindow window, float x=185, float y=130, float w=650, float h=465)
        {
            foreach(var existing in parent.GetComponentsInChildren<FakeWindow>())
                if(existing.Key==key && !existing.closing)
                { window=existing; existing.Focus(); return false; }
            window=Create(parent,key,title,x,y,w,h); return true;
        }
        static FakeWindow Create(Transform parent, string key, string title, float x, float y, float w, float h)
        {
            var border = UIFactory.Panel(parent,title,x,y,w,h,WindowSkin.Shadow);
            var self=border.gameObject.AddComponent<FakeWindow>();self.Key=key;
            var bg=UIFactory.Panel(border.transform,"배경",3,3,w-6,h-6,WindowSkin.Pink);
            WindowSkin.FrameFill(bg.rectTransform);WindowSkin.Bevel(bg.rectTransform);
            var bar=UIFactory.Rect(border.transform,"제목 표시줄",8,8,w-16,32);WindowSkin.Title(bar);
            var caption=UIFactory.Text(bar,title,8,1,w-70,30,20,Color.white);
            caption.textWrappingMode=TMPro.TextWrappingModes.NoWrap;caption.overflowMode=TMPro.TextOverflowModes.Ellipsis;
            bar.gameObject.AddComponent<WindowDrag>().Window=self;
            UIFactory.Button(bar,"X",w-50,2,28,27,self.Close);
            self.Toolbar=UIFactory.Rect(border.transform,"툴바",10,43,w-20,34);
            self.well=UIFactory.Panel(border.transform,"내용 테두리",10,80,w-20,h-90,Color.white).rectTransform;
            WindowSkin.Bevel(self.well,true);
            self.Content=UIFactory.Rect(border.transform,"내용",12,82,w-20,h-55);
            self.Resize(new Vector2(w,h));
            GameApp.Instance.Audio.Play("sfx_window_open"); self.StartCoroutine(self.Open()); return self;
        }
        IEnumerator Open()
        {
            for(float t=0;t<.15f;t+=Time.unscaledDeltaTime) { transform.localScale=Vector3.one*Mathf.Lerp(.04f,1,1-Mathf.Pow(1-t/.15f,3)); yield return null; }
            transform.localScale=Vector3.one;
            if(transform.GetSiblingIndex()==transform.parent.childCount-1) Focus();
        }
        public void SetFileBackground(float top=0)
        {
            FolderView.Attach(this);

        }
        public void AddHiddenToggle(Action changed,bool independent=false)
        {
            if(HasHiddenToggle) { hiddenChanged=changed; independentHidden=independent; return; }
            independentHidden=independent;
            hiddenChanged=changed;
            var toggle=UIFactory.Button(Toolbar,"설정 · 숨김파일 표시",5,2,300,30,ToggleHidden);
            hiddenMark=UIFactory.CheckMark(toggle.transform,270,1);hiddenMark.gameObject.SetActive(ShowHidden);
        }
        public void ToggleHidden()
        {
            if(!HasHiddenToggle)return;
            if(independentHidden){localHidden=!localHidden;hiddenMark.gameObject.SetActive(localHidden);hiddenChanged?.Invoke();return;}
            GameApp.Instance.Save.SetHidden(HiddenView,!ShowHidden);
            foreach(var window in transform.parent.GetComponentsInChildren<FakeWindow>())
                if(window.HiddenView==HiddenView&&window.hiddenMark){window.hiddenMark.gameObject.SetActive(window.ShowHidden);window.hiddenChanged?.Invoke();}
        }
        public Button AddToolbarRightButton(string label,float width,float height,float top,Action action)
        {
            var button=UIFactory.Button(Toolbar,label,0,top,width,height,action);
            var rect=(RectTransform)button.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,1);
            rect.anchoredPosition=new Vector2(-4,-top);return button;
        }
        public void SetTitle(string title){transform.Find("제목 표시줄").GetComponentInChildren<TMPro.TMP_Text>().text=title;gameObject.name=title;}
        public void SetHiddenView(string view){hiddenViewOverride=view;if(hiddenMark)hiddenMark.gameObject.SetActive(ShowHidden);}
        void Update()
        {
            var selected=EventSystem.current?.currentSelectedGameObject;
            if(selected && selected.transform.IsChildOf(transform)) lastSelection=selected;
        }
        public void Focus()
        {
            if(closing || !gameObject.activeInHierarchy) return;
            transform.SetAsLastSibling();
            if(!EventSystem.current) return;
            if(lastSelection && lastSelection.activeInHierarchy) EventSystem.current.SetSelectedGameObject(lastSelection);
            else
            {
                var target=Content.GetComponentInChildren<Selectable>();
                EventSystem.current.SetSelectedGameObject(target ? target.gameObject : gameObject);
            }
        }
        public void OnPointerDown(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left)Focus(); }
        // 비율 기반 창에서만 호출한다. 기존 고정 크기 창은 그대로 유지한다.
        public void Resize(Vector2 size)
        {
            Rect.sizeDelta=size;
            ((RectTransform)transform.Find("배경")).sizeDelta=size-Vector2.one*6;
            var bar=(RectTransform)transform.Find("제목 표시줄");bar.sizeDelta=new Vector2(size.x-16,32);
            bar.GetComponentInChildren<TMPro.TMP_Text>().rectTransform.sizeDelta=new Vector2(size.x-70,30);
            ((RectTransform)bar.Find("X")).anchoredPosition=new Vector2(size.x-50,-2);
            Toolbar.sizeDelta=new Vector2(size.x-20,34);
            well.sizeDelta=new Vector2(size.x-20,Mathf.Max(12,size.y-90));
            Content.sizeDelta=size-new Vector2(20,55);
            // Uniform fit preserves image aspect ratios, existing puzzle coordinates and hit tests.
            float scale=Mathf.Min((well.rect.width-4)/Content.rect.width,(well.rect.height-4)/Content.rect.height);
            Content.localScale=Vector3.one*scale;
            Content.anchoredPosition=new Vector2(10+(well.rect.width-Content.rect.width*scale)*.5f,-82);

        }
        // Screen-space lens rendering must remain one logical pixel per desktop pixel.
        public void UseScreenSpaceContent()
        {
            Content.localScale=Vector3.one;Content.sizeDelta=well.sizeDelta-Vector2.one*4;
            Content.anchoredPosition=new Vector2(12,-82);well.GetComponent<Image>().color=Color.clear;well.GetComponent<Image>().raycastTarget=false;
        }
        public void Clamp()
        {
            var p = Rect.anchoredPosition; var size=((RectTransform)transform.parent).rect.size;
            p.x=Mathf.Clamp(p.x,32-Rect.rect.width,size.x-32); p.y=Mathf.Clamp(p.y,-size.y+36,-36); Rect.anchoredPosition=p;
        }
        public void Close()
        {
            if(closing) return;
            closing=true;
            // Ownership is logical: child dialogs remain siblings in the window layer.
            foreach(var child in transform.parent.GetComponentsInChildren<FakeWindow>(true))
                if(child.ParentWindow==this)child.Close();
            ParentWindow=null;
            gameObject.SetActive(false); Destroy(gameObject);
            GameApp.Instance.Audio.Play("sfx_window_close"); Closed?.Invoke();
        }
    }
    public sealed class WindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public FakeWindow Window; Vector2 last;
        public void OnBeginDrag(PointerEventData e) { if(e.button!=PointerEventData.InputButton.Left)return;Window.Focus(); RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Window.transform.parent,e.position,e.pressEventCamera,out last); }
        public void OnDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left)return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Window.transform.parent,e.position,e.pressEventCamera,out var now);
            Window.Rect.anchoredPosition+=now-last; last=now; Window.Clamp();
        }
    }
}
