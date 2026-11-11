#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        void CheckGalmuri(string label){Check(app.Root.GetComponentsInChildren<TMP_Text>().All(t=>t.font==(app.IsTitleScreen?app.Catalog.theme.titleFont:app.Catalog.theme.font)),label+" 화면별 폰트 연결");}
        IEnumerator FontPreview()
        {
            Check(app.Catalog.theme.font.faceInfo.familyName=="Galmuri11"&&TMP_Settings.defaultFontAsset==app.Catalog.theme.font,"Galmuri11 테마 및 TMP 기본 폰트");
            app.Catalog.theme.font.TryAddCharacters("가나다라마바사아자차카타파하ㄹㅍABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789",out string missing);Check(string.IsNullOrEmpty(missing),"한글·자모·영문·숫자 글리프");
            app.ShowTitle();yield return new WaitForSeconds(.4f);CheckGalmuri("타이틀");Capture("font-title");yield return ClickButton("Start");yield return new WaitForSeconds(.3f);CheckGalmuri("시작 메뉴");Capture("font-start");yield return ClickButton("Option");yield return new WaitForSeconds(.3f);Capture("font-options");
            app.Save.NewGame("Stage05");app.Begin(false);yield return new WaitForSeconds(.4f);var stage=app.ScreenRoot.GetComponentInChildren<Stage05Controller>();stage.OpenFolder("nowof");yield return new WaitForSeconds(.3f);CheckGalmuri("폴더");Capture("font-folder");app.CloseAllWindows();yield return null;
            stage.Open("hint");yield return new WaitForSeconds(.3f);CheckGalmuri("문서");Capture("font-document");app.CloseAllWindows();yield return null;
            stage.Open("photo");yield return new WaitForSeconds(.3f);CheckGalmuri("프로그램");Capture("font-photo");app.CloseAllWindows();yield return null;
            yield return PortraitPreview();
            for(int n=1;n<=5;n++)
            {
                app.Dialogue.CloseCurrent();yield return null;var stageData=app.Catalog.Stage("Stage0"+n);var data=JsonUtility.FromJson<DialogueDocument>(stageData.dialogue.text);
                var line=data.sequences.SelectMany(q=>q.lines).Where(l=>!string.IsNullOrEmpty(l.text)).OrderByDescending(l=>l.text.Length).First();
                var json=new TextAsset(JsonUtility.ToJson(new DialogueDocument{sequences=new[]{new DialogueSequence{id="fontLongest",lines=new[]{line}}}}));app.Dialogue.Load(json);app.Dialogue.Play(()=>{},"fontLongest");yield return new WaitForSeconds(.4f);
                var text=app.Modal.GetComponentsInChildren<TMP_Text>().First(t=>t.rectTransform.rect.width==916&&t.rectTransform.rect.height==102);text.maxVisibleCharacters=99999;text.ForceMeshUpdate();CheckGalmuri("대화 "+n);Capture("font-dialogue-"+n);Check(text.GetPreferredValues(text.text,916,10000).y<=102,"Stage"+n+" 최장 대사 페이지 높이");app.Dialogue.CloseCurrent();Destroy(json);yield return null;
            }
        }
    }
}
#endif
