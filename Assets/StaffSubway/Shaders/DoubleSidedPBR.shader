Shader "STAFF/Subway/Double Sided PBR" {
 Properties {
 _Color("Color",Color)=(1,1,1,1) _MainTex("Albedo",2D)="white"{}
 _Metallic("Metallic",Range(0,1))=0 _Glossiness("Smoothness",Range(0,1))=.5
 _MetallicGlossMap("Metallic Smoothness",2D)="white"{} _GlossMapScale("Smoothness Scale",Range(0,1))=1
 [HDR]_EmissionColor("Emission Color",Color)=(0,0,0,1) _EmissionMap("Emission",2D)="white"{}
 }
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}

 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
 sampler2D _MainTex,_MetallicGlossMap,_EmissionMap,_ReflectionTex;
 CBUFFER_START(UnityPerMaterial)
 float4 _MainTex_ST,_Color,_EmissionColor;float _Metallic,_Glossiness,_GlossMapScale,_UseVertexColor,_Grid,_TileSize,_ReflectionStrength;
 CBUFFER_END
 struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;float2 lightmapUV:TEXCOORD1;float4 color:COLOR;};
 struct V{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;float2 lightmapUV:TEXCOORD3;float4 color:COLOR;float fog:TEXCOORD4;};
 V vert(A i){V o=(V)0;o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=TRANSFORM_TEX(i.uv,_MainTex);o.lightmapUV=i.lightmapUV*unity_LightmapST.xy+unity_LightmapST.zw;o.color=i.color;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
 void surface(V i,out SurfaceData s){s=(SurfaceData)0;float4 c=tex2D(_MainTex,i.uv)*_Color;s.albedo=c.rgb;s.alpha=c.a;s.metallic=_Metallic;s.smoothness=_Glossiness;s.normalTS=float3(0,0,1);s.occlusion=1;
 #if defined(_METALLICGLOSSMAP)
 float4 mr=tex2D(_MetallicGlossMap,i.uv);s.metallic=mr.r;s.smoothness=mr.a*_GlossMapScale;
 #endif
 #if defined(_EMISSION)
 s.emission=tex2D(_EmissionMap,i.uv).rgb*_EmissionColor.rgb;
 #endif
 
 }
 half4 frag(V i,bool facing:SV_IsFrontFace):SV_Target{SurfaceData s;surface(i,s);InputData d=(InputData)0;d.positionWS=i.positionWS;d.normalWS=normalize(i.normalWS)*(facing?1:-1);d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);d.bakedGI=SAMPLE_GI(i.lightmapUV,SampleSH(d.normalWS),d.normalWS);d.shadowMask=SAMPLE_SHADOWMASK(i.lightmapUV);half4 c=UniversalFragmentPBR(d,s);c.rgb=MixFog(c.rgb,i.fog);return c;}
 float3 _LightDirection,_LightPosition;
 V shadow(A i){V o=vert(i);float3 l=_LightDirection;
 #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
 l=normalize(_LightPosition-o.positionWS);
 #endif
 o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,l));
 #if UNITY_REVERSED_Z
 o.positionCS.z=min(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
 #else
 o.positionCS.z=max(o.positionCS.z,UNITY_NEAR_CLIP_VALUE*o.positionCS.w);
 #endif
 return o;}
 half4 depth(V i):SV_Target{return 0;}
 half4 normals(V i,bool f:SV_IsFrontFace):SV_Target{return half4(normalize(i.normalWS)*(f?1:-1),0);}
 V metaVert(A i){V o=vert(i);o.positionCS=UnityMetaVertexPosition(i.positionOS.xyz,i.lightmapUV,i.lightmapUV,unity_LightmapST,unity_DynamicLightmapST);return o;}
 half4 metaFrag(V i):SV_Target{SurfaceData s;surface(i,s);MetaInput m=(MetaInput)0;m.Albedo=s.albedo;m.Emission=s.emission;return MetaFragment(m);}
 ENDHLSL
Pass { Name "Forward" Tags{"LightMode"="UniversalForward"} Cull Off ZWrite On
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment frag
 #pragma shader_feature_local _METALLICGLOSSMAP
 #pragma shader_feature_local _EMISSION
 #pragma multi_compile _ LIGHTMAP_ON
 #pragma multi_compile _ DIRLIGHTMAP_COMBINED
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile _ _ADDITIONAL_LIGHTS
 #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_fog
 ENDHLSL
}
Pass { Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} Cull Off ZWrite On
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex shadow
 #pragma fragment depth
 #pragma shader_feature_local _METALLICGLOSSMAP
 #pragma shader_feature_local _EMISSION
 #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
 ENDHLSL
}
Pass { Name "DepthOnly" Tags{"LightMode"="DepthOnly"} Cull Off ZWrite On
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment depth
 #pragma shader_feature_local _METALLICGLOSSMAP
 #pragma shader_feature_local _EMISSION
 ENDHLSL
}
Pass { Name "DepthNormals" Tags{"LightMode"="DepthNormals"} Cull Off ZWrite On
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment normals
 #pragma shader_feature_local _METALLICGLOSSMAP
 #pragma shader_feature_local _EMISSION
 ENDHLSL
}
Pass { Name "Meta" Tags{"LightMode"="Meta"} Cull Off ZWrite On
 HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex metaVert
 #pragma fragment metaFrag
 #pragma shader_feature_local _METALLICGLOSSMAP
 #pragma shader_feature_local _EMISSION
 ENDHLSL
}
}
}