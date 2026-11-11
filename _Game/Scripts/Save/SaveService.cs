using System;
using System.IO;
using UnityEngine;
namespace CoComputar
{
    public sealed class SaveService
    {
        readonly string directory;
        public ProgressData Progress { get; private set; }
        public PlayerUnlocks Unlocks { get; private set; }
        public OptionsData Options { get; private set; }
        public bool Exists => File.Exists(Path.Combine(directory, "progress.json"));
        public SaveService(string root = null)
        {
            directory = root ?? (Array.IndexOf(Environment.GetCommandLineArgs(),"-coQA")>=0 ? Path.Combine(Application.temporaryCachePath,"CoComputarQA") : Application.persistentDataPath);
            Directory.CreateDirectory(directory);
            Unlocks = Read<PlayerUnlocks>("unlocks.json") ?? new PlayerUnlocks();
            Progress = Read<ProgressData>("progress.json") ?? new ProgressData();
            Options = Read<OptionsData>("options.json") ?? new OptionsData();
            // 이전 단일 필터의 곡률을 그대로 이어받고, 이후 두 값은 독립 저장한다.
            if(Options.crtVersion<1) { Options.curvature=Options.filter;Options.crtVersion=1;SaveOptions(); }
        }
        T Read<T>(string name) where T : class
        {
            var path = Path.Combine(directory, name);
            if (!File.Exists(path)) return null;
            try { return JsonUtility.FromJson<T>(File.ReadAllText(path)); }
            catch (Exception) { File.Copy(path, path + ".corrupt", true); return null; }
        }
        void Write(string name, object value)
        {
            var path = Path.Combine(directory, name);
            File.WriteAllText(path + ".tmp", JsonUtility.ToJson(value, true));
            if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak");
            else File.Move(path + ".tmp", path);
        }
        public bool CanSkipDialogue()=>Unlocks.skipUnlocked||Unlocks.gameCleared;
        public void UnlockDialogueSkip(){Unlocks.skipUnlocked=true;Write("unlocks.json",Unlocks);}
        // 엔딩 도달 시 호출: GameApp.Instance.Save.SetGameCleared(); 현재는 자동 호출하지 않는다.
        public void SetGameCleared(){Unlocks.gameCleared=true;Unlocks.skipUnlocked=true;Write("unlocks.json",Unlocks);}
        public bool Stage06SpecialSeen => Progress.flags.Contains("stage06.specialSeen");
        public void MarkStage06SpecialSeen(){Flag("stage06.specialSeen");}
        public void ObtainStage06Star(){Unlocks.stage06StarObtained=true;Write("unlocks.json",Unlocks);Flag("stage06.starObtained");}
        public void NewGame(string first)
        {
            Progress = new ProgressData { stageId = first };if(Unlocks.stage06StarObtained)Progress.flags.Add("stage06.starObtained");
            Options.hiddenViews = new System.Collections.Generic.List<string>();
            SaveOptions(); SaveProgress();
        }
        // 퍼즐 상태만 명시적으로 초기화한다. 해금·다른 스테이지·옵션은 보존한다.
        public void ResetStagePuzzles(string stageId)
        {
            if(stageId=="Stage09"){Progress.stage09=null;Progress.flags.RemoveAll(k=>k.StartsWith("stage09."));Options.hiddenViews?.RemoveAll(k=>k.StartsWith("s9:"));SaveProgress();SaveOptions();return;}
            if(stageId=="Stage06"){Progress.flags.RemoveAll(key=>key=="stage06.specialSeen");SaveProgress();return;}
            // Stage07 진행/랜덤 값은 새 컨트롤러에서 생성. 선택 이력 두 플래그는 분기 기록으로 보존한다.
            if(stageId=="Stage08"){Options.hiddenViews?.RemoveAll(key=>key.StartsWith("s8:"));SaveOptions();return;}
            if(stageId=="Stage07"){Options.hiddenViews?.RemoveAll(key=>key.StartsWith("s7:"));SaveOptions();return;}
            if(stageId=="Stage05"){Options.hiddenViews?.RemoveAll(key=>key.StartsWith("s5:folder:"));SaveOptions();Progress.stage05=null;SaveProgress();return;}
            if(stageId=="Stage04"){Progress.flags.RemoveAll(key=>key=="stage04.diaryOpened"||key=="stage04.diaryTrashed");SaveProgress();return;}
            if(stageId=="Stage03"){SetHidden("s3:secret",false);Progress.flags.RemoveAll(key=>key=="stage03.pastSolved"||key=="stage03.p1Talk"||key=="stage03.pastPngOpened");SaveProgress();return;}
            if(stageId!="Stage02")return;
            Progress.flags.RemoveAll(key=>key=="stage02.tulipSolved");
            Progress.stage02FirstKey="";Progress.stage02TalkedBeforeFirstKey=false;SaveProgress();
        }
        public void Enter(string id) { Progress.stageId = id; SaveProgress(); }
        public void Complete(string id, string next)
        {
            if (!Progress.cleared.Contains(id)) Progress.cleared.Add(id);
            Progress.stageId = next; SaveProgress();
        }
        public void Flag(string key) { if (!Progress.flags.Contains(key)) Progress.flags.Add(key); SaveProgress(); }
        public void SaveProgress() => Write("progress.json", Progress);
        public void CompleteDeepEnding(){Unlocks.stage08EndingDeep=true;SetGameCleared();Flag("stage08.ending.deep");}
        public bool Hidden(string view) => Options.hiddenViews != null && Options.hiddenViews.Contains(view);
        public void SetHidden(string view, bool enabled)
        {
            Options.hiddenViews ??= new System.Collections.Generic.List<string>();
            if(enabled) { if(!Options.hiddenViews.Contains(view)) Options.hiddenViews.Add(view); }
            else Options.hiddenViews.Remove(view);
            SaveOptions();
        }
        public void SaveOptions() => Write("options.json", Options);
    }
}
