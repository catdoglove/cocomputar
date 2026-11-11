#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator Stage07Checks(bool preserveFlow=false)
        {
            var data=Stage07Config.Load();
            for(int seed=0;seed<100;seed++)
            {
                var r=new Stage07Randomizer(data,seed);bool reachable=true;
                foreach(var l in r.Letters)reachable&=Enumerable.Range(0,36).Any(p=>r.Visible(l.Shape,p%6,p/6,data.visibleDistance).Contains(l));
                Check(r.Letters.Count==26&&r.Letters.Select(l=>l.Letter).Distinct().Count()==26&&r.Numbers.Values.Distinct().Count()==6&&r.Numbers.Values.All(v=>v.Length==3)&&reachable&&Enumerable.Range(0,3).All(s=>r.Letters.Count(l=>l.Shape==s)>=8&&r.Letters.Count(l=>l.Shape==s&&l.Color=="blue")==1&&r.Letters.Count(l=>l.Shape==s&&l.Color=="green")==1&&!r.Visible(s,0,0,1).Any(l=>l.Color=="blue"||l.Color=="green")),"Stage07 랜덤·가시성 100회 "+seed);
            }
            for(int mask=0;mask<32;mask++){var choices=data.components.Where((c,i)=>(mask&(1<<i))!=0).Select(c=>c.id).ToArray();bool correct=choices.OrderBy(x=>x).SequenceEqual(data.components.Where(c=>c.required).Select(c=>c.id).OrderBy(x=>x));Check((data.ComponentError(choices)==null)==correct,"지도 구성 요소 32조합 "+mask);}
            Check(SerialDialog.Normalize("ab$c12-3") == "ABC-123"&&SerialDialog.Normalize("abcdef123456")=="ABC-123","시리얼 자동 대문자·하이픈·길이 제한");
            if(!preserveFlow){app.Save.NewGame("Stage07");app.Begin(false);}yield return new WaitForSeconds(.4f);app.Save.UnlockDialogueSkip();
            var stage=app.ScreenRoot.GetComponentInChildren<Stage07Controller>();Check(stage&&app.Stages.Current.stageId=="Stage07","Stage07 진입");Capture("stage07-desktop");
            stage.Talk();yield return new WaitForSeconds(.5f);Capture("stage07-dialogue");yield return FinishDialogue();stage.Talk();Check(app.Dialogue.CurrentSequence=="S7_TALK_2","첫 대화 후 반복 분기");yield return FinishDialogue();
            stage.StartInstaller("viewer");yield return new WaitForSeconds(.25f);var wizard=app.Windows.GetComponentInChildren<SetupWizardWindow>();Capture("stage07-setup-start");stage.StartInstaller("map");Check(app.Windows.GetComponentsInChildren<SetupWizardWindow>().Length==1&&!app.Windows.GetComponentInChildren<SerialDialog>(),"마법사 하나만 실행");
            wizard.Cancel();yield return null;var cancel=app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="s7:cancel");cancel.GetComponentsInChildren<Button>().Single(b=>b.name=="예").onClick.Invoke();yield return null;Check(!stage.Installs.IsInstalled("viewer")&&!app.Windows.GetComponentInChildren<SetupWizardWindow>(),"취소 미설치·재시도 가능");
            stage.StartInstaller("viewer");yield return new WaitForSeconds(.2f);wizard=app.Windows.GetComponentInChildren<SetupWizardWindow>();wizard.Next();wizard.Next();Check(wizard.Step==2&&!wizard.Agree,"미동의 기본값·다음 차단");
            yield return MouseClick((RectTransform)wizard.GetComponentsInChildren<Button>().Single(b=>b.name.Contains("사용권 계약에 동의합니다")).transform);wizard.Next();Check(wizard.Step==3,"동의 후 설정 진입");Capture("stage07-setup-settings");
            stage.Talk();Check(app.Dialogue.CurrentSequence=="S7_TALK_3"&&wizard&&wizard.Step==3,"대화 시 설치 창·단계 유지");yield return FinishDialogue();Check(app.Windows.GetComponent<CanvasGroup>().interactable,"대화 종료 창 입력 복구");
            wizard.Next();yield return new WaitForSeconds(2);Capture("stage07-installing");yield return new WaitForSeconds(stage.Data.installSeconds);Check(wizard.Step==5&&stage.Installs.IsInstalled("viewer"),"7초 설치 완료 화면·프로그램 생성");Capture("stage07-installed");wizard.Cancel();yield return null;Check(stage.Installs.IsInstalled("viewer"),"완료 화면 X 설치 유지");
            stage.StartInstaller("viewer");Check(!app.Windows.GetComponentInChildren<SetupWizardWindow>()&&app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.Key=="s7:installed"),"재설치 차단 경고만");app.CloseAllWindows();yield return null;
            stage.Launch("viewer");yield return new WaitForSeconds(.2f);var viewer=app.Windows.GetComponentInChildren<ShapeViewerWindow>();
            for(int i=0;i<3;i++){viewer.Select(i);var letter=stage.Randoms.Letters.Single(l=>l.Shape==i&&l.Color=="blue");viewer.X.value=letter.X;viewer.Y.value=letter.Y;yield return null;Check(viewer.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="letter:"+letter.Letter),"뷰어 파란 글자 실제 표시 "+i);if(i==0)Capture("stage07-viewer");}
            viewer.X.value=0;viewer.Y.value=0;yield return Press(Key.RightArrow);yield return Press(Key.UpArrow);Check(viewer.X.value==1&&viewer.Y.value==1,"뷰어 좌우/상하 방향키 한 칸 조작");viewer.X.value=99;viewer.Y.value=-2;Check(viewer.X.value==5&&viewer.Y.value==0,"뷰어 회전 범위 제한");int savedX=stage.RotationX[0];viewer.Select(0);Check(viewer.X.value==savedX,"도형별 회전 유지");app.CloseAllWindows();yield return null;
            stage.OpenFolder("important");stage.OpenFolder("master");WindowApps.Note("KEY.txt",stage.Randoms.KeyText,"s7:keytext");yield return new WaitForSeconds(.2f);Capture("stage07-key");app.CloseAllWindows();yield return null;
            stage.StartInstaller("map");yield return new WaitForSeconds(.2f);var serial=app.Windows.GetComponentInChildren<SerialDialog>();serial.Input.text="AAA-999";serial.Submit();Check(app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.Key=="s7:serial-error"),"시리얼 오답 재시도");app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="s7:serial-error").Close();serial.Input.text=stage.Randoms.Serial("map");serial.Submit();yield return null;
            wizard=app.Windows.GetComponentInChildren<SetupWizardWindow>();wizard.Next();wizard.Agree=true;wizard.Next();wizard.Next();yield return new WaitForSeconds(5.6f);Check(!stage.Installs.IsInstalled("map")&&wizard.Progress==stage.Data.failPercent,"충돌 설치 지정 퍼센트 정지·미설치");Capture("stage07-install-error");app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="s7:wizard-notice").Close();yield return null;Check(!app.Windows.GetComponentInChildren<SetupWizardWindow>(),"오류 확인 설치창 종료");
            stage.Data.installSeconds=.2f;
            IEnumerator Install(string id,bool secret=false)
            {
                stage.StartInstaller(id);yield return new WaitForSeconds(.2f);var input=app.Windows.GetComponentInChildren<SerialDialog>();if(input){input.Input.text=stage.Randoms.Serial(id);input.Submit();yield return null;}
                var setup=app.Windows.GetComponentInChildren<SetupWizardWindow>();setup.Next();setup.Agree=true;setup.Next();if(id=="map"){
                    Check(!setup.ChoosingComponents&&!setup.GetComponentsInChildren<Button>().Any(b=>b.name.Contains("지면 프로그램")),"지도 설치 설정에 구성 체크 없음");
                    yield return new WaitForSeconds(.2f);yield return MouseClick((RectTransform)setup.GetComponentsInChildren<Button>().Single(b=>b.name.Contains("사용자 지정 설치")).transform);
                    setup.Next();Check(setup.ChoosingComponents&&setup.Step==3&&setup.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="설치 유형"),"사용자 지정 다음은 별도 설치 유형 화면");
                    Capture("stage07-map-components");setup.Previous();Check(!setup.ChoosingComponents&&!setup.DefaultInstall,"구성 화면 이전은 설치 설정·선택 유지");setup.Next();
                    setup.Components.Clear();setup.Next();Check(setup.ChoosingComponents&&setup.Progress==0,"구성 0개 선택 설치 차단");app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="s7:wizard-notice").Close();
                    setup.Components.Clear();foreach(var c in stage.Data.components.Where(c=>c.required))setup.Components.Add(c.id);}setup.SecretOption=secret;setup.RunAfter=false;setup.Next();yield return new WaitForSeconds(.35f);Check(setup.Step==5&&stage.Installs.IsInstalled(id),"설치 성공 "+id);setup.Next();yield return null;
            }
            yield return Install("map");stage.Launch("map");Check(!app.Windows.GetComponentInChildren<Map07Window>()&&app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.Key=="s7:offline"),"인터넷 미설치 지도 실행 차단");app.CloseAllWindows();yield return null;
            stage.StartInstaller("internet");yield return new WaitForSeconds(.2f);serial=app.Windows.GetComponentInChildren<SerialDialog>();serial.Input.text=stage.Randoms.Serial("internet");serial.Submit();yield return null;wizard=app.Windows.GetComponentInChildren<SetupWizardWindow>();wizard.Next();yield return new WaitForSeconds(.25f);
            var contract=wizard.GetComponentInChildren<ScrollRect>();Check(contract.content.rect.height>contract.viewport.rect.height*5,"긴 계약서 실제 스크롤");contract.verticalNormalizedPosition=0;yield return null;yield return null;Capture("stage07-contract-bottom");
            yield return MouseClick((RectTransform)wizard.GetComponentsInChildren<Button>().Single(b=>b.GetComponent<TMP_Text>()?.text=="더보기").transform);yield return null;Check(stage.SecretUnlocked&&wizard.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition>.99f,"더보기 교체 계약서·스크롤 위로");
            contract=wizard.GetComponentInChildren<ScrollRect>();contract.verticalNormalizedPosition=0;yield return null;yield return null;
            var secretToggle=wizard.GetComponentsInChildren<Button>().Single(b=>b.name=="시크릿 브라우저 설치");
            Check(secretToggle.transform.IsChildOf(contract.content),"시크릿 설치 체크는 더보기 약관 내용 하단에 배치");
            yield return MouseClick((RectTransform)secretToggle.transform);Check(wizard.SecretOption&&contract.verticalNormalizedPosition<.01f,"약관 체크 클릭·스크롤 위치 유지");Capture("stage07-secret-contract-option");
            wizard.Agree=true;wizard.Next();Check(!wizard.GetComponentsInChildren<Button>().Any(b=>b.name=="시크릿 브라우저 설치"),"설치 설정의 중복 시크릿 체크 제거");
            wizard.Previous();Check(wizard.SecretOption,"이전 단계 복귀 시 시크릿 선택 유지");wizard.Next();wizard.Next();yield return new WaitForSeconds(.35f);
            Check(wizard.Step==5&&stage.SecretInstalled&&app.Save.Progress.flags.Contains("stage07.secretBrowserInstalled"),"약관 선택한 시크릿 설치 기록");
            Check(!stage.App.ScreenRoot.GetComponentsInChildren<FakeIcon>().Any(i=>i.Id=="s7:internet"||i.Id=="s7:secret"),"인터넷 설치 결과 바탕화면 아이콘 생성 안 함");Capture("stage07-internet-no-icons");
            wizard.Next();yield return null;Check(!app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.Key.StartsWith("s7:browser:")),"인터넷 연결 완료 시 브라우저 자동 실행 안 함");

            stage.Launch("secret");yield return new WaitForSeconds(.2f);Capture("stage07-secret-browser");app.CloseAllWindows();yield return null;
            stage.Launch("map");yield return new WaitForSeconds(.25f);var map=app.Windows.GetComponentInChildren<Map07Window>();Capture("stage07-map");map.Navigate("5");yield return new WaitForSeconds(.55f);stage.ExitDoor();Check(app.Stages.State!=StageState.Transition,"잠긴 문 실행 차단");
            map.Navigate("4b");yield return new WaitForSeconds(.55f);map.Navigate("4t");yield return new WaitForSeconds(.55f);map.Collect("shears");Check(stage.Shears,"정원가위 획득");map.Window.Close();yield return null;stage.Launch("map");yield return new WaitForSeconds(.2f);map=app.Windows.GetComponentInChildren<Map07Window>();Check(stage.Shears&&map.GetComponentsInChildren<FakeIcon>().Any(i=>i.Id=="inventory:shears"),"지도 재실행 인벤토리 유지");
            Check(app.Catalog.audio.Get("sfx_bush_cut")&&app.Catalog.audio.Get("sfx_bush_cut")!=app.Catalog.audio.Get("sfx_icon_press"),"수풀 제거 전용 효과음 등록");
            map.Navigate("1b");yield return new WaitForSeconds(.55f);var shears=map.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="inventory:shears");var bush=map.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="bush");yield return MouseDrag(Center(shears.Rect),Center(bush.Rect));yield return new WaitForSeconds(.8f);Check(stage.CutBushes.Contains("1b")&&!stage.Key,"실제 가위 드래그 수풀 제거·열쇠 노출");map.Collect("key");map.Collect("key");Check(stage.Key&&stage.Shears,"열쇠 중복 방지·가위 비소모");
            map.Navigate("3t");yield return new WaitForSeconds(.55f);Check(!map.PushRock(new Vector2(100,0))&&!map.PushRock(new Vector2(0,100))&&map.PushRock(new Vector2(0,-100))&&stage.RockMoved,"돌 방향 제한·아래 이동·구멍");Capture("stage07-rock");
            map.Navigate("3l");yield return new WaitForSeconds(.55f);map.Use("shears","bush");yield return new WaitForSeconds(.8f);map.Use("shears","doll");Check(stage.AnimationBusy&&!app.Audio.MusicPlaying,"인형 연출 입력 잠금·BGM 일시 정지");yield return new WaitForSeconds(1);Check(!stage.AnimationBusy&&stage.DollBeheaded&&app.Save.Progress.flags.Contains("stage07.dollBeheaded"),"인형 선택 이벤트·입력 복귀·기록");
            Check(!app.Audio.MusicPlaying,"인형 연출 종료 후 구역에 머무는 동안 BGM 정지 유지");
            map.Navigate("");Check(app.Audio.MusicPlaying,"전체 지도 복귀 시 BGM 재개");
            map.Navigate("3l");yield return new WaitForSeconds(.55f);Check(app.Audio.MusicPlaying,"이미 본 인형 재방문 시 음악 정상");
            stage.DollBeheaded=false;map.Use("shears","doll");yield return new WaitForSeconds(1);map.Navigate("3c");Check(app.Audio.MusicPlaying,"인형 구역에서 다른 구역으로 이동 시 BGM 재개");
            map.Navigate("3l");yield return new WaitForSeconds(.55f);stage.DollBeheaded=false;map.Use("shears","doll");yield return new WaitForSeconds(1);map.Window.Close();yield return null;Check(app.Audio.MusicPlaying,"map.exe 닫기 시 BGM 재개");map=Map07Window.Open(stage);

            map.Navigate("5");yield return new WaitForSeconds(.55f);var key=map.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="inventory:key");var padlock=(RectTransform)map.transform.Find("내용/지도 화면/door/자물쇠");yield return MouseDrag(Center(key.Rect),Center(padlock));yield return null;Check(stage.DoorUnlocked&&!stage.Key,"실제 열쇠 드래그 자물쇠 해제·열쇠 소모");Capture("stage07-door-unlocked");
            stage.Talk();Check(app.Dialogue.CurrentSequence=="S7_DOOR"&&map,"잠금 해제 후 NPC 특수 대화·지도 유지");yield return FinishDialogue();stage.Talk();Check(app.Dialogue.CurrentSequence=="S7_AFTER_DOOR","특수 대화 한 번·후속 반복");yield return FinishDialogue();stage.ExitDoor();yield return new WaitForSeconds(12);Check(app.Stages.Current.stageId=="Stage08"&&new SaveService().Progress.cleared.Contains("Stage07"),"Stage07 CMD 전환·클리어 저장");
            for(int order=0;order<4;order++)
            {
                app.Save.Enter("Stage07");app.Begin(false);yield return new WaitForSeconds(.2f);stage=app.ScreenRoot.GetComponentInChildren<Stage07Controller>();stage.Data.installSeconds=.1f;
                Check(!stage.Installs.IsInstalled("map")&&!stage.Shears&&!stage.Key&&!stage.DoorUnlocked&&!stage.SecretUnlocked&&stage.CutBushes.Count==0,"이어하기 Stage07 진행 전체 초기화 "+order);
                foreach(var id in order%2==0?new[]{"viewer","internet","map"}:new[]{"map","viewer","internet"})yield return Install(id);
                stage.Launch("map");yield return new WaitForSeconds(.2f);map=app.Windows.GetComponentInChildren<Map07Window>();
                foreach(var region in order%2==0?new[]{"5","2b","3r"}:new[]{"3l","1b","2a"}){map.Navigate(region);yield return null;}
                map.Navigate("4t");map.Collect("shears");map.Navigate("1b");map.Use("shears","bush");map.Collect("key");map.Navigate("5");map.Use("key","lock");Check(stage.DoorUnlocked,"설치/방문 순서 조합 필수 경로 "+order);app.CloseAllWindows();yield return null;
            }
        }
    }
}
#endif
