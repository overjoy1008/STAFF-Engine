using UnityEngine;
using Staff.Subway;
namespace Staff.Interaction
{
    public sealed class SeatInteraction : WorldInteraction
    {
        [Tooltip("Position of the seated hips; forward is the direction the sitter faces.")]
        public Transform hipsAnchor;
        [Tooltip("Standing position on the floor in front of the seat.")]
        public Transform exitAnchor;
        public PlayerInteraction Occupant { get; internal set; }
        public override string Label => "앉기";
        public override Vector3 Focus => hipsAnchor ? hipsAnchor.position + Vector3.up * .35f : base.Focus;
        public override bool CanUse => base.CanUse && hipsAnchor && exitAnchor && !Occupant;
        public override bool Use(PlayerInteraction player) => CanUse && player.BeginSeat(this);
        protected override void OnDisable()
        {
            if (Occupant) Occupant.CancelSeat(true);
            base.OnDisable();
        }
    }

}
