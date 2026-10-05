using System;
using UnityEditor;
using UnityEngine;

namespace Staff.Characters.Editor
{
    // Iuno is exported with its mesh and morphs baked into a T-pose. Unity's
    // imported FBX skin matrices retain different arm rotations. Reconcile the
    // skin bind matrices with the imported rest transforms before animation.
    public sealed class IunoSkinBindPosePostprocessor : AssetPostprocessor
    {
        public override uint GetVersion() => 1;

        void OnPostprocessModel(GameObject model)
        {
            if (!string.Equals(assetPath, "Assets/ThirdParty/StaffCharacters/Iuno/Iuno.fbx", StringComparison.Ordinal)) return;
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null || mesh.bindposes.Length != renderer.bones.Length) continue;
                var bindposes = new Matrix4x4[renderer.bones.Length];
                for (int i = 0; i < bindposes.Length; i++)
                {
                    if (renderer.bones[i] == null) throw new InvalidOperationException("Iuno skin bone is missing.");
                    bindposes[i] = renderer.bones[i].worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                }
                mesh.bindposes = bindposes;
            }
        }
    }
}
