World interactions
==================
F: use the nearest visible interaction; while seated, F stands up.
The camera remains controllable. R resets. Changing character cancels sitting.
Existing O train-door control remains available; F obeys the same occupancy and transit locks.

Existing Street bench, Black bench and Secondary concourse bench objects receive two seat anchors at runtime. Shared gameplay initializes this in each scene. Train gates register after generated cars appear or are rebuilt. Cabin seat between doors cushions also receive seats along their length, facing the aisle. Seated passengers follow moving cars; exits are checked again at the end of the standing animation. Rebuilt cars register automatically. Scene assets and generated geometry are not rewritten.

New chairs: select the chair, choose STAFF > Interaction > Add seat to selected object.
Adjust Seated hips to the seated pelvis location, with its blue forward axis facing away from the backrest. Put Stand position on clear floor in front. Scene view shows the facing arrow and exit clearance circle. Adjust range in SeatInteraction.
Other actions can derive from WorldInteraction and implement Label, CanUse and Use.

Animation source: Quaternius Universal Animation Library One (CC0), Sitting_Enter, Sitting_Idle_Loop and Sitting_Exit. Existing provenance is in Assets/ThirdParty/QuaterniusAnimations.

UI reference visually inspected before implementation:
https://www.dancihu.com/article/44026.html
https://img1.dancihu.com/2024-02-29/06b37b27-78d4-733f-ab07-616cdb185819.jpg
Honkai: Star Rail / Golden Hour screenshot. Observed lower-right dark horizontal action bar, thin gold outline, circular F key, white action icon and label. Icons and geometry here are original Unity UI primitives; no game screenshot or extracted UI artwork is shipped.
