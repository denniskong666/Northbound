# Task 5 Report — Quests and conflicting missions

## Status

COMPLETE

## RED

- The initial focused EditMode run stopped at the expected compiler errors because the `Northbound.Quests` namespace and its quest/pair types did not yet exist.
- A focused PlayMode scene check initially failed because `TestSandbox` was not in the active scene list. Adding that test scene to `EditorBuildSettings` fixed the test harness configuration.
- The first shared-trigger regression was `6/7`: controllers created with the Elias/Maya quest IDs in opposite orders did not observe the same committed fact. The pair key was made order-independent and the committed state is read from the state store.
- The failed-save retry regression was `7/8`: a failed save left the confirmation pending and prevented another attempt. `ConfirmCommitment` now clears the pending choice while retaining no live commitment.
- The TestSandbox interaction regression first failed compilation for the missing scene context, then failed as expected until the scene had its local dialogue/runtime context. A final RED compilation failure for the missing context save service preceded durable scene wiring.
- The Bootstrap-to-TestSandbox gate regression was `4/5`: a persistent Bootstrap could be selected instead of TestSandbox's own gate. The context now selects an InputGate from its own scene. The attempted pre-isolation persistence run was safety-blocked because it would have deleted the user's default save; the sandbox now owns a dedicated test save filename and the durable reload test runs only against that file.

## GREEN

Focused suites:

- `Northbound.Tests.QuestRunnerTests`: `8/8` passed.
- `Northbound.Tests.MissionPairPlayModeTests`: `5/5` passed.

Final regressions with Unity `6000.3.22f1`:

- EditMode: `33/33` passed.
- PlayMode: `13/13` passed.

## Assets and scene

- Added data-driven quest types and runner under `Assets/Northbound/Scripts/Quests`.
- Added authored Elias (`alternator`) and Maya (`first_light`) quest assets, plus `QuestHint.prefab`.
- TestSandbox now contains separated Elias/Maya trigger colliders, a scene-local mission context, and an instantiated existing DialogueView. The context loads/saves through an isolated `northbound-testsandbox-save.json` file, uses TestSandbox's own InputGate even when Bootstrap persists, and binds the neutral confirmation prompt to the same player gate. The DialogueView provides existing keyboard/mouse confirmation and back-out controls only.
- TestSandbox was added to the active build scene list for the runtime PlayMode scene test.

## Durable commitment contract

`TryCommit` first writes a prospective narrative-state copy through `SaveGameService.Save`. It returns `false` and leaves the live state unmodified on failure. Confirmation also clears its pending selection on failure, leaving both missions available for retry. A successful commitment stores the chosen mission fact and the other mission's explicit `missed_*` fact before reporting success; a fresh state load restores the lock.

## Self-review and independent review

- Sequential-only progress, capped/idempotent report handling, completion facts, pair mutual exclusion, reverse-authored pair ordering, reload persistence, save-failure retry, and TestSandbox input-gate interaction are covered by the new tests.
- Independent review found the failed-save pending lock, absent TestSandbox runtime context, a Bootstrap/local InputGate mismatch, and missing standalone durable save service. Each was fixed and covered by the focused/final regressions, including an actual TestSandbox commit/reload lock and the Bootstrap-to-TestSandbox route.
- No gamepad bindings were introduced; confirmation continues through the existing DialogueView's mouse buttons and keyboard controls.

## Commit

`feat: add quests and conflicting missions`

## Concerns

- Unity emitted its existing licensing-module access-token warning while tests ran, but every final test suite completed successfully.
- No manual Game View session was performed; PlayMode validates the authored scene, dialogue start, exact message, local gate block/release, and cancel availability.
