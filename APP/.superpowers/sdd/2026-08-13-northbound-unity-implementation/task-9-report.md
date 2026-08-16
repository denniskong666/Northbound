# Task 9 Report — Physical Ending Choices

## Delivered

- Added pure `EndingDirection`, `EndingContext`, and `EndingResolver` contracts. The final physical route remains authoritative; Promise, Connection, and carried-object facts only alter dialogue, staging, lighting, or gesture details.
- Added six authored `EndingAsset` resources: Northbound, Home Chosen, No Map, and individual Not Alone assets for Maya, Noah, and Leo.
- Added `EndingTrigger`: a player must remain inside a route while sustaining its authored keyboard direction for 1.25 seconds. The hold indicator becomes visible at 0.4 seconds; stopping or leaving cancels the hold.
- Replaced the old generic finale trigger zones with six distinct, non-overlapping direction corridors in Greybridge. The finale spawn and one-unit movement in every cardinal direction lie outside every ending collider.
- The runtime routes use the real `PlayerMotor` input and global Bootstrap input gate. Entering the Finale chapter plays the `finale` cinematic exactly once before routes unlock; a physical confirmation then records and presents the selected ending without replaying that clip. Friend routes have no bond lock.

## TDD evidence

1. The initial resolver/asset test suite was RED at compilation because `Northbound.Endings`, `EndingAsset`, and the resolver contracts did not exist. It is green after implementation: focused EditMode `7/7`.
2. The first physical-car-route integration test was RED with the original two-unit circular zone: continued downward movement left the zone before 1.25 seconds, so `LastContext` stayed null. It is green after replacing the six circles with separated route corridors: focused route test `1/1`.
3. The trigger suite verifies the 0.4-second indicator threshold, 1.25-second confirmation threshold, cancellation on leaving/stopping, six scene routes, all friend IDs, spawn-drift safety, normal movement confirmation, and Bootstrap cinematic/InputGate integration: focused PlayMode `7/7`.
4. Full fresh regression results: EditMode `49/49`, PlayMode `50/50`.

## Test isolation and limits

- The full PlayMode run exposed a test-only ordering issue: an earlier persistent Bootstrap blocked a duplicate Bootstrap from reloading Greybridge. The Bootstrap integration test now disposes a prior singleton and waits for the chapter controller, then passes in both isolated and full runs.
- Unity regenerated platform defaults and `SceneTemplateSettings.json` during batch testing; those unrequested settings were removed before the feature commit.
- This task validates automated gameplay paths. It does not claim an observed human playtest, final cinematic production, voice acting, or final end-card visual styling; those production dependencies remain for later tasks.

## Formal review — Fix Round 1

All three verified Important findings are addressed.

1. `GameFlowController` starts the pre-choice `finale` cinematic on entering or restoring the Finale chapter when its completion fact is absent. Finale route colliders remain disabled until `cinematic_finale_complete`; confirming a physical route no longer replays that cinematic. `EndingPresentationController` now displays the chosen context's staging, dialogue moment, carried prop, end card, credits, and Enter/Esc return affordance. The selected ending is saved prospectively before the presentation appears.
2. No Map now distinguishes all carry states: notebook writes the date, photo is held to sunrise, house key unlocks a door, and map (or default) is folded and kept. The resolver test was RED at `2/4` for photo/key and is green at `4/4`.
3. `GreybridgeWorldLayout` owns one scene narrative state and uses the actual `ChapterWorldController.CurrentChapterId` for every route. In direct Greybridge, routes are disabled before Finale and enabled after `Apply("finale", ...)`; Bootstrap additionally requires the pre-choice cinematic completion fact. The new direct-scene regression was RED because pre-Finale routes were available, then green after the shared source and chapter-applied refresh were introduced.

Fresh Fix Round 1 verification: sequence route `1/1`, carry resolver `4/4`, full ending PlayMode suite `8/8`, full EditMode `52/52`, and full PlayMode `51/51`.

## Formal review — Fix Round 2

- Removed the generic `Finale Memory` `CinematicRouteTrigger`. Greybridge now exposes exactly the five optional memory routes (`opening`, `maya`, `noah`, `leo`, and `rooftop`); none has the `finale` cinematic ID.
- The PlayMode sequence regression was written first and failed while six routes existed (`expected 5, actual 6`). It now verifies that no interactable `finale` route exists before Finale, while GameFlow's automatic finale clip plays, after it unlocks the physical routes, or after a repeated Finale entry. Free roam before Finale cannot set `cinematic_finale_complete`; GameFlow remains the only initiator and starts the clip once.
- The endpoint copy now correctly says `Press Enter to close ending`. `ReturnToTitle` still only hides the ending and raises its event; it has no title-scene subscriber or navigation implementation. That endpoint wiring is explicitly deferred to Task 11 and is not considered resolved here.

Fresh Fix Round 2 verification: focused route regression `1/1`; focused automatic-finale sequence `1/1`; full EditMode `52/52`; full PlayMode `51/51`.
