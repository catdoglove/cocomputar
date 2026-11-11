using TMPro;
using UnityEngine;
namespace CoComputar
{
    public static class InputCaretStyle
    {
        public const float Period=1f;
        public static Color Color=>UnityEngine.Color.black;
        public static bool Visible(float start)=>(Time.unscaledTime-start)%Period<Period*.5f;
    }
    // 텍스트 데이터에 커서 문자를 넣지 않는다. TMP의 입력/조합/스크롤 좌표를 따라 표시한다.
    public sealed class InputCaret:MonoBehaviour
    {
        TMP_InputField input;TMP_Text caret;float blinkStart;int position=-1;string previous;bool focused;
        public bool Visible=>caret&&caret.gameObject.activeSelf;
        public RectTransform CaretRect=>caret.rectTransform;
        public void Setup(TMP_InputField value)
        {
            input=value;caret=UIFactory.Text(input.textViewport,"|",0,0,16,40,input.textComponent.fontSize,InputCaretStyle.Color);caret.name="공용 입력 커서";caret.gameObject.SetActive(false);
        }
        void LateUpdate()
        {
            bool active=input&&input.isFocused&&input.interactable&&!input.readOnly;
            if(!active){if(caret)caret.gameObject.SetActive(false);focused=false;return;}
            int index=input.stringPosition;
            bool changed=!focused||position!=index||previous!=input.text;
            if(changed){blinkStart=Time.unscaledTime;position=index;previous=input.text;}focused=true;
            var text=input.textComponent;text.ForceMeshUpdate();var info=text.textInfo;int raw=index;index=info.characterCount;for(int i=0;i<info.characterCount;i++){if(info.characterInfo[i].index>=raw){index=i;break;}}float x=0,top=0,height=text.fontSize;
            if(info.characterCount>0){var c=info.characterInfo[Mathf.Clamp(index,0,info.characterCount-1)];x=index<info.characterCount?c.origin:c.xAdvance;top=c.ascender;height=Mathf.Max(text.fontSize,c.ascender-c.descender);}
            if(changed)
            {
                // 입력/커서 이동 때 현재 줄 전체가 보이도록 한다. 수동 스크롤은 방해하지 않는다.
                var point=input.textViewport.InverseTransformPoint(text.rectTransform.TransformPoint(new Vector3(x,top,0)));var rect=input.textViewport.rect;
                float dy=point.y>rect.yMax?rect.yMax-point.y:point.y-height<rect.yMin?rect.yMin-(point.y-height):0;
                float dx=point.x<rect.xMin?rect.xMin-point.x:point.x+text.fontSize*.4f>rect.xMax?rect.xMax-point.x-text.fontSize*.4f:0;
                text.rectTransform.anchoredPosition+=new Vector2(dx,dy);
            }
            caret.fontSize=text.fontSize;caret.rectTransform.position=text.rectTransform.TransformPoint(new Vector3(x,top+text.fontSize*.12f,0));
            caret.gameObject.SetActive(InputCaretStyle.Visible(blinkStart));
        }
    }
}
