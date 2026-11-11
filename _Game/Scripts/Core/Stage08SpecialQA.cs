#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace CoComputar
{
 public sealed partial class PlaythroughQA
 {
  IEnumerator SkipSpecial()
  {
   app.Dialogue.debugSkipOverride=DialogueSystem.SkipOverride.ForceOn;
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftCtrl));
   float until=Time.realtimeSinceStartup+35;
   do{yield return null;}while(app.Dialogue.Active&&Time.realtimeSinceStartup<until);
   InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
   Check(!app.Dialogue.Active,"특수 대화 정상 종료");
  }
  IEnumerator SpecialChecks()
  {
   app.Save.NewGame("Stage08");app.Save.Flag("stage07.secretBrowserInstalled");app.Begin(false);yield return new WaitForSeconds(.5f);
   var s=app.ScreenRoot.GetComponentInChildren<Stage08Controller>();var route=s.Special;
   for(int i=0;i<100;i++){string pw=route.Rules.GeneratePassword();Check(pw.Length>=9&&pw.Length<=11&&pw.Any(char.IsDigit)&&pw.Any(char.IsLetter)&&pw.All(c=>(route.Rules.passwordLetters+route.Rules.passwordDigits).Contains(c)),"비밀번호 규칙 "+i);}
   foreach(var key in new[]{"bgm_special_stage1","bgm_special_stage9","sfx_lock_denied","sfx_npc_vanish","sfx_file_appear","sfx_login_fail","sfx_warp_install","sfx_fake_error","sfx_gameover","sfx_head_split","sfx_key_drop","sfx_forbidden_folder","sfx_deep_enter"})Check(app.Catalog.audio.Get(key)!=null,"특수 오디오 등록 "+key);
   Check(!route.Login("admin89",""),"계정 생성 전 로그인 불가");
   s.Open("your");yield return null;var browser=app.Windows.GetComponentInChildren<FakeBrowserWindow>();browser.Navigate("hospital/about");yield return new WaitForSeconds(.2f);
   Check(browser.Window.GetComponentsInChildren<TMPro.TMP_InputField>().Length==2,"ABOUT 입력 필드");
   Check(!app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.Key=="s8:hospital-notice"),"ABOUT 병원 팝업 중복 없음");Capture("special-login");app.CloseAllWindows();yield return null;
   s.Grant("head");s.Grant("body");s.Merge("head","body");yield return new WaitForSeconds(1);
   route.Talk();yield return SkipSpecial();Check(route.Progress.state==SpecialRouteState.Talked1,"첫 대화 완료");
   route.Talk();yield return SkipSpecial();Check(route.Progress.state==SpecialRouteState.Offered&&s.Busy,"예 아니오 선택 잠금");
   var no=app.Modal.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="아니오");no.onClick.Invoke();yield return SkipSpecial();yield return new WaitForSeconds(1);
   Check(route.Progress.state==SpecialRouteState.FakeDoorGiven&&s.Files.Find("deep:fake")!=null,"아니오 가짜 문 생성");
   s.Open("deep:fake");yield return new WaitForSeconds(3);Check(route.Progress.state==SpecialRouteState.GameOver,"가짜 문 게임오버");Capture("special-gameover");
   var retry=app.Modal.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="다시 시도");retry.onClick.Invoke();yield return SkipSpecial();
   Check(s.Files.Find("deep:fake")==null,"재시도 가짜 문 정리");
   var yes=app.Modal.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="예");yes.onClick.Invoke();yield return SkipSpecial();
   string savedPassword=route.Progress.adminPw;Check(s.Files.Find("deep:account")?.content.Contains(savedPassword)==true,"계정 파일 생성");
   Check(!route.Login("admin89",savedPassword.ToUpperInvariant()),"비밀번호 대소문자 구분");Check(route.Login("admin89",savedPassword),"정확한 관리자 로그인");
   route.Save();app.Save.ResetStagePuzzles("Stage08");app.Begin(false);yield return new WaitForSeconds(.5f);s=app.ScreenRoot.GetComponentInChildren<Stage08Controller>();route=s.Special;
   Check(route.Progress.adminLoggedIn&&route.Progress.adminPw==savedPassword&&s.Files.Find("deep:account")!=null,"이어하기 계정 로그인 파일 보존");
   SetupWizardWindow.OpenSpecial(route);yield return null;var wizard=app.Windows.GetComponentInChildren<SetupWizardWindow>();wizard.Next();wizard.Next();Check(wizard.Step==2,"미동의 진행 차단");wizard.Agree=true;wizard.Next();Check(wizard.Step==3,"별도 경고2");wizard.Next();Check(wizard.Step==4,"별도 경고3");Capture("special-warning");wizard.Next();yield return new WaitForSeconds(route.Rules.installSeconds+.3f);
   Check(wizard.Step==6&&route.Locked&&s.Files.Find("deep:warp")!=null,"설치 완료 순간 워프 생성 잠금");
   app.OpenComputer();Check(app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.Key=="deep:locked"),"공통 내컴퓨터 잠금");wizard.Next();yield return new WaitForSeconds(.3f);
   route.Talk();yield return SkipSpecial();yield return new WaitForSeconds(3);
   Check(route.Progress.state==SpecialRouteState.StageReturn1,"역행CMD 복제장면 진입");
   var folder=app.ScreenRoot.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="deep:folder");folder.Clicked();yield return new WaitForSeconds(.3f);Capture("special-reunion");
   var portraits=new[]{app.Catalog.sprites.Get("portrait_v"),app.Catalog.sprites.Get("portrait_folder"),app.Catalog.sprites.Get("portrait_npc_stage8")};
   var cast=app.Modal.GetComponentsInChildren<Image>().Where(i=>i.sprite&&portraits.Contains(i.sprite)).ToArray();Check(cast.Length>=3,"재회 CG 세 명 동시 표시");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftCtrl));
   float transitionDeadline=Time.realtimeSinceStartup+50;
   while(route.Progress.state!=SpecialRouteState.Stage9Replica&&Time.realtimeSinceStartup<transitionDeadline)yield return null;
   InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return new WaitForSeconds(.8f);
   Check(route.Progress.state==SpecialRouteState.Stage9Replica,"폴더 소멸 대화 후 Stage9 복제: "+route.Progress.state);
   route.Save();app.Begin(false);yield return new WaitForSeconds(.5f);s=app.ScreenRoot.GetComponentInChildren<Stage08Controller>();route=s.Special;Check(route.Progress.state==SpecialRouteState.Stage9Replica,"Stage9 체크포인트 복원");
   var keyNpc=app.ScreenRoot.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="deep:keynpc");keyNpc.Clicked();yield return SkipSpecial();yield return new WaitForSeconds(6);
   Check(route.Progress.state==SpecialRouteState.KeyObtained,"커서 도달 및 열쇠 획득");
   app.ScreenRoot.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="deep:key").Clicked();yield return SkipSpecial();yield return new WaitForSeconds(2);
   Check(route.Progress.state==SpecialRouteState.ForbiddenFolderShown,"검은 금지 폴더 생성");Capture("special-forbidden");
   app.ScreenRoot.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="deep:forbidden").Clicked();yield return SkipSpecial();yield return new WaitForSeconds(28);
   Check(app.Save.Unlocks.stage08EndingDeep&&app.Save.CanSkipDialogue(),"특수 엔딩 영구 저장과 스킵 해금");
   app.Save.NewGame("Stage08");Check(app.Save.Progress.stage08Special==null||app.Save.Progress.stage08Special.state==SpecialRouteState.NotStarted,"새 게임 특수 루트 초기화");Check(app.Save.Unlocks.stage08EndingDeep,"새 게임 엔딩 기록 보존");
  }
 }
}
#endif
