using UnityEngine;
using UnityEngine.InputSystem;

namespace Staff.Characters
{
    [DisallowMultipleComponent, DefaultExecutionOrder(100)]
    public sealed class AdventureCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Transform facing;
        [SerializeField] Vector3 targetOffset = new Vector3(0, 1.25f, 0);
        [SerializeField] float distance = 4.8f;
        [SerializeField] float minDistance = 2;
        [SerializeField] float maxDistance = 9;
        [SerializeField] float pitch = 16;
        [SerializeField] float yaw;
        [SerializeField] Vector2 pitchLimits = new Vector2(-25, 70);
        [SerializeField] float mouseSensitivity = .12f;
        [SerializeField] float stickSensitivity = 140;
        [SerializeField] float followSmoothTime = .075f;
        [SerializeField] float collisionRadius = .22f;
        [SerializeField] LayerMask collisionMask = ~0;
        readonly RaycastHit[] hits = new RaycastHit[32];
        Vector3 pivot, velocity;
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
        bool initialized;
        public enum CameraVersion { Current, Wafflus }
        [SerializeField] CameraVersion version = CameraVersion.Wafflus;
        WafflusCameraRig wafflus;
        readonly CameraCursorHold cursorHold = new CameraCursorHold();
        public CameraCursorHold CursorHold => cursorHold;
        Camera output;
        InputAction toggleVersion;
        float currentFov, savedYaw, savedPitch, savedDistance;
        float wafflusYaw, wafflusPitch, wafflusDistance = 6;
        bool wafflusVisited;
        public bool IsPointerOverControls
        {
            get
            {
                if (Cursor.lockState == CursorLockMode.Locked || Mouse.current == null) return false;
                var point = Mouse.current.position.ReadValue();
                return new Rect(12, 12, 252, 52).Contains(new Vector2(point.x, Screen.height - point.y));
            }
        }
        public CameraVersion Version => version;
        public WafflusCameraRig WafflusRig => wafflus;
        public void ToggleVersion() => SetVersion(version == CameraVersion.Wafflus ? CameraVersion.Current : CameraVersion.Wafflus);
        public void SetVersion(CameraVersion next)
        {
            if (next == version) return;
            if (!output) output = GetComponent<Camera>();
            if (version == CameraVersion.Current)
            { savedYaw = yaw; savedPitch = pitch; savedDistance = distance; currentFov = output.fieldOfView; }
            else if (version == CameraVersion.Wafflus && wafflus)
            {
                wafflusYaw = wafflus.Pov.m_HorizontalAxis.Value;
                wafflusPitch = wafflus.Pov.m_VerticalAxis.Value;
                wafflusDistance = wafflus.ZoomTarget;
                wafflus.SetActive(false);
            }
            if (next == CameraVersion.Wafflus)
            {
                if (!wafflus)
                {
                    var rig = new GameObject("Wafflus Camera Rig");
                    rig.transform.SetParent(transform.parent, false);
                    wafflus = rig.AddComponent<WafflusCameraRig>();
                    wafflus.Initialize(output, transform.parent, collisionMask);
                }
                wafflus.SetActive(true);
                wafflus.SetView(wafflusVisited ? wafflusYaw : yaw, wafflusVisited ? wafflusPitch : 0, wafflusDistance);
                wafflusVisited = true;
            }
            else
            {
                output.fieldOfView = currentFov;
                yaw = savedYaw; pitch = savedPitch; distance = savedDistance;
            }
            version = next; Snap();
        }
        public float Yaw => version == CameraVersion.Wafflus && wafflus ? wafflus.Pov.m_HorizontalAxis.Value : yaw;
        public float Pitch => version == CameraVersion.Wafflus && wafflus ? wafflus.Pov.m_VerticalAxis.Value : pitch;
        public float Distance => currentDistance;
        public Transform Target => target;
        public void Configure(Transform player, Transform model) { target = player; facing = model; Snap(); }
        void OnEnable()
        {
            initialized = false;
            if (wafflus && version == CameraVersion.Wafflus) wafflus.SetActive(true);
            toggleVersion = new InputAction("ToggleCameraVersion", InputActionType.Button, "<Keyboard>/f6");
            toggleVersion.Enable();
        }
        void OnDisable() { toggleVersion?.Dispose(); toggleVersion = null; if (wafflus) wafflus.SetActive(false); cursorHold.Reset(); }
        void OnDestroy() { if (wafflus) Destroy(wafflus.gameObject); }
        void Start()
        {
            // Existing serialized scenes also start with Cinemachine.
            version = CameraVersion.Current;
            SetVersion(CameraVersion.Wafflus);
        }
        void Update()
        {
            if (toggleVersion != null && toggleVersion.WasPressedThisFrame() && Application.isFocused && target)
            {
                var player = target.GetComponent<AdventurePlayer>();
                if (!player || !player.InputBlocked) ToggleVersion();
            }
        }
        void OnGUI()
        {
            if (!target) return;
            var player = target.GetComponent<AdventurePlayer>();
            var roster = target.GetComponent<CharacterSwitcher>();
            if (roster && roster.PickerOpen) return;
            var dance = target.GetComponent<AdventureDance>();
            var monochrome = Object.FindFirstObjectByType<Staff.MathSpace.MonochromeMode>();
            GUI.Box(new Rect(12, 12, 340, 210), GUIContent.none);
            GUI.Label(new Rect(22, 18, 320, 22), version == CameraVersion.Current ? "Camera: Current" : "Camera: Cinemachine / Wafflus");
            if (player && player.InputCaptured && Cursor.lockState == CursorLockMode.Locked)
                GUI.Label(new Rect(22, 40, 320, 22), "F6  Camera mode");
            else if (GUI.Button(new Rect(22, 40, 320, 22), "F6  Switch camera")) ToggleVersion();
            GUI.Label(new Rect(22, 63, 320, 22), "C   Character: " + (roster ? roster.CurrentName : "—"));
            GUI.Label(new Rect(22, 86, 320, 22), "I    Background: " + (monochrome && monochrome.Inverted ? "White" : "Black"));
            GUI.Label(new Rect(22, 109, 320, 22), "H   Shader: " + (roster ? roster.ShaderLabel : "—"));
            GUI.Label(new Rect(22, 132, 320, 22), "1–8  Dance: " + (dance && dance.IsDancing ? dance.CurrentTitle : "Idle"));
            GUI.Label(new Rect(22, 155, 320, 22), "Option / Alt  Hold to use cursor");
            GUI.Label(new Rect(22, 178, 320, 22), "Built-in  ·  Move / jump cancels dance");
        }
        public void UpdateCursor(bool canCapture)
        {
            cursorHold.Update(cursorHold.OptionHeld, canCapture);
            if (wafflus) wafflus.SetLookPaused(cursorHold.LookPaused);
        }
        public void NotifyMovement(AdventurePlayer.Command command, bool grounded, float speed)
        {
            if (version == CameraVersion.Wafflus && wafflus) wafflus.UpdateRecentering(command, grounded, speed);
        }
        public void AddLook(Vector2 mouse, Vector2 stick, float scroll, bool recenter, float dt)
        {
            bool pause = cursorHold.LookPaused;
            if (pause) { mouse = Vector2.zero; stick = Vector2.zero; recenter = false; }
            if (wafflus) wafflus.SetLookPaused(pause);
            if (version == CameraVersion.Wafflus && wafflus)
            { wafflus.AddLook(mouse, stick, scroll, recenter, dt, facing); return; }
            yaw += mouse.x * mouseSensitivity + stick.x * stickSensitivity * dt;
            pitch -= mouse.y * mouseSensitivity + stick.y * stickSensitivity * dt;
            pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
            yaw = Mathf.Repeat(yaw, 360);
            distance = Mathf.Clamp(distance - scroll * .005f, minDistance, maxDistance);
            if (recenter && facing) yaw = facing.eulerAngles.y;
        }
        public void Snap()
        {
            if (!target) return;
            pivot = FocusPoint;
            velocity = Vector3.zero;
            currentDistance = distance;
            initialized = true;
            Simulate(0);
        }
        void LateUpdate() => Simulate(Time.deltaTime);
        public void Simulate(float dt)
        {
            if (!target) return;
            if (!initialized) { Snap(); return; }
            if (version == CameraVersion.Wafflus && wafflus)
            {
                wafflus.Simulate(target, facing, FocusOffset(), dt);
                currentDistance = Vector3.Distance(FocusPoint, transform.position);
                return;
            }
            if (dt > 0) pivot = Vector3.SmoothDamp(pivot, FocusPoint,
                ref velocity, followSmoothTime, Mathf.Infinity, dt);
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            var backwards = rotation * Vector3.back;
            float allowed = distance;
            int count = Physics.SphereCastNonAlloc(pivot, collisionRadius, backwards, hits,
                distance, collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!hits[i].transform.IsChildOf(target)) allowed = Mathf.Min(allowed, Mathf.Max(.1f, hits[i].distance - .06f));
            // Pull in immediately at an obstacle; ease back out once it clears.
            currentDistance = allowed < currentDistance || dt <= 0 ? allowed : Mathf.Lerp(currentDistance, allowed, 1 - Mathf.Exp(-8 * dt));
            transform.SetPositionAndRotation(pivot + backwards * currentDistance, rotation);
        }
    }
}
