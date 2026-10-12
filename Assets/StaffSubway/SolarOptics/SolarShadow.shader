Shader "Hidden/STAFF/SolarShadow" {
 Properties { _MainTex("Alpha",2D)="white"{} _Cutoff("Cutoff",Float)=0 }
 SubShader {Tags{"RenderPipeline"="UniversalPipeline"} Pass {ZWrite On ZTest LEqual Cull Off
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _MainTex_ST;float _Cutoff,_SolarExtent;float4x4 _SolarLightVP,_SolarLightView;
 struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float lightDepth:TEXCOORD1;};
 V Vert(appdata_base i){V o;float4 w=mul(unity_ObjectToWorld,i.vertex);o.position=mul(_SolarLightVP,w);o.lightDepth=-mul(_SolarLightView,w).z/(_SolarExtent*3);o.uv=TRANSFORM_TEX(i.texcoord,_MainTex);return o;}
 float Frag(V i):SV_Target {clip(tex2D(_MainTex,i.uv).a-_Cutoff);return i.lightDepth;}
 ENDHLSL
 }}
}
