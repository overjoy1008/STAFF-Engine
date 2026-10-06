# STAFF subway reference layout

Open `Assets/Scenes/STAFF_Subway_Reference.unity`. This is a separate, editable scene built from the supplied Tripo assets and the station reference composition: a long platform, repeated columns on the left, a train on the right, overhead signs, restrained blue accents and a reflective tiled floor.

This pass uses the material/lighting appearance of attachment 2. The two environment shader states are not implemented as a switch in this layout pass. Character materials and shaders remain those of SampleScene; the source SampleScene is unchanged.

## Scale

One Unity unit is one metre. All model dimensions are normalized from their imported renderer bounds, independently of FBX axis transforms. Normalization is on a separate parent, preserving the source model and its door animation hierarchy.

- Human comparison figures: 1.70 m tall, three greybox mannequins.
- Train cars: 3.10 m wide, 3.85 m high, 20.20 m long; rail/wheel bottom at y=-1.06 m.
- Side door panels: approximately 2.01 m tall; lower edge y=-0.028 m relative to the platform.
- Train-to-platform edge gap: approximately 0.125 m.
- Benches: 2.45 m long, 0.67 m deep, 0.87 m overall height.
- Columns: 0.90 m square, 4.50 m tall, 7 m pitch.
- Platform top: y=0. Main platform width 11.4 m and length 114 m.
- Camera eye height: 1.72 m.

## Assets and additions

`Models/Train/TrainPBRGlow.fbx` comes from `STAFF/3D Studio/Tripo/Subway/PBR Glow`. Base color, emission and metallic/roughness data are retained. ORM green is converted to inverse smoothness in Unity's metallic-map alpha; the blue channel supplies metallic. Glass uses Built-in Standard transparency, not Blender's physical transmission model.

The other ten prop categories use the supplied Tripo meshes: black/blue benches, column, tactile paving, platform edge, floor, hanging sign, LED fixture, station display and railing. Original GLBs were converted to FBX for native Unity importing. `Models/import-manifest.json` records sources and conversion bounds.

Greybox additions: platform slab, wall and ceiling shell, beams, rails/sleepers, distant tactile strips, stairwell, train cabin floor/seats/ceiling, sign faces, light diffusers and human scale figures. The five-car blockout repeats the supplied front module; dedicated intermediate car/coupler topology is a future refinement. Door animations remain in the FBX but no interactive door controller is added.

Wayfinding textures are generated for legibility. The platform numeral uses actual LaTeX via the project's existing typography tool, with source/provenance under `Mathematics/PlatformNumber`. No character models are replaced.

## Explore

Play starts with the same character roster, humanoid animator, AdventurePlayer input/movement, CharacterSwitcher and AdventureCamera/Wafflus rig as SampleScene. The default character is TheHerta in both scenes. Click the Game view to capture input.

- WASD: move; Left Ctrl: walk; Space: jump.
- Shift or right mouse button: dash.
- Mouse: orbit; wheel: zoom; middle mouse button: recenter.
- C: character picker; H: character shader; F6: camera version; 1–8: existing dances.
- Escape: release cursor; left click: capture again; Alt: existing cursor hold.

The reference fly camera is retained disabled under Cameras. The active gameplay camera includes the station's planar reflection; station lights also illuminate character layer 0. Character shader handling stays in the existing character system. Platform, columns, benches and closed train cars have collision. Track-edge and platform-end barriers keep this layout study walkable; train boarding/doors are not interactive.

`STAFF > Subway > Import SampleScene Gameplay` installs the source gameplay hierarchy into a station without an AdventurePlayer. It preserves a pre-import backup in `Library/StaffSubway/before-gameplay.unity`, remaps player/camera references and never saves SampleScene.
Hierarchy separates Cameras, Lighting, Environment, and Scale References. Each supplied model sits under a named instance and `Metre normalization` parent, allowing position/size edits without modifying the source.

Editor menu: `STAFF > Subway > Frame Reference View`. `Build Reference Station` regenerates this owned scene and imports SampleScene gameplay, so save a duplicate before using it after manual layout edits. Existing user scenes are preserved. The diagnostic capture command reapplies reference lighting and saves this scene; use it only when regenerating reference previews.

## Verification

Unity compiler/shader checks, actual renderer bounds, material reimport and saved-scene captures are checked. Working reports and render captures are in `Library/StaffSubway`; this folder is generated and can be recreated. Final preview PNGs are retained alongside this README. Original SampleScene, Packages and rendering/quality settings are hash-checked against the pre-task state.

This is a placement and visual study, not a production lighting/performance pass. It retains individual train parts and uses realtime planar reflection; large-scene optimization, environment mode switching remain later work.

Gameplay validation runs in real Unity Play mode through `Verify Play Mode`: grounded spawn, running, jump/landing, dash, column/edge collision, character replacement, input asset references, one listener and live reflection. The saved `gameplay-view.png` is a render from the gameplay camera.
