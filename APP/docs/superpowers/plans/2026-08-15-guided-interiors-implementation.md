# Guided Interiors and Interaction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild Greybridge as a coherent exterior town with enterable character-specific interiors, five fully directional characters, visible and persistent physical-object feedback, clear first-time guidance, and polished responsive minigames.

**Architecture:** Keep one Greybridge scene and compose isolated exterior/interior location roots at runtime. `LocationTransitionController` owns active-root/camera/spawn transitions, `GuidanceController` derives objective copy and markers from the existing narrative director, and focused visual/feedback components decorate existing quest interfaces without changing story data. The art catalog remains the replaceable boundary for generated proxy images.

**Tech Stack:** Unity 6000.3.22f1, C# 9, Unity 2D physics, legacy Unity UI already used by the project, Input System, NUnit EditMode/PlayMode tests, built-in image generation for PNG proxy art, macOS Standalone build.

## Global Constraints

- Keyboard and mouse only; do not add gamepad bindings or virtual-device production code.
- Keep the approved English narrative, quest ordering, exclusive mission pairs, six cinematic slots, save schema, and four ending directions intact.
- The five current characters are exactly Jamie, Elias, Maya, Noah, and Leo.
- Every moving one of those five characters requires distinct North/South/East/West idle and walk visuals.
- Interior artwork must never be rendered directly over the exterior street.
- A physical objective only disappears or changes state after the quest report succeeds.
- All transient feedback must release the global `InputGate` on success, cancellation, disable, and error.
- Use TDD for every behavior change and run Unity tests without trailing `-quit` so the Test Framework update loop completes.
- Generated art is runtime-ready proxy art with stable catalog IDs; it is not claimed as final studio art.
- The supplied CGI is reference-only: regenerate every exterior/interior as elevated 3/4 top-down hand-painted gameplay art; never paste a raw CGI plate into the world.
- Human observation is still required for composition, readability at the user's display resolution, continuity, and 45–60 minute pacing.

---

### Task 1: Five-Character Directional Animation Contract

**Files:**
- Modify: `Assets/Northbound/Scripts/Art/NorthboundArtCatalog.cs`
- Modify: `Assets/Northbound/Scripts/Art/TopDownCharacterVisual.cs`
- Modify: `Assets/Northbound/Editor/NorthboundArtAssetSeeder.cs`
- Modify: `Assets/Northbound/Tests/EditMode/ArtCatalogTests.cs`
- Modify: `Assets/Northbound/Tests/PlayMode/CharacterVisualPlayModeTests.cs`
- Replace: `Assets/Northbound/Art/Characters/{jamie,elias,maya,noah,leo}-sprite-sheet.png`

**Interfaces:**
- Produces: `CharacterFrameLayout`, serialized per-character `idleRow`, `walkRow`, and four facing columns.
- Produces: `TopDownCharacterVisual.SetScriptedVelocity(Vector2 velocity)` for NPC path movement.
- Preserves: `NorthboundArtCatalog.Character(string id, Facing facing, bool walking) : Sprite`.

- [ ] **Step 1: Write failing direction-identity tests**

Add assertions for each character:

```csharp
foreach (var id in new[] { "jamie", "elias", "maya", "noah", "leo" })
{
    var idle = Enum.GetValues(typeof(Facing)).Cast<Facing>()
        .Select(facing => catalog.Character(id, facing, false)).ToArray();
    var walk = Enum.GetValues(typeof(Facing)).Cast<Facing>()
        .Select(facing => catalog.Character(id, facing, true)).ToArray();
    Assert.That(idle.Distinct().Count(), Is.EqualTo(4));
    Assert.That(walk.Distinct().Count(), Is.EqualTo(4));
}
```

Add a PlayMode case that calls `SetScriptedVelocity(Vector2.left/up/right/down)` on an NPC without a Rigidbody and checks `CurrentFacing` and its walk sprite after each frame.

- [ ] **Step 2: Run focused RED**

Run EditMode `Northbound.Tests.ArtCatalogTests` and PlayMode `Northbound.Tests.CharacterVisualPlayModeTests` with Unity batch commands. Expected RED: missing `SetScriptedVelocity`, and visual-source validation rejects the current front-biased sheet layout.

- [ ] **Step 3: Generate corrected source sheets**

Use the approved model sheets as visual references and the following structure for every character:

```text
Transparent/chroma-key 4-column × 2-row top-down game sprite sheet.
Columns in exact order: North/back, South/front, East/profile, West/profile.
Top row: idle. Bottom row: mid-stride walk.
Same clothes, hair, palette and proportions as the supplied adult model reference.
Camera is elevated 3/4 top-down; no labels, borders, shadows crossing cell edges, or duplicated front views.
```

Copy the five resulting PNGs into the existing stable asset paths and preserve their `.meta` GUIDs.

- [ ] **Step 4: Implement explicit frame metadata**

Add:

```csharp
[Serializable]
public sealed class CharacterFrameLayout
{
    public int columns = 4;
    public int rows = 2;
    public int idleRow = 1;
    public int walkRow = 0;
    public int northColumn = 0;
    public int southColumn = 1;
    public int eastColumn = 2;
    public int westColumn = 3;
}
```

Store it on `CharacterSheet`, use it for sprite rect lookup, and have `TopDownCharacterVisual` prefer non-zero scripted velocity for the current frame, falling back to Rigidbody velocity.

- [ ] **Step 5: Run focused GREEN and commit**

Expected: both focused suites pass and all five IDs return eight non-null, directionally distinct slices.

Commit: `feat: add five-character directional animation`.

---

### Task 2: Persistent Physical-Objective Feedback

**Files:**
- Create: `Assets/Northbound/Scripts/Interaction/ObjectivePropFeedback.cs`
- Create: `Assets/Northbound/Scripts/UI/InteractionFeedbackService.cs`
- Modify: `Assets/Northbound/Scripts/Content/NarrativeObjectiveTrigger.cs`
- Modify: `Assets/Northbound/Scripts/Art/GreybridgeArtBuilder.cs`
- Modify: `Assets/Northbound/Scripts/Core/GameBootstrap.cs`
- Create: `Assets/Northbound/Tests/PlayMode/ObjectivePropFeedbackTests.cs`

**Interfaces:**
- Produces: `ObjectivePropFeedback.Configure(string completedFact, CompletionVisualMode mode)`.
- Produces: `ObjectivePropFeedback.PlaySuccessAndHide()` and `Refresh(NarrativeStateStore state)`.
- Produces: `InteractionFeedbackService.Show(string message, FeedbackKind kind)` plus soft procedural pickup/success/error tones.
- Modifies: `NarrativeObjectiveTrigger.Interact` to apply visual feedback only after successful completion.

- [ ] **Step 1: Write failing pickup lifecycle tests**

Create a real `NarrativeObjectiveTrigger` with a visible child and collider. Assert:

```csharp
trigger.Interact(jamie);
Assert.That(director.State.Has("objective_missing_socket_find_socket_complete"), Is.True);
Assert.That(visual.activeSelf, Is.False);
Assert.That(collider.enabled, Is.False);
```

Add rejection coverage: out-of-order interaction leaves visual/collider enabled and produces an amber guidance message. Add reload coverage that constructs a new feedback component with an already-complete state and starts hidden.

- [ ] **Step 2: Run focused RED**

Expected RED: `ObjectivePropFeedback` and feedback service do not exist; current trigger reports completion but leaves the visual enabled.

- [ ] **Step 3: Implement commit-before-animation behavior**

Change `NarrativeContentDirector.CompleteActiveQuestObjective` to return `bool` if it does not already expose success. `NarrativeObjectiveTrigger` performs:

```csharp
if (!director.CompleteActiveQuestObjective(objectiveId))
{
    feedback.Show("Finish the current step first.", FeedbackKind.Guidance);
    return;
}
objectiveFeedback.PlaySuccessAndHide();
feedback.Show($"{prompt} complete", FeedbackKind.Success);
```

For pickup props, animate scale/alpha over 0.18 seconds and disable the collider. For station wagon, gallery lights, and other service objects, swap to a completed-state child rather than hiding the whole object.

- [ ] **Step 4: Add readable toast and procedural audio**

Create a Screen Space Overlay toast with `LegacyRuntime.ttf`, minimum 34px at 1920×1080. Generate short in-memory sine/triangle AudioClips at startup: rising two-note pickup, soft confirmation, and low error tick. Route them to SFX mixer group when available.

- [ ] **Step 5: Run focused GREEN and commit**

Expected: success, rejection, save/reload, collider state, and input-cleanup cases pass.

Commit: `feat: add persistent objective feedback`.

---

### Task 3: First-Time Guidance and Active Mission Marker

**Files:**
- Create: `Assets/Northbound/Scripts/Guidance/GuidanceStep.cs`
- Create: `Assets/Northbound/Scripts/Guidance/GuidanceController.cs`
- Create: `Assets/Northbound/Scripts/Guidance/ObjectiveMarker.cs`
- Create: `Assets/Northbound/Scripts/Guidance/GuidanceHudView.cs`
- Modify: `Assets/Northbound/Scripts/Core/GameBootstrap.cs`
- Modify: `Assets/Northbound/Scripts/Content/NarrativeContentDirector.cs`
- Modify: `Assets/Northbound/Scripts/World/GreybridgeWorldLayout.cs`
- Create: `Assets/Northbound/Tests/EditMode/GuidanceControllerTests.cs`
- Create: `Assets/Northbound/Tests/PlayMode/GuidanceFlowTests.cs`

**Interfaces:**
- Produces: `GuidanceController.CurrentStep`, `CurrentDestinationId`, `Refresh()`.
- Produces: `GuidanceController.RegisterTarget(string targetId, Transform target, MarkerKind kind)`.
- Produces: `GuidanceHudView.Show(string chapter, string objective, string instruction)`.
- Consumes: active quest/objective and narrative facts from `NarrativeContentDirector` and `NarrativeStateStore`.

- [ ] **Step 1: Write guidance state RED tests**

Cover the opening sequence:

```csharp
Assert.That(guidance.CurrentStep.instruction, Does.Contain("Ruth's Diner"));
state.Set("tutorial_moved", true);
guidance.Refresh();
Assert.That(guidance.CurrentStep.instruction, Does.Contain("front door"));
```

Test that only one required star marker is active, optional dialogue uses a smaller speech marker, and tutorial facts prevent hints repeating after reload.

- [ ] **Step 2: Run focused RED**

Expected RED: guidance namespace/types absent.

- [ ] **Step 3: Implement data-driven guidance**

Derive text from active chapter and quest metadata rather than hard-coded scene order. Required tutorial facts are `tutorial_moved`, `tutorial_interacted`, `tutorial_objective_seen`, and `tutorial_paused`. Persist them through the existing narrative save authority.

- [ ] **Step 4: Implement HUD and world marker**

Use a top-left high-contrast panel for chapter/objective/instruction. Use a looping scale/alpha star above the registered required target; add a vertical screen-edge arrow when the target is outside the camera view. The marker has no collider and never intercepts clicks.

- [ ] **Step 5: Run focused GREEN and commit**

Expected: tutorial, required-marker uniqueness, optional-marker priority, save/reload, and input-gate visibility tests pass.

Commit: `feat: add guided mission navigation`.

---

### Task 4: Door Transitions and Isolated Location Roots

**Files:**
- Create: `Assets/Northbound/Scripts/World/LocationDefinition.cs`
- Create: `Assets/Northbound/Scripts/World/DoorInteractor.cs`
- Create: `Assets/Northbound/Scripts/World/LocationTransitionController.cs`
- Create: `Assets/Northbound/Scripts/World/LocationFadeView.cs`
- Modify: `Assets/Northbound/Scripts/Player/FollowCamera.cs`
- Modify: `Assets/Northbound/Scripts/World/GreybridgeWorldLayout.cs`
- Create: `Assets/Northbound/Tests/PlayMode/LocationTransitionTests.cs`

**Interfaces:**
- Produces: `LocationDefinition { id, root, spawn, cameraBounds, displayName }`.
- Produces: `DoorInteractor.Configure(string prompt, string destinationId, LocationTransitionController controller)`.
- Produces: `LocationTransitionController.Register(LocationDefinition definition)` and `IEnumerator Travel(string destinationId)`.
- Consumes: Jamie Transform, global `InputGate`, `FollowCamera`, and `GuidanceController`.

- [ ] **Step 1: Write door transition RED tests**

Test exterior-to-garage and garage-to-exterior. Assert exactly one location root is active, Jamie reaches the registered spawn, camera bounds update, prompt reads `[E] Enter Vale Auto Garage`, and input releases after fade. Add missing-destination and cancellation tests that leave exterior active.

- [ ] **Step 2: Run focused RED**

Expected RED: location and door types absent.

- [ ] **Step 3: Implement location registry and fade-safe travel**

Travel acquires one lease in a `try/finally`, fades to opaque, switches roots, moves Jamie, refreshes Physics2D transforms/camera, fades back, and disposes the lease even when configuration is invalid.

- [ ] **Step 4: Convert the existing world builder**

Create these roots and stable IDs:

```text
exterior
jamie_home
vale_garage
ruths_diner
maya_studio
noah_electronics
rooftop_overlook
```

Move logical route/objective/NPC parents to their authored location root. Exterior contains only town art, exterior colliders, building entrances, finale zones, and routes genuinely authored outdoors.

- [ ] **Step 5: Run focused GREEN and commit**

Expected: all entrances/exits, active-root uniqueness, camera bounds, failure cleanup, and existing path/ending tests pass.

Commit: `feat: add enterable Greybridge locations`.

---

### Task 5: Exterior Town and Five Character Interiors

**Files:**
- Replace/Create: `Assets/Northbound/Art/Environment/greybridge-exterior.png`
- Create: `Assets/Northbound/Art/Environment/jamie-home.png`
- Replace: `Assets/Northbound/Art/Environment/garage-plate.png`
- Replace: `Assets/Northbound/Art/Environment/diner-plate.png`
- Create: `Assets/Northbound/Art/Environment/maya-studio.png`
- Create: `Assets/Northbound/Art/Environment/noah-electronics.png`
- Replace: `Assets/Northbound/Art/Environment/rooftop-plate.png`
- Create: `Assets/Northbound/Art/Environment/building-facades.png`
- Modify: `Assets/Northbound/Editor/NorthboundArtAssetSeeder.cs`
- Modify: `Assets/Northbound/Scripts/Art/NorthboundArtCatalog.cs`
- Modify: `Assets/Northbound/Scripts/Art/GreybridgeArtBuilder.cs`
- Modify: `Assets/Northbound/Tests/EditMode/ArtCatalogTests.cs`
- Modify: `Assets/Northbound/Tests/PlayMode/GreybridgeArtPlayModeTests.cs`

**Interfaces:**
- Produces catalog environment IDs matching Task 4 location IDs.
- Produces one visual root per location with textured ground, facade/interior plate, foreground props, collision/occlusion boundaries, and door marker.

- [ ] **Step 1: Write art-structure RED tests**

Assert exterior contains facade SpriteRenderers but no interior plate names. Assert every interior ID has a distinct texture reference, root, spawn, door, return door, and at least five readable foreground prop renderers. Reject the former gallery/electronics alias to the street texture.

- [ ] **Step 2: Run focused RED**

Expected RED: missing location IDs and repeated street texture references.

- [ ] **Step 3: Generate coherent environment plates**

Use the supplied CGI/location references and exact prompt family:

```text
Original hand-painted 2D/2.5D game environment, elevated 3/4 top-down camera,
Greybridge working-class town at blue hour, readable walkable floor and door,
warm practical lighting, restrained painterly texture, no characters, no text,
no perspective horizon inside rooms, no UI, 16:9 gameplay composition.
```

For each interior, name the story props in the prompt: garage tools/station wagon; diner counter/booths/order pass; Maya paintings/mural/gallery lamps; Noah recorder/cables/radios; Jamie packed personal objects. Generate an exterior sheet with coordinated facades and doors, not interior views. Reject any result with a visible cinematic horizon, eye-level camera, photographic CGI rendering, inconsistent wall angles, or character-scale mismatch.

- [ ] **Step 4: Rebuild the catalog and compose layers**

Run `Northbound.Editor.NorthboundArtAssetSeeder.Rebuild`. `GreybridgeArtBuilder` creates background, facade/wall, floor props, foreground/occlusion, and door layers with stable sorting orders. Interior roots default inactive except the current location.

- [ ] **Step 5: Run focused GREEN and commit**

Expected: catalog uniqueness and scene structure pass; a captured game frame shows no interior image lying on exterior street.

Commit: `feat: rebuild Greybridge exterior and interiors`.

---

### Task 6: Diner Connection-Matching Minigame

**Files:**
- Modify: `Assets/Northbound/Scripts/Minigames/DinerShiftGame.cs`
- Create: `Assets/Northbound/Scripts/Minigames/DinerConnectionView.cs`
- Create: `Assets/Northbound/Art/UI/diner-orders.png`
- Modify: `Assets/Northbound/Tests/PlayMode/MinigameTests.cs`

**Interfaces:**
- Produces: `DinerShiftGame.SelectedOrderId`, `TryMatch(string orderId, string tableId) : bool`.
- Produces: `DinerConnectionView.SetSelection`, `AnimateConnection`, `AnimateMismatch`, and `RemoveCompletedPair`.
- Preserves: `SelectOrder`, `DeliverToTable`, mouse button names used by compatibility tests, and keyboard `1–3` plus `Q/W/E`.

- [ ] **Step 1: Write connection-feedback RED tests**

Use actual Button pointer events and queued keyboard states. Assert selecting coffee highlights its card, choosing coffee table creates a visible connection and removes both completed targets, while coffee-to-pie shakes and leaves both active. Assert status text changes on every input.

- [ ] **Step 2: Run focused RED**

Expected RED: connection view/selection property absent and completed controls remain visible.

- [ ] **Step 3: Implement shared input path and visual connection**

Mouse and keyboard both call `TryMatch`. Draw the line with a UI `Image` rotated/scaled between target centers. Correct line turns green and fades with its pair; mismatch uses an amber line and short anchored-position shake.

- [ ] **Step 4: Add order/table illustration sheet and audio feedback**

Generate six readable icons (coffee, pie, soup and three distinct tables) on transparent background. Use the shared success/error feedback service for audio and toast text.

- [ ] **Step 5: Run focused GREEN and commit**

Expected: existing diner tests plus visual connection, disappearance, mismatch, and keyboard parity tests pass.

Commit: `feat: polish diner matching game`.

---

### Task 7: Wiring and Trunk Visual Interaction Upgrade

**Files:**
- Modify: `Assets/Northbound/Scripts/Minigames/WiringGame.cs`
- Create: `Assets/Northbound/Scripts/Minigames/WiringBoardView.cs`
- Modify: `Assets/Northbound/Scripts/Minigames/TrunkPackingGame.cs`
- Create: `Assets/Northbound/Scripts/Minigames/TrunkGridView.cs`
- Create: `Assets/Northbound/Art/UI/wiring-tiles.png`
- Create: `Assets/Northbound/Art/UI/trunk-items.png`
- Modify: `Assets/Northbound/Tests/PlayMode/MinigameTests.cs`

**Interfaces:**
- Produces wiring tile rotation visuals and a source-to-recorder illuminated path.
- Produces trunk item silhouettes, placement preview, occupied-cell rendering, and packed-item sprites.
- Preserves authored layout/shape data and existing public model APIs.

- [ ] **Step 1: Write visual-state RED tests**

Assert each wire tile's RectTransform rotation matches model rotation; only connected prefixes glow. Assert trunk hover/selected origin produces green preview when `Fits` is true and red preview otherwise; placed shapes remain rendered in occupied cells.

- [ ] **Step 2: Run focused RED**

Expected RED: board/grid visual types absent.

- [ ] **Step 3: Implement wiring presentation**

Create four large square tile Buttons with cable sprites. Rotate the sprite by 90° per model turn, illuminate sequential connected segments, and animate the recorder indicator on completion. Keep reliable rising-edge `1–4` and `R` controls.

- [ ] **Step 4: Implement trunk presentation**

Create item cards from five silhouettes, a 6×4 grid with hover/cell states, rotation preview, and occupied overlays. Failed placement produces red feedback and does not mutate occupancy; confirmation remains locked until three items are placed.

- [ ] **Step 5: Run focused GREEN and commit**

Expected: all current minigame tests plus board/grid visual-state and keyboard/mouse parity tests pass.

Commit: `feat: polish wiring and trunk games`.

---

### Task 8: Integration, Release Validation, and Human Handoff

**Files:**
- Modify: `Assets/Northbound/Tests/PlayMode/ReleaseAcceptanceTests.cs`
- Modify: `docs/qa/playtest-script.md`
- Modify: `docs/qa/playtest-results.md`
- Modify: `docs/qa/release-checklist.md`
- Modify only if failures require it: runtime files from Tasks 1–7

**Interfaces:**
- Consumes all prior tasks.
- Produces: ignored `Builds/macOS/Northbound.app` and exact verification evidence.

- [ ] **Step 1: Add full-route acceptance coverage**

Run a temporary-save Bootstrap route that completes the tutorial, enters every authored interior, talks to all five characters, completes representative pickup/service/minigame objectives, reloads, and verifies completed props stay hidden. Assert the exterior never has an active interior root and no unexpected Error/Exception logs occur.

- [ ] **Step 2: Run complete EditMode and PlayMode suites**

Use Unity 6000.3.22f1 and results:

```text
/private/tmp/northbound-guided-final-edit.xml
/private/tmp/northbound-guided-final-play.xml
```

Expected: zero failed, inconclusive, or skipped tests; logs contain no compiler, assertion, missing-script, or unexpected exception signatures.

- [ ] **Step 3: Build macOS release**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit \
  -projectPath /Users/kongzhiren/Documents/ChatGPT/Northbond/.worktrees/northbound-unity \
  -executeMethod Northbound.Editor.NorthboundReleaseBuilder.BuildMacOS \
  -logFile /private/tmp/northbound-guided-build.log
```

Expected: `Build Finished, Result: Success`, exact release scenes Bootstrap/Greybridge, and a Universal x86_64/arm64 executable.

- [ ] **Step 4: Update honest QA records**

Record automated counts and build facts. Leave composition, display-resolution readability, CGI continuity, three observed playtests, theme understanding, and 45–60 minute pacing explicitly pending until a human performs them.

- [ ] **Step 5: Commit release candidate**

Run `git diff --check`, confirm only task-owned files are staged, and commit `test: verify guided interiors release`.
