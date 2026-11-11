using System;
using System.Collections.Generic;
using UnityEngine;
namespace CoComputar
{
    // 확장자와 무관하게 위치를 기준으로 열기와 퍼즐 사용을 차단한다.
    public sealed class RecycleFile
    {
        public string Id,Name,Sprite,Location;public bool Hidden,BlockedInPast;public Action Open;public string[] WindowKeys;
    }
    public sealed class RecycleFiles
    {
        public const string Trash="s2:trash",Desktop="s2:desktop",Collection="s2:collection",Exit="s2:exit";
        public readonly List<RecycleFile> Files=new();public Action Changed;
        public Func<bool> IsPast;
        public RecycleFile Find(string id)=>Files.Find(f=>f.Id==id);
        public bool CanUse(string id)=>Find(id)?.Location!=Trash;
        public void Add(RecycleFile file){if(Find(file.Id)==null){Files.Add(file);Changed?.Invoke();}}
        public void Open(string id)
        {
            var file=Find(id);if(file==null||!CanUse(id))return;
            if(file.BlockedInPast&&IsPast?.Invoke()==true){GameApp.Instance.Audio.PlayPuzzle("sfx_error",replace:true);return;}
            file.Open?.Invoke();
        }
        public bool Move(string id,string destination)
        {
            var file=Find(id);if(file==null||file.Location==destination)return false;
            file.Location=destination;
            if(destination==Trash)
                foreach(var window in GameApp.Instance.Windows.GetComponentsInChildren<FakeWindow>())
                    if(Array.IndexOf(file.WindowKeys??Array.Empty<string>(),window.Key)>=0)window.Close();
            Changed?.Invoke();return true;
        }
        public void Remove(string id){Files.RemoveAll(f=>f.Id==id);Changed?.Invoke();}
    }
}
