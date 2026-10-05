Shader "STAFF/Character Toon"
{
 Properties {
  _BaseMap("Color",2D)="white"{}
  _BaseColor("Tint",Color)=(1,1,1,1)
  _ShadowColor("Shadow",Color)=(.68,.65,.75,1)
  _ToonEnabled("Two Tone",Float)=1
  _Threshold("Light Boundary",Range(-1,1))=.05
  _Cutoff("Alpha Cutoff",Range(0,1))=.1
  [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull",Float)=0
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  Pass {
   Tags {"LightMode"="UniversalForward"} Cull [_Cull]
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST, _BaseColor, _ShadowColor; float _Cutoff, _ToonEnabled, _Threshold;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct V {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float2 uv:TEXCOORD1;};
   V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);return o;}
   half4 frag(V i):SV_Target {half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;clip(c.a-_Cutoff);Light l=GetMainLight();half ndl=dot(normalize(i.normalWS),l.direction);half light=lerp(saturate(ndl*.5+.5),step(_Threshold,ndl),saturate(_ToonEnabled));return half4(c.rgb*lerp(_ShadowColor.rgb,half3(1,1,1),light),1);}
   ENDHLSL
  }
 }
}
