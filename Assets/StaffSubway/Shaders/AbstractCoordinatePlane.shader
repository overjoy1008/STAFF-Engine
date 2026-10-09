Shader "STAFF/Environment/Abstract Coordinate Plane"
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
 SubShader { Tags { "RenderType"="AbstractAnalytic" "Queue"="Geometry" }
 Pass { Cull Off ZWrite On Blend SrcAlpha OneMinusSrcAlpha
 CGPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "AbstractAnalyticPlane.cginc"
 ENDCG
 } } Fallback Off
}
