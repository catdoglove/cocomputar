using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CoComputar
{
    public sealed class FakeCursor : MonoBehaviour
    {
        public bool Controlled;
        public bool ControlPaused;
        public Vector2 ControlledPosition;
        public Vector2 Position=>pointer?pointer.anchoredPosition:Vector2.zero;
        public void SetVisible(bool value){if(pointer)pointer.gameObject.SetActive(value);}
        Image arrow;RectTransform textPointer,linkPointer;string hint;
        readonly List<RaycastResult> hits=new();PointerEventData hoverEvent;EventSystem hoverSystem;
        public bool TextCursorActive=>textPointer&&textPointer.gameObject.activeSelf;
        RectTransform pointer, body, head; Vector2 previous; float angle, velocity; bool hanging;
        public void Setup(RectTransform root)
        {
            pointer=UIFactory.Rect(root,"분홍 커서",500,300,28,38);
            arrow=UIFactory.Sprite(pointer,GameApp.Instance.Catalog.sprites.Get("cursor"),0,0,28,38);
            textPointer=UIFactory.Rect(pointer,"텍스트 마우스 커서",-6,-12,13,24);
            void Bar(float x,float y,float w,float h,Color color){UIFactory.Panel(textPointer,"I-beam",x,y,w,h,color).raycastTarget=false;}
            var pink=new Color(1,.25f,.65f);
            Bar(0,0,13,3,pink);Bar(5,0,3,24,pink);Bar(0,21,13,3,pink);
            Bar(1,1,11,1,Color.white);Bar(6,1,1,22,Color.white);Bar(1,22,11,1,Color.white);
            textPointer.gameObject.SetActive(false);
            linkPointer=UIFactory.Rect(pointer,"링크 손 커서",-5,-3,28,34);
            string[] pixels={"...XX........","..XWWX.......","..XWWX.......","..XWWX.......","..XWWXXX.....","..XWWXWWXX...","..XWWXWWWWXX.","XXXWWWWWWWWWX","XWWWWWWWWWWWX",".XWWWWWWWWWWX","..XWWWWWWWWX.","...XWWWWWWWX.","...XXXXXXXX.."};
            for(int row=0;row<pixels.Length;row++)for(int col=0;col<pixels[row].Length;col++)if(pixels[row][col]!='.')UIFactory.Panel(linkPointer,"손",col*2,row*2,2,2,pixels[row][col]=='X'?Color.black:Color.white).raycastTarget=false;
            linkPointer.gameObject.SetActive(false);
            body=UIFactory.Rect(pointer,"매달린 몸",-16,52,52,55); body.pivot=new Vector2(.5f,1); body.anchoredPosition=new Vector2(9,-52);
            UIFactory.Sprite(body,GameApp.Instance.Catalog.sprites.Get("player_body"),0,0,52,55);
            head=UIFactory.Sprite(pointer,GameApp.Instance.Catalog.sprites.Get("player_head"),-17,26,52,38).rectTransform;
            UnityEngine.Cursor.visible=false; SetHanging(false);
        }
        TMPro.TMP_Text resizeHint;
        public void SetResizeHint(string hint)
        {
            if(!pointer)return;if(!resizeHint)resizeHint=UIFactory.Text(pointer,"",0,0,42,42,32,Color.white);
            this.hint=hint;resizeHint.text=hint??"";RefreshPointer();
        }
        void RefreshPointer()
        {
            bool editable=false,link=false;var events=EventSystem.current;
            if(events&&events.currentInputModule&&events.currentInputModule.isActiveAndEnabled&&Mouse.current!=null)
            {
                if(hoverEvent==null||hoverSystem!=events){hoverSystem=events;hoverEvent=new PointerEventData(events);}
                hoverEvent.position=Mouse.current.position.ReadValue();hits.Clear();events.RaycastAll(hoverEvent,hits);
                if(hits.Count>0)
                {
                    var input=hits[0].gameObject.GetComponentInParent<TMP_InputField>();
                    editable=input&&input.isActiveAndEnabled&&input.IsInteractable()&&!input.readOnly;
                    link=hits[0].gameObject.GetComponentInParent<BrowserLink>()!=null;
                }
            }
            bool text=editable&&string.IsNullOrEmpty(hint);
            textPointer.gameObject.SetActive(text);bool hand=link&&!text&&string.IsNullOrEmpty(hint);linkPointer.gameObject.SetActive(hand);arrow.enabled=!text&&!hand&&string.IsNullOrEmpty(hint);
        }
        public void SetHanging(bool value) { hanging=value; if(body) body.gameObject.SetActive(value);if(head) head.gameObject.SetActive(value); }
        void LateUpdate()
        {
            if(Mouse.current==null || !pointer) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(GameApp.Instance.Root,Mouse.current.position.ReadValue(),GameApp.Instance.UICamera,out var pos);
            if(Controlled)pos=ControlPaused?pointer.anchoredPosition:ControlledPosition;
            pos.x=Mathf.Clamp(pos.x,0,1023); pos.y=Mathf.Clamp(pos.y,-767,0);
            pointer.anchoredPosition=pos;RefreshPointer();
            float dt=Mathf.Min(Time.unscaledDeltaTime,.04f);
            float target=Mathf.Clamp(-(pos.x-previous.x)*1.2f,-32,32); previous=pos;
            velocity+=(target-angle)*65*dt; velocity*=Mathf.Exp(-10*dt); angle+=velocity*dt;
            body.localRotation=Quaternion.Euler(0,0,angle);
        }
    }
}

