# Northbound production asset manifest

This manifest defines replacement and delivery contracts. It does not claim that absent art/audio masters exist or that a human has approved creative continuity. At Task 11 close, the only checked-in production media payloads are the six technically validated 3D CGI videos in stable `*_proxy.mp4` slots. Current gameplay characters/environments remain prefab/procedural placeholders; no final PNG or WAV masters are checked in.

## Global import contracts

- Gameplay sprites: transparent PNG, 100 pixels per unit, bottom-centre pivot `(0.5, 0)`, `Characters` or documented environment sorting layer. Frame counts remain `TBD` until animation sheets are delivered; the importer must not infer or fabricate frames.
- Portraits: 1024×1024 transparent PNG, centred pivot `(0.5, 0.5)`, UI-only (no pixels-per-unit or sorting-layer dependency).
- Music/SFX/voice masters: 48 kHz, 24-bit WAV. Peak targets below are delivery ceilings; level balancing is non-destructive through `NorthboundMixer`, never by rewriting source gain.
- Cinematics: 1920×1080, 30 fps, 8-bit progressive H.264/AVC video with stereo AAC at 48 kHz. English subtitles live separately in Unity cinematic/dialogue data.
- Replacement workflow: overwrite the binary payload at the stable project filename, retain its `.meta` file and GUID, reimport, validate this manifest, then verify the Git LFS pointer in the index.

## Gameplay art delivery slots

These are reserved final slots. The referenced model sheets are shot-list authorities but are not present in this branch, so costume/hair specifics require the owner-supplied sheets before approval.

| Slot / final filename | Dimensions / pivot / PPU | Sorting | Costume and frame contract | Owner/status |
| --- | --- | --- | --- | --- |
| Jamie gameplay — `Art/Characters/Jamie/jamie_gameplay.png` | Sheet dimensions TBD; `(0.5,0)`; 100 PPU | `Characters` | Adult Greybridge outfit must match `jamie-model-sheet.png`; idle/walk frame count TBD | Character art / reserved, file absent |
| Elias gameplay — `Art/Characters/Elias/elias_gameplay.png` | Sheet dimensions TBD; `(0.5,0)`; 100 PPU | `Characters` | Mechanic/work outfit and grease-marked hands must match `elias-model-sheet.png`; frames TBD | Character art / reserved, file absent |
| Maya gameplay — `Art/Characters/Maya/maya_gameplay.png` | Sheet dimensions TBD; `(0.5,0)`; 100 PPU | `Characters` | Paint-working outfit must match `maya-model-sheet.png`; frames TBD | Character art / reserved, file absent |
| Noah gameplay — `Art/Characters/Noah/noah_gameplay.png` | Sheet dimensions TBD; `(0.5,0)`; 100 PPU | `Characters` | Outfit/headphones must match `noah-model-sheet.png`; frames TBD | Character art / reserved, file absent |
| Leo gameplay — `Art/Characters/Leo/leo_gameplay.png` | Sheet dimensions TBD; `(0.5,0)`; 100 PPU | `Characters` | Diner/work outfit must match `leo-model-sheet.png`; frames TBD | Character art / reserved, file absent |
| Five character portraits — `Art/Portraits/{jamie,elias,maya,noah,leo}.png` | 1024×1024 each; `(0.5,0.5)`; UI | UI | Same adult hair, face, palette, proportions, and costume as gameplay/model sheet; single frame each | Character art / reserved, files absent |
| Greybridge environment — `Art/Environment/greybridge_tiles.png` | Sheet dimensions and frames TBD; tile pivot per tile; 100 PPU | `Ground`, `Environment` | Muted teal/warm amber palette; transparent where layered | Environment art / reserved, file absent |
| Station wagon — `Art/Props/station_wagon.png` | Dimensions TBD; `(0.5,0)`; 100 PPU | `Props` | Exact faded blue design and silhouette from `station-wagon-model-sheet.png`; frames TBD | Prop art / reserved, file absent |
| Garage, diner, rooftop hero sets — `Art/Environment/{vale_auto_garage,ruths_diner,rooftop_overlook}.png` | Dimensions TBD; bottom-aligned pivots; 100 PPU | `Environment`, `Foreground` as authored | Must match shot-list reference composition/palette; animation frames TBD | Environment art / reserved, files absent |

## Audio delivery slots

| Slot / final filename family | Master contract | Peak target | Mixer routing | Owner/status |
| --- | --- | --- | --- | --- |
| Score — `Audio/Music/*.wav` | 48 kHz / 24-bit WAV | ≤ −6 dBFS | `Master/Music` | Composer / reserved, masters absent |
| World and interaction SFX — `Audio/SFX/*.wav` | 48 kHz / 24-bit WAV | ≤ −3 dBFS | `Master/SFX` | Sound design / reserved, masters absent |
| UI SFX — `Audio/UI/*.wav` | 48 kHz / 24-bit WAV | ≤ −6 dBFS | `Master/SFX` | Sound design / reserved, masters absent |
| English dialogue/efforts — `Audio/Voice/*.wav` | 48 kHz / 24-bit mono WAV unless spatial scene requires stereo | ≤ −6 dBFS | `Master/Voice` | Voice production / reserved, masters absent |

`NorthboundMixer.mixer` exposes Master, Music, SFX, and Voice volumes and supplies exact `Normal`, `Cinematic`, and `Pause` snapshots. Normalization and presentation changes occur in the mixer; source files remain unchanged.

## Cinematic slots

The user-facing delivery names come from `flow-video-shot-list.md`. The project filenames remain stable to preserve Unity GUIDs. All six supplied 3D CGI replacements are imported into these stable proxy-named slots and satisfy the technical contract; creative continuity and English voice-content approval remain human review items.

| Slot | User delivery filename | Stable project filename / GUID | Technical contract | Duration | Owner/status |
| --- | --- | --- | --- | --- | --- |
| Opening Promise | `opening_promise.mp4` | `Cinematics/Opening/opening_proxy.mp4` / `7e64c50de94fc4c53aa5f5ba90f8dc26` | 1920×1080, 30 fps, H.264 + AAC stereo 48 kHz | 49.900s (40–50) | Production supplied CGI replacement; technical pass, human continuity pending |
| Maya highlight | `highlight_maya.mp4` | `Cinematics/Highlights/maya_proxy.mp4` / `c63014df10e2f43eba1509661ac8e5e8` | 1920×1080, 30 fps, H.264 + AAC stereo 48 kHz | 59.900s (45–60) | Production supplied CGI replacement; technical pass, human continuity pending |
| Noah highlight | `highlight_noah.mp4` | `Cinematics/Highlights/noah_proxy.mp4` / `6bdabb3697aca47a7bd0b5ee44a40605` | 1920×1080, 30 fps, H.264 + AAC stereo 48 kHz | 59.900s (45–60) | Production supplied CGI replacement; technical pass, human continuity pending |
| Leo highlight | `highlight_leo.mp4` | `Cinematics/Highlights/leo_proxy.mp4` / `b20da990f02724b25ae683e441434150` | 1920×1080, 30 fps, H.264 + AAC stereo 48 kHz | 59.900s (45–60) | Production supplied CGI replacement; technical pass, human continuity pending |
| Rooftop Fracture | `rooftop_fracture.mp4` | `Cinematics/Rooftop/rooftop_proxy.mp4` / `ac844021f365c40549538217d8cf5f31` | 1920×1080, 30 fps, H.264 + AAC stereo 48 kHz | 74.900s (60–75) | Production supplied CGI replacement; technical pass, human continuity pending |
| Are You Coming? | `are_you_coming.mp4` | `Cinematics/Finale/finale_proxy.mp4` / `dcbb2c89781e44b0a86222495371be48` | 1920×1080, 30 fps, H.264 + AAC stereo 48 kHz | 44.900s (30–45) | Production supplied CGI replacement; technical pass, human continuity pending |

The supplied HEVC/44.1 kHz sources were converted to H.264/AAC-48 and trimmed only to documented maximum durations. AAC presence is verified; that alone does not prove English voice content.

## Continuity review criteria

Use the shot list at `docs/production/flow-video-shot-list.md` shot by shot. The model-sheet images named there are currently unavailable in the tracked project, so a human must compare against the owner-held originals before approval.

| Character | Hair / costume / palette / proportions | Key props | Screen-direction checks |
| --- | --- | --- | --- |
| Jamie | Preserve model-sheet hair silhouette, adult outfit, skin tone, muted palette, face, height, and body proportions in every shot | Old map, shared mission items; no unsolicited prop swaps | Maintain Jamie’s spatial role between Elias and the other friends; finale ends centred with all routes visible |
| Elias | Preserve mechanic hair/outfit, grease-marked hands, skin tone, palette, and proportions; no wardrobe drift | Old map, exact faded-blue wagon, second key, childhood photograph | Northern road/wagon remain his consistent visual pull; rooftop eyelines must match reverses |
| Maya | Preserve hair, paint-working costume, skin tone, palette, and proportions | Painted panel, paintings, studio key, blank folder/canvas | Gallery and rooftop shot/reverse-shot eyelines stay consistent; no unexplained side swaps |
| Noah | Preserve hair, outfit/headphones, skin tone, palette, and proportions | Recorder, microphones, cables, heavy equipment case | N05 is a NEW shot with Noah + Jamie references; N06 extends N05 and preserves booth/control-room orientation |
| Leo | Preserve hair, diner/work outfit, skin tone, palette, and proportions | Fries, restored table, family photograph, coffee cup, travel bag | Diner entrances/booth reverses and rooftop eyelines remain consistent |

Global review: preserve exact identities, faces, clothing, body proportions, vehicle/environment design, restrained acting, muted teal/warm amber palette, and intentional screen direction. Use `no unintended background people`; background visitors/regulars explicitly required by a shot remain allowed. All six videos are used. No ending videos are produced because endings are real-time Unity presentations.

## Git LFS and replacement validation

All `*.mp4` payloads are covered by the repository’s Git LFS attributes. Validate both layers before delivery:

1. `git lfs ls-files` lists all six stable project paths.
2. `git cat-file -p :<path>` begins with `version https://git-lfs.github.com/spec/v1`, proving the Git index stores a pointer rather than the media payload.
3. The working file remains playable and matches the codec/dimension/rate/duration row above.
4. The `.mp4.meta` GUID equals the value in this manifest; replacement never deletes or regenerates the meta file.
