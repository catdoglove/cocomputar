using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed class PhotoCorrectionRenderer:MonoBehaviour
    {
        Stage05Controller stage;FakeWindow window;Image picture,overlay;Button reward;Material material;Slider[] sliders=new Slider[3];TMP_Text title;string loaded;float held;bool cleaned;FakeWindow picker;
        public string Loaded=>loaded;
        public float RewardAlpha=>overlay.color.a;
        public void Setup(Stage05Controller owner,FakeWindow w)
        {
            stage=owner;window=w;stage.PhotoFile=null;stage.PhotoValues=Vector3.zero;w.Closed+=Cleanup;UIFactory.Button(w.Toolbar,"열기",10,5,90,32,OpenPicker);UIFactory.Button(w.Toolbar,"복원",110,5,90,32,()=>SetValues(0,0,0));UIFactory.Button(w.Toolbar,"도움말",210,5,100,32,()=>FileUI.Notice("사진을 열거나 이 창으로 끌어오세요.\n오른쪽 막대로 사진을 보정합니다.",sound:false));
            var area=UIFactory.Panel(w.Content,"사진 영역",12,52,475,400,new Color(.22f,.22f,.24f));picture=UIFactory.Sprite(area.transform,null,8,8,459,350);picture.gameObject.SetActive(false);title=UIFactory.Text(area.transform,"사진 열기",12,362,445,30,20,Color.white);
            overlay=UIFactory.Sprite(area.transform,GameApp.Instance.Catalog.sprites.Get("s5_half_a"),200,160,75,75);overlay.gameObject.name="사진 보상";overlay.color=Color.clear;overlay.raycastTarget=false;reward=overlay.gameObject.AddComponent<Button>();reward.transition=Selectable.Transition.None;reward.onClick.AddListener(CollectReward);
            var drop=w.Content.gameObject.AddComponent<DropTarget>();drop.Category="s5:photoeditor";drop.IsFolderWindow=true;
            var config=stage.Data.photo;
            string[] names={"밝기","채도","색상"};for(int i=0;i<3;i++)
            {
                UIFactory.Text(w.Content,names[i],520,80+i*115,240,30,23);var label=UIFactory.Text(w.Content,"0",700,80+i*115,80,30,23);
                var slider=UIFactory.Slider(w.Content,520,120+i*115,0,v=>{label.text=Mathf.RoundToInt(v).ToString();held=0;Apply();GameApp.Instance.Audio.PlayPuzzle("sfx_photo_adjust_tick");});
                sliders[i]=slider;slider.minValue=config.sliderMin;slider.maxValue=config.sliderMax;slider.wholeNumbers=true;slider.SetValueWithoutNotify(0);
                for(int tick=config.sliderMin;tick<=config.sliderMax;tick++){var mark=UIFactory.Panel(slider.transform,"눈금",8+214f*(tick-config.sliderMin)/(config.sliderMax-config.sliderMin),31,1,tick%5==0?6:3,new Color(.25f,.25f,.3f));mark.raycastTarget=false;}
                UIFactory.Text(w.Content,config.sliderMin.ToString(),520,155+i*115,45,22,16);UIFactory.Text(w.Content,config.sliderMax.ToString(),730,155+i*115,45,22,16);
            }
            material=new Material(Resources.Load<Shader>("PhotoCorrection"));picture.material=material;Apply();

        }
        public void OpenPicker()
        {
            if(!stage.Ready)return;if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"s5:photopicker","사진 열기",out var w,180,90,760,580))return;picker=w;w.SetFileBackground();
            var viewport=UIFactory.Panel(w.Content,"사진 목록",0,0,740,520,Color.white);viewport.gameObject.AddComponent<RectMask2D>();var nodes=stage.Files.Nodes.Where(n=>n.contentType=="image"&&stage.Files.CanUse(n)&&stage.IsFileVisible(n)).ToArray();var content=UIFactory.Rect(viewport.transform,"사진",0,0,720,Mathf.Ceil(nodes.Length/5f)*100);var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport.rectTransform;scroll.content=content;scroll.horizontal=false;scroll.scrollSensitivity=40;scroll.movementType=ScrollRect.MovementType.Clamped;
            for(int i=0;i<nodes.Length;i++){var node=nodes[i];FakeIcon.Create(content,node.id,node.displayName,"picture",8+i%5*140,i/5*100,()=>{Load(node.id);w.Close();window.Focus();});}
        }
        public bool Load(string id)
        {
            var n=stage.Files.Find(id);if(!stage.Ready||n==null||n.contentType!="image"||!stage.Files.CanUse(n)||!stage.IsFileVisible(n))return false;
            loaded=id;stage.PhotoFile=id;held=0;picture.gameObject.SetActive(true);picture.sprite=GameApp.Instance.Catalog.sprites.Get(n.imageKey);title.text=n.displayName;SetValues(0,0,0);return true;
        }
        public void SetValues(float brightness,float color,float saturation){sliders[0].value=brightness;sliders[1].value=color;sliders[2].value=saturation;Apply();}
        bool CorrectPhoto=>!cleaned&&stage&&loaded=="photo"+stage.Randoms.Frequency&&stage.RadioSolved&&stage.Files.CanUse(stage.Files.Find(loaded));
        int MatchedCount()
        {
            if(!CorrectPhoto)return 0;int count=0;
            for(int i=0;i<3;i++)if(Mathf.Abs(sliders[i].value-stage.Randoms.PhotoTarget[i])<=stage.Data.photo.tolerance)count++;
            return count;
        }
        void Apply()
        {
            if(!material||cleaned)return;stage.PhotoValues=new Vector3(sliders[0].value,sliders[1].value,sliders[2].value);
            var d=(stage.PhotoValues-stage.Randoms.PhotoTarget)/Mathf.Max(1,stage.Data.photo.sliderMax-stage.Data.photo.sliderMin);
            material.SetFloat("_Brightness",d.x);material.SetFloat("_Clarity",d.y);material.SetFloat("_Saturation",d.z);RefreshReward();
        }
        void Update()
        {
            if(cleaned||!stage||!stage.Ready)return;
            if(!stage.PhotoSolved&&MatchedCount()==3){held+=Time.unscaledDeltaTime;if(held>=stage.Data.photo.hold)stage.PhotoSuccess();}else held=0;
            if(!stage.PhotoRewardClaimed)RefreshReward();
        }
        void RefreshReward()
        {
            bool pending=CorrectPhoto&&stage.PhotoSolved&&!stage.PhotoRewardClaimed;
            int count=MatchedCount();float alpha=count==0?0:count==1?.3f:count==2?.7f:1;
            overlay.color=new Color(1,1,1,stage.PhotoRewardClaimed?0:pending?1:alpha);
            overlay.raycastTarget=pending;reward.interactable=pending;
        }
        public void CollectReward()
        {
            if(cleaned||!reward.interactable||!CorrectPhoto||!stage.ClaimPhotoReward())return;
            overlay.raycastTarget=false;reward.interactable=false;StartCoroutine(FadeReward());
        }
        IEnumerator FadeReward()
        {
            for(float t=0;t<.2f;t+=Time.unscaledDeltaTime){overlay.color=new Color(1,1,1,1-t/.2f);yield return null;}
            overlay.color=Color.clear;
        }
        // 모든 정상 닫기 경로가 호출하며, 화면 전환으로 직접 파괴될 때도 같은 정리를 수행한다.
        void Cleanup()
        {
            if(cleaned)return;cleaned=true;StopAllCoroutines();held=0;loaded=null;
            if(picker)picker.Close();picker=null;
            if(stage&&stage.gameObject.activeInHierarchy){stage.PhotoFile=null;stage.PhotoValues=Vector3.zero;}
            foreach(var slider in sliders)if(slider)slider.SetValueWithoutNotify(0);
            if(picture){picture.sprite=null;picture.material=null;picture.gameObject.SetActive(false);}
            if(overlay){overlay.color=Color.clear;overlay.raycastTarget=false;}
            if(reward)reward.interactable=false;
            if(material){Destroy(material);material=null;}
        }
        void OnDestroy(){Cleanup();}
    }
}
