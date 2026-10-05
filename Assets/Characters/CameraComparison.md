# Camera comparison

Play starts with Wafflus / Cinemachine. F6 cycles Cinemachine → Current → Cinemachine. Each version retains its camera heading, pitch and zoom.

Hold either Alt/Option to release the cursor and pause camera look. Both modes support this. Zoom remains active and dancing continues. Releasing one key while the other is held keeps the cursor free. Once both keys are released, the existing camera view is preserved and relative mouse movement resumes; the relock-frame mouse delta is ignored to avoid a warp-induced camera jump. Cinemachine rotation inertia and auto-recentering are paused during the hold.

Escape, focus loss and the character picker can still release gameplay input; releasing Option never overrides those states. Movement, jump and dash still cancel dancing.

MouseMovement mode and its runtime implementation have been removed. Cinemachine retains its existing integration and Unity 6 API compatibility fixes.

Verification covers both modes, default startup, two-mode cycling, cursor hold/release, frozen look/recentering, delta-jump prevention, resumed relative look and dancing. Batch Game view cannot validate native operating-system cursor locking; input state and requested cursor visibility/capture are tested.

Both cameras now focus at the active humanoid character’s rest-pose chest center (chest bind position plus 0.05 world units; upper chest or spine fallback), measured when the character changes. Animated torso motion and dash lean do not change this height. Cinemachine uses an unlimited soft zone to retain continuous damping instead of hard frame-boundary corrections during fast movement. Untagged AdventurePlayer roots are tagged Player at runtime to match the existing Cinemachine self-collision exclusion.

Cinemachine zoom interpolation gain is now 12 (previously 4) for quicker scroll response. Zoom step and limits are unchanged.
