// Solar disc interpolation and limb darkening adapted from Blender v5.1.2
// intern/cycles/kernel/svm/sky.h, Blender Foundation, Apache-2.0.
Shader "STAFF/Subway/Live Nishita Presets"
{
 Properties { [HideInInspector] _StarMap("NASA celestial map",2D)="black"{} _SkyCurrent("Current atmosphere",2D)="black"{} _SkyPrevious("Previous atmosphere",2D)="black"{} }
 SubShader {
 Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"}
 Cull Off ZWrite Off
 Pass {
 CGPROGRAM
 #pragma target 4.5
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 Texture2D<float4> _SkyCurrent,_SkyPrevious;
 StructuredBuffer<float4> _SunData;
 sampler2D _Lut;float _Exposure,_DiscOnly;
 float3 lut(float3 p){p=min(p,47);return tex2Dlod(_Lut,float4((p.b*48+p.r+.5)/2304,(p.g+.5)/48,0,0)).rgb;}
 float3 agx(float3 rgb){rgb=max(rgb*exp2(_Exposure+8),exp2(-12));float3 p=saturate((log2(rgb)+12)/28)*47;float3 b=floor(p),f=frac(p);float3 low=lerp(lerp(lut(b),lut(b+float3(1,0,0)),f.r),lerp(lut(b+float3(0,1,0)),lut(b+float3(1,1,0)),f.r),f.g);float3 high=lerp(lerp(lut(b+float3(0,0,1)),lut(b+float3(1,0,1)),f.r),lerp(lut(b+float3(0,1,1)),lut(b+float3(1,1,1)),f.r),f.g);return GammaToLinearSpace(lerp(low,high,f.b));}
 float4 _LutSize,_SunDir;
 float _Rotation,_Elevation,_Radius,_SunIntensity,_SunDisc,_Horizon,_Brightness,_Blend;
#include "NishitaWeather.cginc"
 struct v2f{float4 position:SV_POSITION;float3 direction:TEXCOORD0;};
 v2f vert(appdata_base v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.direction=v.vertex.xyz;return o;}
 float3 readSky(Texture2D<float4> tex,float2 uv){
  // Explicit bilinear filtering is portable even for float textures on Metal.
  float2 p=uv*_LutSize.xy-.5;int2 a=int2(floor(p));float2 f=frac(p);
  int x0=(a.x+int(_LutSize.x))%int(_LutSize.x),x1=(x0+1)%int(_LutSize.x);
  int y0=clamp(a.y,0,int(_LutSize.y)-1),y1=clamp(a.y+1,0,int(_LutSize.y)-1);
  return lerp(lerp(tex.Load(int3(x0,y0,0)).xyz,tex.Load(int3(x1,y0,0)).xyz,f.x),
              lerp(tex.Load(int3(x0,y1,0)).xyz,tex.Load(int3(x1,y1,0)).xyz,f.x),f.y);
 }
 float4 frag(v2f i):SV_Target{
  float3 d=normalize(i.direction);float elevation=asin(clamp(d.y,-1,1));
  float2 uv=float2(frac((atan2(d.x,d.z)-_Rotation)/(2*UNITY_PI)+.5),sign(elevation)*sqrt(abs(elevation)*2/UNITY_PI)*.5+.5);
  float3 xyz=lerp(readSky(_SkyPrevious,uv),readSky(_SkyCurrent,uv),_Blend);
  // Stable angle near the solar centre, including tiny radii.
  float angle=2*asin(min(length(d-_SunDir.xyz)*.5,1));
  float aa=max(fwidth(angle),1e-6);
  float coverage=1-smoothstep(max(_Radius-aa,0),_Radius+aa,angle);
  if(_DiscOnly>.5)return float4((_SunDisc>0&&elevation>_Horizon?coverage:0).xxx,1);
  if(_SunDisc>0 && _Radius>0 && coverage>0 && elevation>_Horizon){
   float y=saturate((elevation-_Elevation)/max(2*_Radius,1e-6)+.5);
   float limb=1-.6*(1-sqrt(max(1-pow(angle/max(_Radius,1e-6),2),0)));
   xyz+=lerp(_SunData[0].xyz,_SunData[1].xyz,y)*_SunIntensity*limb*coverage;
  }
  float3 rgb=float3(dot(xyz,float3(3.2404542,-1.5371385,-.4985314)),dot(xyz,float3(-.969266,1.8760108,.041556)),dot(xyz,float3(.0556434,-.2040259,1.0572252)));
  return float4(weatherComposite(agx(max(rgb,0)*_Brightness),d,i.position.xy),1);
 }
 ENDCG
 }
 }
}
