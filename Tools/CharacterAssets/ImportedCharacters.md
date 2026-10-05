# Imported character roster

Open `Assets/Scenes/SampleScene.unity` and enter Play mode. Kafka (without coat) is always the starting character. Click the Game view to capture input, then press **C** to open the portrait picker. Click any of the 12 portraits to switch and resume playing; C, Escape, or Close dismisses the picker. While open, movement and camera input are blocked and the cursor is released. The robot remains selectable. **I** inverts the existing Monochrome Mode (background, grid and LaTeX symbols); press it again to restore. Character texture colors are preserved.

Movement stays on the existing `AdventurePlayer`: WASD runs, Left Ctrl + WASD walks, Shift/right mouse dashes, Space jumps, and Escape releases the cursor. Stationary characters use the robot's existing humanoid idle/breathing animation. Switching preserves the player position, camera, movement state, and animator state.

`CharacterSwitcher` creates one selected character instance and preserves the original robot. Each prefab uses a valid Humanoid Avatar and the shared `HumanoidAdventure.controller`. The original robot controller and camera are unchanged. The C and I bindings are local to the switcher and only accept input while the player has captured input and Unity has focus.

Assets: `Assets/ThirdParty/StaffCharacters/`. Each `character.json` records the original model identifier, variant and source URL. Character assets retain their original creators' terms; they are not relicensed as project code. Imported FBX files and textures are derived copies; source PMX assets remain in the sibling STAFF library.

Conversion: `../STAFF/3D Assets/Character Studio/tools/export_unity_characters.py`, run in Blender with the destination directory after `--`. It consolidates MMD D/EX leg weights, establishes a Humanoid hierarchy, maps 53 body/finger bones, and exports original-color PNG textures. Unity setup: `Tools > Characters > Build Imported Character Roster` (intended for regenerating assets, not needed to play). Adult bodies target 1.8 m; Aino and Ju Fufu target 1.35 m. The existing controller capsule remains unchanged.

Rendering defaults to a simple URP two-tone cutout shader. **H** toggles two-tone lighting on/off for the active character, including the silver robot. Imported characters use smooth diffuse lighting when off; the robot restores its original materials. The selection persists across C character switches and F8 Easter egg switches for the current Play session. H uses the same captured-input/focus requirement as C. Texture colors and alpha cutouts are preserved; the toggle only changes lighting. Runtime material overrides do not modify shared project materials.

`Staff.Characters.Editor.CharacterToonVerification.RunBatch` checks H input, C/F8 persistence, robot material restoration, shader compilation, and rendered on/off differences across the complete roster. Results are in `Library/StaffToon/`.

 MMD hair/cloth physics and the WAVEFILE dance are not part of locomotion; extra bones retain their relative pose and follow the animated skeleton. This is not the original games' shader or physics implementation.

Verification: `Staff.Characters.Editor.CharacterRosterVerification.RunBatch` enters real Play mode and checks the C picker binding, all 11 avatars, breathing, walk/run clips and displacement, jump/landing and return to the robot. Results and pose screenshots are in `Library/StaffCharacters/`. The verifier exits only in batch mode; a live editor session is preserved.

## Corrected reference pose and Easter egg

The extra FBX facing rotation was removed. Avatar setup now uses the installed Unity editor’s Enforce T-Pose implementation to calibrate its reference skeleton from the MMD A-pose without changing mesh bind poses. Verification explicitly checks forward-facing eye/head geometry and both hands below shoulders during idle for all 11 characters.

**F8** toggles the preserved backwards/raised-arms Easter egg on or off. **C** opens the picker in either mode. Kafka stays the default. `Resources/StaffCharacterRoster.asset` supplies the latest roster even if the scene was kept open during the update. Stop and restart Play mode after scripts import.

The Easter egg has independent FBX/Avatar assets under `Assets/ThirdParty/StaffCharacterEasterEgg` and prefabs under `Assets/Characters/Prefabs/EasterEgg`. It shares only unchanged textures/materials and locomotion clips with the normal characters.

## Portrait picker verification

`Staff.Characters.Editor.CharacterPickerVerification.RunBatch` checks Kafka startup, I inversion/restoration, C/Escape menu controls, gameplay input blocking, every portrait and selection, H state retention, and the preserved F8 mode. Results and a menu screenshot are in `Library/StaffPicker/`. Portrait textures in `Resources/CharacterPortraits` come from the validated default-pose renders; the picker does not instantiate extra 3D models.
