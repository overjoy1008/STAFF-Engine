using UnityEngine;
using Staff.Subway;
namespace Staff.Interaction
{
    public sealed class SubwayDoorInteraction : WorldInteraction
    {
        public SubwayDoors doors;
        public BoxCollider gate;
        public override Vector3 Focus => gate ? gate.transform.TransformPoint(gate.center) : base.Focus;
        public override string Label => doors && doors.IsOpen ? "문 닫기" : "문 열기";
        public override bool CanUse => base.CanUse && doors && !doors.InputLocked;
        public override bool Use(PlayerInteraction player)
        {
            if (!CanUse) return false;
            bool before = doors.IsOpen;
            doors.Toggle();
            return before != doors.IsOpen;
        }
    }
}
