# Northbound Top-Down 2D Art Design

**Status:** Approved direction; implementation pending  
**Date:** 2026-08-14  
**Purpose:** Replace the current functional greybox with a cohesive, readable, production-quality 3/4 top-down 2D presentation without changing the approved narrative or interaction flow.

## 1. Art Direction

Northbound will use a **polished hand-painted 2D style with restrained pixel texture**, viewed from a **3/4 top-down camera**. The readability target is the strong silhouette language of games such as Brawl Stars, while all characters, costumes, locations, props, colors, and iconography remain original to Northbound.

The result must not resemble a prototype made of colored rectangles. Every playable or narrative-critical object must have an authored visual, and locations must communicate their identity before the player reads a label.

### Visual principles

- Large, distinct silhouettes readable at gameplay zoom.
- Simplified but recognizable faces, hair, clothing layers, and signature accessories.
- Warm tungsten interiors against a cool blue industrial town at night.
- Painted material variation: cracked asphalt, brick, worn concrete, metal, glass, wood, vinyl, and puddles.
- Subtle pixel grain and hard-edged details, but no deliberately low-resolution or blocky presentation.
- Soft contact shadows and selective pools of light to anchor characters and props.
- High-contrast interaction highlights that remain visually separate from narrative art.

## 2. Canonical References

The supplied adult character sheets are canonical for the current timeline:

- Jamie: `jamie-model-sheet.png`
- Elias: `elias-model-sheet.png`
- Maya: `maya-model-sheet.png`
- Noah: `noah-model-sheet.png`
- Leo: `leo-model-sheet.png`
- Ensemble proportions and relative height: `adult-cast-lineup.png`

The child lineup is reserved for possible memory or flashback imagery and is not used for the current playable world.

Environment and prop references:

- Ruth's Diner: `ruths-diner.png`
- Vale Auto Garage: `vale-auto-garage.png`
- Rooftop Overlook: `rooftop-overlook.png`
- Blue station wagon: `station-wagon-model-sheet.png`
- The six approved CGI films provide secondary continuity references for lighting, palette, costume color, and emotional tone.

## 3. Camera and Scale

- Orthographic 3/4 top-down presentation, approximately 55–65 degrees downward from horizontal.
- Characters are shown as full-body top-down sprites, not portrait cutouts.
- The head and shoulders remain slightly exaggerated so identity survives at gameplay scale.
- Jamie occupies roughly 8–11% of the vertical game view at the default zoom.
- Environment art uses a consistent 64-pixel modular grid, but may be delivered at 2× resolution for crisp high-DPI display.
- Pixel-perfect snapping is not required; stable filtering and consistent pixels-per-unit are required.

## 4. Character Set

Five adult characters receive matching four-direction sprite sets.

| Character | Required visual identifiers | Silhouette emphasis |
|---|---|---|
| Jamie | short dark curls, tan work jacket, deep green hoodie, black jeans, high-top shoes | compact, layered hood and jacket |
| Elias | tallest build, dark curls, rust work jacket, navy shirt, heavy work boots | broad shoulders and long stance |
| Maya | black bob with bangs, painted denim jacket, mustard sweater, wide dark trousers | squared jacket and clean bob |
| Noah | glasses, headphones, burgundy hoodie, recorder at hip, dark cargo trousers | headphones and narrow posture |
| Leo | dense curls, faded red short-sleeve shirt, white tee, blue jeans | open shirt and relaxed stance |

### Animation minimum

- Four directional idle poses: north, south, east, west.
- Four directional walk cycles with at least four frames per direction.
- Jamie uses the full animated set during play.
- NPCs may initially use directional idle plus a restrained two-frame breathing motion, but their full walk frames remain authored and replaceable.
- Animation state changes must derive from existing movement direction and must not alter physics, input, collision, or quest logic.

## 5. Vehicle and Props

The blue station wagon receives four directional views plus an open-trunk state. Its worn blue paint, roof rack, black bumpers, square lights, and age marks must remain recognizable.

Critical authored props include:

- Garage lift, tool benches, pegboard, cabinets, work lights, tires, boxes, and scattered parts.
- Diner counter, red booths, stools, checker floor, tables, coffee equipment, hanging lights, and service door.
- Rooftop brick parapet, access door, utility pipes, folding chair, paper map, chalk marks, puddles, and city-light edge treatment.
- Street lamps, storefronts, sidewalk seams, road markings, dumpsters, posters, fences, utility poles, and parked vehicles.
- Quest-object visuals for the socket, battery, fan belt, fuses, toolbox, paintings, gallery lights, alternator, recorder, radio case, photograph, notebook, house key, and old map.

## 6. Greybridge World Presentation

The existing logical map bounds and trigger coordinates remain authoritative. Visual art is layered around those coordinates:

1. Ground layer: asphalt, sidewalks, alleys, rooftop membrane, interior floors.
2. Structural layer: buildings, walls, counters, booths, garage equipment, parapets.
3. Prop layer: vehicles, tools, furniture, posters, clutter, quest objects.
4. Character layer: Jamie and four NPCs with contact shadows.
5. Lighting layer: cool ambient tint, warm local lights, reflections and selective emissive windows.
6. Interaction layer: prompts, outlines and mission feedback, always above world art.

Primary locations must be recognizable without text:

- **Vale Auto Garage:** warm workshop light, blue wagon, dense tool wall, worn concrete.
- **Ruth's Diner:** amber lights, red vinyl booths, checker floor, long counter.
- **Rooftop Overlook:** dark wet roof, brick parapet, lone chair/map, industrial skyline.
- **Old Neighborhood and streets:** cool blue streets, brick storefronts, pools of lamplight, shutdown traces that visibly change by chapter.
- **Maya/Noah/Leo mission areas:** distinctive art, radio/electronics equipment, exhibition elements, and diner-side props rather than generic colored markers.

## 7. Runtime Integration

- Replace `CreatePrimitive(PrimitiveType.Quad)` proxy visuals with SpriteRenderer-based authored art or composed prefab visuals.
- Preserve existing object names, transforms, colliders, triggers, chapter facts, save IDs, interaction interfaces, and ending-zone logic.
- Character prefabs remain the stable prefab identities used by the narrative catalog.
- Rendering order is deterministic: ground < structures < props < characters < effects < interaction UI.
- Sprite import settings, pixels-per-unit, filtering, compression, pivots, and animation clips are standardized through an editor importer or explicit metadata.
- If an art asset is unavailable, the runtime may show a deliberate illustrated placeholder bearing the correct silhouette; it must never fall back to a plain colored square.

## 8. Acceptance Criteria

The art pass is accepted automatically when:

- All five named characters have distinct non-primitive sprites matching their reference identifiers.
- Jamie visibly changes facing and walk animation in all four movement directions.
- Garage, diner, rooftop, street, and mission areas contain textured authored ground and props.
- The station wagon and required quest objects have recognizable visuals.
- No visible world object uses the old untextured primitive proxy renderer.
- Existing collision, traversal, dialogue, mission, cinematic, ending, menu, save, and settings tests remain green.
- A PlayMode visual-smoke test proves every required art catalog entry resolves and every primary area instantiates its visual root.
- A representative 1920×1080 gameplay screenshot is captured for Garage, Diner, Rooftop, and Street.

Human review remains required for:

- Final likeness approval of the five characters.
- Readability and emotional tone at the user's actual monitor size.
- CGI-to-game costume, prop, and lighting continuity.
- Subjective polish comparison against the requested commercial-quality reference bar.

## 9. Scope Boundaries

- This pass does not redesign the story, missions, controls, map topology, or ending logic.
- It does not copy Brawl Stars characters, maps, textures, UI, or proprietary assets.
- It does not convert the project into full 3D gameplay.
- It does not require final voice acting, final music, or new CGI.
- Adult cast is canonical for playable present-day scenes; child cast remains a future replaceable memory-art slot.

