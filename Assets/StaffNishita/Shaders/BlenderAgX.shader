Shader "Hidden/Staff/Nishita/BlenderAgX"
{
 Properties { _MainTex("Source",2D)="white"{} _Lut("Blender AgX",2D)="white"{} }
 SubShader { Cull Off ZWrite Off ZTest Always
  Pass {
   CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   sampler2D _MainTex, _Lut; float _Exposure;
   float3 lut(float3 p) {
    p=min(p,47);
    return tex2Dlod(_Lut,float4((p.b*48+p.r+0.5)/2304,(p.g+0.5)/48,0,0)).rgb;
   }
   float4 frag(v2f_img i):SV_Target {
    float3 rgb=max(tex2D(_MainTex,i.uv).rgb*exp2(_Exposure+8),exp2(-12));
    float3 p=saturate((log2(rgb)+12)/28)*47;
    // Explicit trilinear interpolation also works on GPUs without float-texture filtering.
    float3 base=floor(p), f=frac(p);
    float3 low=lerp(lerp(lut(base),lut(base+float3(1,0,0)),f.r),
                    lerp(lut(base+float3(0,1,0)),lut(base+float3(1,1,0)),f.r),f.g);
    float3 high=lerp(lerp(lut(base+float3(0,0,1)),lut(base+float3(1,0,1)),f.r),
                     lerp(lut(base+float3(0,1,1)),lut(base+float3(1,1,1)),f.r),f.g);
    float3 display=lerp(low,high,f.b);
    // The LUT contains display sRGB. Unity's linear project encodes the backbuffer again.
    return float4(GammaToLinearSpace(display),1);
   }
   ENDCG
  }
 }
}
