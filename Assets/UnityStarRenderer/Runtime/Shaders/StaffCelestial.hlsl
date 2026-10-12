#ifndef STAFF_CELESTIAL_INCLUDED
#define STAFF_CELESTIAL_INCLUDED
float4x4 _StaffCelestialRotation;
float _StaffStarVisibility;
float _StaffStarTime;
void StaffTwinkleTime_float(out float Out) { Out = _StaffStarTime; }
void StaffFarPlane_float(float3 Position, out float3 Out) {
 Out = _WorldSpaceCameraPos + mul((float3x3)_StaffCelestialRotation, normalize(Position)) * (_ProjectionParams.z * 0.99);
}
void StaffVisibility_float(float3 Position, float Alpha, out float Out) {
 float3 d = normalize(Position - _WorldSpaceCameraPos);
 Out = Alpha * saturate(_StaffStarVisibility) * smoothstep(-0.01, 0.09, d.y);
}
void StaffRadiance_float(float3 Color, out float3 Out) { Out = Color * max(1.0, _StaffStarVisibility); }
#endif
