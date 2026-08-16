# Room Bounds, Directional Animation, and Main-Path Guidance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every enterable room fill the view and physically contain Jamie, show real four-direction movement, and state the next main-story action clearly.

**Architecture:** `LocationTransitionController` owns location activation, camera bounds, and a new player movement boundary. `PlayerMotor` exposes the movement vector it actually applies so `TopDownCharacterVisual` selects the correct directional frame. `GuidanceController` resolves a named location and action into a persistent HUD, with markers only for that action.

**Tech Stack:** Unity 6000.3.22f1, C#, Unity Test Framework, 2D physics, legacy Unity UI.

## Global Constraints

- Keyboard/mouse only.
- Keep the seven existing location IDs and story route data intact.
- Do not change English narrative content or quest ordering.
- Prove every behavior with a failing PlayMode or EditMode test before production changes.

---

### Task 1: Location viewport and physical containment

**Files:**
- Modify: `Assets/Northbound/Scripts/World/LocationDefinition.cs`
- Modify: `Assets/Northbound/Scripts/World/LocationTransitionController.cs`
- Modify: `Assets/Northbound/Scripts/World/GreybridgeWorldLayout.cs`
- Test: `Assets/Northbound/Tests/PlayMode/LocationTransitionTests.cs`

- [x] **Step 1: Write a failing runtime test**

Assert that travelling to every interior creates four non-trigger wall colliders under its location root, that a player Rigidbody2D cannot cross a wall, and that its camera bounds represent the visible room interior rather than its center alone.

- [x] **Step 2: Run the focused test and record the expected RED failure**

Run the LocationTransition PlayMode filter. It must fail because the current locations contain no room-wall colliders.

- [x] **Step 3: Implement the smallest containment model**

Extend `LocationDefinition` with `walkableBounds`. On travel, create/update four boundary colliders for the active root, apply camera bounds that account for the orthographic viewport, and scale room art to cover the calculated viewport.

- [x] **Step 4: Re-run the focused test and record GREEN**

### Task 2: Real directional player visuals

**Files:**
- Modify: `Assets/Northbound/Scripts/Player/PlayerMotor.cs`
- Modify: `Assets/Northbound/Scripts/Art/TopDownCharacterVisual.cs`
- Test: `Assets/Northbound/Tests/PlayMode/CharacterVisualPlayModeTests.cs`

- [x] **Step 1: Write a failing integration test**

Set Jamie's actual `PlayerMotor` input to north, south, east, and west and assert the running `TopDownCharacterVisual` updates to the corresponding walking sprite before physics velocity is available.

- [x] **Step 2: Run the focused test and record RED**

- [x] **Step 3: Implement one real input-to-visual data path**

`PlayerMotor` publishes its applied movement input; `TopDownCharacterVisual` reads it when attached to a motor, retaining the last facing direction on stop.

- [x] **Step 4: Re-run the focused test and record GREEN**

### Task 3: Explicit main-path HUD and marker labels

**Files:**
- Modify: `Assets/Northbound/Scripts/Guidance/GuidanceStep.cs`
- Modify: `Assets/Northbound/Scripts/Guidance/GuidanceController.cs`
- Modify: `Assets/Northbound/Scripts/Guidance/GuidanceHudView.cs`
- Test: `Assets/Northbound/Tests/EditMode/GuidanceControllerTests.cs`
- Test: `Assets/Northbound/Tests/PlayMode/GuidanceFlowTests.cs`

- [x] **Step 1: Write failing tests**

Assert that each first-step state returns a non-empty location name, goal, and imperative action; assert the HUD displays all three and only the currently required target is marked.

- [x] **Step 2: Run focused tests and record RED**

- [x] **Step 3: Implement named route guidance**

Add `locationName` and `nextAction` to `GuidanceStep`; map destination route/goal to its registered location and produce copy such as `GO TO: Vale Auto Garage`, `ENTER: Press E at the gold door`, and `NEXT: Talk to Elias`.

- [x] **Step 4: Re-run focused tests and record GREEN**

### Task 4: Regression and handoff

**Files:**
- Modify: `docs/qa/playtest-script.md`

- [x] **Step 1: Run full EditMode and PlayMode suites**

- [x] **Step 2: Update the manual script**

Add room-boundary, full-screen interior, four-direction Jamie, and main-path guidance checks.

- [ ] **Step 3: Commit the verified fixes**
