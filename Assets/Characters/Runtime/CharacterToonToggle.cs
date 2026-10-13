using System;
using System.Collections.Generic;
using UnityEngine;

namespace Staff.Characters
{
    public enum CharacterShaderMode
    {
        CharacterToonSmooth, CharacterToonTwoTone,
        HoyoGenshin, HoyoStarRail, HoyoZenless, HoyoWutheringWaves
    }

    public sealed class CharacterToonToggle : IDisposable
    {
        public const int ModeCount = 6;
        public static bool IsCharacterToon(CharacterShaderMode mode) => (int)mode < 2;
        public static string ShaderName(CharacterShaderMode mode) => mode switch
        {
            CharacterShaderMode.HoyoGenshin => "HoyoToon/Genshin/Character",
            CharacterShaderMode.HoyoStarRail => "HoyoToon/Star Rail/Character",
            CharacterShaderMode.HoyoZenless => "HoyoToon/Zenless Zone Zero/Character",
            CharacterShaderMode.HoyoWutheringWaves => "HoyoToon/Wuthering Waves/Character",
            _ => "STAFF/Character Toon"
        };
        public static string Label(CharacterShaderMode mode) => mode switch
        {
            CharacterShaderMode.CharacterToonSmooth => "CharacterToon Smooth",
            CharacterShaderMode.CharacterToonTwoTone => "CharacterToon TwoTone",
            CharacterShaderMode.HoyoGenshin => "HoyoToon Genshin Impact",
            CharacterShaderMode.HoyoStarRail => "HoyoToon Honkai: Star Rail",
            CharacterShaderMode.HoyoZenless => "HoyoToon Zenless Zone Zero",
            CharacterShaderMode.HoyoWutheringWaves => "HoyoToon Wuthering Waves",
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        readonly Dictionary<(Material, CharacterShaderMode), Material> converted = new();
        readonly Dictionary<Material, Material> originals = new();
        readonly MaterialPropertyBlock properties = new();

        public void Apply(GameObject model, CharacterShaderMode mode)
        {
            if (!model) return;
            if ((uint)mode >= ModeCount) throw new ArgumentOutOfRangeException(nameof(mode));
            // Resource materials retain all five shader assets in player builds.
            var template = Resources.Load<Material>("StaffShaderModes/" + mode);
            var shader = template ? template.shader : Shader.Find(ShaderName(mode));
            if (!shader || !shader.isSupported) throw new InvalidOperationException("Unavailable shader: " + ShaderName(mode));
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (!source) continue;
                    if (originals.TryGetValue(source, out var original)) source = original;
                    // These are effect layers, not character surfaces.
                    if (source.shader.name == "STAFF/ExpressionCheekBlush") { materials[i] = source; continue; }
                    var key = (source, mode);
                    if (!converted.TryGetValue(key, out var result))
                    {
                        // Always start from the source, never from another mode's converted material.
                        result = template ? new Material(template) : new Material(shader);
                        result.name = source.name + " (" + Label(mode) + ")";
                        result.hideFlags = HideFlags.HideAndDontSave;
                        var tex = source.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                        var color = source.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                        var targetTex = IsCharacterToon(mode) ? "_BaseMap" : "_MainTex";
                        if (source.HasProperty(tex))
                        {
                            result.SetTexture(targetTex, source.GetTexture(tex));
                            result.SetTextureScale(targetTex, source.GetTextureScale(tex));
                            result.SetTextureOffset(targetTex, source.GetTextureOffset(tex));
                            if(result.HasProperty("_StaffSourceST")){var scale=source.GetTextureScale(tex);var offset=source.GetTextureOffset(tex);result.SetVector("_StaffSourceST",new Vector4(scale.x,scale.y,offset.x,offset.y));}
                        }
                        var tint = source.HasProperty(color) ? source.GetColor(color) : Color.white;
                        SetColor(result, "_BaseColor", tint);
                        SetColor(result, "_Color", tint);
                        // Game material IDs may select any of these albedo multipliers.
                        for (int j = 2; j <= 5; j++) SetColor(result, "_Color" + j, tint);
                        float cull = source.HasProperty("_Cull") ? source.GetFloat("_Cull") : 0;
                        SetFloat(result, "_Cull", cull); SetFloat(result, "_CullMode", cull);
                        float cutoff = source.HasProperty("_Cutoff") ? source.GetFloat("_Cutoff") : .1f;
                        SetFloat(result, "_Cutoff", cutoff);
                        SetFloat(result, "_StaffSourceOpacity", tint.a);
                        SetFloat(result, "_StaffSourceCutoff", Mathf.Max(.001f, cutoff));
                        SetFloat(result, "_ToonEnabled", mode == CharacterShaderMode.CharacterToonTwoTone ? 1 : 0);
                        // Requested legacy brightness/color-temperature settings only. Keep the
                        // game-specific shadow equations and all six modes.
                        if (!IsCharacterToon(mode))
                        {
                            SetFloat(result, "_StaffExposure", .8f);
                            SetFloat(result, "_StaffNeutralLighting", 1);
                            foreach (var property in new[] { "_PostShallowTint", "_PostShallowFadeTint", "_PostShadowTint", "_PostShadowFadeTint", "_PostFrontTint", "_PostSssTint" })
                                SetColor(result, property, Color.white);
                        }
                        // PMX does not carry game-authored outline normals/scale. Disable the
                        // optional outline passes rather than changing their shading equations.
                        if (!IsCharacterToon(mode))
                        {
                            SetFloat(result, "_EnableOutline", 0); SetFloat(result, "_OutlineEnabled", 0);
                            SetFloat(result, "_OutlineType", 0); SetFloat(result, "_Outline", 0);
                            result.SetShaderPassEnabled("StaffHoyoOutline", false);
                            result.SetShaderPassEnabled("StaffHoyoNyxOutline", false);
                        }
                        // PMX imports only provide albedo. Keep official Base defaults; do not
                        // fabricate SDF/packed maps or replace the game shadow algorithms.
                        converted.Add(key, result); originals.Add(result, source);
                    }
                    materials[i] = result;
                }
                renderer.sharedMaterials = materials;
                renderer.GetPropertyBlock(properties);
                properties.SetFloat("_StaffTwoTone", 0);
                properties.SetFloat("_StaffNeutralLighting", IsCharacterToon(mode) ? 0 : 1);
                properties.SetFloat("_StaffExposure", IsCharacterToon(mode) ? 1 : .8f);
                properties.SetFloat("_ToonEnabled", mode == CharacterShaderMode.CharacterToonTwoTone ? 1 : 0);
                renderer.SetPropertyBlock(properties); properties.Clear();
            }
            var faceLighting = model.GetComponent<CharacterFaceLighting>() ?? model.AddComponent<CharacterFaceLighting>();
            faceLighting.Configure(mode);
        }
        static void SetFloat(Material material, string name, float value) { if (material.HasProperty(name)) material.SetFloat(name, value); }
        static void SetColor(Material material, string name, Color value) { if (material.HasProperty(name)) material.SetColor(name, value); }
        public void Dispose()
        {
            foreach (var material in converted.Values) UnityEngine.Object.Destroy(material);
            converted.Clear(); originals.Clear();
        }
    }
}
