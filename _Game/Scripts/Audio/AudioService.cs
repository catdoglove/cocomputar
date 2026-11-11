using UnityEngine;
namespace CoComputar
{
    public sealed class AudioService : MonoBehaviour
    {
        AudioSource music, effects, puzzleEffects,dateEffects;
        string puzzleKey;bool eventMusicPaused;readonly System.Collections.Generic.List<AudioSource> pausedMusic=new();
        GameCatalog catalog;public bool BgmMuted{get;private set;}public event System.Action<bool> BgmMutedChanged;
        public void Setup(GameCatalog data)
        {
            catalog = data;
            music = gameObject.AddComponent<AudioSource>(); music.loop = true;
            effects = gameObject.AddComponent<AudioSource>();
            dateEffects=gameObject.AddComponent<AudioSource>();dateEffects.playOnAwake=false;dateEffects.outputAudioMixerGroup=data.effectsGroup;
            puzzleEffects=gameObject.AddComponent<AudioSource>();puzzleEffects.playOnAwake=false;
            puzzleEffects.outputAudioMixerGroup=data.effectsGroup;
            music.outputAudioMixerGroup = data.musicGroup; effects.outputAudioMixerGroup = data.effectsGroup;
        }
        public void Apply(OptionsData data)
        {
            bool muted=data.music<=0;if(muted!=BgmMuted){BgmMuted=muted;BgmMutedChanged?.Invoke(muted);}
            if (catalog.mixer)
            {
                catalog.mixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(.0001f, data.music)) * 20);
                catalog.mixer.SetFloat("EffectsVolume", Mathf.Log10(Mathf.Max(.0001f, data.effects)) * 20);
            }
            else { music.volume = data.music; effects.volume = puzzleEffects.volume = dateEffects.volume = data.effects; }
        }
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        public event System.Action<string> Played;
        #endif
        public void Play(string key)
        {
            var clip=catalog.audio.Get(key);if(!clip)return;if(key=="sfx_sepia_on")dateEffects.PlayOneShot(clip);else effects.PlayOneShot(clip);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Played?.Invoke(key);
            #endif
        }
        public void PlayCapped(string key,float cap)
        {
            var clip=catalog.audio.Get(key);if(!clip)return;
            effects.PlayOneShot(clip,Mathf.Clamp01(cap));
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Played?.Invoke(key);
            #endif
        }
        public void PlayDateEffect(string key)
        {
            var clip=catalog.audio.Get(key);if(!clip)return;
            dateEffects.Stop();dateEffects.clip=clip;dateEffects.loop=false;dateEffects.Play();
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Played?.Invoke(key);
            #endif
        }
        public bool DateEffectPlaying=>dateEffects.isPlaying;
        public void StopDateEffect()=>dateEffects.Stop();
        public bool EffectsPlaying=>effects.isPlaying||puzzleEffects.isPlaying||dateEffects.isPlaying;
        public void StopEffects(){dateEffects.Stop();effects.Stop();puzzleEffects.Stop();puzzleEffects.loop=false;puzzleKey=null;}
        public bool MusicPlaying=>music.isPlaying;
        public void PauseEventMusic()
        {
            if(eventMusicPaused)return;eventMusicPaused=true;pausedMusic.Clear();
            foreach(var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if(source!=effects&&source!=puzzleEffects&&source!=dateEffects&&source.isPlaying&&(source==music||source.outputAudioMixerGroup==catalog.musicGroup||source.transform.IsChildOf(GameApp.Instance.Windows)))
                {source.Pause();pausedMusic.Add(source);}
        }
        public void ResumeEventMusic()
        {
            foreach(var source in pausedMusic)if(source&&source.gameObject.activeInHierarchy)source.UnPause();
            pausedMusic.Clear();eventMusicPaused=false;
        }
        public void SetMusicFade(float value){if(!music)return;music.volume=Mathf.Clamp01(value)*(catalog.mixer?1:GameApp.Instance.Save.Options.music);}
        public void Music(string key) {SetMusicFade(1);eventMusicPaused=false;pausedMusic.Clear(); music.clip = catalog.audio.Get(key); if (music.clip) music.Play(); }
        // 반복 실패는 겹치지 않고 성공은 실패음을 교체하는 전용 단일 채널.
        public bool PlayPuzzle(string key,bool loop=false,bool replace=false)
        {
            if(puzzleEffects.isPlaying&&!replace)return false;
            var clip=catalog.audio.Get(key);if(!clip)return false;
            puzzleEffects.Stop();puzzleKey=key;puzzleEffects.clip=clip;puzzleEffects.loop=loop;puzzleEffects.Play();
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Played?.Invoke(key);
            #endif
            return true;
        }
        public bool PuzzlePlaying(string key)=>puzzleKey==key&&puzzleEffects.isPlaying;
        public void StopPuzzle(string key){if(puzzleKey==key){puzzleEffects.Stop();puzzleEffects.loop=false;puzzleKey=null;}}
    }
}
