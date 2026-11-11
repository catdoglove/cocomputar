using System.Collections;
using UnityEngine;
namespace CoComputar
{
    public sealed class StoryEffects : MonoBehaviour
    {
        public static StoryEffects Get()=>GameApp.Instance.GetComponent<StoryEffects>()??GameApp.Instance.gameObject.AddComponent<StoryEffects>();
        public void Glitch(float seconds){StartCoroutine(GlitchRoutine(seconds));}
        IEnumerator GlitchRoutine(float seconds){var c=GameApp.Instance.Catalog.crt;c.glitchAmount=1;GameApp.Instance.Audio.Play("sfx_glitch");yield return new WaitForSecondsRealtime(seconds);c.glitchAmount=0;}
        public void Sepia(bool on,string sound="sfx_sepia_on"){StopCoroutine(nameof(Fade));if(on){GameApp.Instance.Catalog.crt.sepiaAmount=1;GameApp.Instance.Catalog.crt.UpdateStory();GameApp.Instance.Audio.PlayDateEffect(sound);StartCoroutine(Flash());}else StartCoroutine(nameof(Fade));}
        IEnumerator Flash(){var p=UIFactory.Panel(GameApp.Instance.Modal,"섬광",0,0,1024,768,new Color(1,1,.7f,.65f));yield return new WaitForSecondsRealtime(.08f);Destroy(p.gameObject);}
        IEnumerator Fade(){var c=GameApp.Instance.Catalog.crt;while(c.sepiaAmount>0){c.sepiaAmount=Mathf.Max(0,c.sepiaAmount-Time.unscaledDeltaTime);yield return null;}}
        void Update(){var c=GameApp.Instance.Catalog.crt;c.UpdateStory();}
        public void ResetEffects(){StopAllCoroutines();var c=GameApp.Instance.Catalog.crt;c.sepiaAmount=c.glitchAmount=0;c.UpdateStory();}
    }
}
