using System.IO;
using System.Linq;
using CoComputar;
using UnityEditor;
using UnityEngine;
public static class Stage07Builder
{
    const string R="Assets/_Game/";
    static T Asset<T>(string p) where T:ScriptableObject{var a=AssetDatabase.LoadAssetAtPath<T>(R+p);if(a)return a;a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,R+p);return a;}
    public static void Setup()
    {
        // 임시 아이콘도 데이터베이스에 등록하여 원화 파일만 교체할 수 있다.
        Directory.CreateDirectory(R+"Art/Stage07");Icon("icon_floppy",false);Icon("s7_shears",true);AssetDatabase.Refresh();
        var catalog=Asset<GameCatalog>("Data/Catalog.asset");
        foreach(var file in Directory.GetFiles(R+"Art/Stage07","*.png"))
        {
            var p=file.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(p);importer.textureType=TextureImporterType.Sprite;importer.filterMode=FilterMode.Point;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
            var key=Path.GetFileNameWithoutExtension(p);catalog.sprites.entries.RemoveAll(e=>e.key==key);catalog.sprites.entries.Add(new SpriteEntry{key=key,sprite=AssetDatabase.LoadAssetAtPath<Sprite>(p)});
        }
        if(!catalog.sprites.entries.Any(e=>e.key=="portrait_floppy"))catalog.sprites.entries.Add(new SpriteEntry{key="portrait_floppy",sprite=catalog.sprites.Get("portrait_folder")});
        foreach(var key in new[]{"bgm_stage07","sfx_wizard_next","sfx_wizard_cancel","sfx_wizard_error","sfx_install_tick","sfx_install_done","sfx_serial_wrong","sfx_viewer_rotate","sfx_map_pin","sfx_map_move","sfx_bush_cut","sfx_item_get","sfx_rock_drag","sfx_doll_pop","sfx_lock_open","sfx_door_locked"})
            if(!catalog.audio.entries.Any(e=>e.key==key))catalog.audio.entries.Add(new AudioEntry{key=key,clip=catalog.audio.Get(key.StartsWith("bgm")?"bgm_stage06":key.Contains("error")||key.Contains("wrong")||key.Contains("locked")?"sfx_error":key.Contains("done")||key.Contains("get")||key.Contains("lock_open")?"sfx_key_get":"sfx_icon_press")});
        var bushSound=catalog.audio.entries.Single(e=>e.key=="sfx_bush_cut");
        if(!bushSound.clip||bushSound.clip==catalog.audio.Get("sfx_icon_press"))
            bushSound.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(R+"Audio/Stage07/sfx_bush_cut.wav");
        var assets=Asset<Stage07Assets>("Resources/Stage07Assets.asset");assets.ui=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Text/Stage07UI.json");assets.shortContract=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Text/Stage07ContractShort.txt");assets.longContract=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Text/Stage07ContractLong.txt");assets.secretContract=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Text/Stage07ContractSecret.txt");
        var theme=Asset<DesktopTheme>("Data/Stage07Theme.asset");theme.top=theme.bottom=new Color(.52f,.56f,.67f);
        var stage=Asset<StageDefinition>("Data/Stage07.asset");stage.stageId="Stage07";stage.nextStageId="Stage08";stage.title="Co-computar. / 설치 마법사";stage.desktopTheme=theme;stage.dialogue=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Dialogue/Stage07.json");stage.clearCondition="door.exe";stage.preserveWindowsDuringDialogue=true;
        var go=new GameObject("Stage07",typeof(RectTransform),typeof(Stage07Controller));stage.entryPrefab=PrefabUtility.SaveAsPrefabAsset(go,R+"Data/Stage07.prefab");Object.DestroyImmediate(go);
        var next=Asset<StageDefinition>("Data/Stage08.asset");next.stageId="Stage08";next.title="STAGE 08 / 복구 대기";
        catalog.stages=catalog.stages.Where(s=>s.stageId!="Stage07"&&s.stageId!="Stage08").Concat(new[]{stage,next}).ToArray();
        foreach(var a in new Object[]{assets,catalog,catalog.sprites,catalog.audio,theme,stage,next})EditorUtility.SetDirty(a);AssetDatabase.SaveAssets();Debug.Log("Stage07 구성 완료");
    }
    static void Icon(string name,bool shears)
    {
        string path=R+"Art/Stage07/"+name+".png";if(File.Exists(path))return;
        var t=new Texture2D(64,64,TextureFormat.RGBA32,false);t.SetPixels(Enumerable.Repeat(Color.clear,4096).ToArray());
        void Rect(int x,int y,int w,int h,Color c){for(int py=y;py<y+h;py++)for(int px=x;px<x+w;px++)t.SetPixel(px,py,c);}
        var ink=new Color(.06f,.09f,.14f);
        if(!shears){Rect(5,4,54,56,ink);Rect(8,7,48,50,new Color(.23f,.27f,.45f));Rect(16,37,32,22,new Color(.73f,.75f,.78f));Rect(35,40,8,16,ink);Rect(14,8,36,24,new Color(.92f,.92f,.87f));Rect(18,24,28,3,new Color(.9f,.35f,.55f));Rect(18,18,24,2,ink);}
        else{for(int y=22;y<58;y++){int x=18+(y-22)/2;Rect(x,y,5,1,new Color(.8f,.82f,.85f));Rect(59-x,y,5,1,new Color(.5f,.53f,.58f));}Rect(12,8,13,18,new Color(.8f,.12f,.19f));Rect(38,8,13,18,new Color(.8f,.12f,.19f));Rect(16,12,5,9,Color.clear);Rect(42,12,5,9,Color.clear);Rect(27,24,9,6,ink);}
        t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);
    }
    public static void Build(){Setup();ProjectBuilder.BuildWindows();}
}
