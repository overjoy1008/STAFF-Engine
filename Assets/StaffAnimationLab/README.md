# STAFF Animation Lab

Open `Assets/StaffAnimationLab/Scenes/Animation Test.unity` and press Play.
Alternatively use **Tools > Animation Lab > Open Test Scene**.

The standalone scene previews 92 Humanoid clips on the existing 11 STAFF characters:
84 new Quaternius Standard animations, three Hotaru7MMD Genshin fan motions,
and five Unity Starter Assets clips. The two reference T-poses are excluded.
It does not modify the gameplay controller or implement swimming/climbing physics.

## Controls

- Left panel: category, search and individual animation selection.
- Left/right arrows: previous/next animation in the full catalog.
- C or Character button: cycle characters while retaining the selected clip.
- Space: pause/resume. R: restart.
- Timeline: scrub the pose. Slow / Normal / Fast: half, normal or one-and-a-half speed.
- Repeat: repeat any clip; disable for one-shot inspection.
- Right-drag over the viewport: orbit. Mouse wheel: zoom.
- Context props: show/hide the seat, ledge or water-height reference.
- Keyboard shortcuts are suppressed while typing in Search.

Swimming previews are raised above the stage so the whole body is visible.
Preview uses in-place clips and manually evaluated Playables; character movement
and secondary physics are disabled for consistent comparisons. Props are visual
references, not collision or hand/foot IK targets. Ledge climb is a single climb-up
animation, not a continuous wall-climbing cycle. Weapon clips do not spawn weapons.
Original character proportions can produce hand/seat/ledge offsets requiring IK
when integrating these clips into gameplay.

## Sources

- Quaternius and Gonzalo Furnier: [Universal Animation Library](https://quaternius.itch.io/universal-animation-library)
  and [Universal Animation Library Two](https://quaternius.itch.io/universal-animation-library-2).
  Free Standard versions, CC0. Licenses and file hashes are in
  `Assets/ThirdParty/QuaterniusAnimations`. These are not Mixamo animations.
- Hotaru7MMD: the three already-installed fan recreations; original terms and
  sources remain in `Assets/ThirdParty/GenshinFanLocomotion`.
- Unity Starter Assets: existing files and provenance in `Assets/ThirdParty/UnityStarterAssets`.

`Inventory.tsv` records each displayed name, category, source and actual clip.
`Catalog.asset` references clips and character prefabs for player builds.
No paid packs were purchased. Original downloaded ZIP archives are stored in
`../STAFF/3D Assets/Animation Test Library`.

## Maintenance and validation

`Staff.AnimationLab.Editor.AnimationLabSetup.BuildBatch` imports the two FBX files,
rebuilds the catalog and this generated scene, then exits Unity. Run only in an
isolated batch project; it overwrites the generated lab scene and materials.
`Staff.AnimationLab.Editor.AnimationLabVerification.RunBatch` opens the lab in Play
mode and samples every clip on every character, checks Humanoid validity, finite
poses, timeline seeking and pause, and renders representative captures.
Results are written to `Library/StaffAnimationLab`.
