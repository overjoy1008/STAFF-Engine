using System;
using System.Collections.Generic;
using UnityEngine;

namespace Staff.Characters
{
    // Per-player state: no shared project material is modified by the H key.
    public sealed class CharacterToonToggle : IDisposable
    {
        readonly Dictionary<Material, Material> converted = new Dictionary<Material, Material>();
        readonly Dictionary<Material, Material> originals = new Dictionary<Material, Material>();
        readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        static readonly int Enabled = Shader.PropertyToID("_ToonEnabled");
        public void Apply(GameObject model, bool enabled)
        {
            if (!model) return;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (!source) continue;
                    if (originals.TryGetValue(source, out var original)) source = original;
                    if (source.shader.name == "STAFF/Character Toon") continue;
                    var result = source;
                    if (enabled)
                    {
                        if (!converted.TryGetValue(source, out result))
                        {
                            result = new Material(Shader.Find("STAFF/Character Toon")) {name=source.name+" (Runtime Toon)"};
                            if (source.HasProperty("_BaseColor")) result.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                            if (source.HasProperty("_BaseMap"))
                            {
                                result.SetTexture("_BaseMap",source.GetTexture("_BaseMap"));
                                result.SetTextureScale("_BaseMap",source.GetTextureScale("_BaseMap"));
                                result.SetTextureOffset("_BaseMap",source.GetTextureOffset("_BaseMap"));
                            }
                            result.SetFloat("_Cull",source.HasProperty("_Cull")?source.GetFloat("_Cull"):2);
                            result.SetFloat("_Cutoff",source.HasProperty("_AlphaClip")&&source.GetFloat("_AlphaClip")>.5f?source.GetFloat("_Cutoff"):0);
                            converted.Add(source,result);originals.Add(result,source);
                        }
                    }
                    if (materials[i] != result) {materials[i]=result;changed=true;}
                }
                if (changed) renderer.sharedMaterials=materials;
                renderer.GetPropertyBlock(properties);
                properties.SetFloat(Enabled,enabled?1:0);
                renderer.SetPropertyBlock(properties);
                properties.Clear();
            }
        }
        public void Dispose()
        {
            foreach (var material in converted.Values) UnityEngine.Object.Destroy(material);
            converted.Clear();originals.Clear();
        }
    }
}
