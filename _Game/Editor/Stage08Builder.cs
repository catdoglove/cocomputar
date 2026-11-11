using System.IO;
using System.Linq;
using CoComputar;
using UnityEditor;
using UnityEngine;
public static class Stage08Builder
{
    const string R="Assets/_Game/";
    static T Asset<T>(string path) where T:ScriptableObject{var a=AssetDatabase.LoadAssetAtPath<T>(R+path);if(a)return a;a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,R+path);return a;}
    public static void Setup()
    {
        CreateColorAssets();AssetDatabase.Refresh();var catalog=Asset<GameCatalog>("Data/Catalog.asset");var assets=Asset<Stage08Assets>("Resources/Stage08Assets.asset");
        assets.specialDialogue=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Dialogue/Stage08_Special.json");
        assets.specialRules=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Sites/Stage08SpecialRules.json");
        assets.sites=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Sites/Stage08Sites.json");assets.products=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Sites/Stage08Products.json");assets.rules=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Sites/Stage08Rules.json");assets.files=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Sites/Stage08Files.json");assets.mail=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Mail/Stage08Mail.json");
        foreach(var file in Directory.GetFiles(R+"Art/Stage08","*.png"))
        {
            var p=file.Replace('\\','/');var importer=AssetImporter.GetAtPath(p) as TextureImporter;if(!importer)continue;importer.textureType=TextureImporterType.Sprite;importer.filterMode=FilterMode.Point;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
            var key=Path.GetFileNameWithoutExtension(p);catalog.sprites.entries.RemoveAll(e=>e.key==key);catalog.sprites.entries.Add(new SpriteEntry{key=key,sprite=AssetDatabase.LoadAssetAtPath<Sprite>(p)});
        }
        var aliases=new[]{"s8_plank:document","s8_handle:picture","s8_globe:computer","s8_candy:picture","s8_bread:picture","s8_mart:picture","s8_doll:portrait_folder","s8_color:picture","s8_umbrella:picture","icon_npc_stage8:portrait_npc_stage8"};
        foreach(var alias in aliases){var a=alias.Split(':');if(!catalog.sprites.entries.Any(e=>e.key==a[0]))catalog.sprites.entries.Add(new SpriteEntry{key=a[0],sprite=catalog.sprites.Get(a[1])});}
        foreach(var alias in new[]{"bgm_stage08:bgm_stage07","sfx_new_mail:sfx_key_get","sfx_mail_new:sfx_key_get","sfx_browser_click:sfx_click","sfx_browser_nav:sfx_click","sfx_status_toast:sfx_click","sfx_download:sfx_key_get","sfx_cart_add:sfx_click","sfx_order_ok:sfx_key_get","sfx_mail_send:sfx_click","sfx_autotype_key:sfx_click","sfx_token_buy:sfx_key_get","bgm_special_stage1:bgm_stage01","bgm_special_stage9:bgm_murmur","sfx_lock_denied:sfx_denied","sfx_npc_vanish:sfx_npc_pop","sfx_file_appear:sfx_key_get","sfx_login_fail:sfx_pw_wrong","sfx_install_warn:sfx_denied","sfx_warp_install:sfx_install_done","sfx_fake_error:sfx_error","sfx_gameover:sfx_game_over","sfx_head_split:sfx_doll_pop","sfx_key_drop:sfx_item_get","sfx_forbidden_folder:sfx_glitch","sfx_deep_enter:sfx_lock_open"}){var a=alias.Split(':');if(!catalog.audio.entries.Any(e=>e.key==a[0]))catalog.audio.entries.Add(new AudioEntry{key=a[0],clip=catalog.audio.Get(a[1])});}
        var theme=Asset<DesktopTheme>("Data/Stage08Theme.asset");theme.top=theme.bottom=new Color(.28f,.48f,.52f);
        var stage=Asset<StageDefinition>("Data/Stage08.asset");stage.stageId="Stage08";stage.nextStageId="Stage09";stage.title="Co-computar. / 가상 인터넷";stage.desktopTheme=theme;stage.dialogue=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Dialogue/Stage08.json");stage.clearCondition="door.exe";
        var go=new GameObject("Stage08",typeof(RectTransform),typeof(Stage08Controller));stage.entryPrefab=PrefabUtility.SaveAsPrefabAsset(go,R+"Data/Stage08.prefab");Object.DestroyImmediate(go);
        var next=Asset<StageDefinition>("Data/Stage09.asset");next.stageId="Stage09";next.title="STAGE 09 / 복구 대기";
        catalog.stages=catalog.stages.Where(s=>s.stageId!="Stage08"&&s.stageId!="Stage09").Concat(new[]{stage,next}).ToArray();
        foreach(var a in new Object[]{assets,catalog,catalog.sprites,catalog.audio,theme,stage,next})EditorUtility.SetDirty(a);AssetDatabase.SaveAssets();Debug.Log("Stage08 구성 완료");
    }
    static void CreateColorAssets()
    {
        var names=new[]{"red","blue","orange","purple","white","black"};var colors=new[]{Color.red,Color.blue,new Color(1,.5f,0),new Color(.5f,0,.8f),Color.white,Color.black};
        for(int i=0;i<names.Length;i++)
        {
            string path=R+"Art/Stage08/s8_"+names[i]+".png";if(File.Exists(path))continue;
            var texture=new Texture2D(96,96,TextureFormat.RGBA32,false);texture.SetPixels(Enumerable.Repeat(colors[i],96*96).ToArray());texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
        }
    }
    public static void Build(){Setup();ProjectBuilder.BuildWindows();}
}
