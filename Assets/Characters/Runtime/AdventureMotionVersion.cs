using UnityEngine;
using UnityEngine.InputSystem;

namespace Staff.Characters
{
    // Both versions share one state machine, retaining jump/dance state and parameters.
    public sealed class AdventureMotionVersion : MonoBehaviour
    {
        AnimatorOverrideController genshin;
        AdventurePlayer player;
        public bool UseGenshin { get; private set; } = false;
        float labelUntil;

        void Awake()
        {
            player = GetComponent<AdventurePlayer>();
            genshin = Resources.Load<AnimatorOverrideController>("StaffGenshinMotion");
            if (!genshin) Debug.LogError("Missing StaffGenshinMotion animation resource.", this);
        }
        void Start() => ApplyTo(player.Animator);

        public void ApplyTo(Animator animator)
        {
            if (!genshin || !animator || !animator.isHuman) return;
            var current = animator.runtimeAnimatorController;
            // Leave unrelated controllers alone.
            if (current != genshin && current != genshin.runtimeAnimatorController) return;
            var target = UseGenshin ? genshin : genshin.runtimeAnimatorController;
            if (current == target) return;
            // Unity can reset parameter values when switching between a base
            // controller and an override, even though the state machine is shared.
            var parameters = animator.parameters;
            var values = new float[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.type == AnimatorControllerParameterType.Float) values[i] = animator.GetFloat(parameter.nameHash);
                else if (parameter.type == AnimatorControllerParameterType.Int) values[i] = animator.GetInteger(parameter.nameHash);
                else if (parameter.type == AnimatorControllerParameterType.Bool) values[i] = animator.GetBool(parameter.nameHash) ? 1 : 0;
            }
            var state = animator.GetCurrentAnimatorStateInfo(0);
            animator.runtimeAnimatorController = target;
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.type == AnimatorControllerParameterType.Float) animator.SetFloat(parameter.nameHash, values[i]);
                else if (parameter.type == AnimatorControllerParameterType.Int) animator.SetInteger(parameter.nameHash, (int)values[i]);
                else if (parameter.type == AnimatorControllerParameterType.Bool) animator.SetBool(parameter.nameHash, values[i] != 0);
            }
            var after = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != 0 && (after.fullPathHash != state.fullPathHash ||
                Mathf.Abs(after.normalizedTime - state.normalizedTime) > .001f))
                animator.Play(state.fullPathHash, 0, state.normalizedTime);
        }

        public bool HandleInput(bool accept)
        {
            if (!accept || player.InputBlocked || Keyboard.current == null ||
                !Keyboard.current.lKey.wasPressedThisFrame || !genshin) return false;
            UseGenshin = !UseGenshin;
            ApplyTo(player.Animator);
            labelUntil = Time.unscaledTime + 3;
            Debug.Log("Motion: " + (UseGenshin ? "Genshin fan (Hotaru7MMD)" : "Unity Starter Assets"), this);
            return true;
        }

        void OnGUI()
        {
            if (Time.unscaledTime > labelUntil || labelUntil == 0) return;
            GUI.Box(new Rect(16, Screen.height - 54, 370, 32),
                "[L] Motion: " + (UseGenshin ? "Genshin fan (Hotaru7MMD)" : "Unity Starter Assets"));
        }
    }
}
