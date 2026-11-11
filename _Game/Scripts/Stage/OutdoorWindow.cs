using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed class OutdoorWindow:MonoBehaviour
    {
        Stage02Controller stage;RectTransform area;Image cloud;DropTarget target;float elapsed;bool raining;int cloudIndex=-1;Transform plants;string plantState;
        readonly Transform[] plantSlots=new Transform[4];readonly string[] slotStates=new string[4];
        public static void Open(Stage02Controller stage)
        {
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"s2:outdoor","야외.gif",out var w,430,90,570,540))return;
            var self=w.Content.gameObject.AddComponent<OutdoorWindow>();self.stage=stage;self.area=w.Content;
            if(stage.OutdoorNight)
            {
                var night=GameApp.Instance.Catalog.sprites.Get(stage.Data.outdoorNightImage);
                if(night){var picture=UIFactory.Sprite(w.Content,night,0,0,550,475);picture.preserveAspect=false;}
                else UIFactory.Panel(w.Content,"밤 배경",0,0,550,475,new Color32(12,18,38,255));
                self.plants=UIFactory.Rect(w.Content,"식물",0,355,550,120);self.DrawPlants();return;
            }
            var bg=UIFactory.Sprite(w.Content,GameApp.Instance.Catalog.sprites.Get(stage.Data.outdoorImage),0,0,550,475);bg.preserveAspect=false;
            UIFactory.Panel(w.Content,"하늘",0,0,550,130,new Color32(153,217,234,255));
            UIFactory.Panel(w.Content,"잔디",0,400,550,75,new Color32(181,230,29,255));
            var clip=UIFactory.Rect(w.Content,"구름 범위",0,0,550,150);clip.gameObject.AddComponent<RectMask2D>();
            self.cloud=UIFactory.Sprite(clip,null,550,30,150,85);self.cloud.raycastTarget=true;self.target=self.cloud.gameObject.AddComponent<DropTarget>();
            self.plants=UIFactory.Rect(w.Content,"식물",0,355,550,120);self.DrawPlants();
        }
        void Update()
        {
            if(!stage||stage.OutdoorNight)return;if(!raining)elapsed+=Time.unscaledDeltaTime;
            int count=stage.Data.clouds.Length;float duration=Mathf.Min(10,stage.Data.cloudCycleSeconds)/count;int index=(int)(elapsed/duration)%count;
            if(index!=cloudIndex){cloudIndex=index;cloud.sprite=GameApp.Instance.Catalog.sprites.Get(stage.Data.clouds[index]);}
            cloud.rectTransform.anchoredPosition=new Vector2(Mathf.Lerp(550,-150,elapsed%duration/duration),-25);
            target.Category=index==stage.Data.photoCloud&&!raining?"photo-cloud":"drawn-cloud";
            if(CurrentPlantState!=plantState)DrawPlants();
        }
        string CurrentPlantState=>$"{stage.Rained}/{stage.Buds[0]}/{stage.Buds[1]}/{stage.Buds[2]}/{stage.Buds[3]}/{stage.BlackTaken}/{stage.Sepia}/{stage.PlantsGone}/{stage.TulipsPicked[0]}/{stage.TulipsPicked[1]}/{stage.TulipsPicked[2]}/{stage.TulipsPicked[3]}";
        public bool Water(){if(stage.OutdoorNight||raining||stage.Rained||cloudIndex!=stage.Data.photoCloud)return false;StartCoroutine(Rain());return true;}
        IEnumerator Rain()
        {
            raining=true;GameApp.Instance.Audio.PlayPuzzle(stage.Data.rainSound,true,true);var drops=UIFactory.Rect(area,"비",0,0,550,400);drops.gameObject.AddComponent<RectMask2D>();
            var streaks=new RectTransform[24];for(int i=0;i<24;i++){var p=UIFactory.Panel(drops,"빗방울",cloud.rectTransform.anchoredPosition.x+i%8*18,90+i/8*70,2,16,new Color(.3f,.5f,.85f,.7f));p.raycastTarget=false;streaks[i]=p.rectTransform;}
            for(float t=0;t<stage.Data.rainSeconds;t+=Time.unscaledDeltaTime){for(int i=0;i<streaks.Length;i++)streaks[i].anchoredPosition=new Vector2(streaks[i].anchoredPosition.x,-(90+(t*220+i*29)%300));yield return null;}
            GameApp.Instance.Audio.StopPuzzle(stage.Data.rainSound);Destroy(drops.gameObject);stage.Rained=true;raining=false;GameApp.Instance.Audio.Play("sfx_sprout_grow");DrawPlants();
        }
        void OnDisable(){if(stage&&GameApp.Instance)GameApp.Instance.Audio.StopPuzzle(stage.Data.rainSound);}
        void DrawPlants()
        {
            // 생성 직후 Update에서 버튼을 다시 만들면 누름/뗌 사이 클릭이 사라진다.
            plantState=CurrentPlantState;
            bool hidden=stage.PlantsGone||(!stage.Rained&&!stage.Sepia);
            for(int i=0;i<4;i++)
            {
                string state=$"{hidden}/{stage.Sepia}/{stage.Buds[i]}/{stage.TulipsPicked[i]}/{(i==stage.BlackFlowerIndex&&stage.BlackTaken)}";
                if(slotStates[i]==state)continue;slotStates[i]=state;
                if(plantSlots[i]){plantSlots[i].gameObject.SetActive(false);Destroy(plantSlots[i].gameObject);}
                if(hidden||(stage.Sepia&&stage.TulipsPicked[i]))continue;
                var slot=UIFactory.Rect(plants,"식물 슬롯 "+i,0,0,550,120);plantSlots[i]=slot;
                int index=i;string sprite=stage.Sepia?"tulip":stage.Buds[i]?(i==stage.BlackFlowerIndex?"photo_bloom":"bud"):"sprout";
                bool tulip=stage.Sepia;
                var hit=UIFactory.Panel(slot,"식물 클릭 영역 "+i,35+i*130,15,65,100,Color.clear);
                var image=UIFactory.Sprite(hit.transform,GameApp.Instance.Catalog.sprites.Get(sprite),0,0,65,100);
                image.gameObject.AddComponent<PlantGrowth>();
                var button=hit.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;button.interactable=tulip?!stage.TulipsPicked[index]:!stage.Buds[index];
                button.onClick.AddListener(()=>{
                    if(stage.PlantsGone)return;
                    if(tulip)
                    {
                        if(!stage.Sepia||stage.TulipsPicked[index])return;
                        button.interactable=false;stage.PickTulip(index);
                    }
                    else{if(stage.Sepia||stage.Buds[index]||!stage.FileStore.CanUse("outdoor"))return;button.interactable=false;stage.Buds[index]=true;GameApp.Instance.Audio.Play("sfx_bloom");}
                    if(isActiveAndEnabled)DrawPlants();
                });
                if(i==stage.BlackFlowerIndex&&stage.Buds[i]&&!stage.BlackTaken&&!stage.Sepia){var key=UIFactory.Sprite(slot,GameApp.Instance.Catalog.sprites.Get("black_key"),50+i*130,5,38,55);key.name="검은 열쇠";key.raycastTarget=true;key.gameObject.AddComponent<Button>().onClick.AddListener(()=>{stage.TakeBlack();DrawPlants();});}
            }
        }
    }
    public sealed class PlantGrowth:MonoBehaviour
    {
        IEnumerator Start(){for(float t=0;t<.55f;t+=Time.unscaledDeltaTime){transform.localScale=new Vector3(1,Mathf.SmoothStep(.05f,1,t/.55f),1);yield return null;}transform.localScale=Vector3.one;}
    }
}
