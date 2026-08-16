# Northbound morning playtest script

Open `Builds/macOS/Northbound.app`, choose **New Game**, and use only keyboard/mouse.

1. Confirm the upper-left guidance always has three clear lines: `GO TO` for the current location, `NOW` for the current objective, and `MOVE`/`ENTER`/`NEXT` for the exact next keyboard action. Follow only its single gold marker.
2. Walk Jamie in all four directions and confirm the sprite visibly faces north, south, east, and west instead of always showing one direction.
3. Follow the marker to a building door, press `E`, and confirm the fade leads to a separate full-screen interior. In each room, walk against all four edges: Jamie must not cross the room art or expose blue/empty space beyond it. Enter and leave the garage, diner, Maya's studio, Noah's electronics room, rooftop, and Jamie's home.
4. Speak to Elias, Maya, Noah, and Leo. Check that all five characters have distinct, readable visuals and that every nearby interaction shows a large `[E]` prompt.
5. Start a mission at its visible marker. Pick up a required world prop with `E`; it must disappear only after successful collection and remain absent after leaving/re-entering or continuing the save.
6. At the diner, select an order and deliver it to its matching table; verify the selected link and delivered order disappearance. In Wiring, rotate the visible path until source and recorder connect. In Trunk, place at least three objects into visible grid cells and confirm the load.
7. Open pause with Escape, test Settings, then resume. Watch and skip one cinematic only after its Skip prompt appears.
8. Complete one mutually exclusive mission pair and reach the finale. Hold a final direction only deliberately; do not treat this short inspection as a full 45–60 minute timing study.

Record any unclear text, mismatched perspective, absent prompt, unreachable prop, or input that produces no response in `playtest-results.md`.
