using UnityEngine;
using UnityEngine.InputSystem;

namespace Staff.Characters
{
    [RequireComponent(typeof(Camera)), DisallowMultipleComponent, DefaultExecutionOrder(30000)]
    public sealed class AdventureCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Transform facing;
        [SerializeField] Vector3 targetOffset = new Vector3(0, 1.25f, 0);
        [SerializeField] float yaw;
        [SerializeField] LayerMask collisionMask = ~0;
        [Header("V: third person / Classic / original Eyes / light Eyes / stabilized Eyes")]
        [SerializeField] Key perspectiveKey = Key.V;
        [SerializeField, Range(45, 100)] float firstPersonFov = 80;
        [SerializeField, Range(.005f, .1f)] float firstPersonNearClip = .015f;
        [SerializeField, Range(-89, 0)] float firstPersonMinPitch = -80;
        [SerializeField, Range(0, 89)] float firstPersonMaxPitch = 88;
        public enum FirstPersonMode { Classic, Eyes, StabilizedEyes, LightEyes }
        [SerializeField] FirstPersonMode firstPersonMode = FirstPersonMode.Eyes;
        [SerializeField, Range(0, 1)] float stabilizedEyeStrength = .95f;
        [SerializeField, Range(0, .5f)] float stabilizedEyeSmoothingTime = .18f;
        public FirstPersonMode Mode => firstPersonMode;
        public float ActiveEyeStabilization => Mode == FirstPersonMode.Eyes ? 0 : Mode == FirstPersonMode.LightEyes ? stabilizedEyeStrength * .75f : stabilizedEyeStrength;
        public float EyeStabilization { get => stabilizedEyeStrength; set { stabilizedEyeStrength = Mathf.Clamp01(value); eyeReady = false; } }
        Vector3 restEye, smoothEye;
        bool eyeReady, canLookAtCursor;
        float cursorGazeWeight;
        public bool CursorGazeActive => !IsFirstPerson && canLookAtCursor && cursorHold.OptionHeld;
        public bool IsFirstPerson { get; private set; }
        public int FirstPersonFilteredRenderers => firstPersonBody.FilteredRenderers;
        readonly FirstPersonBody firstPersonBody = new FirstPersonBody();
        Animator firstPersonAnimator;
        readonly FirstPersonGaze gaze = new FirstPersonGaze();
        public FirstPersonGaze Gaze => gaze;
        int gazeFrame = -1;
        public void PrepareFirstPersonGaze(float dt)
        {
            if (!target) return;
            var player = target.GetComponent<AdventurePlayer>();
            gaze.Bind(player ? player.Animator : null);
            if (gazeFrame == Time.frameCount) return;
            gazeFrame = Time.frameCount;
            if (IsFirstPerson)
            {
                cursorGazeWeight = 0;
                if (Mode != FirstPersonMode.Classic)
                    gaze.Apply(firstPitch, Mathf.DeltaAngle(BodyYaw, firstYaw), dt, ActiveEyeStabilization);
                return;
            }
            bool active = CursorGazeActive && player && !player.InputBlocked && Mouse.current != null && !IsPointerOverControls;
            cursorGazeWeight = Mathf.MoveTowards(cursorGazeWeight, active ? 1 : 0, Mathf.Max(0, dt) * 7);
            if (cursorGazeWeight > 0 && gaze.Head)
            {
                if (active) UpdateCursorGazeAngles(Mouse.current.position.ReadValue());
                gaze.Apply(cursorPitch, cursorYaw, dt, 1, cursorGazeWeight);
            }
        }
        float BodyYaw => facing ? facing.eulerAngles.y : target ? target.eulerAngles.y : 0;
        float cursorPitch, cursorYaw;
        void UpdateCursorGazeAngles(Vector2 screenPoint)
        {
            // A plane in front of the character makes the on-screen arrow a
            // reachable look target, regardless of which side the camera views.
            var forward = Quaternion.Euler(0, BodyYaw, 0) * Vector3.forward;
            var plane = new Plane(output.transform.forward, gaze.EyeMidpoint + forward * 1.5f);
            var ray = output.ScreenPointToRay(screenPoint);
            if (!plane.Raycast(ray, out float distance)) return;
            var local = Quaternion.Inverse(Quaternion.Euler(0, BodyYaw, 0)) * (ray.GetPoint(distance) - gaze.EyeMidpoint);
            cursorYaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -70, 70);
            cursorPitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -50, 55);
        }
        public void SetFirstPersonMode(FirstPersonMode mode)
        {
            if (firstPersonMode == mode) return;
            gaze.Clear(); gazeFrame = -1;
            firstPersonMode = mode; firstPersonAnimator = null; eyeReady = false;
            if (IsFirstPerson) Simulate(0);
        }
        float firstYaw, firstPitch, savedNear, savedFov, savedZoom;
        public bool HandlePerspectiveInput(bool accept)
        {
            var player = target ? target.GetComponent<AdventurePlayer>() : null;
            if (!accept || !player || player.InputBlocked || cursorHold.LookPaused || Keyboard.current == null) return false;
            if (!Keyboard.current[perspectiveKey].wasPressedThisFrame) return false;
            if (!IsFirstPerson)
            {
                SetFirstPersonMode(FirstPersonMode.Classic);
                SetFirstPerson(true);
            }
            else if (Mode == FirstPersonMode.Classic) SetFirstPersonMode(FirstPersonMode.Eyes);
            else if (Mode == FirstPersonMode.Eyes) SetFirstPersonMode(FirstPersonMode.LightEyes);
            else if (Mode == FirstPersonMode.LightEyes) SetFirstPersonMode(FirstPersonMode.StabilizedEyes);
            else SetFirstPerson(false);
            return true;
        }
        public void SetFirstPerson(bool value)
        {
            if (IsFirstPerson == value || !target) return;
            EnsureRig();
            if (value)
            {
                firstYaw = facing ? facing.eulerAngles.y : Yaw;
                firstPitch = Mathf.Clamp(Pitch, firstPersonMinPitch, firstPersonMaxPitch);
                savedNear = output.nearClipPlane; savedFov = output.fieldOfView; savedZoom = wafflus.ZoomTarget;
                IsFirstPerson = true; gazeFrame=-1; eyeReady=false; cursorGazeWeight=0; gaze.Restore(); wafflus.SetActive(false);
            }
            else
            {
                firstPersonBody.Dispose(); gaze.Clear(); firstPersonAnimator = null;
                IsFirstPerson = false;
                output.nearClipPlane = savedNear; output.fieldOfView = savedFov;
                wafflus.SetView(firstYaw, Mathf.Clamp(firstPitch, -80, 80), savedZoom);
                wafflus.SetActive(isActiveAndEnabled); clearance.Reset();
            }
            Simulate(0);
        }
        void OnPreCull() { if (IsFirstPerson && isActiveAndEnabled) firstPersonBody.BeforeRender(); }
        void OnPostRender() => firstPersonBody.AfterRender();
        void UpdateFirstPerson(float dt)
        {
            var player = target.GetComponent<AdventurePlayer>();
            var animation = player ? player.Animator : null;
            if (animation != firstPersonAnimator)
            {
                firstPersonAnimator = animation;
                eyeReady = false;
                CacheRestEye(animation);
                gaze.Bind(animation);
                firstPersonBody.Bind(animation);
            }
            PrepareFirstPersonGaze(dt);
            if (Mode == FirstPersonMode.Classic) UpdateClassicView();
            else if (gaze.Head && (Mode == FirstPersonMode.Eyes || ActiveEyeStabilization <= 0))
            {
                // Exact original Eyes path; do not evaluate the filter at all.
                eyeReady = false;
                transform.SetPositionAndRotation(gaze.EyeMidpoint, gaze.FaceRotation);
            }
            else if (gaze.Head)
            {
                // Filter only root-relative animation bob; controller movement,
                // jumping and teleports remain immediate. No delayed world-space pursuit.
                var basis = Quaternion.Euler(0, BodyYaw, 0);
                var localEye = Quaternion.Inverse(basis) * (gaze.AnimationEyeMidpoint - target.position);
                if (!eyeReady || stabilizedEyeSmoothingTime <= 0) { smoothEye = localEye; eyeReady = true; }
                else smoothEye = Vector3.Lerp(smoothEye, localEye, 1 - Mathf.Exp(-Mathf.Max(0, dt) / stabilizedEyeSmoothingTime));
                var correction = Vector3.ClampMagnitude(basis * (smoothEye - localEye) * ActiveEyeStabilization, .06f);
                transform.SetPositionAndRotation(gaze.EyeMidpoint + correction, gaze.FaceRotation);
            }
            output.fieldOfView = firstPersonFov; output.nearClipPlane = firstPersonNearClip;
            currentDistance = 0;
        }
        void CacheRestEye(Animator animation)
        {
                restEye = new Vector3(0, 1.65f, 0);
                if (animation && animation.isHuman)
                {
                    var head = animation.GetBoneTransform(HumanBodyBones.Head);
                    if (head)
                    {
                        Vector3 rest = head.position;
                        foreach (var skin in animation.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            if (!skin.sharedMesh) continue;
                            int index = System.Array.IndexOf(skin.bones, head);
                            var poses = skin.sharedMesh.bindposes;
                            if (index < 0 || index >= poses.Length) continue;
                            rest = skin.transform.TransformPoint(poses[index].inverse.MultiplyPoint3x4(Vector3.zero)); break;
                        }
                        // Root-relative rest height: jumping follows the controller, while
                        // head roll, PMX physics and dash lean cannot shake the horizon.
                        restEye = Quaternion.Inverse(Quaternion.Euler(0, facing ? facing.eulerAngles.y : 0, 0)) * (rest - target.position);
                    }
                }
        }
        void UpdateClassicView()
        {
            var basis = Quaternion.Euler(0, firstYaw, 0);
            // Looking down leans the viewpoint forward, keeping chest at the lower
            // edge and revealing hands/legs instead of looking into the neck opening.
            float lean = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(35, 80, firstPitch));
            var eye = target.position + basis * (restEye + new Vector3(0, .08f, .055f) + Vector3.forward * (.295f * lean));
            // The offset stays close to the controller axis. A short cast protects
            // the eye near a wall without applying third-person body clearance.
            var origin = target.position + Vector3.up * (restEye.y + new Vector3(0, .08f, .055f).y);
            var delta = eye - origin;
            if (delta.magnitude > .001f)
            {
                foreach (var hit in Physics.SphereCastAll(origin, .04f, delta.normalized, delta.magnitude, collisionMask, QueryTriggerInteraction.Ignore))
                    if (!hit.transform.IsChildOf(target)) eye = origin + delta.normalized * Mathf.Min(Vector3.Distance(origin, eye), Mathf.Max(0, hit.distance - .015f));
            }
            transform.SetPositionAndRotation(eye, Quaternion.Euler(firstPitch, firstYaw, 0));
        }
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
            if (!GetComponent<FirstPersonGazeDriver>()) gameObject.AddComponent<FirstPersonGazeDriver>();
            var rig = new GameObject("Cinemachine Camera Rig");
            rig.transform.SetParent(transform.parent, false);
            wafflus = rig.AddComponent<WafflusCameraRig>();
            wafflus.Initialize(output, transform.parent, collisionMask);
            wafflus.SetView(yaw, 0, 6);
            wafflus.SetActive(isActiveAndEnabled);
        }
        public float Yaw => IsFirstPerson ? firstYaw : wafflus ? wafflus.Pov.m_HorizontalAxis.Value : yaw;
        public float Pitch => IsFirstPerson ? firstPitch : wafflus ? wafflus.Pov.m_VerticalAxis.Value : 0;
        public float Distance => currentDistance;
        public Transform Target => target;
        public void Configure(Transform player, Transform model) { target = player; facing = model; Snap(); }
        void OnEnable()
        {
            initialized = false;
            EnsureRig();
            wafflus.SetActive(!IsFirstPerson);
        }
        void OnDisable() { if (IsFirstPerson) SetFirstPerson(false); firstPersonBody.Dispose(); gaze.Clear(); cursorGazeWeight=0; canLookAtCursor=false; if (wafflus) wafflus.SetActive(false); cursorHold.Reset(); }
        void OnDestroy() { gaze.Clear(); firstPersonBody.Dispose(); if (wafflus) Destroy(wafflus.gameObject); }
        void Start() => Snap();
        void OnGUI()
        {
            if (!target) return;
            var roster = target.GetComponent<CharacterSwitcher>();
            if (roster && roster.PickerOpen) return;
            var dance = target.GetComponent<AdventureDance>();
            var monochrome = Object.FindFirstObjectByType<Staff.MathSpace.MonochromeMode>();
            GUI.Box(new Rect(12, 12, 340, 187), GUIContent.none);
            GUI.Label(new Rect(22, 18, 320, 22), IsFirstPerson ? perspectiveKey + "   First person: " + (Mode == FirstPersonMode.Classic ? "Classic" : Mode == FirstPersonMode.Eyes ? "Eyes (Original)" : Mode == FirstPersonMode.LightEyes ? "Eyes (Light)" : "Eyes (Stabilized)") : perspectiveKey + "   Camera: Third person (Cinemachine)");
            GUI.Label(new Rect(22, 40, 320, 22), "C   Character: " + (roster ? roster.CurrentName : "—"));
            var environmentMode = GetComponent<Staff.Subway.AbstractEnvironment>();
            GUI.Label(new Rect(22, 63, 320, 22), environmentMode ? "I    Environment: " + environmentMode.Mode : "I    Background: " + (monochrome && monochrome.Inverted ? "White" : "Black"));
            if(Object.FindFirstObjectByType<Staff.Subway.SubwayDoors>()) GUI.Label(new Rect(22, 171, 380, 22), "O    Doors / R    Reset / [ Arrive / ] Depart");
            GUI.Label(new Rect(22, 86, 320, 22), "H   Shader: " + (roster ? roster.ShaderLabel : "—"));
            GUI.Label(new Rect(22, 109, 320, 22), "1–8  Dance: " + (dance && dance.IsDancing ? dance.CurrentTitle : "Idle"));
            GUI.Label(new Rect(22, 132, 320, 22), IsFirstPerson ? "Option / Alt  Hold to use cursor" : "Option / Alt  Face follows cursor");
            GUI.Label(new Rect(22, 155, 320, 22), perspectiveKey + "   Third / Classic / Original / Light / Stabilized");
        }
        public void UpdateCursor(bool canCapture)
        {
            canLookAtCursor = canCapture;
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
            if (IsFirstPerson)
            {
                if (!pause)
                {
                    firstYaw = Mathf.Repeat(firstYaw + mouse.x * .12f + stick.x * 140 * dt, 360);
                    firstPitch = Mathf.Clamp(firstPitch - mouse.y * .12f - stick.y * 140 * dt, firstPersonMinPitch, firstPersonMaxPitch);
                    if (recenter) firstPitch = 0;
                }
                return;
            }
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
            if (IsFirstPerson) { UpdateFirstPerson(dt); return; }
            wafflus.Simulate(target, facing, FocusOffset(), dt);
            ApplyCharacterClearance(dt);
            currentDistance = Vector3.Distance(FocusPoint, transform.position);
        }
    }
}
