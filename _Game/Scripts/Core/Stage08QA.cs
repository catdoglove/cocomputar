#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
 public sealed partial class PlaythroughQA
 {
  IEnumerator Stage08Checks()
  {
   app.Save.NewGame("Stage08");app.Begin(false);yield return new WaitForSeconds(.4f);
   var s=app.ScreenRoot.GetComponentInChildren<Stage08Controller>();Check(s!=null,"Stage08 진입");
   Check(s.Files.Children("desktop").Count()==3&&!s.NpcCreated,"초기 파일 3개 + 공통 내 컴퓨터, NPC 없음");
   Check(s.Messages.Count(m=>m.folder=="inbox")==4&&s.Messages.Count(m=>m.folder=="sent")==2&&s.Messages.Count(m=>m.folder=="spam")==5&&s.Messages.Count(m=>m.folder=="trash")==2,"초기 메일 4/2/5/2");
   Check(Stage08Validation.CandyWays(s.Products,s.Rules.budget,s.Rules.maxQuantity)>0,"5000원 조합 존재");
   Check(s.Products.Where(p=>p.shop=="doll").All(p=>Stage08Validation.DollValid(p,s.Rules.startTokens)),"인형 가격 및 세금 규칙");
   s.Open("your");Check(!app.Windows.GetComponentInChildren<FakeBrowserWindow>(),"시크릿 미설치 차단");app.CloseAllWindows();yield return null;
   s.Open("my");yield return new WaitForSeconds(.3f);var b=app.Windows.GetComponentInChildren<FakeBrowserWindow>();Check(b&&b.Route=="my","my 브라우저 실행");Capture("stage08-home");
   foreach(var site in s.Sites.Where(site=>site.yourOnly)){b.Navigate(site.id+"/home");Check(b.Location=="notfound/home","my 전용 경로 차단 "+site.id);}
   b.Navigate("mart/home");b.Navigate("mart/sale");b.Travel(-1);Check(b.Location=="mart/home","뒤로 이동");b.Travel(1);Check(b.Location=="mart/sale","앞으로 이동");
   b.Travel(-1);b.Navigate("candy/home");b.Travel(1);Check(b.Location=="candy/home","새 탐색 미래 기록 제거");
   b.Navigate("candy/products");yield return new WaitForSeconds(.2f);Capture("stage08-candy");
   var cheap=s.Products.First(p=>p.shop=="candy"&&p.price==500);s.AddCart(cheap,1);s.Order("candy");Check(!s.Ordered,"미만 주문 차단");app.CloseAllWindows();yield return null;s.ClearCart("candy");
   var two=s.Products.First(p=>p.shop=="candy"&&p.price==2000);s.AddCart(two,1);s.AddCart(two,1);Check(!s.AddCart(two,1)&&s.CartTotal("candy")==4000,"초과 담기 차단");app.CloseAllWindows();yield return null;
   s.AddCart(cheap,1);s.AddCart(cheap,1);s.Order("candy");Check(s.Ordered&&s.CartTotal("candy")==0,"정확히 5000원 주문 완료");app.CloseAllWindows();yield return null;
   Check(s.Grant("plank")&&!s.Grant("plank"),"판자 1회 획득");s.Grant("paint");var zip=s.Files.Find("paint");float x=zip.x,y=zip.y;s.Extract("paint");Check(s.Files.Find("paint")==null&&s.Files.Find("colors").available&&s.Files.Find("colors").x==x&&s.Files.Find("colors").y==y,"압축 소모·같은 위치 폴더");
   Check(s.Files.CanUse(s.Files.Find("black")),"검정색 파일 접근 가능");s.Grant("handle");
   Check(!s.Send("wrong",s.Rules.autoTypeText,new[]{"plank","black","handle"}),"잘못된 주소 차단");app.CloseAllWindows();yield return null;
   Check(!s.Send(s.Rules.replyAddress,"",new[]{"plank","black","handle"}),"빈 본문 차단");app.CloseAllWindows();yield return null;
   Check(s.Send(s.Rules.replyAddress,s.Rules.autoTypeText,new[]{"plank"}),"오답 발송");yield return new WaitForSecondsRealtime(s.Rules.replySeconds+.2f);Check(s.Messages.Last().attachment==null,"오답 회신에는 문 없음");app.CloseAllWindows();yield return null;
   int count=s.Messages.Count;s.Send(s.Rules.replyAddress,s.Rules.autoTypeText,new[]{"plank","black","handle"});Check(s.Messages.Count==count+1,"회신 지연");yield return new WaitForSecondsRealtime(s.Rules.replySeconds+.2f);Check(s.Messages.Last().attachment=="door","정답 3파일 회신");app.CloseAllWindows();yield return null;
   Stage08MailWindow.Open(s);yield return new WaitForSeconds(.2f);Capture("stage08-mail");app.CloseAllWindows();yield return null;
   app.Save.Flag("stage07.secretBrowserInstalled");s.Open("your");yield return new WaitForSeconds(.2f);b=app.Windows.GetComponentInChildren<FakeBrowserWindow>();Check(b&&b.Route=="your","설치 후 your 실행");
   foreach(var site in s.Sites)foreach(var page in site.pages){b.Navigate(site.id+"/"+page.id);yield return null;Check(b!=null,"페이지 렌더 "+site.id+"/"+page.id);}
   app.CloseAllWindows();yield return null;
   var other=FakeBrowserWindow.Open(s,"my");var eyeBrowser=FakeBrowserWindow.Open(s,"your");
   s.BeginEyeEvent(eyeBrowser.Window);Check(s.Busy&&!s.Ready,"눈알 연출 입력 잠금");s.BeginEyeEvent(eyeBrowser.Window);
   yield return new WaitForSecondsRealtime(s.Rules.eyeSeconds+.2f);Check(s.EyeDone&&!s.Busy&&other&&!eyeBrowser,"눈알 1회 종료·해당 창만 닫힘");
   eyeBrowser=FakeBrowserWindow.Open(s,"your");eyeBrowser.Navigate("eye/home");Check(eyeBrowser.Location=="notfound/home","눈알 재방문 차단");
   app.CloseAllWindows();yield return null;s.Grant("head");s.Grant("body");s.Merge("head","body");yield return new WaitForSecondsRealtime(.8f);Check(s.NpcCreated&&!s.Files.Find("head").available&&!s.Files.Find("body").available,"두 부품 소모 NPC 생성");
   s.Grant("door");s.Open("door");yield return new WaitForSecondsRealtime(12);Check(app.Stages.Current.stageId=="Stage09","door CMD 다음 스테이지");
   app.Save.ResetStagePuzzles("Stage08");app.Save.Enter("Stage08");app.Begin(false);yield return new WaitForSeconds(.3f);s=app.ScreenRoot.GetComponentInChildren<Stage08Controller>();Check(s&&!s.Ordered&&!s.NpcCreated&&!s.EyeDone&&s.Acquired.Count==0&&s.Messages.Count==13,"재시작 진행 초기화");Check(app.Save.Progress.flags.Contains("stage08.npcCreated"),"선택 이력 보존");
   s.Open("mail");yield return null;
   var back=app.ScreenRoot.GetComponentsInChildren<UnityEngine.UI.Button>().Single(x=>x.GetComponentInChildren<TMPro.TMP_Text>()?.text=="테스트: 스테이지7로 돌아가기");
   yield return MouseClick((RectTransform)back.transform);yield return new WaitForSeconds(.3f);
   Check(app.Stages.Current.stageId=="Stage07"&&app.Save.Progress.stageId=="Stage07"&&!app.Windows.GetComponentInChildren<Stage08MailWindow>(),"테스트 버튼 Stage07 이동·메일 창 정리");
  }
 }
}
#endif
