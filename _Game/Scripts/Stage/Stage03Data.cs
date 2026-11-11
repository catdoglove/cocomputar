using System;
using System.Collections.Generic;
using UnityEngine;
namespace CoComputar
{
    public enum LensFilter { Neutral,Red,Blue,Yellow,Green }
    [Serializable] public class DesktopLayout { public string id,name,sprite,kind;public float x,y;public Rect Bounds=>new Rect(x-8,y-8,116,128); }
    [Serializable] public class TrailData
    {
        public Vector2[] points;public float startHold=3,holdSeconds=1,decayPerSecond=.5f,stillSpeed=24;
        public float maxGap=80,margin=64,rotationOffset=-90;public int maxAttempts=512;
        public Rect reservedWindow=new Rect(450,260,330,275);
        [NonSerialized] public Vector2 goal;[NonSerialized] public bool usedFallback;
        static Vector2[] DefaultRoute()=>new[]{new Vector2(100,660),new Vector2(100,510),new Vector2(180,420),new Vector2(350,420),new Vector2(350,200),new Vector2(650,200),new Vector2(900,200),new Vector2(900,500),new Vector2(900,660)};
        public bool Safe(Vector2 p)
        {
            const float radius=30;var bounds=new Rect(p.x-radius,p.y-radius,2*radius,2*radius);
            return bounds.xMin>=margin&&bounds.xMax<=1024-margin&&bounds.yMin>=38+margin&&bounds.yMax<=768-margin&&!bounds.Overlaps(reservedWindow);
        }
        bool Sample(Vector2[] route,out List<Vector2> result)
        {
            result=new List<Vector2>();if(route==null||route.Length<2)return false;int attempts=0;
            for(int i=0;i<route.Length;i++)
            {
                if(!float.IsFinite(route[i].x)||!float.IsFinite(route[i].y))return false;
                float distance=i==0?0:Vector2.Distance(route[i-1],route[i]);if(distance/maxGap>maxAttempts)return false;
                int steps=i==0?1:Mathf.Max(1,Mathf.CeilToInt(distance/maxGap));
                for(int n=1;n<=steps;n++){if(++attempts>maxAttempts)return false;var p=i==0?route[0]:Vector2.Lerp(route[i-1],route[i],n/(float)steps);if(!Safe(p))return false;if(result.Count==0||Vector2.Distance(result[result.Count-1],p)>.01f)result.Add(p);}
            }
            return result.Count>=2;
        }
        public void Build()
        {
            maxGap=float.IsFinite(maxGap)?Mathf.Clamp(maxGap,8,120):80;margin=float.IsFinite(margin)?Mathf.Clamp(margin,0,300):64;maxAttempts=Mathf.Clamp(maxAttempts,2,4096);
            if(!Sample(points,out var route))
            {
                usedFallback=true;
                // A bounded deterministic fallback, never random retries or an unreachable puzzle.
                if(!Sample(DefaultRoute(),out route)){margin=64;reservedWindow=new Rect(450,260,330,275);maxAttempts=512;Sample(DefaultRoute(),out route);}
            }
            goal=route[route.Count-1];route.RemoveAt(route.Count-1);points=route.ToArray();
        }
        public float Angle(int index){var next=index+1<points.Length?points[index+1]:goal;var d=next-points[index];return Mathf.Atan2(-d.y,d.x)*Mathf.Rad2Deg+rotationOffset;}
    }
    [Serializable] public class LensClue {public string text,filter;public float x,y;}
    [Serializable] public class FilmCard {public string id,displayName,realName,symbol,filter;public float x,y;}
    [Serializable] public class LensTint {public string filter,color;}
    [Serializable] public class Stage03Data
    {
        public int secretYear=1990,secretMonth=2,secretDay=12;public LensTint[] filterColors;
        public FileRecord[] secretFiles;public int[] cardDisplayOrder={2,0,1,3};public string[] pastIconExceptions={"computer"};public DesktopLayout[] layout;public TrailData trail;public FilmCard[] cards;public LensClue[] dateParts;
        public string pastImage,visionImage,npc,youngNpc,music,murmur,denied,filterSound,footstep,installTick,ding,wrong,unlock,warp,pop,filmName,updatedFilmName,deniedMessage;
        public float visionSeconds=18,installSeconds=4;
    }
    public sealed class Stage03Randomizer
    {
        public string Password{get;private set;}public readonly List<Rect> Names=new();
        public static List<Rect> Exclusions(Stage03Data data)
        {
            var avoid=new List<Rect>();foreach(var icon in data.layout)avoid.Add(icon.Bounds);
            foreach(var point in data.trail.points)avoid.Add(new Rect(point.x-40,point.y-40,80,80));var end=data.trail.goal;avoid.Add(new Rect(end.x-50,end.y-30,110,110));return avoid;
        }
        public Stage03Randomizer(Stage03Data data)
        {
            var digits=new List<int>();for(int i=0;i<10;i++)digits.Add(i);
            for(int i=0;i<4;i++){int n=UnityEngine.Random.Range(0,digits.Count);Password+=digits[n].ToString();digits.RemoveAt(n);}
            var avoid=Exclusions(data);var slots=new List<Rect>();
            for(int y=180;y<700;y+=45)for(int x=160;x<860;x+=45)
            {var r=new Rect(x,y,138,34);if(!avoid.Exists(a=>a.Overlaps(r)))slots.Add(r);}
            while(Names.Count<4&&slots.Count>0){int n=UnityEngine.Random.Range(0,slots.Count);var r=slots[n];Names.Add(r);slots.RemoveAll(a=>a.Overlaps(new Rect(r.x-8,r.y-8,r.width+16,r.height+16)));}
            if(Names.Count!=4)throw new InvalidOperationException("숨김 이름 후보 공간이 부족합니다. DesktopLayout과 TrailData를 조정하세요.");
        }
    }
}
