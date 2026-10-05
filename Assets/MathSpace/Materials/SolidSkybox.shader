Shader "STAFF/Solid Skybox"
{
    Properties { _Color("Background", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            CBUFFER_END
            float4 Vert(float4 positionOS : POSITION) : SV_POSITION { return UnityObjectToClipPos(positionOS); }
            half4 Frag() : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
