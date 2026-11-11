#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator PastImageChecks()
        {
            for(int preset=0;preset<2;preset++)
            {
                app.Save.NewGame("Stage03");app.Begin(false);yield return new WaitForSeconds(.3f);
                var stage=app.ScreenRoot.GetComponentInChildren<Stage03Controller>();
                typeof(Stage03Controller).GetProperty("Updated").SetValue(stage,true);
                var target=new DateTime(1990,2,12);
                if(preset==1)stage.ApplyDate(target);
                Check(!stage.Past&&!stage.PastPngOpened,"열람 전 날짜만으로 과거 진입 안 함");
                Check(stage.SubmitPassword(stage.Puzzle.Password)&&!stage.Past,"암호 해결은 과거 진입을 발동하지 않음");
                stage.OpenPastImage();Check(!stage.PastPngOpened&&!app.Windows.GetComponentInChildren<ImageViewer>(),"숨김 상태 열기 차단·열람 기록 없음");
                stage.OpenSecret();yield return new WaitForSeconds(.3f);
                var folder=app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="s3:secret");
                yield return MouseClick((RectTransform)folder.Toolbar.Find("설정 · 숨김파일 표시"));yield return null;
                yield return MouseClick(folder.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s3:past").Rect);yield return new WaitForSeconds(.3f);
                Check(stage.PastPngOpened&&new SaveService().Progress.flags.Contains("stage03.pastPngOpened"),"실제 아이콘 클릭 후 열람 플래그 디스크 저장·로드");
                Check(stage.Past==(preset==1),"이미지 열 때 날짜 일치 여부로 진입 판정");
                if(preset==0)stage.ApplyDate(target);
                yield return new WaitForSeconds(.3f);
                Check(stage.Past&&app.Catalog.crt.sepiaAmount==1&&app.Audio.DateEffectPlaying,"두 입력 순서 모두 과거 세피아·효과음 진입");
                var viewer=app.Windows.GetComponentInChildren<ImageViewer>();
                Check(viewer&&app.Windows.GetComponentsInChildren<FakeWindow>().Length==1,"과거 진입 시 과거.png만 유지·다른 창 정상 닫기");
                yield return MouseClick((RectTransform)viewer.Window.Content.Find("+"));Check(viewer.Level==1,"과거 상태 뷰어 실제 확대 클릭");
                var before=viewer.PictureRoot.anchoredPosition;var start=Center((RectTransform)viewer.transform);
                yield return MouseDrag(start,start+new Vector2(-70,50));Check(viewer.PictureRoot.anchoredPosition!=before,"과거 상태 뷰어 실제 드래그 이동");
                stage.ApplyDate(DateTime.Now);Check(app.DisplayDate.Value.Date==target.Date,"과거 날짜 변경 잠금 유지");
                viewer.Window.Close();yield return null;Check(stage.PastPngOpened,"뷰어 닫기 후 열람 기록 유지");
                app.Save.Enter("Stage03");app.Begin(false);yield return null;
                stage=app.ScreenRoot.GetComponentInChildren<Stage03Controller>();
                Check(!stage.PastPngOpened&&!new SaveService().Progress.flags.Contains("stage03.pastPngOpened")&&!stage.Past&&!app.Save.Hidden("s3:secret"),"이어하기 열람·과거·숨김 옵션 초기화");
            }
        }
    }
}
#endif

