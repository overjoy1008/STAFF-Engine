// URP engine API bridge; game shading equations are preserved.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
// Previous HoyoToon neutral-light setting preserves illuminance and albedo colors.
float _StaffExposure, _StaffNeutralLighting;
float3 StaffLightColor(float3 color) { return lerp(color, dot(color,float3(.2126,.7152,.0722)).xxx, saturate(_StaffNeutralLighting)); }
// Per-fragment context for the upstream ForwardAdd equations, evaluated in URP.
static bool StaffAdditionalActive = false;
static float4 StaffAdditionalPosition, StaffAdditionalColor;
static float StaffAdditionalAttenuation, StaffAdditionalShadow;
void StaffSetAdditional(Light light, float3 sourcePosition) {
 StaffAdditionalActive=true;
 StaffAdditionalPosition=float4(sourcePosition+light.direction,1);
 StaffAdditionalColor=float4(StaffLightColor(light.color),1);
 StaffAdditionalAttenuation=light.distanceAttenuation*light.shadowAttenuation;
 StaffAdditionalShadow=light.shadowAttenuation;
}
#define UnityObjectToClipPos(v) TransformObjectToHClip((v).xyz)
#define UnityObjectToWorldDir(v) TransformObjectToWorldDir(v)
#define ShadeSH9(v) StaffLightColor(SampleSH((v).xyz))
#define ComputeGrabScreenPos(v) ComputeScreenPos(v)
#define _WorldSpaceLightPos0 (StaffAdditionalActive ? StaffAdditionalPosition : float4(GetMainLight().direction,0))
#define _LightColor0 (StaffAdditionalActive ? StaffAdditionalColor : float4(StaffLightColor(GetMainLight().color),1))
#define UNITY_DECLARE_DEPTH_TEXTURE(t)
#undef SAMPLE_DEPTH_TEXTURE
#define SAMPLE_DEPTH_TEXTURE(t,uv) SampleSceneDepth(uv)
#define SHADOW_COORDS(n) float4 shadowCoord:TEXCOORD##n;
#define TRANSFER_SHADOW(o) o.shadowCoord=TransformWorldToShadowCoord(o.ws_pos.xyz);
#define SHADOW_ATTENUATION(i) (StaffAdditionalActive ? StaffAdditionalShadow : GetMainLight(TransformWorldToShadowCoord(i.ws_pos.xyz)).shadowAttenuation)
#define UNITY_LIGHT_ATTENUATION(name,i,pos) half name=(StaffAdditionalActive ? StaffAdditionalAttenuation : GetMainLight(TransformWorldToShadowCoord(pos)).shadowAttenuation)
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

#define LinearEyeDepth(z) LinearEyeDepth(z,_ZBufferParams)
#undef UNITY_LIGHTMODEL_AMBIENT
#define UNITY_LIGHTMODEL_AMBIENT float4(SampleSH(float3(0,1,0)),1)

#define UNITY_INITIALIZE_OUTPUT(type,name) name=(type)0;

float _StaffSourceOpacity, _StaffSourceCutoff;
float4 _StaffSourceST;
#define fixed half
#define fixed2 half2
#define fixed3 half3
#define fixed4 half4
