using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoComputar
{
    [Serializable] public class SpriteEntry { public string key; public Sprite sprite; }
    [Serializable] public class AudioEntry { public string key; public AudioClip clip; }
    [Serializable] public class HighlightWord { public string word; public string color = "#FF4FD8"; }
    [Serializable] public class DialogueLine
    {
        public string id, speaker, text, stringKey, emotion, portraitKey, characterId, expressionId;
        public HighlightWord[] highlightWords;
        public string cmd, soundKey, style,flag;
        public float seconds,characterSeconds;public bool paginate;
        public string[] glitchWords;
    }
    [Serializable] public class DialogueSequence { public string id; public DialogueLine[] lines; }
    [Serializable] public class DialogueDocument { public DialogueSequence[] sequences; }
    [Serializable] public class FileRecord
    {
        public string id, name, category, spriteKey, content, imageKey;
        public bool hidden;
        public static bool IsVisible(bool hidden,bool showHidden)=>!hidden||showHidden;
        public bool IsVisible(bool showHidden)=>IsVisible(hidden,showHidden);
    }
    [Serializable] public class FileDocument { public FileRecord[] files; }
    [Serializable] public class FolderNode { public string id, name; public string[] children; public bool exit; }
    [Serializable] public class FolderDocument { public string root; public FolderNode[] nodes; }
    [Serializable] public class PlayerUnlocks { public bool skipUnlocked,gameCleared; public bool stage06StarObtained; public bool stage08EndingDeep; }
    [Serializable] public class ProgressData
    {
        public Stage09Progress stage09;
        public Stage05Progress stage05;
        public Stage08SpecialProgress stage08Special;
        public string stage02FirstKey = "";
        public bool stage02TalkedBeforeFirstKey;
        public int version = 1;
        public string stageId = "Stage01";
        public List<string> cleared = new();
        public List<string> flags = new();
    }
    [Serializable] public class OptionsData
    {
        public float music = .35f, effects = .55f, filter = .65f, curvature = .65f;
        public int crtVersion;
        public List<string> hiddenViews = new();
    }
    public enum StageState { Intro, SortNormal, SortHidden, Maze, Transition }
    public sealed class SortingModel
    {
        public readonly FileRecord[] Files;
        readonly Dictionary<string, string> locations = new();
        public SortingModel(FileRecord[] files) { Files = files; }
        public string Location(string id) => locations.TryGetValue(id, out var value) ? value : "";
        public void Move(string id, string category) { locations[id] = category; }
        public bool IsCorrect(bool includeHidden)
        {
            foreach (var file in Files)
                if ((!file.hidden || includeHidden) && Location(file.id) != file.category) return false;
            return true;
        }
    }
}
