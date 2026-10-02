STAFF Starter Assets silver humanoid third-person prototype

Play Assets/Scenes/SampleScene.unity. Click the Game view if input is not captured.
WASD: camera-relative run. Hold Left Ctrl: walk.
Press Left Shift or Right Mouse: a short fast dash, then normal running.
Holding the button never repeats a dash; release and press again after cooldown.
Default run: 8 m/s (walk remains 1.8 m/s), acceleration 48 m/s squared.
Default dash: 18 m/s easing down over 0.24 seconds; 0.6 seconds between starts.
Humanoid running/dash playback is tuned to 1.55x/1.9x; dash lean is 12 degrees.
No movement input: dash in the facing direction, then stop. Ground-only;
jumping, losing ground or releasing input focus cancels the burst. No invulnerability.
Space: jump. Mouse: orbit. Wheel: zoom. Middle mouse: recenter behind the robot.
Escape: release cursor / stop steering. Left click: capture again.
Gamepad: left stick move, stick press dash, south button jump,
right stick orbit, right stick press recenter.

Hierarchy: Characters / Player Humanoid (CharacterController + AdventurePlayer)
Cameras / Main Camera retains its original Camera and AudioListener, with
AdventureCamera added. Lighting and the coordinate-space objects are preserved.
The existing Environment / Monochrome Mode remains available in both modes.
Character colors intentionally stay unchanged by coordinate-space inversion.

The active model is Unity Starter Assets Armature, with a valid Human Avatar,
silver URP material and approximately 1.8m standing height.
Animator uses a blended idle/walk/run/sprint tree plus jump and fall/landing
transitions. Dash reuses faster Running with a small visual forward lean (no new
dedicated dash clip). Fall uses InAir animation. Collision capsule remains upright.
Unused original source clips remain available for later integration.
This is a traversal prototype inspired by third-person action RPG controls;
it does not implement Genshin characters, combat, climbing, gliding or stamina.

Active model/animation source: Assets/ThirdParty/UnityStarterAssets/SOURCE.txt
License: Unity Companion License, not CC0. Original notice is retained there.
The previous orange Quaternius robot is no longer in the scene. Its source assets
and legacy prefab remain available for recovery, with their separate CC0 license.
Controller/camera are project-local C# using Unity CharacterController and the
already installed Input System. No new package dependencies or UI are required.
Input bindings are editable in Input/Adventure.inputactions. Movement and camera
values are editable on their respective Inspector components.
