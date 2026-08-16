# Northbound 3D cinematic replacement manifest

Replace the contents of the existing MP4 files at these exact paths. Keep each existing `.mp4.meta` file in place so Unity preserves the reference GUID; after replacing a movie, return to Unity and let it reimport. The six supplied 3D CGI replacements are now technically validated in these stable proxy-named slots; the names remain unchanged solely to preserve the replacement contract.

| Slot | Exact project path | Story purpose | Recommended final duration |
| --- | --- | --- | --- |
| Opening | `Assets/Northbound/Cinematics/Opening/opening_proxy.mp4` | Establish the childhood promise and match-cut into the present garage. | 40–50 seconds |
| Maya highlight | `Assets/Northbound/Cinematics/Highlights/maya_proxy.mp4` | Show Maya's exhibition, studio offer, and self-directed future. | 45–60 seconds |
| Noah highlight | `Assets/Northbound/Cinematics/Highlights/noah_proxy.mp4` | Show Noah confronting family duty and choosing the radio work. | 45–60 seconds |
| Leo highlight | `Assets/Northbound/Cinematics/Highlights/leo_proxy.mp4` | Show the diner's final service and Leo admitting his attachment. | 45–60 seconds |
| Rooftop | `Assets/Northbound/Cinematics/Rooftop/rooftop_proxy.mp4` | Break the group's shared promise and leave Jamie beside the faded arrow. | 60–75 seconds |
| Finale | `Assets/Northbound/Cinematics/Finale/finale_proxy.mp4` | Ask “Are You Coming?” and match the final frame into player-controlled direction choice. | 30–45 seconds |

## Delivery specification

- Container: MP4
- Video: H.264/AVC, 1920×1080, progressive, 30 fps, 8-bit YUV 4:2:0
- Audio: AAC-LC, 48 kHz stereo if supplied; omit audio if the clip is intentionally silent
- No alpha channel, no HDR, no variable frame rate, no embedded subtitles
- Export with fast-start/web-optimized metadata when available
- Follow the per-slot durations above. The player has a two-second skip lockout, then supports Space, Esc, and the on-screen mouse button.

Subtitles are separate game text in each `CinematicAsset`; do not burn dialogue subtitles into the final renders. The project’s current files are the supplied 3D CGI replacements converted to contract-valid H.264/AAC-48: Opening 49.900s, Maya/Noah/Leo 59.900s each, Rooftop 74.900s, and Finale 44.900s. Technical validation is not human continuity approval. The shot-level production plan is `docs/production/flow-video-shot-list.md`; its 37 generated shots assemble into all six final files. No ending video is required because all endings are rendered in Unity after the Finale clip returns control.
