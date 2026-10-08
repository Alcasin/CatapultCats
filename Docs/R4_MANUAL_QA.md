# R4 — Presentation, HUD, and level flow

## Accepted visual baseline — do not regenerate

1. Exit Play Mode and wait for Unity compilation.
2. Do **not** run the full R4 Apply command. The saved Gameplay scene and generated sprites are now accepted, authored presentation. Apply refuses to rebuild an existing R4 scene, and sprite generation reuses existing art without repainting it.
3. No narrow Editor update is needed: the instruction is built by GameplayHud at runtime. Press Play to see its source update; the cream header and other UI retain their existing layout/colors.
4. Normal Gameplay Play begins with Level01_FirstShot, even if the Level Editor last playtested another level. Playtest uses a one-shot session override and does not rewrite production Gameplay.

The verified sequence is FirstShot, Collapse, GlassHouse, Ricochet, MouseFortress. Do not rerun R3 setup either: it rebuilds the scene foundation. Validate R4 is optional and must not be run in Play Mode.

## Manual checks

- Cat and mouse faces/silhouettes read clearly at gameplay size. Wood grain, cyan glass, dark metal heavy blocks, and green arrow ramps remain distinguishable when rotated. Check the same prefab visuals in the Level Editor.
- Sky, clouds, lawn, ground edge, and wooden slingshot are readable and decorative. Check 16:9 and a narrower landscape Game view: the pullback zone and current targets remain visible, with no HUD overlap.
- Dragging shows exactly four short dots, no solid trajectory line; launch still feels unchanged. Check the faint flight trail and short break bursts are unobtrusive.
- The first level initially shows a larger, dark instruction just above the ground, without a panel. It hides while dragging and during flight, disappears after the first valid shot, and does not return on later shots/retries/levels in that session.
- A valid launch removes one remaining-cat icon/count. An invalid short drag does not consume a cat. UI button clicks must not start a cat drag.
- Retry during aiming, flight, and settling: the same level is restored, fresh structure/mice and full cat stock return, and the cat is ready at that level's anchor.
- Win shows Retry and Next Level. Retry stays on the same level; Next advances through all five authored levels without a scene switch.
- Miss all shots: Fail appears only after resolution, offers Retry, and has no Next button.
- Win MouseFortress: Play Again returns to FirstShot. No persistent progression is stored.
- Use Level Editor > Playtest Current Level on a different authored level (and, optionally, one outside the playable sequence). That exact level loads and Retry stays there; an outside-sequence playtest does not offer progression. Exit Play Mode and normal Gameplay Play starts FirstShot again.
- Confirm accepted destruction/crush behavior, heavy mass, resolver timing, and shot lifecycle remain intact. No time-scale changes or camera shake are used.

## Automated/static coverage

`LevelSequenceTests` checks first/next/final and invalid-index/unknown-level lookup. Run the existing EditMode suite in Unity's Test Runner. Static .NET compilation is not a substitute for Unity import, sprite serialization, UI input, or visual/gameplay QA.

Temporary local compile tooling lives in ignored `Temp/R4StaticValidation.targets`; it only includes new sources and the existing UI assembly in Unity's generated solution for a compile-oriented check. It is not a runtime dependency or project asset.
