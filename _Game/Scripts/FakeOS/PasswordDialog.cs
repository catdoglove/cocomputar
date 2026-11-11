using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed class PasswordDialog:MonoBehaviour
    {
        System.Func<string,bool> validate;System.Action accepted;string wrong;FakeWindow window;TMP_Text field;string digits="";bool shaking;Button input;float blinkStart;
        public bool InputFocused=>EventSystem.current&&EventSystem.current.currentSelectedGameObject==input.gameObject;
        public bool CaretVisible=>InputFocused&&InputCaretStyle.Visible(blinkStart);
        public string Digits=>digits;
        public static void Open(Stage03Controller stage)
        {
            Open("s3:password",stage.SubmitPassword,stage.OpenSecret,stage.Data.wrong);
        }
        public static void Open(string key,System.Func<string,bool> validate,System.Action accepted,string wrong="sfx_pw_wrong")
        {
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,key,"접근 허가 필요",out var w,300,220,420,285))return;
            var self=w.gameObject.AddComponent<PasswordDialog>();self.window=w;self.validate=validate;self.accepted=accepted;self.wrong=wrong;
            UIFactory.Text(w.Content,"암호를 입력하시오",70,25,300,35,25);
            var box=UIFactory.Panel(w.Content,"숫자 입력",50,85,300,40,new Color(.78f,1,.4f));self.input=box.gameObject.AddComponent<Button>();self.input.transition=Selectable.Transition.None;self.input.onClick.AddListener(self.FocusInput);self.FocusInput();self.field=UIFactory.Text(box.transform,"",8,3,280,35,26,InputCaretStyle.Color);
            UIFactory.Button(w.Content,"확인",50,165,120,40,self.Submit);UIFactory.Button(w.Content,"취소",230,165,120,40,w.Close);
        }
        void FocusInput(){EventSystem.current.SetSelectedGameObject(input.gameObject);blinkStart=Time.unscaledTime;}
        void LateUpdate(){if(field)field.text=digits+(CaretVisible?"|":"");}
        void Update()
        {
            var key=Keyboard.current;if(key==null||shaking||GameApp.Instance.Dialogue.Active)return;
            if(window.transform.GetSiblingIndex()!=window.transform.parent.childCount-1)return;
            if(!InputFocused)return;string before=digits;
            Key[] upper={Key.Digit0,Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6,Key.Digit7,Key.Digit8,Key.Digit9};
            Key[] pad={Key.Numpad0,Key.Numpad1,Key.Numpad2,Key.Numpad3,Key.Numpad4,Key.Numpad5,Key.Numpad6,Key.Numpad7,Key.Numpad8,Key.Numpad9};
            for(int i=0;i<10;i++)if(key[upper[i]].wasPressedThisFrame||key[pad[i]].wasPressedThisFrame)if(digits.Length<4)digits+=i.ToString();
            if(key.backspaceKey.wasPressedThisFrame&&digits.Length>0)digits=digits.Substring(0,digits.Length-1);
            if(before!=digits)blinkStart=Time.unscaledTime;
            if(key.escapeKey.wasPressedThisFrame)window.Close();
            else if(key.enterKey.wasPressedThisFrame||key.numpadEnterKey.wasPressedThisFrame){EventSystem.current.SetSelectedGameObject(null);Submit();}
        }
        public void Submit()
        {
            if(shaking)return;if(validate(digits)){window.Close();accepted();return;}
            digits="";FocusInput();GameApp.Instance.Audio.Play(wrong);StartCoroutine(Shake());
        }
        IEnumerator Shake(){shaking=true;var origin=window.Rect.anchoredPosition;for(float t=0;t<.3f;t+=Time.unscaledDeltaTime){window.Rect.anchoredPosition=origin+Vector2.right*Mathf.Sin(t*90)*6;yield return null;}window.Rect.anchoredPosition=origin;shaking=false;}
    }
}
