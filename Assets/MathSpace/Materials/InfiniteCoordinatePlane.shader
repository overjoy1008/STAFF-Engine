Shader "STAFF/Infinite Coordinate Plane"
{
    Properties
    {
        _Kind ("0 Floor, 1 X Axis, 2 Z Axis", Float) = 0
        _MinorColor ("Minor Grid", Color) = (0.17,0.17,0.17,1)
        _MajorColor ("Major Grid", Color) = (0.30,0.30,0.30,1)
        _AxisWidth ("Axis Width in World Units", Float) = 0.12
        _CellSize ("Grid Cell Size", Float) = 1
        _Inverted ("Black-White Inversion", Float) = 0
    }
    SubShader
    {
        Tags {  "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Cull Off
            ZTest LEqual
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"
            
            float4x4 _StaffInverseViewProjection;
            float3 ComputeWorldSpacePosition(float2 uv,float depth,float4x4 invVP) {
                float4 clipPos=float4(uv*2-1,depth,1);
                #if UNITY_UV_STARTS_AT_TOP
                clipPos.y=-clipPos.y;
                #endif
                clipPos.y *= _ProjectionParams.x;
                float4 world=mul(invVP,clipPos);return world.xyz/world.w;
            }
            CBUFFER_START(UnityPerMaterial)
                float _Kind;
                half4 _MinorColor;
                half4 _MajorColor;
                float _AxisWidth;
                float _CellSize;
                float _Inverted;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            struct Output { half4 color : SV_Target; float depth : SV_Depth; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = float4(input.positionOS.xy, 0, 1);
                return output;
            }
            float Grid(float2 p, float spacing, float width)
            {
                float2 q = p / spacing;
                float2 derivative = max(fwidth(q), 0.00001);
                float2 distance = abs(frac(q + 0.5) - 0.5);
                float2 coverage = saturate((width / spacing * 0.5 - distance) / derivative + 0.5);
                float stability = 1.0 - smoothstep(0.18, 0.65, max(derivative.x, derivative.y));
                return max(coverage.x, coverage.y) * stability;
            }
            Output Frag(Varyings input)
            {
                float2 uv = input.positionCS.xy / _ScreenParams.xy;
                #if UNITY_REVERSED_Z
                    float nearDepth = 1;
                    float farDepth = 0;
                #else
                    float nearDepth = UNITY_NEAR_CLIP_VALUE;
                    float farDepth = 1;
                #endif
                float3 nearWS = ComputeWorldSpacePosition(uv, nearDepth, _StaffInverseViewProjection);
                float3 farWS = ComputeWorldSpacePosition(uv, farDepth, _StaffInverseViewProjection);
                float3 direction = farWS - nearWS;
                clip(abs(direction.y) - 0.00001);
                float t = -nearWS.y / direction.y;
                clip(t);
                clip(1.0 - t);
                float3 hitWS = nearWS + direction * t;
                float4 clipPosition = mul(UNITY_MATRIX_VP,float4(hitWS,1));
                Output output;
                output.depth = clipPosition.z / clipPosition.w;
                #if !UNITY_REVERSED_Z
                    output.depth = (output.depth - UNITY_NEAR_CLIP_VALUE) / (1.0 - UNITY_NEAR_CLIP_VALUE);
                #endif
                if (_Kind < 0.5)
                {
                    float minor = Grid(hitWS.xz, max(_CellSize, 0.01), 0.018);
                    float major = Grid(hitWS.xz, max(_CellSize, 0.01) * 5, 0.03);
                    float fade = 1.0 - smoothstep(25, 130, distance(hitWS.xz, _WorldSpaceCameraPos.xz));
                    output.color = half4(max(_MinorColor.rgb * minor, _MajorColor.rgb * major) * fade, 1);
                }
                else
                {
                    // X: z = 0; Z: x = 0. Analytic lines have no start/end vertices.
                    float distance = _Kind < 1.5 ? abs(hitWS.z) : abs(hitWS.x);
                    float derivative = max(fwidth(distance), 0.00001);
                    float coverage = saturate((_AxisWidth * 0.5 - distance) / derivative + 0.5);
                    clip(coverage - 0.001);
                    output.color = half4(1,1,1,coverage);
                }
                if (_Inverted > 0.5)
                {
                    #if defined(UNITY_COLORSPACE_GAMMA)
                        output.color.rgb = 1.0 - output.color.rgb;
                    #else
                        output.color.rgb = GammaToLinearSpace(1.0 - LinearToGammaSpace(output.color.rgb));
                    #endif
                }
                return output;
            }
            ENDHLSL
        }
    }
}
