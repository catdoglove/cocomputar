Shader "CoComputar/PhotoCorrection"
{
 Properties { [PerRendererData] _MainTex("사진",2D)="white" {} _Brightness("테두리 오차",Float)=0 _Clarity("선명도 오차",Float)=0 _Saturation("채도 오차",Float)=0 }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 sampler2D _MainTex;float4 _MainTex_TexelSize;float _Brightness,_Clarity,_Saturation;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
 fixed4 frag(v2f i):SV_Target {float2 blur=_MainTex_TexelSize.xy*abs(_Clarity)*9;fixed4 c=(tex2D(_MainTex,i.uv)+tex2D(_MainTex,i.uv+blur)+tex2D(_MainTex,i.uv-blur)+tex2D(_MainTex,i.uv+float2(blur.x,-blur.y))+tex2D(_MainTex,i.uv+float2(-blur.x,blur.y)))/5;float gray=dot(c.rgb,float3(.299,.587,.114));c.rgb=lerp(gray.xxx,c.rgb,max(0,1-_Saturation*1.5));float edge=smoothstep(.24,.5,max(abs(i.uv.x-.5),abs(i.uv.y-.5)));c.rgb=lerp(c.rgb,_Brightness>0?float3(1,1,1):float3(0,0,0),edge*abs(_Brightness));c.a*=1-abs(_Clarity)*.65;return c*i.color;}
 ENDCG }
 }
}
