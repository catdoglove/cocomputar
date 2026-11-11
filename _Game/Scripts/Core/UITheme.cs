using TMPro;
using UnityEngine;
namespace CoComputar
{
    [CreateAssetMenu(menuName = "Co-computar/Theme")]
    public sealed class UITheme : ScriptableObject
    {
        public Color pink = new(1,.31f,.85f), green = new(.30f,1,.30f);
        public Color lavender = new(.86f,.68f,.96f), navy = new(.08f,.05f,.32f);
        public Color desktop = new(.12f,.79f,.91f), paper = new(.94f,.93f,.94f);
        public TMP_FontAsset font;
        public TMP_FontAsset titleFont;
        public string clock = "PM 10:34";
        public float charactersPerSecond = 32;
        [Min(.01f)] public float dialogueSkipSeconds = .025f;
        [Min(.1f)] public float ecgSweepSeconds = 2.6f;
    }
}
