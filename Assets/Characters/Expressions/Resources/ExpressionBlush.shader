Shader "STAFF/ExpressionCheekBlush" {
 Properties { _Strength ("Blush", Range(0,1)) = 0 }
 SubShader {
 Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
 Pass {
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off
 ZTest LEqual
 Cull Back
 Offset -1, -1
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata { float4 vertex:POSITION; float4 color:COLOR; };
 struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; };
 float _Strength;
 v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;return o; }
 fixed4 frag(v2f i):SV_Target { return fixed4(1.0,0.22,0.34,i.color.a*_Strength*0.6); }
 ENDCG
 }
 }
}
