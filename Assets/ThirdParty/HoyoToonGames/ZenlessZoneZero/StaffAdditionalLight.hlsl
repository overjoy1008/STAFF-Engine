// Generated from this game's upstream fragment: native ForwardAdd equations.
#undef _IS_PASS_BASE
#define _IS_PASS_LIGHT
#define POINT
float4 StaffAdditionalFragment(vs_out i, bool vface : SV_ISFRONTFACE)
{
    // intialize inputs : 
    float3 normal = normalize(i.normal);
    float3 view = normalize(i.view);
    float2 uv_a = i.uv_a.xy;
    float2 uv_b = i.uv_b.xy;

    float2 main_uv = uv_a;
    float2 light_uv = uv_a;

    float2 selector_uv[4] = 
    {
        i.uv_a.xy, i.uv_a.zw, i.uv_b.xy, i.uv_b.zw
    };

    if(((_DoubleSided) && (!vface)) || ((_SymmetryUV) && (i.uv_a.z > 1))) main_uv = selector_uv[_DoubleUV % 4]; // flip uv if backface culling is enabled
    if((_DoubleSided) && (!vface)) light_uv = selector_uv[_DoubleUV % 4]; // flip uv if backface culling is enabled

    // get light attenuation:
    UNITY_LIGHT_ATTENUATION(atten, i, i.ws_pos.xyz);

    // sample textures
    float4 diffuse = _MainTex.Sample(sampler_linear_repeat, main_uv);
    float4 light = _LightTex.Sample(sampler_linear_repeat, light_uv); // rg normal, b diffuse bias
    float4 other_data = _OtherDataTex.Sample(sampler_linear_repeat, light_uv); // r mat id, g metallic, b specular mask
    float4 other_data2 = _OtherDataTex2.Sample(sampler_linear_repeat, light_uv); // r transparency, g smoothness, b emission mask 

    if(_LegacyOtherData) other_data2.xyz = float3(diffuse.w, light.w, other_data.w);
    if(_UseLegacyFace) i.test.z = 1.0f;

    float alpha = diffuse.w;
    // diffuse color shifting
    if(_EnableHueShift && _EnableColorHue)
    {
        float mask = packed_channel_picker(sampler_linear_repeat, _HueMaskTexture, main_uv, _DiffuseMaskSource);
        mask = _UseHueMask ? mask : 1.0f;
        if(_EnableColorHue)diffuse.xyz = hue_shift(diffuse.xyz, other_data.x, _ColorHue, _ColorHue2, _ColorHue3, _ColorHue4, _ColorHue5, _GlobalColorHue, _AutomaticColorShift, _ShiftColorSpeed, 1);
        diffuse.xyz = saturate(diffuse.xyz);
    }

    float4 final_color = diffuse;

    #if defined(_IS_PASS_BASE)

        // eye shadow, eye highlight, and eye
        if(_MaterialType == 5 || _MaterialType == 6)
        {  
            #if defined(_IS_STENCIL)
                clip(-1);
            #endif
            diffuse = diffuse * i.eye_shadow;
            if(_EnableLUT) 
            {
                diffuse.xyz = LUT_2D(diffuse.xyz, _Lut2DTexParam);
            }
            if(_ApplyLighting)CalcLighting(normal, diffuse.xyz);
            
            return diffuse;
        }
        else if( _MaterialType == 2)
        {
            #if defined(_IS_STENCIL)
                clip(-1);
            #endif
            if(_EnableLUT) 
            {
                diffuse.xyz = LUT_2D(diffuse.xyz, _Lut2DTexParam);
            }
            if(_ApplyLighting)CalcLighting(normal, diffuse.xyz);
            return diffuse;
        }
        if(_MaterialType == 3)
        {
            float4 hair_shadow = float4(1.0f, 0.9f, 0.9f, 1.0f);
            #if defined(_IS_STENCIL)
                clip(-1);
            #endif
            if(_EnableLUT) 
            {
                hair_shadow.xyz = LUT_2D(hair_shadow.xyz, _Lut2DTexParam);
            }
            // if(_ApplyLighting)CalcLighting(normal, hair_shadow.xyz);
            return hair_shadow;
        }
        

        // get material id : 
        float material_id = _MaterialType == 1 ? i.bit_flag.z : other_data.x;
        float skin_id = Material_Detect(other_data.x).x;

        // normal mapping : 
        float3 bumped = (_MaterialType != 1 && _UseBumpMap) ? normal_mapping(light.rgb, i.normal, i.tangent, vface) : i.normal;
        bumped = normalize(bumped);

        float ndotl = dot(bumped, _WorldSpaceLightPos0.xyz);

        if(_MaterialType == 1) ndotl = shadow_area_face(_LightTex, i.test, _WorldSpaceLightPos0.xyz).x * 2.0f - 1.0f;
        float4 shadow = float4((float3)1.0f, 0.0f);
        float3 highlight = 0.0f;
        if(_MaterialType != 1)
        {
            float unity_shadow = 1;
            unity_shadow = SHADOW_ATTENUATION(i);
            unity_shadow = smoothstep(0.0f, 1.f, unity_shadow);

            if(!_UseSelfShadow) 
            {
                unity_shadow = 1.f;
            }
            shadow = shadow_body(bumped, _WorldSpaceLightPos0.xyz, light, other_data.x, unity_shadow);
            
            // return float4((float3)unity_shadow, 1.0f);
        }
        else
        {
            shadow.xyz = shadow_face(bumped, _WorldSpaceLightPos0.xyz, light, 1.0f, ndotl, i.test.z);
            float3 nose = nose_line(normalize(i.view), bumped, material_id, diffuse.w);
            diffuse.xyz = diffuse.xyz * nose;
            highlight = face_high(normal, _WorldSpaceLightPos0, view, _LightTex, i.test);
            // return float4(highlight.xxx, 1.0f);
        }

        float4 material_color;
        float4 check = material_id.xxxx < float4(0.2f, 0.4f, 0.6f, 0.8f);
        material_color = check.wwww ? _Color2 : _Color;
        material_color = check.zzzz ? _Color3 : material_color;
        material_color = check.yyyy ? _Color4 : material_color; 
        material_color = check.xxxx ? _Color5 : material_color;

        if(_MaterialType != 2 )final_color.xyz = saturate(diffuse.xyz * shadow.xyz);
        float3 specular_color = specular(bumped, view, _WorldSpaceLightPos0.xyz, other_data, other_data2, final_color.xyz, shadow.w, i.pos_model);
        specular_color = skin_id ? specular_color * 0.1f : specular_color;
        final_color.xyz = matcap_body(bumped, other_data.x, other_data2.z, uv_a, final_color);
        if(_MaterialType != 1) final_color.xyz = final_color.xyz + specular_color;
        
        final_color = final_color * material_color;
        float3 rim = (float3)0.0f;
        if(_RimGlow)
        {
            rim = ndotv_rim(bumped, view, _WorldSpaceLightPos0, other_data, other_data2, diffuse, i.bit_flag);
            float rim_mask = rim_screen_mask(normal, _WorldSpaceLightPos0, i.ss_model, i.ws_pos);
            rim_mask = saturate(pow(rim_mask, 1)*2.5);
            if(_EnableHueShift && _EnableRimHue)
            {
                float mask = packed_channel_picker(sampler_linear_repeat, _HueMaskTexture, i.uv_a.xy, _RimMaskSource);
                mask = _UseHueMask ? mask : 1.0f;
                if(_EnableRimHue)rim.xyz = hue_shift(rim.xyz, other_data.x, _RimHue, _RimHue2, _RimHue3, _RimHue4, _RimHue5, _GlobalRimHue, _AutomaticRimShift, _ShiftRimSpeed, mask);
            }
            rim = ((rim * 1.5) * rim_mask);
            final_color.xyz = rim + final_color;
        }
        
        if(_EnableLUT) 
        {
            final_color.xyz = LUT_2D(final_color.xyz, _Lut2DTexParam);
        }

        if(_ApplyLighting) CalcLighting(bumped, (final_color.xyz)); // apply environment lighting
        final_color.xyz = emission(final_color.xyz, other_data2, other_data, uv_a); // layer 1 emission
        if(_MaterialType == 1) final_color.xyz = final_color.xyz + highlight;
        if(_SecondaryEmission) final_color.xyz = secondary_emission(final_color.xyz, uv_a, uv_b, material_id); // layer 2 emission
        
        float2 screen_uv_real[4] =
        {
            i.uv_a.xy, i.uv_a.zw, i.uv_b.xy, i.uv_b.zw
        };

        float4 screen_uv[3] =
        {
            i.ss_model, (i.ws_pos), float4(screen_uv_real[_ScreenUVEnum % 4], 0.0f, 1.0f)
        };

        if(_ScreenImage) final_color.xyz = screen_image(final_color.xyz, uv_a, uv_b, screen_uv[_ScreenUVSource % 3], material_id, view, i.ws_pos); // star cock thing
        
        if(_UseAlpha) apply_alpha(final_color, alpha);

        #if defined(_IS_STENCIL)
            if(_EnableStencil)
            {
                if(_MaterialType == 4)
                {
                    alpha = other_data2.x;
                    alpha = pow(alpha, 1.0);
                    final_color.w = max(alpha, _MinStencilAlpha);
                }
            }
            else
            {
                discard;
            }
        #endif


        if(_DebugMode != 0)
        {
            if(_DebugDiffuse != 0 && _DebugMode != 0)
            {
                if(_DebugDiffuse == 1) return float4(diffuse.xyz, 1.0);
                else return float4(alpha.xxx, 1.0);
            }
            if(_DebugLightMap != 0)
            {
                if(_DebugLightMap == 1) return float4(light.xy, 1.0f,  1.0f);
                if(_DebugLightMap == 2) return float4(light.zzz, 1.0f);
                if(_DebugLightMap == 3) return float4(light.www, 1.0f);
                // if(_DebugLightMap == 4) return float4(light.www, 1.0f);
            }
            if(_DebugOtherData != 0)
            {
                if(_DebugOtherData == 1) return float4(other_data.xxx, 1.0f);
                if(_DebugOtherData == 2) return float4(other_data.yyy, 1.0f);
                if(_DebugOtherData == 3) return float4(other_data.zzz, 1.0f);
                if(_DebugOtherData == 4) return float4(other_data.www, 1.0f);
            }
            if(_DebugOtherData2 != 0)
            {
                if(_DebugOtherData2 == 1) return float4(other_data2.xxx, 1.0f);
                if(_DebugOtherData2 == 2) return float4(other_data2.yyy, 1.0f);
                if(_DebugOtherData2 == 3) return float4(other_data2.zzz, 1.0f);
                if(_DebugOtherData2 == 4) return float4(other_data2.www, 1.0f);
            }
            if(_DebugVertexColor != 0)
            {
                if(_DebugVertexColor == 1) return float4(i.color.xxx, 1.0f);
                if(_DebugVertexColor == 2) return float4(i.color.yyy, 1.0f);
                if(_DebugVertexColor == 3) return float4(i.color.zzz, 1.0f);
                if(_DebugVertexColor == 4) return float4(i.color.www, 1.0f);
            }
            if(_DebugUV != 0)
            {
                if(_DebugUV == 1) return float4(i.uv_a.xy, 0.0f, 1.0f);
                if(_DebugUV == 2) return float4(i.uv_a.zw, 0.0f, 1.0f);
                if(_DebugUV == 3) return float4(i.uv_b.xy, 0.0f, 1.0f);
                if(_DebugUV == 4) return float4(i.uv_b.zw, 0.0f, 1.0f);
                if(_DebugUV == 5) return float4(i.uv_b.zw * 0.5f + 0.5f, 1.0f, 1.0f);
            }
            if(_DebugTangent != 0)
            {
                if(_DebugTangent == 1) return float4(i.tangent.xyz, 1.0f);
            }
            if(_DebugNormalVector != 0)
            {
                if(_DebugNormalVector == 1) return float4(i.normal.xyz * 0.5 + 0.5, 1.0f);
                if(_DebugNormalVector == 2) return float4(i.normal.xyz, 1.0f);
                if(_DebugNormalVector == 3) return float4(bumped * 0.5 + 0.5, 1.0f);
                if(_DebugNormalVector == 4) return float4(bumped, 1.0f);
            }
            if(_DebugMatcap != 0)
            {
                return float4(matcap_body(bumped, other_data.x, other_data2.z, uv_a, float4(0.0f, 0.0f, 0.0f, 1.0f)).xyz, 1.0f);
            }
            if(_DebugSpecular) return float4(specular_color.xyz, 1.0f);
            if(_DebugRimLight) return float4(rim.xyz, 1.0f);
            if(_DebugFaceVector == 1) return float4(normalize(UnityObjectToWorldDir(_headForwardVector.xyz)).xyz, 1.0f);
            if(_DebugFaceVector == 2) return float4(normalize(UnityObjectToWorldDir(_headRightVector.xyz)).xyz, 1.0f);
            if(_DebugFaceVector == 3) return float4(normalize(UnityObjectToWorldDir(_headUpVector.xyz)).xyz, 1.0f);
            if(_DebugEmission != 0)
            {
                if(_DebugEmission == 1) 
                {
                    float4 debug_emission = float4(0.0f, 0.0f, 0.0f, 1.0f);
                    debug_emission.xyz = emission(debug_emission.xyz, other_data2, other_data, uv_a);
                    return debug_emission;
                }
                if(_DebugEmission == 2)
                {
                    float4 debug_emission = float4(0.0f, 0.0f, 0.0f, 1.0f);
                    debug_emission.xyz = secondary_emission(debug_emission.xyz, uv_a, uv_b, material_id); // layer 2 emission;
                    return debug_emission;
                }
                if(_DebugEmission == 3) // both
                {
                    float4 debug_emission = float4(0.0f, 0.0f, 0.0f, 1.0f);
                    debug_emission.xyz = emission(debug_emission.xyz, other_data2, other_data, uv_a);
                    debug_emission.xyz = secondary_emission(debug_emission.xyz, uv_a, uv_b, material_id); // layer 2 emission;

                    return debug_emission;
                }
            }
            if(_DebugMaterialIDs != 0)
            {
                if(_DebugMaterialIDs < 6)
                {
                    float ID = 0;
                    int array_ID = _DebugMaterialIDs;

                    ID = material_id < 0.8f ? 2 : 1;
                    ID = material_id < 0.6f ? 3 : ID;
                    ID = material_id < 0.4f ? 4 : ID;
                    ID = material_id < 0.2f ? 5 : ID;
                    
                    if(array_ID == ID)
                    {
                        return float4((float3)1.0f, 1.0f);
                    }
                    else
                    {
                        return float4((float3)0.0f, 1.0f);
                    }
                }
                else
                {
                    float4 ID_Color = float4((float3)0.0f, 1.0f);
                    ID_Color.xyz = material_id < 0.8f ? float3(0,1,0): float3(1,0,0);
                    ID_Color.xyz = material_id < 0.6f ? float3(0,0,1) : ID_Color.xyz;
                    ID_Color.xyz = material_id < 0.4f ? float3(1,1,1) : ID_Color.xyz;
                    ID_Color.xyz = material_id < 0.2f ? float3(0,0,0) : ID_Color.xyz;

                    return ID_Color;
                }
            }
            if(_DebugLights == 1) return float4((float3)0.0f, 1.0f);
        }


    #endif
    #if defined(_IS_PASS_LIGHT)
        float3 light_direction = _WorldSpaceLightPos0,xyz;
        #if defined(POINT) || defined(SPOT)
            light_direction = normalize(_WorldSpaceLightPos0.xyz - i.ws_pos.xyz);
        #endif
        float shadow_area = dot(normal, light_direction);
        if(_MaterialType == 1) shadow_area = shadow_area_face(_LightTex, i.test, light_direction);

        float light_intesnity = max(0.001f, (0.299f * _LightColor0.r + 0.587f * _LightColor0.g + 0.114f * _LightColor0.b));
        float3 light_pass_color = ((diffuse.xyz * 5.0f) * _LightColor0.xyz) * atten * shadow_area * 0.5f;
        float3 light_color = lerp(light_pass_color.xyz, lerp(0.0f, min(light_pass_color, light_pass_color / light_intesnity), _WorldSpaceLightPos0.w), _FilterLight); // prevents lights from becoming too intense
        #if defined(POINT) || defined(SPOT)
        final_color.xyz = (light_color) * 0.5f;
        #elif defined(DIRECTIONAL)
        final_color.xyz = 0.0f; // dont let extra directional lights add onto the model, this will fuck a lot of shit up
        #endif
        if((_MaterialType == 3 || _MaterialType == 5 || _MaterialType == 6)) clip(-1);   
        if(_DebugMode)
        {
            if(_DebugLights != 1) return float4((float3)0.0f, 1.0f);
        }     
    #endif
    return final_color;
}
#undef POINT
#undef _IS_PASS_LIGHT
#define _IS_PASS_BASE
