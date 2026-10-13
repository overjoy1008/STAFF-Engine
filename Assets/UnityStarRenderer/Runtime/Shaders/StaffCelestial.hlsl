#ifndef STAFF_CELESTIAL_INCLUDED
#define STAFF_CELESTIAL_INCLUDED
float4x4 _StaffCelestialRotation;
float _StaffStarVisibility;
float _StaffStarTime;
float4 _StaffMoonDisc;float _StaffMoonVisibility;
void StaffTwinkleTime_float(out float Out) { Out = _StaffStarTime; }
void StaffFarPlane_float(float3 Position, out float3 Out) {
 Out = _WorldSpaceCameraPos + mul((float3x3)_StaffCelestialRotation, normalize(Position)) * (_ProjectionParams.z * 0.99);
}
void StaffVisibility_float(float3 Position, float Alpha, out float Out) {
 float3 d = normalize(Position - _WorldSpaceCameraPos);
 float3 celestial = mul(transpose((float3x3)_StaffCelestialRotation), d);
 float seed = dot(celestial,float3(17.13,39.71,73.37));
 float wave = (.5+.5*sin(_StaffStarTime*(1.6+.4*sin(seed))+seed))*(.5+.5*sin(_StaffStarTime*2.73+seed*1.71));
 float twinkle = .4+.6*wave;
 float standout=1+2.5*smoothstep(.86,1,sin(seed*2.13));
 float moonAngle=2*asin(min(length(d-_StaffMoonDisc.xyz)*.5,1));
 float moonMask=1-_StaffMoonVisibility*(1-smoothstep(_StaffMoonDisc.w-fwidth(moonAngle),_StaffMoonDisc.w+fwidth(moonAngle),moonAngle));
 Out = Alpha * moonMask * standout * twinkle * saturate(_StaffStarVisibility) * smoothstep(-0.01, 0.09, d.y);
}
void StaffRadiance_float(float3 Color, out float3 Out) { Out = Color * max(1.0, _StaffStarVisibility); }
#endif
