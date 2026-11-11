using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CoComputar
{
    [Serializable] public sealed class Stage09Progress
    {
        public bool d1,d2,d3,photoDelivered,keyObtained,npcGone,unpacked;
        public List<string> unlocked=new();public FakeFileNode[] files;
    }
    [Serializable] public class Stage09Text {public string id,text;}
    [Serializable] public class Stage09Hint {public string fileId,text;public float x,y;public bool answer;}
    [Serializable] public class Stage09Rules
    {
        public string answerFileId,answerExtension,npcLabel,npcIcon,doorSprite;public bool randomizeAnswer;
        public float npcX,npcY,zoomStep,headWaitSeconds;public int revealZoom,maxZoom;
        public Rect headRect,doorRect;public Stage09Hint[] hints;public Stage09Text[] texts;
        public string Text(string id)=>texts.First(x=>x.id==id).text;
    }
    public static class Stage09Logic
    {
        public static string Dialogue(Stage09Progress p)=>p.photoDelivered&&!p.keyObtained?"S9_FINAL":p.unpacked&&p.d2&&!p.d3?"S9_D3":p.d1&&!p.d2?"S9_D2":!p.d1?"S9_D1":"S9_IDLE";
        public static bool Photo(Stage09Progress p,FakeFileNode n,Stage09Rules r)=>p.d1&&!p.photoDelivered&&n!=null&&n.available&&n.id==r.answerFileId&&FakeFileSystem.Eq(n.extension,r.answerExtension);
        // UTF-8을 포함한 모든 인코딩에서 같은 길이의 깨진 문자만 표시한다.
        public static string Garble(string id,string encoding)
        {
            uint seed=2166136261;foreach(char c in id+":"+encoding)seed=(seed^c)*16777619;
            const string alphabet="魑鬱縺繧譁蜿髯饕¶§¤‡¿※龜齟靄";var b=new System.Text.StringBuilder();
            for(int i=0;i<1200;i++){seed=seed*1664525+1013904223;b.Append(i%48==47?'\n':alphabet[(int)(seed%alphabet.Length)]);}return b.ToString();
        }
    }
}
