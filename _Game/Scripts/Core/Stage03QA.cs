#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator S3MoveLens(FilmLensWindow lens,Vector2 point)
        {
            var bar=(RectTransform)lens.Window.transform.Find("제목 표시줄");
            float x=Mathf.Clamp(point.x-155,0,694),y=Mathf.Clamp(point.y-140,38,493);
            var from=Center(bar);var delta=app.Root.TransformVector(new Vector3(x-lens.Window.Rect.anchoredPosition.x,-y-lens.Window.Rect.anchoredPosition.y));
            var end=RectTransformUtility.WorldToScreenPoint(app.UICamera,bar.TransformPoint(bar.rect.center)+delta);
            yield return MouseDrag(from,end);yield return new WaitForSeconds(.2f);
        }
        IEnumerator S3FinishDialogue(){yield return Hold(Key.LeftCtrl,5);Check(!app.Dialogue.Active,"Stage03 Ctrl 대화 종료");}
        IEnumerator Stage03Checks(bool preserveFlow=false)
        {
            if(!preserveFlow){app.Save.NewGame("Stage03");app.Begin(false);}app.Save.Flag("stage02.keep");yield return new WaitForSeconds(.3f);
            var stage=app.ScreenRoot.GetComponentInChildren<Stage03Controller>();Check(stage&&app.Stages.Current.stageId=="Stage03","Stage03 데이터·진입 프리팹");
            var dialogueData=JsonUtility.FromJson<DialogueDocument>(app.Stages.Current.dialogue.text);Check(dialogueData.sequences.Select(q=>q.id).Distinct().Count()==dialogueData.sequences.Length,"Stage03 대사 ID 중복 없음");Check(dialogueData.sequences.Single(q=>q.id=="S3_LOOP_B").lines.Length==11,"반복 B 원문 11줄 연결");
            Check(app.Catalog.theme.font.HasCharacter('ㄹ')&&app.Catalog.theme.font.HasCharacter('ㅍ'),"호환 자모 글리프");
            var trail=stage.Data.trail;
            Check(trail.points.All(p=>trail.Safe(p))&&trail.Safe(trail.goal),"발자국 회전 영역 여백·초기 필름 창 회피");
            Check(trail.points.Select((p,i)=>Vector2.Distance(p,i+1<trail.points.Length?trail.points[i+1]:trail.goal)).All(d=>d<=trail.maxGap+.01f),"발자국 최대 간격·최종 목표 간격");
            Check(trail.points.Select((p,i)=>{var next=i+1<trail.points.Length?trail.points[i+1]:trail.goal;var direction=(Vector2)(Quaternion.Euler(0,0,trail.Angle(i))*Vector3.up);return Vector2.Dot(direction,new Vector2(next.x-p.x,p.y-next.y).normalized)>.999f;}).All(v=>v),"모든 발자국 다음 목표 방향 회전");
            var invalidTrail=new TrailData{points=new[]{new Vector2(500,300),new Vector2(500,400)},margin=300,maxAttempts=2,reservedWindow=new Rect(0,0,1024,768)};invalidTrail.Build();Check(invalidTrail.usedFallback&&invalidTrail.points.Length>1&&invalidTrail.points.All(p=>invalidTrail.Safe(p)),"불가능한 설정 유한 대체 경로");
            for(int i=0;i<100;i++)
            {
                var p=new Stage03Randomizer(stage.Data);var avoid=Stage03Randomizer.Exclusions(stage.Data);
                bool valid=p.Password.Length==4&&p.Password.Distinct().Count()==4&&p.Names.Count==4;
                for(int n=0;n<p.Names.Count;n++)valid&=p.Names[n].x>=24&&p.Names[n].y>=38&&p.Names[n].xMax<=1000&&p.Names[n].yMax<=744&&!avoid.Any(a=>a.Overlaps(p.Names[n]))&&!p.Names.Where((a,j)=>j!=n).Any(a=>a.Overlaps(p.Names[n]));
                Check(valid,"랜덤 암호/충돌 없는 배치 "+(i+1));
            }
            stage.Click("bad1");Check(app.Dialogue.CurrentSequence=="S3_INTRO_A"&&!stage.IntroDone,"exe 먼저: 도입 A·실행 차단");yield return S3FinishDialogue();stage.Click("bad1");yield return null;Check(app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.Key.StartsWith("alert:")),"금지 exe 경고");app.CloseAllWindows();yield return null;
            app.Begin(false);yield return null;stage=app.ScreenRoot.GetComponentInChildren<Stage03Controller>();stage.Talk();Check(app.Dialogue.CurrentSequence=="S3_INTRO_B","NPC 먼저: 도입 B");yield return S3FinishDialogue();
            stage.Talk();Check(app.Dialogue.CurrentSequence=="S3_LOOP_A","업데이트 전 반복 A");yield return S3FinishDialogue();
            stage.OpenFilm();yield return new WaitForSeconds(.3f);var lens=app.Windows.GetComponentInChildren<FilmLensWindow>();stage.OpenFilm();Check(app.Windows.GetComponentsInChildren<FilmLensWindow>().Length==1,"렌즈 싱글 인스턴스");
            Check(!lens.Shows("trail:0"),"최초 렌즈 위치 X와 겹치지 않음");
            lens.Window.Rect.anchoredPosition=new Vector2(100,-493);yield return null;yield return null;
            var partial=(RectTransform)lens.Area.Find("trail:0");Check(partial&&partial.rect.width>0&&partial.rect.width<40,"렌즈 경계에서 X 일부만 마스크 표시");
            lens.Window.Rect.anchoredPosition=new Vector2(450,-260);yield return null;yield return null;Check(!lens.Shows("trail:0"),"렌즈를 치우면 다시 숨김");
            yield return S3MoveLens(lens,new Vector2(70,240));yield return MouseClick(S2Icon("pieces").Rect);yield return new WaitForSeconds(.3f);
            Check(app.Windows.GetComponentsInChildren<FakeWindow>().Any(w=>w.Key=="s3:pieces"),"렌즈 본문 클릭 통과로 아래 폴더 열기");
            foreach(var w in app.Windows.GetComponentsInChildren<FakeWindow>())if(w!=lens.Window)w.Close();yield return null;
            int guard=0;while(stage.TrailStep<stage.Data.trail.points.Length&&guard++<512)
            {
                int step=stage.TrailStep;yield return S3MoveLens(lens,stage.Data.trail.points[step]);Check(lens.Contains(stage.Data.trail.points[step]),"발자국 "+step+" 실제 창 드래그 도달");if(step>0){yield return null;Check(lens.Shows("trail:"+step),"발자국 "+step+" 필름 본문 실제 표시");}
                if(step==0){yield return new WaitForSeconds(.6f);Check(stage.TrailStep==0&&stage.TrailProgress>0&&stage.TrailProgress<1,"X 체류 3초·점진 진행");lens.Window.Close();yield return new WaitForSeconds(.1f);stage.OpenFilm();yield return new WaitForSeconds(.3f);lens=app.Windows.GetComponentInChildren<FilmLensWindow>();Check(stage.TrailStep==0,"렌즈 재열기 진행 보존");yield return S3MoveLens(lens,stage.Data.trail.points[0]);}
                yield return new WaitForSeconds(step==0?3.1f:1.1f);Check(stage.TrailStep>step,"체류 후 다음 발자국 생성");
            }
            yield return S3MoveLens(lens,stage.Data.trail.goal);yield return null;yield return null;
            Check(stage.TrailStep==stage.Data.trail.points.Length&&lens.Shows("patch"),"발자국 종점 숨김 패치");Capture("stage03-trail");
            var patch=(RectTransform)lens.Area.Find("patch");yield return MouseClick(patch);Check(stage.PatchFound&&Icons().Any(i=>i.Id=="patch"),"숨김 패치 클릭 → 일반 아이콘");
            lens.Window.Close();yield return null;yield return MouseClick(S2Icon("patch").Rect);Check(stage.Busy,"패치 설치 입력 잠금");float installDeadline=Time.realtimeSinceStartup+stage.Data.installSeconds+5;while(stage.Busy&&Time.realtimeSinceStartup<installDeadline)yield return null;yield return null;
            Check(stage.Updated&&!stage.Busy&&!Icons().Any(i=>i.Id=="patch")&&!app.Windows.GetComponentInChildren<FilmLensWindow>(),"설치 완료·패치 소모·렌즈 닫힘");
            stage.OpenPieces();yield return new WaitForSeconds(.3f);Check(app.Windows.GetComponentsInChildren<FakeIcon>().Select(i=>i.GetComponentInChildren<TMPro.TMP_Text>().text).SequenceEqual(new[]{"3_a.png","1_b.png","2_c.png","4_d.png"}),"조각들 표시 순서 변경·암호 순서 유지");
            stage.OpenFilm();Check(app.Dialogue.Active&&app.Dialogue.CurrentSequence=="S3_FILM_FIRST"&&!app.Windows.GetComponentInChildren<FilmLensWindow>(),"필름 먼저 실행 시 대화 우선·렌즈 대기");yield return null;Check(app.Windows.GetComponentsInChildren<FakeWindow>().Length==0,"새 대화 시작 시 모든 창 닫힘");yield return S3FinishDialogue();yield return new WaitForSeconds(.3f);Check(app.Windows.GetComponentInChildren<FilmLensWindow>()&&new SaveService().Progress.flags.Contains("stage03.p1Talk"),"연결 대화 완료 후 렌즈 실행·저장 재로드 유지");stage.Talk();Check(app.Dialogue.CurrentSequence=="S3_LOOP_B","새 대화 후 P1 대사 중복 없음");yield return S3FinishDialogue();
            // Independent NPC-first branch fixture; production state is set only by dialogue completion.
            typeof(Stage03Controller).GetField("p1Talk",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(stage,false);app.Save.Progress.flags.Remove("stage03.p1Talk");app.Save.SaveProgress();
            stage.Talk();Check(app.Dialogue.CurrentSequence=="S3_P1_OK","업데이트 후 첫 대화");yield return S3FinishDialogue();stage.Talk();Check(app.Dialogue.CurrentSequence=="S3_LOOP_B"&&app.Dialogue.Active,"업데이트 후 반복 B");yield return new WaitForSeconds(.3f);Check(app.Modal.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text=="근데 되게 시끄럽지 않았어?"),"반복 B 첫 대사 실제 표시");Capture("stage03-loop-b");yield return S3FinishDialogue();stage.Talk();Check(app.Dialogue.Active&&app.Dialogue.CurrentSequence=="S3_LOOP_B","암호 해금 전 반복 B 재대화");yield return S3FinishDialogue();
            stage.OpenFilm();yield return new WaitForSeconds(.3f);lens=app.Windows.GetComponentInChildren<FilmLensWindow>();Check(lens.Filter==LensFilter.Red&&!app.Dialogue.Active,"NPC 먼저 대화 후 추가 대화 없이 ver2 기본 빨강");
            yield return MouseClick((RectTransform)lens.Window.Toolbar.Find("Blue"));Check(lens.Filter==LensFilter.Blue,"필터 탭 실제 마우스 클릭");
            for(int i=0;i<4;i++)
            {
                var card=stage.Data.cards[i];lens.Select(Stage03Controller.Parse(card.filter));yield return S3MoveLens(lens,stage.Puzzle.Names[i].center);
                Check(lens.Shows("name:"+i),"색 필터별 바탕화면 파일명 "+i);
                stage.OpenCard(i);yield return new WaitForSeconds(.3f);var hidden=stage.Hidden.Last(h=>h.id=="s3:card:"+card.realName);yield return S3MoveLens(lens,hidden.ScreenBounds.center);
                Check(lens.Shows(hidden.id),"이미지 위 숫자 필터 "+i);Capture("stage03-card-"+i);
                lens.Select(LensFilter.Neutral);yield return null;yield return null;Check(!lens.Shows(hidden.id),"잘못된 필터 숫자 숨김 "+i);
                hidden.owner.GetComponentInParent<FakeWindow>().Close();yield return null;
            }
            lens.Window.Close();yield return null;stage.OpenSecret();yield return new WaitForSeconds(.3f);Capture("stage03-password");stage.OpenSecret();Check(app.Windows.GetComponentsInChildren<PasswordDialog>().Length==1,"암호창 중복 방지");
            var password=app.Windows.GetComponentInChildren<PasswordDialog>();Check(password.InputFocused,"암호 입력 자동 포커스");
            bool initialCaret=password.CaretVisible;yield return new WaitForSeconds(.51f);Check(password.CaretVisible!=initialCaret,"암호 커서 깜빡임");
            yield return Press(Key.Digit1);Check(password.CaretVisible&&password.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text=="1|"),"입력 끝 커서 이동");yield return Press(Key.Backspace);Check(password.CaretVisible,"삭제 후 커서 유지");Check(app.Windows.GetComponentInChildren<PasswordDialog>().Digits=="","암호 Backspace");
            for(int i=0;i<4;i++)yield return Press(Key.Digit0);yield return Press(Key.Enter);yield return new WaitForSeconds(.4f);Check(!stage.SecretUnlocked&&app.Windows.GetComponentInChildren<PasswordDialog>().Digits=="","오답 입력 비움·잠금 유지");
            Key[] digits={Key.Numpad0,Key.Numpad1,Key.Numpad2,Key.Numpad3,Key.Numpad4,Key.Numpad5,Key.Numpad6,Key.Numpad7,Key.Numpad8,Key.Numpad9};foreach(char d in stage.Puzzle.Password)yield return Press(digits[d-'0']);yield return Press(Key.NumpadEnter);yield return new WaitForSeconds(.3f);
            Check(stage.SecretUnlocked&&!app.Windows.GetComponentInChildren<PasswordDialog>()&&Icons().Any(i=>i.Id=="s3:door"),"숫자키 암호 정답·비밀 폴더 해금");
            var secretWindow=app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="s3:secret");
            Check(!secretWindow.ShowHidden&&!secretWindow.Content.GetComponentsInChildren<FakeIcon>().Any(i=>i.Id=="s3:past"),"과거.png 기본 숨김·아이콘/입력 대상 미생성");
            Check(secretWindow.Content.GetComponentsInChildren<FakeIcon>().Select(i=>i.Id).SequenceEqual(new[]{"s3:door"})&&S2Icon("pieces"),"비밀 폴더 기본 목록 door만·바탕화면 조각들 유지");stage.OpenPastImage();Check(!app.Windows.GetComponentInChildren<ImageViewer>(),"숨김 상태 직접 열기도 차단");
            var hiddenToggle=(RectTransform)secretWindow.Toolbar.Find("설정 · 숨김파일 표시");yield return MouseClick(hiddenToggle);yield return null;
            Check(secretWindow.Content.GetComponentsInChildren<FakeIcon>().Select(i=>i.Id).SequenceEqual(new[]{"s3:door","s3:past"}),"숨김 표시 시 비밀 폴더 door·과거만");
            var pastIcon=secretWindow.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s3:past");Check(secretWindow.ShowHidden&&pastIcon.GetComponent<CanvasGroup>().alpha==.5f,"기존 숨김 토글·반투명 아이콘 재사용");
            yield return MouseClick(hiddenToggle);yield return null;Check(!secretWindow.Content.GetComponentsInChildren<FakeIcon>(true).Any(i=>i.Id=="s3:past"),"숨김 해제 시 클릭·드래그·드롭·목록에서 완전 제외");
            yield return MouseClick(hiddenToggle);secretWindow.Close();yield return null;stage.OpenSecret();yield return new WaitForSeconds(.3f);secretWindow=app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="s3:secret");Check(secretWindow.ShowHidden,"일반 창 재열기 숨김 옵션 유지");
            yield return MouseClick(secretWindow.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s3:past").Rect);yield return new WaitForSeconds(.3f);Check(app.Windows.GetComponentInChildren<ImageViewer>(),"표시된 과거.png 실제 클릭 뷰어 열기");Capture("stage03-hidden-past");
            stage.Talk();Check(app.Dialogue.CurrentSequence=="S3_LOOP_C","해금 후 반복 C");yield return S3FinishDialogue();stage.OpenPastImage();yield return new WaitForSeconds(.3f);stage.OpenFilm();yield return new WaitForSeconds(.3f);lens=app.Windows.GetComponentInChildren<FilmLensWindow>();
            lens.Window.Close();yield return null;var pastViewer=app.Windows.GetComponentInChildren<ImageViewer>();
            yield return MouseClick((RectTransform)pastViewer.Window.Content.Find("+"));Check(pastViewer.Level==1,"과거 이미지 확대 버튼");
            var panBefore=pastViewer.PictureRoot.anchoredPosition;var panStart=Center((RectTransform)pastViewer.transform);yield return MouseDrag(panStart,panStart+new Vector2(-70,50));Check(pastViewer.PictureRoot.anchoredPosition!=panBefore,"확대 이미지 실제 드래그 이동");
            pastViewer.SetZoom(2);pastViewer.PictureRoot.anchoredPosition=Vector2.zero;stage.OpenFilm();yield return new WaitForSeconds(.3f);lens=app.Windows.GetComponentInChildren<FilmLensWindow>();var date=stage.Hidden.First(h=>h.id=="date:Red");yield return S3MoveLens(lens,date.ScreenBounds.center);yield return null;
            Check(lens.Shows(date.id)&&Mathf.Abs(lens.Area.Find(date.id).GetComponentInChildren<TMPro.TMP_Text>().fontSize-27*pastViewer.Window.Content.localScale.x)<.1f,"날짜 글씨 2.25배 확대 및 창 콘텐츠 배율 연동");Capture("stage03-date-zoom");
            pastViewer.SetZoom(0);Check(pastViewer.PictureRoot.anchoredPosition==Vector2.zero,"기본 배율 복귀·위치 보정");
            foreach(var c in stage.Data.dateParts){var h=stage.Hidden.Last(x=>x.id=="date:"+c.filter);lens.Select(Stage03Controller.Parse(c.filter));yield return S3MoveLens(lens,h.ScreenBounds.center);Check(lens.Shows(h.id),"과거 그림 날짜 단서 "+c.filter);}
            app.CloseAllWindows();yield return null;
            var preserved=app.ScreenRoot.GetComponentsInChildren<FakeIcon>().Where(i=>new[]{"film","bad1","bad2","pieces","secret"}.Contains(i.Id)).ToArray();var positions=preserved.Select(i=>i.Rect.anchoredPosition).ToArray();
            Check(preserved.Where(i=>i.Id=="film"||i.Id=="bad1"||i.Id=="bad2").All(i=>i.GetComponentsInChildren<Image>().Any(im=>im.sprite==app.Catalog.sprites.Get("s3_program"))),"실행파일 3개 프로그램 창 도트 아이콘");
            stage.Click("pieces");yield return new WaitForSeconds(.3f);Check(app.Windows.GetComponentsInChildren<FakeWindow>().Length>0,"과거 진입 전 폴더 창 준비");int warpCount=0;Action<string> heardWarp=k=>{if(k==stage.Data.warp)warpCount++;};app.Audio.Played+=heardWarp;app.Catalog.crt.Apply(0,0);stage.ApplyDate(new DateTime(1990,2,12));
            Check(app.Catalog.crt.material.GetVector("_Light").z==1&&app.Audio.DateEffectPlaying&&warpCount==1,"직접 이어하기·화면효과 0에서도 재질 세피아 즉시 적용·전용 효과음");Check(app.Modal.Find("섬광"),"과거 진입 섬광 연결");stage.ApplyDate(new DateTime(1990,2,12));Check(warpCount==1,"과거 효과음 중복 방지");Check(stage.Past&&app.Catalog.crt.sepiaAmount==1,"해금 후 과거 전환");Check(stage.HiddenInPastCount==5&&preserved.All(i=>!i.GetComponent<CanvasGroup>().blocksRaycasts),"과거 숨김 상태·페이드 중 클릭 차단");yield return new WaitForSeconds(.3f);
            Check(preserved.All(i=>i&&!i.gameObject.activeSelf)&&S2Icon("computer").gameObject.activeInHierarchy,"폴더/exe 비파괴 숨김·내 컴퓨터 예외");Check(app.Windows.GetComponentsInChildren<FakeWindow>().Length==0,"과거 진입 열린 창 모두 닫힘");stage.Click("pieces");stage.OpenSecret();stage.OpenFilm();Check(app.Windows.GetComponentsInChildren<FakeWindow>().Length==0,"숨긴 아이콘 실행 경로 차단");Capture("stage03-past-desktop");stage.Talk();Check(app.Dialogue.CurrentSequence=="S3_PAST","어린그림 첫 대화");yield return S3FinishDialogue();Check(Icons().Any(i=>i.Id=="memory"),"환영 실행파일 생성");Check(app.Catalog.crt.material.GetVector("_Light").z==1&&warpCount==1,"대화 종료 후에도 과거 세피아 유지");Capture("stage03-young");
            stage.Click("memory");Check(stage.Busy&&!app.CursorLayer.gameObject.activeSelf,"환영 입력 잠금·커서 숨김");stage.Click("memory");stage.Talk();Check(!app.Dialogue.Active,"환영 중 대화·중복 실행 차단");yield return new WaitForSeconds(7);Capture("stage03-memory");yield return new WaitForSeconds(stage.Data.visionSeconds-5);
            Check(app.Catalog.crt.material.GetVector("_Light").z==0&&!app.Audio.DateEffectPlaying,"현재 복귀 재질 세피아·전용 효과음 해제");app.Audio.Played-=heardWarp;Check(app.CursorLayer.gameObject.activeSelf,"복귀 완료 커서 복원");Check(stage.PastDone&&!stage.Past&&!stage.Busy&&app.DisplayDate==null&&app.Catalog.crt.sepiaAmount==0&&app.Audio.MusicPlaying&&app.Save.Progress.flags.Contains("stage03.pastSolved"),"환영 완료·현재 복귀·음악·선택 기록");stage.ApplyDate(new DateTime(1990,2,12));Check(!stage.Past&&!Icons().Any(i=>i.Id=="memory"),"과거 이벤트 한 번만");
            Check(stage.HiddenInPastCount==0&&preserved.Select((icon,i)=>icon&&icon.gameObject.activeSelf&&icon.Rect.anchoredPosition==positions[i]&&icon.GetComponent<CanvasGroup>().alpha==1).All(v=>v),"현재 복귀 동일 아이콘·위치·투명도 복원");
            stage.Click("pieces");yield return new WaitForSeconds(.3f);Check(app.Windows.GetComponentsInChildren<FakeIcon>().Count(i=>i.Id.StartsWith("s3:card:"))==4,"복원된 폴더 파일 4개 유지");app.CloseAllWindows();yield return null;
            stage.OpenSecret();yield return new WaitForSeconds(.3f);yield return Icon("s3:door");yield return new WaitForSeconds(12);Check(app.Stages.Current.stageId=="Stage04"&&app.Save.Progress.cleared.Contains("Stage03"),"door.exe CMD → Stage04 연결·클리어 저장");
            if(preserveFlow)yield return Stage04Checks(true);
            app.Save.Enter("Stage03");app.Begin(false);yield return null;stage=app.ScreenRoot.GetComponentInChildren<Stage03Controller>();Check(!app.Save.Hidden("s3:secret")&&!new SaveService().Hidden("s3:secret"),"이어하기 비밀 폴더 숨김 옵션 초기화·저장");Check(!new SaveService().Progress.flags.Contains("stage03.p1Talk")&&stage.TrailStep==0&&stage.TrailProgress==0,"이어하기 새 대화 저장 플래그·발자국 초기화");Check(stage.HiddenInPastCount==0&&app.ScreenRoot.GetComponentsInChildren<FakeIcon>().Count(i=>new[]{"film","bad1","bad2","pieces","secret"}.Contains(i.Id))==5,"이어하기 숨김 상태 초기화");Check(!stage.IntroDone&&!stage.Updated&&!stage.SecretUnlocked&&!stage.Past&&!stage.PastDone&&!stage.PatchFound&&stage.TrailStep==0&&!app.Save.Progress.flags.Contains("stage03.pastSolved")&&app.Save.Progress.flags.Contains("stage02.keep")&&app.Save.Progress.cleared.Contains("Stage03"),"이어하기 퍼즐 초기화·다른 기록/해금 보존");
            stage.ApplyDate(new DateTime(1990,2,12));Check(!stage.Past,"해금 전 날짜 이벤트 차단");Capture("stage03-desktop");app.Save.SetHidden("s3:secret",true);app.Save.NewGame("Stage03");Check(!app.Save.Hidden("s3:secret"),"새 게임 숨김 옵션 초기화");
        }
    }
}
#endif
