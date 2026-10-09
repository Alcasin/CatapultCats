# R5 — original procedural SFX

## Apply and listen

1. Open Unity 6000.3.8f1 and wait for compilation/import.
2. Outside Play Mode, run **CatapultCats > Apply R5 Audio** once. If Gameplay has unsaved edits, save those accepted edits first and rerun.
3. Expect one `[CatapultCats R5] Apply succeeded` message. Rerunning reuses the manager, six audio sources, listener and clips; it preserves Inspector volume tuning.
4. Play Level01 and Level03. Check soft drag start, cancelled drags (no launch cue), actual launch, substantial impacts, wood/glass breaks, mouse defeat and win/fail.
5. Retry during a shot and after a result several times. Old voices must stop; new pieces must still produce sounds. Click HUD buttons: no stretch sound.
6. Continue through all five levels; after final completion choose Play Again. Audio must continue on Level01. Listen for unwanted impact spam or excessive layering.
7. Report individual sound names that need adjustment. The sounds are not accepted until manual listening QA.

Do not run R3/R4 Apply. No manual component/clip wiring is needed. The narrow R5 command saves only Gameplay, adding an audio-only root; all existing component values, transforms, object names/active states, LevelDefinitions and sequence are checked for preservation before saving.

## Installed-scene stretch polish

For an already-installed R5 scene, wait for compilation and run **CatapultCats > Tune R5 Stretch Audio** outside Play Mode. Do not rerun the full Apply command. This narrow, repeatable command validates the existing stretch clip/event-source wiring and saves only `stretchVolume = 0.40`; all other serialized component values are checked for preservation. A C# default change alone does not migrate the saved scene's old 0.20 value.

Drag the cat and hold for at least half a second before launching: expect one short stretch cue at drag start, not a continuous sound throughout the pull. Cancel a drag, click Retry/UI, then start another drag; check no repeated/UI-triggered stretch and unchanged launch/other effects. Check the recording too. The unchanged waveform is 0.19 seconds; increasing 0.20 to 0.40 adds about 6 dB with the existing 0.35 headroom. Static code/reference review found no broken drag notification or premature drag-time reset. OBS filtering or runtime voice contention cannot be confirmed from static review alone.

## Original assets

All sounds are standard-library procedural synthesis, not recordings, downloads, copyrighted samples or external services. Deterministic seeds make regeneration reproducible.

| File | Duration | Design | Initial cue volume |
| --- | --- | --- | --- |
| SlingshotStretch.wav | 0.19 s | Soft rising elastic tension | 0.40 |
| CatLaunch.wav | 0.32 s | Rounded descending boing and filtered whoosh | 0.55 |
| ImpactThump.wav | 0.18 s | Low-mid transient thump | 0.38 |
| WoodBreak.wav | 0.24 s | Dry filtered crack with woody resonances | 0.48 |
| GlassBreak.wav | 0.42 s | Soft crack and staggered bright tinkles | 0.42 |
| MouseDefeat.wav | 0.20 s | Playful synthetic pop/squeak, no animal recordings | 0.48 |
| LevelWin.wav | 0.66 s | Warm rising three-note cue | 0.55 |
| LevelFail.wav | 0.56 s | Gentle descending two-note cue | 0.48 |

Cue volumes are multiplied by 0.35 for mix headroom. Initial maximum six-voice summation is below full-scale for these clips. Impacts and breaks vary pitch from 0.94–1.06; other cues keep pitch 1. Six reusable nonlooping 2D sources cap polyphony; important cues can replace older lower-priority voices. Win/fail clear earlier voices for clarity. Impact-only gates: normal impulse >= 1.25 and relative speed >= 1, global interval 0.16 s, same collider-pair interval 0.30 s. These gates never affect damage/physics. Collision callbacks are drained after break/defeat notifications so the same contact does not also produce a thump.

## Reproduction and static audit

```powershell
python Tools/generate_r5_sfx.py --check
python Tools/generate_r5_sfx.py
```

The second command reproduces identical existing files without replacing different audio. Only use `--overwrite` intentionally after changing synthesis parameters; WAV metadata/GUIDs remain separate and untouched. PCM is 44.1 kHz mono 16-bit, decompressed-on-load, with normalization disabled. Audit checks non-silence, durations, file sizes, peaks, near-zero DC and zero endpoints. Peaks are 0.40–0.62; combined size is about 239 KiB. The generator needs no Python packages and is outside Unity's asset tree.

Runtime bindings reuse `Launched`, `Broken`, `Defeated` and `StateChanged`. The only existing-script additions are `DragStarted` after a successful drag begins and `Loaded` after level configuration, for exact instance/material binding and reset on Retry/Next Level/Play Again. No gameplay tuning, rules, HUD, flow logic, prefabs, authored data or existing artwork is changed.

Static compilation and WAV checks do not prove runtime listening quality, scene serialization/import success, or event behavior in Play Mode; the manual checks above remain required. No batchmode, staging, commit or push is part of R5 implementation.
