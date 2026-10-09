#ifndef STAFF_ABSTRACT_COLOR
#define STAFF_ABSTRACT_COLOR
// Both mesh and analytic surfaces discard albedo, BRDF lighting and reflections.
float3 StaffAbstractColor(float3 emission, float3 structuralGraphic) {
 return emission + structuralGraphic;
}
#endif
