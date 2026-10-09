# Abstract environment rendering

`AbstractEnvironment` on the gameplay camera switches the configured Environment hierarchy between its original materials and black unlit surfaces with white structural edges. Press **I** in Play mode to cycle **Real → Abstract → Imaginary → Real**. Both the subway scene and SampleScene start in Real. Imaginary complements the displayed Abstract environment in sRGB, including emission colors; HDR channels are clamped to the visible range before inversion. Characters are excluded. Character material controls remain on **H**.

The surface pass ignores base color, lighting, metallic/smoothness, reflection probes and planar reflections. It samples the original emission map with its texture transform and HDR emission color. The station's sign graphics are explicitly retained as monochrome luminance, and the procedural floor grid remains visible. Glass becomes a solid black surface in Abstract mode.

A separate depth/normal buffer identifies environment silhouettes, depth discontinuities and changes in surface direction. This is not a triangle wireframe: coplanar triangulation does not introduce extra lines. The buffer uses a negative alpha marker to exclude characters from the final edge application. The original environment materials are stored by reference and restored on returning to Real / PBR; no source material assets are overwritten. Planar reflection is suspended during Abstract mode and restored to its previous enabled state afterward.

## Configuration

- `Environment Root`: renderers beneath this transform and Additional Environment Roots are converted. AdventurePlayer descendants are excluded.
- `Start Abstract`: initial Play mode appearance.
- `Line Width`: screen-pixel sampling radius.
- `Normal Threshold`: sensitivity to structural surface changes.
- `Depth Threshold`: relative depth discontinuity sensitivity.
- Shader references are serialized to keep the shaders available in builds.

This implementation targets the project's Built-in Render Pipeline. It uses one additional full-resolution geometry pass. Fine edges depend on screen resolution and geometry; flat painted details do not become outlines automatically. Scene view retains its original appearance outside Play mode.

Editor menu: `STAFF > Subway > Install Three Modes Both Scenes` installs the component in both saved scenes. SampleScene replaces every environment material, including its coordinate plane and LaTeX glyphs. The infinite plane uses an Abstract analytic surface adapter (shared `AbstractColor.cginc`) and contributes actual reconstructed depth/normals to the same edge pass. Coordinates and Mathematical Symbols are additional environment roots. Glyphs retain their original LaTeX texture, clipped by alpha in both the surface and geometry passes. There is no original-material bypass or background-only inversion. `Verify Abstract Mode` checks shader behavior and material restoration in Play mode and writes reports/captures to `Library/StaffSubway`.

## Verification status

Verified in the running Unity 6000.3.11f1 Editor on macOS / Metal, using Built-in RP (2026-10-08). Fixed a shader compilation failure caused by the reserved HLSL identifier `line` in the floor-grid calculation.

The Play mode verification passed shader compilation/support checks, rejected error-magenta renders, captured Abstract and Real / PBR frames, verified emission retention and reflection disable/restore, checked unchanged character material references, and restored the exact original environment material references across three toggle cycles. Captures and reports are in `Library/StaffSubway/<scene>-abstract-gameplay.png`, `<scene>-imaginary-gameplay.png`, `<scene>-real-gameplay.png`, and `<scene>-mode-validation.txt`. Both scenes passed three-mode Play verification; visual inspection confirmed white Imaginary surfaces, dark lines, complementary subway emission and unchanged character appearance.

Visual inspection confirmed black surfaces, white structural edges, blue emission and the original character appearance. Fine geometry at the platform edge and in the distance still produces dense/aliased lines; this is a remaining visual-quality limitation rather than a shader compilation failure.
