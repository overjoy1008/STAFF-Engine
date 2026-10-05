using Cinemachine;
using UnityEngine;

namespace Staff.Characters
{
    // Integration adapter for Wafflus' MIT-licensed camera. See ThirdParty/WafflusCamera.
    public sealed class WafflusCameraRig : MonoBehaviour, AxisState.IInputAxisProvider
    {
        public CinemachineVirtualCamera VirtualCamera { get; private set; }
        public CinemachinePOV Pov { get; private set; }
        public CinemachineFramingTransposer Body { get; private set; }
        public CinemachineCollider Obstacle { get; private set; }
        CinemachineBrain brain;
        Transform anchor;
        Vector2 look;
        bool lookPaused;
        public void SetLookPaused(bool paused)
        {
            lookPaused = paused;
            if (paused && Pov) { look = Vector2.zero; Pov.m_HorizontalAxis.Reset(); Pov.m_VerticalAxis.Reset(); }
        }
        float zoomTarget = 6;
        public float ZoomTarget => zoomTarget;
        public float GetAxisValue(int axis) => axis == 0 ? look.x : axis == 1 ? look.y : 0;

        public void Initialize(Camera output, Transform parent, LayerMask mask)
        {
            brain = output.gameObject.AddComponent<CinemachineBrain>();
            brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.ManualUpdate;
            brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0);
            var pivot = new GameObject("Wafflus Follow Target");
            pivot.transform.SetParent(parent, false); anchor = pivot.transform;
            VirtualCamera = gameObject.AddComponent<CinemachineVirtualCamera>();
            VirtualCamera.Follow = anchor; VirtualCamera.LookAt = anchor;
            VirtualCamera.m_Lens.FieldOfView = 60;
            VirtualCamera.m_Lens.NearClipPlane = output.nearClipPlane;
            VirtualCamera.m_Lens.FarClipPlane = output.farClipPlane;
            Body = VirtualCamera.AddCinemachineComponent<CinemachineFramingTransposer>();
            Body.m_CameraDistance = 6;
            Body.m_XDamping = .2f; Body.m_YDamping = .4f; Body.m_ZDamping = 1;
            Body.m_TargetMovementOnly = true;
            // Keep damping continuous during fast movement. The bounded soft zone
            // otherwise applies an immediate hard correction when a dash crosses it.
            Body.m_UnlimitedSoftZone = true;
            Body.m_ScreenX = Body.m_ScreenY = .5f;
            Body.m_DeadZoneWidth = Body.m_DeadZoneHeight = Body.m_DeadZoneDepth = 0;
            Body.m_SoftZoneWidth = Body.m_SoftZoneHeight = .8f;
            Pov = VirtualCamera.AddCinemachineComponent<CinemachinePOV>();
            Pov.m_RecenterTarget = CinemachinePOV.RecenterTargetMode.FollowTargetForward;
            Pov.m_VerticalAxis = new AxisState(-90, 90, false, false, .1f, .8f, .05f, "", true);
            Pov.m_HorizontalAxis = new AxisState(0, 360, true, false, .16f, .8f, .25f, "", false);
            Pov.m_VerticalAxis.m_SpeedMode = Pov.m_HorizontalAxis.m_SpeedMode = AxisState.SpeedMode.InputValueGain;
            Pov.m_HorizontalRecentering = new AxisState.Recentering(false, 0, 4);
            Pov.m_VerticalRecentering = new AxisState.Recentering(false, 1, 2);
            Pov.UpdateInputAxisProvider();
            Obstacle = gameObject.AddComponent<CinemachineCollider>();
            Obstacle.m_CollideAgainst = mask;
            Obstacle.m_IgnoreTag = "Player";
            Obstacle.m_MinimumDistanceFromTarget = .1f;
            Obstacle.m_CameraRadius = .1f;
            Obstacle.m_Strategy = CinemachineCollider.ResolutionStrategy.PullCameraForward;
            Obstacle.m_MaximumEffort = 4;
            Obstacle.m_SmoothingTime = Obstacle.m_Damping = Obstacle.m_DampingWhenOccluded = 0;
            SetActive(false);
        }
        public void SetActive(bool active)
        {
            look = Vector2.zero;
            VirtualCamera.enabled = active; brain.enabled = active;
            if (active) { Pov.m_VerticalAxis.Reset(); Pov.m_HorizontalAxis.Reset(); VirtualCamera.PreviousStateIsValid = false; }
        }
        public void SetView(float yaw, float pitch, float distance)
        {
            Pov.m_HorizontalAxis.Value = Mathf.Repeat(yaw, 360);
            Pov.m_VerticalAxis.Value = Mathf.Clamp(pitch, -90, 90);
            zoomTarget = Mathf.Clamp(distance, 1, 6); Body.m_CameraDistance = zoomTarget;
            VirtualCamera.PreviousStateIsValid = false;
        }
        public void AddLook(Vector2 mouse, Vector2 stick, float scroll, bool recenter, float dt, Transform facing)
        {
            // Unity InputSystem mouse wheel reports 120 units per desktop notch.
            look = lookPaused ? Vector2.zero : mouse + new Vector2(stick.x * (140 * dt / .16f), stick.y * (140 * dt / .1f));
            zoomTarget = Mathf.Clamp(zoomTarget - scroll / 120f, 1, 6);
            if (recenter && facing) { Pov.m_HorizontalAxis.Value = facing.eulerAngles.y; Pov.m_HorizontalRecentering.CancelRecentering(); }
        }
        public void Simulate(Transform target, Transform facing, Vector3 offset, float dt)
        {
            anchor.position = target.position + offset;
            anchor.rotation = Quaternion.Euler(0, facing ? facing.eulerAngles.y : target.eulerAngles.y, 0);
            // Faster scroll response while preserving the existing zoom range and step.
            Body.m_CameraDistance = Mathf.Lerp(Body.m_CameraDistance, zoomTarget, 12 * dt);
            bool recentering = Pov.m_HorizontalRecentering.m_enabled;
            if (lookPaused) Pov.m_HorizontalRecentering.m_enabled = false;
            brain.ManualUpdate();
            Pov.m_HorizontalRecentering.m_enabled = recentering;
            look = Vector2.zero;
        }
        public void UpdateRecentering(AdventurePlayer.Command command, bool grounded, float speed)
        {
            // Camera orientation stays under user control, including after movement stops.
            Pov.m_HorizontalRecentering.m_enabled = false;
            Pov.m_VerticalRecentering.m_enabled = false;
            Pov.m_HorizontalRecentering.CancelRecentering();
            Pov.m_VerticalRecentering.CancelRecentering();
        }
        void OnDestroy() { if (anchor) Destroy(anchor.gameObject); if (brain) Destroy(brain); }
    }
}
