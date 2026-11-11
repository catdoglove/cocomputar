using System;
using System.IO;
using System.Linq;
using CoComputar;
using UnityEngine;
using UnityEditor;
using TMPro;
public static class Stage03Builder
{
    const string Root="Assets/_Game/";
    static T Asset<T>(string path) where T:ScriptableObject{var a=AssetDatabase.LoadAssetAtPath<T>(Root+path);if(a)return a;a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,Root+path);return a;}
    public static void Setup()
    {
        // 코드로 만든 도트 발자국 임시 에셋. 동일 파일을 교체하면 최종 그림을 사용할 수 있다.
        foreach(bool right in new[]{false,true})
        {
            string path=Root+"Art/Stage03/s3_foot_"+(right?"right":"left")+".png";if(File.Exists(path))continue;
            var tex=new Texture2D(24,40,TextureFormat.RGBA32,false);for(int y=0;y<40;y++)for(int x=0;x<24;x++){float px=right?23-x:x;bool foot=Mathf.Pow((px-12)/6,2)+Mathf.Pow((y-25)/12f,2)<1||Mathf.Pow((px-8)/4,2)+Mathf.Pow((y-5)/4f,2)<1;tex.SetPixel(x,y,foot?Color.red:Color.clear);}tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
        }
        MakeProgramIcon();MakePaintingPlaceholder(false);MakePaintingPlaceholder(true);
        AssetDatabase.Refresh();var catalog=Asset<GameCatalog>("Data/Catalog.asset");
        foreach(var path in Directory.GetFiles(Root+"Art/Stage03","*.png"))
        {
            var p=path.Replace('\\','/');var imp=(TextureImporter)AssetImporter.GetAtPath(p);imp.textureType=TextureImporterType.Sprite;imp.filterMode=FilterMode.Point;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
            var key=Path.GetFileNameWithoutExtension(p);catalog.sprites.entries.RemoveAll(e=>e.key==key);catalog.sprites.entries.Add(new SpriteEntry{key=key,sprite=AssetDatabase.LoadAssetAtPath<Sprite>(p)});
        }
        foreach(var pair in new[]{("icon_geurim","picture"),("icon_geurim_young","picture"),("portrait_geurim","portrait_folder"),("portrait_geurim_young","portrait_folder")})
            if(!catalog.sprites.entries.Any(e=>e.key==pair.Item1))catalog.sprites.entries.Add(new SpriteEntry{key=pair.Item1,sprite=catalog.sprites.Get(pair.Item2)});
        foreach(string key in new[]{"bgm_stage03","bgm_murmur","sfx_denied","sfx_filter_switch","sfx_footprint","sfx_install_tick","sfx_pw_wrong","sfx_unlock","sfx_time_warp","sfx_npc_pop"})
        {var audio=catalog.audio.entries.Find(e=>e.key==key);if(audio==null){audio=new AudioEntry{key=key};catalog.audio.entries.Add(audio);}if(!audio.clip)audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/Stage03/"+key+".wav");}
        var theme=Asset<DesktopTheme>("Data/Stage03Theme.asset");theme.top=new Color32(229,220,246,255);theme.bottom=new Color32(179,164,210,255);
        var stage=Asset<StageDefinition>("Data/Stage03.asset");stage.stageId="Stage03";stage.nextStageId="Stage04";stage.title="Co-computar. / 필름";stage.desktopTheme=theme;stage.dialogue=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"Data/Dialogue/Stage03.json");
        var go=new GameObject("Stage03",typeof(RectTransform),typeof(Stage03Controller));stage.entryPrefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"Data/Stage03.prefab");UnityEngine.Object.DestroyImmediate(go);
        var next=Asset<StageDefinition>("Data/Stage04.asset");next.stageId="Stage04";next.title="STAGE 04 / 복구 대기";catalog.stages=new[]{catalog.Stage("Stage01"),catalog.Stage("Stage02"),stage,next};
        // 실제 사용 자모와 무늬를 아틀라스에 미리 추가한다. 동적 다중 아틀라스도 유지한다.
        catalog.theme.font.TryAddCharacters(new string(Enumerable.Range(0x3131,0x318F-0x3131).Select(c=>(char)c).ToArray())+"필름×",out string missing);if(!string.IsNullOrEmpty(missing))
        {
            var fallback=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"Art/Fonts/JamoFallback.asset");
            if(!fallback){fallback=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(Root+"Art/Fonts/NotoSansKR.ttf"),32,5,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);AssetDatabase.CreateAsset(fallback,Root+"Art/Fonts/JamoFallback.asset");AssetDatabase.AddObjectToAsset(fallback.material,fallback);foreach(var atlas in fallback.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,fallback);}
            fallback.TryAddCharacters(missing,out string unavailable);if(!string.IsNullOrEmpty(unavailable))throw new Exception("자모 fallback 누락: "+unavailable);
            if(!catalog.theme.font.fallbackFontAssetTable.Contains(fallback))catalog.theme.font.fallbackFontAssetTable.Add(fallback);EditorUtility.SetDirty(fallback);
        }
        var materialPath=Root+"Resources/MemoryBlur.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!mat){mat=new Material(Shader.Find("CoComputar/MemoryBlur"));AssetDatabase.CreateAsset(mat,materialPath);}
        foreach(var obj in new UnityEngine.Object[]{catalog,catalog.sprites,catalog.audio,catalog.theme.font,theme,stage,next})EditorUtility.SetDirty(obj);AssetDatabase.SaveAssets();Debug.Log("Stage03 구성 완료");
    }
    static void MakeProgramIcon()
    {
        string path=Root+"Art/Stage03/s3_program.png";if(File.Exists(path))return;
        var t=new Texture2D(48,48,TextureFormat.RGBA32,false);for(int y=0;y<48;y++)for(int x=0;x<48;x++)t.SetPixel(x,y,Color.clear);
        void Box(int x,int y,int w,int h,Color c){for(int a=x;a<x+w;a++)for(int b=y;b<y+h;b++)t.SetPixel(a,b,c);}
        Color dark=new Color32(35,30,48,255),grey=new Color32(185,185,200,255),light=new Color32(247,242,255,255);
        Box(3,3,43,41,dark);Box(1,5,43,41,dark);Box(2,6,41,39,grey);Box(2,44,41,1,light);Box(2,6,1,38,light);
        Box(4,36,37,7,dark);Box(5,37,25,5,new Color32(71,75,218,255));Box(5,41,25,1,new Color32(100,205,232,255));
        for(int x=32;x<=38;x+=3)Box(x,39,2,2,grey);
        Box(4,8,37,26,dark);Box(5,9,35,24,light);Box(5,32,35,1,new Color32(139,141,157,255));Box(5,9,1,23,grey);
        t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
    }
    static void MakePaintingPlaceholder(bool young)
    {
        foreach(bool portrait in new[]{false,true})
        {
            string key=(portrait?"portrait_":"icon_")+"geurim"+(young?"_young":"");string path=Root+"Art/Stage03/"+key+".png";if(File.Exists(path))continue;
            int width=portrait?96:48,height=portrait?144:48;var t=new Texture2D(width,height,TextureFormat.RGBA32,false);for(int y=0;y<height;y++)for(int x=0;x<width;x++)t.SetPixel(x,y,Color.clear);
            void Box(int x,int y,int w,int h,Color c){for(int a=x;a<x+w;a++)for(int b=y;b<y+h;b++)if(a>=0&&b>=0&&a<width&&b<height)t.SetPixel(a,b,c);}
            Color frame=new Color(.42f,.25f,.15f),gold=new Color(.8f,.61f,.3f),shirt=young?new Color(.32f,.7f,.9f):new Color(.72f,.72f,.69f);
            if(portrait){int shift=young?-12:0;Box(18,5,23,22,frame);Box(55,5,23,22,frame);Box(13,23,70,58+shift,frame);Box(17,28,62,50+shift,shirt);Box(4,32,9,36,shirt);Box(83,32,9,36,shirt);Box(15,78+shift,66,60,frame);Box(19,82+shift,58,52,gold);Box(23,86+shift,50,44,new Color(.52f,.77f,.88f));Box(23,86+shift,50,12,new Color(.29f,.5f,.27f));Box(30,112+shift,7,7,frame);Box(60,112+shift,7,7,frame);Box(42,101+shift,13,3,frame);}
            else{Box(3,3,42,42,frame);Box(6,6,36,36,gold);Box(9,9,30,30,shirt);Box(14,28,5,5,frame);Box(29,28,5,5,frame);Box(20,19,8,3,frame);}
            t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
        }
    }
    public static void Build(){Setup();ProjectBuilder.BuildWindows();}
}
