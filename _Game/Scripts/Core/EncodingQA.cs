#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator EncodingChecks()
        {
            app.Save.NewGame("Stage04");app.Begin(false);yield return new WaitForSeconds(1.3f);yield return Hold(UnityEngine.InputSystem.Key.LeftCtrl,8);
            var stage=app.ScreenRoot.GetComponentInChildren<Stage04Controller>();var file=stage.Files.Find("to_document");stage.Open("to_document");yield return new WaitForSeconds(.3f);
            Check(file.encoding==NotepadEncodings.Default,"최초 문서 인코딩 ANSI 기본값");var variants=new HashSet<string>();
            foreach(var option in NotepadEncodings.Options.Where(o=>o.Id!=NotepadEncodings.Correct).Concat(NotepadEncodings.Options.Where(o=>o.Id==NotepadEncodings.Correct)))
            {
                var note=app.Windows.GetComponentInChildren<NotepadWindow>();note.SaveDialog();yield return new WaitForSeconds(.3f);
                var save=app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="s4:save:"+file.id);
                var selector=save.Content.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<TMP_Text>().text.EndsWith(" ▼"));yield return MouseClick((RectTransform)selector.transform);yield return null;
                var panel=(RectTransform)save.Content.Find("인코딩 선택");var buttons=panel.GetComponentsInChildren<Button>();
                Check(buttons.Select(b=>b.name).SequenceEqual(new[]{"ANSI","UTF-2","UTF-4","UTF-8","UTF-16","UTF-32"}),"6개 항목 배열 순서: "+option.Label);
                Check(-panel.anchoredPosition.y>=0&&-panel.anchoredPosition.y+panel.rect.height<=save.Content.rect.height,"6개 행 모두 내용 영역 내부: "+option.Label);
                if(option.Id=="ANSI")Capture("stage04-encoding-six");
                yield return MouseClick((RectTransform)buttons.Single(b=>b.name==option.Label).transform);yield return S4Click("Save");yield return S4Click("확인");
                Check(file.encoding==option.Id,"실제 선택 저장 ID: "+option.Label);
                note=NotepadWindow.Open(stage,file,true);yield return null;
                if(option.Id==NotepadEncodings.Correct)Check(note.Body.text==file.content&&stage.Puzzle1,"네 번째 UTF-8만 정상 본문·퍼즐 정답");
                else{Check(note.Body.text!=file.content&&!note.Body.text.Contains(stage.Password)&&!stage.Puzzle1,"오답 외계어·암호 은닉: "+option.Label);variants.Add(note.Body.text);}
            }
            Check(variants.Count==5,"오답 5개 인코딩 각각 다른 외계어 본문");
            app.Begin(false);yield return new WaitForSeconds(1.3f);stage=app.ScreenRoot.GetComponentInChildren<Stage04Controller>();Check(stage.Files.Find("to_document").encoding==NotepadEncodings.Default&&!stage.Puzzle1,"이어하기 초기 인코딩·퍼즐 복원");
        }
    }
}
#endif
