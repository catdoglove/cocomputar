using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    public sealed class DateTimeWindow:MonoBehaviour
    {
        DateTime draftBase,draftAnchor;
        DateTime draft{get=>draftBase+(DateTime.UtcNow-draftAnchor);set{draftBase=value;draftAnchor=DateTime.UtcNow;}}
        Func<bool> locked;bool Locked=>locked?.Invoke()==true;
        DateTime drawnDate;Action<DateTime> apply;FakeWindow window;RectTransform calendar;TextMeshProUGUI year,time,month;
        RectTransform hourHand,minuteHand,secondHand;GameObject months,years;ScrollRect yearScroll;
        public static void Open(Action<DateTime> apply,DateTime value,Func<bool> locked=null)
        {
            if(!FakeWindow.TryCreate(GameApp.Instance.Windows,"date-time","날짜 및 시간 속성",out var w,205,140,620,465))return;
            var self=w.gameObject.AddComponent<DateTimeWindow>();self.window=w;self.draft=value;self.apply=apply;self.locked=locked;
            UIFactory.Text(w.Content,"날짜",12,5,250,30);UIFactory.Text(w.Content,"시간",330,5,200,30);
            self.month=UIFactory.Button(w.Content,"",15,42,125,32,self.MonthMenu).GetComponentInChildren<TextMeshProUGUI>();
            self.year=UIFactory.Button(w.Content,"",155,42,125,32,self.YearMenu).GetComponentInChildren<TextMeshProUGUI>();
            self.calendar=UIFactory.Rect(w.Content,"달력",15,95,275,235);
            var face=UIFactory.Panel(w.Content,"아날로그 시계",352,45,180,180,new Color(.7f,.73f,.74f));
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;UIFactory.Panel(face.transform,"눈금",88+Mathf.Sin(a)*78,88-Mathf.Cos(a)*78,4,4,Color.white).raycastTarget=false;}
            self.hourHand=self.Hand(face.transform,"시침",49,4,Color.cyan);self.minuteHand=self.Hand(face.transform,"분침",69,3,Color.white);self.secondHand=self.Hand(face.transform,"초침",73,1,Color.gray);
            self.time=UIFactory.Text(w.Content,"",340,235,220,32,24);
            string[] labels={"시","분","초"};for(int i=0;i<3;i++){int part=i;float x=330+i*80;UIFactory.Text(w.Content,labels[i],x,275,70,25,17);UIFactory.Button(w.Content,"+",x,305,32,28,()=>self.ChangeTime(part,1));UIFactory.Button(w.Content,"−",x+35,305,32,28,()=>self.ChangeTime(part,-1));}
            UIFactory.Text(w.Content,"게임 안의 날짜와 시간만 변경합니다.",15,340,550,26,17);
            UIFactory.Button(w.Content,"확인",225,375,105,32,()=>{if(!self.Locked){apply(self.draft);w.Close();}});UIFactory.Button(w.Content,"취소",340,375,105,32,w.Close);UIFactory.Button(w.Content,"적용",455,375,105,32,()=>{if(!self.Locked)apply(self.draft);});self.Draw();
        }
        RectTransform Hand(Transform parent,string name,float length,float width,Color color){var r=UIFactory.Panel(parent,name,90,90,width,length,color).rectTransform;r.pivot=new Vector2(.5f,0);r.GetComponent<Image>().raycastTarget=false;return r;}
        void YearMenu()
        {
            if(Locked)return;
            if(years){Destroy(years);return;}
            if(months)Destroy(months);
            years=UIFactory.Panel(window.Content,"년도 선택",155,76,145,208,UIFactory.Theme.paper).gameObject;
            var viewport=UIFactory.Panel(years.transform,"년도 목록",2,2,116,204,UIFactory.Theme.paper).rectTransform;viewport.gameObject.AddComponent<RectMask2D>();
            int first=Math.Max(2100,draft.Year),last=Math.Min(1900,draft.Year),count=first-last+1;
            var content=UIFactory.Rect(viewport,"년도 항목",0,0,116,count*29);
            yearScroll=years.AddComponent<ScrollRect>();yearScroll.viewport=viewport;yearScroll.content=content;yearScroll.horizontal=false;yearScroll.movementType=ScrollRect.MovementType.Clamped;yearScroll.scrollSensitivity=29;
            for(int y=first;y>=last;y--){int selected=y;var b=UIFactory.Button(content,y+"년",0,(first-y)*29,116,29,()=>{Destroy(years);SetDate(selected,draft.Month,draft.Day);});b.GetComponentInChildren<TextMeshProUGUI>().fontSize=20;}
            var track=UIFactory.Panel(years.transform,"년도 스크롤",120,27,23,154,UIFactory.Theme.paper).rectTransform;
            var handle=UIFactory.Panel(track,"스크롤 손잡이",0,0,23,24,UIFactory.Theme.pink).rectTransform;
            handle.anchorMin=Vector2.zero;handle.anchorMax=Vector2.one;handle.offsetMin=handle.offsetMax=Vector2.zero;
            var bar=track.gameObject.AddComponent<Scrollbar>();bar.handleRect=handle;bar.targetGraphic=handle.GetComponent<Image>();bar.direction=Scrollbar.Direction.BottomToTop;
            yearScroll.verticalScrollbar=bar;
            UIFactory.Button(years.transform,"▲",120,2,23,23,()=>ScrollYears(-1));UIFactory.Button(years.transform,"▼",120,183,23,23,()=>ScrollYears(1));
            Canvas.ForceUpdateCanvases();yearScroll.verticalNormalizedPosition=1-Mathf.Clamp01((first-draft.Year)*29f/Mathf.Max(1,count*29-204));
        }
        void ScrollYears(int direction){if(yearScroll)yearScroll.verticalNormalizedPosition-=direction*29/Mathf.Max(1,yearScroll.content.rect.height-yearScroll.viewport.rect.height);}
        void Update(){if(Locked){if(years)Destroy(years);if(months)Destroy(months);}foreach(var button in window.Content.GetComponentsInChildren<Button>())button.interactable=!Locked||button.GetComponentInChildren<TextMeshProUGUI>()?.text=="취소";var now=draft;if(now.Date!=drawnDate)Draw();else DrawClock(now);}
        void DrawClock(DateTime now)
        {
            time.text=now.ToString("HH:mm:ss");
            double seconds=now.TimeOfDay.TotalSeconds;
            hourHand.localRotation=Quaternion.Euler(0,0,-(float)(seconds/120%360));
            minuteHand.localRotation=Quaternion.Euler(0,0,-(float)(seconds/10%360));
            secondHand.localRotation=Quaternion.Euler(0,0,-now.Second*6);
        }
        void SetDate(int y,int m,int d){if(Locked)return;draft=new DateTime(y,m,Math.Min(d,DateTime.DaysInMonth(y,m))).Add(draft.TimeOfDay);Draw();}
        void ChangeTime(int part,int delta){if(Locked)return;draft=part==0?draft.AddHours(delta):part==1?draft.AddMinutes(delta):draft.AddSeconds(delta);Draw();}
        void MonthMenu()
        {
            if(Locked)return;
            if(months){Destroy(months);return;}if(years)Destroy(years);months=UIFactory.Panel(window.Content,"월 선택",15,78,270,160,UIFactory.Theme.paper).gameObject;
            for(int i=1;i<=12;i++){int m=i;UIFactory.Button(months.transform,i+"월",(i-1)%3*90,(i-1)/3*40,87,38,()=>{Destroy(months);SetDate(draft.Year,m,draft.Day);});}
        }
        void Draw()
        {
            drawnDate=draft.Date;year.text=draft.Year+"년 ▼";month.text=draft.Month+"월 ▼";DrawClock(draft);UIFactory.Clear(calendar);
            string[] names={"일","월","화","수","목","금","토"};for(int i=0;i<7;i++)UIFactory.Text(calendar,names[i],i*38,0,35,25,18);
            int start=(int)new DateTime(draft.Year,draft.Month,1).DayOfWeek;
            for(int day=1;day<=DateTime.DaysInMonth(draft.Year,draft.Month);day++){int selected=day,index=start+day-1;var b=UIFactory.Button(calendar,day.ToString(),index%7*38,30+index/7*32,34,29,()=>SetDate(draft.Year,draft.Month,selected),day==draft.Day?UIFactory.Theme.pink:UIFactory.Theme.paper);b.GetComponentInChildren<TextMeshProUGUI>().fontSize=17;}

        }
    }
}
