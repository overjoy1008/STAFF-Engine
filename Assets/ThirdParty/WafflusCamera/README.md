# Wafflus camera comparison

Upstream: https://github.com/Wafflus/unity-genshin-impact-movement-system
Snapshot: 3bb190862b96cee17ec0127b857ce9fa37d6de14

Original source/prefab snapshots are retained as .txt references and covered by the upstream MIT license. Runtime adapter: Assets/Characters/Runtime/WafflusCameraRig.cs. Uses actual Cinemachine 2.8.4 POV, FramingTransposer, Collider and Brain. Cinemachine retains its Unity Companion License.

Unity 6 / URP compatibility: CinemachinePixelPerfect.cs namespace changed from UnityEngine.Experimental.Rendering.Universal to UnityEngine.Rendering.Universal. The runtime asmdef also references Unity.RenderPipelines.Universal.2D.Runtime; the obsolete PixelPerfect edit-mode property check was removed. These changes affect the unused 2D extension; camera algorithms unchanged.

Adaptations for the existing project: follow target uses its existing player pivot offset; collision mask uses its existing scene layers rather than upstream layer 22; existing movement controller supplies recenter direction/speed; mouse-wheel input is normalized to desktop notches. Character movement and animations are unchanged.

Cinemachine is the only camera; legacy Current mode and F6 switching are removed. Initial view uses distance 6, pitch 0 and the serialized horizontal heading. Alt/Option pauses look and releases the cursor. No scene/prefab edits required.

Hold either Alt/Option to free the cursor in both modes; release both to resume relative look from the unchanged camera view. MouseMovement mode has been removed.
