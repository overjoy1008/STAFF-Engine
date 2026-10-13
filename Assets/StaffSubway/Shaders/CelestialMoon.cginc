#ifndef STAFF_MOON_INCLUDED
#define STAFF_MOON_INCLUDED
sampler2D _MoonMap;
float4 _MoonDisc,_MoonParams,_MoonGlow,_MoonTint;
float3 _MoonRight,_MoonUp;
float3 moonComposite(float3 sky,float3 d){
 if(_MoonParams.w<=0)return sky;
 float front=dot(d,_MoonDisc.xyz);if(front<=0)return sky;
 float2 p=float2(dot(d,_MoonRight),dot(d,_MoonUp))/max(front*tan(_MoonDisc.w),.00001);
 float r=length(p),aa=max(fwidth(r),.001),mask=1-smoothstep(1-aa,1+aa,r);
 float horizon=smoothstep(-.002,.008,d.y),phase=_MoonParams.x*6.2831853;
 float litFraction=.5-.5*cos(phase);
 float halo=exp(-r*r/max(_MoonGlow.y*_MoonGlow.y,.01));
 sky+=_MoonTint.rgb*halo*_MoonGlow.x*litFraction*_MoonParams.w*horizon;
 if(mask<=0)return sky;
 float z=sqrt(max(0,1-dot(p,p)));float3 normal=float3(p,z);
 float c=cos(_MoonGlow.z),s=sin(_MoonGlow.z);float2 q=float2(c*p.x-s*p.y,s*p.x+c*p.y);
 float2 uv=float2(.5+atan2(q.x,z)/6.2831853,.5+asin(clamp(q.y,-1,1))/3.14159265);
 float3 albedo=tex2D(_MoonMap,uv).rgb;
 float illumination=max(0,dot(normal,float3(sin(phase),0,-cos(phase))));
 float3 surface=albedo*2.8*_MoonTint.rgb*_MoonParams.y*(_MoonParams.z+illumination);
 return lerp(sky,surface,mask*_MoonParams.w*horizon);
}
#endif
