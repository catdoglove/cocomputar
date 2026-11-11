using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CoComputar
{
    [Serializable] public sealed class WebLink { public string label,target; }
    [Serializable] public sealed class WebWidget { public string kind,text,target,image,action,mode; public bool yourOnly;public int limit;public int[] corruptPositions; }
    [Serializable] public sealed class PageData { public string id,title;public WebWidget[] widgets; }
    [Serializable] public sealed class SiteData { public string id,title,color,domain;public bool yourOnly;public WebLink[] menu;public PageData[] pages; }
    [Serializable] public sealed class SiteDocument { public SiteData[] sites; }
    [Serializable] public sealed class WebProduct { public string shop,id,name,description,yourDescription,category,image,action;public int price;public bool yourOnly,taxExempt,freeBlocked; }
    [Serializable] public sealed class ProductDocument { public WebProduct[] products; }
    [Serializable] public sealed class WebText { public string id,text; }
    [Serializable] public sealed class NewsArticle { public string id,title,body; }
    [Serializable] public sealed class MailMessage { public string id,folder,sender,recipient,subject,date,body,attachment;public bool read;public string[] attachments; }
    [Serializable] public sealed class MailDocument { public MailMessage[] messages; }
    [Serializable] public sealed class Stage08Rules
    {
        public string autoTypeText,replyAddress;public float replySeconds,statusSeconds,eyeSeconds,notFoundChance,glitchDelay,globeEraseSeconds,soundCap;
        public int startTokens,budget,maxQuantity,eyeCount;public WebText[] texts;public NewsArticle[] news;
        public string Text(string id)=>texts.FirstOrDefault(t=>t.id==id)?.text??id;
    }
    public static class Stage08Validation
    {
        // 유한 수량 동전 문제: 상품당 0~3개, 금액 단위와 무관한 DP.
        public static long CandyWays(WebProduct[] products,int budget,int max)
        {
            var ways=new long[budget+1];ways[0]=1;
            foreach(var p in products.Where(p=>p.shop=="candy"))
            {var next=new long[budget+1];for(int sum=0;sum<=budget;sum++)for(int count=0;count<=max&&sum+count*p.price<=budget;count++)next[sum+count*p.price]+=ways[sum];ways=next;}
            return ways[budget];
        }
        public static int Total(WebProduct p)=>p.taxExempt?p.price:(int)Math.Ceiling(p.price*1.1m);
        public static bool DollValid(WebProduct p,int tokens)=>p.id=="body"?p.taxExempt&&p.price==tokens:p.freeBlocked?p.price==0:p.category=="인형 몸"?p.price>tokens:p.price>=1726&&p.price<=1897&&Total(p)>tokens;
        public static bool CorrectAttachments(IEnumerable<string> ids){var a=ids.ToArray();return a.Length==3&&new HashSet<string>(a).SetEquals(new[]{"plank","black","handle"});}
        public static string Diary(WebWidget w)
        {
            if(w.mode!="Reversed")return w.text;var chars=w.text.ToCharArray();foreach(var i in w.corruptPositions??Array.Empty<int>())if(i>=0&&i<chars.Length)chars[i]='ㅁ';Array.Reverse(chars);return new string(chars);
        }
    }
}
