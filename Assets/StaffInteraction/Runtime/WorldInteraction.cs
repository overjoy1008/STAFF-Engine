using System.Collections.Generic;
using UnityEngine;
using Staff.Subway;

namespace Staff.Interaction
{
    public abstract class WorldInteraction : MonoBehaviour
    {
        public static readonly HashSet<WorldInteraction> Available = new();
        [Min(.2f)] public float range = 2.5f;
        public virtual Vector3 Focus => transform.position + Vector3.up * .8f;
        public abstract string Label { get; }
        public virtual bool CanUse => isActiveAndEnabled;
        public abstract bool Use(PlayerInteraction player);
        protected virtual void OnEnable() => Available.Add(this);
        protected virtual void OnDisable() => Available.Remove(this);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry() => Available.Clear();
    }

}
