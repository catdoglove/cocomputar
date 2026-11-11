using System.Linq;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed class PlaylistView:MonoBehaviour
    {
        Stage05Controller stage;FakeWindow window;RectTransform list;int selected;Button up,down;TMP_Text lockText;Image reward;Button rewardButton;
        public void Setup(Stage05Controller owner,FakeWindow w)
        {
            stage=owner;window=w;
            UIFactory.Panel(w.Content,"플레이어",8,8,390,90,new Color(.16f,.17f,.19f));UIFactory.Text(w.Content,"00:00    SOUND / STEREO",20,14,350,35,21,new Color(.7f,1,.7f));
            lockText=UIFactory.Text(w.Content,"[잠김]",290,54,100,28,19,Color.white);UIFactory.Text(w.Content,"▶    II    ■",20,54,250,28,24,Color.white);
            reward=UIFactory.Sprite(w.Content,GameApp.Instance.Catalog.sprites.Get("s5_half_b"),320,46,40,44);reward.gameObject.name="리스트 보상";rewardButton=reward.gameObject.AddComponent<Button>();rewardButton.transition=Selectable.Transition.None;rewardButton.onClick.AddListener(CollectReward);
            list=UIFactory.Rect(w.Content,"재생 목록",10,110,385,410);up=UIFactory.Button(w.Content,"위로",30,535,150,40,()=>Move(-1));down=UIFactory.Button(w.Content,"아래로",220,535,150,40,()=>Move(1));Draw();
        }
        public void CollectReward()
        {
            if(!stage.ClaimPlaylistReward())return;reward.raycastTarget=false;rewardButton.interactable=false;StopAllCoroutines();StartCoroutine(FadeReward());
        }
        IEnumerator FadeReward(){for(float t=0;t<.2f;t+=Time.unscaledDeltaTime){reward.color=new Color(1,1,1,1-t/.2f);yield return null;}reward.color=Color.clear;}
        void Move(int d){if(stage.PlaylistSolved)return;int next=Mathf.Clamp(selected+d,0,9);stage.MoveSong(selected,d);selected=next;Draw();}
        public void Draw()
        {
            UIFactory.Clear(list);for(int i=0;i<stage.Playlist.Count;i++)
            {
                int index=i;string song=stage.Playlist[i];bool blank=song==stage.Data.classicSong&&!stage.ClassicRegistered;
                var row=UIFactory.Button(list,(blank?"ㅁㅁㅁㅁㅁㅁㅁㅁㅁ":song),0,i*40,382,37,()=>{if(stage.PlaylistSolved)return;selected=index;Draw();},i==selected?new Color(.95f,.75f,.87f):new Color(.91f,.89f,.8f));var text=row.GetComponentInChildren<TMP_Text>();text.fontSize=17;text.alignment=TextAlignmentOptions.MidlineLeft;text.rectTransform.anchoredPosition=new Vector2(44,-1);text.rectTransform.sizeDelta=new Vector2(332,35);UIFactory.Sprite(row.transform,GameApp.Instance.Catalog.sprites.Get("music"),5,4,30,29);
                if(blank){var drop=row.gameObject.AddComponent<DropTarget>();drop.Category="s5:slot";drop.IsFolderWindow=true;}
                row.interactable=!stage.PlaylistSolved;
            }
            up.interactable=down.interactable=!stage.PlaylistSolved;lockText.text=stage.PlaylistSolved?"":"[잠김]";bool pending=stage.PlaylistSolved&&!stage.PlaylistRewardClaimed;reward.color=pending?Color.white:Color.clear;reward.raycastTarget=pending;rewardButton.interactable=pending;
        }
    }
}
