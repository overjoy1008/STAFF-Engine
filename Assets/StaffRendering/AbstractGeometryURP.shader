Shader "Hidden/STAFF/URP Geometry"{
 Properties{_MainTex("Albedo",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Cutoff("Cutoff",Float)=.1 _AlphaClip("Clip",Float)=0 _Excluded("Character",Float)=0}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline"} Pass{Cull Off ZWrite On HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float4x4 _StaffGeometryVP,_StaffGeometryV;
 sampler2D _MainTex;float4 _MainTex_ST,_Color;float _Cutoff,_AlphaClip,_Excluded;
 struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float3 n:TEXCOORD0;float d:TEXCOORD1;float2 uv:TEXCOORD2;};
 V vert(A i){V o;o.p=mul(_StaffGeometryVP,float4(TransformObjectToWorld(i.p.xyz),1));o.n=TransformObjectToWorldNormal(i.n);o.d=-mul(_StaffGeometryV,float4(TransformObjectToWorld(i.p.xyz),1)).z;o.uv=TRANSFORM_TEX(i.uv,_MainTex);return o;}
 float4 frag(V i):SV_Target{if(_AlphaClip>.5)clip(tex2D(_MainTex,i.uv).a*_Color.a-_Cutoff);return float4(normalize(i.n)*.5+.5,_Excluded>.5?-1:i.d);}
 ENDHLSL}}}
