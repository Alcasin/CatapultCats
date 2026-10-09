# R6 — Windows portfolio release QA

Status: **standalone build and gameplay QA pending**. R1–R5 Editor gameplay was manually accepted. Do not mark this checklist complete from static source review or successful compilation alone.

Record the tested commit/worktree, Unity version, Windows version, build date and any failures when performing QA. Do not replace the accepted levels, artwork, WAVs or audio balance to resolve an unexplained difference; report the evidence first.

## Editor preflight

- [ ] Open in Unity 6000.3.8f1 and wait for imports/compilation. No missing scripts or Console compile errors.
- [ ] Run **Window > General > Test Runner > EditMode > Run All**. Record the actual result; resolve failures before treating the demo as release-ready.
- [ ] Save any intentionally accepted Gameplay edits. Do not run R3/R4/R5 Apply, stretch tuning, stability repair or regeneration as part of R6.
- [ ] Run **CatapultCats > Build Windows Portfolio Demo** outside Play Mode. If Windows Build Support is missing, install the matching Unity Hub module and retry.
- [ ] Confirm the Console reports `[CatapultCats R6] Build succeeded`. Investigate build errors and warnings; do not substitute an old executable after a failed build.

The build command reads the saved scene in a temporary preview scene and validates essential components/references, the five ordered levels, catalog and eight accepted clips. It builds only Gameplay for Windows x64, not LevelAuthoring. It applies 1280×720 windowed defaults for the build and restores the Editor's original PlayerSettings in `finally`. It does not save Gameplay or change authored assets. Existing project-foundation tests still check the original Editor defaults.

## Standalone acceptance

Launch `Builds/Windows/CatapultCats.exe` directly from Windows, not through Playtest Current Level. Keep the full Unity output folder together.

- [ ] Fresh launch opens **Level01_FirstShot**, not the last edited/playtested level. The header, cat stock, level art and short tutorial appear correctly.
- [ ] First launch uses a 1280×720, 16:9 window. Confirm no cropped HUD, distorted sprites, unreadable text, camera drift or differences in Warm horizon/background composition. Existing Unity player preferences may affect a previously run player's window; distinguish that from fresh-install defaults.
- [ ] Mouse drag/release launches normally with the accepted four-dot preview. Release inside minimum drag distance cancels without consuming a cat. UI clicks never grab/launch the cat.
- [ ] Hold a drag: one short stretch cue, no loop/restarts. Launch sound happens only on an actual launch, not cancellation.
- [ ] Leave each level untouched for at least 10 seconds. Structures remain acceptably stable, including final user-authored Level05. Repeat the idle check after Retry several times; static geometry does not prove dynamic stability.
- [ ] Wood and glass break at the accepted feel; heavy objects/direct crush behave as accepted. No mass/collider/threshold/timing differences are apparent.
- [ ] Retry while aiming, during a shot and after win/fail reloads the same level with fresh cats and objectives, without old debris or sounds leaking across attempts.
- [ ] Win shows the correct result and Next Level. Failure with mice remaining and exhausted cats shows the retryable fail result; a cancelled drag does not produce either result.
- [ ] Complete the exact order: **First Shot → Collapse → Glass House → Ricochet → Mouse Fortress**. No missing/wrong level, stale objectives or skipped transitions.
- [ ] After the final win, **Play Again** returns to Level01 and a new campaign remains playable. The tutorial's existing one-time-per-session behavior is unchanged.
- [ ] Check all eight sounds: stretch, launch, meaningful impact, wood, glass, mouse defeat, win and fail. Confirm accepted loudness, no continuous loops, repetitive contact spam or excessive overlapping voices.
- [ ] Audio continues through repeated Retry, Next Level and final Play Again. Old voices stop on reload; OBS/game audio capture can hear the accepted cues if recording.
- [ ] HUD buttons work throughout, including after moving focus away and returning. The cat does not react to Retry/result button clicks. Camera and 16:9 presentation remain intact.
- [ ] Close the window / Alt+F4, relaunch and confirm a fresh Level01 campaign. No editor, absolute machine path, missing asset or playtest override is required.
- [ ] Review the Unity Console/build report and standalone `Player.log` for exceptions, missing references, duplicate listeners or other errors. With the current project identity, the player log is normally under `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Catapult Cats\Player.log`.

## Handoff

- [ ] Record pass/fail and reproduction steps for any unexpected standalone difference. A successful build alone is not completed gameplay QA.
- [ ] Check Git status: accepted level assets, Gameplay presentation, prefabs, WAVs, volumes and physics remain unchanged; generated `Builds/Windows/` content is ignored.
- [ ] Capture actual screenshots/video only after standalone QA, then add them at the README's capture insertion location. Do not invent release/download links.
- [ ] Share the complete Windows folder when distributing the demo. Stage/commit/push/tag only when separately authorized by the user; none is part of this implementation task.
