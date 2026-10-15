# Movement animation versions

Play starts with Hotaru7MMD Genshin fan idle, walk and jog motions.
Press L while gameplay input is captured to switch to Unity Starter Assets, or back.
The selection persists across character changes within the current play session.
A temporary label identifies the selection. Picker/focus/cursor input gates apply.

Only idle, walk and run clips are replaced. Dash reuses the run/jog at the existing
faster playback rate and retains its movement/lean behavior. Jump, fall and all
dances retain the existing clips. The shared state machine and movement tuning
are unchanged. These are fan recreations, not extracted game animations.

Sources and creator terms: Assets/ThirdParty/GenshinFanLocomotion/manifest.json
and Hotaru7MMD-read-me.txt. The override references the existing local FBX files.
