using System.IO;
using System.Linq;
using CoComputar;
using UnityEngine;
using UnityEditor;
public static class Stage06Builder
{
    const string R="Assets/_Game/";
    static T Asset<T>(string path) where T:ScriptableObject{var a=AssetDatabase.LoadAssetAtPath<T>(R+path);if(a)return a;a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,R+path);return a;}
    public static void Setup()
    {
        AssetDatabase.Refresh();var catalog=Asset<GameCatalog>("Data/Catalog.asset");
        foreach(var file in Directory.GetFiles(R+"Art/Stage06","*.png")){var p=file.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(p);importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();var key=Path.GetFileNameWithoutExtension(p);catalog.sprites.entries.RemoveAll(e=>e.key==key);catalog.sprites.entries.Add(new SpriteEntry{key=key,sprite=AssetDatabase.LoadAssetAtPath<Sprite>(p)});}
        foreach(var key in new[]{"bgm_stage06","sfx_card_flip","sfx_card_match","sfx_card_mismatch","sfx_card_clear","sfx_highscore_enter","sfx_rope_jump","sfx_rope_miss","sfx_game_over","sfx_lcd_tick","sfx_glitch_static","sfx_snake_eat","sfx_snake_die","sfx_spit","sfx_resize_tick","sfx_sticker_get","sfx_sticker_stick","sfx_file_morph","sfx_popup_spawn","sfx_popup_error","sfx_bgm_cut"})
            if(!catalog.audio.entries.Any(e=>e.key==key))catalog.audio.entries.Add(new AudioEntry{key=key,clip=catalog.audio.Get((key=="sfx_card_flip"||key=="sfx_rope_jump"||key=="sfx_snake_eat")?"sfx_icon_press":key.StartsWith("bgm")?"bgm_stage05":key=="sfx_card_mismatch"||key.Contains("miss")||key.Contains("over")||key.Contains("die")||key.Contains("error")?"sfx_error":key.Contains("glitch")?"sfx_glitch":"sfx_key_get")});
        var theme=Asset<DesktopTheme>("Data/Stage06Theme.asset");theme.top=theme.bottom=new Color(.35f,.62f,.55f);
        var stage=Asset<StageDefinition>("Data/Stage06.asset");stage.stageId="Stage06";stage.nextStageId="Stage07";stage.title="Co-computar. / 게임과 조이스틱";stage.desktopTheme=theme;stage.dialogue=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Dialogue/Stage06.json");stage.clearCondition="door.exe";
        var go=new GameObject("Stage06",typeof(RectTransform),typeof(Stage06Controller));stage.entryPrefab=PrefabUtility.SaveAsPrefabAsset(go,R+"Data/Stage06.prefab");Object.DestroyImmediate(go);
        var next=Asset<StageDefinition>("Data/Stage07.asset");next.stageId="Stage07";next.title="STAGE 07 / 복구 대기";catalog.stages=new[]{catalog.Stage("Stage01"),catalog.Stage("Stage02"),catalog.Stage("Stage03"),catalog.Stage("Stage04"),catalog.Stage("Stage05"),stage,next};
        foreach(var a in new Object[]{catalog,catalog.sprites,catalog.audio,theme,stage,next})EditorUtility.SetDirty(a);AssetDatabase.SaveAssets();Debug.Log("Stage06 구성 완료");
    }
    public static void Build(){Setup();ProjectBuilder.BuildWindows();}
}
