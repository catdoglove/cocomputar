using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace CoComputar
{
    // 공용 창 위에서 동작하는 입력/목록 UI. 한국어 IME는 TMP_InputField에 맡긴다.
    public static class FileUI
    {
        public static TMP_InputField Input(Transform parent,string value,float x,float y,float width,float height,bool multiline=false)
        {
            var box=UIFactory.Panel(parent,"입력",x,y,width,height,Color.white);var viewport=UIFactory.Rect(box.transform,"입력 영역",5,3,width-10,height-6);viewport.gameObject.AddComponent<RectMask2D>();
            var text=UIFactory.Text(viewport,"",0,0,width-10,height-6,20,Color.black);text.richText=false;
            var input=box.gameObject.AddComponent<TMP_InputField>();input.targetGraphic=box;input.textViewport=viewport;input.textComponent=text;input.lineType=multiline?TMP_InputField.LineType.MultiLineNewline:TMP_InputField.LineType.SingleLine;input.text=value;input.caretWidth=2;input.customCaretColor=true;input.caretColor=Color.clear;box.gameObject.AddComponent<InputCaret>().Setup(input);return input;
        }
        public static TMP_Text ScrollText(Transform parent,string value,float x,float y,float width,float height,Color color)
        {
            var box=UIFactory.Panel(parent,"읽기 전용 본문",x,y,width,height,Color.white);var view=UIFactory.Rect(box.transform,"스크롤 영역",0,0,width-22,height);view.gameObject.AddComponent<RectMask2D>();
            var text=UIFactory.Text(view,value,8,4,width-38,height,21,color);text.richText=false;text.rectTransform.sizeDelta=new Vector2(width-38,Mathf.Max(height,text.GetPreferredValues(value,width-38,100000).y+20));
            var scroll=box.gameObject.AddComponent<ScrollRect>();scroll.viewport=view;scroll.content=text.rectTransform;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=26;
            var track=UIFactory.Panel(box.transform,"세로 스크롤바",width-20,0,20,height,new Color(.76f,.76f,.78f));var handle=UIFactory.Panel(track.transform,"손잡이",0,0,20,50,new Color(.4f,.4f,.44f));
            handle.rectTransform.sizeDelta=Vector2.zero;handle.rectTransform.anchoredPosition=Vector2.zero;handle.rectTransform.pivot=new Vector2(.5f,.5f);var bar=track.gameObject.AddComponent<Scrollbar>();bar.targetGraphic=handle;bar.handleRect=handle.rectTransform;bar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=bar;scroll.verticalNormalizedPosition=1;return text;
        }
        public static FakeWindow Notice(string message,string key="s4:notice",bool sound=true,string title="Co-computar 메모장")
        {
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,key,title,out var w,280,250,470,220))return w;
            UIFactory.Text(w.Content,"!",12,24,48,55,43,new Color(.8f,.55f,0));UIFactory.Text(w.Content,message,62,18,370,90,21,Color.black);UIFactory.Button(w.Content,"확인",165,114,120,35,w.Close);if(sound)GameApp.Instance.Audio.Play("sfx_error");return w;
        }
    }
    public sealed class FileOwner:MonoBehaviour {public string Id;}
    public sealed class FileRightClick:MonoBehaviour,IPointerClickHandler
    {
        public Action<PointerEventData> Click;public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Right)Click?.Invoke(e);}
    }
    public sealed class FilePopup:MonoBehaviour
    {
        void Update(){if(Keyboard.current?.escapeKey.wasPressedThisFrame==true)Close();}
        public void Close(){gameObject.SetActive(false);Destroy(gameObject);}
    }
    public sealed class FileRename:MonoBehaviour
    {
        TMP_InputField input;Action<string> commit;
        public static void Begin(FakeIcon icon,string name,Action<string> action)
        {
            if(icon.GetComponentInChildren<FileRename>())return;var field=FileUI.Input(icon.transform,name,icon.IsDetails?32:-30,icon.IsDetails?0:65,icon.IsDetails?Mathf.Max(90,icon.Rect.rect.width*.64f-34):180,35);var self=field.gameObject.AddComponent<FileRename>();self.input=field;self.commit=action;
            field.onValidateInput=(text,pos,c)=>"\\/:*?\"<>|".IndexOf(c)>=0?'\0':c;field.onSubmit.AddListener(text=>{if(!field.wasCanceled)self.Commit(text);});field.onEndEdit.AddListener(_=>{if(self)Destroy(self.gameObject);});self.StartCoroutine(self.Focus());
        }
        IEnumerator Focus()
        {
            yield return null;input.Select();input.ActivateInputField();
            // TMP applies activation/select-all in LateUpdate; collapse after that step.
            yield return null;if(!input||!input.isFocused)yield break;
            input.selectionStringAnchorPosition=input.text.Length;input.selectionStringFocusPosition=input.text.Length;input.ForceLabelUpdate();
        }
        void Update(){var k=Keyboard.current;if(k==null||!input.isFocused)return;if(k.escapeKey.wasPressedThisFrame){Destroy(gameObject);return;}}
        void Commit(string text){gameObject.SetActive(false);Destroy(gameObject);commit(text);}

    }
}
