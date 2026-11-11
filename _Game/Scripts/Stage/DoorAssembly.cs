using System.Collections.Generic;
using System.Linq;
namespace CoComputar
{
    // 표시 이름 대신 원본 ID를 합친다. 두 o는 서로 다른 조각이다.
    public sealed class DoorAssembly
    {
        static readonly string[] Originals={"d","o1","o2","r"};
        readonly Dictionary<string,HashSet<string>> groups=Originals.ToDictionary(id=>id,id=>new HashSet<string>{id});
        public IReadOnlyDictionary<string,HashSet<string>> Groups=>groups;
        public bool Complete=>groups.Count==1&&groups.Values.First().SetEquals(Originals);
        public string Label(string id)=>groups.TryGetValue(id,out var parts)?string.Concat(Originals.Where(parts.Contains).Select(p=>p.StartsWith("o")?"o":p)):"";
        public bool Merge(string source,string target)
        {
            if(source==target||!groups.TryGetValue(source,out var from)||!groups.TryGetValue(target,out var into)||from.Overlaps(into))return false;
            into.UnionWith(from);groups.Remove(source);return true;
        }
    }
}
