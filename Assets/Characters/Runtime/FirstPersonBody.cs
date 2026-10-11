using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Staff.Characters
{
    // Camera-local transparent head pass. All original triangles remain skinned and rendered.
    // Original geometry is restored after the camera renders, including in the Editor.
    public sealed class FirstPersonBody : System.IDisposable
    {
        sealed class Skin
        {
            public SkinnedMeshRenderer renderer, shadow;
            public Mesh original, filtered;
            public ShadowCastingMode casting;
            public float[] weights;
            public Material[] materials;
        }
        readonly List<Skin> skins = new List<Skin>();
        Animator animator;
        Material transparent;
        bool applied;
        public int FilteredRenderers => skins.Count;
        public void Bind(Animator next)
        {
            if (animator == next) return;
            Dispose(); animator = next;
            if (!animator || !animator.isHuman) return;
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (!head) return;
            var shader = Resources.Load<Shader>("FirstPersonTransparentHead");
            if (!shader) { Debug.LogError("First-person transparent head shader is missing."); return; }
            transparent = new Material(shader) { hideFlags=HideFlags.DontSave };
            foreach (var renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.name == "First-person full-body shadow") continue;
                var mesh = renderer.sharedMesh;
                if (!mesh || !mesh.isReadable) continue;
                var bones = renderer.bones;
                var hidden = new bool[bones.Length];
                for (int i = 0; i < bones.Length; i++) hidden[i] = bones[i] && (bones[i] == head || bones[i].IsChildOf(head));
                var weights = mesh.boneWeights;
                if (weights.Length != mesh.vertexCount) continue;
                var remove = new bool[weights.Length];
                bool any = false;
                for (int i = 0; i < weights.Length; i++)
                {
                    var w = weights[i];
                    float h = Weight(w.boneIndex0, w.weight0, hidden) + Weight(w.boneIndex1, w.weight1, hidden)
                        + Weight(w.boneIndex2, w.weight2, hidden) + Weight(w.boneIndex3, w.weight3, hidden);
                    remove[i] = h >= .35f; any |= remove[i];
                }
                if (!any) continue;
                var filtered = Object.Instantiate(mesh);
                filtered.name = mesh.name + " (first-person body)";
                var transparentTriangles = new List<int>();
                filtered.subMeshCount = mesh.subMeshCount + 1;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var triangles = mesh.GetTriangles(s); var keep = new List<int>(triangles.Length);
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        if (remove[triangles[i]] || remove[triangles[i+1]] || remove[triangles[i+2]])
                        { transparentTriangles.Add(triangles[i]); transparentTriangles.Add(triangles[i+1]); transparentTriangles.Add(triangles[i+2]); continue; }
                        keep.Add(triangles[i]); keep.Add(triangles[i+1]); keep.Add(triangles[i+2]);
                    }
                    filtered.SetTriangles(keep, s, false);
                }
                filtered.SetTriangles(transparentTriangles, mesh.subMeshCount, false);
                var go = new GameObject("First-person full-body shadow");
                go.hideFlags = HideFlags.DontSave; go.layer = renderer.gameObject.layer;
                go.transform.SetParent(renderer.transform, false);
                var shadow = go.AddComponent<SkinnedMeshRenderer>();
                shadow.sharedMesh = mesh; shadow.bones = bones; shadow.rootBone = renderer.rootBone;
                shadow.localBounds = renderer.localBounds; shadow.sharedMaterials = renderer.sharedMaterials;
                shadow.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                shadow.updateWhenOffscreen = true; shadow.enabled = false;
                skins.Add(new Skin {renderer=renderer, original=mesh, filtered=filtered, shadow=shadow, weights=new float[mesh.blendShapeCount]});
            }
        }
        static float Weight(int index, float weight, bool[] hidden) => index >= 0 && index < hidden.Length && hidden[index] ? weight : 0;
        public void BeforeRender()
        {
            if (applied) return;
            applied = true;
            foreach (var s in skins)
            {
                if (!s.renderer || !s.renderer.enabled || !s.renderer.gameObject.activeInHierarchy) continue;
                s.casting = s.renderer.shadowCastingMode;
                s.materials = s.renderer.sharedMaterials;
                s.shadow.sharedMaterials = s.materials;
                var viewMaterials = new Material[s.original.subMeshCount + 1];
                for (int m=0; m<s.original.subMeshCount; m++) viewMaterials[m]=s.materials[Mathf.Min(m,s.materials.Length-1)];
                viewMaterials[viewMaterials.Length-1]=transparent;
                for (int b = 0; b < s.weights.Length; b++) { s.weights[b] = s.renderer.GetBlendShapeWeight(b); s.shadow.SetBlendShapeWeight(b, s.weights[b]); }
                s.shadow.enabled = s.casting == ShadowCastingMode.On || s.casting == ShadowCastingMode.TwoSided || s.casting == ShadowCastingMode.ShadowsOnly;
                s.renderer.sharedMesh = s.filtered;
                s.renderer.sharedMaterials = viewMaterials;
                for (int b = 0; b < s.weights.Length; b++) s.renderer.SetBlendShapeWeight(b, s.weights[b]);
                s.renderer.shadowCastingMode = s.casting == ShadowCastingMode.ShadowsOnly ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.Off;
            }
        }
        public void AfterRender()
        {
            if (!applied) return;
            applied = false;
            foreach (var s in skins)
            {
                if (s.renderer && s.renderer.sharedMesh == s.filtered)
                { s.renderer.sharedMesh = s.original; s.renderer.sharedMaterials = s.materials; s.renderer.shadowCastingMode = s.casting;
                  for (int b = 0; b < s.weights.Length; b++) s.renderer.SetBlendShapeWeight(b, s.weights[b]); }
                if (s.shadow) s.shadow.enabled = false;
            }
        }
        public void Dispose()
        {
            AfterRender();
            foreach (var s in skins)
            { if (s.filtered) Object.Destroy(s.filtered); if (s.shadow) Object.Destroy(s.shadow.gameObject); }
            skins.Clear(); animator = null;
            if (transparent) Object.Destroy(transparent); transparent = null;
        }
    }
}
