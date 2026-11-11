#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator HalfClearChecks()
        {
            app.Save.NewGame("Stage05");app.Begin(false);yield return new WaitForSeconds(.3f);
            var stage=app.ScreenRoot.GetComponentInChildren<Stage05Controller>();
            Check(app.Catalog.audio.Get(stage.Data.puzzleClearSfx),"공통 클리어 효과음 에셋 등록");
            int sounds=0;Action<string> heard=k=>{if(k==stage.Data.puzzleClearSfx)sounds++;};app.Audio.Played+=heard;
            stage.RadioSuccess();stage.Open("photo");yield return new WaitForSeconds(.2f);
            var editor=app.Windows.GetComponentInChildren<PhotoCorrectionRenderer>();editor.Load("photo"+stage.Randoms.Frequency);var target=stage.Randoms.PhotoTarget;
            editor.SetValues(target.x,target.y,0);yield return new WaitForSeconds(.6f);Check(sounds==0&&!stage.PhotoSolved,"부분 선명도 클리어 효과음 없음");
            editor.SetValues(target.x,target.y,target.z);yield return new WaitForSeconds(.6f);
            Check(stage.PhotoSolved&&editor.RewardAlpha==1&&sounds==1&&!stage.PhotoRewardClaimed,"A 선명도 완료 순간 1회·획득 전 재생");
            editor.SetValues(0,0,0);editor.SetValues(target.x,target.y,target.z);stage.PhotoSuccess();yield return new WaitForSeconds(.6f);Check(sounds==1,"슬라이더 재조정·중복 완료 호출 재생 안 함");
            app.CloseAllWindows();yield return null;stage.Open("photo");yield return new WaitForSeconds(.2f);editor=app.Windows.GetComponentInChildren<PhotoCorrectionRenderer>();editor.Load("photo"+stage.Randoms.Frequency);Check(sounds==1,"A 재열기 재생 안 함");
            stage.Open("sound");yield return null;stage.RegisterClassic("classic");
            for(int i=0;i<10;i++){int at=stage.Playlist.IndexOf(stage.Randoms.Hint[i]);while(at>i){stage.MoveSong(at,-1);at--;}}
            app.Windows.GetComponentInChildren<PlaylistView>().Draw();
            Check(stage.PlaylistSolved&&!stage.PlaylistRewardClaimed&&sounds==2,"B 리스트 완료 순간 1회·획득 전 재생");
            stage.MoveSong(0,1);app.CloseAllWindows();yield return null;stage.Open("sound");yield return new WaitForSeconds(.2f);Check(sounds==2,"B 재열기·재정렬 재생 안 함");
            var saved=new SaveService().Progress.stage05;Check(saved.photoSolved&&saved.playlistSolved,"A/B 클리어 상태 디스크 저장");
            app.Save.Progress.stage05=saved;app.Stages.Enter("Stage05",app.ScreenRoot,app.GetComponent<FakeCursor>());yield return new WaitForSeconds(.3f);stage=app.ScreenRoot.GetComponentInChildren<Stage05Controller>();
            stage.Open("photo");yield return null;editor=app.Windows.GetComponentInChildren<PhotoCorrectionRenderer>();editor.Load("photo"+stage.Randoms.Frequency);editor.SetValues(target.x,target.y,target.z);stage.Open("sound");stage.PhotoSuccess();stage.MoveSong(0,1);yield return new WaitForSeconds(.6f);
            Check(stage.PhotoSolved&&stage.PlaylistSolved&&sounds==2,"저장 불러오기 후 A/B 효과음 재생 안 함");
            app.Audio.Played-=heard;
            app.Begin(false);yield return null;stage=app.ScreenRoot.GetComponentInChildren<Stage05Controller>();Check(!stage.PhotoSolved&&!stage.PlaylistSolved,"이어하기 재시작은 두 퍼즐 초기화");
        }
    }
}
#endif
