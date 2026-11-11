using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    public enum FolderViewMode { Large, Details }
    [Serializable] public class FolderViewSettings { public string[] enabledStages; }
    public static class FileTypeLabels
    {
        public static readonly Dictionary<string,string> Extensions=new(StringComparer.OrdinalIgnoreCase){
            {"txt","텍스트 문서"},{"md","문서"},{"dir","문서"},{"png","이미지 파일"},{"jpg","이미지 파일"},{"jpeg","이미지 파일"},{"gif","이미지 파일"},{"bmp","이미지 파일"},
            {"mp3","음악 파일"},{"wav","음악 파일"},{"ogg","음악 파일"},{"exe","응용 프로그램"},{"zip","압축(ZIP) 파일"},{"ico","아이콘 파일"}};
        public static string For(FakeIcon icon)
        {
            if(icon.SpriteKey=="folder"||icon.SpriteKey=="computer")return "파일 폴더";
            var name=icon.GetComponentInChildren<TMP_Text>().text;int dot=name.LastIndexOf('.');
            if(dot>=0&&Extensions.TryGetValue(name.Substring(dot+1),out var label))return label;
            return "파일";
        }
    }
    // Presentation only: the original FakeIcon instances and their file-operation callbacks are retained.
    public sealed class FolderView:MonoBehaviour
    {
        public sealed class MenuItem { public string Id,Label,Type;public MenuItem(string id,string label,string type){Id=id;Label=label;Type=type;} }
        public static readonly MenuItem[] ViewMenuItems={new("details","자세히 보기","radio"),new("large","크게 보기","radio"),new("hidden","숨김파일 표시","toggle")};
        public FolderViewMode ViewMode{get;private set;}
        public FakeWindow Window{get;private set;}
        public bool MenuOpen=>menu;
        public RectTransform FileArea;
        Button menuButton;GameObject menu;bool restoring;readonly List<Entry> entries=new();readonly List<Group> groups=new();
        sealed class Entry
        {
            public FakeIcon Icon;public RectTransform Parent;public Vector2 Position,Size;public int Sibling;
        }
        sealed class Group
        {
            public RectTransform Parent,Root,Rows,Viewport;public ScrollRect PreviousScroll;public bool ScrollEnabled,VBarActive,HBarActive;
            public Vector2 ParentSize,ParentPosition;public TMP_Text NameHeader,TypeHeader;public float Width,Height;
        }
        public static bool EnabledForCurrentStage()
        {
            var text=Resources.Load<TextAsset>("FolderViewSettings");var id=GameApp.Instance.Stages.Current?.stageId;
            return text&&JsonUtility.FromJson<FolderViewSettings>(text.text).enabledStages.Contains(id);
        }
        public static FolderView Attach(FakeWindow window)
        {
            if(!EnabledForCurrentStage())return null;
            var view=window.GetComponent<FolderView>();if(view)return view;
            view=window.gameObject.AddComponent<FolderView>();view.Window=window;view.EnsureMenu();return view;
        }
        public static void BeforeClear(Transform parent)
        {
            var view=parent.GetComponentInParent<FolderView>();if(!view||view.restoring)return;
            if(view.entries.Any(e=>e.Parent&&(e.Parent==parent||e.Parent.IsChildOf(parent))))view.Restore();
        }
        void EnsureMenu()
        {
            if(!menuButton)menuButton=UIFactory.Button(Window.Toolbar,"보기",4,2,74,30,ToggleMenu);
            if(!Window.HasHiddenToggle&&Window.Key!="sorting"&&!Window.Key.StartsWith("category:"))Window.AddHiddenToggle(()=>{});
            var old=Window.Toolbar.Find("설정 · 숨김파일 표시");if(old)old.gameObject.SetActive(false);
        }
        public void ToggleMenu()
        {
            if(menu){CloseMenu();return;}Window.Focus();EnsureMenu();
            var app=GameApp.Instance;var block=UIFactory.Panel(app.Modal,"폴더 보기 메뉴",0,0,1024,768,Color.clear);menu=block.gameObject;
            block.gameObject.AddComponent<Button>().onClick.AddListener(CloseMenu);
            var corners=new Vector3[4];((RectTransform)menuButton.transform).GetWorldCorners(corners);var pos=(Vector2)app.Modal.InverseTransformPoint(corners[0]);
            var panel=UIFactory.Panel(block.transform,"항목",Mathf.Clamp(pos.x,0,800),Mathf.Clamp(-pos.y,38,638),220,124,Color.white);WindowSkin.Bevel(panel.rectTransform);
            for(int i=0;i<ViewMenuItems.Length;i++)
            {
                var item=ViewMenuItems[i];var button=UIFactory.Button(panel.transform,item.Label,3,3+i*39,214,38,()=>Select(item.Id),Color.white);
                var text=button.GetComponentInChildren<TMP_Text>();text.alignment=TextAlignmentOptions.MidlineLeft;text.rectTransform.sizeDelta=new Vector2(176,36);
                var colors=button.colors;colors.highlightedColor=WindowSkin.Pink;colors.selectedColor=WindowSkin.Pink;button.colors=colors;
                bool selected=item.Id=="hidden"?Window.ShowHidden:item.Id=="large"?ViewMode==FolderViewMode.Large:ViewMode==FolderViewMode.Details;
                if(selected)UIFactory.CheckMark(button.transform,185,7);
                if(item.Id=="hidden")button.interactable=Window.HasHiddenToggle;
            }
        }
        public void Select(string id)
        {
            CloseMenu();if(id=="hidden"){Window.ToggleHidden();return;}
            var next=id=="details"?FolderViewMode.Details:FolderViewMode.Large;if(next==ViewMode)return;Restore();ViewMode=next;
        }
        void CloseMenu(){if(menu){menu.SetActive(false);Destroy(menu);menu=null;}}
        void OnDisable(){CloseMenu();}
        void LateUpdate()
        {
            if(!Window)return;EnsureMenu();if(ViewMode!=FolderViewMode.Details)return;
            var icons=Window.Content.GetComponentsInChildren<FakeIcon>().Where(i=>i.gameObject.activeInHierarchy).ToArray();
            if(icons.Any(i=>i.IsDragging))return;
            if(groups.Count==0||groups.Any(g=>!g.Parent||!g.Root)||entries.Count!=icons.Length||icons.Any(i=>!entries.Any(e=>e.Icon==i)))
            {
                Restore();Build(icons);
            }
            foreach(var g in groups)if(g.Parent&&g.Root)Layout(g);
        }
        void Build(FakeIcon[] icons)
        {
            foreach(var parent in icons.Select(i=>(RectTransform)i.transform.parent).Distinct().DefaultIfEmpty(FileArea?FileArea:Window.Content).ToArray())
            {
                var batch=icons.Where(i=>i.transform.parent==parent).ToArray();var previous=parent.GetComponentsInParent<ScrollRect>().FirstOrDefault(s=>s.content==parent);
                var g=new Group{Parent=parent,ParentSize=parent.sizeDelta,ParentPosition=parent.anchoredPosition,PreviousScroll=previous,ScrollEnabled=previous&&previous.enabled};
                if(previous){previous.StopMovement();previous.enabled=false;parent.anchoredPosition=Vector2.zero;if(previous.verticalScrollbar){g.VBarActive=previous.verticalScrollbar.gameObject.activeSelf;previous.verticalScrollbar.gameObject.SetActive(false);}if(previous.horizontalScrollbar){g.HBarActive=previous.horizontalScrollbar.gameObject.activeSelf;previous.horizontalScrollbar.gameObject.SetActive(false);}}
                float width=previous?previous.viewport.rect.width:parent.rect.width,height=previous?previous.viewport.rect.height:parent.rect.height;
                g.Root=UIFactory.Panel(parent,"자세히 보기",0,0,width,height,Color.white).rectTransform;
                g.NameHeader=UIFactory.Text(g.Root,"이름",34,3,width*.65f-38,27,18);g.TypeHeader=UIFactory.Text(g.Root,"유형",width*.65f,3,width*.35f-4,27,18);
                UIFactory.Panel(g.Root,"헤더 선",0,30,width,1,WindowSkin.Purple).raycastTarget=false;
                g.Viewport=UIFactory.Panel(g.Root,"목록 보기 영역",0,32,width-16,Mathf.Max(32,height-32),Color.white).rectTransform;g.Viewport.gameObject.AddComponent<RectMask2D>();
                g.Rows=UIFactory.Rect(g.Viewport,"행",0,0,width-16,Mathf.Max(height-32,batch.Count()*34));
                var scroll=g.Root.gameObject.AddComponent<ScrollRect>();scroll.viewport=g.Viewport;scroll.content=g.Rows;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=34;
                var track=UIFactory.Panel(g.Root,"스크롤바",width-16,32,16,Mathf.Max(32,height-32),WindowSkin.Pink);var handle=UIFactory.Panel(track.transform,"손잡이",0,0,16,30,WindowSkin.Purple);handle.rectTransform.sizeDelta=Vector2.zero;handle.rectTransform.pivot=new Vector2(.5f,.5f);
                var bar=track.gameObject.AddComponent<Scrollbar>();bar.targetGraphic=handle;bar.handleRect=handle.rectTransform;bar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=bar;
                // A row remains a descendant of its original file area, retaining folder drop routing.
                var originalIndices=batch.ToDictionary(i=>i,i=>i.transform.GetSiblingIndex());
                foreach(var icon in batch)
                {
                    entries.Add(new Entry{Icon=icon,Parent=parent,Position=icon.Rect.anchoredPosition,Size=icon.Rect.sizeDelta,Sibling=originalIndices[icon]});
                    icon.transform.SetParent(g.Rows,false);
                }
                groups.Add(g);Layout(g);
            }
        }
        void Layout(Group g)
        {
            float width=g.PreviousScroll?g.PreviousScroll.viewport.rect.width:g.Parent.rect.width;
            float height=g.PreviousScroll?g.PreviousScroll.viewport.rect.height:g.Parent.rect.height;
            width=Mathf.Max(160,width);height=Mathf.Max(68,height);g.Root.sizeDelta=new Vector2(width,height);g.Viewport.sizeDelta=new Vector2(width-16,height-32);
            var rows=entries.Where(e=>e.Parent==g.Parent&&e.Icon).ToArray();float column=(width-16)*.64f;
            g.Rows.sizeDelta=new Vector2(width-16,Mathf.Max(height-32,rows.Length*34));
            g.NameHeader.rectTransform.sizeDelta=new Vector2(column-38,27);g.TypeHeader.rectTransform.anchoredPosition=new Vector2(column,-3);g.TypeHeader.rectTransform.sizeDelta=new Vector2(width-column-4,27);
            var track=(RectTransform)g.Root.Find("스크롤바");track.anchoredPosition=new Vector2(width-16,-32);track.sizeDelta=new Vector2(16,height-32);
            ((RectTransform)g.Root.Find("헤더 선")).sizeDelta=new Vector2(width,1);
            for(int i=0;i<rows.Length;i++){var icon=rows[i].Icon;icon.Rect.anchoredPosition=new Vector2(0,-i*34);icon.SetDetails(true,width-16,column,FileTypeLabels.For(icon));}
        }
        void Restore()
        {
            restoring=true;
            foreach(var e in entries)if(e.Icon&&e.Parent){e.Icon.transform.SetParent(e.Parent,false);e.Icon.Rect.anchoredPosition=e.Position;e.Icon.Rect.sizeDelta=e.Size;e.Icon.transform.SetSiblingIndex(e.Sibling);e.Icon.SetDetails(false,0,0,null);}
            foreach(var g in groups){if(g.PreviousScroll){g.PreviousScroll.enabled=g.ScrollEnabled;if(g.PreviousScroll.verticalScrollbar)g.PreviousScroll.verticalScrollbar.gameObject.SetActive(g.VBarActive);if(g.PreviousScroll.horizontalScrollbar)g.PreviousScroll.horizontalScrollbar.gameObject.SetActive(g.HBarActive);if(g.Parent){g.Parent.sizeDelta=g.ParentSize;g.Parent.anchoredPosition=g.ParentPosition;}}if(g.Root){g.Root.gameObject.SetActive(false);Destroy(g.Root.gameObject);}}
            entries.Clear();groups.Clear();restoring=false;
        }
    }
}
