using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed class FakeIcon : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string Id;
        public string SpriteKey;public bool IsDetails{get;private set;}public bool IsDragging=>dragging;
        TMPro.TMP_Text typeLabel;
        Image rowHover;bool pointerOver,originalBlocksRaycasts;
        public bool RowHovered => rowHover && rowHover.enabled;
        public void OnPointerEnter(PointerEventData e)
        {
            // Unity equivalent of hover-capable input: never retain a touch hover.
            pointerOver=!(e is UnityEngine.InputSystem.UI.ExtendedPointerEventData p) || p.pointerType!=UnityEngine.InputSystem.UI.UIPointerType.Touch;
        }
        public void OnPointerExit(PointerEventData e){pointerOver=false;}
        void LateUpdate()
        {
            if(!rowHover)return;
            var selection=EventSystem.current?EventSystem.current.currentSelectedGameObject:null;
            bool selected=selection&&(selection==gameObject||selection.transform.IsChildOf(transform));
            // Keep selection/drop tints on the existing row Image above the hover layer.
            rowHover.enabled=IsDetails&&pointerOver&&!dragging&&!selected&&GetComponent<Image>().color.a==0&&!GetComponentInChildren<TMPro.TMP_InputField>();
        }
        public bool Draggable;
        public bool AllowWindowDrops;
        public bool ScreenBoundDrag;
        public bool RejectOutsideDrop;
        public Action Clicked, DoubleClicked;
        public Action<FakeIcon, DropTarget> Dropped;
        public Action<PointerEventData> DroppedOutside, DropWithoutTarget;
        Vector2 origin, offset; bool dragging;
        Image artwork;
        public Image Artwork => artwork;
        RectTransform dragVisual;CanvasGroup originalGroup;float originalAlpha;
        public RectTransform DragVisual=>dragVisual;
        void BeginDragVisual()
        {
            var app=GameApp.Instance;
            dragVisual=UIFactory.Rect(app.DragLayer,"드래그 중: "+Id,0,0,92,100);
            var picture=UIFactory.Sprite(dragVisual,artwork.sprite,16,0,60,64);picture.color=artwork.color;
            var sourceLabel=GetComponentInChildren<TMPro.TextMeshProUGUI>();
            var label=UIFactory.Text(dragVisual,sourceLabel.text,0,66,92,34,sourceLabel.fontSize,sourceLabel.color);label.alignment=sourceLabel.alignment;
            float alpha=1;foreach(var group in GetComponentsInParent<CanvasGroup>())alpha*=group.alpha;
            var visualGroup=dragVisual.gameObject.AddComponent<CanvasGroup>();visualGroup.alpha=alpha;visualGroup.blocksRaycasts=false;
            originalGroup=GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();originalAlpha=originalGroup.alpha;originalBlocksRaycasts=originalGroup.blocksRaycasts;originalGroup.alpha=0;originalGroup.blocksRaycasts=false;
            UpdateDragVisual();
        }
        void UpdateDragVisual() {if(dragVisual) {dragVisual.position=Rect.position;dragVisual.localScale=Rect.lossyScale/GameApp.Instance.DragLayer.lossyScale.x;}}
        void EndDragVisual()
        {
            if(originalGroup){originalGroup.alpha=originalAlpha;originalGroup.blocksRaycasts=originalBlocksRaycasts;}
            if(dragVisual) {dragVisual.gameObject.SetActive(false);Destroy(dragVisual.gameObject);}
            originalGroup=null;dragVisual=null;
        }
        public RectTransform Rect => (RectTransform)transform;
        public static FakeIcon Create(Transform parent,string id,string label,string spriteKey,float x,float y,Action click=null,bool drag=false,bool hidden=false)
        {
            var bg=UIFactory.Panel(parent,label,x,y,92,100,new Color(1,1,1,0));
            var icon=bg.gameObject.AddComponent<FakeIcon>(); icon.Id=id; icon.SpriteKey=spriteKey; icon.Clicked=click; icon.Draggable=drag;
            icon.artwork=UIFactory.Sprite(bg.transform,GameApp.Instance.Catalog.sprites.Get(spriteKey),16,0,60,64);
            var text=UIFactory.Text(bg.transform,label,0,66,92,34,16); text.alignment=TMPro.TextAlignmentOptions.Top;
            if(hidden) bg.gameObject.AddComponent<CanvasGroup>().alpha=.5f;
            return icon;
        }
        public void SetDetails(bool details,float width,float column,string type)
        {
            IsDetails=details;var label=GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if(details)
            {
                if(!rowHover){rowHover=UIFactory.Panel(transform,"행 호버",0,0,width,34,ColorBlock.defaultColorBlock.highlightedColor);rowHover.raycastTarget=false;rowHover.transform.SetAsFirstSibling();rowHover.enabled=false;}
                rowHover.rectTransform.sizeDelta=new Vector2(width,34);
                Rect.sizeDelta=new Vector2(width,34);artwork.rectTransform.anchoredPosition=new Vector2(4,-3);artwork.rectTransform.sizeDelta=new Vector2(26,28);
                label.rectTransform.anchoredPosition=new Vector2(34,0);label.rectTransform.sizeDelta=new Vector2(Mathf.Max(40,column-38),34);label.alignment=TMPro.TextAlignmentOptions.MidlineLeft;label.textWrappingMode=TMPro.TextWrappingModes.NoWrap;label.overflowMode=TMPro.TextOverflowModes.Ellipsis;
                if(!typeLabel)typeLabel=UIFactory.Text(transform,type,column,0,width-column,34,16);
                typeLabel.text=type;typeLabel.rectTransform.anchoredPosition=new Vector2(column,0);typeLabel.rectTransform.sizeDelta=new Vector2(width-column-4,34);typeLabel.alignment=TMPro.TextAlignmentOptions.MidlineLeft;typeLabel.enableAutoSizing=true;typeLabel.fontSizeMin=10;typeLabel.fontSizeMax=16;
            }
            else
            {
                if(rowHover)rowHover.enabled=false;
                artwork.rectTransform.anchoredPosition=new Vector2(16,0);artwork.rectTransform.sizeDelta=new Vector2(60,64);label.rectTransform.anchoredPosition=new Vector2(0,-66);label.rectTransform.sizeDelta=new Vector2(92,34);label.alignment=TMPro.TextAlignmentOptions.Top;label.textWrappingMode=TMPro.TextWrappingModes.Normal;label.overflowMode=TMPro.TextOverflowModes.Overflow;
                if(typeLabel){typeLabel.gameObject.SetActive(false);Destroy(typeLabel.gameObject);typeLabel=null;}
            }
        }
        public void OnPointerDown(PointerEventData e) { if(e.button!=PointerEventData.InputButton.Left)return; SetPressed(true); GameApp.Instance.Audio.Play("sfx_icon_press"); }
        public void OnPointerUp(PointerEventData e) { if(e.button!=PointerEventData.InputButton.Left)return; SetPressed(false); }
        void SetPressed(bool pressed) { if(artwork) artwork.color=pressed?new Color(.55f,.65f,.55f,1):Color.white; }
        void OnDisable() {EndDragVisual();SetPressed(false);dragging=false;pointerOver=false;if(rowHover)rowHover.enabled=false;}
        void OnApplicationFocus(bool focused) {if(!focused) {pointerOver=false;EndDragVisual();SetPressed(false);if(dragging)Rect.anchoredPosition=origin;dragging=false;}}
        public void OnPointerClick(PointerEventData e) { if(e.button!=PointerEventData.InputButton.Left||dragging) return; if(e.clickCount==2 && DoubleClicked!=null) DoubleClicked(); else Clicked?.Invoke(); }
        public void OnBeginDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||!Draggable) return; dragging=true; origin=Rect.anchoredPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent,e.position,e.pressEventCamera,out var local);
            offset=Rect.anchoredPosition-local; transform.SetAsLastSibling();BeginDragVisual();
        }
        public void OnDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||!dragging) return;
            var parent=(RectTransform)transform.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,e.position,e.pressEventCamera,out var local);
            var pos=local+offset;
            if(DroppedOutside!=null || AllowWindowDrops) {Rect.anchoredPosition=pos;if(ScreenBoundDrag){var root=GameApp.Instance.ScreenRoot;var point=root.InverseTransformPoint(Rect.position);point.x=Mathf.Clamp(point.x,0,932);point.y=Mathf.Clamp(point.y,-668,-38);Rect.position=root.TransformPoint(point);}UpdateDragVisual();return;}
            Rect.anchoredPosition=new Vector2(Mathf.Clamp(pos.x,0,Mathf.Max(0,parent.rect.width-Rect.rect.width)),Mathf.Clamp(pos.y,-Mathf.Max(0,parent.rect.height-Rect.rect.height),0));
            UpdateDragVisual();
        }
        public void OnEndDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||!dragging) return; EndDragVisual();SetPressed(false);
            if(DroppedOutside!=null) {Rect.anchoredPosition=origin;dragging=false;DroppedOutside(e);return;}
            var dropBounds=IsDetails?GetComponentInParent<ScrollRect>()?.viewport:null;
            if(!dropBounds)dropBounds=(RectTransform)transform.parent;
            if(RejectOutsideDrop&&!RectTransformUtility.RectangleContainsScreenPoint(dropBounds,e.position,e.pressEventCamera))
            {Rect.anchoredPosition=origin;dragging=false;DropWithoutTarget?.Invoke(e);return;}
            DropTarget windowTarget=null;
            if(AllowWindowDrops)
            {
                var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
                var top=hits.Find(h=>h.gameObject!=gameObject && !h.gameObject.transform.IsChildOf(transform));
                if(top.gameObject)
                {
                    var candidate=top.gameObject.GetComponentInParent<DropTarget>();
                    if(candidate && candidate.IsFolderWindow)windowTarget=candidate;
                    else if(!top.gameObject.transform.IsChildOf(transform.parent))
                    {Rect.anchoredPosition=origin;dragging=false;DropWithoutTarget?.Invoke(e);return;}
                }
                else {Rect.anchoredPosition=origin;dragging=false;DropWithoutTarget?.Invoke(e);return;}
            }
            var targets=transform.parent.GetComponentsInChildren<DropTarget>(); DropTarget hit=windowTarget;
            var corners=new Vector3[4]; Rect.GetWorldCorners(corners);
            var bounds=new Rect(corners[0],corners[2]-corners[0]);
            foreach(var target in targets)
            {
                if(windowTarget)break;
                if(target.transform==transform||target.transform.IsChildOf(transform))continue;
                ((RectTransform)target.transform).GetWorldCorners(corners);
                if(IsDetails?RectTransformUtility.RectangleContainsScreenPoint((RectTransform)target.transform,e.position,e.pressEventCamera):bounds.Overlaps(new Rect(corners[0],corners[2]-corners[0]))) { hit=target; break; }
            }
            Rect.anchoredPosition=origin; if(hit) Dropped?.Invoke(this,hit);else DropWithoutTarget?.Invoke(e); dragging=false;
        }
    }
    public sealed class DropTarget : MonoBehaviour { public string Category; public bool IsFolderWindow; public Action Received; }
}
