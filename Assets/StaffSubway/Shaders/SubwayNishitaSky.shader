Shader "STAFF/Subway/Nishita Sky" {
 Properties {
 _MainTex("Nishita scene-linear panorama",2D)="black"{}
 _Exposure("Sky exposure",Float)=0.125
 _Rotation("Rotation",Float)=0
 _DiscOnly("Sun disc only",Float)=0
 }
 SubShader {
 Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline"}
 Cull Off ZWrite Off
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 float _Exposure,_Rotation,_DiscOnly;
 struct V {float4 position:SV_POSITION;float3 direction:TEXCOORD0;};
 V vert(float4 position:POSITION){V o;o.position=TransformObjectToHClip(position.xyz);o.direction=position.xyz;return o;}
 half4 frag(V i):SV_Target {
 float3 d=normalize(i.direction);
 float2 uv=float2(frac(atan2(d.x,d.z)/(2*PI)+.5+_Rotation/360),asin(clamp(d.y,-1,1))/PI+.5);
 float3 sky=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb;
 // Match the baked panorama's physical solar disc; suppress atmospheric radiance only.
 if(_DiscOnly>.5){float elevation=radians(35),rotation=radians(75-_Rotation);float3 sun=float3(sin(rotation)*cos(elevation),sin(elevation),cos(rotation)*cos(elevation));float angle=acos(clamp(dot(d,sun),-1,1));float aa=max(fwidth(angle),.00001);float disc=1-smoothstep(radians(.2725)-aa,radians(.2725)+aa,angle);return half4(disc.xxx,1);}
 return half4(min(sky*_Exposure,8),1);
 }
 ENDHLSL }
 }
}
