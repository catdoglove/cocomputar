using UnityEngine;
using UnityEngine.Audio;
namespace CoComputar
{
    [CreateAssetMenu(menuName = "Co-computar/Catalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        public UITheme theme;
        public SpriteDatabase sprites;
        public AudioDatabase audio;
        public CRTSettings crt;
        public AudioMixer mixer;
        public AudioMixerGroup musicGroup, effectsGroup;
        public StageDefinition[] stages;
        public StageDefinition Stage(string id) => System.Array.Find(stages, s => s.stageId == id);
    }
}
