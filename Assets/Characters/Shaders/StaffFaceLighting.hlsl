#ifndef STAFF_FACE_LIGHTING_INCLUDED
#define STAFF_FACE_LIGHTING_INCLUDED
float _StaffFaceEnabled, _StaffSkinStrength;
float4 _StaffFaceCenter, _StaffFaceRight, _StaffFaceUp, _StaffFaceForward, _StaffFaceRadius, _StaffSkinScale;
float4 _StaffLeftEyePatch, _StaffRightEyePatch, _StaffMouthPatch;
float4 _StaffLeftEyeNormal, _StaffRightEyeNormal, _StaffMouthNormal;
float StaffDetailMask(float2 p, float4 patch) {
 float distance=length((p-patch.xy)/max(patch.zw,float2(.001,.001)));
 return 1-smoothstep(.65,1,distance);
}
float3 StaffFaceNormal(float3 positionWS, float3 originalNormal) {
 if(_StaffFaceEnabled<.5) return originalNormal;
 float3 offset=positionWS-_StaffFaceCenter.xyz;
 float2 p=float2(dot(offset,_StaffFaceRight.xyz),dot(offset,_StaffFaceUp.xyz))/max(_StaffFaceRadius.xy,float2(.001,.001));
 // Exact original normal outside the small eye/lip regions. Keep the central nose intact.
 float noseProtection=smoothstep(.20,.27,abs(p.x));
 float left=StaffDetailMask(p,_StaffLeftEyePatch)*noseProtection;
 float right=StaffDetailMask(p,_StaffRightEyePatch)*noseProtection;
 float mouth=StaffDetailMask(p,_StaffMouthPatch);
 if(left+right+mouth<=0) return originalNormal;
 float3 normal=lerp(originalNormal,_StaffLeftEyeNormal.xyz,left);
 normal=lerp(normal,_StaffRightEyeNormal.xyz,right);
 return normalize(lerp(normal,_StaffMouthNormal.xyz,mouth));
}
float3 StaffSkinColor(float3 albedo) {return albedo*lerp(float3(1,1,1),_StaffSkinScale.rgb,saturate(_StaffSkinStrength));}
#endif
