using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace CoComputar
{
    // 코드로 그리는 눈: 원본 이미지 편집 없이 동공 좌표만 갱신한다.
    public sealed class Stage08Eye:Graphic
    {
        public bool Trembles;Vector2 pupil;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;Vector2 c=r.center;
            void Disk(Vector2 center,float rx,float ry,Color shade)
            {int start=vh.currentVertCount;vh.AddVert(center,shade,Vector2.zero);for(int i=0;i<=32;i++){float a=i*Mathf.PI/16;vh.AddVert(center+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*ry),shade,Vector2.zero);if(i>0)vh.AddTriangle(start,start+i,start+i+1);}}
            Disk(c,38,22,new Color(.65f,.01f,.08f));Disk(c,34,18,Color.black);Disk(c+pupil,16,16,new Color(.9f,.03f,.1f));Disk(c+pupil,7,7,Color.black);
        }
        void Update()
        {
            if(Mouse.current==null)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,Mouse.current.position.ReadValue(),GameApp.Instance.UICamera,out var p);
            pupil=Vector2.ClampMagnitude((p-rectTransform.rect.center)*.1f,7);if(Trembles)pupil+=new Vector2(Mathf.Sin(Time.unscaledTime*31),Mathf.Cos(Time.unscaledTime*29))*1.3f;SetVerticesDirty();
        }
    }
    public sealed class Stage08Globe:MonoBehaviour
    {void Update(){transform.localScale=new Vector3(.8f+.2f*Mathf.Sin(Time.unscaledTime*2),1,1);}}
}
