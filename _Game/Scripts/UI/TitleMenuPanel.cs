using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace CoComputar
{
    // 타이틀 전용 팝업. 바탕화면 옵션 창에는 적용하지 않는다.
    public sealed class TitleMenuPanel : MonoBehaviour
    {
        public RectTransform Content { get; private set; }
        Button[] spaceButtons;int openedFrame;
        public void BindSpaceSelection(params Button[] buttons) {spaceButtons=buttons;openedFrame=Time.frameCount;}
        public static TitleMenuPanel Open(Transform parent,float x,float y,float width,float height)
        {
            foreach(var old in parent.GetComponentsInChildren<TitleMenuPanel>()) old.Close();
            var cover=UIFactory.Panel(parent,"타이틀 메뉴 바깥",0,0,1024,768,Color.clear);
            var panel=cover.gameObject.AddComponent<TitleMenuPanel>();
            var outside=cover.gameObject.AddComponent<Button>();outside.transition=Selectable.Transition.None;
            outside.navigation=new Navigation{mode=Navigation.Mode.None};outside.onClick.AddListener(panel.Close);
            var border=UIFactory.Panel(cover.transform,"초록 메뉴 패널",x,y,width,height,UIFactory.Theme.green);
            panel.Content=UIFactory.Panel(border.transform,"메뉴 내용",2,2,width-4,height-4,new Color(.015f,.085f,.025f)).rectTransform;
            // Image가 패널 안의 빈 공간도 가로채서 바깥 클릭으로 처리되지 않게 한다.
            var inside=panel.Content.gameObject.AddComponent<Button>();inside.transition=Selectable.Transition.None;inside.navigation=new Navigation{mode=Navigation.Mode.None};
            panel.StartCoroutine(panel.Unfold(border.rectTransform));return panel;
        }
        IEnumerator Unfold(RectTransform rect)
        {
            var group=rect.gameObject.AddComponent<CanvasGroup>();group.interactable=false;
            for(float t=0;t<.18f;t+=Time.unscaledDeltaTime)
            {
                float p=Mathf.SmoothStep(0,1,t/.18f);rect.localScale=new Vector3(Mathf.Lerp(.03f,1,p),1,1);group.alpha=p;yield return null;
            }
            rect.localScale=Vector3.one;group.alpha=1;group.interactable=true;
        }
        public void Style(Selectable item,TextMeshProUGUI label=null,bool showArrow=false)
        {
            if(!label) label=item.GetComponentInChildren<TextMeshProUGUI>();
            if(item is Button)
            {
                item.GetComponent<Image>().color=Color.clear;item.transition=Selectable.Transition.None;
                item.GetComponent<Outline>().enabled=false;label.alignment=TextAlignmentOptions.Left;label.fontSize=26;
            }
            else
            {
                item.transform.Find("트랙").GetComponent<Image>().color=UIFactory.Theme.green;
                item.transition=Selectable.Transition.None;item.targetGraphic.CrossFadeColor(Color.white,0,true,true);label.color=UIFactory.Theme.green;
                return;
            }
            item.gameObject.AddComponent<TitleMenuTint>().Setup(item,label,showArrow);
        }
        void Update()
        {
            var keyboard=Keyboard.current;if(keyboard==null)return;
            if(keyboard.escapeKey.wasPressedThisFrame) {Close();return;}
            // Start에서 명시적으로 등록한 두 항목만 처리한다. 열린 프레임의 키는 재사용하지 않는다.
            if(spaceButtons==null || Time.frameCount<=openedFrame || !keyboard.spaceKey.wasPressedThisFrame)return;
            var selected=EventSystem.current.currentSelectedGameObject;
            foreach(var button in spaceButtons)
                if(button && button.gameObject==selected && button.IsActive() && button.IsInteractable())
                {button.onClick.Invoke();break;}
        }
        public void Close() { EventSystem.current.SetSelectedGameObject(null);gameObject.SetActive(false);Destroy(gameObject); }
    }
    public sealed class TitleMenuTint : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        Selectable item;TextMeshProUGUI label,arrow;bool hover;
        public void Setup(Selectable control,TextMeshProUGUI text,bool showArrow)
        {
            item=control;label=text;
            if(showArrow)
            {
                label.rectTransform.anchoredPosition+=new Vector2(30,0);
                label.rectTransform.sizeDelta-=new Vector2(30,0);
                arrow=UIFactory.Text(control.transform,"▶",2,1,28,label.rectTransform.rect.height,label.fontSize,UIFactory.Theme.pink);
                arrow.name="포커스 화살표";arrow.alignment=TextAlignmentOptions.Left;
            }
            Paint();
        }
        public void OnPointerEnter(PointerEventData e) {hover=true;if(arrow && item.IsInteractable())EventSystem.current.SetSelectedGameObject(item.gameObject);Paint();}
        public void OnPointerExit(PointerEventData e) {hover=false;Paint();}
        void Update() {Paint();}
        void Paint()
        {
            if(!label || !item) return;
            bool focus=EventSystem.current.currentSelectedGameObject==item.gameObject;
            if(item is Slider && Mouse.current!=null)
            {
                var point=Mouse.current.position.ReadValue();
                hover=RectTransformUtility.RectangleContainsScreenPoint(label.rectTransform,point,GameApp.Instance.UICamera) || RectTransformUtility.RectangleContainsScreenPoint((RectTransform)item.transform,point,GameApp.Instance.UICamera);
            }
            bool active=arrow?focus:(hover || focus);
            if(arrow) arrow.enabled=item.interactable && active;
            label.color=!item.interactable?UIFactory.Theme.green*.35f:(active?UIFactory.Theme.pink:UIFactory.Theme.green);
        }
    }
}
