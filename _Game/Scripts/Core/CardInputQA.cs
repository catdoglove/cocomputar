#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        IEnumerator CardInputChecks()
        {
            app.Save.NewGame("Stage06");app.Begin(false);yield return new WaitForSeconds(.4f);
            var stage=app.ScreenRoot.GetComponentInChildren<Stage06Controller>();stage.Move("card_data"+stage.Randoms.CardMissing.ToString("00"),"card");stage.Open("card_exe");yield return new WaitForSeconds(.3f);
            var card=app.Windows.GetComponentInChildren<CardGameWindow>();card.StartGame(2);yield return null;
            Button Card(int i)=>card.GetComponentsInChildren<Button>().Single(b=>b.name=="card:"+i);
            bool Locked(int i)=>!Card(i).interactable&&!Card(i).GetComponent<Image>().raycastTarget&&Card(i).transition==Selectable.Transition.None;
            var sounds=new List<string>();void Heard(string key)=>sounds.Add(key);app.Audio.Played+=Heard;
            Check(Enumerable.Range(0,card.Cards.Length).All(Locked),"시작 전 전체 카드 클릭·호버 차단");
            yield return MouseClick((RectTransform)Card(0).transform);card.Flip(0);Check(sounds.Count==0&&!card.Matched.Any(v=>v),"시작 전 클릭 무음·상태 유지");
            yield return MouseClick((RectTransform)card.GetComponentsInChildren<Button>().Single(b=>b.name=="게임 시작").transform);sounds.Clear();
            Check(card.Previewing&&Enumerable.Range(0,card.Cards.Length).All(Locked),"3초 공개 중 전체 카드 차단");yield return MouseClick((RectTransform)Card(0).transform);card.Flip(0);Check(sounds.Count==0,"공개 중 클릭 무음");
            yield return new WaitForSeconds(stage.Data.cardPreviewSeconds+.1f);Check(card.AcceptsCards&&Card(0).interactable,"공개 종료 미해결 카드 입력 허용");
            yield return MouseClick((RectTransform)Card(0).transform);Check(Locked(0)&&sounds.SequenceEqual(new[]{"sfx_card_flip"}),"첫 카드 선택·뒤집기음 한 번·재입력 차단");sounds.Clear();
            yield return MouseClick((RectTransform)Card(0).transform);card.Flip(0);Card(0).onClick.Invoke();Check(sounds.Count==0&&!card.Matched.Any(v=>v),"동일 첫 카드 재클릭·직접 호출 무음·자기 짝 금지");
            int wrong=Enumerable.Range(1,card.Cards.Length-1).First(i=>card.Cards[i]!=card.Cards[0]);yield return MouseClick((RectTransform)Card(wrong).transform);
            Check(sounds.SequenceEqual(new[]{"sfx_card_flip","sfx_card_mismatch"})&&Enumerable.Range(0,card.Cards.Length).All(Locked),"불일치 실패음 한 번·비교 중 입력 차단");
            yield return new WaitForSeconds(stage.Data.cardMismatchSeconds+.1f);Check(Card(0).interactable&&Card(wrong).interactable,"불일치 후 두 카드 입력 복원");
            int partner=Enumerable.Range(1,card.Cards.Length-1).First(i=>card.Cards[i]==card.Cards[0]);sounds.Clear();yield return MouseClick((RectTransform)Card(0).transform);yield return MouseClick((RectTransform)Card(partner).transform);
            Check(card.Matched[0]&&card.Matched[partner]&&Locked(0)&&Locked(partner),"일치 카드 앞면 고정·클릭/호버/레이캐스트 제거");Check(sounds.Count(k=>k=="sfx_card_match")==1,"일치 성공음 한 번");sounds.Clear();
            var faces=Card(0).GetComponentInChildren<TMPro.TMP_Text>().text;
            for(int i=0;i<3;i++){yield return MouseClick((RectTransform)Card(0).transform);yield return MouseClick((RectTransform)Card(partner).transform);}
            card.Flip(0);Card(partner).onClick.Invoke();Check(sounds.Count==0&&card.Matched.Count(v=>v)==2&&Card(0).GetComponentInChildren<TMPro.TMP_Text>().text==faces,"맞춘 카드 반복 클릭·직접 호출 무음·상태 불변");
            foreach(var pair in card.Cards.Select((v,i)=>(v,i)).Where(x=>!card.Matched[x.i]).GroupBy(x=>x.v))
            {
                var ids=pair.Select(x=>x.i).ToArray();
                var untouched=Enumerable.Range(0,card.Cards.Length).Where(i=>!card.Matched[i]&&!ids.Contains(i)).ToArray();
                var before=untouched.Select(i=>Card(i).targetGraphic.canvasRenderer.GetColor()).ToArray();
                card.Flip(ids[0]);card.Flip(ids[1]);
                bool stable=true;
                for(float t=0;t<.2f;t+=Time.unscaledDeltaTime)
                {
                    for(int j=0;j<untouched.Length;j++)if(Card(untouched[j]).targetGraphic.canvasRenderer.GetColor()!=before[j])stable=false;
                    yield return null;
                }
                if(untouched.Length>0)Check(stable,"짝 성공 직후 0.2초 미선택 뒷면 색상 유지");
            }
            Check(card.Cleared&&card.Matched.All(v=>v)&&Enumerable.Range(0,card.Cards.Length).All(Locked)&&stage.Files.Find("A").available,"전체 일치 클리어·보상·모든 카드 입력 차단");Check(sounds.Count(k=>k=="sfx_card_clear")==1,"최종 클리어음 한 번");
            sounds.Clear();card.Flip(0);Card(0).onClick.Invoke();Check(sounds.Count==0,"클리어 후 재호출 무음");
            var parent=card.GetComponent<FakeWindow>();
            FakeWindow Record()=>app.Windows.GetComponentsInChildren<FakeWindow>().Single(w=>w.Key=="s6:initials");
            void OpenRecord()=>typeof(CardGameWindow).GetMethod("Initials",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(card,null);
            var record=Record();Check(record.ParentWindow==parent,"기록 등록창 부모 연결");
            yield return new WaitForSeconds(.2f);
            yield return MouseClick((RectTransform)record.transform.Find("제목 표시줄/X"));
            Check(parent&&parent.gameObject.activeInHierarchy&&!app.Windows.GetComponentsInChildren<ScoreInitialsInput>().Any(),"기록창 취소 시 게임 유지");
            OpenRecord();record=Record();record.GetComponent<ScoreInitialsInput>().Confirm();yield return null;
            Check(parent&&parent.gameObject.activeInHierarchy&&!app.Windows.GetComponentsInChildren<ScoreInitialsInput>().Any(),"등록 완료 시 게임 유지");
            OpenRecord();record=Record();var order=new List<string>();record.Closed+=()=>{order.Add("child");record.Close();};parent.Closed+=()=>order.Add("parent");
            parent.SetParentWindow(record);Check(!parent.ParentWindow,"부모 자식 순환 연결 차단");
            parent.Focus();yield return new WaitForSeconds(.2f);sounds.Clear();
            yield return MouseClick((RectTransform)parent.transform.Find("제목 표시줄/X"));
            Check(order.SequenceEqual(new[]{"child","parent"})&&!app.Windows.GetComponentsInChildren<ScoreInitialsInput>().Any(),"부모 X 자식 먼저 종료·중복 닫기 무시");
            Check(sounds.Count(k=>k=="sfx_window_close")==2,"두 창 닫기 효과음 각각 한 번");
            app.Audio.Played-=Heard;app.CloseAllWindows();yield return null;stage.Open("card_exe");yield return new WaitForSeconds(.2f);card=app.Windows.GetComponentInChildren<CardGameWindow>();Check(!card.AcceptsCards&&!card.Previewing&&!card.Matched.Any(v=>v),"재실행 시작 대기·카드 초기화");Capture("card-input-reset");
            OpenRecord();app.CloseAllWindows();yield return null;Check(!app.Windows.GetComponentsInChildren<FakeWindow>().Any(),"전체 창 닫기 자식 포함 정리");
            stage.Open("card_exe");yield return new WaitForSeconds(.2f);card=app.Windows.GetComponentInChildren<CardGameWindow>();OpenRecord();app.ResetScreen();yield return null;Check(!app.Windows.GetComponentsInChildren<FakeWindow>().Any(),"스테이지 화면 초기화 자식 포함 정리");
        }
    }
}
#endif
