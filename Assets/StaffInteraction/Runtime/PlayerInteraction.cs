using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.InputSystem;
using Staff.Characters;

namespace Staff.Interaction
{
    [DefaultExecutionOrder(50), DisallowMultipleComponent]
    public sealed class PlayerInteraction : MonoBehaviour
    {
        AdventurePlayer player;
        CharacterController body;
        SeatAnimations clips;
        SeatInteraction seat;
        Animator animated;
        PlayableGraph graph;
        AnimationClipPlayable motion;
        AnimationPlayableOutput output;
        InteractionPrompt prompt;
        Vector3 approach, seatedRootOffset, exitPosition;
        Quaternion approachRotation;
        bool restoreBody;
        float elapsed, nextScan;
        enum Phase { None, Enter, Idle, Exit }
        Phase phase;
        public bool IsSeated => phase != Phase.None;
        public WorldInteraction Target { get; private set; }

        void Awake()
        {
            player = GetComponent<AdventurePlayer>(); body = GetComponent<CharacterController>();
            clips = Resources.Load<SeatAnimations>("StaffSeatAnimations");
        }
        void Start()
        {
            InteractionSceneInstaller.Install(gameObject.scene);
            prompt = gameObject.AddComponent<InteractionPrompt>();
        }
        void Update()
        {
            if (Time.unscaledTime >= nextScan)
            {
                InteractionSceneInstaller.Install(gameObject.scene);
                InteractionSceneInstaller.InstallDoors(gameObject.scene);
                nextScan = Time.unscaledTime + 1;
            }
            HandleInput(player && player.enabled && player.InputCaptured && !player.InputBlocked && Application.isFocused);
        }
        public void HandleInput(bool accept)
        {
            accept = accept && player && player.enabled && !player.InputBlocked;
            Target = null;
            if (IsSeated)
            {
                if (!seat || !seat.isActiveAndEnabled || !animated) { CancelSeat(true); return; }
                if (accept && phase == Phase.Idle && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                    RequestStand();
            }
            else if (accept)
            {
                Target = FindTarget();
                if (Target && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                    if (!Target.Use(this)) prompt?.Flash("지금은 사용할 수 없습니다");
            }
            if (prompt) prompt.Show(accept ? (IsSeated ? phase == Phase.Idle ? "일어나기" : "" : Target ? Target.Label : "") : "",
                IsSeated || Target is SeatInteraction);
        }
        public WorldInteraction FindTarget()
        {
            WorldInteraction best = null; float score = float.PositiveInfinity;
            var camera = Camera.main;
            foreach (var item in WorldInteraction.Available)
            {
                if (!item || item.gameObject.scene != gameObject.scene || !item.CanUse) continue;
                Vector3 delta = item.Focus - (transform.position + Vector3.up * .8f);
                if (delta.sqrMagnitude > item.range * item.range) continue;
                if (item is SeatInteraction && (!player.Grounded || player.IsDashing)) continue;
                if (camera)
                {
                    var screen = camera.WorldToViewportPoint(item.Focus);
                    if (screen.z <= 0 || screen.x < .05f || screen.x > .95f || screen.y < .05f || screen.y > .95f) continue;
                }
                if (!Visible(item)) continue;
                float candidate = delta.sqrMagnitude;
                if (candidate < score) { score = candidate; best = item; }
            }
            return best;
        }
        bool Visible(WorldInteraction item)
        {
            Vector3 start = transform.position + Vector3.up * 1.25f;
            var direction = item.Focus - start;
            foreach (var hit in Physics.RaycastAll(start, direction.normalized, direction.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(item.transform) || (item is SeatInteraction && item.transform.parent == hit.transform)) continue;
                // Authored station benches have a separate proxy collider around the entire bench.
                // Only its final contact is permitted; intervening walls remain blocking.
                if (item is SeatInteraction && hit.collider.name.EndsWith("bench collider") &&
                    hit.collider.bounds.Contains(item.Focus - Vector3.up * .35f)) continue;
                return false;
            }
            return true;
        }
        public bool BeginSeat(SeatInteraction target)
        {
            if (IsSeated || !target || !target.CanUse || !player.Grounded || player.IsDashing || player.InputBlocked ||
                !clips || !clips.enter || !clips.idle || !clips.exit || !player.Animator || !player.Animator.isHuman ||
                Vector3.Distance(target.Focus, transform.position + Vector3.up * .8f) > target.range || !Visible(target)) return false;
            if (!FindExit(target, out exitPosition)) return false;
            player.GetComponent<AdventureDance>()?.StopDance();
            approach = transform.position; approachRotation = player.Visual.rotation;
            restoreBody = body.enabled; body.enabled = false;
            player.ClearInteractionMotion();
            seat = target; seat.Occupant = this; animated = player.Animator;
            player.Visual.rotation = seat.hipsAnchor.rotation;
            graph = PlayableGraph.Create("Seated character"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            output = AnimationPlayableOutput.Create(graph, "Seat", animated);
            Play(clips.idle); graph.Play(); graph.Evaluate(0);
            var hips = animated.GetBoneTransform(HumanBodyBones.Hips);
            if (!hips) { CancelSeat(true); return false; }
            // Retargeted avatars have different leg lengths. Align their actual humanoid hips to the seat.
            seatedRootOffset = seat.hipsAnchor.InverseTransformVector(transform.position - hips.position);
            AlignSeat();
            phase = Phase.Enter; Play(clips.enter);
            return true;
        }
        void Play(AnimationClip clip)
        {
            if (motion.IsValid()) graph.DestroyPlayable(motion);
            motion = AnimationClipPlayable.Create(graph, clip);
            motion.SetApplyFootIK(false); motion.SetApplyPlayableIK(false);
            output.SetSourcePlayable(motion); elapsed = 0;
        }
        void LateUpdate()
        {
            if (!IsSeated || !graph.IsValid() || !seat) return;
            elapsed += Time.deltaTime;
            if (phase == Phase.Enter && elapsed >= clips.enter.length) { phase = Phase.Idle; Play(clips.idle); }
            else if (phase == Phase.Exit && elapsed >= clips.exit.length)
            {
                if (FindExit(seat, out exitPosition)) { FinishSeat(exitPosition); return; }
                phase = Phase.Idle; Play(clips.idle); prompt?.Flash("일어날 공간이 없습니다");
            }
            motion.SetTime(phase == Phase.Idle ? elapsed % clips.idle.length : elapsed);
            AlignSeat(); graph.Evaluate(0);
        }
        void AlignSeat()
        {
            transform.position = seat.hipsAnchor.position + seat.hipsAnchor.TransformVector(seatedRootOffset);
            player.Visual.rotation = seat.hipsAnchor.rotation;
        }
        public bool RequestStand()
        {
            if (phase != Phase.Idle) return false;
            if (!FindExit(seat, out exitPosition)) { prompt?.Flash("일어날 공간이 없습니다"); return false; }
            phase = Phase.Exit; Play(clips.exit); return true;
        }
        bool FindExit(SeatInteraction target, out Vector3 destination)
        {
            var forward = target.hipsAnchor.forward;
            var right = target.hipsAnchor.right;
            var origin = target.exitAnchor.position;
            foreach (var point in new[] { origin, origin + right * .65f, origin - right * .65f, origin + forward * .7f })
            {
                if (!Physics.Raycast(point + Vector3.up * .6f, Vector3.down, out var floor, 1.5f, ~0, QueryTriggerInteraction.Ignore) || floor.normal.y < .7f) continue;
                var p = new Vector3(point.x, floor.point.y + .035f, point.z);
                float radius = body.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
                float height = Mathf.Max(radius * 2, body.height * transform.lossyScale.y);
                var center = p + transform.TransformVector(body.center);
                bool clear = true;
                foreach (var collider in Physics.OverlapCapsule(center + Vector3.up * (height / 2 - radius), center - Vector3.up * (height / 2 - radius), radius * .95f, ~0, QueryTriggerInteraction.Ignore))
                    if (!collider.transform.IsChildOf(transform)) { clear = false; break; }
                if (clear) { destination = p; return true; }
            }
            destination = default; return false;
        }
        public void CancelSeat(bool returnToApproach)
        {
            if (!IsSeated && !graph.IsValid()) return;
            Vector3 destination = transform.position;
            if (returnToApproach) destination = seat && FindExit(seat, out var currentExit) ? currentExit : approach;
            FinishSeat(destination);
            if (returnToApproach && player.Visual) player.Visual.rotation = approachRotation;
        }
        void FinishSeat(Vector3 destination)
        {
            if (graph.IsValid()) graph.Destroy();
            if (seat) seat.Occupant = null;
            seat = null; phase = Phase.None;
            transform.position = destination;
            body.enabled = restoreBody;
            player.ClearInteractionMotion();
            if (animated)
            {
                animated.Rebind(); animated.SetFloat("Speed", 0); animated.SetBool("Grounded", true);
                animated.SetFloat("VerticalSpeed", 0); animated.Play("Base Layer.Locomotion", 0, 0); animated.Update(0);
            }
            animated = null;
        }
        void OnDisable() { CancelSeat(true); if (prompt) prompt.Show("", false); }
    }
}
