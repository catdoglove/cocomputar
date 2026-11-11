using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace CoComputar
{
    public sealed partial class Stage05Controller
    {
        public void OpenRadio(){if(FakeWindow.TryCreate(app.Windows,"s5:radio","radio.exe",out var w,215,155,600,440)){w.gameObject.AddComponent<FileOwner>().Id="radio";w.gameObject.AddComponent<RadioAudioMixer>().Setup(this,w);}}
        public void OpenPhoto(){if(FakeWindow.TryCreate(app.Windows,"s5:photoeditor","photo_editor.exe",out var w,145,115,820,540)){w.gameObject.AddComponent<FileOwner>().Id="photo";w.gameObject.AddComponent<PhotoCorrectionRenderer>().Setup(this,w);}}
        public void OpenPlaylist(){if(FakeWindow.TryCreate(app.Windows,"s5:playlist","soundtest.exe",out var w,300,60,430,660)){w.gameObject.AddComponent<FileOwner>().Id="sound";w.gameObject.AddComponent<PlaylistView>().Setup(this,w);}}
    }
    public sealed class RadioAudioMixer:MonoBehaviour
    {
        Stage05Controller stage;FakeWindow window;AudioSource noise,music;AudioLowPassFilter filter;Slider slider;Button minus,plus;TMP_Text number;float held;int frequency;bool cleaned;
        public Color NumberColor=>number.color;public int Frequency=>frequency;public float Held=>held;
        public void Setup(Stage05Controller owner,FakeWindow w)
        {
            stage=owner;window=w;frequency=owner.RadioSolved?owner.Randoms.Frequency:(owner.Randoms.Frequency+37)%101;
            var body=UIFactory.Panel(w.Content,"라디오",15,15,550,335,new Color(.74f,.73f,.66f));
            var speaker=UIFactory.Panel(body.transform,"스피커",25,30,165,175,new Color(.12f,.14f,.15f));for(int i=0;i<12;i++)UIFactory.Panel(speaker.transform,"그릴",5,i*14,155,3,new Color(.6f,.62f,.6f));
            for(int i=0;i<38;i++)UIFactory.Panel(body.transform,"잡음 점",220+(i*47)%290,25+(i*31)%170,3+i%4,3,Color.black);
            number=UIFactory.Text(body.transform,"",250,205,150,45,32,Color.black);number.alignment=TextAlignmentOptions.Center;
            minus=UIFactory.Button(body.transform,"◀",20,272,50,35,()=>SetFrequency(frequency-1));plus=UIFactory.Button(body.transform,"▶",475,272,50,35,()=>SetFrequency(frequency+1));
            slider=UIFactory.Slider(body.transform,145,275,frequency/100f,v=>{if(noise)SetFrequency(Mathf.RoundToInt(v));},false);slider.maxValue=100;slider.wholeNumbers=true;slider.SetValueWithoutNotify(frequency);UIFactory.Text(body.transform,"0",75,280,40,30,18);UIFactory.Text(body.transform,"100",425,280,50,30,18);
            var app=GameApp.Instance;noise=gameObject.AddComponent<AudioSource>();noise.clip=app.Catalog.audio.Get("sfx_radio_noise");noise.outputAudioMixerGroup=app.Catalog.effectsGroup;noise.loop=true;noise.Play();
            var musicObject=new GameObject("라디오 음악");musicObject.transform.SetParent(transform);music=musicObject.AddComponent<AudioSource>();music.clip=app.Catalog.audio.Get("sfx_radio_music");music.outputAudioMixerGroup=app.Catalog.effectsGroup;music.loop=true;music.Play();filter=gameObject.AddComponent<AudioLowPassFilter>();app.Audio.SetMusicFade(0);w.Closed+=Cleanup;Refresh();
        }
        public void SetFrequency(int value){if(stage.RadioSolved||!stage.Ready)return;value=Mathf.Clamp(value,0,100);if(value!=frequency)held=0;frequency=value;slider.SetValueWithoutNotify(value);Refresh();}
        void Refresh()
        {
            float distance=Mathf.Abs(frequency-stage.Randoms.Frequency);float blend=distance<=1?1-distance*.15f:Mathf.Clamp01(stage.Data.musicByDistance.Evaluate(distance));music.volume=blend*.8f;noise.volume=(1-blend)*.65f;noise.pitch=.7f+frequency*.008f;filter.cutoffFrequency=600+frequency*90;
            slider.interactable=minus.interactable=plus.interactable=!stage.RadioSolved;number.text=frequency.ToString();number.color=Color.black;
            if(stage.RadioSolved){noise.Stop();music.volume=.8f;number.rectTransform.anchoredPosition=new Vector2(235,-112);number.color=new Color(.6f,.08f,.3f);}
        }
        void Update()
        {
            if(!stage||!stage.Ready)return;
            if(!stage.RadioSolved)
            {
                if(window.transform.GetSiblingIndex()==window.transform.parent.childCount-1){if(Keyboard.current?.leftArrowKey.wasPressedThisFrame==true)SetFrequency(frequency-1);if(Keyboard.current?.rightArrowKey.wasPressedThisFrame==true)SetFrequency(frequency+1);}
                if(frequency==stage.Randoms.Frequency){held+=Time.unscaledDeltaTime;number.color=Color.Lerp(Color.black,new Color(.75f,.1f,.4f),held/stage.Data.radioHold);if(held>=stage.Data.radioHold){stage.RadioSuccess();Refresh();}}else held=0;
            }
            else number.rectTransform.localScale=Vector3.one*(1+Mathf.Sin(Time.unscaledTime*17)*.025f);
        }
        void Cleanup(){if(cleaned)return;cleaned=true;held=0;if(noise)noise.Stop();if(music)music.Stop();if(stage&&!stage.FinalDone)GameApp.Instance.Audio.SetMusicFade(1);}
        void OnDestroy(){Cleanup();}
    }
}

