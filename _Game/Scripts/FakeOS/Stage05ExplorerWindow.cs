using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CoComputar
{
    // Shared Stage05/Stage06 explorer. Folder navigation belongs to this window, file operations to the controller.
    public sealed class Stage05ExplorerWindow : MonoBehaviour
    {
        public const float TreeMinWidth=180,FilesMinWidth=220,DividerWidth=6;
        const float RowHeight=28,Indent=22,BarSize=16;
        static int serial;
        public string CurrentFolderId{get;private set;}
        public FakeWindow Window{get;private set;}
        public float TreeWidth{get;private set;}=255;
        public IReadOnlyCollection<string> Expanded=>expanded;
        readonly HashSet<string> expanded=new();
        readonly FakeFileNode desktopRoot=new(){id="desktop",displayName="바탕화면",contentType="folder",iconKey="computer"};
        bool IsNavigable(string id)=>id=="desktop"||IsExplorerFolder(id);
        FakeFileNode Folder(string id)=>id=="desktop"?desktopRoot:Files.Find(id);
        Stage06Controller stage06;
        FakeFileSystem Files=>stage06?stage06.Files:stage.Files;
        bool Ready=>stage06?stage06.Ready:stage.Ready;
        string Prefix=>stage06?"s6:":"s5:";
        bool IsExplorerFolder(string id)=>stage06?Files.Find(id)?.contentType=="folder"&&Files.CanUse(Files.Find(id)):stage.IsExplorerFolder(id);
        void OpenFile(string id){if(stage06)stage06.Open(id);else stage.Open(id);}
        void AddFileIcon(Transform parent,FakeFileNode node,float x,float y,Action open){if(stage06)stage06.AddIcon(parent,node,x,y,open);else stage.AddIcon(parent,node,x,y,open);}
        IEnumerable<FakeFileNode> Roots()=>stage06?Files.Children("desktop"):new[]{Files.Find("look"),Files.Find("soft")}.Where(n=>n!=null);
        Stage05Controller stage;FileOwner owner;Pane tree,files;RectTransform divider;Vector2 lastSize;bool ready;
        sealed class Pane
        {
            public RectTransform Root,Viewport,Content,VTrack,HTrack;public ScrollRect Scroll;
        }
        public static Stage05ExplorerWindow Open(Stage05Controller stage,string id)
        {
            // Desktop launches intentionally get unique windows; internal navigation never calls Open.
            FakeWindow.TryCreate(GameApp.Instance.Windows,"s5:explorer:"+(++serial),stage.Files.Find(id).displayName,out var window,180,80,790,600);
            var self=window.gameObject.AddComponent<Stage05ExplorerWindow>();self.stage=stage;self.Window=window;
            self.Setup(id);return self;
        }
        public static Stage05ExplorerWindow Open(Stage06Controller stage,string id)
        {
            FakeWindow.TryCreate(GameApp.Instance.Windows,"s6:explorer:"+(++serial),stage.Files.Find(id).displayName,out var window,180,80,790,600);
            var self=window.gameObject.AddComponent<Stage05ExplorerWindow>();self.stage06=stage;self.Window=window;self.Setup(id);return self;
        }
        void Setup(string id)
        {
            FolderView.Attach(Window);owner=Window.gameObject.AddComponent<FileOwner>();tree=MakePane("폴더 트리");files=MakePane("폴더 내용");if(Window.GetComponent<FolderView>())Window.GetComponent<FolderView>().FileArea=files.Content;
            var drop=files.Viewport.gameObject.AddComponent<DropTarget>();drop.IsFolderWindow=true;
            divider=UIFactory.Panel(Window.Content,"트리 구분선",255,0,DividerWidth,500,new Color(.65f,.65f,.65f)).rectTransform;
            divider.gameObject.AddComponent<Stage05ExplorerSplitter>().Explorer=this;
            Window.AddHiddenToggle(()=>Refresh(),stage06!=null);ready=true;Navigate(id);
        }
        Pane MakePane(string name)
        {
            var p=new Pane();p.Root=UIFactory.Panel(Window.Content,name,0,0,250,500,Color.white).rectTransform;
            p.Viewport=UIFactory.Panel(p.Root,"보기 영역",0,0,234,484,Color.white).rectTransform;p.Viewport.gameObject.AddComponent<RectMask2D>();
            p.Content=UIFactory.Rect(p.Viewport,"목록",0,0,234,484);
            p.Scroll=p.Root.gameObject.AddComponent<ScrollRect>();p.Scroll.viewport=p.Viewport;p.Scroll.content=p.Content;p.Scroll.movementType=ScrollRect.MovementType.Clamped;p.Scroll.scrollSensitivity=32;
            p.VTrack=UIFactory.Panel(p.Root,"세로 스크롤바",234,0,BarSize,484,new Color(.87f,.87f,.87f)).rectTransform;
            p.HTrack=UIFactory.Panel(p.Root,"가로 스크롤바",0,484,234,BarSize,new Color(.87f,.87f,.87f)).rectTransform;
            p.Scroll.verticalScrollbar=Bar(p.VTrack,true);p.Scroll.horizontalScrollbar=Bar(p.HTrack,false);return p;
        }
        Scrollbar Bar(RectTransform track,bool vertical)
        {
            var handle=UIFactory.Panel(track,"손잡이",0,0,16,16,new Color(.65f,.65f,.68f));
            handle.rectTransform.sizeDelta=Vector2.zero;handle.rectTransform.pivot=new Vector2(.5f,.5f);
            var bar=track.gameObject.AddComponent<Scrollbar>();bar.targetGraphic=handle;bar.handleRect=handle.rectTransform;bar.direction=vertical?Scrollbar.Direction.BottomToTop:Scrollbar.Direction.LeftToRight;return bar;
        }
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        void LayoutPane(Pane p,float x,float width,float height)
        {
            Place(p.Root,x,0,width,height);Place(p.Viewport,0,0,width-BarSize,height-BarSize);
            Place(p.VTrack,width-BarSize,0,BarSize,height-BarSize);Place(p.HTrack,0,height-BarSize,width-BarSize,BarSize);
        }
        public void SetTreeWidth(float width)
        {
            TreeWidth=Mathf.Clamp(width,TreeMinWidth,Window.Content.rect.width-FilesMinWidth-DividerWidth);Layout();Refresh();
        }
        void Layout()
        {
            var size=Window.Content.rect.size;TreeWidth=Mathf.Clamp(TreeWidth,TreeMinWidth,size.x-FilesMinWidth-DividerWidth);
            LayoutPane(tree,0,TreeWidth,size.y);LayoutPane(files,TreeWidth+DividerWidth,size.x-TreeWidth-DividerWidth,size.y);
            Place(divider,TreeWidth,0,DividerWidth,size.y);lastSize=size;
        }
        void LateUpdate()
        {
            if(!ready)return;
            if(Window.Rect.rect.width<450||Window.Rect.rect.height<280)Window.Resize(new Vector2(Mathf.Max(450,Window.Rect.rect.width),Mathf.Max(280,Window.Rect.rect.height)));
            if(lastSize!=Window.Content.rect.size){Layout();Refresh();}
        }
        public void Navigate(string id)
        {
            if(!Ready||!IsNavigable(id))return;
            CurrentFolderId=id;owner.Id=id;Window.SetTitle(Folder(id).displayName);Window.SetHiddenView(Prefix+"folder:"+id);
            expanded.Add("desktop");var visited=new HashSet<string>();var node=Files.Find(id);
            while(node!=null&&visited.Add(node.id)){expanded.Add(node.id);node=Files.Find(node.parentFolder);}
            Layout();Refresh();files.Content.anchoredPosition=Vector2.zero;RevealSelected();
        }
        void OpenEntry(FakeFileNode node){if(node.contentType=="folder")Navigate(node.id);else OpenFile(node.id);}
        IEnumerable<FakeFileNode> Folders(string parent)=>(parent=="desktop"?Roots():Files.Children(parent)).Where(n=>n.contentType=="folder"&&Files.CanUse(n)&&n.IsVisible(Window.ShowHidden));
        public void Refresh()
        {
            if(!ready)return;
            if(!IsNavigable(CurrentFolderId)){Window.Close();return;}
            Window.SetTitle(Folder(CurrentFolderId).displayName);
            DrawTree();DrawFiles();
        }
        void DrawTree()
        {
            var position=tree.Content.anchoredPosition;UIFactory.Clear(tree.Content);
            var rows=new List<(FakeFileNode node,int depth)>();var visited=new HashSet<string>();
            void Visit(FakeFileNode node,int depth)
            {
                if(!visited.Add(node.id))return;rows.Add((node,depth));if(expanded.Contains(node.id))foreach(var child in Folders(node.id))Visit(child,depth+1);
            }
            Visit(desktopRoot,0);
            float width=Mathf.Max(tree.Viewport.rect.width,rows.Count==0?0:rows.Max(r=>r.depth*Indent+70+r.node.displayName.Length*18));
            tree.Content.sizeDelta=new Vector2(width,Mathf.Max(tree.Viewport.rect.height,rows.Count*RowHeight+4));
            for(int i=0;i<rows.Count;i++)
            {
                var node=rows[i].node;float x=rows[i].depth*Indent,y=i*RowHeight+2;bool selected=node.id==CurrentFolderId;
                var row=UIFactory.Panel(tree.Content,"tree:"+node.id,x+24,y,width-x-24,RowHeight,selected?new Color(0,0,.5f):Color.white);
                var button=row.gameObject.AddComponent<Button>();button.targetGraphic=row;button.onClick.AddListener(()=>Navigate(node.id));
                UIFactory.Sprite(row.transform,GameApp.Instance.Catalog.sprites.Get(node.iconKey),2,2,24,24);
                var label=UIFactory.Text(row.transform,node.displayName,30,2,width-x-56,24,18,selected?Color.white:Color.black);label.textWrappingMode=TextWrappingModes.NoWrap;
                if(node.id!="desktop"){var drop=row.gameObject.AddComponent<DropTarget>();drop.IsFolderWindow=true;drop.Category=Prefix+node.id;}
                if(Folders(node.id).Any())
                {
                    var toggle=UIFactory.Button(tree.Content,expanded.Contains(node.id)?"−":"+",x+4,y+5,17,17,()=>{if(!Ready)return;if(!expanded.Remove(node.id))expanded.Add(node.id);DrawTree();},new Color(.91f,.91f,.91f));
                    toggle.name="toggle:"+node.id;var text=toggle.GetComponentInChildren<TMP_Text>();text.fontSize=16;text.color=Color.black;
                }
            }
            tree.Content.anchoredPosition=position;
        }
        void RevealSelected()
        {
            var row=tree.Content.Find("tree:"+CurrentFolderId) as RectTransform;if(!row)return;
            float y=-row.anchoredPosition.y,top=tree.Content.anchoredPosition.y;
            float next=y<top?y:y+RowHeight>top+tree.Viewport.rect.height?y+RowHeight-tree.Viewport.rect.height:top;
            tree.Content.anchoredPosition=new Vector2(0,Mathf.Clamp(next,0,Mathf.Max(0,tree.Content.rect.height-tree.Viewport.rect.height)));
        }
        void DrawFiles()
        {
            var position=files.Content.anchoredPosition;UIFactory.Clear(files.Content);
            files.Viewport.GetComponent<DropTarget>().Category=CurrentFolderId=="desktop"?"":Prefix+CurrentFolderId;
            var nodes=(CurrentFolderId=="desktop"?Folders("desktop"):Files.Children(CurrentFolderId)).Where(n=>n.IsVisible(Window.ShowHidden)).ToArray();
            int columns=Mathf.Max(1,Mathf.FloorToInt(files.Viewport.rect.width/112));
            files.Content.sizeDelta=new Vector2(files.Viewport.rect.width,Mathf.Max(files.Viewport.rect.height,Mathf.Ceil(nodes.Length/(float)columns)*112+8));
            for(int i=0;i<nodes.Length;i++){var node=nodes[i];AddFileIcon(files.Content,node,8+i%columns*112,8+i/columns*112,()=>OpenEntry(node));}
            files.Content.anchoredPosition=position;
        }
    }
    public sealed class Stage05ExplorerSplitter:MonoBehaviour,IBeginDragHandler,IDragHandler
    {
        public Stage05ExplorerWindow Explorer;
        public void OnBeginDrag(PointerEventData e){ }
        public void OnDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left)return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(Explorer.Window.Content,e.position,e.pressEventCamera,out var p))Explorer.SetTreeWidth(p.x);
        }
    }
}
