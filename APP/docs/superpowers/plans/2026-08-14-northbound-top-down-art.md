# Northbound Top-Down Art Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Greybridge's primitive greybox with a cohesive, playable 3/4 top-down 2D world using the approved five-character, vehicle, diner, garage, and rooftop references.

**Architecture:** Keep the existing scene topology, coordinates, colliders, narrative IDs, and input unchanged. Introduce a `NorthboundArtCatalog` resource for sprites and visual presets; instantiate composed SpriteRenderer prefabs from `GreybridgeWorldLayout` in place of primitive quads. Generated raster assets are reference-derived, imported as sprites with explicit filtering, pivots, pixels-per-unit and sort ordering.

**Tech Stack:** Unity 6000.3.22f1, C#, SpriteRenderer/Animator, Unity Test Framework, built-in image generation, macOS standalone build.

## Global Constraints

- Preserve all existing quest, dialogue, cinematic, ending, save, collider, trigger, and object-ID behavior.
- Keyboard and mouse only; do not add controller handling.
- Use original Northbound art only; do not use Brawl Stars or other third-party game assets.
- Adult model sheets are canonical for Jamie, Elias, Maya, Noah, and Leo in present-day gameplay.
- Camera is orthographic 3/4 top-down; all characters need four directional idle and walk visuals.
- Required visual identity: tan/green Jamie, rust/navy Elias, painted-denim/mustard Maya, burgundy/glasses/headphones Noah, red/white Leo, worn blue station wagon.
- Required primary locations: Vale Auto Garage, Ruth's Diner, Rooftop Overlook, Old Neighborhood streets, gallery/electronics mission areas.
- Never fall back to a visible untextured primitive square for a named world visual.
- Preserve the untracked Task12 release test scaffold and do not modify its scope until the art pass has its own validation.

---

## File Structure

| Path | Responsibility |
|---|---|
| `Assets/Northbound/Art/Characters/*.png` | Reference-derived four-direction sprite sheets for the five adult characters. |
| `Assets/Northbound/Art/Props/station-wagon.png` | Four-direction wagon and open-trunk state. |
| `Assets/Northbound/Art/Environment/*.png` | Ground, structural, and prop atlases for street, garage, diner, rooftop, gallery, and electronics zones. |
| `Assets/Northbound/Art/NorthboundArtCatalog.asset` | Serialized catalog assigning every required sprite and visual preset. |
| `Assets/Northbound/Scripts/Art/NorthboundArtCatalog.cs` | Typed resource API for named art entries. |
| `Assets/Northbound/Scripts/Art/TopDownCharacterVisual.cs` | Four-direction SpriteRenderer animator driven by `Rigidbody2D.velocity` or PlayerMotor movement direction. |
| `Assets/Northbound/Scripts/Art/GreybridgeArtBuilder.cs` | Composes named location backgrounds, buildings, props, quest-object sprites, and contact shadows at existing map coordinates. |
| `Assets/Northbound/Scripts/World/GreybridgeWorldLayout.cs` | Uses the art builder instead of `CreatePrimitive` proxy visuals while retaining logical map components. |
| `Assets/Northbound/Prefabs/Characters/*.prefab` | Character SpriteRenderer/Animator composition; identities and interaction components remain intact. |
| `Assets/Northbound/Tests/EditMode/ArtCatalogTests.cs` | Asset metadata and catalog completeness tests. |
| `Assets/Northbound/Tests/PlayMode/GreybridgeArtPlayModeTests.cs` | Runtime visual-root, named-sprite, directional-animation, and proxy-regression tests. |
| `docs/production/art-asset-manifest.md` | Source reference, filename, purpose, replacement and human-review ledger. |

## Task 1: Art Catalog and Asset Contract

**Files:**
- Create: `Assets/Northbound/Scripts/Art/NorthboundArtCatalog.cs`
- Create: `Assets/Northbound/Art/NorthboundArtCatalog.asset`
- Create: `Assets/Northbound/Tests/EditMode/ArtCatalogTests.cs`
- Create: `docs/production/art-asset-manifest.md`

**Interfaces:**
- Produces `Northbound.Art.NorthboundArtCatalog` with `Sprite Character(string id, Facing facing, bool walking)`, `Sprite Prop(string id)`, and `Sprite Environment(string id)`.
- Produces `Facing { North, South, East, West }` shared by character visuals.
- Consumers: `TopDownCharacterVisual`, `GreybridgeArtBuilder`, `GreybridgeWorldLayout`, PlayMode smoke tests.

- [ ] **Step 1: Write failing completeness tests**

```csharp
[Test]
public void Catalog_ContainsFiveCharactersFourDirectionsAndCoreLocations()
{
    var catalog = Resources.Load<NorthboundArtCatalog>("Northbound/NorthboundArtCatalog");
    foreach (var id in new[] { "jamie", "elias", "maya", "noah", "leo" })
    foreach (var facing in Enum.GetValues(typeof(Facing)).Cast<Facing>())
    {
        Assert.That(catalog.Character(id, facing, false), Is.Not.Null);
        Assert.That(catalog.Character(id, facing, true), Is.Not.Null);
    }
    CollectionAssert.IsSubsetOf(
        new[] { "street", "garage", "diner", "rooftop", "gallery", "electronics", "station_wagon" },
        catalog.Ids);
}
```

- [ ] **Step 2: Run the focused EditMode test and record RED**

Run: `Unity -batchmode -projectPath .worktrees/northbound-unity -runTests -testPlatform EditMode -testFilter Northbound.Tests.ArtCatalogTests -testResults /private/tmp/northbound-art-catalog-red.xml -logFile /private/tmp/northbound-art-catalog-red.log`

Expected: compilation failure because `NorthboundArtCatalog` and `Facing` do not exist.

- [ ] **Step 3: Implement the typed catalog and manifest**

```csharp
public enum Facing { North, South, East, West }

[CreateAssetMenu(menuName = "Northbound/Art Catalog")]
public sealed class NorthboundArtCatalog : ScriptableObject
{
    [Serializable] public sealed class CharacterSet { public string id; public Sprite southIdle, northIdle, eastIdle, westIdle; public Sprite southWalk, northWalk, eastWalk, westWalk; }
    [Serializable] public sealed class NamedSprite { public string id; public Sprite sprite; }
    [SerializeField] private CharacterSet[] characters = Array.Empty<CharacterSet>();
    [SerializeField] private NamedSprite[] props = Array.Empty<NamedSprite>();
    [SerializeField] private NamedSprite[] environments = Array.Empty<NamedSprite>();
    public Sprite Character(string id, Facing facing, bool walking) { /* exact facing lookup; null only for unknown id */ }
    public Sprite Prop(string id) { /* exact id lookup */ }
    public Sprite Environment(string id) { /* exact id lookup */ }
}
```

Record each generated asset's source reference, purpose, output dimensions, import settings and future replacement slot in `art-asset-manifest.md`.

- [ ] **Step 4: Generate and import art assets**

Generate the five character sheets, wagon sheet, and six named location/prop atlases using the approved model sheets as visual references. Each character sheet has a transparent or chroma-key background, a four-cell idle row and four-cell walk row in North/South/East/West order. Remove chroma key if needed, validate alpha, then import as multiple sprites. Configure point/bilinear filtering intentionally, 64 pixels per unit, transparent background, no mipmaps, pivot `(0.5, 0.18)` for characters, and deterministic sprite names matching the catalog.

- [ ] **Step 5: Run focused test to verify GREEN and commit**

Run the command from Step 2 and require all assertions to pass. Commit only catalog, source, art assets, metadata, tests, and manifest with `feat: add Northbound top-down art catalog`.

## Task 2: Five Directional Character Prefabs

**Files:**
- Create: `Assets/Northbound/Scripts/Art/TopDownCharacterVisual.cs`
- Modify: `Assets/Northbound/Prefabs/Characters/Jamie.prefab`
- Modify: `Assets/Northbound/Prefabs/Characters/Elias.prefab`
- Modify: `Assets/Northbound/Prefabs/Characters/Maya.prefab`
- Modify: `Assets/Northbound/Prefabs/Characters/Noah.prefab`
- Modify: `Assets/Northbound/Prefabs/Characters/Leo.prefab`
- Create: `Assets/Northbound/Tests/PlayMode/CharacterVisualPlayModeTests.cs`

**Interfaces:**
- Consumes `NorthboundArtCatalog.Character` and `Facing` from Task 1.
- Produces `TopDownCharacterVisual.Configure(string characterId, NorthboundArtCatalog catalog)` and `CurrentFacing`.
- `GreybridgeWorldLayout.EnsureVisibleCharacter` must use this component instead of creating a Quad.

- [ ] **Step 1: Write failing directional runtime test**

```csharp
[UnityTest]
public IEnumerator JamieVisual_ChangesFromSouthIdleToEastWalk()
{
    var jamie = Object.FindFirstObjectByType<PlayerMotor>().gameObject;
    var visual = jamie.GetComponentInChildren<TopDownCharacterVisual>();
    var before = visual.CurrentSprite;
    jamie.GetComponent<Rigidbody2D>().linearVelocity = Vector2.right;
    yield return null;
    Assert.That(visual.CurrentFacing, Is.EqualTo(Facing.East));
    Assert.That(visual.CurrentSprite, Is.Not.EqualTo(before));
}
```

- [ ] **Step 2: Run focused PlayMode test and record RED**

Run: `Unity -batchmode -projectPath .worktrees/northbound-unity -runTests -testPlatform PlayMode -testFilter Northbound.Tests.CharacterVisualPlayModeTests -testResults /private/tmp/northbound-character-red.xml -logFile /private/tmp/northbound-character-red.log`

Expected: `TopDownCharacterVisual` does not exist or Jamie still has no SpriteRenderer.

- [ ] **Step 3: Implement movement-driven visual selection**

```csharp
public sealed class TopDownCharacterVisual : MonoBehaviour
{
    public Facing CurrentFacing { get; private set; } = Facing.South;
    public Sprite CurrentSprite => renderer.sprite;
    public void Configure(string id, NorthboundArtCatalog catalog) { /* bind exactly one child SpriteRenderer and shadow */ }
    private void LateUpdate()
    {
        var velocity = body == null ? Vector2.zero : body.linearVelocity;
        if (velocity.sqrMagnitude > 0.0025f) CurrentFacing = FacingFrom(velocity);
        renderer.sprite = catalog.Character(id, CurrentFacing, velocity.sqrMagnitude > 0.0025f);
    }
}
```

Use a child shadow sprite beneath each character and explicit sorting orders: shadow `20`, character `30`, interaction outline `40`.

- [ ] **Step 4: Apply the same visual prefab composition to all five adults**

Each prefab has exactly one `SpriteRenderer`, one `TopDownCharacterVisual`, and no renderer based on `PrimitiveType.Quad`. Preserve all existing narrative/interactor scripts and stable prefab names.

- [ ] **Step 5: Run focused test to verify GREEN and commit**

Run the Step 2 command; also assert the four NPC visuals each resolve a distinct south idle sprite. Commit with `feat: add directional character visuals`.

## Task 3: Textured Greybridge Composition

**Files:**
- Create: `Assets/Northbound/Scripts/Art/GreybridgeArtBuilder.cs`
- Modify: `Assets/Northbound/Scripts/World/GreybridgeWorldLayout.cs`
- Create: `Assets/Northbound/Tests/PlayMode/GreybridgeArtPlayModeTests.cs`

**Interfaces:**
- Consumes `NorthboundArtCatalog.Environment` and `Prop` from Task 1.
- Produces `GreybridgeArtBuilder.Build(Transform parent)` and a named visual root for each primary area.
- Does not create colliders, change positions of existing triggers, or alter `WalkableBounds`.

- [ ] **Step 1: Write failing visual-root and primitive-proxy test**

```csharp
[UnityTest]
public IEnumerator Greybridge_BuildsTexturedVisualRootsWithoutPrimitiveQuads()
{
    yield return SceneManager.LoadSceneAsync("Greybridge", LoadSceneMode.Single);
    foreach (var name in new[] { "Art Street", "Art Garage", "Art Diner", "Art Rooftop", "Art Gallery", "Art Electronics" })
        Assert.That(GameObject.Find(name), Is.Not.Null);
    Assert.That(Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
        .Any(r => r.gameObject.name == "Visible Character Proxy"), Is.False);
}
```

- [ ] **Step 2: Run focused PlayMode test and record RED**

Run: `Unity -batchmode -projectPath .worktrees/northbound-unity -runTests -testPlatform PlayMode -testFilter Northbound.Tests.GreybridgeArtPlayModeTests -testResults /private/tmp/northbound-art-world-red.xml -logFile /private/tmp/northbound-art-world-red.log`

Expected: named art roots are absent and old MeshRenderer primitive proxies are present.

- [ ] **Step 3: Implement layered art builder**

```csharp
public sealed class GreybridgeArtBuilder : MonoBehaviour
{
    public void Build(Transform mapRoot)
    {
        CreateLayer(mapRoot, "Art Street", "street", new Vector2(-2f, 0f), 0);
        CreateLayer(mapRoot, "Art Garage", "garage", new Vector2(-22f, -4f), 2);
        CreateLayer(mapRoot, "Art Diner", "diner", new Vector2(-7f, 3f), 2);
        CreateLayer(mapRoot, "Art Rooftop", "rooftop", new Vector2(23f, 9f), 2);
        CreateLayer(mapRoot, "Art Gallery", "gallery", new Vector2(13f, 5f), 2);
        CreateLayer(mapRoot, "Art Electronics", "electronics", new Vector2(10f, -2f), 2);
        CreateProp(mapRoot, "Art Station Wagon", "station_wagon", new Vector2(-20f, -4f), 15);
    }
}
```

`CreateLayer` must add a SpriteRenderer using the catalog sprite, set `sortingOrder`, and never add a collider. Add named quest-object prop sprites at the existing `ObjectivePosition` coordinates and environmental change props at the existing fact-marker coordinates. Replace `CreateArea`, `CreateMarker`, `CreateMarkerForFact`, and `EnsureVisibleCharacter` rendering paths so their logical trigger objects remain but their visible primitive quads are disabled or omitted.

- [ ] **Step 4: Add chapter-state visual changes**

Bind visible shutdown traces to the existing facts `missed_first_light`, `missed_static`, `missed_alternator`, `missed_pack_trunk`, `missed_road_test`, and `missed_last_night_open`. The art changes must occur through the existing `WorldFactBinding` lifecycle, so save/restore keeps them correct.

- [ ] **Step 5: Run focused test to verify GREEN and commit**

Run the Step 2 command; assert garage art includes wagon sprite, diner art includes booth/counter sprite, and rooftop art includes parapet/chair/map props. Commit with `feat: replace Greybridge proxy art`.

## Task 4: End-to-End Visual Validation and Release Handoff

**Files:**
- Modify: `Assets/Northbound/Tests/PlayMode/GreybridgeArtPlayModeTests.cs`
- Modify: `docs/production/art-asset-manifest.md`
- Create: `docs/production/art-review-checklist.md`

**Interfaces:**
- Consumes runtime art roots, character visual component, existing gameplay test scenes, and Task12 release acceptance scaffold.
- Produces screenshots and a human review checklist; no gameplay API changes.

- [ ] **Step 1: Write failing end-to-end art contract test**

```csharp
[UnityTest]
public IEnumerator PrimaryWorldArt_IsResolvableDuringRealBootstrapFlow()
{
    yield return BootstrapIntoGreybridgeAndDismissOpeningCinematic();
    Assert.That(Object.FindFirstObjectByType<TopDownCharacterVisual>(), Is.Not.Null);
    Assert.That(Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Length, Is.GreaterThan(35));
    Assert.That(Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
        .Any(r => r.gameObject.name.Contains("Proxy")), Is.False);
}
```

- [ ] **Step 2: Run focused PlayMode test and record RED**

Run: `Unity -batchmode -projectPath .worktrees/northbound-unity -runTests -testPlatform PlayMode -testFilter Northbound.Tests.GreybridgeArtPlayModeTests.PrimaryWorldArt_IsResolvableDuringRealBootstrapFlow -testResults /private/tmp/northbound-art-e2e-red.xml -logFile /private/tmp/northbound-art-e2e-red.log`

Expected: failure until all primary art roots and character visuals are wired into real Bootstrap flow.

- [ ] **Step 3: Capture representative screenshots**

In an isolated save session, capture 1920×1080 screenshots after scene load for Garage, Diner, Rooftop, and Street. Store them under `docs/production/screenshots/` and link them from the manifest. The capture script must not write to the user's default save path.

- [ ] **Step 4: Write the human review checklist**

The checklist must have explicit yes/no entries for: each character's outfit identifiers, sprite readability during movement, garage/diner/rooftop recognition without labels, wagon identity, no visible squares, CGI lighting continuity, no collider/quest regression, and 1080p visual legibility.

- [ ] **Step 5: Run green suite, then resume release work**

Run the focused art suite, full EditMode suite, and full PlayMode suite. Require zero failures, compiler errors, missing-script warnings, or unexpected Error/Assert logs. Commit with `test: verify Northbound top-down art`. Then resume Task12 from its preserved release-acceptance scaffold, capture its valid RED, and continue the macOS release plan.

## Self-Review

- Design coverage: Tasks 1–4 cover the catalog, five adult characters, four-direction movement, wagon, every required primary area, lighting/props, fact-driven world changes, visual smoke, screenshots, and human review. Existing narrative and runtime constraints are preserved in each task.
- No-placeholder scan: all generated-art asset classes, names, visual IDs, test methods, run commands, and change boundaries are explicit. The plan deliberately records human approval only where subjective judgment cannot be automated.
- Type consistency: `Facing`, `NorthboundArtCatalog`, `TopDownCharacterVisual`, and `GreybridgeArtBuilder` are declared in Task 1–3 before Task 4 consumes them; all catalog lookup names use the same exact strings.

## Execution Choice

The user explicitly authorized immediate, quota-conscious work. Execute inline in this session: use `superpowers:executing-plans`, complete tasks in order, maintain TDD evidence, and keep user-facing updates brief.

## Task 5: 1080p UI Readability and Real Interaction Verification

**Files:**
- Modify: `Assets/Northbound/Scripts/Minigames/MinigameController.cs`
- Modify: `Assets/Northbound/Scripts/Minigames/DinerShiftGame.cs`
- Modify: `Assets/Northbound/Scripts/Minigames/WiringGame.cs`
- Modify: `Assets/Northbound/Scripts/Minigames/TrunkPackingGame.cs`
- Modify: `Assets/Northbound/Prefabs/UI/DialogueView.prefab`
- Modify: `Assets/Northbound/Prefabs/UI/InteractionPrompt.prefab`
- Modify: `Assets/Northbound/Tests/PlayMode/MinigameTests.cs`
- Create: `Assets/Northbound/Tests/PlayMode/InteractionReadabilityPlayModeTests.cs`

**Interfaces:**
- Produces `MinigameController.CreateStatusLabel()` and `SetStatus(string, Color)` for live feedback.
- Produces a Diner selected-order state that is visible and reports either a successful delivery or a clear mismatch message.
- Consumes existing `PlayerInteractor.TryInteract()`, `NarrativeObjectiveTrigger`, and minigame `BeginActive()` APIs without bypassing them.

- [ ] **Step 1: Write failing player-facing input and readability tests**

```csharp
[UnityTest]
public IEnumerator DinerShift_KeyboardSelectionAndTableDeliveryUpdateVisibleStatus()
{
    var game = StartConfiguredDinerShift();
    Press(Key.Digit1); yield return null;
    Assert.That(game.VisibleStatus, Does.Contain("Coffee selected"));
    Press(Key.Q); yield return null;
    Assert.That(game.VisibleStatus, Does.Contain("Coffee delivered"));
}

[UnityTest]
public IEnumerator EveryInteractiveQuestObject_HasAVisibleSpriteAndEPath()
{
    yield return BootstrapIntoGreybridgeAndDismissOpeningCinematic();
    foreach (var objective in Object.FindObjectsByType<NarrativeObjectiveTrigger>(FindObjectsSortMode.None))
    {
        Assert.That(objective.GetComponentInChildren<SpriteRenderer>(), Is.Not.Null, objective.name);
        MoveJamieIntoRange(objective.transform.position);
        Assert.That(Object.FindFirstObjectByType<PlayerInteractor>().CurrentInteractable, Is.Not.Null, objective.name);
    }
}
```

- [ ] **Step 2: Run focused PlayMode suite and record RED**

Run: `Unity -batchmode -projectPath .worktrees/northbound-unity -runTests -testPlatform PlayMode -testFilter Northbound.Tests.InteractionReadabilityPlayModeTests -testResults /private/tmp/northbound-interaction-readability-red.xml -logFile /private/tmp/northbound-interaction-readability-red.log`

Expected: missing visible-status API and objective SpriteRenderers.

- [ ] **Step 3: Implement feedback and reliable input paths**

Add a readable high-contrast live status row below every minigame instruction. For Diner, update it on order selection, matching delivery, mismatched table, completion and retry. Keep the 1920×1080 CanvasScaler reference but use a 34px minimum status font, 32px button text, and a visible selected-button state. Keyboard tests must inject real Input System key presses and invoke the real component update; mouse tests must invoke each Button's pointer-click handler.

- [ ] **Step 4: Bind art props to every physical objective**

`GreybridgeArtBuilder` must attach a child SpriteRenderer and contact shadow to every `NarrativeObjectiveTrigger` and carry-choice object. The rendered prop shares its parent transform, leaves the trigger collider untouched, and uses sorting order 35 so it remains visible above ground but below the player.

- [ ] **Step 5: Verify GREEN and commit**

Run the focused suite in Step 2 plus `Northbound.Tests.MinigameTests`, assert button and keyboard routes both complete the Diner sequence, and commit with `fix: make minigames and quest objects readable`.
