// Shared HLSL adapters. Coordinates follow Blender: Z up, distances specified per model.
#define M_PI_F 3.14159265358979323846
#define M_PI_2_F 1.57079632679489661923
#define M_2PI_F 6.28318530717958647692
#define M_1_PI_F 0.31830988618379067154
#define M_1_4PI_F 0.07957747154594766788
#define make_float2 float2
#define make_float3 float3
#define make_float4 float4
#define sinf sin
#define cosf cos
#define sqrtf sqrt
#define expf exp
#define powf pow
#define logf log
#define asinf asin
#define fmaxf max
#define fmax max
#define fabs abs
#define fabsf abs
#define mix lerp
#define len length
float sqr(float v){return v*v;}
float len_squared(float3 v){return dot(v,v);}
float reduce_add(float3 v){return v.x+v.y+v.z;}
float safe_sqrtf(float v){return sqrt(max(v,0));}
float3 XYZToRGB(float3 c){return max(float3(dot(c,float3(3.2404542,-1.5371385,-0.4985314)),dot(c,float3(-0.969266,1.8760108,0.041556)),dot(c,float3(0.0556434,-0.2040259,1.0572252))),0);}
float3 SunDirection(float elevation){return float3(cos(elevation),0,sin(elevation));}
float _Elevation, _Altitude, _Air, _Aerosols, _Ozone, _Diameter;
int _Width, _Height;
RWTexture2D<float4> _Sky;
RWStructuredBuffer<float4> _SunData;
