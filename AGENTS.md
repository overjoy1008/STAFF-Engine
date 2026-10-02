# STAFF Engine project conventions

All visible mathematical numbers, symbols, and formulas in Unity must come from
actual LaTeX typesetting or the installed 3Blue1Brown Manim toolchain in
`../STAFF/Manim`. Do not substitute Unicode letters, ordinary fonts, Unity UI Text,
TextMesh, or TextMeshPro for mathematical glyphs, including the origin O.

Use `Tools/MathTypography/render_latex.py` to generate outlined SVG and transparent
PNG assets from LaTeX; keep the .tex source and generation metadata with the asset.
Attach `LatexSymbol` provenance to world-space symbol renderers. The installed
Manim Python environment has the required Pillow dependency. Asset names and
Inspector/editor-only labels are not rendered mathematical notation.

Organize Hierarchy by object role (Cameras, Lighting, Environment, Coordinates,
Mathematical Symbols). Use a UI category only for actual game interface objects.
Preserve existing cameras/lights and user-authored content when editing scenes.
