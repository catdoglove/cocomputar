using System.Linq;
using UnityEngine;
namespace CoComputar
{
    public sealed class Stage07Assets:ScriptableObject
    {
        public TextAsset ui,shortContract,longContract,secretContract;
        Stage07TextData text;
        public string Text(string key){text??=JsonUtility.FromJson<Stage07TextData>(ui.text);return text.entries.FirstOrDefault(e=>e.id==key)?.text??key;}
    }
}
