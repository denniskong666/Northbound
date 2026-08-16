# Task 8 report: reliable skippable cinematics

## Delivered

- `CinematicPlayer` now has a `VideoPlayer` adapter, prepare-before-play lifecycle, two-second skip lockout, keyboard (Space/Esc) and mouse skip, idempotent restoration, and an explicit non-saving `Cancel` cleanup path.
- `IVideoPlayback` now reports prepare/decode failures. Playback failure records `LastError`, emits `Failed`, and restores the input lease, gameplay audio, camera, and canvas once whether it occurs before or after prepare.
- Completion persists a prospective narrative state through the injected `SaveGameService` before the `Finished` event. A failed save keeps the completion fact unset, emits `Failed`, restores gameplay, and allows a later replay/retry.
- Bootstrap injects its save service and hosts a cinematic canvas/catalog. Greybridge exposes six unique `CinematicRouteTrigger`s: opening, Maya, Noah, Leo, rooftop, and finale.
- Six silent 1920×1080 H.264 proxy clips remain in their deterministic project slots; the replacement manifest is `docs/cinematics/3d-video-replacement-manifest.md`.

## TDD evidence

1. `Greybridge_EveryCinematicRouteIsReachableThroughThePlayerInteractor` was red with 1 route instead of 6, then green after adding the five missing stable routes.
2. `PlaybackFailure_BeforeOrAfterPrepareRestoresOnceAndRecordsTheError` was red at compilation because playback exposed no failure event or player error state, then green after adding the adapter callback and idempotent cleanup.
3. `Completion_PersistsBeforeFinishedAndSaveFailureFailsClosedWithoutBlockingInput` was red at compilation because `CinematicPlayer.Initialize` accepted no save service, then green after prospective-save-before-finished behavior.
4. The route integration test was updated to call `Cancel()` after verifying each PlayerInteractor route starts the real Bootstrap service. Its RED compile result verified the required non-saving cleanup API; it is green with no default-save writes from the test.

## Deferred intentionally

- Audio mixer snapshot assets/mixer routing are deferred to Task 11. The cinematic asset already carries snapshot fields and cleanup restores the configured gameplay snapshot when those assets exist.
- Git LFS conversion is coordinated by the root agent and is not part of this fix round.
