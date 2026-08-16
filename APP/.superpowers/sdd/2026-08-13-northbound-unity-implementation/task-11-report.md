# Task 11 report

## Outcome

Task 11 now uses visible, keyboard-focused authored Title/Pause/Settings/Credits prefabs through Bootstrap, exposes and persists every required setting, applies the four mixer volumes and distinct runtime snapshots, routes dialogue reaction and cinematic audio through Voice, applies shared subtitle settings, makes reduced motion observable through camera snapping, scales ending hold time, supports the actual Escape pause/resume path, and treats Title as a real session boundary. Title intentionally owns the global input lease and prevents gameplay, chapter, and cinematic progress until New Game or Continue. New Game rebuilds a fresh prologue session; Continue rebuilds from the latest disk save; credits/endings return to Title without a frozen clock or leaked pause/dialogue/cinematic lease.

The six supplied 3D CGI replacements remain in stable proxy-named Unity slots to preserve GUIDs. They are technically validated and stored through Git LFS. Human continuity and English voice-content approval remain external and are not claimed.

## RED / GREEN evidence

Existing committed baseline on `09de2ae`:

- SettingsModel EditMode: 4/4 passed at `/private/tmp/task11-completion-baseline-settings3.xml`.
- Existing MenuFlow PlayMode: 4/4 passed at `/private/tmp/task11-completion-baseline-menu.xml`.

Completion TDD cycles:

- Authored prefab structure/settings: RED 0/2 at `/private/tmp/task11-completion-menu-red.xml` (controls/controller absent); GREEN 2/2 at `/private/tmp/task11-completion-menu-green2.xml`.
- Bootstrap/menu/credits/ending/settings integration: RED 4/7 at `/private/tmp/task11-completion-menuflow-red.xml`; GREEN 8/8 at `/private/tmp/task11-completion-menuflow-green2.xml` after adding the runtime settings case.
- Escape keyboard path: repeated diagnostics proved queued key state changed while Unity batch did not advance `wasPressedThisFrame`/InputAction phase reliably. Production now uses the real keyboard pressed state plus an internal rising-edge latch. GREEN 1/1 at `/private/tmp/task11-completion-keyboard-green11.xml`.
- Escape/input ownership self-review: RED 8/10 at `/private/tmp/task11-completion-final-gaps-red.xml` proved Escape could pause while a cinematic/dialogue already owned InputGate; the pause guard now rejects a blocked gate. Together with actual dialogue/cinematic subtitle-background creation, GREEN 10/10 at `/private/tmp/task11-completion-final-gaps-green.xml`.
- Cinematic subtitle visibility: RED 0/1 at `/private/tmp/task11-completion-subtitle-hide-red.xml` proved disabling subtitles left an empty background panel; GREEN 1/1 at `/private/tmp/task11-completion-subtitle-hide-green.xml`.
- Authored-panel visibility: isolated RED 0/1 at `/private/tmp/task11-completion-menu-visibility-red2.xml` proved the real Pause button inherited the hidden Title CanvasGroup alpha; child panel groups now explicitly ignore that parent group, and the real Pause→Settings→Title→Credits button path is GREEN 1/1 at `/private/tmp/task11-completion-menu-visibility-green.xml`.
- Final independent review hardening: prefab-scale RED 0/1 at `/private/tmp/task11-completion-review-menu-red.xml` proved generated root RectTransforms were zero-scale; GREEN 1/1 at `/private/tmp/task11-completion-review-menu-green.xml`. Focus/reset RED 9/11 at `/private/tmp/task11-completion-review-flow-red2.xml` proved no initial keyboard selection and stale in-memory narrative after New Game; GREEN 11/11 at `/private/tmp/task11-completion-review-flow-green.xml` after selecting the first real control on each panel and resetting the shared NarrativeStateStore on confirmed New Game.
- Ending interaction multiplier: RED 0/1 at `/private/tmp/task11-completion-interaction-red.xml`; GREEN 1/1 at `/private/tmp/task11-completion-interaction-green.xml`.
- Reduced-motion camera effect: RED 0/1 at `/private/tmp/task11-completion-motion-red.xml`; GREEN 1/1 at `/private/tmp/task11-completion-motion-green.xml`.
- Cinematic mixer snapshot references: RED 1/2 at `/private/tmp/task11-completion-snapshots-red.xml`; GREEN 2/2 at `/private/tmp/task11-completion-snapshots-green.xml`.
- Distinct and audible mixer states: RED 0/1 at `/private/tmp/task11-completion-mixer-values-red.xml` proved all snapshots had empty value maps; GREEN 1/1 at `/private/tmp/task11-completion-mixer-values-green2.xml` after authoring distinct Normal/Cinematic/Pause values. Real PauseController mix RED 0/1 at `/private/tmp/task11-completion-pause-mix-red.xml`; GREEN 1/1 at `/private/tmp/task11-completion-pause-mix-green.xml` proves Pause attenuates Music by 12 dB and SFX by 18 dB, preserves Master/Voice, and Title/Resume restores the exact user mix.
- Dialogue Voice routing: RED 0/1 at `/private/tmp/task11-completion-voice-route-red.xml`; combined audio GREEN 3/3 at `/private/tmp/task11-completion-audio-green.xml`.
- Media GUID/dimension/frame-rate/duration characterization: 6/6 at `/private/tmp/task11-completion-media-contract.xml`.

Settings controls validated in the real Bootstrap PlayMode path:

- Master/Music/SFX/Voice volume sliders persist and apply exposed mixer parameters.
- Subtitle scale and background opacity persist on the shared SettingsModel consumed by DialogueView and RenderTextureHost through SubtitleView.
- Reduced motion, minigame skip, and interaction-time multiplier persist on the same live model used by camera/minigame/ending runtime services.
- Safe clamps remain volume/opacity `0–1`, subtitle scale `.75–1.5`, and interaction multiplier `.5–1.5`.

## Full verification

Unity: `/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app`, version `6000.3.22f1`.

Reliable test shape (no trailing `-quit` and no `-nographics`):

```sh
/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode \
  -testResults /private/tmp/task11-review-round2-final2-edit.xml \
  -logFile /private/tmp/task11-review-round2-final2-edit.log

/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD" -runTests -testPlatform PlayMode \
  -testResults /private/tmp/task11-review-round2-final2-play.xml \
  -logFile /private/tmp/task11-review-round2-final2-play.log
```

- Final fresh EditMode after Formal Review Fix Round 2 and review polish: 69/69 passed, zero failed/inconclusive/skipped; `/private/tmp/task11-review-round2-final2-edit.xml` and `.log`.
- Final fresh PlayMode after Formal Review Fix Round 2 and review polish: 91/91 passed, zero failed/inconclusive/skipped; `/private/tmp/task11-review-round2-final2-play.xml` and `.log`.
- Both test processes exited themselves with code 0 after writing complete XML; no PID required manual termination.
- Logs were inspected. Compilation error counts are zero. Unity logs a non-blocking access-token refresh error after entitlement resolution plus duplicate-hint IL post-processing warnings; neither affects the completed tests.

Licensing/process investigation:

- First sandboxed attempt: Unity PID 72897 / LicensingClient PID 72905, `/private/tmp/task11-completion-baseline-settings.log`; no XML, licensing channel timed out. Exact processes terminated.
- Second sandboxed attempt: Unity PID 73017 / LicensingClient PID 73024, `/private/tmp/task11-completion-baseline-settings2.log`; same reproduced timeout. Exact processes terminated.
- Running Unity outside sandbox IPC restrictions resolved licensing; all later runs initialized normally.
- One temporary attempt to reference `Unity.InputSystem.TestFramework` triggered a native Burst compiler crash (`/private/tmp/task11-completion-keyboard-green6.log`). That test-only reference was reverted immediately, its crash artifact was removed, and all subsequent focused/full runs completed normally.

## Formal Review Fix Round 1

The formal review correctly identified four integration gaps that structural menu/audio tests did not expose:

- Title/session lifecycle: RED 12/16 at `/private/tmp/task11-review-round1-lifecycle-red.xml` reproduced all four new failures: opening/prologue progress behind Title, stale New Game runtime state, non-refreshing/non-loading Continue, and the absent stateful confirmation panel. GREEN 16/16 at `/private/tmp/task11-review-round1-lifecycle-green2.xml`. `GameBootstrap` now keeps the session inactive while Title owns input, asynchronously reloads Greybridge at each session boundary, cancels dialogue/cinematic/ending/minigame state, preserves the shared state-store identity while replacing its data, and explicitly starts a fresh prologue/opening or restores the saved chapter.
- Authored confirmation: prefab RED 0/1 at `/private/tmp/task11-review-round1-confirmation-prefab-red.xml`; GREEN 1/1 at `/private/tmp/task11-review-round1-confirmation-prefab-green.xml`. `TitleMenu.prefab` now contains an initially inactive New Game confirmation panel with real Confirm/Cancel buttons. Request focuses Confirm, Escape/Cancel never deletes, cancellation restores New Game focus, and confirmation alone performs the session reset.
- Video audio import/routing: importer RED 0/6 at `/private/tmp/task11-review-round1-media-audio-red.xml`; GREEN 6/6 at `/private/tmp/task11-review-round1-media-audio-green.xml`. All six preserved `.meta` files now enable audio import. Bootstrap configures the runtime `VideoPlayer` for one enabled AudioSource track routed to `Master/Voice`; the real Bootstrap test asserts the live component and mixer-group path.
- Exact mixer behavior: exact-name RED 2/4 at `/private/tmp/task11-review-round1-mixer-name-red.xml`; GREEN 4/4 at `/private/tmp/task11-review-round1-mixer-name-green.xml`. The normal snapshot is exactly `Normal`. Live cinematic mix RED 0/1 at `/private/tmp/task11-review-round1-cinematic-mix-red.xml`; after applying cinematic attenuation on top of captured user base volumes, GREEN 1/1 at `/private/tmp/task11-review-round1-cinematic-mix-green3.xml` and full CinematicPlayer GREEN 11/11 at `/private/tmp/task11-review-round1-cinematic-player-green.xml`. Cinematic presentation preserves Master/Voice, attenuates Music by 6 dB and SFX by 12 dB, and restores all four captured values exactly on skip or playback error. Pause independently preserves Master/Voice, attenuates Music by 12 dB and SFX by 18 dB, and returns to `Normal` on Resume/Title.
- The first fresh full PlayMode run was intentionally retained as regression evidence: `/private/tmp/task11-review-round1-full-play.xml` was 87/89 because two older smoke tests assumed Bootstrap immediately started gameplay. Those tests now traverse real New Game and Confirm buttons; the gate-isolation test also explicitly clears the legitimate opening-cinematic lease before measuring its own lease. That checkpoint became 89/89 before the two independent-review regressions raised the final suite total to 91.
- Independent review found that the authored Pause Return-to-Title button still called the menu-only `ShowTitle`, a focused Cancel submit could be overridden by the controller's global confirmation callback, and Settings Apply while paused replaced attenuation and was later overwritten by the stale captured base. RED `/private/tmp/task11-review-round1-review-red2.xml` was 16/18 for the session-boundary and paused-volume cases; direct selected-Cancel callback RED `/private/tmp/task11-review-round1-cancel-red.xml` was 0/1. GREEN `/private/tmp/task11-review-round1-review-green.xml` is 18/18. Pause/Credits Return now route through Bootstrap's complete session cleanup, dialogue session reset removes pending completion delegates, confirmation only runs when Confirm is selected, and Settings Apply refreshes/reapplies the paused effective mix before exact base restore.
- The independent follow-up review found no remaining Critical or Important code issue. Its only staging concern was Unity/test-generated scene, SceneTemplate, and crash-report noise; those exact untracked artifacts were removed before commit.

Systematic Unity process evidence during this round: sandboxed PID 143 spawned LicensingClient PID 150 and stalled at the read-only licensing database. A retry PID 262 then lost its client channel while LicensingClient PID 328 exited. Only those exact task-owned processes were targeted. One independent-review RED attempt crashed in the native Burst linker before tests and produced no XML (`/private/tmp/task11-review-round1-review-red.log`); its identical clean retry produced the expected RED XML above. All final focused/full runs initialized and exited normally with complete XML.

## Formal Review Fix Round 2

- Mixer topology RED: the finalized Unity Editor/reflection contract was 4/5 at `/private/tmp/task11-review-round2-mixer-red4.xml`. Master passed, while `Master/Music` failed first because it owned zero attenuation effects; SFX and Voice had the same empty serialized effect topology. The contract asserts each of Master/Music/SFX/Voice owns exactly one `AudioMixerEffectController` named `Attenuation`, and compares every exposed parameter GUID against that group's real internal `GetGUIDForVolume()` result. Earlier diagnostic attempts were not counted as RED because they revealed and corrected two test assumptions: `FindMatchingGroups("Master")` returns the group tree, and Unity 6 serializes mixer GUIDs as an internal Generic GUID rather than public `Hash128`.
- Supported asset repair: a temporary, subsequently removed Editor script used Unity 6's own internal editor API through reflection: `AudioMixerEffectController("Attenuation")`, `AudioMixerController.AddNewSubAsset`, and `AudioMixerGroupController.InsertEffect`. `/private/tmp/task11-review-round2-mixer-repair.log` records one added attenuation unit for each of Music, SFX, and Voice. The existing mixer asset GUID, group volume GUIDs, exposed parameter GUIDs, snapshot file IDs, and cinematic references were preserved; no hand-authored effect YAML was used.
- Focused GREEN: AudioMixer EditMode 5/5 at `/private/tmp/task11-review-round2-mixer-green2.xml`; CinematicPlayer PlayMode 11/11 at `/private/tmp/task11-review-round2-cinematic-final.xml`; MenuFlow PlayMode 18/18 at `/private/tmp/task11-review-round2-menuflow-green.xml`. The live Bootstrap cinematic test now also proves `AudioSource` routing to `Master/Voice` and independent runtime Set/Get for MasterVolume, MusicVolume, SFXVolume, and VoiceVolume. Together with the editor binding contract, these values now address real per-group attenuation units.
- Independent Round 2 review approved the change with no Critical or Important finding. It verified four unique effect subassets, unchanged mixer/group/snapshot/cinematic GUID references, and genuine RED/GREEN coverage. Its only Minor note was test isolation after runtime SetFloat; the test now captures and restores all four original mixer values in `finally`, followed by the final focused/full runs above.

## Media, shot list, and LFS evidence

- User source shot list was copied byte-for-byte before editing: both source and project copy had SHA-256 `2df388e1b6d293f8656d56b81b60cb5adcdc5e6a85d7bdd9241b5e525634a21c`.
- The project copy then received only documented alignment corrections: N05 is NEW with NOAH + JAMIE, N06 extends N05, all six videos are used, no ending videos are generated, and the global restriction says `no unintended background people`.
- `docs/production/asset-manifest.md` now distinguishes current files from reserved final slots and records dimensions/pivots/PPU/sorting/costume-frame status/audio peaks/codecs/durations/owners plus detailed continuity criteria.
- Unity media contract checks verify all six stable GUIDs, 1920×1080 import dimensions, 30 fps, and duration windows.
- Prior AVFoundation evidence remains: H.264/AVC video, AAC stereo at 48 kHz; Opening 49.900s, Maya/Noah/Leo 59.900s, Rooftop 74.900s, Finale 44.900s.
- `git lfs ls-files` lists exactly the six cinematic payloads. For every path, `git check-attr filter` returns `lfs` and `git cat-file -p :<path>` begins with `version https://git-lfs.github.com/spec/v1`.
- Indexed LFS OIDs: Opening `4aeac3dceb…`, Maya `e1ef369117…`, Noah `4e27242534…`, Leo `3543cdd037…`, Rooftop `0358423c72…`, Finale `7258dbbfe5…`.

## Remaining limitations / concerns

- Human creative continuity review is still required for Jamie, Elias, Maya, Noah, and Leo. Owner-held model-sheet image files named by the shot list are not present in the tracked project, so the manifest does not fabricate approval.
- AAC track presence does not establish that English voice content is present or approved.
- Final PNG gameplay/portrait/environment art and 48 kHz WAV music/SFX/voice masters are reserved delivery slots and remain absent; current runtime placeholders are preserved honestly.
