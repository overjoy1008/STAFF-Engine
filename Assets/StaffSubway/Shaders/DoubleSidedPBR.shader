Shader "STAFF/Subway/Double Sided PBR" {
 Properties {
 _Color("Color",Color)=(1,1,1,1) _MainTex("Albedo",2D)="white"{}
 _Metallic("Metallic",Range(0,1))=0 _Glossiness("Smoothness",Range(0,1))=.5
 _MetallicGlossMap("Metallic Smoothness",2D)="white"{} _GlossMapScale("Smoothness Scale",Range(0,1))=1
 [HDR]_EmissionColor("Emission Color",Color)=(0,0,0,1) _EmissionMap("Emission",2D)="white"{}
 }
 SubShader { Tags {"RenderType"="Opaque"} LOD 300 Cull Off
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows addshadow
 #pragma target 3.0
 #pragma shader_feature_local _METALLICGLOSSMAP
 #pragma shader_feature_local _EMISSION
 sampler2D _MainTex,_MetallicGlossMap,_EmissionMap;
 fixed4 _Color;half _Metallic,_Glossiness,_GlossMapScale;half4 _EmissionColor;
 struct Input {float2 uv_MainTex;float2 uv_MetallicGlossMap;float2 uv_EmissionMap;float facing:VFACE;};
 void surf(Input IN,inout SurfaceOutputStandard o){
  fixed4 color=tex2D(_MainTex,IN.uv_MainTex)*_Color;o.Albedo=color.rgb;o.Alpha=1;
  #if defined(_METALLICGLOSSMAP)
  half4 mr=tex2D(_MetallicGlossMap,IN.uv_MetallicGlossMap);o.Metallic=mr.r;o.Smoothness=mr.a*_GlossMapScale;
  #else
  o.Metallic=_Metallic;o.Smoothness=_Glossiness;
  #endif
  #if defined(_EMISSION)
  o.Emission=tex2D(_EmissionMap,IN.uv_EmissionMap).rgb*_EmissionColor.rgb;
  #endif
  o.Normal=float3(0,0,IN.facing>=0?1:-1);
 }
 ENDCG
 } Fallback "Diffuse"
}
