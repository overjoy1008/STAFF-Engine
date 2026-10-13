STAFF Math City — Reference Environment

Scene: Assets/Scenes/STAFF_City_Reference.unity
Open: STAFF > City > Open Math City

A first-pass editable environment for the Hanyul / Shua mathematics story.
It is a contemporary human city, not the C.P.U. spin-off city.
The original generated art reference was completed and inspected before scene layout work:
Reference/City-Concept.png (built-in image_gen; generated 2026-10-13).
Reference/Prompt.txt contains its generation prompt.

REFERENCE TO SCENE
- Foreground zebra crossing and long narrow urban street.
- Ivory corner bookshop with sage awnings; ochre bookshop opposite.
- Midrise apartments, balconies, rooftop AC and aerials.
- Concrete utility poles, sagging cables, bollards, vending machines and plants.
- Ginkgo trees, an elevated commuter railway and distant urban massing.
- Warm surfaces, cool shadow shapes and atmospheric distance.

The scene geometry is authored procedurally for this project, not downloaded from
INDIEPLAY or extracted from an anime. The prior asset research was a candidate
list; no external 3D asset package is claimed as imported here. Painted surface
and sky shaders, modular meshes and ordinary-language sign textures are included.
No mathematical glyphs are rendered using ordinary fonts.

CONTROLS / SHARED SYSTEMS
The scene now uses Assets/StaffShared/Prefabs/STAFF Gameplay.prefab and STAFF Sky.prefab.
WASD movement; mouse look; V perspective; C character selection; R respawn.
T sky mode; F9 flare; F10 rays. See Assets/StaffShared/README.txt for shared editing.
The original City Reference Camera is retained as a disabled composition reference.

EDITING
Hierarchy is grouped by Environment, Lighting, Cameras.
Detailed geometry is combined by material per building/tree/prop group.
Two foreground buildings are also saved as reusable prefabs.
The builder source is Editor/MathCityBuilder.cs.
Rebuilding regenerates this city scene. Save a separate scene copy before custom edits.
The builder refuses an unsaved city scene and preserves other dirty scenes.
Each rebuild writes independent mesh assets so earlier meshes are not mutated.

RENDERING
The active shared gameplay camera uses the shared PC renderer with Nishita,
volumetric clouds, stars, sun/moon and solar optics. The painted surface shader
responds to shared sun intensity, color and ambient lighting.
The original city renderer remains available for the disabled reference camera.

SCOPE
This is an initial exterior environment with a shared, configurable 3-car subway on the elevated railway.
Not included: furnished building interiors, NPCs, combat, navmesh,
collision-tested character traversal, baked GI, production LODs or streaming.
The generated concept is a target reference; the Unity result is a stylized first pass.

VALIDATION
Actual camera captures and validation reports are copied to docs/MathCity.
