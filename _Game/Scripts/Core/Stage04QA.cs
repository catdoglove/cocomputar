#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator S4Right(FakeIcon icon){var point=Center(icon.Rect);InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=2});yield return null;yield return null;InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;yield return null;}
        IEnumerator S4Click(string name){var b=Buttons().Last(b=>b.name==name);yield return MouseClick((RectTransform)b.transform);yield return new WaitForSeconds(.2f);}
        IEnumerator S4WalkDream(PrologueController maze)
        {
            foreach(var cell in MazePath(maze,new Vector2Int(21,11)))
            {
                var target=PrologueController.Center(cell.x,cell.y);float deadline=Time.realtimeSinceStartup+3;
                while(!maze.Transitioning&&Vector2.Distance(maze.Position,target)>3.5f)
                {
                    if(Time.realtimeSinceStartup>deadline)throw new Exception("꿈 이동 시간 초과 "+cell);
                    var d=target-maze.Position;Key key=Mathf.Abs(d.x)>2.5f?(d.x>0?Key.D:Key.A):(d.y>0?Key.S:Key.W);
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;if(maze.Blocked(maze.Position))throw new Exception("꿈 벽 관통");
                }
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());if(maze.Transitioning)yield break;yield return null;
            }
        }
        IEnumerator Stage04Checks(bool preserveFlow=false)
        {
            var fixture=new FakeFileSystem(JsonUtility.FromJson<FakeFileData>(Resources.Load<TextAsset>("Stage04Files").text));
            Check(!fixture.ExtractArchive("zip","missing")&&fixture.Find("zip").available,"폴더 누락 시 ZIP 보존");
            fixture.Find("important").contentType="document";Check(!fixture.ExtractArchive("zip","important")&&fixture.Find("zip").available,"폴더 생성 불가 시 ZIP 보존");fixture.Find("important").contentType="folder";
            fixture.Find("zip").x=333;fixture.Find("zip").y=444;int refreshes=0;fixture.Changed+=()=>refreshes++;
            Check(fixture.ExtractArchive("zip","important")&&fixture.Find("important").x==333&&fixture.Find("important").y==444&&refreshes==1,"변경된 ZIP 위치 계승·목록 단일 갱신");Check(!fixture.ExtractArchive("zip","important"),"중복 해제 무시");
            if(!preserveFlow){app.Save.NewGame("Stage04");app.Begin(false);}else app.Save.UnlockDialogueSkip();yield return new WaitForSeconds(1.3f);
            var s=app.ScreenRoot.GetComponentInChildren<Stage04Controller>();Check(s&&app.Dialogue.CurrentSequence=="S4_INTRO","Stage04 강제 도입·스테이지 연결");Capture("stage04-intro");yield return Hold(Key.LeftCtrl,8);
            Check(s.IntroDone&&s.RestoreUnlocked&&!app.Dialogue.Active,"도입 종료 setFlag 복원 해금");Check(Icons().Count(i=>i.Id.StartsWith("s4:"))==3&&Icons().Count(i=>i.Id=="computer")==1,"초기 컴퓨터·문서·zip·휴지통 배치");
            var files=s.Files;var doc=files.Find("to_document");var home=files.Find("home");var diary=files.Find("diary");var money=files.Find("money");
            Check(Regex.IsMatch(s.Password,"^[0-9]{4}$")&&doc.content.Contains(s.Password),"문서 토큰·압축 암호 일치");Check(!Regex.IsMatch(doc.garbledContent,"[0-9]")&&!doc.garbledContent.Contains("PASSWORD")&&doc.content.Split('\n').Length==doc.garbledContent.Split('\n').Length,"깨진 문자열 비밀번호 비노출·줄 수 유지");Check(!files.CanUse(home),"해제 전 하위 파일 접근 차단");
            s.Talk();Check(app.Dialogue.CurrentSequence=="S4_LOOP_1","퍼즐 전 반복 대사 1");yield return Hold(Key.LeftCtrl,4);
            yield return MouseClick(Icons().Single(i=>i.Id=="s4:to_document").Rect);yield return new WaitForSeconds(.3f);var note=app.Windows.GetComponentInChildren<NotepadWindow>();Check(note&&note.Body.text==doc.garbledContent&&!s.Puzzle1,"ANSI 기본 본문·퍼즐 미완료");Capture("stage04-ansi");
            s.Open("to_document");s.Open("to_document");Check(app.Windows.GetComponentsInChildren<NotepadWindow>().Length==1,"메모장 단일 인스턴스");
            yield return S4Click("Save");Check(app.Windows.GetComponentsInChildren<TMP_InputField>().Any(f=>!f.interactable&&f.text==doc.displayName),"저장 파일명 비활성");yield return S4Click("ANSI ▼");yield return S4Click("UTF-8");yield return S4Click("Save");Check(doc.encoding=="UTF8"&&note.Body.text==doc.garbledContent&&!s.Puzzle1,"Save 인코딩만 변경·본문/완료 시점 유지");yield return S4Click("확인");
            yield return S4Click("Open");Check(Buttons().Any(b=>!b.interactable&&b.name.Contains("important.zip")),"Open 비텍스트 회색 선택 불가");yield return S4Click(Buttons().First(b=>b.name.StartsWith("to_document.txt")).name);yield return S4Click("Open");Check(note.Body.text==doc.content&&s.Puzzle1,"Open 재열람 UTF8·퍼즐1 완료");Capture("stage04-decoded");
            app.CloseAllWindows();yield return null;s.Talk();Check(app.Dialogue.CurrentSequence=="S4_LOOP_2","퍼즐1 후 반복 대사 2");yield return Hold(Key.LeftCtrl,5);
            s.Open("zip");Check(app.Windows.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("압축된 파일")),"zip 좌클릭 해제 안내");app.CloseAllWindows();yield return null;
            yield return S4Right(Icons().Single(i=>i.Id=="s4:zip"));Check(app.Modal.GetComponentsInChildren<Button>().Count(b=>b.name=="압축해제")==1&&!app.Modal.GetComponentsInChildren<Button>().Any(b=>b.name=="삭제"),"zip 우클릭 압축해제 단독 메뉴");yield return S4Click("압축해제");yield return new WaitForSeconds(.3f);var password=app.Windows.GetComponentInChildren<PasswordDialog>();Check(password&&password.InputFocused,"재사용 암호창 자동 포커스");yield return Press(Key.Enter);yield return new WaitForSeconds(.4f);Check(password&&password.Digits==""&&!files.Find("important").available&&files.Find("zip")!=null,"오답 흔들림·비움·해제 방지");Key[] digits={Key.Numpad0,Key.Numpad1,Key.Numpad2,Key.Numpad3,Key.Numpad4,Key.Numpad5,Key.Numpad6,Key.Numpad7,Key.Numpad8,Key.Numpad9};foreach(char c in s.Password)yield return Press(digits[c-'0']);yield return Press(Key.Enter);yield return new WaitForSeconds(.3f);Check(files.Find("important").available&&!app.Windows.GetComponentInChildren<PasswordDialog>(),"정답 입력 important 생성");
            var extracted=files.Find("important");Check(files.Find("zip")==null&&!files.Children("trash").Any(n=>n.id=="zip")&&!Icons().Any(i=>i.Id=="s4:zip"),"압축 성공 ZIP 완전 제거·휴지통 이동 없음");Check(extracted.parentFolder=="desktop"&&extracted.x==60&&extracted.y==370,"폴더 ZIP 원래 위치 배치");s.Extract();Check(!app.Windows.GetComponentInChildren<PasswordDialog>(),"소비한 ZIP 재압축해제 차단");
            s.Open("important");yield return new WaitForSeconds(.3f);Check(app.Windows.GetComponentsInChildren<FakeIcon>().Length==3,"압축 폴더 원본 파일 세 개");s.Open("home");Check(app.Windows.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("손상된")),"유효하지만 내용 불일치 열기 오류");app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="s4:notice").Close();yield return null;
            var hi=Icons().Single(i=>i.Id=="s4:home");yield return S4Right(hi);Check(!Buttons().Last(b=>b.name=="복원").interactable,"정상 확장자 복원 비활성");yield return S4Click("이름 바꾸기");var input=hi.GetComponentInChildren<TMP_InputField>();Check(input&&input.isFocused&&input.selectionStringAnchorPosition==input.text.Length&&input.selectionStringFocusPosition==input.text.Length,"인라인 이름 자동 포커스·확장자 끝 커서");input.text="집.tXt";yield return Press(Key.Enter);yield return new WaitForSeconds(.2f);Check(home.displayName=="집.tXt"&&files.ActionFor(home)=="notepad","인라인 Enter·한글 이름·대소문자 확장자");s.Open("home");yield return new WaitForSeconds(.3f);note=app.Windows.GetComponentsInChildren<NotepadWindow>().Single(n=>n.File.id=="home");Check(Regex.Replace(home.content,"[^a-z]","")=="money"&&note.Body.rectTransform.rect.height>1000,"money 소문자 다섯 개·스크롤 긴 본문");Capture("stage04-home");
            s.Delete("home");Check(!app.Windows.GetComponentsInChildren<NotepadWindow>().Any(n=>n.File.id=="home")&&!files.CanUse(home),"삭제 시 관련 창 닫힘·열람 차단");s.Open("home");Check(!app.Windows.GetComponentInChildren<NotepadWindow>(),"휴지통 문서 실행 차단");files.RestoreTrash(home);Check(home.parentFolder=="important"&&files.ActionFor(home)=="notepad","휴지통 원래 경로·기능 복원");
            app.CloseAllWindows();yield return null;s.Open("important");yield return new WaitForSeconds(.3f);
            Check(!files.Rename(home,"",out _)&&!files.Rename(home,"a?.txt",out _)&&!files.Rename(home,"일기.txt",out _),"빈 이름·금지 문자·대소문자 중복 거부");
            s.Rename("home","집.zzz");Check(home.isBroken&&home.iconKey=="icon_broken","무효 확장자 깨짐 아이콘");s.Restore("home");Check(home.displayName=="집.JPG"&&!home.isBroken&&home.iconKey==home.originalIcon,"복원 원래 이름·아이콘");
            foreach(var ext in new[]{"jpg","png","gif","bmp","txt","exe"}){files.Rename(money,"wrong."+ext,out _);Check(!money.isBroken&&files.ActionFor(money)==null,"money 잘못된 내용/이름 정상 아이콘·오류 "+ext);}
            files.Rename(money,"money.dir",out _);Check(money.isBroken,"dir은 일기만 허용");s.Restore("money");Check(money.displayName=="???.ex?e"&&money.isBroken,"깨진 원본도 복원 가능");
            s.Rename("diary","일기.JPG");s.Open("diary");yield return new WaitForSeconds(.3f);var viewer=app.Windows.GetComponentInChildren<ImageViewer>();Check(viewer&&viewer.Definition.zoomInLevels==4&&!viewer.PictureRoot.Find("dir").gameObject.activeSelf,"일기 이미지뷰어·기본 배율 단서 숨김");viewer.SetZoom(2);Check(viewer.PictureRoot.Find("dir").gameObject.activeSelf,"2단계 확대 dir 오버레이");Capture("stage04-diary-hint");
            s.Rename("diary","일기.dir");s.Open("diary");yield return new WaitForSeconds(.3f);Check(app.Windows.GetComponentsInChildren<TMP_InputField>().Length==2&&app.Save.Progress.flags.Contains("stage04.diaryOpened"),"일기 두 페이지 입력·선택 기록");Capture("stage04-diary");yield return S4Click("저장");Check(!s.Ready&&!app.Audio.MusicPlaying,"일기 저장 즉시 BGM·입력 잠금");yield return new WaitForSeconds(3.2f);Check(s.Ready&&app.Audio.MusicPlaying&&!app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.Key=="s4:diary"),"3초 뒤 경고·일기 닫기·BGM 복귀");
            app.CloseAllWindows();yield return null;s.Delete("important");Check(!files.CanUse(diary)&&!files.CanUse(money),"부모 폴더 휴지통 하위 실행 차단");files.RestoreTrash(files.Find("important"));Check(files.Nodes.Count(n=>n.id=="important")==1&&files.CanUse(money),"압축 해제 폴더 휴지통 복원·하위 파일 복구");
            s.Rename("money","MoNeY.ExE");s.Open("money");s.Open("money");yield return new WaitForSeconds(4.5f);Check(s.Puzzle2&&Icons().Count(i=>i.Id=="s4:mydream")==1,"money 대소문자 실행·설치 중복 방지·mydream 생성");s.Talk();Check(app.Dialogue.CurrentSequence=="S4_LOOP_3","퍼즐2 후 반복 대사 3");yield return Hold(Key.LeftCtrl,5);Capture("stage04-ready-dream");
            var fallback=FakeFileSystem.GarbleFallback("id","a1\n나2");Check(fallback==FakeFileSystem.GarbleFallback("id","a1\n나2")&&fallback.Split('\n').Length==2&&!Regex.IsMatch(fallback,"[0-9]"),"ANSI 폴백 고정 시드·줄바꿈·숫자 은닉");
            s.Open("mydream");yield return new WaitForSeconds(.3f);var maze=app.Windows.GetComponentInChildren<PrologueController>();Check(maze&&maze.Map.start=="paper"&&app.CursorLayer.gameObject.activeSelf,"새 꿈 레이아웃·창 조작 커서 유지");Capture("stage04-dream");Check(maze.Blocked(PrologueController.Center(0,0)),"종이 벽 충돌");
            yield return S4WalkDream(maze);yield return new WaitForSeconds(13);Check(app.Stages.Current.stageId=="Stage05"&&app.Save.Progress.cleared.Contains("Stage04"),"Space 없이 문 접촉·CMD·Stage04 클리어·Stage05 자리");
            if(preserveFlow)yield return Stage05Checks(true);
            app.Save.Flag("stage03.keep");app.Save.Enter("Stage04");app.Begin(false);yield return new WaitForSeconds(1.2f);s=app.ScreenRoot.GetComponentInChildren<Stage04Controller>();Check(!s.Puzzle1&&!s.Puzzle2&&!s.Files.Find("important").available&&s.Files.Find("to_document").encoding=="ANSI"&&s.Files.Find("money").displayName=="???.ex?e","이어하기 파일명·인코딩·해제·퍼즐 초기화");Check(s.Files.Find("zip")?.available==true&&s.Files.Find("zip").parentFolder=="desktop","이어하기 ZIP 원본 복구");Check(!app.Save.Progress.flags.Contains("stage04.diaryOpened")&&app.Save.Progress.flags.Contains("stage03.keep")&&app.Save.Progress.cleared.Contains("Stage04"),"선택 기록만 초기화·다른 기록/해금 보존");yield return Hold(Key.LeftCtrl,8);Capture("stage04-reset");
        }
    }
}
#endif
