#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace CoComputar
{
    // 타이틀 변경만 검사하는 별도 실행 경로. 저장 데이터와 오디오 설정은 변경하지 않는다.
    public sealed class ECGQA : MonoBehaviour
    {
        readonly List<string> results=new();string output;GameApp app;
        void Check(bool passed,string name) {if(!passed) throw new Exception("ECG QA 실패: "+name);results.Add("통과: "+name);Debug.Log(results.Last());}
        IEnumerator Start()
        {
            Application.targetFrameRate=60;app=GameApp.Instance;output=Path.GetFullPath("../QA");
            Application.logMessageReceived+=OnLog;
            yield return new WaitForSeconds(.3f);
            var ecg=app.ScreenRoot.GetComponentInChildren<ECGGraphic>();
            Check(ecg && Math.Abs(ecg.SweepSeconds-2.6f)<.001,"기본 주기 2.6초 및 테마 변수 연결");
            Check(ecg.color==app.Catalog.theme.green && ecg.rectTransform.anchoredPosition==new Vector2(50,-327),"기존 선 색과 배치 유지");
            Check(ecg.AlphaAt(.5f,0)==0 && ecg.AlphaAt(.95f,0)==0,"첫 표시 때 전체 선 노출 없이 투명하게 시작");
            double half=ecg.SweepSeconds*.5;
            Check(ecg.AlphaAt(.48f,half)>.95f && ecg.AlphaAt(.48f,half)>ecg.AlphaAt(.35f,half) && ecg.AlphaAt(.35f,half)>ecg.AlphaAt(.2f,half) && ecg.AlphaAt(.1f,half)==0,"선명한 머리 → 옅은 꼬리 → 소멸");
            Check(ecg.AlphaAt(.005f,ecg.SweepSeconds*.025)<ecg.AlphaAt(.04f,ecg.SweepSeconds*.06),"왼쪽 진입부 점진적 페이드 인");
            bool continuous=true;
            for(int i=0;i<=924;i++) continuous &= Math.Abs(ecg.AlphaAt(i/924f,ecg.SweepSeconds*2-.00001)-ecg.AlphaAt(i/924f,ecg.SweepSeconds*2+.00001))<.005;
            Check(continuous,"반복 경계에서 전체 알파 연속성, 이전 꼬리 유지");
            bool precision=true;
            for(int i=0;i<=924;i++) precision &= Math.Abs(ecg.AlphaAt(i/924f,half+ecg.SweepSeconds*100000d)-ecg.AlphaAt(i/924f,half+ecg.SweepSeconds*2d))<.0001;
            Check(precision,"10만 주기 후 위상 정밀도 유지");
            // 실제 Update와 렌더링에서 한 바퀴의 여러 시점을 저장한다.
            foreach(var phase in new[]{.06,.35,.52,.70,.98,.02})
            {
                while(Math.Abs(ecg.Phase-phase)>.009) yield return null;
                Capture("ecg-"+phase.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture));
            }
            yield return new WaitForEndOfFrame();var mesh=ecg.canvasRenderer.GetMesh();
            var vertices=mesh.vertices;var colors=mesh.colors32;
            bool shape=vertices.Length==1732;
            for(int i=0;i<70 && shape;i++)
            {
                float y=-Mathf.Exp(-Mathf.Pow((i-28)/5f,2))*38+Mathf.Exp(-Mathf.Pow((i-38)/5f,2))*32;
                int index=(363+i)*4;
                shape &= Vector3.Distance(vertices[index],new Vector3(362+i*2.8f,-y))<.001f;
                shape &= Vector3.Distance(vertices[index+2],new Vector3(366+i*2.8f,-y-4))<.001f;
            }
            Check(shape && vertices[2].y==-3 && vertices[726].y==-3,"원본 직선 두께 3, 파형 70점 좌표·4×4 크기 유지");
            var frames=new List<float>();double began=Time.realtimeSinceStartupAsDouble,initial=ecg.ElapsedSeconds;
            int cycles=0;double previous=ecg.Phase;bool monotonic=true;
            Debug.Log("ECG 120초 실제 실행 검사 시작");
            while(Time.realtimeSinceStartupAsDouble-began<120)
            {
                yield return null;double phase=ecg.Phase;
                if(phase<previous) cycles++;else monotonic &= phase>=previous;
                previous=phase;frames.Add(Time.unscaledDeltaTime);
            }
            yield return new WaitForEndOfFrame();mesh=ecg.canvasRenderer.GetMesh();
            Check(vertices.SequenceEqual(mesh.vertices),"120초 후 파형 좌표 및 크기 불변");
            double duration=ecg.ElapsedSeconds-initial;
            Check(monotonic && Math.Abs(cycles-duration/ecg.SweepSeconds)<=1,"120초 실제 실행에서 이동 방향·주기 유지");
            frames.Sort();float p95=frames[(int)(frames.Count*.95f)];
            File.WriteAllText(Path.Combine(output,"ecg-performance.txt"),$"duration={duration:F3}s\nloops={cycles}\nframes={frames.Count}\np95={p95*1000:F2}ms\nmax={frames.Last()*1000:F2}ms\n");
            Check(p95<.05f,"120초 실행 프레임 시간 p95 < 50ms");
            Capture("ecg-after-120s");
            File.WriteAllLines(Path.Combine(output,"ecg-checks.txt"),results);Debug.Log("ECG QA PASS "+results.Count);
            yield return null;Application.Quit(0);
        }
        void OnLog(string message,string stack,LogType type)
        {
            if(type==LogType.Exception || type==LogType.Error || type==LogType.Assert) Application.Quit(1);
        }
        void OnDestroy() {Application.logMessageReceived-=OnLog;}
        void Capture(string name)
        {
            Canvas.ForceUpdateCanvases();int width=Screen.width,height=Screen.height;
            var rt=new RenderTexture(width,height,24);rt.Create();
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(app.UICamera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});
            var old=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());RenderTexture.active=old;Destroy(image);rt.Release();Destroy(rt);
        }
    }
}
#endif
