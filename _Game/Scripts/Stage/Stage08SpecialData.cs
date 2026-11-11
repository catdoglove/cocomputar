using System;
using System.Linq;
using UnityEngine;
namespace CoComputar
{
    public enum SpecialRouteState { NotStarted,Talked1,Offered,FakeDoorGiven,GameOver,AdminFileGiven,LoggedIn,InstallerDownloaded,WarpInstalled,Talked3,StageReturn1,FolderTalked,FolderGone,Stage9Replica,KeyTalking,KeyObtained,KeyTalked,ForbiddenFolderShown,FinalTalked,Ending }
    [Serializable] public sealed class Stage08SpecialProgress
    {
        public SpecialRouteState state,checkpoint;
        public string adminId="admin89",adminPw="";
        public bool adminLoggedIn,warpInstalled;
        public FakeFileNode[] files;
        public string[] acquired;
        public bool ordered,eyeDone,globeTaken;
        public int tokens;
    }
    [Serializable] public sealed class SpecialRecord { public string title,body; }
    [Serializable] public sealed class Stage08SpecialRules
    {
        public int passwordMin=9,passwordMax=11,errorCount=6;
        public string passwordLetters="abcdefghjkmnpqrstuvwxyz",passwordDigits="23456789";
        public float removeLineSeconds=.03f,installSeconds=4,endingSeconds=4,cursorSeconds=20,idleSeconds=15,sfxCap=.55f;
        public string accountTemplate,lockWarning,loginFailure,gameover,choice;
        public string[] warnings,installFiles,errorMessages,library;
        public SpecialRecord[] records;
        public string GeneratePassword()
        {
            var random=new System.Random(Guid.NewGuid().GetHashCode());
            int length=random.Next(Math.Max(2,passwordMin),Math.Max(passwordMin,passwordMax)+1);
            string alphabet=passwordLetters+passwordDigits;
            var chars=new char[length];chars[0]=passwordLetters[random.Next(passwordLetters.Length)];chars[1]=passwordDigits[random.Next(passwordDigits.Length)];
            for(int i=2;i<length;i++)chars[i]=alphabet[random.Next(alphabet.Length)];
            for(int i=length-1;i>0;i--){int j=random.Next(i+1);(chars[i],chars[j])=(chars[j],chars[i]);}
            return new string(chars);
        }
    }
}
