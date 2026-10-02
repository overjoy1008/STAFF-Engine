using UnityEngine;
using UnityEngine.InputSystem;

namespace Staff.Characters
{
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class AdventurePlayer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] Animator animator;
        [SerializeField] Transform visual;
        [SerializeField] AdventureCamera followCamera;
        [Header("Camera-relative locomotion")]
        [SerializeField] float walkSpeed = 1.8f;
        [SerializeField] float runSpeed = 8;
        [SerializeField] float acceleration = 48;
        [SerializeField] float turnSharpness = 20;
        [Header("Press Shift / Right Mouse: dash, then run")]
        [SerializeField, Min(0)] float dashSpeed = 18;
        [SerializeField, Min(.01f)] float dashDuration = .24f;
        [SerializeField, Min(0)] float dashCooldown = .6f;
        [SerializeField, Range(0, 20)] float dashLean = 12;
        [Header("Jump and ground")]
        [SerializeField] float jumpHeight = 1.25f;
        [SerializeField] float gravity = 24;
        [SerializeField] float coyoteTime = .12f;
        [SerializeField] float jumpBuffer = .12f;
        [SerializeField] LayerMask groundMask = ~0;
        CharacterController controller;
        InputActionAsset actions;
        InputAction move, lookMouse, lookStick, jump, sprint, walk, zoom, recenter, cancel, capture;
        readonly RaycastHit[] groundHits = new RaycastHit[16];
        Vector3 horizontalVelocity, spawn;
        float verticalVelocity, sinceGround = 10, sinceJumpPress = 10;
        float dashRemaining, dashRecovery;
        Vector3 dashDirection;
        bool sprintWasHeld;
        bool inputCaptured;
        public bool Grounded { get; private set; }
        public float VerticalVelocity => verticalVelocity;
        public float HorizontalSpeed => horizontalVelocity.magnitude;
        public bool IsDashing => dashRemaining > 0;
        public bool InputCaptured => inputCaptured;
        public Animator Animator => animator;
        public Transform Visual => visual;

        public struct Command
        {
            public Vector2 move;
            public bool jump, sprint, walk;
        }

        public void Configure(InputActionAsset input, Animator animation, Transform model, AdventureCamera camera)
        { inputActions = input; animator = animation; visual = model; followCamera = camera; }

        void Awake()
        {
            controller = GetComponent<CharacterController>(); spawn = transform.position;
            if (!followCamera && Camera.main) followCamera = Camera.main.GetComponent<AdventureCamera>();
            if (followCamera && !followCamera.Target) followCamera.Configure(transform, visual);
        }
        void OnEnable()
        {
            if (!inputActions) return;
            actions = Instantiate(inputActions);
            move = actions.FindAction("Move", true);
            lookMouse = actions.FindAction("LookMouse", true);
            lookStick = actions.FindAction("LookStick", true);
            jump = actions.FindAction("Jump", true);
            sprint = actions.FindAction("Sprint", true);
            walk = actions.FindAction("Walk", true);
            zoom = actions.FindAction("Zoom", true);
            recenter = actions.FindAction("Recenter", true);
            cancel = actions.FindAction("ReleaseCursor", true);
            capture = actions.FindAction("CaptureCursor", true);
            actions.Enable();
        }
        void Start() => CaptureInput();
        void OnDisable()
        {
            ReleaseInput();
            if (actions) { actions.Disable(); Destroy(actions); }
        }
        void OnApplicationFocus(bool focus) { if (!focus) ReleaseInput(); }
        public void CaptureInput()
        {
            inputCaptured = true;
            // Re-focusing while Shift/RMB is already held is not a fresh dash.
            sprintWasHeld = sprint != null && sprint.IsPressed();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        public void ReleaseInput()
        {
            inputCaptured = false;
            dashRemaining = 0;
            horizontalVelocity = Vector3.zero;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        void Update()
        {
            if (!actions) return;
            if (cancel.WasPressedThisFrame()) ReleaseInput();
            else if (capture.WasPressedThisFrame()) CaptureInput();
            // Escape/unfocused Game view stops steering, but gravity keeps working.
            bool accept = inputCaptured && Application.isFocused;
            if (accept && followCamera)
                followCamera.AddLook(lookMouse.ReadValue<Vector2>(), lookStick.ReadValue<Vector2>(),
                    zoom.ReadValue<float>(), recenter.WasPressedThisFrame(), Time.deltaTime);
            Simulate(new Command {
                move = accept ? move.ReadValue<Vector2>() : Vector2.zero,
                jump = accept && jump.WasPressedThisFrame(),
                sprint = accept && sprint.IsPressed(), walk = accept && walk.IsPressed()
            }, Time.deltaTime);
        }

        // The runtime and deterministic verification use this same movement path.
        public void Simulate(Command command, float dt)
        {
            if (dt <= 0) return;
            if (!controller) controller = GetComponent<CharacterController>();
            if (!controller.enabled) return;
            dt = Mathf.Min(dt, .05f);
            bool dashPressed = command.sprint && !sprintWasHeld;
            sprintWasHeld = command.sprint;
            dashRecovery = Mathf.Max(0, dashRecovery - dt);
            Grounded = verticalVelocity <= 0 && (controller.isGrounded || ProbeGround());
            sinceGround = Grounded ? 0 : sinceGround + dt;
            sinceJumpPress = command.jump ? 0 : sinceJumpPress + dt;
            if (Grounded && verticalVelocity < 0) verticalVelocity = -2;
            if (sinceJumpPress <= jumpBuffer && sinceGround <= coyoteTime)
            {
                verticalVelocity = Mathf.Sqrt(2 * gravity * jumpHeight);
                Grounded = false;
                sinceGround = coyoteTime + 1;
                sinceJumpPress = jumpBuffer + 1;
                dashRemaining = 0;
            }
            Vector2 input = Vector2.ClampMagnitude(command.move, 1);
            float yaw = followCamera ? followCamera.Yaw : 0;
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y);
            if (dashPressed && Grounded && dashRecovery <= 0 && !IsDashing)
            {
                // Commit the initial heading, even when dashing from a standstill.
                dashDirection = direction.sqrMagnitude > .001f ? direction.normalized :
                    Vector3.ProjectOnPlane(visual ? visual.forward : transform.forward, Vector3.up).normalized;
                if (dashDirection.sqrMagnitude < .001f) dashDirection = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                dashRemaining = Mathf.Max(.01f, dashDuration);
                dashRecovery = Mathf.Max(dashCooldown, dashDuration);
            }
            if (!Grounded) dashRemaining = 0;
            bool dashing = IsDashing;
            if (dashing)
            {
                float progress = 1 - dashRemaining / Mathf.Max(.01f, dashDuration);
                float speed = Mathf.Lerp(dashSpeed, Mathf.Max(runSpeed, dashSpeed * .65f), progress);
                horizontalVelocity = dashDirection * speed;
            }
            else
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity,
                    direction * (command.walk ? walkSpeed : runSpeed), acceleration * dt);
            if (visual)
            {
                Vector3 heading = dashing ? dashDirection : direction;
                if (heading.sqrMagnitude < .001f) heading = Vector3.ProjectOnPlane(visual.forward, Vector3.up);
                if (heading.sqrMagnitude > .001f)
                    visual.rotation = Quaternion.Slerp(visual.rotation,
                        Quaternion.LookRotation(heading) * Quaternion.Euler(dashing ? dashLean : 0, 0, 0),
                        1 - Mathf.Exp(-turnSharpness * dt));
            }
            verticalVelocity = Mathf.Max(verticalVelocity - gravity * dt, -40);
            var start = transform.position;
            var flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            dashRemaining = (flags & CollisionFlags.Sides) != 0 ? 0 : Mathf.Max(0, dashRemaining - dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0) verticalVelocity = 0;
            if ((flags & CollisionFlags.Below) != 0 && verticalVelocity <= 0)
            { Grounded = true; verticalVelocity = -2; }
            Vector3 actual = transform.position - start;
            actual.y = 0;
            if (animator)
            {
                animator.SetFloat("Speed", actual.magnitude / dt, .10f, dt);
                animator.SetBool("Grounded", Grounded);
                animator.SetFloat("VerticalSpeed", verticalVelocity);
            }
            if (transform.position.y < -25) Respawn();
        }
        bool ProbeGround()
        {
            int count = Physics.SphereCastNonAlloc(transform.position + Vector3.up * .3f, .18f,
                Vector3.down, groundHits, .18f, groundMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!groundHits[i].transform.IsChildOf(transform) && groundHits[i].normal.y > .55f) return true;
            return false;
        }
        public void Respawn()
        {
            controller.enabled = false;
            transform.position = spawn;
            controller.enabled = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0;
            sinceGround = sinceJumpPress = 10;
            dashRemaining = dashRecovery = 0;
            sprintWasHeld = false;
            if (followCamera) followCamera.Snap();
        }
    }
}
