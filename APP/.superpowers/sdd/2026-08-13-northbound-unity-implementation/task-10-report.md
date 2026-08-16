# Task 10 — Complete English Narrative Integration

Status: Fix Round 1 verified.  Base commit: `4ca00e0`.

## Delivered scope

- Added the authoritative `content-manifest.json` and resource catalog: 16 quests, 49 dialogue assets, 35 route triggers, 6 cinematics, 6 ending assets, and 5 primary characters (Jamie, Elias, Maya, Noah, Leo).
- Authored 348 spoken dialogue nodes and seven four-choice Jamie response points. Every tone choice has a distinct NPC response and a recorded state fact. Required missions, optional conversations, missed-state traces, farewells, cinematic connective scenes, and the four ending families are all enumerated and validated.
- Added four NPC prefabs, 35 trigger prefabs, content validation/metrics/path-simulation runtime code, and Greybridge integration.  The runtime graph advances from prologue through finale without a dead end; exclusive pairs are enforced, missed traces are discoverable, and the character-highlighting order is deterministic.
- Added isolated full-path PlayMode tests.  They use a temporary save-service factory and do not delete or mutate the player's normal save data.

The 45–60 minute figure is an authored-content planning estimate (text, interaction, exploration, and cinematic allowance), not an observed human playthrough duration.

## TDD and validation evidence

The implementation began with failing tests for missing content types. Subsequent RED tests exposed: the original 135-node draft being below the required depth, a missing Packed Trunk missed-state marker, pair-ID ordering that prevented a selected Pack Trunk route from resuming, serialized UI components with missing scripts in the three minigame prefabs, and overlap between narrative and legacy triggers. The final implementation adds objective-specific world interactions, canonical pair lookup, a no-missing-script Bootstrap regression, runtime-safe cinematic composition, clean minigame prefabs, idempotent director initialization, and non-overlapping automatic cinematic routes.

- Bootstrap runtime regression: 1/1 passed (`/tmp/task10r1-bootstrap-final.xml`), including CinematicPlayer, MinigameService, and no missing-script component checks.
- Focused full-path suite: 14/14 passed (`/tmp/task10r1-focused-green.xml`).
- Full EditMode suite: 52/52 passed (`/tmp/task10r1-edit-green.xml`).
- Full PlayMode suite: 65/65 passed (`/tmp/task10r1-play-green.xml`).

The PlayMode suite covers both Northbound variants, Home, No Map, and the Maya/Noah/Leo friend variants (seven simulated end states); unique mission-pair selection; missed-state facts; finale reachability; manifest/reference integrity; runtime Greybridge trigger coverage; and Jamie's scene presence.

## Fix Round 1 runtime note

The three authored minigame prefabs no longer serialize stale UI script references; `MinigameController` constructs the currently installed UI helpers when a minigame starts. CinematicCanvas similarly guarantees its Canvas, VideoPlayer, and UI dependencies at runtime. The final PlayMode log contains no missing-script messages. `git diff --check` is clean.

## Fix Round 2 review evidence

- Timed subtitle cues now advance against CinematicPlayer elapsed playback time and clear on cancellation, errors, and skips. Opening has ten cue lines matching its ten approved spoken lines, including Leo's fries question and Noah's statistical answer.
- No Map maps all four carried-object variants to authored ending dialogue assets. Physical carry selection clears the other three carried facts and persists the selected fact.
- One More Table holds Chapter 2 until the required `chapter_two_rooftop` dialogue completes. Greybridge routes now occupy distinct positions and all five runtime character instances have visible renderer proxies.
- Validator coverage includes quest completion facts, cinematic completion facts and ordered cues, and every ending dialogue mapping.
- Round 2 evidence: focused 16/16 (`/tmp/task10r2-green2-fullpath.xml`); timed cue runtime 1/1 (`/tmp/task10r2-cues.xml`); EditMode 52/52 (`/tmp/task10r2-edit.xml`); PlayMode 68/68 (`/tmp/task10r2-play.xml`). The duration figure remains an authored planning estimate, not a human-observed playtime.

## Fix Round 3 cue parity

All six CinematicAssets now generate timed cues directly from their linked cinematic DialogueAssets, so cue text, count, and order cannot drift from approved spoken lines. RED parity evidence is `/tmp/task10r3-red.xml`; GREEN parity evidence is 1/1 at `/tmp/task10r3-green.xml`.

Round 3 final regression: EditMode 52/52 (`/tmp/task10r3-edit-final.xml`) and PlayMode 68/68 (`/tmp/task10r3-play-final.xml`).

Round 4 fact-authority proof: temporary self-authorization produced the expected RED 1/1 (`/tmp/task10r4-selfauth-red.xml`); restored validator GREEN 1/1 (`/tmp/task10r4-current-green.xml`). Final EditMode 52/52 (`/tmp/task10r4-final-edit.xml`) and PlayMode 70/70 (`/tmp/task10r4-final-play.xml`) passed.
