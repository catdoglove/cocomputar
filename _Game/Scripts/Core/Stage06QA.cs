#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator Stage06Checks()
        {
            app.Save.MarkStage06SpecialSeen();
            Check(new SaveService().Stage06SpecialSeen,"열람 기록 저장·로드");
            app.Save.NewGame("Stage06");
            Check(!app.Save.Stage06SpecialSeen&&!new SaveService().Stage06SpecialSeen,"새 게임 열람 기록 초기화");
            app.Save.MarkStage06SpecialSeen();
            app.Save.ResetStagePuzzles("Stage05");
            Check(app.Save.Stage06SpecialSeen,"다른 스테이지 초기화는 열람 기록 보존");
            app.Save.ResetStagePuzzles("Stage06");
            Check(!app.Save.Stage06SpecialSeen&&!new SaveService().Stage06SpecialSeen,"Stage06 재시작 열람 기록 초기화");
            var config=JsonUtility.FromJson<Stage06Config>(Resources.Load<TextAsset>("Stage06Config").text);
            for(int i=0;i<100;i++){var files=new FakeFileSystem(JsonUtility.FromJson<FakeFileData>(Resources.Load<TextAsset>("Stage06FileLayout").text));var random=new Stage06Randomizer(config);random.Apply(files);Check(random.CardMissing>=1&&random.CardMissing<=99&&random.RopeMissing>=1&&random.RopeMissing<=20&&random.DoorIndex>=3&&random.DoorIndex<=6&&files.Nodes.Select(n=>n.id).Distinct().Count()==files.Nodes.Count&&GameIntegrityChecker.Check(files,config,"card").Length==1&&GameIntegrityChecker.Check(files,config,"rope").Length==1,"Stage06 랜덤 100회 검증 "+i);}
            Check(TimePropertyParser.Parse(config.initialText,config.schema,out var defaults,out _)&&defaults["play_time"]=="1","초기 설정 정상");
            Check(TimePropertyParser.Parse(config.initialText.Replace("play_time = 1","play_time=99999999999999999999999999"),config.schema,out var clamped,out _)&&clamped["play_time"]=="999","큰 정수 999 보정");
            foreach(var bad in new[]{"player_age = x","player_age = 100","player_name = ABC","player_color = red","play_bg = outside","player = P3","play_time = -1","play_time = 1.5"}){var key=bad.Split('=')[0].Trim();string text=string.Join("\n",config.initialText.Split('\n').Select(l=>l.StartsWith(key+" =")?bad:l));Check(!TimePropertyParser.Parse(text,config.schema,out _,out _),"손상 설정 거절 "+bad);}
            Check(!TimePropertyParser.Parse(config.initialText+"\nunknown=1",config.schema,out _,out _)&&!TimePropertyParser.Parse(config.initialText+"\nplayer=P1",config.schema,out _,out _),"알 수 없는 키·중복 키 거절");
            app.Save.NewGame("Stage06");app.Begin(false);yield return new WaitForSeconds(.4f);var stage=app.ScreenRoot.GetComponentInChildren<Stage06Controller>();Check(stage&&!app.Dialogue.Active,"Stage06 진입·자동 대화 없음");Capture("stage06-desktop");stage.Talk();Check(app.Dialogue.CurrentSequence=="S6_TALK_1","조이스틱 첫 대화");yield return new WaitForSeconds(.3f);Capture("stage06-dialogue");yield return Hold(Key.LeftCtrl,8);
            stage.Open("games");yield return new WaitForSeconds(.2f);var explorer=app.Windows.GetComponentInChildren<Stage05ExplorerWindow>();
            Check(explorer&&explorer.CurrentFolderId=="games"&&explorer.Expanded.Contains("desktop"),"Stage06 게임 폴더 공통 트리·바탕화면 상위");
            yield return MouseClick(explorer.Window.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s6:card").Rect);yield return null;
            Check(explorer.CurrentFolderId=="card"&&app.Windows.GetComponentsInChildren<FakeWindow>().Length==1,"하위 게임 폴더 같은 창 이동");
            Check(explorer.Window.Content.GetComponentsInChildren<ScrollRect>().Length==2,"Stage06 트리·파일 독립 스크롤");
            var missing="card_data"+stage.Randoms.CardMissing.ToString("00");explorer.Navigate(stage.Randoms.CardLocation);yield return null;
            var source=explorer.Window.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s6:"+missing);
            var target=(RectTransform)explorer.Window.Content.Find("폴더 트리/보기 영역/목록/tree:card");yield return MouseDrag(Center(source.Rect),Center(target));yield return null;
            Check(stage.Files.Find(missing).parentFolder=="card"&&GameIntegrityChecker.Check(stage.Files,config,"card").Length==0,"트리 폴더 드롭으로 누락 파일 복구");stage.Move(missing,stage.Randoms.CardLocation);
            explorer.Navigate("rope_system");yield return null;Capture("stage06-explorer");app.CloseAllWindows();yield return null;
            stage.Open("card_exe");yield return new WaitForSeconds(.3f);Check(!app.Windows.GetComponentInChildren<CardGameWindow>()&&app.Windows.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("data 파일")),"누락 data 게임 실행 차단");Capture("stage06-integrity");app.CloseAllWindows();yield return null;
            stage.Move("card_bg01.xyz","desktop");Check(GameIntegrityChecker.Check(stage.Files,config,"card").Length==2,"현재 파일 위치 다중 그룹 오류");stage.Move("card_bg01.xyz","card_sound");
            stage.Move("card_data"+stage.Randoms.CardMissing.ToString("00"),"card");stage.Move("rope_data"+stage.Randoms.RopeMissing.ToString("00"),"rope");Check(GameIntegrityChecker.Check(stage.Files,config,"card").Length==0,"잘못 놓인 파일 복원 후 검사 통과");
            stage.OpenFolder("rope_system");yield return new WaitForSeconds(.3f);var folder=app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.GetComponent<Stage05ExplorerWindow>()?.CurrentFolderId=="rope_system");Check(!folder.Content.GetComponentsInChildren<FakeIcon>().Any(i=>i.Id=="s6:time"),"설정 파일 기본 숨김");yield return MouseClick((RectTransform)folder.Toolbar.Find("설정 · 숨김파일 표시"));yield return null;var timeIcon=folder.Content.GetComponentsInChildren<FakeIcon>().Single(i=>i.Id=="s6:time");Check(timeIcon.GetComponent<CanvasGroup>().alpha==.5f,"설정 숨김 파일 반투명");yield return MouseClick(timeIcon.Rect);yield return new WaitForSeconds(.3f);
            var note=app.Windows.GetComponentInChildren<NotepadWindow>();var input=note.GetComponentInChildren<TMP_InputField>();input.text=config.initialText.Replace("play_time = 1","play_time = 60");note.GetComponent<FakeWindow>().Close();yield return null;Check(stage.Files.Find("time").content==config.initialText,"미저장 편집 폐기");stage.Open("time");yield return null;note=app.Windows.GetComponentInChildren<NotepadWindow>();input=note.GetComponentInChildren<TMP_InputField>();input.text=config.initialText.Replace("play_time = 1","play_time = 9999");note.SaveDialog();Check(stage.Files.Find("time").content.Contains("play_time = 999"),"저장 시 보정 반영");app.CloseAllWindows();yield return null;
            stage.Files.Find("time").content=config.initialText;stage.Open("rope_exe");yield return new WaitForSeconds(.3f);var rope=app.Windows.GetComponentInChildren<RopeGameWindow>();rope.StartGame();rope.Jump();rope.Tick(1.01f);Check(!rope.Running&&rope.Successes<10,"기본 1초 제한으로 클리어 불가");Capture("stage06-rope");app.CloseAllWindows();yield return null;stage.Talk();Check(app.Dialogue.CurrentSequence=="S6_TALK_2","게임 진입 후 두 번째 대화");yield return Hold(Key.LeftCtrl,8);
            stage.Files.Find("time").content=config.initialText.Replace("player = P1","player = p2").Replace("player_age = 10","player_age=12");stage.Open("rope_exe");yield return new WaitForSeconds(.3f);Check(!app.Windows.GetComponentInChildren<RopeGameWindow>()&&app.Windows.GetComponentInChildren<ImageViewer>(),"특별 조건은 이미지 전용·게임 실행 없음");Check(!app.Audio.MusicPlaying&&!stage.Ready,"특별 이미지 BGM 일시정지·조작 잠금");
            var special=app.Windows.GetComponentInChildren<ImageViewer>();var origin=special.Window.Rect.anchoredPosition;
            yield return MouseClick((RectTransform)special.Window.transform.Find("제목 표시줄/X"));
            yield return MouseDrag(Center((RectTransform)special.Window.transform.Find("제목 표시줄")),Center((RectTransform)special.Window.transform.Find("제목 표시줄"))+new Vector2(80,40));
            yield return Press(Key.Space);Check(special&&special.Window.gameObject.activeInHierarchy&&special.Window.Rect.anchoredPosition==origin,"특별 이미지 X·드래그·Space 무반응");
            Capture("stage06-special");yield return new WaitForSeconds(config.easterStillSeconds+config.easterSeconds+.3f);
            Check(stage.Ready&&app.Audio.MusicPlaying&&!special&&UnityEngine.EventSystems.EventSystem.current&&UnityEngine.EventSystems.EventSystem.current.enabled,"연출 종료 이미지 정리·BGM·UI 입력 복구");
            Check(new SaveService().Stage06SpecialSeen&&stage.Files.Find("time").content==config.initialText,"특별 이벤트 현재 진행 기록 저장·기본 텍스트 복원");
            stage.Open("time");yield return null;Check(app.Windows.GetComponentInChildren<NotepadWindow>().GetComponentInChildren<TMP_InputField>().text==config.initialText,"재열기 설정 기본값 표시");app.CloseAllWindows();yield return null;
            stage.SaveText(stage.Files.Find("time"),config.initialText.Replace("player = P1","player = P2").Replace("player_age = 10","player_age = 12"));app.CloseAllWindows();yield return null;stage.Open("rope_exe");yield return null;
            Check(app.Windows.GetComponentInChildren<RopeGameWindow>()&&!app.Windows.GetComponentInChildren<ImageViewer>()&&stage.Ready,"특수 값 재저장 후 재발동 없이 일반 게임 실행");app.CloseAllWindows();yield return null;
            string[][] orders={new[]{"card","rope","snake"},new[]{"card","snake","rope"},new[]{"rope","card","snake"},new[]{"rope","snake","card"},new[]{"snake","card","rope"},new[]{"snake","rope","card"}};
            for(int order=0;order<orders.Length;order++)
            {
                app.Begin(false);yield return new WaitForSeconds(.3f);stage=app.ScreenRoot.GetComponentInChildren<Stage06Controller>();Check(!app.Save.Stage06SpecialSeen&&!new SaveService().Stage06SpecialSeen,"이어하기 이벤트 열람 기록 초기화");Check(stage.stuck.Count==0&&!stage.BookComplete&&!stage.BlockSeen&&!stage.PlayedGame&&stage.Scores.Rows(2).Count==0,"이어하기 Stage06 전체 초기화 "+order);
                stage.Move("card_data"+stage.Randoms.CardMissing.ToString("00"),"card");stage.Move("rope_data"+stage.Randoms.RopeMissing.ToString("00"),"rope");stage.Files.Find("time").content=config.initialText.Replace("play_time = 1","play_time = 60");
                foreach(string game in orders[order])
                {
                    stage.Open(game+"_exe");yield return new WaitForSeconds(.25f);
                    if(game=="card")
                    {
                        var card=app.Windows.GetComponentInChildren<CardGameWindow>();card.StartGame(2);Check(!card.AcceptsCards&&!card.Previewing,"카드 시작 전 입력 잠금");card.Flip(0);var layout=card.Cards.ToArray();yield return MouseClick((RectTransform)card.GetComponentsInChildren<Button>().Single(b=>b.name=="게임 시작").transform);Check(card.Previewing&&!card.AcceptsCards,"시작 클릭 후 앞면 공개·입력 잠금");card.BeginPreview();card.Flip(0);if(order==0)Capture("stage06-card-preview");yield return new WaitForSeconds(config.cardPreviewSeconds+.1f);Check(!card.Previewing&&card.AcceptsCards&&card.Cards.SequenceEqual(layout),"공개 종료 입력 허용·배치 유지");if(order==0)Capture("stage06-card");foreach(var group in card.Cards.Select((v,i)=>(v,i)).GroupBy(x=>x.v)){var pair=group.Select(x=>x.i).ToArray();card.Flip(pair[0]);card.Flip(pair[1]);yield return null;}
                        Check(card.Cleared&&stage.Files.Find("A").available&&app.Save.Unlocks.stage06StarObtained,"HARD 보상 A·별 영구 해금");var initials=app.Windows.GetComponentsInChildren<ScoreInitialsInput>().Single();initials.Input.text="ABC";initials.Confirm();Check(stage.Scores.Rows(2).Count==1&&stage.Scores.Text(2).Contains("1     ZZZ     00:00:01"),"이니셜 등록·ZZZ 1위 잠금");
                    }
                    else if(game=="rope")
                    {
                        rope=app.Windows.GetComponentInChildren<RopeGameWindow>();rope.StartGame();for(int i=0;i<10;i++){rope.Tick(.4f);rope.Jump();rope.Tick(.201f);}Check(rope.Successes==10&&stage.Files.Find("B").available,"설정 수정 후 줄넘기 10회 B 보상");
                    }
                    else
                    {
                        var snake=app.Windows.GetComponentInChildren<SnakeGameWindow>();snake.Window.transform.SetAsFirstSibling();int guard=0;
                        while(snake.Running&&snake.Apples<10&&guard++<4000){var next=SnakeNext(snake,config);snake.Turn(next-snake.Snake[0]);snake.Step();}
                        Check(snake.Apples==10,"큰 논리 판 지렁이 사과 10개 도달");if(order==0){snake.GetComponent<ResizableFakeWindow>().Resize(Vector2.one,new Vector2(900,-900));yield return null;Capture("stage06-snake");}yield return new WaitForSeconds(.9f);Check(stage.Files.Find("C").available,"뱉기 후 C 보상");
                    }
                    app.CloseAllWindows();yield return null;
                }
                stage.Open("book");yield return new WaitForSeconds(.2f);stage.Stick("star","A");Check(!app.Save.Stage06SpecialSeen&&!new SaveService().Stage06SpecialSeen,"이어하기 이벤트 열람 기록 초기화");Check(stage.stuck.Count==0,"별 스티커 슬롯 거절");foreach(string previewId in new[]{"A","B","C"})stage.Open(previewId);yield return new WaitForSeconds(.2f);
                stage.Stick("A","B");Check(app.Windows.GetComponentsInChildren<ImageViewer>().Count(v=>v.Definition.id=="s6:A"&&!v.Window.IsClosing)==1,"잘못된 스티커 드롭은 뷰어 유지");
                foreach(string id in new[]{"A","B","C"}){stage.Stick(id,id);Check(!app.Windows.GetComponentsInChildren<ImageViewer>().Any(v=>v.Definition.id=="s6:"+id&&!v.Window.IsClosing),"소비된 스티커 뷰어 닫기 "+id);Check(app.Windows.GetComponentsInChildren<ImageViewer>().Any(v=>v.Definition.id=="s6:book"&&!v.Window.IsClosing)&&new[]{"A","B","C"}.Where(other=>!stage.stuck.Contains(other)).All(other=>app.Windows.GetComponentsInChildren<ImageViewer>().Any(v=>v.Definition.id=="s6:"+other&&!v.Window.IsClosing)),"스티커북과 미사용 스티커 뷰어 유지 "+id);if(!stage.BookComplete){var book=app.Windows.GetComponentInChildren<StickerBookWindow>();var slot=book.Viewer.PictureRoot.Find("slot:"+id);Check(slot&&slot.localScale==Vector3.one&&slot.GetComponentInChildren<RawImage>(),"스티커 원래 슬롯 위치 즉시 덮기 "+id);if(order==0){yield return null;Capture("stage06-sticker-"+id);}}}yield return new WaitForSeconds(.35f);Check(stage.BookComplete&&stage.Files.Find("book").displayName=="스티커북.exe"&&new[]{"A","B","C"}.All(id=>!stage.Files.Find(id).available),"6가지 순서 스티커북 완성 "+order);if(order==0)Capture("stage06-book");app.CloseAllWindows();yield return null;
            }
            stage.Open("book");Check(app.Dialogue.CurrentSequence=="S6_BLOCK"&&stage.BlockSeen,"첫 exe 방해 이벤트");yield return Hold(Key.LeftCtrl,8);yield return new WaitForSeconds(1.2f);stage.Open("book");yield return new WaitForSeconds(1.5f);var storm=stage.GetComponent<PopupStorm>();Check(storm.Popups.Count==10&&storm.Popups.Count(p=>p.Answer)==1,"팝업 10개 정답 1개");Capture("stage06-popup");
            var answer=storm.Popups.Single(p=>p.Answer);var answerPosition=answer.Window.Rect.anchoredPosition;for(int i=0;i<20;i++){var normal=storm.Popups.First(p=>!p.Answer);if(i%2==0){Check(normal.Confirm.interactable,"일반 오류 확인 활성화");normal.Confirm.onClick.Invoke();}else normal.Window.transform.Find("제목 표시줄/X").GetComponent<Button>().onClick.Invoke();yield return null;Check(storm.Popups.Count==10&&answer.Window.Rect.anchoredPosition==answerPosition,"팝업 닫기 재생성·정답 유지 "+i);}
            for(int i=0;i<3;i++)
            {
                yield return new WaitForSecondsRealtime(.2f); // Wait for the shared window opening animation before clicking its moving X.
                var oldAnswer=answer;var oldPosition=answer.Window.Rect.anchoredPosition;answer.Window.Focus();
                var closeAnswer=answer.Window.transform.Find("제목 표시줄/X").GetComponent<Button>();Check(closeAnswer.interactable,"정답 팝업 X 활성화");
                yield return MouseClick((RectTransform)closeAnswer.transform);yield return null;
                answer=storm.Popups.Single(p=>p.Answer);
                Check(storm.Popups.Count==10&&answer!=oldAnswer&&answer.Window.Rect.anchoredPosition!=oldPosition&&app.Stages.Current.stageId=="Stage06","정답 X 실제 클릭·다른 위치 재생성·정답 보존");
            }
            answer.Window.Focus();answer.Confirm.onClick.Invoke();yield return new WaitForSeconds(14);Check(app.Stages.Current.stageId=="Stage07"&&app.Save.Progress.cleared.Contains("Stage06"),"Stage06 CMD 클리어 저장·다음 장 연결");
        }
        Vector2Int SnakeNext(SnakeGameWindow snake,Stage06Config c)
        {
            var start=snake.Snake[0];var queue=new Queue<Vector2Int>();queue.Enqueue(start);var parent=new Dictionary<Vector2Int,Vector2Int>();parent[start]=start;
            while(queue.Count>0){var p=queue.Dequeue();if(p==snake.Apple){while(parent[p]!=start)p=parent[p];return p;}foreach(var d in new[]{Vector2Int.right,Vector2Int.down,Vector2Int.left,Vector2Int.up}){var n=p+d;if(n.x<0||n.y<0||n.x>=c.snakeCols||n.y>=c.snakeRows||parent.ContainsKey(n)||snake.Snake.Skip(1).Take(snake.Snake.Count-2).Contains(n))continue;parent[n]=p;queue.Enqueue(n);}}
            throw new Exception("지렁이 QA 경로 없음");
        }
    }
}
#endif
