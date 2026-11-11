using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
public static class GalmuriFontBuilder
{
    public static void Setup()
    {
        const string root="Assets/_Game/";AssetDatabase.Refresh();
        var source=AssetDatabase.LoadAssetAtPath<Font>(root+"Art/Fonts/Galmuri11.ttf");
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(root+"Art/Fonts/Galmuri11 SDF.asset");
        if(font&&(!font.material||font.atlasTextures.Any(t=>!t))){AssetDatabase.DeleteAsset(root+"Art/Fonts/Galmuri11 SDF.asset");font=null;}
        if(!font)
        {
            font=TMP_FontAsset.CreateFontAsset(source,32,5,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
            font.name="Galmuri11 Dynamic";AssetDatabase.CreateAsset(font,root+"Art/Fonts/Galmuri11 SDF.asset");AssetDatabase.AddObjectToAsset(font.material,font);foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();
        }
        var fallback=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(root+"Art/Fonts/JamoFallback.asset");
        font.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>();if(fallback)font.fallbackFontAssetTable.Add(fallback);
        string text=string.Concat(Directory.GetFiles(root,"*.json",SearchOption.AllDirectories).Concat(Directory.GetFiles(root+"Scripts","*.cs",SearchOption.AllDirectories)).Select(File.ReadAllText));
        string characters=new string(text.Where(c=>!char.IsControl(c)&&c!='\ufeff').Distinct().ToArray());
        font.TryAddCharacters(characters,out string missing);
        if(fallback&&!string.IsNullOrEmpty(missing)){fallback.TryAddCharacters(missing,out string remaining);EditorUtility.SetDirty(fallback);Debug.Log("Galmuri 보조 글리프: "+missing+" / 미지원: "+remaining);}
        var theme=AssetDatabase.LoadAssetAtPath<CoComputar.UITheme>(root+"Data/Theme.asset");theme.font=font;
        var settings=AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset");var serialized=new SerializedObject(settings);serialized.FindProperty("m_defaultFontAsset").objectReferenceValue=font;serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);EditorUtility.SetDirty(theme);EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();Debug.Log("Galmuri11 공통·기본 폰트 적용 완료");TitleFontBuilder.Setup();
    }
    public static void Build(){Setup();ProjectBuilder.BuildWindows();}
}
