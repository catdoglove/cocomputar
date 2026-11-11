using System.Collections.Generic;
using UnityEngine;
namespace CoComputar
{
    [CreateAssetMenu(menuName = "Co-computar/Sprites")]
    public sealed class SpriteDatabase : ScriptableObject
    {
        public List<SpriteEntry> entries = new();
        public Sprite Get(string key) => entries.Find(e => e.key == key)?.sprite;
    }
}
