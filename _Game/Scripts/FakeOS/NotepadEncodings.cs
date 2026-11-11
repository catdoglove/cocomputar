using System;
namespace CoComputar
{
    // IDs remain compatible with existing file data; labels/order do not decide correctness.
    public static class NotepadEncodings
    {
        public const string Default="ANSI",Correct="UTF8";
        public readonly struct Option
        {
            public readonly string Id,Label;
            public Option(string id,string label){Id=id;Label=label;}
        }
        public static readonly Option[] Options={new("ANSI","ANSI"),new("UTF2","UTF-2"),new("UTF4","UTF-4"),new("UTF8","UTF-8"),new("UTF16","UTF-16"),new("UTF32","UTF-32")};
        public static bool IsCorrect(string id)=>id==Correct;
        public static string Normalize(string id)=>Array.Exists(Options,o=>o.Id==id)?id:Default;
        public static string Label(string id)=>Array.Find(Options,o=>o.Id==Normalize(id)).Label;
    }
}
