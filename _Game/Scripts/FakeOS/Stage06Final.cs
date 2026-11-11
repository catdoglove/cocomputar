using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed class StickerBookWindow:MonoBehaviour
    {
        Stage06Controller stage;public ImageViewer Viewer;
        public static StickerBookWindow Open(Stage06Controller stage)
        {
            var viewer=ImageViewer.Open(new ViewerDefinition{id="s6:book",title="스티커북.png",imageKey="s6_book",zoomInLevels=0});var self=viewer.GetComponent<StickerBookWindow>();if(self)return self;self=viewer.gameObject.AddComponent<StickerBookWindow>();self.stage=stage;self.Viewer=viewer;self.Draw();return self;
        }
        public void Draw()
        {
            foreach(var slot in stage.Data.slots)
            {
                var existing=Viewer.PictureRoot.Find("slot:"+slot.id);if(existing){existing.name="retired:"+slot.id;existing.gameObject.SetActive(false);Destroy(existing.gameObject);}
                if(stage.BookComplete)continue;
                var area=UIFactory.Panel(Viewer.PictureRoot,"slot:"+slot.id,slot.x,slot.y,slot.width,slot.height,Color.clear);
                var target=area.gameObject.AddComponent<DropTarget>();target.Category="s6slot:"+slot.id;target.IsFolderWindow=true;
                if(stage.stuck.Contains(slot.id))
                {
                    // Reveal the exact matching region of the finished book, without aspect-fit gaps.
                    var artwork=UIFactory.Rect(area.transform,"붙인 스티커",0,0,slot.width,slot.height).gameObject.AddComponent<RawImage>();
                    var sprite=GameApp.Instance.Catalog.sprites.Get("s6_book_done");var r=sprite.textureRect;
                    artwork.texture=sprite.texture;
                    artwork.uvRect=new Rect((r.x+r.width*slot.x/590f)/sprite.texture.width,(r.y+r.height*(1-(slot.y+slot.height)/410f))/sprite.texture.height,r.width*slot.width/590f/sprite.texture.width,r.height*slot.height/410f/sprite.texture.height);
                    artwork.raycastTarget=false;
                }
            }
            if(stage.BookComplete)Viewer.Picture.sprite=GameApp.Instance.Catalog.sprites.Get("s6_book_done");
        }
    }
    public sealed partial class Stage06Controller
    {
        public void Stick(string id,string slot)
        {
            if(!Ready||id!=slot||!Data.slots.Any(s=>s.id==id)||!Files.Find(id).available||stuck.Contains(id)){app.Audio.Play("sfx_drag_return");return;}
            stuck.Add(id);Files.Find(id).available=false;
            // Consumed stickers must not leave their standalone preview windows open.
            foreach(var viewer in app.Windows.GetComponentsInChildren<ImageViewer>())
                if(viewer.Definition.id=="s6:"+id)viewer.Window.Close();
            Files.Notify();app.Audio.Play("sfx_sticker_stick");foreach(var book in app.Windows.GetComponentsInChildren<StickerBookWindow>())book.Draw();
            if(BookComplete){var book=Files.Find("book");book.displayName="스티커북.exe";book.extension="exe";book.iconKey="program";app.Audio.Play("sfx_file_morph");Files.Notify();}
        }
        void RunBook()
        {
            if(!BlockSeen){BlockSeen=true;busy=true;app.Audio.SetMusicFade(0);app.Audio.Play("sfx_bgm_cut");app.Dialogue.Play(()=>StartCoroutine(RestoreBgm()),"S6_BLOCK");return;}
            var storm=GetComponent<PopupStorm>();if(storm&&storm.Active){storm.FocusAnswerArea();return;}if(storm)Destroy(storm);gameObject.AddComponent<PopupStorm>().Begin(this);
        }
        IEnumerator RestoreBgm(){for(float t=0;t<1;t+=Time.unscaledDeltaTime){app.Audio.SetMusicFade(t);yield return null;}app.Audio.SetMusicFade(1);busy=false;}
        public void FinishStage(){if(busy)return;busy=true;manager.RequestClear("door.exe");}
    }
    public sealed class FakePopup:MonoBehaviour{public bool Answer;public FakeWindow Window;public Button Confirm;}
    public sealed class PopupStorm:MonoBehaviour
    {
        Stage06Controller stage;public bool Active{get;private set;}bool clearing;readonly List<FakePopup> popups=new();int serial;
        public IReadOnlyList<FakePopup> Popups=>popups.Where(p=>p&&p.gameObject.activeInHierarchy).ToList();
        public void Begin(Stage06Controller owner){stage=owner;Active=true;StartCoroutine(Spawn());}
        IEnumerator Spawn(){for(int i=0;i<stage.Data.popupCount&&Active;i++){Create(i==stage.Randoms.DoorIndex,new Vector2(440-i*Mathf.Min(stage.Data.popupStepX,370f/(stage.Data.popupCount-1)),70+i*Mathf.Min(stage.Data.popupStepY,350f/(stage.Data.popupCount-1))));yield return new WaitForSecondsRealtime(stage.Data.popupDelay);}}
        FakePopup Create(bool answer,Vector2 position)
        {
            FakeWindow.TryCreate(GameApp.Instance.Windows,"s6:popup:"+(++serial),"경고",out var w,position.x,position.y,470,250);var p=w.gameObject.AddComponent<FakePopup>();p.Answer=answer;p.Window=w;popups.Add(p);
            if(answer)UIFactory.Sprite(w.Content,GameApp.Instance.Catalog.sprites.Get("door"),15,20,70,85);else UIFactory.Text(w.Content,"!",25,10,60,85,64,Color.yellow);
            bool userClose=false;
            void ClosePopup(){if(!Active||clearing||!w.gameObject.activeInHierarchy)return;userClose=true;w.Close();}
            UIFactory.Text(w.Content,"알 수 없는 오류가 발생했습니다.",95,25,340,75,21,Color.black);p.Confirm=UIFactory.Button(w.Content,"확인",170,130,120,34,()=>{if(answer){if(Active&&!clearing)StartCoroutine(Clear());}else ClosePopup();});
            var close=w.transform.Find("제목 표시줄/X").GetComponent<Button>();close.interactable=true;
            close.onClick.RemoveAllListeners();close.onClick.AddListener(ClosePopup);
            w.Closed+=()=>{popups.Remove(p);if(clearing||!Active)return;if(!userClose){Active=false;StopAllCoroutines();return;}var next=new Vector2(UnityEngine.Random.Range(10,544),UnityEngine.Random.Range(42,500));
                var previous=new Vector2(w.Rect.anchoredPosition.x,-w.Rect.anchoredPosition.y);
                if(Vector2.Distance(next,previous)<40)next=new Vector2(previous.x<277?500:20,previous.y<271?450:60);
                Create(answer,next);};GameApp.Instance.Audio.Play("sfx_popup_spawn");return p;
        }
        public void FocusAnswerArea(){var first=popups.LastOrDefault(p=>p);if(first)first.Window.Focus();}
        IEnumerator Clear(){clearing=true;foreach(var p in popups.ToArray()){if(p)p.Window.Close();yield return new WaitForSecondsRealtime(.05f);}Active=false;stage.FinishStage();}
        void OnDestroy(){Active=false;clearing=true;foreach(var p in popups.ToArray())if(p)p.Window.Close();}
    }
}
