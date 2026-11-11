using System.IO;
using System.Linq;
using CoComputar;
using UnityEditor;
using UnityEngine;
public static class Stage09Builder
{
    const string R="Assets/_Game/";
    static T Asset<T>(string path) where T:ScriptableObject{var a=AssetDatabase.LoadAssetAtPath<T>(R+path);if(a)return a;a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,R+path);return a;}
    public static void Setup()
    {
        AssetDatabase.Refresh();var catalog=Asset<GameCatalog>("Data/Catalog.asset");var assets=Asset<Stage09Assets>("Resources/Stage09Assets.asset");
        assets.rules=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Stage09/Rules.json");assets.files=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Stage09/Files.json");assets.dialogue=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Stage09/Dialogue.json");
        foreach(var file in Directory.GetFiles(R+"Art/Stage09","*.png")){
            var path=file.Replace('\\','/');var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType=TextureImporterType.Sprite;importer.filterMode=FilterMode.Point;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.SaveAndReimport();
            var key=Path.GetFileNameWithoutExtension(path);catalog.sprites.entries.RemoveAll(e=>e.key==key);catalog.sprites.entries.Add(new SpriteEntry{key=key,sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path)});
        }
        foreach(var alias in new[]{"bgm_stage09:bgm_murmur","sfx_unlock:sfx_lock_open","sfx_wrong_key:sfx_denied","sfx_unzip:sfx_install_done","sfx_rename_ok:sfx_rename","sfx_image_corrupt:sfx_error","sfx_zoom_reveal:sfx_click","sfx_photo_deliver:sfx_item_get","sfx_key_click:sfx_click","sfx_npc_fade:sfx_npc_pop","sfx_door_open:sfx_lock_open"}){var a=alias.Split(':');if(!catalog.audio.entries.Any(e=>e.key==a[0]))catalog.audio.entries.Add(new AudioEntry{key=a[0],clip=catalog.audio.Get(a[1])});}
        var theme=Asset<DesktopTheme>("Data/Stage09Theme.asset");theme.top=theme.bottom=new Color(.42f,.40f,.53f);
        var stage=Asset<StageDefinition>("Data/Stage09.asset");stage.stageId="Stage09";stage.nextStageId="Stage10";stage.title="Co-computar. / 열쇠의 기억";stage.desktopTheme=theme;stage.dialogue=assets.dialogue;stage.clearCondition="homeDoor";
        var go=new GameObject("Stage09",typeof(RectTransform),typeof(Stage09Controller));stage.entryPrefab=PrefabUtility.SaveAsPrefabAsset(go,R+"Data/Stage09.prefab");Object.DestroyImmediate(go);
        var next=Asset<StageDefinition>("Data/Stage10.asset");next.stageId="Stage10";next.title="STAGE 10 / 복구 대기";
        catalog.stages=catalog.stages.Where(s=>s.stageId!="Stage09"&&s.stageId!="Stage10").Concat(new[]{stage,next}).ToArray();
        foreach(var a in new Object[]{assets,catalog,catalog.sprites,catalog.audio,theme,stage,next})EditorUtility.SetDirty(a);AssetDatabase.SaveAssets();Debug.Log("Stage09 구성 완료");
    }
    public static void Build(){Setup();ProjectBuilder.BuildWindows();}
}
