#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator FlowerKeyChecks()
        {
            var seen=new HashSet<int>();var randomState=Random.state;
            for(int seed=0;seed<64&&seen.Count<4;seed++)
            {
                Random.InitState(seed);app.Save.NewGame("Stage02");app.Begin(false);yield return null;
                var stage=app.ScreenRoot.GetComponentInChildren<Stage02Controller>();int index=stage.BlackFlowerIndex;
                Check(index>=0&&index<4,"시작 시 유효한 꽃 위치 선택");if(!seen.Add(index))continue;
                stage.Rained=true;stage.OpenOutdoor();yield return new WaitForSeconds(.3f);
                var outdoor=app.Windows.GetComponentInChildren<OutdoorWindow>();var plants=outdoor.transform.Find("식물");
                int blooms=0;System.Action<string> heard=k=>{if(k=="sfx_bloom")blooms++;};app.Audio.Played+=heard;
                for(int i=0;i<4;i++)if(i!=index){var b=plants.Find("식물 슬롯 "+i).GetComponentInChildren<Button>();yield return MouseClick((RectTransform)b.transform);}
                stage.TakeBlack();Check(!stage.BlackTaken&&blooms==3&&!plants.GetComponentsInChildren<Transform>().Any(t=>t.name=="검은 열쇠"),"꽃 "+index+": 나머지 세 꽃 기존 반응·열쇠 없음");
                var selected=plants.Find("식물 슬롯 "+index).GetComponentInChildren<Button>();yield return MouseClick((RectTransform)selected.transform);
                var keys=plants.GetComponentsInChildren<Transform>().Where(t=>t.name=="검은 열쇠").ToArray();
                Check(keys.Length==1&&keys[0].parent.name=="식물 슬롯 "+index&&((RectTransform)keys[0]).anchoredPosition.x==50+index*130&&blooms==4,"꽃 "+index+": 선택된 꽃 위치에 열쇠 1개 표시");app.Audio.Played-=heard;
                app.CloseAllWindows();yield return null;stage.OpenOutdoor();yield return new WaitForSeconds(.3f);outdoor=app.Windows.GetComponentInChildren<OutdoorWindow>();
                var key=(RectTransform)outdoor.transform.Find("식물/식물 슬롯 "+index+"/검은 열쇠");Check(stage.BlackFlowerIndex==index&&key,"창 재열기에도 열쇠 위치 유지");
                yield return MouseClick(key);stage.TakeBlack();Check(stage.BlackTaken&&stage.FileStore.Files.Count(f=>f.Id=="black.key")==1&&!outdoor.GetComponentsInChildren<Transform>().Any(t=>t.name=="검은 열쇠"),"꽃 "+index+": 실제 열쇠 클릭 획득·중복 방지");
            }
            Random.state=randomState;Check(seen.Count==4,"네 위치 모두 선택 및 획득 가능");
        }
    }
}
#endif
