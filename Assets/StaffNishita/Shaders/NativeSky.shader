Shader "Staff/Nishita/Native Blender Panorama"
{
 Properties { _MainTex("Scene-linear Blender panorama", 2D) = "black" {} _Rotation("Rotation", Float)=0 }
 SubShader {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
  Cull Off ZWrite Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   sampler2D _MainTex; float _Rotation;
   struct v2f { float4 pos:SV_POSITION; float3 direction:TEXCOORD0; };
   v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.direction=v.vertex.xyz; return o; }
   float4 frag(v2f i):SV_Target {
    float3 d=normalize(i.direction);
    float2 uv=float2(frac(atan2(d.x,d.z)/(2*UNITY_PI)+0.5+_Rotation/360),asin(clamp(d.y,-1,1))/UNITY_PI+0.5);
    // Pre-expose to keep the physical solar disc inside a half-float framebuffer.
    return float4(tex2D(_MainTex,uv).rgb/256,1);
   }
   ENDCG
  }
 }
}
