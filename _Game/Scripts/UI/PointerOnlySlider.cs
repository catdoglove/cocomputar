using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CoComputar
{
    // 방향키를 창이 직접 처리할 때 포커스 탐색/Slider 입력의 중복을 막는다.
    public sealed class PointerOnlySlider:Slider
    {
        public override void OnMove(AxisEventData e){e.Use();}
    }
}
