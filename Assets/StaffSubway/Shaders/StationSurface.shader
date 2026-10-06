Shader "STAFF/Subway/Station Surface" {
 Properties {
 _Color("Tint",Color)=(1,1,1,1) _MainTex("Albedo",2D)="white"{} _UseVertexColor("Vertex Color",Range(0,1))=0
 _Metallic("Metallic",Range(0,1))=0.2 _Glossiness("Smoothness",Range(0,1))=0.5
 _Grid("Floor tile grid",Range(0,1))=0 _TileSize("Tile size metres",Float)=1.2
 _ReflectionTex("Planar reflection",2D)="black"{} _ReflectionStrength("Reflection strength",Range(0,1))=0
 }
 SubShader { Tags {"RenderType"="Opaque"} LOD 300
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert
 #pragma target 3.0
 sampler2D _MainTex,_ReflectionTex;fixed4 _Color;half _UseVertexColor,_Metallic,_Glossiness,_Grid,_TileSize,_ReflectionStrength;
 struct Input {float2 uv_MainTex;float3 worldPos;float4 screenPos;float4 color:COLOR;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.color=v.color;}
 void surf(Input IN,inout SurfaceOutputStandard o){
 fixed3 color=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb*lerp(1,IN.color.rgb,_UseVertexColor);
 float2 uv=IN.worldPos.xz/_TileSize;float2 edge=abs(frac(uv-.5)-.5);float2 edgeAA=1-smoothstep(float2(.002,.002),float2(.002,.002)+fwidth(uv),edge);float gridLine=max(edgeAA.x,edgeAA.y);
 float grain=frac(sin(dot(floor(IN.worldPos.xz*180),float2(12.9898,78.233)))*43758.54);
 color*=lerp(1,lerp(.93,1.04,grain),_Grid);color=lerp(color,color*.22,gridLine*_Grid);
 o.Albedo=color;o.Metallic=_Metallic;o.Smoothness=_Glossiness;o.Alpha=1;
 float2 screen=IN.screenPos.xy/IN.screenPos.w;
 fixed3 reflected=tex2Dbias(_ReflectionTex,float4(screen,0,2.2)).rgb;
 o.Emission=min(reflected,1.5)*_ReflectionStrength*(1-.7*gridLine*_Grid);
 }
 ENDCG
 } FallBack "Standard"
}
