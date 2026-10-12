// Original procedural cloud/starlight composition over the existing Nishita result.
// Cloud extinction is shared by atmosphere, solar disc, and stars.
sampler3D _CloudVolume;
sampler2D _CloudWeather, _StarMap;
float4 _CloudLayer, _WeatherWind;
float3 _StarEast, _StarMeridian, _StarPole;
float _WeatherClouds, _WeatherStars, _CloudDensity, _StarBrightness;
float _NightVisibility, _WeatherDaylight;
int _CloudSteps;

float cloudDensityAt(float3 position)
{
    float h = (position.y - _CloudLayer.x) / _CloudLayer.y;
    if (h <= 0 || h >= 1) return 0;
    float2 uv = position.xz / _CloudLayer.z - _WeatherWind.xy;
    float3 weather = tex2Dlod(_CloudWeather, float4(uv / 8, 0, 0)).rgb;
    float threshold = lerp(.76, .25, _CloudLayer.w);
    float body = saturate((weather.r - threshold) * 5);
    float top = lerp(.28, .97, saturate(weather.g * 1.5 - .25));
    float profile = smoothstep(0, .12, h) * (1 - smoothstep(top * .4, top, h));
    // Broad volume forms with gentle erosion: avoid equal-sized cotton balls or sharp toon bands.
    float3 q = float3(uv.x * 3, h * .6, uv.y * 3);
    float2 shape = tex3Dlod(_CloudVolume, float4(q, 0)).rg;
    float mass = (body + .13) * profile - (1 - shape.r) * .72 - (1 - shape.g) * .13;
    return max(0, mass) * 2.8 * _CloudDensity;
}

float3 celestialStars(float3 d)
{
    if (_WeatherStars < .5 || _NightVisibility <= 0) return 0;
    float3 p = float3(dot(d, _StarEast), dot(d, _StarPole), dot(d, _StarMeridian));
    float2 uv = float2(atan2(p.x, p.z) / (2 * UNITY_PI), asin(clamp(p.y, -1, 1)) / UNITY_PI + .5);
    float2 dx = ddx(uv), dy = ddy(uv);
    dx.x -= round(dx.x); dy.x -= round(dy.x);
    float3 stars = tex2Dgrad(_StarMap, uv, dx, dy).rgb;
    // Polaris marks the exact rotation axis (art-directed approximation of the real star).
    float angle = length(d - _StarPole);
    float radius = max(.0011, fwidth(angle) * .65);
    float polaris = exp(-angle * angle / (2 * radius * radius)) * .9;
    return (stars + polaris * float3(.79, .87, 1)) * _NightVisibility * _StarBrightness
        * smoothstep(-.01, .09, d.y);
}

float3 weatherComposite(float3 sky, float3 d, float2 pixel)
{
    sky += celestialStars(d);
    if (_WeatherClouds < .5 || _CloudLayer.w <= 0 || d.y < .035) return sky;
    float nearT = _CloudLayer.x / max(d.y, .004);
    float farT = (_CloudLayer.x + _CloudLayer.y) / max(d.y, .004);
    // Finite view distance prevents noisy, horizontally stretched samples at the horizon.
    float horizonFade = smoothstep(.055, .25, d.y);
    float stepLength = (farT - nearT) / _CloudSteps;
    float jitter = frac(sin(dot(floor(pixel), float2(12.9898, 78.233))) * 43758.5453);
    float3 sun = normalize(_SunDir.xyz);
    float3 lightDirection = normalize(float3(sun.x, max(.18, sun.y), sun.z));
    float twilight = (1 - smoothstep(0, .3, max(sun.y, 0))) * _WeatherDaylight;
    float3 lit = lerp(float3(.74, .84, 1), float3(1, .70, .42), twilight * .75);
    lit = lerp(float3(.026, .036, .067), lit, _WeatherDaylight);
    float3 shade = lerp(float3(.008, .014, .033), float3(.29, .40, .57), _WeatherDaylight);
    shade = lerp(shade, float3(.28, .20, .29), twilight * .4);
    float transmission = 1;
    float3 cloud = 0;
    [loop] for (int n = 0; n < _CloudSteps; n++)
    {
        float3 position = d * (nearT + (n + jitter) * stepLength);
        float density = cloudDensityAt(position);
        if (density > .001)
        {
            float shadow = 0;
            [unroll] for (int k = 1; k <= 3; k++)
                shadow += cloudDensityAt(position + lightDirection * (k * 170)) * .65;
            float light = exp(-shadow);
            float height = saturate((position.y - _CloudLayer.x) / _CloudLayer.y);
            float3 color = lerp(shade, lit, saturate(light * .85 + height * .2));
            float rim = pow(saturate(dot(d, sun)), 10) * light * .18 * _WeatherDaylight;
            color += rim * float3(1, .88, .72);
            float alpha = 1 - exp(-density * stepLength * .009);
            cloud += transmission * alpha * color;
            transmission *= 1 - alpha;
            if (transmission < .015) break;
        }
    }
    float3 cumulus = sky * transmission + cloud;
    // Sparse, stretched fragments in a separate higher layer add the reference's broken edges.
    float2 highUV = d.xz * ((_CloudLayer.x + _CloudLayer.y * 1.8) / max(d.y, .02)) / _CloudLayer.z;
    highUV -= _WeatherWind.zw;
    float wispNoise = tex2Dlod(_CloudWeather, float4(highUV / 8, 0, 0)).b;
    float wisps = smoothstep(.83, .97, wispNoise) * .08 * _CloudLayer.w * transmission;
    cumulus = lerp(cumulus, lit, wisps);
    return lerp(sky, cumulus, horizonFade);
}
