using TMPro;
using UnityEngine;
namespace CoComputar
{
    // 흑/백의 동일한 문장을 2px 어긋나게 겹치고 타이핑·Ctrl 스킵을 함께 따른다.
    public sealed class DialogueTextEcho:MonoBehaviour
    {
        public TMP_Text Source;TMP_Text target;
        void Awake(){target=GetComponent<TMP_Text>();}
        void LateUpdate(){if(!Source)return;target.text=Source.text;target.maxVisibleCharacters=Source.maxVisibleCharacters;}
    }
}
