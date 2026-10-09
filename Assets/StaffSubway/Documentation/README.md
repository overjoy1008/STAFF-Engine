# STAFF subway reference layout

Open `Assets/Scenes/STAFF_Subway_Reference.unity`. This is a separate, editable scene built from the supplied Tripo assets and the station reference composition: a long platform, repeated columns on the left, a train on the right, overhead signs, restrained blue accents and a reflective tiled floor.

This pass uses the material/lighting appearance of attachment 2. The gameplay camera now includes an Abstract environment mode: I cycles Real → Abstract → Imaginary → Real. Abstract uses black surfaces / white edges with preserved emission; Imaginary inverts the environment, including emission colors. See [Abstract mode](AbstractMode.md) for setup and verification limitations. Character materials and shaders remain those of SampleScene; SampleScene uses the same three-mode controls while preserving its procedural grid and LaTeX symbols.

## Scale

One Unity unit is one metre. All model dimensions are normalized from their imported renderer bounds, independently of FBX axis transforms. Normalization is on a separate parent, preserving the source model and its door animation hierarchy.

- Human comparison figures: 1.70 m tall, three greybox mannequins.
- Train cars: 4.65 m wide, 5.775 m high, 30.30 m long; rail/wheel bottom at y=-1.59 m.
- Side door panels: approximately 3.015 m tall; lower edge y=-0.042 m relative to the platform.
- Train-to-platform edge gap: approximately 0.1875 m.
- Benches: 3.675 m long, 1.005 m deep, 1.305 m overall height.
- Columns: 1.35 m square, 6.75 m tall, 10.5 m pitch.
- Platform top: y=0. Main platform width 17.1 m and length 171 m.
- Reference camera eye height: 2.58 m. The character-follow camera and character dimensions retain their original scale.

## Assets and additions

`Models/Train/TrainPBRGlow.fbx` comes from `STAFF/3D Studio/Tripo/Subway/PBR Glow`. Base color, emission and metallic/roughness data are retained. ORM green is converted to inverse smoothness in Unity's metallic-map alpha; the blue channel supplies metallic. Glass uses Built-in Standard transparency, not Blender's physical transmission model.

The other ten prop categories use the supplied Tripo meshes: black/blue benches, column, tactile paving, platform edge, floor, hanging sign, LED fixture, station display and railing. Original GLBs were converted to FBX for native Unity importing. `Models/import-manifest.json` records sources and conversion bounds.

Greybox additions: platform slab, wall and ceiling shell, beams, rails/sleepers, distant tactile strips, stairwell, train cabin floor/seats/ceiling, sign faces, light diffusers and human scale figures. The five-car consist uses one Front, three Middle and one Rear module. From the near platform end toward +Z the order is Rear → Middle → Middle → Middle → Front; both cabs face outward. All modules use the front asset’s common source-unit scale, retaining the middle module’s slightly narrower body. Gangway seams are 0.025 m. Each module’s original GLB `Doors_All_OpenClose` animation is extracted into its model folder’s `DoorAnimation.json` with source SHA256 (88 tracks across the consist). `SubwayDoors` uses its LINEAR translation keyframes, retargeted to the baked FBX transforms: outward unplug, then side slide. O opens all side and rear doors, holds them open, and closes them on the next press; reversal during motion is continuous. Extraction is reproducible with `Tools/Subway/extract_door_animation.py`.

Wayfinding textures are generated for legibility. The platform numeral uses actual LaTeX via the project's existing typography tool, with source/provenance under `Mathematics/PlatformNumber`. No character models are replaced.

## Explore

Play starts with the same character roster, humanoid animator, AdventurePlayer input/movement, CharacterSwitcher and AdventureCamera/Wafflus rig as SampleScene. The default character is TheHerta in both scenes. Click the Game view to capture input.

- WASD: move; Left Ctrl: walk; Space: jump.
- Shift or right mouse button: dash.
- Mouse: orbit; wheel: zoom; middle mouse button: recenter.
- C: character picker; H: character shader; F6: camera version; 1–8: existing dances.
- I: Real → Abstract → Imaginary; O: train doors open / close.
- Escape: release cursor; left click: capture again; Alt: existing cursor hold.

The reference fly camera is retained disabled under Cameras. The active gameplay camera includes the station's planar reflection; station lights also illuminate character layer 0. Character shader handling stays in the existing character system. Platform, columns, segmented cabin benches, side walls and closed doorways have collision. O opens the doors and enables all 20 platform-side boarding paths, with level sills and continuous cabin/gangway floors. Closed doorways block entry and exit. Closing is deferred while the player occupies a doorway; a closing door reopens if its doorway becomes occupied. Track-edge barriers remain between entrances. End-connection doors also follow the same open/closed collision state.

`STAFF > Subway > Import SampleScene Gameplay` installs the source gameplay hierarchy into a station without an AdventurePlayer. It preserves a pre-import backup in `Library/StaffSubway/before-gameplay.unity`, remaps player/camera references and never saves SampleScene.
Hierarchy separates Cameras, Lighting, Environment, and Scale References. Each supplied model sits under a named instance and `Metre normalization` parent, allowing position/size edits without modifying the source.

Editor menu: `STAFF > Subway > Frame Reference View`. `Build Reference Station` regenerates this owned scene and imports SampleScene gameplay, so save a duplicate before using it after manual layout edits. Existing user scenes are preserved. The diagnostic capture command reapplies reference lighting and saves this scene; use it only when regenerating reference previews.

## Verification

Unity compiler/shader checks, actual renderer bounds, material reimport and saved-scene captures are checked. Working reports and render captures are in `Library/StaffSubway`; this folder is generated and can be recreated. Final preview PNGs are retained alongside this README. Original SampleScene, Packages and rendering/quality settings are hash-checked against the pre-task state.

This is a placement and visual study, not a production lighting/performance pass. It retains individual train parts and uses realtime planar reflection; large-scene optimization, further Abstract edge quality tuning remain later work.

Gameplay validation runs in real Unity Play mode through `Verify Play Mode`: grounded spawn, running, jump/landing, dash, column/edge collision, character replacement, input asset references, one listener and live reflection. The saved `gameplay-view.png` is a render from the gameplay camera.

The station environment, colliders and lighting layout were scaled uniformly by 1.5. Light ranges and procedural floor spacing were scaled to match; character models retain their size.

## Modular consist verification

`STAFF > Subway > Verify Modular Boarding` uses the actual player CharacterController to check all 20 platform entrances, closed-door blocking in both directions, open-door boarding/exit, doorway occupancy protection, and passage across all four gangways. Captures and the report are saved under `Library/StaffSubway`. The interiors remain a greybox cabin, using segmented benches and a continuous aisle.
