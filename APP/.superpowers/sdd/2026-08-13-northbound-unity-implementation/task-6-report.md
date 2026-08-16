# Task 6 Report — Chapter Flow and Changing Greybridge World

## Scope delivered

- Added data-driven `ChapterVariant` assets for Prologue, Chapters 1–4 (including the two Chapter 3 days), and Finale.
- Added `ChapterWorldController`, `WorldFactBinding.Refresh(NarrativeState)`, and `GameFlowController.EnterChapter(string)` with save-before-state-application semantics and reload/respawn support.
- Wired the persistent `Greybridge World` scene root to `GreybridgeWorldLayout`, `ChapterWorldController`, and `GameFlowController`, with all seven chapter assets serialized into the scene.
- Built a connected runtime greybox for Old Neighborhood, Vale Auto Garage, Rooftop Overlook, connecting walkways, spawn points, mission zones, finale regions, world boundary colliders, a keyboard-controlled Jamie proxy, and camera bounds.
- Added fact-controlled physical missed-state traces for Maya, Noah, and Elias mission alternatives.

## TDD evidence

### RED

1. `ChapterWorldControllerTests` was written before the runtime contracts. The focused EditMode run failed to compile because `Northbound.World`, `ChapterWorldController`, `GameFlowController`, and `ChapterVariant` did not exist (`/private/tmp/northbound-task6-red.log`, compiler lines 730–734).
2. `GreybridgeWorldPlayModeTests` was added before the map layout. Its focused PlayMode run failed to compile because `GreybridgeWorldLayout` did not exist (`/private/tmp/northbound-task6-map-red2.log`, line 718).
3. The path-distance/trap test was added before the route API. Its focused PlayMode run failed because `HasClearWalkablePath` and `GetWalkingSeconds` did not exist (`/private/tmp/northbound-task6-path-red.log`, lines 708–709).
4. A save-boundary regression test proved that entering a new chapter without first restoring the old chapter left two current-chapter facts. It failed as intended at `6/7` (`/private/tmp/northbound-task6-replace-red.xml`, lines 65–74); entering now clears every known chapter fact before saving the replacement state.

An intermediate map GREEN attempt exposed a real runtime setup error: Unity primitives still had a `MeshCollider` when a `BoxCollider2D` was added. The PlayMode test failed with the relevant `NullReferenceException`; the layout now removes that component immediately before adding 2D collider geometry.

### GREEN and full regression

All commands omitted `-quit` and `-nographics`; Unity Hub stayed closed.

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode \
  -testFilter Northbound.Tests.ChapterWorldControllerTests \
  -testResults /private/tmp/northbound-task6-final-focused-edit.xml \
  -logFile /private/tmp/northbound-task6-final-focused-edit.log
```

Result: `total="7" passed="7" failed="0"` (`/private/tmp/northbound-task6-replace-green.xml`).

```bash
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode \
  -testResults /private/tmp/northbound-task6-final2-all-edit.xml \
  -logFile /private/tmp/northbound-task6-final2-all-edit.log
```

Result: `total="40" passed="40" failed="0"`.

```bash
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform PlayMode \
  -testResults /private/tmp/northbound-task6-final2-all-play.xml \
  -logFile /private/tmp/northbound-task6-final2-all-play.log
```

Result: `total="15" passed="15" failed="0"`.

The focused map suite also passed `2/2`: it loads Greybridge from the actual scene asset, validates the three map locations, all chapter/finale state objects, 2D collider/spawn/mission wiring, and all 35 spawn-to-mission straight-line traversal checks at the authored keyboard walking speed under 45 seconds.

## Known limits / follow-up ownership

- The current greybox is intentionally simple: geometry, collision, chapter state, and camera containment are present, but later visual-art work should replace the generated quads with painted environment assets without changing the stable object IDs.
- Chapter starting quest IDs are exposed by `ChapterWorldController.CurrentStartingQuestIds`; Task 10 should attach these IDs to the authored complete quest graph.
- The temporary Unity-generated `ProjectSettings` platform defaults and `SceneTemplateSettings.json` were removed after testing, so this commit contains only Task 6 source, data, scene, test, and report files.

## Formal review — Fix Round 1

All four verified Important findings from formal review are addressed in a separate follow-up commit.

1. `ChapterWorldController.Apply` now deactivates the union of every configured variant's activate/deactivate references before applying exactly the selected variant. The regression starts in Chapter 4, then moves to Chapter 3; it failed with the prior-only storefront still active and now passes.
2. `ChapterWorldController` has explicit `BindNarrativeState` / `UnbindNarrativeState` lifecycle methods, subscribes to `NarrativeStateStore.Changed`, and unsubscribes on destruction. `GameFlowController.Initialize` binds the live state. A fact binding now changes immediately after `state.Set` without a second chapter Apply.
3. The runtime-built Jamie proxy receives `GameBootstrap.Instance.InputGate`. An actual Bootstrap→Greybridge PlayMode test acquires the persistent gate, confirms keyboard movement stops, releases it, then confirms keyboard movement resumes. The isolated behavioral RED caught the original missing injection (`-6.0` to `-5.92` while blocked).
4. The Greybridge scene now serializes stable character/location anchor definitions for Jamie, Elias, Maya, Noah, and Leo; runtime `GreybridgeNpcAnchor` components expose the IDs to authored interactions.

Fix-round commands used the same Unity invocation rules. Focused EditMode tests passed `9/9` after fixes; focused Greybridge PlayMode tests passed `4/4`; final full suites passed `total="42" passed="42" failed="0"` in EditMode (`/private/tmp/northbound-task6-fix1-all-edit.xml`) and `total="17" passed="17" failed="0"` in PlayMode (`/private/tmp/northbound-task6-fix1-all-play.xml`).
