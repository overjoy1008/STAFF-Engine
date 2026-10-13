sampler2D _StarMap;
float3 _StarEast, _StarMeridian, _StarPole;
float _WeatherStars, _StarBrightness, _NightVisibility, _StarExposure;
float3 celestialStars(float3 d)
{
    if (_WeatherStars < .5 || _NightVisibility <= 0) return 0;
    float3 p = float3(dot(d, _StarEast), dot(d, _StarPole), dot(d, _StarMeridian));
    float2 uv = float2(.5 - atan2(p.x, p.z) / (2 * UNITY_PI), asin(clamp(p.y, -1, 1)) / UNITY_PI + .5);
    float2 dx = ddx(uv), dy = ddy(uv);
    dx.x -= round(dx.x); dy.x -= round(dy.x);
    float3 stars = tex2Dgrad(_StarMap, uv, dx, dy).rgb;
    // NASA celestial map: RA 0 at the centre, RA increases toward the left.
    // Polaris is already in the map, slightly offset from the north celestial pole.
    stars = max(stars,0) * _StarExposure;
    // Compress HDR highlights while preserving each star's RGB ratios.
    stars /= 1 + max(stars.r,max(stars.g,stars.b));
    return stars * _NightVisibility * _StarBrightness
        * smoothstep(-.01, .09, d.y);
}
float3 weatherComposite(float3 sky,float3 d,float2 pixel) { return sky + celestialStars(d); }
