using System;
using UnityEngine;
namespace CoComputar
{
    [Serializable] public sealed class ExpressionEntry { public string id,spriteKey; }
    [Serializable] public sealed class CharacterExpressions
    {
        public string id,side="left",defaultExpression="normal";
        public string[] speakerAliases;
        public ExpressionEntry[] expressions;
    }
    [Serializable] public sealed class ExpressionCatalog
    {
        public CharacterExpressions[] characters;
        public CharacterExpressions Find(string id) => Array.Find(characters ?? Array.Empty<CharacterExpressions>(),c=>c.id==id || Array.Exists(c.speakerAliases ?? Array.Empty<string>(),a=>a==id));
        public Sprite Resolve(CharacterExpressions character,string expressionId,out bool exact)
        {
            exact=false;if(character==null)return null;
            var entries=character.expressions ?? Array.Empty<ExpressionEntry>();
            var entry=Array.Find(entries,e=>e.id==expressionId);
            var sprite=entry==null?null:GameApp.Instance.Catalog.sprites.Get(entry.spriteKey);
            if(sprite) {exact=true;return sprite;}
            entry=Array.Find(entries,e=>e.id==character.defaultExpression);
            return entry==null?null:GameApp.Instance.Catalog.sprites.Get(entry.spriteKey);
        }
    }
}
