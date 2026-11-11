using System;
using System.Collections.Generic;
using System.Linq;
namespace CoComputar
{
    [Serializable] public class FileHandler {public string extension,action,baseName;}
    [Serializable] public class FakeFileNode
    {
        public string id,displayName,extension,originalName,contentType,iconKey,originalIcon,parentFolder,restoreParent,encoding="UTF8",content,garbledContent,imageKey;
        public bool editable;public bool hidden;public bool IsVisible(bool showHidden)=>FileRecord.IsVisible(hidden,showHidden);
        public bool unrestorable;public bool isBroken,renamable,available=true;public string[] allowedExt;public FileHandler[] handlers;
        public float x,y;
    }
    [Serializable] public class FakeFileData {public FakeFileNode[] files;public string openLabel="Open",saveLabel="Save",helpLabel="Help",helpSpecialRoute="",helpWarningKey="";public int diaryHintZoom=2;public float installSeconds=3;public float dreamWindowRatio=.7f;}
    // 진입할 때마다 새로 만드는 가상 파일 상태. 영구 저장 데이터와 분리한다.
    public sealed class FakeFileSystem
    {
        public const string Desktop="desktop",Trash="trash";
        public readonly List<FakeFileNode> Nodes;public event Action Changed;
        public FakeFileSystem(FakeFileData data){Nodes=data.files.ToList();foreach(var n in Nodes){n.originalName=n.displayName;n.originalIcon=n.iconKey;UpdateType(n);}}
        public FakeFileNode Find(string id)=>Nodes.Find(n=>n.id==id);
        public IEnumerable<FakeFileNode> Children(string parent)=>Nodes.Where(n=>n.available&&n.parentFolder==parent);
        public bool CanUse(FakeFileNode n){var visited=new HashSet<string>();while(n!=null){if(n.unrestorable||!n.available||n.parentFolder==Trash||!visited.Add(n.id))return false;n=Find(n.parentFolder);}return true;}
        public bool IsWithin(FakeFileNode n,string id){var visited=new HashSet<string>();while(n!=null&&visited.Add(n.id)){if(n.id==id)return true;n=Find(n.parentFolder);}return false;}
        public string ActionFor(FakeFileNode n){if(!CanUse(n)||n.isBroken)return null;var name=BaseName(n.displayName);return n.handlers?.FirstOrDefault(h=>Eq(h.extension,n.extension)&&(string.IsNullOrEmpty(h.baseName)||Eq(h.baseName,name)))?.action;}
        public static bool Eq(string a,string b)=>string.Equals(a,b,StringComparison.OrdinalIgnoreCase);
        public static string BaseName(string name){int i=name.LastIndexOf('.');return i<0?name:name.Substring(0,i);}
        void UpdateType(FakeFileNode n)
        {
            int i=n.displayName.LastIndexOf('.');n.extension=i<0?"":n.displayName.Substring(i+1).ToLowerInvariant();if(!n.renamable)return;
            n.isBroken=n.allowedExt==null||!n.allowedExt.Any(e=>Eq(e,n.extension));
            n.iconKey=n.isBroken?"icon_broken":n.extension=="exe"?"program":n.extension=="dir"?"s4_diary_icon":n.extension=="txt"?"document":"picture";
        }
        public bool Rename(FakeFileNode n,string name,out string error)
        {
            error=null;if(n.unrestorable)error="복원할 수 없는 파일입니다.";else if(!n.renamable)error="이 파일의 이름은 바꿀 수 없습니다.";
            else if(string.IsNullOrWhiteSpace(name)||name.IndexOfAny("\\/:*?\"<>|".ToCharArray())>=0||name.Any(char.IsControl))error="사용할 수 없는 파일 이름입니다.";
            else if(Children(n.parentFolder).Any(f=>f!=n&&Eq(f.displayName,name.Trim())))error="같은 이름의 파일이 있습니다.";
            if(error!=null)return false;n.displayName=name.Trim();UpdateType(n);Changed?.Invoke();return true;
        }
        public void RestoreOriginal(FakeFileNode n){if(n.unrestorable)return;n.displayName=n.originalName;UpdateType(n);n.iconKey=n.originalIcon;Changed?.Invoke();}
        public void Delete(FakeFileNode n){if(n.parentFolder==Trash)return;n.restoreParent=n.parentFolder;n.parentFolder=Trash;Changed?.Invoke();}
        public void RestoreTrash(FakeFileNode n){if(n.unrestorable||n.parentFolder!=Trash)return;n.parentFolder=n.restoreParent??Desktop;Changed?.Invoke();}
        public void Reveal(string id){var n=Find(id);if(n.unrestorable)return;n.available=true;if(n.parentFolder==Trash)n.parentFolder=n.restoreParent??Desktop;Changed?.Invoke();}
        public void LockInTrash(FakeFileNode n){if(n.parentFolder!=Trash)n.restoreParent=n.parentFolder;n.parentFolder=Trash;n.unrestorable=true;n.available=true;Changed?.Invoke();}
        // Resolve and validate the output before consuming the archive. Notify once, after both changes.
        public bool ExtractArchive(string archiveId,string folderId)
        {
            var archive=Find(archiveId);var folder=Find(folderId);
            if(archive==null||folder==null||archive==folder||archive.extension!="zip"||!CanUse(archive)||folder.contentType!="folder"||folder.available||folder.unrestorable||IsWithin(archive,folderId))return false;
            if(Children(archive.parentFolder).Any(n=>n!=archive&&n!=folder&&Eq(n.displayName,folder.displayName)))return false;
            folder.parentFolder=archive.parentFolder;folder.x=archive.x;folder.y=archive.y;folder.restoreParent=null;folder.available=true;
            Nodes.Remove(archive);Changed?.Invoke();return true;
        }
        public void Notify()=>Changed?.Invoke();
        public static string GarbleFallback(string id,string text)
        {
            uint seed=2166136261;foreach(char c in id)seed=(seed^c)*16777619;const string chars="ìë^¶§€¥¤©œŒ‡¿";
            var b=new System.Text.StringBuilder();foreach(char c in text){seed=seed*1664525+1013904223;b.Append(c=='\n'||c=='\r'?c:chars[(int)(seed%chars.Length)]);}return b.ToString();
        }
        public string DisplayText(FakeFileNode n)=>!NotepadEncodings.IsCorrect(n.encoding)?(!string.IsNullOrEmpty(n.garbledContent)?n.garbledContent:GarbleFallback(n.id+":"+n.encoding,n.content??"")):n.content??"";
    }
}
