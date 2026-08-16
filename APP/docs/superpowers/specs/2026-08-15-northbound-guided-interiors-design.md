# Northbound Guided Interiors and Interaction Design

## Objective

Replace the visually confusing single-plane Greybridge presentation with a readable top-down town made of coherent streets, enterable buildings, character-specific interiors, visible mission targets, and responsive minigames. Preserve the approved English narrative, quest order, saves, cinematics, and four ending directions.

## Chosen Architecture

Greybridge remains one Unity scene. The exterior town and each interior occupy separate, non-overlapping map zones inside that scene. A door interaction fades the screen, moves Jamie to the target spawn, updates the camera bounds and location label, then fades back in. Returning through the interior exit restores the exterior spawn and camera bounds.

This avoids scene-loading and save-state complexity while ensuring interior artwork is never laid directly over the street. Only the active location's visual root, colliders, NPCs, objective props, and prompts are enabled.

## World Structure

All gameplay art shares one elevated 3/4 top-down, hand-painted 2D/2.5D camera language. Character proportions, floor perspective, wall angles, facade depth, furniture, shadows, and prop scale must look as though they belong to the same game camera. The supplied CGI images are reference material for identity, layout, palette, and story props only; no raw garage, diner, rooftop, or character reference image is placed directly into the playable world.

### Exterior Greybridge

The exterior contains a continuous road and pavement network, trees, lamps, utility poles, parked vehicles, readable building facades, signs, and doors. Interior plates are removed from the exterior plane. Each destination has a distinct silhouette and entrance:

- Jamie Home
- Vale Auto Garage
- Ruth's Diner
- Maya Studio and Gallery
- Noah Electronics and Recording Room
- Rooftop Stairwell and Overlook

The street remains walkable and supports existing chapter variants, finale choices, and cinematic triggers.

### Character Spaces

- **Jamie Home:** bedroom/living space, packed and unpacked objects, photograph, notebook, house key, old map, and the Things We Leave decision.
- **Elias — Vale Auto Garage:** repair bays, workbench, tool board, station wagon, battery, socket, alternator, belts, toolbox, and garage mission conversations.
- **Maya — Studio/Gallery:** unfinished mural, paintings, crates, hanging points, gallery lamps, exhibition entrance, and Maya mission/farewell conversations.
- **Noah — Electronics/Recording Room:** recorder, radio cases, cable bench, wiring board, headphones, speakers, equipment shelves, and Noah mission/farewell conversations.
- **Leo — Ruth's Diner:** counter, kitchen pass, order pickup area, booths/tables, closing equipment, and Leo mission/farewell conversations.

Rooftop scenes use a dedicated stairwell door and overlook space. Supporting NPCs may share the space required by their authored scene, but each of the five main characters has a stable, identifiable home location.

## Character Animation

Jamie and the four main NPCs use explicit North, South, East, and West source frames rather than displaying a front-facing or rear-facing frame in every direction. All five characters have idle and walk frames for each direction. Whenever any of them moves—player-controlled, scripted, or following a scene path—the visual direction changes immediately when the dominant movement axis changes; walking animation advances while moving and returns to the correct directional idle frame when stopped.

The art catalog stores frame-layout metadata per character so generated sheets are not assumed to share an incorrect column order. The same directional animation component accepts either Rigidbody velocity or scripted movement deltas, allowing NPC scene movement to use exactly the same rule as Jamie. Automated tests verify that every one of the five characters has four distinct idle sprites, four distinct walk sprites, and selects the correct sprite for movement on every axis.

## Doors and Location State

`DoorInteractor` is a normal `[E]` interaction with an explicit destination ID, entry spawn, exit spawn, and prompt. It cannot activate while dialogue, a cinematic, pause, or a minigame holds the global input gate.

`LocationTransitionController` performs:

1. Acquire input.
2. Fade to black.
3. Disable the previous location root.
4. Enable the destination root.
5. Move Jamie to the destination spawn.
6. Update camera bounds and location HUD.
7. Fade in and release input.

Failed or missing destination configuration releases input and restores the current location instead of trapping the player.

## Mission Guidance

The HUD has three persistent, high-resolution elements:

- chapter/day title;
- current objective text;
- next destination or action.

Example:

> FIRST MORNING<br>
> Go to Ruth's Diner<br>
> Enter through the front door and speak to Leo.

Only the next valid mission route or objective displays a pulsing star marker. The marker changes from a building entrance to an NPC or physical object as the mission progresses. It disappears after completion. Optional conversations use a smaller speech marker and never compete visually with the required objective.

The first playable chapter includes short contextual controls: movement, interact, current objective, and pause. Each hint disappears after the player performs the action and is not repeated after saving.

## Physical Object Interaction

Every physical objective has:

- a visible world sprite;
- an interaction collider and `[E]` prompt;
- an active-objective marker when applicable;
- success text and a short pickup/complete animation;
- persistent completion state.

On successful non-minigame interaction, the objective is committed first. The prop then scales/fades out, disables its collider, and remains hidden after save/reload. A rejected or out-of-order interaction leaves the prop visible and displays guidance rather than silently doing nothing.

Large service objects that should remain in the room, such as the station wagon or gallery lights, change visual state instead of disappearing.

## Minigame Upgrade

### Diner Shift

Orders appear as illustrated cards and tables as visible targets. The player selects an order and draws/animates a connection to the matching table. Correct matches glow green and leave the board; incorrect matches shake, turn amber, and explain the mismatch. Keyboard `1–3` and `Q/W/E` use the same interaction path as mouse controls.

### Recorder Wiring

Four cable tiles show real line segments and endpoints. Rotating a tile visibly rotates its path. Connected segments illuminate from source to recorder; reset restores the authored layout.

### Trunk Packing

Items have recognizable silhouettes and dimensions. Grid cells show hover/selection/occupied states. Placement previews green or red before confirmation, and successfully packed items visibly occupy the trunk.

All minigames retain large text, explicit current status, mouse support, reliable rising-edge keyboard input, Escape cancellation, input-gate cleanup, and save-safe quest reporting.

## Data and Save Behavior

Location is not required to persist across sessions; Continue restores the authored chapter spawn to avoid loading Jamie into a disabled interior. Completed quests, selected mission pairs, picked-up props, carried object choice, cinematic completion, and ending facts continue to use the existing narrative save.

New tutorial completion facts and physical-object visual facts use stable IDs and are included in the narrative manifest authority validation.

## Verification

Automated tests must cover:

- four distinct idle and walk facings for each of the five characters, including both player and scripted NPC movement;
- every exterior door and return door destination;
- all five main character spaces and anchors;
- one active required marker at a time;
- tutorial progression and non-repetition;
- every physical objective visual, success disappearance/state change, and reload behavior;
- mouse and keyboard success/error paths for all three minigames;
- no input lease, camera, or location root leak after cancel/error;
- the existing full EditMode and PlayMode suites;
- a fresh macOS build containing only Bootstrap and Greybridge.

Visual review must also reject any exterior/interior plate whose camera horizon, vanishing direction, lighting, texture density, or object scale conflicts with the five character sprites.

Human review remains required for composition, readability at the user's display resolution, route clarity, and final pacing.

## Scope Boundary

This pass creates polished, coherent playable proxy art and interaction behavior. It does not claim final studio-quality frame-by-frame animation, voice acting, or observed 45–60 minute pacing. Those remain production and human-playtest acceptance items.
