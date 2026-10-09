# Unity expression integration

Installs the 23 Character Studio presets for all 11 representative characters into `STAFF Engine`.

In Play mode, click the Game view to capture input:

- **Left / Right arrow**: previous / next expression, wrapping at either end.
- **Home**: basic neutral expression.
- **F7**: release the manual expression and restore the dance's automatic face.
- **C**: existing character picker. Expression input is suspended while it is open.

The bottom HUD displays the current expression. The existing WASD movement and dance number shortcuts remain unchanged. Start-up uses the existing automatic dance face until an expression is chosen. Switching characters retains the selected manual expression; Silver Robot has no facial shapes and is explicitly shown as unsupported.

`AdventureExpressions` attaches at scene load to the existing `AdventurePlayer`, so no scene or prefab edits are necessary. It applies supported source morphs after the dance face writer. Interrupted transitions continue from the visible state with a default duration of 0.45 seconds. Manual face selection does not stop the body dance.

Cheek blush uses an additional skinned face-only mesh and an included transparent shader. It follows the existing bones and facial morphs, and its strength transitions with the expression. Source FBX geometry, textures, materials and shaders remain unchanged. Generated meshes are mapped by source mesh and Avatar references, not guessed character names.

Source presets: `web/expressions/presets.js`. Regenerate JSON with `node tools/export_expression_catalog.mjs`. Face material slot mapping is extracted by `tools/expression_material_indices.py`, with the canonical Unity mapping in `Resources/ExpressionMaterials.json`.

`ExpressionSetup.BuildBatch` validates all referenced morph names and generates compact cheek meshes. `ExpressionVerification.RunBatch` checks every character/preset combination in Play mode, geometry deformation, intermediate and interrupted transitions, real Input System keyboard events, automatic/manual face mode and body dance coexistence. Run these in a validation copy before `tools/deploy_unity_expressions.py` installs the library.
