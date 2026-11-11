Shader "CoComputar/MemoryBlur"
{
 Properties { [PerRendererData] _MainTex("Image",2D)="white"{} _Blur("Blur",Float)=0 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 sampler2D _MainTex;float4 _MainTex_TexelSize;float _Blur;
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
 fixed4 frag(v2f i):SV_Target{float2 d=_MainTex_TexelSize.xy*_Blur;fixed4 c=0;for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)c+=tex2D(_MainTex,i.uv+float2(x,y)*d);return c/9*i.color;}
 ENDCG }
 }
}
