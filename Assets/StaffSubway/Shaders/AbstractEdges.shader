Shader "Hidden/STAFF/Abstract Edges" {
 Properties {_MainTex("Source",2D)="white"{}}
 SubShader {Cull Off ZWrite Off ZTest Always Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex,_Geometry;float4 _MainTex_TexelSize;float4 _Geometry_TexelSize;float _Imaginary;float _LineWidth,_NormalThreshold,_DepthThreshold;
 sampler2D _EntrySurface;float _EntryFromAbstract,_EntryFromImaginary,_EntryTargetAbstract;float _EntryActive,_EntryProgress,_EntryRange,_EntryOrthographic;float3 _EntryOrigin;float4x4 _EntryInverseProjection,_EntryCameraToWorld;
 float3 surfacePosition(float2 uv,float depth){float4 v=mul(_EntryInverseProjection,float4(uv*2-1,.5,1));v.xyz/=v.w;float3 p=_EntryOrthographic>.5?float3(v.xy,-depth):v.xyz*(depth/max(.00001,-v.z));return mul(_EntryCameraToWorld,float4(p,1)).xyz;}
 float surfaceGrid(float3 world,float3 normal){
  float3 n=abs(normal);float2 q=n.y>=n.x&&n.y>=n.z?world.xz:n.x>=n.z?world.zy:world.xy;
  q/=1.25;float2 aa=max(fwidth(q),.0001);float2 d=abs(frac(q+.5)-.5);float2 lines=1-smoothstep(aa*.35,aa*1.25,d);return max(lines.x,lines.y)*(1-smoothstep(.3,1,max(aa.x,aa.y)));
 }
 float4 entry(float4 realColor,float4 abstractColor,float2 uv,float4 geometry){
  if(_EntryActive<.5)return abstractColor;
  if(geometry.a<0)return realColor;
  if(geometry.a==0)return lerp(realColor,abstractColor,(_EntryTargetAbstract<.5?smoothstep(0,.22,_EntryProgress):smoothstep(.7,1,_EntryProgress)));
  float3 world=surfacePosition(uv,geometry.a);
  float distanceXZ=length(world.xz-_EntryOrigin.xz);
  if(_EntryTargetAbstract<.5){
   // Sky first; then an inward material front, followed by a short grid pulse
   // on the restored material. The grid never precedes the returning surface.
   float p=saturate((_EntryProgress-.22)/.78);
   float arrival=.70*(1-sqrt(saturate(distanceXZ/max(_EntryRange,.001))));
   float local=p-arrival;
   float material=smoothstep(0,.065,local);
   float grid=smoothstep(.065,.105,local)*(1-smoothstep(.18,.30,local));
   float4 result=lerp(realColor,abstractColor,material);
   result.rgb=lerp(result.rgb,float3(.8,.8,.8),surfaceGrid(world,geometry.rgb*2-1)*grid*.85);
   return result;
  }
  float progress=_EntryProgress;
  float gridRadius=_EntryRange*pow(saturate(progress/.78),2);
  float fillRadius=_EntryRange*pow(saturate((progress-.20)/.80),2);
  float feather=max(.35,_EntryRange*.003);
  float gridMask=(1-smoothstep(gridRadius-feather,gridRadius+feather,distanceXZ))*smoothstep(0,.03,progress);
  float fillMask=(1-smoothstep(fillRadius-feather,fillRadius+feather,distanceXZ))*smoothstep(.20,.24,progress);
  float grid=surfaceGrid(world,geometry.rgb*2-1)*gridMask*(1-fillMask);
  float4 result=lerp(realColor,abstractColor,fillMask);
  result.rgb=lerp(result.rgb,lerp(float3(.8,.8,.8),float3(.025,.025,.025),_EntryFromImaginary),grid*.85);
  return result;
 }
 float test(float4 c,float4 n){
 // Excluded foreground (characters) never receives white lines.
  float normal=length(c.rgb-n.rgb);
 return n.a<=0?1:smoothstep(_NormalThreshold,_NormalThreshold*1.5,normal);
 }
 float4 invertColor(float4 color,float imaginary){
 if(imaginary>.5){
 // Complement in display (sRGB) space, after clamping HDR to its visible range.
 #if !defined(UNITY_COLORSPACE_GAMMA)
 color.rgb=GammaToLinearSpace(1-saturate(LinearToGammaSpace(color.rgb)));
 #else
 color.rgb=1-saturate(color.rgb);
 #endif
 }return color;
 }
 float4 frag(v2f_img i):SV_Target {
 float2 uv=i.uv;float2 guv=uv;
 // URP source copy and explicit geometry matrix have matching texture orientation.
 float4 realColor=tex2D(_MainTex,uv);float4 source=_EntryActive>.5?tex2D(_EntrySurface,uv):realColor;float4 c=tex2D(_Geometry,guv);if(c.a<0)return realColor;if(c.a==0){if(_EntryActive<.5)return invertColor(realColor,_Imaginary);float4 from=_EntryFromAbstract>.5?float4(_EntryFromImaginary.xxx,1):realColor;float4 to=_EntryTargetAbstract>.5?float4(_Imaginary.xxx,1):realColor;return entry(from,to,uv,c);}
 float2 d=_Geometry_TexelSize.xy*_LineWidth;float edge=0;
 float4 l=tex2D(_Geometry,guv-float2(d.x,0)),r=tex2D(_Geometry,guv+float2(d.x,0));
 float4 u=tex2D(_Geometry,guv+float2(0,d.y)),v=tex2D(_Geometry,guv-float2(0,d.y));
 edge=max(max(test(c,l),test(c,r)),max(test(c,u),test(c,v)));
 // Reciprocal depth is linear on a perspective-projected plane. This avoids
 // painting grazing floors/walls white just because their depth changes fast.
 float dx=abs(2/c.a-1/max(l.a,.001)-1/max(r.a,.001))*c.a;
 float dy=abs(2/c.a-1/max(u.a,.001)-1/max(v.a,.001))*c.a;
 edge=max(edge,smoothstep(_DepthThreshold,_DepthThreshold*2,max(dx,dy)));
 // Keep colored emissive pixels intact, including HDR values.
 float light=saturate(max(source.r,max(source.g,source.b))*8);
 if(_EntryActive<.5||_EntryTargetAbstract>.5)source.rgb=lerp(source.rgb,float3(.85,.85,.85),edge*(1-light));
 if(_EntryActive>.5&&_EntryFromAbstract>.5){float fromLight=saturate(max(realColor.r,max(realColor.g,realColor.b))*8);realColor.rgb=lerp(realColor.rgb,float3(.85,.85,.85),edge*(1-fromLight));realColor=invertColor(realColor,_EntryFromImaginary);}
 return entry(realColor,invertColor(source,_Imaginary),uv,c);
 }
 ENDCG }} Fallback Off
}
