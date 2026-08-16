# Northbound playtest results

## Automated evidence

- Full regression pass: EditMode 141/141 and PlayMode 170/170 passed on 2026-08-16 with zero failures, inconclusive results, or skips.
- Release acceptance checks passed 2/2. They verify seven registered locations with one active root, 35+ narrative routes and 13+ objective interactions placed inside a location, and Elias/Maya/Noah/Leo remaining interactable.
- Automated coverage includes five directional character visuals, rooftop cast sprites, persistent disappearing objective props, exact `[E / Enter]` objective copy, enlarged gold pickup framing, six exterior/interior door pairs, Noah's Chapter 4 farewell interaction, mid-quest save restoration, cumulative choice tendencies that can close the opposite finale route, four possible ending paths with distinct route signs and presentation themes, simplified trunk choices, diner/wiring feedback, six cinematic routes, menus, audio feedback, and input restoration.
- The final macOS release build completed successfully after those regressions at `Builds/macOS/Northbound.app`; it is a 637 MB universal arm64/x86_64 application and passed deep signature verification.

## Built-app smoke observation

The 2026-08-16 release build was opened outside the Unity Editor. The observed path covered the Chinese title/HUD, opening-video playback and skip, movement tutorial, the diner-door `[E / 回车]` prompt, entering with Enter, the web-style typewriter dialogue, Down/Enter choice navigation and a branch-specific reply, and ESC pause. `Player.log` contained no exception, error, assertion, missing-reference, or null-reference signatures. The pre-check save was restored byte-for-byte after the temporary New Game run.

## Human observation still required

Fill this after the morning build playthrough. Do not infer these results from automated tests.

| Check | Result | Notes |
| --- | --- | --- |
| First-time route is understandable | Agent smoke pass | Three-step HUD led from movement to the diner door and objective. Independent-player confirmation remains pending. |
| Text is readable at your display resolution | Agent smoke pass | Chinese HUD, pause menu, minigame, dialogue, and three choices were readable at 1224x768. Other displays remain pending. |
| Every intended prompt is reachable | Partial | Diner exterior entrance and first objective were reached in the built app; all route contracts pass automated coverage. |
| Diner, wiring, and trunk controls feel responsive | Pending | |
| Exterior/interior three-quarter art feels visually coherent | Agent smoke pass | Street-to-diner transition and fixed-bottom dialogue composition were inspected. Independent art review remains pending. |
| Pickup disappearance and audio/visual feedback feel clear | Pending | |
| Cinematic/video continuity is acceptable | Pending | |
| Full 45–60 minute pacing | Pending | |
| Three independent players understand the theme and route | Pending | |
