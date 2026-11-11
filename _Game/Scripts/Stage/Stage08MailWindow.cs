using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed class Stage08MailWindow:MonoBehaviour
    {
        Stage08Controller stage;FakeWindow window;string folder="inbox",selected;
        public static void Open(Stage08Controller stage)
        {
            if(!FakeWindow.TryCreate(stage.App.Windows,"s8:mail",stage.Text("mail"),out var w,105,80,820,630))return;
            var self=w.gameObject.AddComponent<Stage08MailWindow>();self.stage=stage;self.window=w;
            UIFactory.Button(w.Toolbar,stage.Text("writeMail"),0,1,145,30,()=>Compose(stage,w));
            stage.MailChanged+=self.Draw;w.Closed+=()=>stage.MailChanged-=self.Draw;self.Draw();
        }
        void Draw()
        {
            if(!window||window.IsClosing)return;UIFactory.Clear(window.Content);var names=stage.Text("mailFolders").Split('|');var ids=new[]{"inbox","sent","spam","trash"};
            for(int i=0;i<4;i++){string id=ids[i];UIFactory.Button(window.Content,names[i]+" ("+stage.Messages.Count(m=>m.folder==id)+")",0,i*44,164,40,()=>{folder=id;selected=null;Draw();});}
            var list=stage.Messages.Where(m=>m.folder==folder).Reverse().ToArray();
            if(!list.Any(m=>m.id==selected))selected=list.FirstOrDefault()?.id;
            UIFactory.Panel(window.Content,"목록 머리글",175,0,618,30,new Color(.83f,.81f,.86f));
            UIFactory.Text(window.Content,stage.Text(folder=="sent"?"recipient":"sender"),181,3,180,25,16);
            UIFactory.Text(window.Content,stage.Text("subject"),370,3,245,25,16);
            UIFactory.Text(window.Content,stage.Text("mailDate"),630,3,145,25,16);
            var area=UIFactory.Panel(window.Content,"메일 목록",175,30,598,160,Color.white).rectTransform;area.gameObject.AddComponent<RectMask2D>();var rows=UIFactory.Rect(area,"목록 행",0,0,598,Mathf.Max(160,list.Length*35));
            var sc=area.gameObject.AddComponent<ScrollRect>();sc.viewport=area;sc.content=rows;sc.horizontal=false;sc.movementType=ScrollRect.MovementType.Clamped;sc.scrollSensitivity=30;
            var track=UIFactory.Panel(window.Content,"메일 스크롤바",775,30,18,160,new Color(.85f,.85f,.88f));var handle=UIFactory.Panel(track.transform,"손잡이",0,0,18,45,WindowSkin.Shadow);handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.sizeDelta=Vector2.zero;var bar=track.gameObject.AddComponent<Scrollbar>();bar.handleRect=handle.rectTransform;bar.targetGraphic=handle;bar.direction=Scrollbar.Direction.BottomToTop;sc.verticalScrollbar=bar;
            for(int i=0;i<list.Length;i++)
            {
                var m=list[i];bool active=selected==m.id;var b=UIFactory.Button(rows,"",0,i*35,598,34,()=>{selected=m.id;m.read=true;Draw();},active?new Color(.13f,.1f,.45f):Color.white);
                var color=active?Color.white:Color.black;
                var sender=UIFactory.Text(b.transform,(m.read?"":"● ")+(folder=="sent"?m.recipient:m.sender),6,3,182,28,14,color);
                var subject=UIFactory.Text(b.transform,m.subject,195,3,252,28,16,color);
                var date=UIFactory.Text(b.transform,m.date,455,3,138,28,14,color);
                foreach(var label in new[]{sender,subject,date}){label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Ellipsis;label.raycastTarget=false;}
            }
            var message=list.FirstOrDefault(m=>m.id==selected);if(message==null)return;
            FileUI.ScrollText(window.Content,message.sender+"\n"+message.subject+"\n"+message.date+"\n\n"+message.body,175,200,615,245,Color.black);
            if(!string.IsNullOrEmpty(message.attachment)){var n=stage.Files.Find(message.attachment);UIFactory.Button(window.Content,n.displayName,180,455,300,35,()=>stage.Grant(n.id));}
        }
        public static void Compose(Stage08Controller stage,FakeWindow parent=null)
        {
            if(!stage.Ready||!FakeWindow.TryCreate(stage.App.Windows,"s8:compose",stage.Text("writeMail"),out var w,175,110,700,560))return;
            if(parent)w.SetParentWindow(parent);var form=w.gameObject.AddComponent<Stage08Compose>();form.Setup(stage,w);
        }
    }
    public sealed class Stage08Compose:MonoBehaviour
    {
        Stage08Controller stage;FakeWindow window;TMP_InputField recipient,body;TMP_Text attached;readonly List<string> files=new();int length;bool updating;
        public void Setup(Stage08Controller s,FakeWindow w)
        {
            stage=s;window=w;UIFactory.Button(w.Toolbar,s.Text("attach"),5,1,120,30,()=>Pick("desktop"));
            UIFactory.Button(w.Toolbar,s.Text("send"),535,1,120,30,()=>{if(stage.Send(recipient.text,body.text,files))w.Close();});
            UIFactory.Text(w.Content,s.Text("recipient"),10,8,135,30,20);recipient=FileUI.Input(w.Content,"",150,5,510,36);
            UIFactory.Text(w.Content,s.Text("subject")+" "+s.Text("replyTitle"),10,50,650,32,20);
            body=FileUI.Input(w.Content,"",10,90,650,270,true);
            // 실제 입력 문자열 대신 입력 한 번당 준비된 문장 한 글자. 삭제 키도 상태를 바꾸지 않는다.
            body.onValidateInput=(text,pos,c)=>'\0';body.onValueChanged.AddListener(_=>Restore());
            attached=UIFactory.Text(w.Content,"",10,370,650,60,17);DrawAttachments();recipient.Select();recipient.ActivateInputField();
        }
        void Restore(){if(updating)return;updating=true;body.SetTextWithoutNotify(stage.Rules.autoTypeText.Substring(0,length));body.caretPosition=length;updating=false;}
        void Update()
        {
            if(!body||!body.isFocused||!stage.Ready)return;var keyboard=Keyboard.current;if(keyboard==null)return;
            foreach(var key in keyboard.allKeys)if(key.wasPressedThisFrame&&key.keyCode!=Key.Backspace&&key.keyCode!=Key.Delete&&key.keyCode!=Key.LeftShift&&key.keyCode!=Key.RightShift&&key.keyCode!=Key.LeftCtrl&&key.keyCode!=Key.RightCtrl&&key.keyCode!=Key.Escape&&key.keyCode!=Key.Tab)
            {if(length<stage.Rules.autoTypeText.Length){length++;stage.App.Audio.Play("sfx_autotype_key");}break;}Restore();
        }
        void DrawAttachments(){attached.text=stage.Text("attach")+": "+string.Join(", ",files.Select(id=>stage.Files.Find(id)?.displayName));}
        void Pick(string parent)
        {
            bool created=FakeWindow.TryCreate(stage.App.Windows,"s8:attach",stage.Text("attach"),out var picker,250,170,540,420);if(created)picker.SetParentWindow(window);
            UIFactory.Clear(picker.Content);UIFactory.Clear(picker.Toolbar);
            UIFactory.Button(picker.Toolbar,stage.Text("desktop"),0,1,160,30,()=>Pick("desktop"));
            var viewport=UIFactory.Panel(picker.Content,"첨부 목록",0,0,510,315,Color.white).rectTransform;viewport.gameObject.AddComponent<RectMask2D>();var rows=UIFactory.Rect(viewport,"첨부 행",0,0,505,315);var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.content=rows;scroll.viewport=viewport;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;
            int i=0;foreach(var n in stage.Files.Children(parent).Where(n=>stage.Files.CanUse(n)).ToArray())
            {
                var node=n;UIFactory.Button(rows,n.displayName,12,8+i++*37,485,34,()=>{if(node.contentType=="folder"){Pick(node.id);return;}if(!files.Contains(node.id))files.Add(node.id);DrawAttachments();picker.Close();});
            }
            foreach(var id in files.ToArray()){string captured=id;UIFactory.Button(rows,stage.Text("removeAttachment")+" "+(stage.Files.Find(id)?.displayName??id),12,8+i++*37,485,34,()=>{files.Remove(captured);DrawAttachments();Pick(parent);});}rows.sizeDelta=new Vector2(505,Mathf.Max(315,8+i*37));
        }
    }
}
