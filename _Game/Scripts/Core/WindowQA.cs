#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator WindowChecks()
        {
            yield return FontPreview();
            app.Dialogue.CloseCurrent();app.CloseAllWindows();yield return null;
            foreach(var size in new[]{new Vector2(425,215),new Vector2(650,465),new Vector2(790,600)})
            {
                FakeWindow.TryCreate(app.Windows,"frame-test","폴더 이름 · 긴 창 제목.txt",out var w,120,100,size.x,size.y);
                w.AddHiddenToggle(()=>{});yield return new WaitForSeconds(.2f);
                Check(w.Rect.rect.size==size,"기존 외곽 크기 유지 "+size);
                var content=FilmLensWindow.Bounds(w.Content);var outer=FilmLensWindow.Bounds(w.Rect);var toolbar=FilmLensWindow.Bounds(w.Toolbar);
                Check(outer.Contains(content.min)&&outer.Contains(content.max)&&content.yMin>toolbar.yMax,"툴바·콘텐츠 겹침 및 잘림 없음 "+size);
                Check(w.Toolbar.GetComponentsInChildren<Button>().Length==1,"숨김 옵션 툴바 배치");
                var before=w.ShowHidden;yield return MouseClick(w.Toolbar.GetComponentInChildren<Button>().GetComponent<RectTransform>());Check(before!=w.ShowHidden,"툴바 실제 입력");
                Check(!FakeWindow.TryCreate(app.Windows,"frame-test","중복",out var same)&&same==w,"기존 창 포커스 유지");
                w.Resize(size+new Vector2(20,20));Check(w.Rect.rect.size==size+new Vector2(20,20),"리사이즈 프레임 갱신");
                Capture("window-frame-"+(int)size.x);yield return MouseClick((RectTransform)w.transform.Find("제목 표시줄/X"));yield return null;Check(!w,"닫기 버튼 실제 입력");
            }
            app.Save.NewGame("Stage05");app.Begin(false);yield return new WaitForSeconds(.5f);var stage=app.ScreenRoot.GetComponentInChildren<Stage05Controller>();
            stage.OpenFolder("trash");yield return new WaitForSeconds(.3f);var trash=app.Windows.GetComponentInChildren<FakeWindow>();Check(trash.Toolbar.GetComponentsInChildren<Button>().Length==2,"휴지통 숨김·복원 공통 툴바");Capture("window-trash");
            app.CloseAllWindows();yield return null;Check(app.Windows.GetComponentsInChildren<FakeWindow>().Length==0,"공통 전체 닫기");
        }
    }
}
#endif
