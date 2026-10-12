STAFF Engine — URP migration, 2026-10-12

Target: Unity 6000.3.11f1 / URP 17.3.0 / Forward rendering / native Render Graph.
Graphics defaults to PC_RPAsset; PC and Mobile quality tiers use their URP assets.
HDR remains enabled. MSAA is disabled for the custom fullscreen path.

HoyoToon provenance
The requested https://github.com/kazama01/HoyoToon-URP fork was inspected at
caf149b3f6dd44ccce76ae52b648bfc02e8a186e. Its only public branch is still Built-in,
and its ZZZ implementation is absent. It is archived under HoyoToonFork.
The functioning renderer is therefore a STAFF URP adaptation of the project's
existing HoyoToon 5.2.7 ZZZ/MMD shader, not an installation of a working upstream
URP fork. Existing texture/color/opacity/two-tone controls remain intact.
Main/additional lighting, shadow casting, stencil and outline scheduling were
adapted. The unused Built-in ForwardAdd pass was removed. GPL notices retained.

Rendering
StaffRenderFeature schedules Hoyo stencil/outlines and the Nishita AgX / abstract
composites using Render Graph. Geometry masks use explicit camera matrices and
separate floating-point color/depth attachments. Imaginary mode preserves character
colors. Analytic mathematical planes retain LaTeX glyph provenance and correct depth.
First-person camera callbacks, planar reflections and editor captures use SRP APIs.
Standard materials/imported models and scene builders now use URP Lit.
Station, double-sided PBR and glass shaders include URP lighting and bake Meta passes.
The planar mirror remains an extra camera render; this is not an SSR implementation.

Lighting
Subway: Progressive CPU, baked indirect, fixed architecture/props/wayfinding marked
Contribute GI. Moving train and characters remain dynamic. UV2 generated for static
imported geometry. Bake completed: 2 directional lightmaps, 168 light probes.
Subway bloom: threshold 1.1, intensity 0.18. Reflection probe refreshed by the bake.
Rebake after geometry/light changes: STAFF > Rendering > Bake Current Scene (URP).
Nishita and MathSpace use separate LightingSettings; dynamic atmosphere is not baked.

Nishita
Blender single/multiple scattering compute code and all existing parameters retained.
AgX moved from OnRenderImage to the URP feature, with the original radiance scaling.
Validation: 14 parameter checks; 1,441 solar minute samples; 7 rendered hours;
extreme inputs finite. Sampled Blender EXR relative error: Single 0.9457%,
Multiple 4.0268%. These are radiance sample comparisons, not whole-image similarity.

Validation and limits
Mac Metal Play Mode: SampleScene, STAFF_Subway_Reference, Nishita Comparison.
HoyoToon / HoyoTwoTone / CharacterToon / Smooth rendered, plus abstract/imaginary
modes. Captured frames have zero error-magenta pixels; used shaders compile.
Snapshots and numeric results are in the companion Validation folder.
The isolated editor emitted a QuickSearch indexing exception during domain reload;
it was unrelated to renderer execution. No Windows/mobile device or FPS benchmark
was performed. Forward+ is not enabled: the custom Hoyo additional-light loop is
currently designed for Forward. This is a project-specific port, not universal
compatibility for all upstream HoyoToon game shaders.

Recovery
Before contains the complete pre-migration Assets, Packages and ProjectSettings.
before-hashes.json records original content; deployed-files.json records applied files.
Deployment refuses to overwrite a file changed since the backup.
