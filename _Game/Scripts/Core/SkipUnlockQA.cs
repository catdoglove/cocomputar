#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator SkipUnlockChecks()
        {
            app.Save.Unlocks.skipUnlocked=false;app.Save.Unlocks.gameCleared=false;app.Begin(true);yield return null;
            Check(!app.Dialogue.CanSkipDialogue(),"최초 새 게임 Ctrl 스킵 잠금");
            var fixture=new TextAsset(JsonUtility.ToJson(new DialogueDocument{sequences=new[]{new DialogueSequence{id="LOCK",lines=Enumerable.Range(0,3).Select(i=>new DialogueLine{speaker="V양",text=new string('가',120)}).ToArray()}}}));
            app.Dialogue.Load(fixture);app.Dialogue.Play(()=>{},"LOCK");yield return Hold(Key.LeftCtrl,.4f);
            var body=app.Modal.GetComponentsInChildren<TMPro.TMP_Text>().First(t=>t.text.Length==120);
            Check(app.Dialogue.Active&&body.maxVisibleCharacters<60,"잠금 중 Ctrl 홀드해도 정상 타이핑 속도");yield return Hold(Key.RightCtrl,.4f);Check(app.Dialogue.Active&&body.maxVisibleCharacters<100,"오른쪽 Ctrl도 스킵 차단");
            app.Dialogue.debugSkipOverride=DialogueSystem.SkipOverride.ForceOn;yield return Hold(Key.LeftCtrl,1);Check(!app.Dialogue.Active&&!app.Save.CanSkipDialogue(),"디버그 강제 ON·영구 상태 미변경");app.Dialogue.debugSkipOverride=DialogueSystem.SkipOverride.Default;
            app.Save.Enter("Stage03");app.Save.Flag("stage03.p1Talk");app.Save.Flag("stage02.keep");app.Begin(false);yield return null;
            Check(app.Dialogue.CanSkipDialogue()&&new SaveService().Unlocks.skipUnlocked&&!app.Save.Unlocks.gameCleared,"이어하기 즉시 영구 해금·재로드·엔딩 미클리어");Check(!app.Save.Progress.flags.Contains("stage03.p1Talk")&&app.Save.Progress.flags.Contains("stage02.keep"),"이어하기 퍼즐 초기화·다른 기록 보존");
            app.Dialogue.Load(fixture);app.Dialogue.Play(()=>{},"LOCK");yield return Hold(Key.RightCtrl,1);Check(!app.Dialogue.Active,"해금 후 오른쪽 Ctrl 빠른 종료");
            app.Dialogue.debugSkipOverride=DialogueSystem.SkipOverride.ForceOff;app.Dialogue.Play(()=>{},"LOCK");yield return Hold(Key.LeftCtrl,.4f);Check(app.Dialogue.Active&&app.Save.CanSkipDialogue(),"디버그 강제 OFF·저장 해금 보존");app.Dialogue.Cancel();app.Dialogue.debugSkipOverride=DialogueSystem.SkipOverride.Default;
            app.Begin(true);yield return null;Check(app.Dialogue.CanSkipDialogue()&&new SaveService().Unlocks.skipUnlocked,"새 게임 덮어쓰기 후 영구 해금 유지");
            app.Save.SetGameCleared();app.Save.NewGame("Stage03");app.Save.ResetStagePuzzles("Stage03");Check(new SaveService().Unlocks.gameCleared&&app.Save.CanSkipDialogue(),"엔딩 연결 함수·새 게임/퍼즐 초기화 후 영구 유지");
            app.Save.Unlocks.skipUnlocked=false;Check(app.Save.CanSkipDialogue(),"gameCleared 단독으로도 스킵 허용");
            string legacyPath=Path.Combine(Application.temporaryCachePath,"SkipLegacyQA-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(legacyPath);File.WriteAllText(Path.Combine(legacyPath,"progress.json"),"{\"stageId\":\"Stage03\"}");var legacy=new SaveService(legacyPath);Check(!legacy.CanSkipDialogue(),"구버전 누락 키 기본값 false");legacy.UnlockDialogueSkip();legacy.ResetStagePuzzles("Stage03");Check(new SaveService(legacyPath).CanSkipDialogue(),"구버전 이어하기 해금 저장 정상");
            app.Save.Enter("Stage03");app.Begin(false);yield return null;yield return CtrlChecks();
        }
    }
}
#endif
