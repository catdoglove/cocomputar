using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CoComputar;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ProjectBuilder
{
    const string Root="Assets/_Game/";
    static T Asset<T>(string path) where T:ScriptableObject
    {
        var asset=AssetDatabase.LoadAssetAtPath<T>(Root+path); if(asset) return asset;
        asset=ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset,Root+path);return asset;
    }
    [MenuItem("Co-computar/프로젝트 구성 및 씬 생성")]
    public static void Setup()
    {
        PlayerSettings.companyName="CoComputar";PlayerSettings.productName="Co-computar.";
        PlayerSettings.defaultScreenWidth=1024;PlayerSettings.defaultScreenHeight=768;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
        PlayerSettings.runInBackground=true;
        var playerSettings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        playerSettings.FindProperty("activeInputHandler").intValue=1; playerSettings.ApplyModifiedPropertiesWithoutUndo();
        // TMP 기본 리소스는 패키지에 동봉된 정식 리소스를 가져온다.
        if(!Directory.Exists("Assets/TextMesh Pro/Resources"))
        {
            var essential=Directory.GetFiles("Library/PackageCache","TMP Essential Resources.unitypackage",SearchOption.AllDirectories).FirstOrDefault();
            if(essential!=null) AssetDatabase.ImportPackage(essential,false);
        }
        var sprites=Asset<SpriteDatabase>("Data/Sprites.asset");
        {
            foreach(var path in Directory.GetFiles(Root+"Art/Resources/Placeholders","*.png"))
            {
                if(sprites.entries.Any(e=>e.key==Path.GetFileNameWithoutExtension(path))) continue;
                var normalized=path.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(normalized);
                importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=64;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                sprites.entries.Add(new SpriteEntry{key=Path.GetFileNameWithoutExtension(path),sprite=AssetDatabase.LoadAssetAtPath<Sprite>(normalized)});
            }
        }
        var audio=Asset<AudioDatabase>("Data/Audio.asset");
        if(audio.entries.Count==0) foreach(var path in Directory.GetFiles(Root+"Audio/SFX","*.wav")) audio.entries.Add(new AudioEntry{key=Path.GetFileNameWithoutExtension(path),clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path.Replace('\\','/'))});
        var theme=Asset<UITheme>("Data/Theme.asset");
        if(!theme.font)
        {
            var font=AssetDatabase.LoadAssetAtPath<Font>(Root+"Art/Fonts/Galmuri11.ttf");
            var tmp=TMP_FontAsset.CreateFontAsset(font,32,5,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
            tmp.name="Galmuri11 Dynamic";
            AssetDatabase.CreateAsset(tmp,Root+"Art/Fonts/Galmuri11 SDF.asset");
            AssetDatabase.AddObjectToAsset(tmp.material,tmp); foreach(var atlas in tmp.atlasTextures) AssetDatabase.AddObjectToAsset(atlas,tmp);
            theme.font=tmp;
        }
        var crt=Asset<CRTSettings>("Data/CRTSettings.asset");
        if(!crt.material)
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(Root+"Shaders/CRT.shadergraph");
            if(!shader) throw new Exception("CRT Shader Graph 가져오기 실패");
            var material=new Material(shader); AssetDatabase.CreateAsset(material,Root+"Shaders/CRT.mat");crt.material=material;
        }
        var renderer=Asset<UniversalRendererData>("Data/Renderer.asset");
        if(renderer.rendererFeatures.Count==0)
        {
            var feature=ScriptableObject.CreateInstance<FullScreenPassRendererFeature>(); feature.name="CRT Shader Graph";feature.passMaterial=crt.material;feature.fetchColorBuffer=true;
            feature.injectionPoint=FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
            AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);feature.Create();
        }
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"Data/URP.asset");
        if(!pipeline) {pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,Root+"Data/URP.asset");}
        pipeline.msaaSampleCount=1;GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
        var stage1=Asset<StageDefinition>("Data/Stage01.asset"); stage1.stageId="Stage01";stage1.nextStageId="Stage02";stage1.title="Co-computar.";stage1.hasPrologue=true;
        stage1.dialogue=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"Data/Dialogue/Stage01.json"); stage1.files=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"Data/Puzzles/Files.json");stage1.folderTree=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"Data/Puzzles/FolderTree.json");
        stage1.letters=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"Data/Puzzles/Letters.json");
        var stage2=Asset<StageDefinition>("Data/Stage02.asset");stage2.stageId="Stage02";
        var catalog=Asset<GameCatalog>("Data/Catalog.asset");catalog.sprites=sprites;catalog.audio=audio;catalog.theme=theme;catalog.crt=crt;catalog.stages=new[]{stage1,stage2};
        SetupMixer(catalog);
        foreach(var obj in new UnityEngine.Object[]{sprites,audio,theme,crt,renderer,pipeline,stage1,stage2,catalog}) EditorUtility.SetDirty(obj);
        AssetDatabase.SaveAssets();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("게임 카메라",typeof(Camera),typeof(AudioListener),typeof(AspectCamera)).GetComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=384;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.nearClipPlane=.1f;camera.farClipPlane=100;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        var app=new GameObject("Co-computar.").AddComponent<GameApp>();app.Catalog=catalog;app.UICamera=camera;
        EditorSceneManager.SaveScene(scene,Root+"Scenes/Main.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"Scenes/Main.unity",true)};
        AssetDatabase.SaveAssets();Stage02Builder.Setup();Stage03Builder.Setup();Stage04Builder.Setup();Stage05Builder.Setup();Stage06Builder.Setup();PortraitBuilder.Setup();Stage07Builder.Setup();Stage08Builder.Setup();GalmuriFontBuilder.Setup(); Debug.Log("프로젝트 구성 완료");
    }
    static void SetupMixer(GameCatalog catalog)
    {
        var type=typeof(Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
        if(type==null) type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.Audio.AudioMixerController")).FirstOrDefault(t=>t!=null);
        if(type==null) throw new Exception("AudioMixer 에디터 형식 없음");
        if(!catalog.mixer)
        {
            var create=type.GetMethod("CreateMixerControllerAtPath",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
            catalog.mixer=(AudioMixer)create.Invoke(null,new object[]{Root+"Audio/Game.mixer"});
        }
        var groups=catalog.mixer.FindMatchingGroups("");
        catalog.musicGroup=groups.FirstOrDefault(g=>g.name=="Music");catalog.effectsGroup=groups.FirstOrDefault(g=>g.name=="Effects");
    }
    [MenuItem("Co-computar/Windows 빌드")]
    public static void BuildWindows()
    {
        var destination=Path.GetFullPath("../Windows/Co-computar.exe");Directory.CreateDirectory(Path.GetDirectoryName(destination));
        var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,destination,BuildTarget.StandaloneWindows64,BuildOptions.Development);
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception("빌드 실패: "+report.summary.result);
        Debug.Log("Windows 빌드 성공: "+destination);
    }
    public static void SetupAndBuild() {Setup();BuildWindows();}
    [MenuItem("Co-computar/Windows 배포 빌드")]
    public static void BuildRelease()
    {
        var destination=Path.GetFullPath("../Windows-Release/Co-computar.exe");Directory.CreateDirectory(Path.GetDirectoryName(destination));
        var report=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,destination,BuildTarget.StandaloneWindows64,BuildOptions.None);
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception("배포 빌드 실패: "+report.summary.result);
        Debug.Log("Windows 배포 빌드 성공: "+destination);
    }
}
