using System;
using System.Linq;
using UnityEngine;
namespace CoComputar
{
    [Serializable] public sealed class Stage05Progress
    {
        public int playlistRewardVersion;public int photoRangeVersion;public Stage05Randomizer randoms;public FakeFileNode[] files;public string[] playlist;
        public bool radioSolved,photoSolved,photoRewardClaimed,playlistSolved,playlistRewardClaimed,classicRegistered,finalDone;
        public Vector3 photoValues;public string photoFile;
    }
    public sealed partial class Stage05Controller
    {
        void FilesChanged(){Refresh();SaveStageProgress();}
        public void SaveStageProgress()
        {
            if(busy||Files==null||app.Save.Progress.stageId!="Stage05")return;
            var snapshot=new Stage05Progress{photoRangeVersion=1,playlistRewardVersion=1,randoms=Randoms,files=Files.Nodes.ToArray(),playlist=Playlist.ToArray(),radioSolved=RadioSolved,photoSolved=PhotoSolved,photoRewardClaimed=PhotoRewardClaimed,playlistSolved=PlaylistSolved,playlistRewardClaimed=PlaylistRewardClaimed,classicRegistered=ClassicRegistered,finalDone=finalDone,photoValues=Vector3.zero,photoFile=null};
            // 저장 스냅샷은 진행 중인 노드 참조와 분리한다. 결합 연출의 중간 상태는 저장하지 않는다.
            app.Save.Progress.stage05=JsonUtility.FromJson<Stage05Progress>(JsonUtility.ToJson(snapshot));
            app.Save.SaveProgress();
        }
        void RestoreProgress()
        {
            var s=app.Save.Progress.stage05;
            // JsonUtility round-trips an unvisited stage's null nested object as an
            // empty snapshot. Never replace the initial file tree with that shell.
            if(!CanRestore(s))return;
            Randoms=s.randoms;Files.Nodes.Clear();Files.Nodes.AddRange(s.files);Playlist=new System.Collections.Generic.List<string>(s.playlist);
            RadioSolved=s.radioSolved;PhotoSolved=s.photoSolved;PhotoRewardClaimed=s.photoRewardClaimed;PlaylistSolved=s.playlistSolved;PlaylistRewardClaimed=s.playlistRewardClaimed||(s.playlistRewardVersion<1&&s.playlistSolved);ClassicRegistered=s.classicRegistered;
            if(ClassicRegistered)Files.Nodes.RemoveAll(n=>n.id=="classic");
            finalDone=s.finalDone;finalStarted=finalDone;PhotoValues=Vector3.zero;PhotoFile=null;
            // 구버전 1~99 정답은 새 범위로 이관하며 완료/획득 기록은 보존한다.
            for(int i=0;i<3;i++){float value=Randoms.PhotoTarget[i];if(s.photoRangeVersion<1)value=Mathf.Ceil(value*Data.photo.sliderMax/100f);Randoms.PhotoTarget[i]=Mathf.Clamp(Mathf.RoundToInt(value),Mathf.Max(1,Data.photo.sliderMin+1),Data.photo.sliderMax);}
        }
        bool CanRestore(Stage05Progress s)
        {
            if(s?.randoms==null||s.files==null||s.files.Length==0||s.playlist==null)return false;
            if(s.files.Any(n=>n==null||string.IsNullOrEmpty(n.id))||s.files.Select(n=>n.id).Distinct().Count()!=s.files.Length)return false;
            // Deleted/moved folders remain nodes, so valid trash and folder moves survive.
            if(!s.files.Any(n=>n.id=="look")||!s.files.Any(n=>n.id=="soft"))return false;
            var r=s.randoms;
            bool Songs(string[] list)=>list!=null&&list.Length==Data.songs.Length&&list.Distinct().Count()==list.Length&&list.All(song=>Data.songs.Contains(song));
            return r.Photos!=null&&r.Photos.Length>0&&r.Photos.Contains(r.Frequency)&&Songs(r.Hint)&&Songs(r.Initial)&&Songs(s.playlist);
        }
    }
}
