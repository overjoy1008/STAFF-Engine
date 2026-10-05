Shader "STAFF/Cartesian Floor Lines"
{
    Properties
    {
        _BaseColor ("Line Color", Color) = (0.23, 0.23, 0.23, 1)
        _FadeStart ("Fade Start (World Units)", Float) = 15
        _FadeEnd ("Fade End (World Units)", Float) = 40
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"  "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Off
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _FadeStart;
                float _FadeEnd;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = mul(unity_ObjectToWorld,input.positionOS).xyz;
                output.positionCS = mul(UNITY_MATRIX_VP,float4(output.positionWS,1));
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float fade = 1.0 - smoothstep(_FadeStart, max(_FadeStart + 0.01, _FadeEnd), length(input.positionWS.xz));
                return half4(_BaseColor.rgb * fade, 1);
            }
            ENDHLSL
        }
    }
}
