using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CoComputar
{
    public sealed class MailPuzzle : MonoBehaviour
    {
        FakeWindow window; RectTransform content; bool solved, dissolving;
        readonly List<FakeIcon> letters=new();
        FileRecord[] data;
        public void Open()
        {
            solved=GameApp.Instance.Save.Progress.flags.Contains("stage01.mail");
            data=JsonUtility.FromJson<FileDocument>(GameApp.Instance.Stages.Current.letters.text).files;
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"mail","남겨진 편지",out window,220,145,750,505)) {Destroy(this);return;}
            window.SetFileBackground(40);window.AddHiddenToggle(Render); content=UIFactory.Rect(window.Content,"우편함",0,40,730,405);
            window.Closed=()=>Destroy(this); Render();
        }
        void Render()
        {
            if(dissolving) return;
            UIFactory.Clear(content); letters.Clear();
            var mail=FakeIcon.Create(content,"mailbox",solved?"배달 완료":"우체통",solved?"mail_closed":"mail_open",550,100);
            if(solved) { DisableMailbox(mail);return; }
            mail.gameObject.AddComponent<DropTarget>().Category="mail";
            for(int i=0;i<data.Length;i++)
            {
                var record=data[i];if(record.hidden&&!window.ShowHidden) continue;
                var icon=FakeIcon.Create(content,record.id,record.name,record.spriteKey,25+i%3*145,25+i/3*135,()=>WindowApps.Note(record.name,record.content,"mail:"+record.id),true,record.hidden);
                icon.Dropped=(item,target)=>{if(item.Id=="5.txt"&&!solved) StartCoroutine(Success());}; letters.Add(icon);
            }
        }
        static void DisableMailbox(FakeIcon icon)
        {
            icon.enabled=false;icon.Draggable=false;icon.Clicked=null;icon.DoubleClicked=null;
            icon.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
        }
        IEnumerator Success()
        {
            solved=true; GameApp.Instance.Save.Flag("stage01.mail"); GameApp.Instance.Audio.Play("sfx_mail_close");
            foreach(var open in GameApp.Instance.Windows.GetComponentsInChildren<FakeWindow>())
                if(System.Array.Exists(data,record=>open.Key=="note:mail:"+record.id)) open.Close();
            var mailbox=content.GetComponentInChildren<DropTarget>();
            DisableMailbox(mailbox.GetComponent<FakeIcon>());
            mailbox.GetComponentInChildren<TMPro.TextMeshProUGUI>().text="배달 완료";
            mailbox.GetComponent<FakeIcon>().Artwork.sprite=GameApp.Instance.Catalog.sprites.Get("mail_closed");
            dissolving=true;int seed=0;
            foreach(var letter in letters) if(letter)
            {
                letter.Draggable=false;letter.Clicked=null;letter.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
                if(letter.Id=="5.txt") {letter.gameObject.SetActive(false);continue;}
                var art=letter.Artwork;
                PixelDriftGraphic.Create(art,719+seed++,1);
            }
            for(float t=0;t<PixelDriftGraphic.Duration;t+=Time.deltaTime)
            {
                foreach(var letter in letters) if(letter)
                {
                    var label=letter.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if(label) {var c=label.color;c.a=1-Mathf.Clamp01(t/.7f);label.color=c;}
                }
                yield return null;
            }
            dissolving=false;
            if(window) Render();
        }
    }
}
