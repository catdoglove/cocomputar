using System.IO;
using System.Linq;
using CoComputar;
using UnityEngine;
using UnityEditor;
public static class Stage04Builder
{
    const string R="Assets/_Game/";
    static T Asset<T>(string path) where T:ScriptableObject{var a=AssetDatabase.LoadAssetAtPath<T>(R+path);if(a)return a;a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,R+path);return a;}
    public static void Setup()
    {
        foreach(bool portrait in new[]{false,true})
        {
            string p=R+"Art/Stage04/"+(portrait?"portrait_memo":"icon_memo")+".png";if(File.Exists(p))continue;
            int w=portrait?96:48,h=portrait?144:48;var t=new Texture2D(w,h,TextureFormat.RGBA32,false);for(int y=0;y<h;y++)for(int x=0;x<w;x++)t.SetPixel(x,y,Color.clear);
            void Box(int x,int y,int a,int b,Color c){for(int i=x;i<x+a;i++)for(int j=y;j<y+b;j++)t.SetPixel(i,j,c);}
            if(portrait){Box(17,4,62,75,new Color(.15f,.15f,.18f));Box(10,23,76,55,Color.white);Box(20,38,60,12,new Color(.12f,.12f,.15f));Box(15,80,67,60,Color.black);Box(19,84,59,52,new Color(.15f,.8f,.75f));for(int i=0;i<5;i++)Box(28,92+i*7,40,2,Color.white);Box(21,128,54,4,Color.black);}
            else{Box(5,3,38,41,Color.black);Box(8,6,32,35,Color.white);Box(3,15,34,30,Color.black);Box(6,18,28,24,new Color(.15f,.8f,.75f));for(int i=0;i<3;i++)Box(12,23+i*6,17,2,Color.black);}
            t.Apply();File.WriteAllBytes(p,t.EncodeToPNG());Object.DestroyImmediate(t);
        }
        // 교체 전까지 쓸 도트 아이콘. 깨짐/압축/일기장을 서로 구분한다.
        foreach(string key in new[]{"icon_broken","s4_zip","s4_diary_icon"})
        {
            string path=R+"Art/Stage04/"+key+".png";if(File.Exists(path))continue;
            var t=new Texture2D(48,48,TextureFormat.RGBA32,false);for(int y=0;y<48;y++)for(int x=0;x<48;x++)t.SetPixel(x,y,Color.clear);
            void Box(int x,int y,int w,int h,Color c){for(int i=x;i<x+w;i++)for(int j=y;j<y+h;j++)t.SetPixel(i,j,c);}
            if(key=="icon_broken"){Box(9,3,30,42,Color.black);Box(12,6,24,36,Color.white);for(int i=0;i<10;i++){Box(16+i*2,12+i*2,3,3,new Color(.8f,.1f,.35f));Box(34-i*2,12+i*2,3,3,new Color(.8f,.1f,.35f));}Box(9,25,11,4,Color.clear);}
            else if(key=="s4_zip"){Box(4,5,40,32,Color.black);Box(7,8,34,26,new Color(.9f,.72f,.25f));Box(7,36,16,5,new Color(.9f,.72f,.25f));for(int y=10;y<33;y+=5){Box(24,y,4,3,Color.black);Box(20,y+2,4,3,Color.white);}}
            else{Box(5,3,38,42,Color.black);Box(8,6,32,36,new Color(.45f,.25f,.13f));Box(12,10,25,28,new Color(.86f,.7f,.44f));Box(8,6,4,36,new Color(.24f,.12f,.06f));Box(32,20,12,8,Color.black);Box(34,22,8,4,new Color(.95f,.7f,.25f));}
            t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);
        }
        AssetDatabase.Refresh();var catalog=Asset<GameCatalog>("Data/Catalog.asset");
        foreach(var file in Directory.GetFiles(R+"Art/Stage04","*.png")){var p=file.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(p);importer.textureType=TextureImporterType.Sprite;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();var key=Path.GetFileNameWithoutExtension(p);catalog.sprites.entries.RemoveAll(e=>e.key==key);catalog.sprites.entries.Add(new SpriteEntry{key=key,sprite=AssetDatabase.LoadAssetAtPath<Sprite>(p)});}
        foreach(var pair in new[]{("program","s3_program")}){catalog.sprites.entries.RemoveAll(e=>e.key==pair.Item1);catalog.sprites.entries.Add(new SpriteEntry{key=pair.Item1,sprite=catalog.sprites.Get(pair.Item2)});}
        foreach(var file in Directory.GetFiles(R+"Audio/Stage04","*.wav")){var p=file.Replace('\\','/');var key=Path.GetFileNameWithoutExtension(p);catalog.audio.entries.RemoveAll(e=>e.key==key);catalog.audio.entries.Add(new AudioEntry{key=key,clip=AssetDatabase.LoadAssetAtPath<AudioClip>(p)});}
        var theme=Asset<DesktopTheme>("Data/Stage04Theme.asset");theme.top=new Color(.05f,.46f,.48f);theme.bottom=new Color(.02f,.22f,.28f);
        var stage=Asset<StageDefinition>("Data/Stage04.asset");stage.stageId="Stage04";stage.nextStageId="Stage05";stage.title="Co-computar. / 메모";stage.desktopTheme=theme;stage.dialogue=AssetDatabase.LoadAssetAtPath<TextAsset>(R+"Data/Dialogue/Stage04.json");
        var go=new GameObject("Stage04",typeof(RectTransform),typeof(Stage04Controller));stage.entryPrefab=PrefabUtility.SaveAsPrefabAsset(go,R+"Data/Stage04.prefab");Object.DestroyImmediate(go);
        var next=Asset<StageDefinition>("Data/Stage05.asset");next.stageId="Stage05";next.title="STAGE 05 / 복구 대기";catalog.stages=new[]{catalog.Stage("Stage01"),catalog.Stage("Stage02"),catalog.Stage("Stage03"),stage,next};
        var fileData=JsonUtility.FromJson<FakeFileData>(File.ReadAllText(R+"Resources/Stage04Files.json"));
        string glyphs=string.Concat(fileData.files.Select(n=>n.garbledContent))+"ìë^¶§€¥¤©œŒ‡¿";
        catalog.theme.font.TryAddCharacters(glyphs,out string missing);
        if(!string.IsNullOrEmpty(missing)){var fallback=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(R+"Art/Fonts/JamoFallback.asset");fallback.TryAddCharacters(missing,out string unavailable);if(!string.IsNullOrEmpty(unavailable))throw new System.Exception("Stage04 글리프 누락: "+unavailable);if(!catalog.theme.font.fallbackFontAssetTable.Contains(fallback))catalog.theme.font.fallbackFontAssetTable.Add(fallback);EditorUtility.SetDirty(fallback);}
        foreach(var a in new Object[]{catalog,catalog.sprites,catalog.audio,catalog.theme.font,stage,next,theme})EditorUtility.SetDirty(a);AssetDatabase.SaveAssets();Debug.Log("Stage04 구성 완료");
    }
    public static void Build(){Setup();ProjectBuilder.BuildWindows();}
}
