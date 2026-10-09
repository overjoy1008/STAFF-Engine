using UnityEngine;
using UnityEngine.InputSystem;

namespace Staff.Characters
{
    [RequireComponent(typeof(Camera)), DisallowMultipleComponent, DefaultExecutionOrder(100)]
    public sealed class AdventureCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Transform facing;
        [SerializeField] Vector3 targetOffset = new Vector3(0, 1.25f, 0);
        [SerializeField] float yaw;
        [SerializeField] LayerMask collisionMask = ~0;
        Animator focusAnimator;
        float focusHeight;
        [SerializeField] float chestCenterOffset = .05f;
        public Vector3 FocusPoint => target ? target.position + FocusOffset() : Vector3.zero;
        Vector3 FocusOffset()
        {
            var player = target ? target.GetComponent<AdventurePlayer>() : null;
            var animator = player ? player.Animator : null;
            if (!animator || !animator.isHuman) return targetOffset;
            if (focusAnimator != animator)
            {
                var chest = animator.GetBoneTransform(HumanBodyBones.Chest)
                    ?? animator.GetBoneTransform(HumanBodyBones.UpperChest)
                    ?? animator.GetBoneTransform(HumanBodyBones.Spine);
                if (!chest) return targetOffset;
                Vector3 restChest = chest.position;
                foreach (var skin in animator.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (!skin.sharedMesh) continue;
                    int index = System.Array.IndexOf(skin.bones, chest);
                    var poses = skin.sharedMesh.bindposes;
                    if (index < 0 || index >= poses.Length) continue;
                    restChest = skin.transform.TransformPoint(poses[index].inverse.MultiplyPoint3x4(Vector3.zero));
                    break;
                }
                // Measure the character's chest in its upright rest pose. Do not feed
                // animated torso bobbing, dance poses or dash lean into the camera.
                focusHeight = player.Visual ? player.Visual.InverseTransformPoint(restChest).y * player.Visual.lossyScale.y
                    + player.Visual.position.y - target.position.y : restChest.y - target.position.y;
                focusHeight += chestCenterOffset;
                focusAnimator = animator;
            }
            return new Vector3(targetOffset.x, focusHeight, targetOffset.z);
        }
        float currentDistance;
        readonly CharacterCameraClearance clearance = new CharacterCameraClearance();
        bool initialized;
        WafflusCameraRig wafflus;
        readonly CameraCursorHold cursorHold = new CameraCursorHold();
        public CameraCursorHold CursorHold => cursorHold;
        Camera output;
        public bool IsPointerOverControls
        {
            get
            {
                if (Cursor.lockState == CursorLockMode.Locked || Mouse.current == null) return false;
                var point = Mouse.current.position.ReadValue();
                return new Rect(12, 12, 340, 187).Contains(new Vector2(point.x, Screen.height - point.y));
            }
        }
        public WafflusCameraRig WafflusRig => wafflus;
        void EnsureRig()
        {
            if (wafflus) return;
            output = GetComponent<Camera>();
            var rig = new GameObject("Cinemachine Camera Rig");
            rig.transform.SetParent(transform.parent, false);
            wafflus = rig.AddComponent<WafflusCameraRig>();
            wafflus.Initialize(output, transform.parent, collisionMask);
            wafflus.SetView(yaw, 0, 6);
            wafflus.SetActive(isActiveAndEnabled);
        }
        public float Yaw => wafflus ? wafflus.Pov.m_HorizontalAxis.Value : yaw;
        public float Pitch => wafflus ? wafflus.Pov.m_VerticalAxis.Value : 0;
        public float Distance => currentDistance;
        public Transform Target => target;
        public void Configure(Transform player, Transform model) { target = player; facing = model; Snap(); }
        void OnEnable()
        {
            initialized = false;
            EnsureRig();
            wafflus.SetActive(true);
        }
        void OnDisable() { if (wafflus) wafflus.SetActive(false); cursorHold.Reset(); }
        void OnDestroy() { if (wafflus) Destroy(wafflus.gameObject); }
        void Start() => Snap();
        void OnGUI()
        {
            if (!target) return;
            var roster = target.GetComponent<CharacterSwitcher>();
            if (roster && roster.PickerOpen) return;
            var dance = target.GetComponent<AdventureDance>();
            var monochrome = Object.FindFirstObjectByType<Staff.MathSpace.MonochromeMode>();
            GUI.Box(new Rect(12, 12, 340, 187), GUIContent.none);
            GUI.Label(new Rect(22, 18, 320, 22), "Camera: Cinemachine");
            GUI.Label(new Rect(22, 40, 320, 22), "C   Character: " + (roster ? roster.CurrentName : "—"));
            var environmentMode = GetComponent<Staff.Subway.AbstractEnvironment>();
            GUI.Label(new Rect(22, 63, 320, 22), environmentMode ? "I    Environment: " + environmentMode.Mode : "I    Background: " + (monochrome && monochrome.Inverted ? "White" : "Black"));
            if(Object.FindFirstObjectByType<Staff.Subway.SubwayDoors>()) GUI.Label(new Rect(22, 171, 380, 22), "O    Doors / R    Reset / [ Arrive / ] Depart");
            GUI.Label(new Rect(22, 86, 320, 22), "H   Shader: " + (roster ? roster.ShaderLabel : "—"));
            GUI.Label(new Rect(22, 109, 320, 22), "1–8  Dance: " + (dance && dance.IsDancing ? dance.CurrentTitle : "Idle"));
            GUI.Label(new Rect(22, 132, 320, 22), "Option / Alt  Hold to use cursor");
            GUI.Label(new Rect(22, 155, 320, 22), "Built-in  ·  Move / jump cancels dance");
        }
        public void UpdateCursor(bool canCapture)
        {
            cursorHold.Update(cursorHold.OptionHeld, canCapture);
            if (wafflus) wafflus.SetLookPaused(cursorHold.LookPaused);
        }
        public void NotifyMovement(AdventurePlayer.Command command, bool grounded, float speed)
        {
            if (wafflus) wafflus.UpdateRecentering(command, grounded, speed);
        }
        public void AddLook(Vector2 mouse, Vector2 stick, float scroll, bool recenter, float dt)
        {
            bool pause = cursorHold.LookPaused;
            if (pause) { mouse = Vector2.zero; stick = Vector2.zero; recenter = false; }
            EnsureRig();
            wafflus.SetLookPaused(pause);
            wafflus.AddLook(mouse, stick, scroll, recenter, dt, facing);
        }
        public void Snap()
        {
            if (!target) return;
            clearance.Reset();
            EnsureRig();
            wafflus.VirtualCamera.PreviousStateIsValid = false;
            initialized = true;
            Simulate(0);
        }
        void ApplyCharacterClearance(float dt)
        {
            if (!output) output = GetComponent<Camera>();
            var player = target.GetComponent<AdventurePlayer>();
            clearance.Apply(output, target, player ? player.Animator : null, collisionMask, dt);
        }
        void LateUpdate() => Simulate(Time.deltaTime);
        public void Simulate(float dt)
        {
            if (!target) return;
            if (!initialized) { Snap(); return; }
            wafflus.Simulate(target, facing, FocusOffset(), dt);
            ApplyCharacterClearance(dt);
            currentDistance = Vector3.Distance(FocusPoint, transform.position);
        }
    }
}
