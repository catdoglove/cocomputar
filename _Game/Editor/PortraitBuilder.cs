using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using CoComputar;
public static class PortraitBuilder
{
    public static void Setup()
    {
        AssetDatabase.Refresh();var sprites=AssetDatabase.LoadAssetAtPath<SpriteDatabase>("Assets/_Game/Data/Sprites.asset");
        foreach(var path in Directory.GetFiles("Assets/_Game/Art","portrait_*.png",SearchOption.AllDirectories))
        {
            var file=path.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(file);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(file);if(!sprite||sprite.rect.width!=sprite.texture.width||sprite.rect.height!=sprite.texture.height)throw new Exception("CG 잘림: "+file);
            string key=Path.GetFileNameWithoutExtension(file);sprites.entries.RemoveAll(e=>e.key==key);sprites.entries.Add(new SpriteEntry{key=key,sprite=sprite});Debug.Log("CG 등록: "+key+" "+sprite.rect.size);
        }
        EditorUtility.SetDirty(sprites);AssetDatabase.SaveAssets();
    }
    public static void Build(){Setup();ProjectBuilder.BuildWindows();}
}
