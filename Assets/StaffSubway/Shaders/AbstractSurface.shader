Shader "STAFF/Environment/Abstract Surface" {
 Properties {
 _EmissionMap("Emission",2D)="white"{} [HDR]_EmissionColor("Emission Color",Color)=(0,0,0,1)
 _MainTex("Sign graphic",2D)="black"{} _Graphic("Preserve sign luminance",Float)=0
 _AlphaClip("Alpha Clip",Float)=0 _Cutoff("Cutoff",Float)=.1
 _Grid("Floor grid",Float)=0 _TileSize("Tile size",Float)=1.25
 }
 SubShader { Tags {"RenderType"="AbstractEnvironment" "Queue"="Geometry"} Cull Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 #include "AbstractColor.cginc"
 struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
 sampler2D _EmissionMap,_MainTex;float4 _EmissionMap_ST,_MainTex_ST;float4 _EmissionColor;float _AlphaClip,_Cutoff;float _Graphic,_Grid,_TileSize;
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 float4 frag(v2f i):SV_Target {
 float3 emission=tex2D(_EmissionMap,i.uv*_EmissionMap_ST.xy+_EmissionMap_ST.zw).rgb*_EmissionColor.rgb;
 float4 sampleGraphic=tex2D(_MainTex,i.uv*_MainTex_ST.xy+_MainTex_ST.zw);if(_AlphaClip>.5)clip(sampleGraphic.a-_Cutoff);float3 graphic=sampleGraphic.rgb;
 float luminance=dot(graphic,float3(.2126,.7152,.0722));
 float2 uv=i.world.xz/max(.01,_TileSize);float2 edge=abs(frac(uv-.5)-.5);float2 aa=max(fwidth(uv),.00001);
 float2 tileLines=1-smoothstep(0,aa*.75,edge);float grid=max(tileLines.x,tileLines.y)*_Grid*.18;
 return float4(StaffAbstractColor(emission,max(grid,smoothstep(.16,.8,luminance)*_Graphic)),1);
 }
 ENDCG }
 } Fallback Off
}
