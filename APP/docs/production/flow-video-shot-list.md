# Northbound — Flow 全部视频生成清单

## 使用方法

- 最终成片共 **6 条**，实际生成 **37 个短镜头**。
- `NEW`：新建生成，并上传表中列出的图片作为 Ingredients/References。
- `EXTEND`：选中上一个保留镜头，点击 Extend，再粘贴对应 Prompt。
- 统一设置：`16:9`、`8 seconds`、最高可用画质；先生成两个版本，只保留人物最稳定的一版。
- 暂时关闭自动对白和字幕。英文配音、环境音和字幕后期统一制作。
- 每条 Prompt 后面都默认附加“统一限制词”。

## Task 11 项目对齐修正

- 本项目副本保留用户原始清单的全部镜头内容，只修正已确认的生成流程：N05 必须 `NEW` 并上传 NOAH + JAMIE，N06 必须继续 `EXTEND N05`。
- 六条成片全部进入 Unity；不生成结局视频，结局继续由 Unity 实时演出。
- 统一限制词使用 `no unintended background people`，避免把清单中明确需要的背景访客或常客误删。

## 参考图片目录

所有图片位于：`docs/production/reference-images/`

| 简称 | 文件 |
| --- | --- |
| ADULT_GROUP | `adult-cast-lineup.png` |
| CHILD_GROUP | `child-cast-lineup.png` |
| JAMIE | `jamie-model-sheet.png` |
| ELIAS | `elias-model-sheet.png` |
| MAYA | `maya-model-sheet.png` |
| NOAH | `noah-model-sheet.png` |
| LEO | `leo-model-sheet.png` |
| CAR | `station-wagon-model-sheet.png` |
| ROOFTOP | `rooftop-overlook.png` |
| GARAGE | `vale-auto-garage.png` |
| DINER | `ruths-diner.png` |

## 统一限制词

将下面一段放在每条 Prompt 最后：

```text
High-end stylized 3D animated feature film, painterly handcrafted textures, restrained natural acting, muted teal and warm amber palette, bittersweet coming-of-age atmosphere, smooth cinematic camera movement, 16:9 landscape. Preserve the supplied character identities, faces, hairstyles, skin tones, clothing, body proportions, vehicle design and environment exactly. No dialogue, no lip sync, no captions, no readable text, no logos, no watermark, no unintended background people, no duplicate characters, no face changes, no wardrobe changes, no deformed hands, no sudden camera jumps, no photorealism, no anime.
```

---

# Video 1 — Opening Promise

成片文件：`opening_promise.mp4`
目标时长：40–50 秒

## O01 — Greybridge Establishing

- 状态：已完成并选中
- 保留文件：`Friends_sitting_on_rooftop_1080p_202608141257.mp4`
- 方式：NEW
- 图片：ROOFTOP

```text
Wide cinematic establishing shot of Greybridge during a neighborhood blackout at blue hour. The camera slowly glides above the quiet brick neighborhood toward the distant northern city lights. Old factories, railway buildings and a water tower create a declining industrial-town silhouette. The distant lights remain hopeful and attractive rather than threatening.
```

## O02 — Five Children on the Rooftop

- 状态：已完成并选中
- 保留文件：`Friends_sitting_on_rooftop_ledge_202608141259.mp4`
- 方式：NEW
- 图片：CHILD_GROUP + ROOFTOP

```text
Wide cinematic shot of the same five childhood friends sitting side by side on the rooftop parapet. Preserve their left-to-right identities and clothing from the supplied childhood lineup. The old road map lies on the roof beneath them and distant city lights glow beyond the ridge. Gentle rooftop wind moves their jackets.
```

## O03 — Elias Points North

- 方式：EXTEND O02
- 图片：不重新上传；使用 O02

```text
Continue seamlessly from the previous shot. The camera gently moves closer and begins a slow orbit around the five friends. Young Elias points excitedly toward the distant lights. Young Maya gives him a skeptical but amused look. Young Leo holds a small carton of fries, young Noah studies the skyline seriously, and young Jamie listens quietly.
```

## O04 — Children's Reactions

- 方式：EXTEND O03
- 图片：不重新上传；使用 O03

```text
Continue seamlessly. Move through restrained close reaction shots of the same five children as they imagine leaving Greybridge together. Elias is hopeful, Maya curious but cautious, Noah thoughtful, Leo warmly amused, and Jamie looks from the city lights back toward the friends. Preserve every face and outfit exactly.
```

## O05 — Signing the Map

- 方式：NEW 或 EXTEND O04
- 图片：CHILD_GROUP + ROOFTOP

```text
Top-down close shot of the old road map spread across the rooftop concrete. The same five children kneel around it and place their hands near the edges. One after another they make small pencil marks on the reverse side while the wind lifts the corners. Hands remain natural and the map contains no readable writing.
```

## O06 — Match Cut to the Garage

- 方式：NEW
- 图片：CAR + GARAGE

```text
Begin with a close view of the weathered road map pinned to the wall of Vale Auto Garage, visually matching the map from the previous shot. The camera slowly pulls back to reveal the warm garage, mechanic tools, old photographs and the exact faded blue station wagon. End on a stable wide view of the car beneath warm work lights.
```

---

# Video 2 — Maya Character Highlight

成片文件：`highlight_maya.mp4`
目标时长：45–60 秒

## M01 — Arriving at the Art Center

- 方式：NEW
- 图片：MAYA + JAMIE + ADULT_GROUP

```text
Exterior at early evening outside a modest community arts center inside a converted Greybridge brick warehouse. Maya and Jamie carry a large painted wooden panel toward the entrance after light rain. Only a few windows are illuminated, making the local exhibition feel small and sparsely attended.
```

## M02 — Hanging the Paintings

- 方式：EXTEND M01

```text
Continue inside the converted warehouse gallery. Maya and Jamie carefully hang paintings on worn white brick walls. The artwork depicts Greybridge's closed market, silent railway crossing, empty diner booths and abandoned steelworks. The camera tracks slowly across the paintings and the two friends.
```

## M03 — Maya Watches a Visitor

- 方式：NEW
- 图片：MAYA

```text
Medium cinematic shot inside the quiet exhibition. Maya stands beside her paintings with folded arms, pretending not to care. Only a few visitors move softly in the background. She secretly watches one visitor study a painting for a long time, and her guarded expression softens slightly.
```

## M04 — The Studio Key

- 方式：EXTEND M03

```text
Continue as an older arts coordinator approaches Maya and offers her a small studio key and a blank folder. Maya hesitates, then accepts the key. Cut to a close-up of the key resting in her paint-stained palm, followed by her restrained emotional reaction. No readable text.
```

## M05 — Gallery Floor Conversation

- 方式：NEW
- 图片：MAYA + JAMIE

```text
After closing, Maya and Jamie sit on the gallery floor with their backs against a worn brick wall beneath paintings of Greybridge. Warm work lights illuminate them while the rest of the warehouse remains dark. Maya studies a painting of the closed factory, then looks toward Jamie and admits something difficult without speaking.
```

## M06 — Opening the Studio

- 方式：EXTEND M05

```text
Continue into early morning. Maya unlocks a small upstairs studio. Dust floats through a beam of light. She places a blank canvas beside a window overlooking Greybridge rather than the northern skyline. Jamie remains in the doorway as Maya looks uncertain but quietly hopeful. Slowly pull back through the doorway.
```

---

# Video 3 — Noah Character Highlight

成片文件：`highlight_noah.mp4`
目标时长：45–60 秒

## N01 — Packing the Equipment

- 方式：NEW
- 图片：NOAH + JAMIE

```text
Inside an old family electronics shop at dusk, Noah quietly packs a portable field recorder, headphones, cables and microphones into a worn equipment case while Jamie watches the entrance. Narrow aisles, repair benches, boxed radios and dim fluorescent lights establish a modest family business.
```

## N02 — Father Finds the Application

- 方式：EXTEND N01

```text
Continue as Noah's father appears at the end of the aisle holding a blank community-radio application folder. He realizes what Noah is doing. Compose the father near the shop counter, Noah beside the equipment case and Jamie slightly between them without interfering. Restrained family tension, no villainous acting.
```

## N03 — Noah Finally Speaks

- 方式：EXTEND N02

```text
Continue the controlled confrontation. Noah's father gestures toward the shop and its unfinished work. Noah initially looks down and grips the equipment-case handle, then slowly raises his head and maintains eye contact for the first time. Keep the acting quiet, realistic and emotionally difficult.
```

## N04 — Choosing to Continue

- 方式：NEW
- 图片：NOAH + JAMIE

```text
At night, Noah carries the heavy equipment case along a quiet Greybridge street with Jamie beside him. He stops under a streetlight and looks back toward the electronics shop. Jamie waits without pulling him forward. After a long hesitation, Noah turns away from the shop and continues walking by his own choice.
```

## N05 — Temporary Radio Booth

- 方式：NEW
- 图片：NOAH + JAMIE

```text
Continue inside a tiny temporary community-radio booth. Noah connects cables, adjusts a microphone and places his field recorder on the desk. Jamie watches through the control-room glass. Old analog meters and warm indicator lights awaken, illuminating Noah's nervous but determined expression.
```

## N06 — Listening to Greybridge

- 方式：EXTEND N05

```text
Close cinematic montage of Noah listening through headphones. Use subtle match cuts suggesting a diner bell, distant train, garage tools and rooftop wind. End with Noah closing his eyes as he hears Greybridge as something worth preserving rather than something that owns him. Slow circular camera movement.
```

---

# Video 4 — Leo Character Highlight

成片文件：`highlight_leo.mp4`
目标时长：45–60 秒

## L01 — Bringing Back the Table

- 方式：NEW
- 图片：LEO + JAMIE + DINER

```text
Blue-hour exterior and interior transition at Ruth's Diner. Leo and Jamie carefully carry an old wooden table through the front door. Leo behaves playfully while protecting the table from the doorway. Warm amber windows contrast with darker closed storefronts outside.
```

## L02 — Final Evening Service

- 方式：EXTEND L01

```text
Continue inside the diner during its final evening. A small group of older regular customers occupies familiar booths. Leo serves coffee and fries with energetic humor while Jamie clears tables. Beneath the performance, Leo repeatedly pauses to look around the room as if memorizing it.
```

## L03 — Grandmother's Table

- 方式：NEW
- 图片：LEO + DINER

```text
Close shot of the restored old wooden table with a small family photograph beside a coffee cup. Leo notices the photograph, becomes quiet, and gently turns it face down before anyone sees how affected he is. Preserve the diner lighting and Leo's identity exactly.
```

## L04 — Closing the Diner

- 方式：EXTEND L03

```text
Continue as the last customers leave one at a time, offering Leo quiet farewell gestures. Jamie locks the front door. Leo turns a blank hanging sign toward the window while warm interior lights begin switching off behind him. The sign contains no readable text.
```

## L05 — The Empty Booth

- 方式：NEW
- 图片：LEO + JAMIE + DINER

```text
Jamie and Leo sit opposite each other in a dark diner booth illuminated only by a kitchen light and cool blue streetlight. Leo's joking expression finally falls away. He looks toward an empty travel bag behind the counter and admits vulnerability without spoken dialogue.
```

## L06 — One Light Left On

- 方式：EXTEND L05

```text
Continue as Leo switches off the final kitchen light and the diner becomes dark. After a pause, he turns on one small lamp above the restored wooden table, creating a single warm island in the room. Jamie stands beside him. End on Leo looking at the light with uncertainty rather than triumph.
```

---

# Video 5 — Rooftop Fracture

成片文件：`rooftop_fracture.mp4`
目标时长：60–75 秒

## R01 — Five Friends Return

- 方式：NEW
- 图片：ADULT_GROUP + ROOFTOP

```text
Wide evening shot of the same rooftop from childhood, now colder and emptier. The five older friends stand instead of sitting. Elias stands nearest the northern skyline, Maya, Noah and Leo form a loose opposing group, and Jamie remains between them. A faded chalk arrow points north.
```

## R02 — Maya Refuses Friday

- 方式：NEW
- 图片：MAYA + ELIAS + ROOFTOP

```text
Slow cinematic push toward Maya as she calmly tells Elias she is not leaving on Friday, expressed through restrained body language rather than audible dialogue. Elias is visible out of focus in the foreground and becomes completely still as he understands her decision.
```

## R03 — The Old Map

- 方式：EXTEND R02

```text
Continue in controlled shot-reverse-shot between Elias and Maya. Elias gestures toward the distant northern lights and holds the old map. Maya studies the childhood map, then gently folds it closed instead of tearing it. The rooftop wind becomes stronger.
```

## R04 — Noah Steps Forward

- 方式：NEW
- 图片：NOAH + ELIAS + ROOFTOP

```text
Noah steps slightly forward and removes the headphones from around his neck. He quietly asks for time without audible dialogue. Elias looks at Noah with wounded disbelief, as though Noah's quiet disagreement hurts more than Maya's direct refusal.
```

## R05 — Leo Stops Joking

- 方式：NEW
- 图片：LEO + ELIAS + ROOFTOP

```text
Leo attempts to relieve the tension with a joke and forces a brief smile that nobody returns. His smile slowly disappears. He looks directly at Elias and finally speaks honestly through restrained expression and gesture. Jamie watches from the side, unable to protect everyone from the moment.
```

## R06 — Elias Loses Composure

- 方式：NEW
- 图片：ELIAS + ADULT_GROUP + ROOFTOP

```text
Emotionally intense medium close shot of Elias losing his composure without violence. His frustration mixes with exhaustion and grief. Grease remains embedded in his mechanic's hands. He looks across the four friends as though the life he spent years building is disappearing. No threatening movement.
```

## R07 — Jamie Cannot Answer

- 方式：NEW
- 图片：JAMIE + ELIAS + ROOFTOP

```text
Elias turns toward Jamie and waits for support. Hold on their eye contact. Jamie cannot answer. Elias gives a small devastated nod, folds the old map and walks toward the rooftop stairs. The others quietly make room, but nobody tries to stop him.
```

## R08 — Jamie and the Arrow

- 方式：EXTEND R07

```text
Continue in a wide static composition after Elias leaves. Maya, Noah and Leo slowly depart in different directions, leaving Jamie alone beside the faded chalk arrow. The camera rises overhead. The arrow points north while Jamie's long shadow falls sideways across it.
```

---

# Video 6 — Are You Coming?

成片文件：`are_you_coming.mp4`
目标时长：30–45 秒

## F01 — Greybridge Before Dawn

- 方式：NEW
- 图片：CAR + GARAGE

```text
Pre-dawn wide shot of Greybridge's quiet neighborhood outside Vale Auto Garage. Closed storefronts, an empty bus stop and pale blue morning fog surround the exact faded blue station wagon. The car's engine turns over after a brief hesitation and begins idling beneath the cold morning light.
```

## F02 — Elias Inside the Car

- 方式：NEW
- 图片：ELIAS + CAR

```text
Inside the exact station wagon, Elias sits behind the steering wheel while the dashboard vibrates with the idling engine. He looks across the empty passenger seats and then toward a small childhood photograph near the dashboard. His expression combines hope, exhaustion and fear.
```

## F03 — The Second Key

- 方式：EXTEND F02

```text
Continue as Elias steps out, walks around the station wagon and places a second metal car key on the roof. Cut to a close-up of the key vibrating subtly from the idling engine. Elias keeps one hand on the roof for a moment before letting go.
```

## F04 — Every Direction Visible

- 方式：NEW
- 图片：ADULT_GROUP + CAR + GARAGE

```text
Wide spatial composition showing every possible direction without highlighting any one as correct. Elias and the station wagon stand toward the northern road. Maya waits near the path toward the arts center, Noah stands between the electronics shop and radio station, Leo stands beside the closed diner, and a separate unmarked road leads away. Jamie stands alone in the center.
```

## F05 — Return Control to the Player

- 方式：EXTEND F04

```text
Continue as Elias looks directly toward Jamie and quietly waits for an answer, with no audible dialogue. The camera slowly pulls backward and rises into the same top-down diagonal angle used by gameplay. End on a stable wide composition with Jamie centered and every direction visible. Hold the final frame for two seconds. Nobody moves toward an ending.
```

---

# 完成顺序

1. 完成 O03–O06，剪出 `opening_promise.mp4`。
2. 依次完成 Maya、Noah、Leo 三个 Highlight。
3. 完成 Rooftop Fracture。
4. 最后完成 Are You Coming。
5. 不生成结局视频；四种结局由 Unity 实时演出。

# 文件命名规则

```text
opening_shot_01 ... opening_shot_06
maya_shot_01 ... maya_shot_06
noah_shot_01 ... noah_shot_06
leo_shot_01 ... leo_shot_06
rooftop_shot_01 ... rooftop_shot_08
finale_shot_01 ... finale_shot_05
```
