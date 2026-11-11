#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using UnityEngine;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator PortraitPreview()
        {
            app.Save.NewGame("Stage05");app.Begin(false);yield return new WaitForSeconds(.3f);
            foreach(var key in new[]{"v","folder","twin_black","twin_white","geurim","geurim_young","memo","speaker"})
            {
                var s=app.Catalog.sprites.Get("portrait_"+key);Check(s&&s.texture.width>=500&&s.rect.width==s.texture.width&&s.rect.height==s.texture.height,"CG 전체 프레임 연결 "+key);
            }
            string[] speakers={"폴더","흑","그림","어린그림","메모","스피커"};
            for(int i=0;i<speakers.Length;i++)
            {
                app.Dialogue.CloseCurrent();yield return null;
                var lines=new[]{new DialogueLine{speaker=speakers[i],text="..."},new DialogueLine{speaker=i==1?"백":"V양",text="..."}};
                var json=new TextAsset(JsonUtility.ToJson(new DialogueDocument{sequences=new[]{new DialogueSequence{id="portraitPreview",lines=lines}}}));
                app.Dialogue.Load(json);app.Dialogue.Play(()=>{},"portraitPreview");yield return new WaitForSeconds(.6f);Capture("cg-"+i);app.Dialogue.CloseCurrent();Destroy(json);yield return null;
            }
        }
    }
}
#endif
