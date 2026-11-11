#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator TulipChecks()
        {
            app.Save.NewGame("Stage02");app.Begin(false);yield return new WaitForSeconds(.2f);
            var stage=app.ScreenRoot.GetComponentInChildren<Stage02Controller>();yield return Icon("twin_black");yield return Hold(Key.LeftCtrl,3);
            stage.WhiteTaken=true;stage.Receive("white.key","twins");yield return Hold(Key.LeftCtrl,3);
            stage.Rained=true;stage.OpenOutdoor();yield return new WaitForSeconds(.7f);
            int sounds=0;Action<string> heard=key=>{if(key=="sfx_bloom")sounds++;};app.Audio.Played+=heard;
            for(int index=0;index<4;index++)
            {
                var plants=app.Windows.GetComponentInChildren<OutdoorWindow>().transform.Find("식물");
                var slot=plants.Find("식물 슬롯 "+index);var button=slot.GetComponentInChildren<Button>();var callback=button.onClick;
                var others=plants.GetComponentsInChildren<PlantGrowth>().Where(p=>!p.transform.IsChildOf(slot)).ToArray();
                yield return MouseClick((RectTransform)button.transform);
                Check(stage.Buds.Count(v=>v)==index+1&&sounds==index+1,"꽃 "+index+": 한 번 클릭 상태·효과음 1회");
                Check(others.All(p=>p&&p.transform.localScale==Vector3.one),"꽃 "+index+": 다른 꽃 재생성·성장 연출 없음");
                button=plants.Find("식물 슬롯 "+index).GetComponentInChildren<Button>();
                for(int n=0;n<10;n++)callback.Invoke();yield return MouseClick((RectTransform)button.transform);
                Check(sounds==index+1&&!button.interactable,"꽃 "+index+": 실제 재클릭·이전 콜백 중복 차단");
                yield return new WaitForSeconds(.6f);
            }
            app.CloseAllWindows();stage.BlackTaken=true;stage.Receive("black.key","twins");yield return Hold(Key.LeftCtrl,3);
            stage.ApplyDate(new DateTime(1989,6,6));yield return new WaitForSeconds(.1f);stage.OpenOutdoor();yield return new WaitForSeconds(.7f);
            app.Audio.Played-=heard;heard=key=>{if(key=="sfx_key_get")sounds++;};app.Audio.Played+=heard;sounds=0;
            for(int index=0;index<4;index++)
            {
                var plants=app.Windows.GetComponentInChildren<OutdoorWindow>().transform.Find("식물");
                var slot=plants.Find("식물 슬롯 "+index);var button=slot.GetComponentInChildren<Button>();var callback=button.onClick;
                var others=plants.GetComponentsInChildren<PlantGrowth>().Where(p=>!p.transform.IsChildOf(slot)).ToArray();
                yield return MouseClick((RectTransform)button.transform);
                Check(stage.TulipsPicked.Count(v=>v)==index+1&&sounds==index+1,"튤립 "+index+": 해당 개체만 한 번 수집·효과음 1회");
                Check(others.All(p=>p&&p.transform.localScale==Vector3.one),"튤립 "+index+": 나머지 개체·연출 그대로 유지");
                for(int n=0;n<10;n++){callback.Invoke();stage.PickTulip(index);}yield return null;
                Check(sounds==index+1&&stage.TulipsPicked.Count(v=>v)==index+1,"튤립 "+index+": 이전 콜백·직접 반복 입력 무효");
                Check(stage.BouquetTaken==(index==3)&&stage.FileStore.Files.Count(f=>f.Id=="bouquet")== (index==3?1:0),"튤립 "+index+": 네 개 수집 후 꽃다발 한 번 지급");
                if(index==0){app.CloseAllWindows();yield return null;stage.OpenOutdoor();yield return new WaitForSeconds(.7f);Check(app.Windows.GetComponentInChildren<OutdoorWindow>().GetComponentsInChildren<Button>().Count()==3,"재열기 후 수집한 튤립만 소멸 유지");}
            }
            yield return null;Check(!stage.Sepia&&app.DisplayDate==null,"네 개 수집 후 기존 퍼즐 종료 전환");
            app.Audio.Played-=heard;
        }
    }
}
#endif
