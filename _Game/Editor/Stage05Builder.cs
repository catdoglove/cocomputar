using System.IO;
using System.Linq;
using CoComputar;
using UnityEngine;
using UnityEditor;
public static class Stage05Builder
{
    const string R="Assets/_Game/";
    static T Asset<T>(string path) where T:ScriptableObject{var a=AssetDatabase.LoadAssetAtPath<T>(R+path);if(a)return a;a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,R+path);return a;}
    public static void Setup()
    {
        AssetDatabase.Refresh();var catalog=Asset<GameCatalog>("Data/Catalog.asset");
        foreach(var file in Directory.GetFiles(R+"Art/Stage05","*.png")){var p=file.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(p);importer.textureType=TextureImporterType.Sprite;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();var key=Path.GetFileNameWithoutExtension(p);catalog.sprites.entries.RemoveAll(e=>e.key==key);catalog.sprites.entries.Add(new SpriteEntry{key=key,sprite=AssetDatabase.LoadAssetAtPath<Sprite>(p)});}
        foreach(var file in Directory.GetFiles(R+"Audio/Stage05","*.wav")){var p=file.Replace('\\','/');var key=Path.GetFileNameWithoutExtension(p);catalog.audio.entries.RemoveAll(e=>e.key==key);catalog.audio.entries.Add(new AudioEntry{key=key,clip=AssetDatabase.LoadAssetAtPath<AudioClip>(p)});}
        var theme=Asset<DesktopTheme>("Data/Stage05Theme.asset");theme.top=theme.bottom=new Color(.95f,.83f,.25f);
        var stage=Asset<StageDefinition>("Data/Stage05.asset");stage.stageId="Stage05";stage.nextStageId="Stage06";stage.title="Co-computar. / 스피커";stage.desktopTheme=theme;stage.dialogue=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Dialogue/Stage05.json");stage.clearCondition="door.exe";
        var go=new GameObject("Stage05",typeof(RectTransform),typeof(Stage05Controller));stage.entryPrefab=PrefabUtility.SaveAsPrefabAsset(go,R+"Data/Stage05.prefab");Object.DestroyImmediate(go);
        var next=Asset<StageDefinition>("Data/Stage06.asset");next.stageId="Stage06";next.title="STAGE 06 / 복구 대기";catalog.stages=new[]{catalog.Stage("Stage01"),catalog.Stage("Stage02"),catalog.Stage("Stage03"),catalog.Stage("Stage04"),stage,next};
        var glyphs=File.ReadAllText(R+"Data/Dialogue/Stage05.json")+File.ReadAllText(R+"Resources/Stage05FileLayout.json");catalog.theme.font.TryAddCharacters(glyphs,out _);
        foreach(var a in new Object[]{catalog,catalog.sprites,catalog.audio,catalog.theme.font,theme,stage,next})EditorUtility.SetDirty(a);AssetDatabase.SaveAssets();Debug.Log("Stage05 구성 완료");
    }
    public static void Build(){Setup();ProjectBuilder.BuildWindows();}
}
