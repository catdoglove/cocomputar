using System.Collections;
using UnityEngine;
namespace CoComputar
{
    // 스테이지 5의 결합 모션을 공통화. 파일 소모와 보상은 호출자가 관리한다.
    public static class IconMerge
    {
        public static IEnumerator Play(Transform parent,Sprite left,Sprite right,AudioService audio)
        {
            var a=UIFactory.Sprite(parent,left,390,478,92,88);var b=UIFactory.Sprite(parent,right,540,478,92,88);audio.Play("sfx_icon_merge");
            for(float t=0;t<.6f;t+=Time.unscaledDeltaTime){a.rectTransform.anchoredPosition=new Vector2(Mathf.Lerp(390,465,t/.6f),-478);b.rectTransform.anchoredPosition=new Vector2(Mathf.Lerp(540,465,t/.6f),-478);yield return null;}
            Object.Destroy(a.gameObject);Object.Destroy(b.gameObject);
        }
    }
}
