Shader "Hidden/STAFF/LocalGodrays" {
 SubShader {Tags{"RenderPipeline"="UniversalPipeline"} Cull Off ZWrite Off ZTest Always
 HLSLINCLUDE
 #include "UnityCG.cginc"
 sampler2D _MainTex,_LocalDepth,_LocalAtlas,_LocalRays;
 float4 _MainTex_TexelSize,_LocalParameters,_LocalAtlasSize,_LocalRaysSize,_LocalPositions[4],_LocalColors[4];
 float3 _LocalCamera;float4x4 _LocalInvVP,_LocalMatrices[24];
 v2f_img Fullscreen(uint id:SV_VertexID){v2f_img o;float2 uv=float2((id<<1u)&2u,id&2u);o.pos=float4(uv*2-1,0,1);
 #if UNITY_UV_STARTS_AT_TOP
 uv.y=1-uv.y;
 #endif
 o.uv=uv;return o;}
 float3 World(float2 uv,float d){float2 xy=uv*2-1;
 #if UNITY_UV_STARTS_AT_TOP
 xy.y=-xy.y;
 #endif
 #if !defined(UNITY_REVERSED_Z)
 d=lerp(UNITY_NEAR_CLIP_VALUE,1,d);
 #endif
 float4 w=mul(_LocalInvVP,float4(xy,d,1));return w.xyz/max(abs(w.w),1e-7)*sign(w.w);}
 float Shadow(float3 p,uint light){
  float3 delta=p-_LocalPositions[light].xyz;float3 a=abs(delta);
  uint face=a.x>=a.y&&a.x>=a.z?(delta.x>0?0u:1u):a.y>=a.z?(delta.y>0?2u:3u):(delta.z>0?4u:5u);
  float4 clip=mul(_LocalMatrices[light*6u+face],float4(p,1));float2 uv=clip.xy/max(clip.w,1e-6)*.5+.5;
  #if UNITY_UV_STARTS_AT_TOP
  uv.y=1-uv.y;
  #endif
  uv=clamp(uv,.5/_LocalAtlasSize.z,1-.5/_LocalAtlasSize.z);
  uv=(uv+float2(face,light))/float2(6,_LocalParameters.x);
  float closest=tex2Dlod(_LocalAtlas,float4(uv,0,0)).r*_LocalPositions[light].w;
  return step(length(delta),closest+.025);
 }
 // Altered HLSL point-light port of three-good-godrays, Casey Primozic / N8python.
 // See three-good-godrays-LICENSE.txt. Sphere clipping limits work to each light volume.
 float4 March(v2f_img i):SV_Target{
  float raw=tex2D(_LocalDepth,i.uv).r;float3 ray=World(i.uv,raw)-_LocalCamera;float sceneDistance=length(ray);ray/=max(sceneDistance,1e-6);
  float noise=frac(52.9829189*frac(dot(i.uv*_MainTex_TexelSize.zw,float2(.06711056,.00583715))));float3 scattering=0;
  uint steps=(uint)_LocalParameters.y;
  [loop]for(uint l=0u;l<(uint)_LocalParameters.x;l++){
   float3 offset=_LocalCamera-_LocalPositions[l].xyz;float radius=_LocalPositions[l].w;
   float b=dot(offset,ray),disc=b*b-dot(offset,offset)+radius*radius;if(disc<=0)continue;
   float near=max(0,-b-sqrt(disc)),far=min(sceneDistance,-b+sqrt(disc));if(far<=near)continue;
   float stepSize=(far-near)/steps,illum=0;
   [loop]for(uint n=0u;n<steps;n++){
    float3 p=_LocalCamera+ray*(near+(n+noise)*stepSize);float attenuation=saturate(1-distance(p,_LocalPositions[l].xyz)/radius);
    illum+=Shadow(p,l)*attenuation*attenuation*stepSize*_LocalParameters.z;
   }
   float alignment=saturate(dot(ray,normalize(_LocalPositions[l].xyz-_LocalCamera+float3(1e-6,0,0))));
   float phase=.08+.92*alignment*alignment*alignment*alignment;
   scattering+=_LocalColors[l].rgb*(1-exp(-illum))*phase;
  }
  return float4(scattering*_LocalParameters.w,min(LinearEyeDepth(raw),60000));
 }
 float4 Blur(v2f_img i):SV_Target{
  float4 center=tex2D(_MainTex,i.uv);float3 sum=0;float total=0;
  [unroll]for(int y=-2;y<=2;y++)[unroll]for(int x=-2;x<=2;x++){
   float4 tap=tex2D(_MainTex,i.uv+float2(x,y)*abs(_MainTex_TexelSize.xy));float delta=(tap.a-center.a)/max(center.a,.001);
   float weight=exp(-.5*(x*x+y*y)/2.25-.5*delta*delta/.0025);sum+=tap.rgb*weight;total+=weight;
  }return float4(sum/max(total,1e-6),center.a);
 }
 float4 Composite(v2f_img i):SV_Target{
  float4 scene=tex2D(_MainTex,i.uv);float eye=min(LinearEyeDepth(tex2D(_LocalDepth,i.uv).r),60000);float2 p=i.uv*_LocalRaysSize.xy-.5,b=floor(p),f=frac(p);float3 sum=0;float total=0;
  [unroll]for(int y=0;y<=1;y++)[unroll]for(int x=0;x<=1;x++){
   float4 tap=tex2D(_LocalRays,(b+float2(x,y)+.5)*_LocalRaysSize.zw);float2 offset=float2(x,y)-f;float delta=(tap.a-eye)/max(eye,.001);
   float weight=exp(-dot(offset,offset)/2-.5*delta*delta/.0025);sum+=tap.rgb*weight;total+=weight;
  }scene.rgb+=sum/max(total,1e-6);return scene;
 }
 ENDHLSL
 Pass{Name "Local shadow raymarch" HLSLPROGRAM
 #pragma target 4.5
 #pragma vertex Fullscreen
 #pragma fragment March
 ENDHLSL}
 Pass{Name "Local bilateral" HLSLPROGRAM
 #pragma target 4.5
 #pragma vertex Fullscreen
 #pragma fragment Blur
 ENDHLSL}
 Pass{Name "Local composite" HLSLPROGRAM
 #pragma target 4.5
 #pragma vertex Fullscreen
 #pragma fragment Composite
 ENDHLSL}
 }
}
