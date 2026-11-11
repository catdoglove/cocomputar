using System;
using System.IO;
using System.Linq;
using CoComputar;
using UnityEngine;
using UnityEditor;
public static class Stage02Builder
{
    const string Root="Assets/_Game/";
    static T Asset<T>(string path) where T:ScriptableObject
    {var a=AssetDatabase.LoadAssetAtPath<T>(Root+path);if(a)return a;a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,Root+path);return a;}
    public static void Setup()
    {
        if(!File.Exists(Root+"Art/Stage02/twin_black.png"))Stage02Art.Generate();
        AssetDatabase.Refresh();var catalog=Asset<GameCatalog>("Data/Catalog.asset");
        var theme=Asset<DesktopTheme>("Data/Stage02Theme.asset");
        foreach(string path in Directory.GetFiles(Root+"Art/Stage02","*.png"))
        {
            var p=path.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(p);importer.textureType=TextureImporterType.Sprite;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            string key=Path.GetFileNameWithoutExtension(p);catalog.sprites.entries.RemoveAll(e=>e.key==key);catalog.sprites.entries.Add(new SpriteEntry{key=key,sprite=AssetDatabase.LoadAssetAtPath<Sprite>(p)});
        }
        // 임시 아트 연결도 데이터베이스에만 둔다. 최종 이미지를 넣으면 같은 키의 참조만 교체한다.
        foreach(var pair in new[]{("twin_black","cursor"),("twin_white","cursor"),("portrait_twin_black","portrait_folder"),("portrait_twin_white","portrait_folder")})
            if(!catalog.sprites.entries.Any(e=>e.key==pair.Item1))catalog.sprites.entries.Add(new SpriteEntry{key=pair.Item1,sprite=catalog.sprites.Get(pair.Item2)});
        foreach(string key in new[]{"bgm_stage02","sfx_key_get","sfx_zoom","sfx_rain","sfx_sprout_grow","sfx_bloom","sfx_sepia_on","sfx_glitch","sfx_file_merge","sfx_drag_return"})
            if(!catalog.audio.entries.Any(e=>e.key==key))catalog.audio.entries.Add(new AudioEntry{key=key,clip=catalog.audio.entries.Find(e=>e.key==(key.StartsWith("bgm")?"bgm_stage01":"sfx_ding")).clip});
        var stage=Asset<StageDefinition>("Data/Stage02.asset");stage.stageId="Stage02";stage.nextStageId="Stage03";stage.title="Co-computar. / 풍경수집";stage.desktopTheme=theme;
        var keySound=catalog.audio.entries.Find(e=>e.key=="sfx_key_get");
        if(keySound.clip==catalog.audio.Get("sfx_ding"))keySound.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/SFX/sfx_key_get.wav");
        foreach(string soundKey in new[]{"sfx_water_fail","sfx_rain","sfx_sepia_on"})
        {
            var sound=catalog.audio.entries.Find(e=>e.key==soundKey);
            if(sound==null){sound=new AudioEntry{key=soundKey};catalog.audio.entries.Add(sound);}
            if(!sound.clip||sound.clip==catalog.audio.Get("sfx_ding"))sound.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/SFX/"+soundKey+".wav");
        }
        stage.dialogue=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"Data/Dialogue/Stage02.json");
        var go=new GameObject("Stage02",typeof(RectTransform),typeof(Stage02Controller));stage.entryPrefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"Data/Stage02.prefab");UnityEngine.Object.DestroyImmediate(go);
        var next=Asset<StageDefinition>("Data/Stage03.asset");next.stageId="Stage03";if(!next.entryPrefab)next.title="STAGE 03 / 복구 대기";
        var fourth=catalog.Stage("Stage04");catalog.stages=fourth?new[]{catalog.Stage("Stage01"),stage,next,fourth}:new[]{catalog.Stage("Stage01"),stage,next};
        foreach(var obj in new UnityEngine.Object[]{catalog,catalog.sprites,catalog.audio,theme,stage,next})EditorUtility.SetDirty(obj);
        AssetDatabase.SaveAssets();Debug.Log("Stage02 데이터 구성 완료");
    }
    public static void Build(){Setup();ProjectBuilder.BuildWindows();}
}
