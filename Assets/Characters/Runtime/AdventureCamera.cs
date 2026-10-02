using UnityEngine;

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
        float currentDistance;
        bool initialized;
        public float Yaw => yaw;
        public float Pitch => pitch;
        public float Distance => currentDistance;
        public Transform Target => target;
        public void Configure(Transform player, Transform model) { target = player; facing = model; Snap(); }
        void OnEnable() => initialized = false;
        public void AddLook(Vector2 mouse, Vector2 stick, float scroll, bool recenter, float dt)
        {
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
            pivot = target.position + targetOffset;
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
            if (dt > 0) pivot = Vector3.SmoothDamp(pivot, target.position + targetOffset,
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
