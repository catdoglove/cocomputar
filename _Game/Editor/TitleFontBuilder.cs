using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
public static class TitleFontBuilder
{
    public static void Setup()
    {
        const string root="Assets/_Game/";AssetDatabase.Refresh();
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(root+"Art/Fonts/TitleNeo SDF.asset");
        if(!font)
        {
            var source=AssetDatabase.LoadAssetAtPath<Font>(root+"Art/Fonts/NeoDunggeunmo.ttf");
            font=TMP_FontAsset.CreateFontAsset(source,32,5,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
            AssetDatabase.CreateAsset(font,root+"Art/Fonts/TitleNeo SDF.asset");AssetDatabase.AddObjectToAsset(font.material,font);foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
            EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();
        }
        var theme=AssetDatabase.LoadAssetAtPath<CoComputar.UITheme>(root+"Data/Theme.asset");theme.titleFont=font;
        font.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{theme.font};
        string text=string.Concat(Directory.GetFiles(root+"Scripts","*.cs",SearchOption.AllDirectories).Select(File.ReadAllText));font.TryAddCharacters(new string(text.Where(c=>!char.IsControl(c)).Distinct().ToArray()),out string missing);
        EditorUtility.SetDirty(font);EditorUtility.SetDirty(theme);AssetDatabase.SaveAssets();Debug.Log("타이틀 Neo둥근모 적용: "+font.faceInfo.familyName);
    }
    public static void Build(){Setup();ProjectBuilder.BuildWindows();}
}
