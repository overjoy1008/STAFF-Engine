Shader "STAFF/City/Painted Sky" {
 Properties { _Top("Zenith",Color)=(.22,.47,.70,1) _Horizon("Horizon",Color)=(.84,.87,.83,1) }
 SubShader {
 Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline"}
 Cull Off ZWrite Off
 Pass {
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float4 _Top,_Horizon;
 struct V {float4 position:SV_POSITION;float3 ray:TEXCOORD0;};
 V vert(float4 p:POSITION){V o;o.position=TransformObjectToHClip(p.xyz);o.ray=p.xyz;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 a=floor(p),b=frac(p);b=b*b*(3-2*b);return lerp(lerp(hash(a),hash(a+float2(1,0)),b.x),lerp(hash(a+float2(0,1)),hash(a+1),b.x),b.y);}
 float fbm(float2 p){return noise(p)*.55+noise(p*2.03)*.28+noise(p*4.07)*.12+noise(p*8.13)*.05;}
 half4 frag(V i):SV_Target {
 float3 d=normalize(i.ray);float h=saturate(d.y);
 float3 color=lerp(_Horizon.rgb,_Top.rgb,pow(h,.42));
 float2 uv=d.xz/max(.12,d.y+.15)*1.7+float2(11,3);
 float n=fbm(uv),density=smoothstep(.51,.65,n)*smoothstep(.01,.12,d.y);
 float3 cloud=lerp(float3(.66,.73,.79),float3(1,.96,.86),smoothstep(.50,.72,n));
 color=lerp(color,cloud,density*.95);
 return half4(color,1);
 }
 ENDHLSL
 }
 }
}
