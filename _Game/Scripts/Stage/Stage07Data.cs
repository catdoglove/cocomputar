using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CoComputar
{
    [Serializable] public class SetupWizardData { public string id,name,installer,serialColor;public bool serial;public string[] files; }
    [Serializable] public class ComponentRule { public string id,label,description;public bool required; }
    [Serializable] public class MapPinData { public string label,start;public float x,y; }
    [Serializable] public class MapExitData { public string label,target; }
    [Serializable] public class MapRegionData { public string id,name,objectType;public MapExitData[] exits; }
    [Serializable] public class Stage07Config
    {
        public string bgm,doorEventTrigger,rockMovedEvent,mapImage,browserAddress;public string[] keyPrograms,secretBrowserPages;
        public float installSeconds;public int failPercent,rotationMax,visibleDistance;public bool requireInternet;
        public SetupWizardData[] programs;public ComponentRule[] components;public MapPinData[] pins;public MapRegionData[] regions;
        public static Stage07Config Load()=>JsonUtility.FromJson<Stage07Config>(Resources.Load<TextAsset>("Stage07Config").text);
        public string ComponentError(IEnumerable<string> choices)
        {
            var set=new HashSet<string>(choices);if(set.Count==0)return "구성요소_없음";
            if(set.Any(id=>!components.Any(c=>c.id==id&&c.required)))return "설치_충돌";
            return components.Where(c=>c.required).All(c=>set.Contains(c.id))?null:"설치_누락";
        }
    }
    [Serializable] public class Stage07TextEntry { public string id,text; }
    [Serializable] public class Stage07TextData { public Stage07TextEntry[] entries; }
    public sealed class Shape3DLetter { public char Letter;public int Shape,X,Y,Slot;public string Color; }
    public sealed class Stage07Randomizer
    {
        public readonly List<Shape3DLetter> Letters=new();public readonly Dictionary<string,string> Numbers=new();
        public Stage07Randomizer(Stage07Config data,int? seed=null)
        {
            var rng=new System.Random(seed??Guid.NewGuid().GetHashCode());var chars="ABCDEFGHIJKLMNOPQRSTUVWXYZ".OrderBy(_=>rng.Next()).ToArray();int cursor=0;
            var counts=new[]{8,9,9}.OrderBy(_=>rng.Next()).ToArray();
            for(int shape=0;shape<3;shape++)
            {
                int side=Math.Max(3,data.rotationMax+1);var coords=Enumerable.Range(0,side*side).OrderBy(_=>rng.Next()).ToList();
                for(int slot=0;slot<counts[shape];slot++)
                {
                    int coord=slot<2?coords.First(c=>c%side>data.visibleDistance||c/side>data.visibleDistance):coords[0];coords.Remove(coord);
                    Letters.Add(new Shape3DLetter{Letter=chars[cursor++],Shape=shape,X=coord%side,Y=coord/side,Slot=slot,Color=slot==0?"blue":slot==1?"green":rng.Next(2)==0?"red":"orange"});
                }
            }
            var values=Enumerable.Range(0,1000).OrderBy(_=>rng.Next()).Take(data.keyPrograms.Length).ToArray();
            for(int i=0;i<values.Length;i++)Numbers[data.keyPrograms[i]]=values[i].ToString("000");
        }
        public string Serial(string id)=>string.Concat(Letters.Where(l=>l.Color==(id=="map"?"blue":"green")).OrderBy(l=>l.Shape).Select(l=>l.Letter))+"-"+Numbers[id=="map"?"map.exe":"인터넷"];
        public string KeyText=>string.Join("\n",Numbers.Select(n=>n.Key+" : "+n.Value))+"\ndoor.exe : ???";
        public IEnumerable<Shape3DLetter> Visible(int shape,int x,int y,int distance)=>Letters.Where(l=>l.Shape==shape&&Math.Max(Math.Abs(l.X-x),Math.Abs(l.Y-y))<=distance);
    }
    public sealed class InstallStateService
    {
        readonly HashSet<string> installed=new();public bool IsInstalled(string id)=>installed.Contains(id);
        public bool Complete(string id)=>installed.Add(id);
    }
}
