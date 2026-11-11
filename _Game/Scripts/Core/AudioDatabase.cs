using System.Collections.Generic;
using UnityEngine;
namespace CoComputar
{
    [CreateAssetMenu(menuName = "Co-computar/Audio")]
    public sealed class AudioDatabase : ScriptableObject
    {
        public List<AudioEntry> entries = new();
        public AudioClip Get(string key) => entries.Find(e => e.key == key)?.clip;
    }
}
