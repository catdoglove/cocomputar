using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
namespace CoComputar
{
    [Serializable] public class IntegrityGroup{public string game,name,folder;public string[] files;}
    [Serializable] public class TimePropertyRule{public string key,kind,value;public string[] allowed;public int min,max;public bool clamp;}
    [Serializable] public class EasterEggRule{public string key,value;}
    [Serializable] public class StickerSlot{public string id;public float x,y,width,height;}
    [Serializable] public class Stage06Config
    {
        public int cardCount,ropeCount,ropeGoal,snakeCols,snakeRows,snakeInitialCols,snakeInitialRows,snakeGoal,popupCount;
        public float cardPreviewSeconds=3,easterStillSeconds=2;
        public int[] cardPairs;public float starTimeLimitSec,cardMismatchSeconds,ropePeriod,jumpSeconds,snakeCell,snakeStep,easterSeconds,popupDelay,popupStepX,popupStepY;
        public string initialText,bgm;public IntegrityGroup[] groups;public TimePropertyRule[] schema;public EasterEggRule[] easterConditions;public StickerSlot[] slots;
    }
    public sealed class Stage06Randomizer
    {
        public readonly int CardMissing,RopeMissing,DoorIndex;public readonly string CardLocation,RopeLocation;
        public Stage06Randomizer(Stage06Config c){CardMissing=UnityEngine.Random.Range(1,c.cardCount+1);RopeMissing=UnityEngine.Random.Range(1,c.ropeCount+1);CardLocation=new[]{"card_sound","card_picture"}[UnityEngine.Random.Range(0,2)];RopeLocation=new[]{"rope_sound","rope_picture","rope_system"}[UnityEngine.Random.Range(0,3)];DoorIndex=UnityEngine.Random.Range(3,Mathf.Min(7,c.popupCount));}
        public void Apply(FakeFileSystem files){files.Find("card_data"+CardMissing.ToString("00")).parentFolder=CardLocation;files.Find("rope_data"+RopeMissing.ToString("00")).parentFolder=RopeLocation;}
    }
    public static class GameIntegrityChecker
    {
        public static string[] Check(FakeFileSystem files,Stage06Config config,string game)=>config.groups.Where(g=>g.game==game&&g.files.Any(id=>{var n=files.Find(id);return n==null||!files.CanUse(n)||n.parentFolder!=g.folder;})).OrderBy(g=>Array.IndexOf(new[]{"data","sound","picture","system"},g.name)).Select(g=>g.name+" 파일의 데이터가 일부 누락되었습니다.").ToArray();
    }
    public static class TimePropertyParser
    {
        public static bool Parse(string text,TimePropertyRule[] schema,out Dictionary<string,string> values,out string normalized)
        {
            values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);var output=new StringBuilder();bool valid=true;
            foreach(string raw in (text??"").Replace("\r","").Split('\n'))
            {
                if(string.IsNullOrWhiteSpace(raw)){output.AppendLine(raw);continue;}
                var pair=raw.Split('=');string rendered=raw;
                if(pair.Length!=2){valid=false;output.AppendLine(raw);continue;}
                string key=pair[0].Trim(),value=pair[1].Trim();var rule=Array.Find(schema,r=>string.Equals(r.key,key,StringComparison.OrdinalIgnoreCase));
                if(rule==null||values.ContainsKey(key)){valid=false;output.AppendLine(raw);continue;}
                if(rule.kind=="int")
                {
                    // 큰 숫자도 정수 문자열로 검사한 다음 상한을 보정한다.
                    if(!System.Numerics.BigInteger.TryParse(value,out var number))valid=false;
                    else{if(rule.clamp&&number>rule.max){number=rule.max;value=number.ToString();rendered=key+" = "+value;}if(number<rule.min||number>rule.max)valid=false;}
                }
                else if(rule.kind=="enum"){if(!rule.allowed.Any(v=>string.Equals(v,value,StringComparison.OrdinalIgnoreCase)))valid=false;}
                else if(value!=rule.value)valid=false;
                values[key]=value;output.AppendLine(rendered);
            }
            normalized=output.ToString().TrimEnd('\r','\n');foreach(var rule in schema)if(!values.ContainsKey(rule.key))valid=false;return valid;
        }
        public static bool Special(Dictionary<string,string> values,EasterEggRule[] rules)=>rules.Length>0&&rules.All(r=>values.TryGetValue(r.key,out var value)&&string.Equals(value,r.value,StringComparison.OrdinalIgnoreCase));
    }
}
