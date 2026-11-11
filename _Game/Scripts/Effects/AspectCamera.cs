using UnityEngine;
namespace CoComputar
{
    [RequireComponent(typeof(Camera))]
    public sealed class AspectCamera : MonoBehaviour
    {
        Camera target;
        RectTransform[] bars;
        void Awake()
        {
            target=GetComponent<Camera>();
            var canvas=new GameObject("4대3 검은 여백",typeof(RectTransform),typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=32000;
            bars=new RectTransform[4];
            for(int i=0;i<4;i++)
            {
                var go=new GameObject("여백",typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(canvas.transform,false);
                var image=go.GetComponent<UnityEngine.UI.Image>();image.color=Color.black;image.raycastTarget=false;bars[i]=(RectTransform)go.transform;
            }
        }
        void Update()
        {
            var ratio = (float)Screen.width / Mathf.Max(1, Screen.height); const float wanted = 4f/3f;
            target.rect = ratio > wanted ? new Rect((1-wanted/ratio)/2,0,wanted/ratio,1) : new Rect(0,(1-ratio/wanted)/2,1,ratio/wanted);
            var r=target.rect;
            SetBar(0,new Vector2(0,0),new Vector2(r.xMin,1));SetBar(1,new Vector2(r.xMax,0),Vector2.one);
            SetBar(2,new Vector2(0,0),new Vector2(1,r.yMin));SetBar(3,new Vector2(0,r.yMax),Vector2.one);
        }
        void SetBar(int i,Vector2 min,Vector2 max) {bars[i].anchorMin=min;bars[i].anchorMax=max;bars[i].offsetMin=bars[i].offsetMax=Vector2.zero;}
    }
}
