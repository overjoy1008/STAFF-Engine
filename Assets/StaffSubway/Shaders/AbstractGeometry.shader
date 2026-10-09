Shader "Hidden/STAFF/Abstract Geometry" {
 CGINCLUDE
 #include "UnityCG.cginc"
 struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float depth:TEXCOORD1;float2 uv:TEXCOORD2;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.depth=-UnityObjectToViewPos(v.vertex).z;o.uv=v.texcoord;return o;}
 sampler2D _MainTex;float4 _MainTex_ST;float _AlphaClip,_Cutoff;
 float4 env(v2f i):SV_Target{if(_AlphaClip>.5)clip(tex2D(_MainTex,i.uv*_MainTex_ST.xy+_MainTex_ST.zw).a-_Cutoff);return float4(normalize(i.normal)*.5+.5,i.depth);}
 float4 skip(v2f i):SV_Target{clip(-1);return 0;}
 float4 excluded(v2f i):SV_Target{return float4(0,0,0,-1);}
 ENDCG
 SubShader {Tags{"RenderType"="AbstractEnvironment"} Cull Off Pass{ CGPROGRAM
 #pragma vertex vert
 #pragma fragment env
 ENDCG }}
 SubShader {Tags{"RenderType"="Opaque"} Cull Off Pass{ CGPROGRAM
 #pragma vertex vert
 #pragma fragment excluded
 ENDCG }}
 SubShader {Tags{"RenderType"="TransparentCutout"} Cull Off Pass{ CGPROGRAM
 #pragma vertex vert
 #pragma fragment excluded
 ENDCG }}
 SubShader {Tags{"RenderType"="Transparent"} Cull Off Pass{ CGPROGRAM
 #pragma vertex vert
 #pragma fragment excluded
 ENDCG }}
 SubShader {Tags{"RenderType"="AbstractPreserved"} Cull Off Pass{ CGPROGRAM
 #pragma vertex vert
 #pragma fragment skip
 ENDCG }}
 SubShader { Tags {"RenderType"="AbstractAnalytic"} Pass { Cull Off ZWrite On
 CGPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #define STAFF_ABSTRACT_GEOMETRY
 #include "AbstractAnalyticPlane.cginc"
 ENDCG
 }}
 Fallback Off
}
