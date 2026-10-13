// Generated from this game's upstream fragment: native ForwardAdd equations.
float _MultiLight;
#undef _IS_PASS_BASE
#define _IS_PASS_LIGHT
#define POINT
float4 StaffAdditionalFragment(vs_out i, bool vface)
{
    float4 ws_pos = mul(unity_ObjectToWorld, i.ws_pos);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    // GET LIGHT ATTENUATION FOR BOTH PASSES : 
    UNITY_LIGHT_ATTENUATION(atten, i, ws_pos.xyz);
    // FIX POTENTIAL ISSUES WITH ALPHA CLIPPING 
    float transparnecy_check = _IsTransparent;
    float testTresh =_AlphaTestThreshold;
    if(_IsTransparent) testTresh = 0.0f;

    // INITIALIZE VERTEX SHADER INPUTS : 
    float3 normal    = normalize(i.normal);
    float3 vs_normal = normalize(mul((float3x3)UNITY_MATRIX_V, normal));
    float3 view      = normalize(i.view);
    float3 vs_view   = normalize(mul((float3x3)UNITY_MATRIX_V, view));
    float2 uv        = i.uv.xy;
    float4 vcol      = i.v_col;
    float3 light = _WorldSpaceLightPos0.xyz;
    float hair_alpha = 1.0f;
    float3 rim_light = (float3)0.0f;
    float3 specular = (float3)0.0f;
    float3 emission_color = (float3)0.0f;
    float emis_area = 0.0f;
    float3 test_normal = normal;
    float3 tangents = i.tangent.xyz;

    // MATERIAL COLOR :
    float4 color = (_HairMaterial) ? _Color0 * _Color : _Color;

    if(!vface && _backfdceuv2) // use uv2 if vface is false
    { // so basically if its a backfacing face
        uv.xy = i.uv.zw;
        color = _BackColor;
        normal.z = normal.z * -1.0f;
        tangents.z = tangents.z * -1.0f;
    }

    color.a = 1.0f; // this prevents issues with the alpha value of the material being less than 1
    // might remove later

    if(_DissoveON)
    {
        // Coverage and dissolve clipping already run in the base fragment.
    }

    // INITIALIZE OUTPUT COLOR : 
    float4 out_color = color;

    // COMPUTE HALF floatTOR : 
    float3 half_vector = normalize(view + _WorldSpaceLightPos0);

    // DOT PRODUCTS : 
    float ndotl = dot(normal, light);
    float ndoth = dot(normal, half_vector);
    float ndotv = dot(normal, view);

    // SAMPLE TEXTURES : 
    float4 diffuse = _MainTex.Sample(sampler_MainTex, uv);
    float4 lightmap = _LightMap.Sample(sampler_linear_repeat, uv);
    float lightmap_alpha = _LightMap.Sample(sampler_linear_repeat, i.uv.xy).w;
    #if defined(faceishadow)
        float4 facemap = _FaceMap.Sample(sampler_linear_repeat, uv);
        float4 faceexp = _FaceExpression.Sample(sampler_linear_repeat, uv);
    #endif
    #if defined(use_emission)
        float4 emistex = _EmissionTex.Sample(sampler_linear_repeat, uv);
    #endif
    #if defined(second_diffuse)
        if(_UseSecondaryTex)
        {
            float2 secondary_uv[3] =
            {
                i.uv.xy, i.uv.zw, i.uv_2.xy
            };
            float4 secondary = _SecondaryDiff.Sample(sampler_linear_repeat, secondary_uv[_SecondaryUV % 3]);
            diffuse = lerp(diffuse, secondary, _SecondaryFade);
        }
    #endif

    // if(_StarrySky)
    // {
    //     diffuse = starry_sky(diffuse, uv);
    // }

    #if defined(can_shift)
        float diffuse_mask = packed_channel_picker(sampler_linear_repeat, _HueMaskTexture, uv, _DiffuseMaskSource);
        float rim_mask = packed_channel_picker(sampler_linear_repeat, _HueMaskTexture, uv, _RimMaskSource);
        float emission_mask = packed_channel_picker(sampler_linear_repeat, _HueMaskTexture, uv, _EmissionMaskSource);
        if(!_UseHueMask) 
        {
            diffuse_mask = 1.0f;
            rim_mask = 1.0f;
            emission_mask = 1.0f;
        }
    #endif
    
    #if defined (_IS_PASS_LIGHT)
        if(_FaceMaterial) normal = float3(0.5f, 0.5f, 1.0f);
        #if defined(POINT) || defined(SPOT)
            light = normalize(_WorldSpaceLightPos0.xyz - i.ws_pos.xyz);
        #endif
        
        // SHADOW
        // since this pass doesnt want the colors of the shadows, just use the shadow only functions: 
        ndotl = dot(normal, light);

        float3 shadow_area = (float3)1.0f;
        shadow_area = shadow_rate(ndotl, lightmap.y, i.v_col.x, 1.0f, 1.0f);
        // metalshadow = shadow_area_transition(lightmapao, vertexao, ndotl, material_id);
        #if defined(faceishadow)
            ndotl = dot(float3(0.5f, 0.5f, 1.0f), light);
            if(_FaceMaterial) shadow_area = ndotl;
        #endif

        float light_intesnity = max(0.001f, (0.299f * _LightColor0.r + 0.587f * _LightColor0.g + 0.114f * _LightColor0.b));
        float3 light_pass_color = ((diffuse.xyz * 1.0f) * _LightColor0.xyz) * atten * saturate(shadow_area);
        float3 light_color = lerp(light_pass_color.xyz, lerp(0.0f, min(light_pass_color, light_pass_color / light_intesnity), _WorldSpaceLightPos0.w), _FilterLight); // prevents lights from becoming too intense
        #if defined(POINT) || defined(SPOT)
        out_color.xyz = (light_color);
        #elif defined(DIRECTIONAL)
        out_color.xyz = 0.0f; // dont let extra directional lights add onto the model, this will fuck a lot of shit up
        #endif
    #endif
        
    return out_color;
}
#undef POINT
#undef _IS_PASS_LIGHT
#define _IS_PASS_BASE
