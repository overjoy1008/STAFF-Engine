Shader "Hidden/STAFF/SolarOptics" {
 SubShader { Tags {"RenderPipeline"="UniversalPipeline"} Cull Off ZWrite Off ZTest Always
 HLSLINCLUDE
 #include "UnityCG.cginc"
 sampler2D _MainTex,_SolarSceneDepth,_SolarShadow,_SolarVisibility,_SolarRays,_VolumetricCloudsLightingTexture;
 float4 _MainTex_TexelSize,_SolarScreen,_SolarSunUV,_FlareParameters,_FlareOptions,_RayParameters,_RayColor,_SolarRaysSize;
 float3 _SolarCamera,_SolarSun;float _SolarExtent,_SolarClouds,_RaysEnabled,_RayStrength;float4x4 _SolarInvVP,_SolarLightVP,_SolarLightView;
 #include "ManciniLensFlare.hlsl"
 v2f_img Fullscreen(uint id:SV_VertexID){v2f_img o;float2 uv=float2((id<<1u)&2u,id&2u);o.pos=float4(uv*2-1,0,1);
 #if UNITY_UV_STARTS_AT_TOP
 uv.y=1-uv.y;
 #endif
 o.uv=uv;return o;}

 float EyeDepth(float2 uv){return LinearEyeDepth(tex2D(_SolarSceneDepth,uv).r);}
 float3 WorldPosition(float2 uv,float depth){
  float2 xy=uv*2-1;
  #if UNITY_UV_STARTS_AT_TOP
   xy.y=-xy.y;
  #endif
  #if !defined(UNITY_REVERSED_Z)
   depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
  #endif
  float4 w=mul(_SolarInvVP,float4(xy,depth,1));return w.xyz/max(abs(w.w),1e-7)*sign(w.w);
 }
 float4 Visibility(v2f_img i):SV_Target {
  float2 uv=_SolarSunUV.xy;
  if(_SolarSunUV.z<.5||any(uv<0)||any(uv>1))return float4(0,1,0,1);
  float visible=0,cloud=0;
  [unroll]for(int y=-2;y<=2;y++)[unroll]for(int x=-2;x<=2;x++){
   float2 tap=uv+float2(x,y)*_SolarScreen.zw*1.5;
   float d=tex2D(_SolarSceneDepth,tap).r;
   #if defined(UNITY_REVERSED_Z)
    float clear=step(d,0.000001);
   #else
    float clear=step(.999999,d);
   #endif
   float trans=_SolarClouds>.5?saturate(tex2D(_VolumetricCloudsLightingTexture,tap).a):1;
   visible+=clear*trans;cloud+=trans;
  }
  float edge=smoothstep(0,.04,min(min(uv.x,uv.y),min(1-uv.x,1-uv.y)));
  return float4(visible/25*edge,cloud/25,0,1);
 }
 // Altered directional-light HLSL port of three-good-godrays / N8python.
 // Integrates shadow-map visibility along the camera ray, then applies Beer-Lambert extinction.
 float2 ShadowInfo(float3 world){
  float4 clip=mul(_SolarLightVP,float4(world,1));float2 uv=clip.xy/clip.w*.5+.5;
  #if UNITY_UV_STARTS_AT_TOP
   uv.y=1-uv.y;
  #endif
  float lightDistance=-mul(_SolarLightView,float4(world,1)).z;
  if(any(uv<0)||any(uv>1)||lightDistance<0||lightDistance>_SolarExtent*3)return float2(0,lightDistance);
  float nearest=tex2Dlod(_SolarShadow,float4(uv,0,0)).r*_SolarExtent*3;
  return float2(step(lightDistance,nearest+.035),lightDistance);
 }
 float4 Raymarch(v2f_img i):SV_Target {
  float raw=tex2D(_SolarSceneDepth,i.uv).r;float linearDepth=LinearEyeDepth(raw);
  float3 w=WorldPosition(i.uv,raw);float3 ray=w-_SolarCamera;float distanceToScene=length(ray);ray/=max(distanceToScene,1e-6);
  float lengthToMarch=min(distanceToScene,_SolarExtent);
  float noise=frac(52.9829189*frac(dot(i.uv*_SolarScreen.xy,float2(.06711056,.00583715))));
  int samples=(int)round(_RayParameters.z+(_RayParameters.z/8+2)*noise);
  float illum=0;float factor=lengthToMarch*_RayParameters.x;
  float earlyOut=-log(max(1-_RayParameters.y,.001))*samples;
  [loop]for(int n=0;n<112;n++){
   if(n>=samples)break;
   float3 samplePos=_SolarCamera+ray*((n+noise)/samples)*lengthToMarch;
   float2 shadow=ShadowInfo(samplePos);
   illum+=shadow.x*factor*pow(saturate(1-shadow.y/(_SolarExtent*3)),_RayParameters.w);
   if(illum>earlyOut)break;
  }
  float cloud=tex2Dlod(_SolarVisibility,float4(.5,.5,0,0)).g;
  float angular=.25+.75*pow(saturate(dot(ray,_SolarSun)),3);
  illum=clamp(1-exp(-illum/samples),0,_RayParameters.y)*cloud*angular;
  return float4(illum,0,0,min(linearDepth,60000));
 }
 // Bilateral smoothing preserves both illumination boundaries and scene silhouettes.
 float4 Bilateral(v2f_img i):SV_Target {
  float4 center=tex2D(_MainTex,i.uv);float total=0,value=0;
  [unroll]for(int y=-2;y<=2;y++)[unroll]for(int x=-2;x<=2;x++){
   float4 tap=tex2D(_MainTex,i.uv+float2(x,y)*abs(_MainTex_TexelSize.xy));
   float delta=(tap.a-center.a)/max(center.a,.001);float dv=(tap.r-center.r)/.1;
   float weight=exp(-.5*(x*x+y*y)/2.25-.5*dv*dv-.5*delta*delta/.01);
   value+=tap.r*weight;total+=weight;
  }
  return float4(value/max(total,1e-6),0,0,center.a);
 }
 float4 Composite(v2f_img i):SV_Target {
  float4 scene=tex2D(_MainTex,i.uv);
  if(_RaysEnabled>.5){
   float eye=EyeDepth(i.uv);float2 pos=i.uv*_SolarRaysSize.xy-.5;float2 b=floor(pos),f=frac(pos);float total=0,illum=0;
   [unroll]for(int y=0;y<=1;y++)[unroll]for(int x=0;x<=1;x++){
    float4 tap=tex2D(_SolarRays,(b+float2(x,y)+.5)*_SolarRaysSize.zw);
    float2 d=float2(x,y)-f;float delta=(tap.a-min(eye,60000))/max(eye,.001);
    float weight=exp(-dot(d,d)/2-.5*delta*delta/.01);total+=weight;illum+=tap.r*weight;
   }
   scene.rgb=lerp(scene.rgb,_RayColor.rgb,illum/max(total,1e-6)*_RayStrength);
  }
  float visibility=tex2D(_SolarVisibility,float2(.5,.5)).r;
  if(_FlareOptions.w>0&&visibility>.0001)scene.rgb+=StaffLensFlare(i.uv,_SolarSunUV.xy)*visibility;
  return scene;
 }
 ENDHLSL
 Pass {Name "Sun visibility" HLSLPROGRAM
 #pragma target 4.5
 #pragma vertex Fullscreen
 #pragma fragment Visibility
 ENDHLSL }
 Pass {Name "Godrays raymarch" HLSLPROGRAM
 #pragma target 4.5
 #pragma vertex Fullscreen
 #pragma fragment Raymarch
 ENDHLSL }
 Pass {Name "Bilateral filter" HLSLPROGRAM
 #pragma target 4.5
 #pragma vertex Fullscreen
 #pragma fragment Bilateral
 ENDHLSL }
 Pass {Name "Solar composite" HLSLPROGRAM
 #pragma target 4.5
 #pragma vertex Fullscreen
 #pragma fragment Composite
 ENDHLSL }
 }
}
