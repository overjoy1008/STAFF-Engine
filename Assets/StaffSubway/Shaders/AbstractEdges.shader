Shader "Hidden/STAFF/Abstract Edges" {
 Properties {_MainTex("Source",2D)="white"{}}
 SubShader {Cull Off ZWrite Off ZTest Always Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex,_Geometry;float4 _MainTex_TexelSize;float4 _Geometry_TexelSize;float _Imaginary;float _LineWidth,_NormalThreshold,_DepthThreshold;
 float test(float4 c,float4 n){
 // Excluded foreground (characters) never receives white lines.
  float normal=length(c.rgb-n.rgb);
 return n.a<=0?1:smoothstep(_NormalThreshold,_NormalThreshold*1.5,normal);
 }
 float4 invertEnvironment(float4 color){
 if(_Imaginary>.5){
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
 #if UNITY_UV_STARTS_AT_TOP
 if(_MainTex_TexelSize.y<0)guv.y=1-guv.y;
 #endif
 float4 source=tex2D(_MainTex,uv);float4 c=tex2D(_Geometry,guv);if(c.a<0)return source;if(c.a==0)return invertEnvironment(source);
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
 source.rgb=lerp(source.rgb,float3(.85,.85,.85),edge*(1-light));return invertEnvironment(source);
 }
 ENDCG }} Fallback Off
}
