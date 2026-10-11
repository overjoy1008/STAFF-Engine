using UnityEngine;
namespace Staff.Characters
{
    // Pose the neck before PmxPhysics (100), then sample eyes in the camera (30000).
    [DefaultExecutionOrder(50), DisallowMultipleComponent]
    public sealed class FirstPersonGazeDriver : MonoBehaviour
    {
        AdventureCamera owner;
        void Awake() => owner=GetComponent<AdventureCamera>();
        void Update() { if(owner) owner.Gaze.Restore(); }
        void LateUpdate() { if(owner && owner.isActiveAndEnabled) owner.PrepareFirstPersonGaze(Time.deltaTime); }
        void OnDisable() { if(owner)owner.Gaze.Restore(); }
    }
}
