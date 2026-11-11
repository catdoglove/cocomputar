using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CoComputar
{
    public sealed class Map07Window:MonoBehaviour
    {
        public FakeWindow Window;public string Region{get;private set;}="";Stage07Controller stage;RectTransform scene,inventory;GameObject inputBlock;Coroutine transition;Image transitionImage;bool dollMusicPaused;
        public static Map07Window Open(Stage07Controller stage)
        {
            if(!FakeWindow.TryCreate(stage.App.Windows,"s7:map","map.exe",out var w,90,60,845,650))return w.GetComponent<Map07Window>();
            var self=w.gameObject.AddComponent<Map07Window>();self.Window=w;self.stage=stage;
            w.Content.gameObject.AddComponent<RectMask2D>();
            UIFactory.Button(w.Toolbar,"전체 지도",4,0,145,32,()=>self.Navigate(""));
            self.scene=UIFactory.Panel(w.Content,"지도 화면",5,5,815,430,new Color(.8f,.86f,.65f)).rectTransform;
            self.inventory=UIFactory.Panel(w.Content,"인벤토리",5,440,815,140,new Color(.88f,.85f,.73f)).rectTransform;self.Draw();return self;
        }
        public void Navigate(string region)
        {
            if(!stage.Ready||region!=""&&!stage.Data.regions.Any(r=>r.id==region))return;
            if(region!=Region)RestoreDollMusic();
            bool fromMap=Region=="";if(transition!=null)StopCoroutine(transition);if(transitionImage)Destroy(transitionImage.gameObject);scene.anchoredPosition=new Vector2(5,-5);
            Region=region;stage.App.Audio.Play(fromMap?"sfx_map_pin":"sfx_map_move");Draw();if(region!="")transition=StartCoroutine(Transition(fromMap));
        }
        IEnumerator Transition(bool fromMap)
        {
            var pin=stage.Data.pins.FirstOrDefault(p=>p.start==Region);
            if(fromMap&&pin!=null)transitionImage=UIFactory.Sprite(scene,stage.App.Catalog.sprites.Get(stage.Data.mapImage),170,0,455,430);
            for(float t=0;t<.5f;t+=Time.unscaledDeltaTime)
            {
                float progress=t/.5f;
                if(transitionImage){float scale=Mathf.Lerp(1,2.4f,progress);transitionImage.rectTransform.localScale=Vector3.one*scale;transitionImage.rectTransform.anchoredPosition=new Vector2(170-pin.x*455*(scale-1),pin.y*430*(scale-1));transitionImage.color=new Color(1,1,1,1-progress);}
                else scene.anchoredPosition=new Vector2(5+(1-progress)*45,-5);
                yield return null;
            }
            scene.anchoredPosition=new Vector2(5,-5);if(transitionImage)Destroy(transitionImage.gameObject);transitionImage=null;transition=null;
        }
        public void Draw()
        {
            UIFactory.Clear(scene);UIFactory.Clear(inventory);
            UIFactory.Text(inventory,"소지품 · 드래그하여 사용",12,5,770,30,19);
            if(stage.Shears)Item("shears","정원가위","s7_shears",20);
            if(stage.Key)Item("key","열쇠","white_key",140);
            if(Region=="")
            {
                UIFactory.Sprite(scene,stage.App.Catalog.sprites.Get(stage.Data.mapImage),170,0,455,430);
                foreach(var p in stage.Data.pins)
                {
                    var pin=UIFactory.Button(scene,"●",170+p.x*455-16,p.y*430-16,32,32,()=>Navigate(p.start),Color.clear);pin.GetComponentInChildren<TMPro.TMP_Text>().color=Color.red;
                    UIFactory.Text(scene,p.label,170+p.x*455+16,p.y*430-12,150,26,16,Color.black);
                }
                return;
            }
            var region=stage.Data.regions.Single(r=>r.id==Region);
            UIFactory.Panel(scene,"하늘",0,0,815,215,new Color(.67f,.81f,.84f));UIFactory.Panel(scene,"땅",0,215,815,215,new Color(.43f,.57f,.3f));
            UIFactory.Panel(scene,"길",300,215,215,215,new Color(.76f,.68f,.5f));UIFactory.Text(scene,region.name,20,12,740,38,27,Color.black);
            for(int i=0;i<region.exits.Length;i++){var e=region.exits[i];float x=e.label=="왼쪽"?20:e.label=="오른쪽"?650:330;float y=e.label=="위"?60:360;UIFactory.Button(scene,e.label+" →",x,y,140,40,()=>Navigate(e.target),new Color(.63f,.48f,.28f));}
            string kind=region.objectType;
            if(kind=="keyBush"||kind=="bush"||kind=="dollBush")
            {
                if(!stage.CutBushes.Contains(Region))Object("bush","수풀","tree",310,115,()=>{},"bush");
                else if(kind=="keyBush"&&!stage.Key&&!stage.DoorUnlocked)Object("key","열쇠","white_key",350,180,()=>Collect("key"));
                else if(kind=="dollBush")
                {
                    var doll=Object("doll","인형",stage.DollBeheaded?"player_body":"player_front",345,180,()=>{},stage.DollBeheaded?null:"doll");
                    if(stage.DollBeheaded)doll.Artwork.color=new Color(.8f,.7f,.65f);
                }
            }
            if(kind=="shears"&&!stage.Shears)Object("shears","정원가위","s7_shears",350,160,()=>Collect("shears"));
            if(kind=="pond")
            {var pond=UIFactory.Panel(scene,"연못",80,160,650,170,new Color(.23f,.53f,.64f));for(int i=0;i<7;i++)UIFactory.Panel(pond.transform,"물결",30+i*83,35+i%3*33,62,3,new Color(.65f,.84f,.86f)).raycastTarget=false;}
            if(kind=="rock")
            {
                UIFactory.Panel(scene,"돌구멍",320,150,170,120,new Color(.05f,.06f,.05f));UIFactory.Panel(scene,"약한 빛",365,195,65,15,new Color(.25f,.28f,.2f));
                var rock=UIFactory.Panel(scene,"돌",300,stage.RockMoved?280:135,210,155,new Color(.37f,.39f,.36f));WindowSkin.Bevel(rock.rectTransform);
                if(!stage.RockMoved){var drag=rock.gameObject.AddComponent<MapRockDrag>();drag.Owner=this;}
            }
            if(kind=="door")
            {
                var door=Object("door","door.exe","door",350,150,()=>{if(stage.DoorUnlocked)stage.ExitDoor();else{stage.App.Audio.Play("sfx_door_locked");StartCoroutine(Shake(scene.Find("door").GetComponent<RectTransform>()));}});
                if(!stage.DoorUnlocked){var padlock=UIFactory.Panel(door.transform,"자물쇠",48,30,24,28,new Color(.96f,.72f,.16f));UIFactory.Text(padlock.transform,"U",0,-14,24,24,22,Color.black);var target=padlock.gameObject.AddComponent<Map07Target>();target.Kind="lock";}
            }
        }
        FakeIcon Object(string id,string label,string sprite,float x,float y,Action click,string target=null)
        {
            var icon=FakeIcon.Create(scene,id,label,sprite,x,y,()=>{if(stage.Ready)click();});icon.name=id;
            if(target!=null)icon.gameObject.AddComponent<Map07Target>().Kind=target;return icon;
        }
        void Item(string id,string label,string sprite,float x)
        {
            var icon=FakeIcon.Create(inventory,"inventory:"+id,label,sprite,x,36,null,true);icon.DroppedOutside=e=>
            {
                if(!stage.Ready)return;var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
                var top=hits.FirstOrDefault(h=>h.gameObject&&!h.gameObject.transform.IsChildOf(icon.transform));
                var target=top.gameObject?top.gameObject.GetComponentInParent<Map07Target>():null;
                if(target&&target.transform.IsChildOf(scene))Use(id,target.Kind);else stage.App.Audio.Play("sfx_drag_return");
            };
        }
        public void Collect(string item)
        {
            if(!stage.Ready)return;
            if(item=="shears"&&Region=="4t"&&!stage.Shears)stage.Shears=true;
            else if(item=="key"&&Region=="1b"&&stage.CutBushes.Contains(Region)&&!stage.Key&&!stage.DoorUnlocked)stage.Key=true;else return;
            stage.App.Audio.Play("sfx_item_get");Draw();StartCoroutine(Spark());
        }
        public void Use(string item,string target)
        {
            if(!stage.Ready)return;var kind=stage.Data.regions.FirstOrDefault(r=>r.id==Region)?.objectType;
            if(item=="shears"&&stage.Shears&&target=="bush"&&(kind=="bush"||kind=="keyBush"||kind=="dollBush")&&stage.CutBushes.Add(Region))
            {stage.App.Audio.Play("sfx_bush_cut");Draw();StartCoroutine(Leaves());return;}
            if(item=="shears"&&stage.Shears&&target=="doll"&&kind=="dollBush"&&stage.CutBushes.Contains(Region)&&!stage.DollBeheaded)
            {stage.DollBeheaded=true;stage.App.Save.Flag("stage07.dollBeheaded");StartCoroutine(Doll());return;}
            if(item=="key"&&stage.Key&&target=="lock"&&kind=="door"&&!stage.DoorUnlocked)
            {stage.Key=false;stage.DoorUnlocked=true;stage.App.Audio.Play("sfx_lock_open");Draw();StartCoroutine(Spark());return;}
            stage.App.Audio.Play("sfx_drag_return");
        }
        public bool PushRock(Vector2 delta)
        {
            if(!stage.Ready||Region!="3t"||stage.RockMoved)return false;
            if(delta.y>=-40||-delta.y<=Mathf.Abs(delta.x)){stage.App.Audio.Play("sfx_drag_return");return false;}
            stage.RockMoved=true;stage.App.Audio.Play("sfx_rock_drag");stage.EmitMapEvent(stage.Data.rockMovedEvent);Draw();return true;
        }
        IEnumerator Spark()
        {
            var glow=UIFactory.Text(inventory,"+",95,45,55,65,50,new Color(1,.8f,.2f));for(float t=0;t<.6f;t+=Time.unscaledDeltaTime){if(!glow)yield break;glow.color=new Color(1,.8f,.2f,1-t/.6f);glow.rectTransform.anchoredPosition+=Vector2.up*Time.unscaledDeltaTime*35;yield return null;}if(glow)Destroy(glow.gameObject);
        }
        IEnumerator Leaves()
        {
            var leaves=new List<Image>();for(int i=0;i<15;i++)leaves.Add(UIFactory.Panel(scene,"잎",345+i%5*12,180+i/5*12,8,5,new Color(.12f,.4f,.12f)));
            for(float t=0;t<.7f;t+=Time.unscaledDeltaTime){for(int i=0;i<leaves.Count;i++)if(leaves[i]){leaves[i].raycastTarget=false;leaves[i].rectTransform.anchoredPosition+=new Vector2((i%5-2)*40,50+i*3)*Time.unscaledDeltaTime;leaves[i].color=new Color(.12f,.4f,.12f,1-t/.7f);}yield return null;}foreach(var leaf in leaves)if(leaf)Destroy(leaf.gameObject);
        }
        IEnumerator Doll()
        {
            dollMusicPaused=true;stage.App.Audio.PauseEventMusic();
            stage.AnimationBusy=true;inputBlock=UIFactory.Panel(stage.App.Modal,"인형 연출 입력 잠금",0,0,1024,768,Color.clear).gameObject;
            stage.App.Audio.Play("sfx_doll_pop");Draw();var head=UIFactory.Sprite(scene,stage.App.Catalog.sprites.Get("player_head"),355,160,65,65);
            try{for(float t=0;t<.85f;t+=Time.unscaledDeltaTime){head.rectTransform.anchoredPosition=new Vector2(355+t*580,-160+280*t-100*t*t);head.rectTransform.localRotation=Quaternion.Euler(0,0,-t*600);yield return null;}}
            finally{if(head)Destroy(head.gameObject);Unlock();}
        }
        IEnumerator Shake(RectTransform target){var origin=target.anchoredPosition;for(float t=0;t<.3f;t+=Time.unscaledDeltaTime){if(!target)yield break;target.anchoredPosition=origin+new Vector2(Mathf.Sin(t*80)*5,0);yield return null;}if(target)target.anchoredPosition=origin;}
        void Unlock(){if(inputBlock)Destroy(inputBlock);inputBlock=null;stage.AnimationBusy=false;}
        void RestoreDollMusic(){if(!dollMusicPaused)return;dollMusicPaused=false;if(stage)stage.App.Audio.ResumeEventMusic();}
        void OnDisable(){RestoreDollMusic();if(stage)Unlock();}
    }
    public sealed class Map07Target:MonoBehaviour {public string Kind;}
    public sealed class MapRockDrag:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public Map07Window Owner;Vector2 start,origin;RectTransform Rect=>(RectTransform)transform;
        public void OnBeginDrag(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;start=e.position;origin=Rect.anchoredPosition;}
        public void OnDrag(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;var delta=(e.position-start)/Rect.lossyScale.x;Rect.anchoredPosition=origin+new Vector2(Mathf.Clamp(delta.x,-12,12),Mathf.Clamp(delta.y,-140,12));}
        public void OnEndDrag(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;var delta=(e.position-start)/Rect.lossyScale.x;Rect.anchoredPosition=origin;Owner.PushRock(delta);}
    }
}
