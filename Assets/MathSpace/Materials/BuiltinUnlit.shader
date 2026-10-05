Shader "STAFF/Unlit" {
 Properties { _BaseMap("Texture",2D)="white"{} _BaseColor("Color",Color)=(1,1,1,1) _Cutoff("Cutoff",Range(0,1))=.1 _AlphaClip("Alpha Clip",Float)=0 [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull",Float)=2 [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source",Float)=1 [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination",Float)=0 _ZWrite("Depth Write",Float)=1 }
 SubShader { Tags { "RenderType"="Opaque" } Pass { Cull [_Cull] Blend [_SrcBlend] [_DstBlend] ZWrite [_ZWrite]
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _BaseMap;float4 _BaseMap_ST,_BaseColor;float _Cutoff,_AlphaClip;
 struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 V vert(appdata_base v){V o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.texcoord,_BaseMap);return o;}
 fixed4 frag(V i):SV_Target{fixed4 c=tex2D(_BaseMap,i.uv)*_BaseColor;if(_AlphaClip>.5)clip(c.a-_Cutoff);return c;}
 ENDCG
 } }
}
