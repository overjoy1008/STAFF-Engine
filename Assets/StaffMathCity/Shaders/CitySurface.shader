Shader "STAFF/City/Painted Surface" {
 Properties { _BaseColor("Pigment",Color)=(1,1,1,1) _BaseMap("Surface",2D)="white"{} _ShadowTint("Cool shadow",Color)=(.46,.57,.70,1) _Grain("Pigment grain",Range(0,1))=.08 _WorldScale("World texture scale",Float)=0 }
 SubShader {
  Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
  Cull Off
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  CBUFFER_START(UnityPerMaterial)
  float4 _BaseColor, _BaseMap_ST, _ShadowTint; float _Grain, _WorldScale;
  CBUFFER_END
  TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
  struct A {float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0;};
  struct V {float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; float fog:TEXCOORD3;};
  V vert(A i) {V o; o.positionWS=TransformObjectToWorld(i.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS); o.normalWS=TransformObjectToWorldNormal(i.normalOS); o.uv=TRANSFORM_TEX(i.uv,_BaseMap); o.fog=ComputeFogFactor(o.positionCS.z); return o;}
  ENDHLSL
  Pass {
   Name "PaintedForward" Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #pragma multi_compile_fog
   half4 frag(V i):SV_Target {
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    float ndl=dot(normalize(i.normalWS),sun.direction);
    float band=smoothstep(.04,.17,ndl)*lerp(.08,1,sun.shadowAttenuation);
    float2 uv=i.uv; if(_WorldScale>0){float3 an=abs(normalize(i.normalWS));uv=(an.y>an.x && an.y>an.z)?i.positionWS.xz:(an.x>an.z?i.positionWS.zy:i.positionWS.xy);uv*=_WorldScale;} float4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv);clip(tex.a-.4);float3 pigment=_BaseColor.rgb*tex.rgb;
    float noise=frac(sin(dot(floor(i.positionWS*75),float3(12.9898,78.233,37.719)))*43758.5453);
    pigment*=1+(noise-.5)*_Grain;
    float3 ambient=max(SampleSH(normalize(i.normalWS)),float3(.008,.008,.012));
    float3 color=pigment*(ambient*.6+lerp(_ShadowTint.rgb,float3(1.02,.99,.90),band)*sun.color*(.12+.88*band));
    color*=.93+.07*saturate(i.normalWS.y);
    return half4(MixFog(color,i.fog),1);
   }
   ENDHLSL
  }
  Pass {
   Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"}
   ZWrite On ZTest LEqual ColorMask 0 Cull Off
   HLSLPROGRAM
   #pragma vertex shadowVert
   #pragma fragment shadowFrag
   float3 _LightDirection;
   float4 shadowVert(A i):SV_POSITION {
    float3 w=TransformObjectToWorld(i.positionOS.xyz),n=TransformObjectToWorldNormal(i.normalOS);
    float4 p=TransformWorldToHClip(ApplyShadowBias(w,n,_LightDirection));
    #if UNITY_REVERSED_Z
    p.z=min(p.z,UNITY_NEAR_CLIP_VALUE*p.w);
    #else
    p.z=max(p.z,UNITY_NEAR_CLIP_VALUE*p.w);
    #endif
    return p;
   }
   half4 shadowFrag():SV_Target {return 0;}
   ENDHLSL
  }
  Pass {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment depthFrag
   half4 depthFrag(V i):SV_Target{clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a-.4);return i.positionCS.z;}
   ENDHLSL
  }
 }
}
