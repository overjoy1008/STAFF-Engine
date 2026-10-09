using UnityEngine;

namespace Staff.Characters
{
    // Runs after camera damping/collision so a fast backwards dash cannot overtake the lens.
    // The user's zoom remains untouched. Start retreating early and ease both directions.
    public sealed class CharacterCameraClearance
    {
        static readonly HumanBodyBones[] BodyBones = {
            HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest,
            HumanBodyBones.UpperChest, HumanBodyBones.Head,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
            HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
            HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm,
            HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm,
            HumanBodyBones.LeftHand, HumanBodyBones.RightHand
        };
        readonly Transform[] bones = new Transform[BodyBones.Length];
        readonly RaycastHit[] hits = new RaycastHit[32];
        readonly Collider[] overlaps = new Collider[32];
        Animator cachedAnimator;
        float retreat;
        public void Reset() { retreat = 0; cachedAnimator = null; }

        public void Apply(Camera camera, Transform target, Animator animator, LayerMask mask, float dt)
        {
            if (!camera || !target) return;
            if (animator != cachedAnimator)
            {
                cachedAnimator = animator;
                for (int i = 0; i < bones.Length; i++)
                    bones[i] = animator && animator.isHuman ? animator.GetBoneTransform(BodyBones[i]) : null;
            }
            Vector3 position = camera.transform.position;
            Vector3 forward = camera.transform.forward;
            float required = float.NegativeInfinity;
            bool measured = false;
            // Bone envelopes exclude long hair, weapon and effect bounds. They still follow
            // animated legs/arms and scale with the avatar, including during a dash or dance.
            float padding = .3f * Mathf.Max(.1f, Mathf.Abs(target.lossyScale.y));
            if (animator && animator.isHuman)
            {
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                if (head && hips) padding = Mathf.Max(padding, Vector3.Distance(head.position, hips.position) * .4f);
                foreach (var bone in bones)
                {
                    if (!bone) continue;
                    measured = true;
                    required = Mathf.Max(required, camera.nearClipPlane + padding
                        - Vector3.Dot(bone.position - position, forward));
                }
            }
            if (!measured)
            {
                var controller = target.GetComponent<CharacterController>();
                Bounds bounds = controller ? controller.bounds : new Bounds(target.position + Vector3.up, new Vector3(1, 2, 1));
                Vector3 e = bounds.extents;
                float depth = Mathf.Abs(forward.x) * e.x + Mathf.Abs(forward.y) * e.y + Mathf.Abs(forward.z) * e.z;
                required = camera.nearClipPlane + .1f + depth - Vector3.Dot(bounds.center - position, forward);
            }
            // Begin .6 world units before the body reaches its safety envelope.
            // Fast exponential retreat reaches 90% in ~.13s; return remains slower.
            float desired = Mathf.Max(0, required + .6f);
            float rate = desired > retreat ? 18f : 6f;
            retreat = dt <= 0 ? desired
                : Mathf.Lerp(retreat, desired, 1 - Mathf.Exp(-rate * Mathf.Min(dt, .1f)));
            // Only a real near-plane intrusion (e.g. a teleport) bypasses easing.
            retreat = Mathf.Max(retreat, Mathf.Max(0, required));
            if (retreat <= .0001f) return;
            float halfHeight = camera.nearClipPlane * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
            float radius = Mathf.Max(.22f, halfHeight * Mathf.Sqrt(1 + camera.aspect * camera.aspect));
            // Sphere casts do not report colliders overlapping their origin.
            int overlapCount = Physics.OverlapSphereNonAlloc(position, radius, overlaps, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlapCount; i++)
                if (!overlaps[i].transform.IsChildOf(target)) return;
            if (overlapCount == overlaps.Length)
                foreach (var collider in Physics.OverlapSphere(position, radius, mask, QueryTriggerInteraction.Ignore))
                    if (!collider.transform.IsChildOf(target)) return;
            int count = Physics.SphereCastNonAlloc(position, radius, -forward, hits, retreat, mask, QueryTriggerInteraction.Ignore);
            float allowed = retreat;
            for (int i = 0; i < count; i++) Limit(hits[i], target, ref allowed);
            if (count == hits.Length)
                foreach (var hit in Physics.SphereCastAll(position, radius, -forward, retreat, mask, QueryTriggerInteraction.Ignore))
                    Limit(hit, target, ref allowed);
            // Preserve world collision if a wall leaves insufficient room for the character.
            camera.transform.position = position - forward * allowed;
        }

        static void Limit(RaycastHit hit, Transform target, ref float allowed)
        {
            if (hit.transform && !hit.transform.IsChildOf(target))
                allowed = Mathf.Min(allowed, Mathf.Max(0, hit.distance - .05f));
        }
    }
}
