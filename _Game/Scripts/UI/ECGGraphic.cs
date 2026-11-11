using System;
using UnityEngine;
using UnityEngine.UI;
namespace CoComputar
{
    // 파형의 좌표는 고정하고 알파만 움직인다. 오른쪽의 이전 꼬리는 루프 후에도 자연스럽게 소멸한다.
    public sealed class ECGGraphic : MaskableGraphic
    {
        public float SweepSeconds { get; private set; } = 2.6f;
        public double ElapsedSeconds => Time.unscaledTimeAsDouble-startedAt;
        public double Phase => ElapsedSeconds/SweepSeconds%1.0;
        double startedAt;
        public void Setup(Color lineColor,float seconds)
        {
            color=lineColor;raycastTarget=false;SweepSeconds=Mathf.Max(.1f,seconds);
            startedAt=Time.unscaledTimeAsDouble;SetVerticesDirty();
        }
        void Update() { SetVerticesDirty(); }
        public float AlphaAt(float x,double elapsed)
        {
            double cycles=elapsed/SweepSeconds,phase=cycles-Math.Floor(cycles);
            if(cycles<1 && x>phase) return 0;
            float age=(float)((phase-x+1)%1);
            float head=Mathf.SmoothStep(0,1,age/.008f);
            float tail=1-Mathf.SmoothStep(0,1,age/.36f);
            float edge=Mathf.SmoothStep(0,1,x/.04f)*Mathf.SmoothStep(0,1,(1-x)/.04f);
            return head*tail*edge;
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();double elapsed=ElapsedSeconds;
            // 이전 왼쪽 선 362×3, 오른쪽 선 364×3, 파형 점 70개의 4×4 크기를 보존한다.
            for(float x=0;x<362;x+=2) Quad(mesh,x,0,2,3,elapsed);
            for(float x=560;x<924;x+=2) Quad(mesh,x,0,2,3,elapsed);
            for(int i=0;i<70;i++)
            {
                float y=-Mathf.Exp(-Mathf.Pow((i-28)/5f,2))*38+Mathf.Exp(-Mathf.Pow((i-38)/5f,2))*32;
                Quad(mesh,362+i*2.8f,y,4,4,elapsed);
            }
        }
        void Quad(VertexHelper mesh,float x,float y,float width,float height,double elapsed)
        {
            int index=mesh.currentVertCount;
            var left=color;left.a*=AlphaAt(x/924,elapsed);
            var right=color;right.a*=AlphaAt((x+width)/924,elapsed);
            mesh.AddVert(new Vector3(x,-y),left,Vector2.zero);
            mesh.AddVert(new Vector3(x+width,-y),right,Vector2.zero);
            mesh.AddVert(new Vector3(x+width,-y-height),right,Vector2.zero);
            mesh.AddVert(new Vector3(x,-y-height),left,Vector2.zero);
            mesh.AddTriangle(index,index+1,index+2);mesh.AddTriangle(index+2,index+3,index);
        }
    }
}
