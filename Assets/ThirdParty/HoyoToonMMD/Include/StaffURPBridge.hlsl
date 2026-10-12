// STAFF URP adapter for the existing HoyoToon 5.2.7 ZZZ/MMD shader.
// Inspected kazama01 fork caf149b is still Built-in and has no ZZZ implementation.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#define UnityObjectToClipPos(v) TransformObjectToHClip((v).xyz)
#define UnityObjectToWorldDir(v) TransformObjectToWorldDir(v)
#define ShadeSH9(v) SampleSH((v).xyz)
#define ComputeGrabScreenPos(v) ComputeScreenPos(v)
#define _WorldSpaceLightPos0 float4(GetMainLight().direction,0)
#define _LightColor0 float4(GetMainLight().color,1)
#define UNITY_DECLARE_DEPTH_TEXTURE(t)
#define SAMPLE_DEPTH_TEXTURE(t,uv) SampleSceneDepth(uv)
#define SHADOW_COORDS(n) float4 shadowCoord:TEXCOORD##n;
#define TRANSFER_SHADOW(o) o.shadowCoord=TransformWorldToShadowCoord(o.ws_pos.xyz);
#define SHADOW_ATTENUATION(i) GetMainLight(TransformWorldToShadowCoord(i.ws_pos.xyz)).shadowAttenuation
#define UNITY_LIGHT_ATTENUATION(name,i,pos) half name=GetMainLight(TransformWorldToShadowCoord(pos)).shadowAttenuation
#define V2F_SHADOW_CASTER float4 pos:SV_POSITION
float3 _LightDirection, _LightPosition;
float4 StaffShadowPosition(float3 positionOS,float3 normalOS){
 float3 p=TransformObjectToWorld(positionOS);float3 n=TransformObjectToWorldNormal(normalOS);
 #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
 float3 l=normalize(_LightPosition-p);
 #else
 float3 l=_LightDirection;
 #endif
 float4 clip=TransformWorldToHClip(ApplyShadowBias(p,n,l));
 #if UNITY_REVERSED_Z
 clip.z=min(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
 #else
 clip.z=max(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
 #endif
 return clip;
}
#define TRANSFER_SHADOW_CASTER_NORMALOFFSET(o) o.pos=StaffShadowPosition(v.vertex.xyz,v.normal);
#define SHADOW_CASTER_FRAGMENT(i) return 0;
