using System;
using System.Linq;
using UnityEngine;
namespace CoComputar
{
    [Serializable] public class PhotoFixData {public int sliderMin=0,sliderMax=20;public float tolerance=0,hold=.5f;}
    [Serializable] public class Stage05Config {public int photoCount=31,secondTalkInteractions=5;public float radioHold=2.5f,secondTalkSeconds=120;public float[] radioDistances={0,1,12},radioGains={1,.85f,0};public PhotoFixData photo;public string puzzleClearSfx="sfx_key_get";public string classicSong="classic_song";public string[] songs;public AnimationCurve musicByDistance=AnimationCurve.Linear(0,1,12,0);}
    [Serializable] public sealed class Stage05Randomizer
    {
        public Vector3 PhotoTarget;public int Frequency;public int[] Photos;public string[] Hint,Initial;
        public Stage05Randomizer(Stage05Config data,System.Random random=null)
        {
            random??=new System.Random();PhotoTarget=new Vector3(random.Next(Mathf.Max(1,data.photo.sliderMin+1),data.photo.sliderMax+1),random.Next(Mathf.Max(1,data.photo.sliderMin+1),data.photo.sliderMax+1),random.Next(Mathf.Max(1,data.photo.sliderMin+1),data.photo.sliderMax+1));Frequency=random.Next(101);int count=Mathf.Clamp(data.photoCount,30,31),offset=random.Next(count);
            Photos=Enumerable.Range(0,count).Select(i=>(Frequency-offset+i+101)%101).ToArray();
            if(data.songs.Length!=10||data.songs.Distinct().Count()!=10||data.songs.Select(Prefix).Distinct().Count()!=10||!data.songs.Contains(data.classicSong))throw new ArgumentException("곡 목록은 classic_song 포함, 서로 다른 접두어 10개가 필요합니다.");
            Hint=Shuffle(data.songs,random);Initial=Shuffle(data.songs,random);if(Initial.SequenceEqual(Hint)){var t=Initial[0];Initial[0]=Initial[1];Initial[1]=t;}
        }
        static string[] Shuffle(string[] source,System.Random random){var a=(string[])source.Clone();for(int i=a.Length-1;i>0;i--){int j=random.Next(i+1);(a[i],a[j])=(a[j],a[i]);}return a;}
        public static string Prefix(string title)=>title.Split('_')[0]+"_";
    }
}
