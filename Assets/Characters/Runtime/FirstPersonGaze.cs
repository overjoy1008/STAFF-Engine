using UnityEngine;
namespace Staff.Characters
{
    // Runs after Animator/secondary animation. Restored in Update before the next
    // animation evaluation so additive gaze never accumulates on unkeyed bones.
    public sealed class FirstPersonGaze
    {
        Animator animator;
        Transform head, neck, leftEye, rightEye;
        Quaternion headPose, neckPose, faceInHead;
        bool applied;
        float pitch, yaw;
        public bool HasEyes => leftEye && rightEye;
        public Transform LeftEye => leftEye;
        public Transform RightEye => rightEye;
        public Transform Head => head;
        public Transform Neck => neck;
        public Vector3 EyeMidpoint => HasEyes ? (leftEye.position + rightEye.position) * .5f : head ? head.position : Vector3.zero;
        Vector3 animationEyeLocal;
        public Vector3 AnimationEyeMidpoint => animator ? animator.transform.TransformPoint(animationEyeLocal) : EyeMidpoint;
        public Quaternion FaceRotation => head ? head.rotation * faceInHead : Quaternion.identity;
        public float AppliedPitch => pitch;
        public float AppliedYaw => yaw;
        public void Bind(Animator next)
        {
            if(animator==next) return;
            Restore(); animator=next; head=neck=leftEye=rightEye=null; pitch=yaw=0;
            if(!animator || !animator.isHuman) return;
            head=animator.GetBoneTransform(HumanBodyBones.Head);
            neck=animator.GetBoneTransform(HumanBodyBones.Neck);
            leftEye=animator.GetBoneTransform(HumanBodyBones.LeftEye);
            rightEye=animator.GetBoneTransform(HumanBodyBones.RightEye);
            if(!head) return;
            var rest=RestMatrix(head);
            Vector3 forward=animator.transform.forward;
            if(HasEyes)
            {
                Vector3 right=RestMatrix(rightEye).GetColumn(3)-RestMatrix(leftEye).GetColumn(3);
                var normal=Vector3.Cross(right,animator.transform.up).normalized;
                if(normal.sqrMagnitude>.5f) forward=Vector3.Dot(normal,forward)<0 ? -normal : normal;
            }
            faceInHead=Quaternion.Inverse(rest.rotation)*Quaternion.LookRotation(forward,animator.transform.up);
        }
        Matrix4x4 RestMatrix(Transform bone)
        {
            foreach(var skin in animator.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if(!skin.sharedMesh)continue;
                int i=System.Array.IndexOf(skin.bones,bone);
                var poses=skin.sharedMesh.bindposes;
                if(i>=0&&i<poses.Length)return skin.transform.localToWorldMatrix*poses[i].inverse;
            }
            return bone.localToWorldMatrix;
        }
        public void Apply(float targetPitch,float targetYaw,float dt,float stabilization = 0,float weight = 1)
        {
            Restore(); if(!head)return;
            animationEyeLocal = animator.transform.InverseTransformPoint(EyeMidpoint);
            float t=dt<=0 ? 0 : 1-Mathf.Exp(-14*dt);
            pitch=Mathf.Lerp(pitch,Mathf.Clamp(targetPitch,-80,88),t);
            yaw=Mathf.LerpAngle(yaw,Mathf.Clamp(targetYaw,-75,75),t);
            headPose=head.localRotation;if(neck)neckPose=neck.localRotation;
            var face=FaceRotation;
            // Cancel locomotion head roll/pitch before adding intentional gaze.
            var upright=Quaternion.Euler(0,animator.transform.eulerAngles.y,0);
            var basis=Quaternion.Slerp(face,upright,stabilization);
            var desired=Quaternion.Slerp(face,Quaternion.AngleAxis(yaw,Vector3.up)*basis*Quaternion.Euler(pitch,0,0),weight);
            // Share the turn across the neck and skull, instead of rotating only
            // the camera or bending a single joint through the whole angle.
            if(neck)
            {
                var partial = stabilization <= 0 && weight >= 1
                    ? Quaternion.AngleAxis(yaw*.4f,Vector3.up)*face*Quaternion.Euler(pitch*.4f,0,0)
                    : Quaternion.Slerp(face,desired,.4f);
                neck.rotation=(partial*Quaternion.Inverse(face))*neck.rotation;
            }
            head.rotation=desired*Quaternion.Inverse(faceInHead);
            applied=true;
        }
        public void Restore()
        {
            if(!applied)return;
            if(neck)neck.localRotation=neckPose;
            if(head)head.localRotation=headPose;
            applied=false;
        }
        public void Clear(){Restore();animator=null;head=neck=leftEye=rightEye=null;}
    }
}
