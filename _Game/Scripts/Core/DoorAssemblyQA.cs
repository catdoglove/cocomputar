#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Linq;
namespace CoComputar
{
    public sealed partial class PlaythroughQA
    {
        int DoorAssemblyOrders(List<(string source,string target)> path=null)
        {
            path??=new();var assembly=new DoorAssembly();
            foreach(var step in path)if(!assembly.Merge(step.source,step.target))return -1000;
            var keys=assembly.Groups.Keys.ToArray();
            if(assembly.Merge(keys[0],keys[0])||assembly.Merge("missing",keys[0]))return -1000;
            if(assembly.Groups.Values.SelectMany(p=>p).Distinct().Count()!=4)return -1000;
            if(assembly.Complete)return assembly.Label(keys[0])=="door"?1:-1000;
            int count=0;
            foreach(var source in keys)foreach(var target in keys)
            {
                if(source==target)continue;
                var next=new List<(string,string)>(path){(source,target)};count+=DoorAssemblyOrders(next);
            }
            return count;
        }
    }
}
#endif
