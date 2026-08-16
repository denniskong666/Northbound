# Northbound Unity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a polished, English-language, 45–60 minute macOS-playable version of *Northbound* with top-down 2.5D exploration, mutually exclusive narrative missions, invisible state tracking, pre-rendered cinematics, and four player-selected ending directions.

**Architecture:** Use one persistent `Bootstrap` scene for services and UI, plus one additive `Greybridge` gameplay scene whose chapter-specific objects are activated from narrative facts. Keep narrative state, quest evaluation, dialogue presentation, cinematics, and world presentation in separate focused modules. Author content as ScriptableObjects so plot changes do not require changes to system code.

**Tech Stack:** Unity 6.3 LTS (`6000.3.17f1`), Universal 2D project template, C#/.NET Standard 2.1, Unity Input System, Unity Test Framework, TextMeshPro, Tilemap, `VideoPlayer`, JSON save files, Git LFS for production media.

## Global Constraints

- Target one complete English playthrough of 45–60 minutes.
- Ship macOS first; keep input actions suitable for later Windows and gamepad builds.
- Use stylized 2D/2.5D gameplay and four cinematic playback slots backed by six produced video files.
- Include no combat, romance route, online service, procedural quest, open-world driving, or visible morality meter.
- Keep all four core ending directions and all three friend variants reachable in every playthrough.
- Hidden state may alter tone and detail but must never select the ending for the player.
- Ordinary dialogue uses English text, portraits, and short vocal reactions; cinematics use English voice and subtitles.
- Every cinematic must be skippable and must restore the correct narrative, audio, camera, and input state.
- Use proxy art and silent proxy videos until their production replacements pass the same import contract.
- Do not commit `Library/`, `Temp/`, `Logs/`, `Obj/`, `Builds/`, or local user settings.
- Store video, production audio, and layered art through Git LFS.
- Source narrative authority: `docs/superpowers/specs/2026-08-13-northbound-narrative-design.md`.

## Planned File Structure

```text
Assets/Northbound/
  Art/{Characters,Environment,Portraits,UI}/
  Audio/{Music,SFX,Voices}/
  Cinematics/{Opening,Highlights,Rooftop,Finale}/
  Data/{Chapters,Dialogue,Endings,Quests}/
  Prefabs/{Characters,Interactables,Triggers,UI,World}/
  Scenes/{Bootstrap.unity,Greybridge.unity,TestSandbox.unity}/
  Scripts/
    Core/{GameBootstrap.cs,GameFlowController.cs,InputGate.cs,SceneIds.cs}
    Narrative/{NarrativeState.cs,NarrativeFact.cs,NarrativeStateStore.cs,SaveGameService.cs}
    Dialogue/{DialogueAsset.cs,DialogueLine.cs,DialogueRunner.cs,DialogueView.cs}
    Interaction/{IInteractable.cs,PlayerInteractor.cs,InteractionPromptView.cs}
    Player/{PlayerMotor.cs,FollowCamera.cs}
    Quests/{QuestAsset.cs,QuestObjective.cs,QuestRunner.cs,MissionPairController.cs}
    World/{ChapterWorldController.cs,ChapterVariant.cs,WorldFactBinding.cs}
    Minigames/{MinigameController.cs,DinerShiftGame.cs,WiringGame.cs,TrunkPackingGame.cs}
    Cinematics/{CinematicAsset.cs,CinematicPlayer.cs}
    Endings/{EndingDirection.cs,EndingResolver.cs,EndingTrigger.cs}
    UI/{PauseController.cs,SettingsModel.cs,SubtitleView.cs}
  Tests/
    EditMode/{NarrativeStateTests.cs,SaveGameServiceTests.cs,QuestRunnerTests.cs,EndingResolverTests.cs}
    PlayMode/{PlayerInteractionTests.cs,MissionPairPlayModeTests.cs,CinematicPlayerTests.cs,FullPathSmokeTests.cs}
  Northbound.Runtime.asmdef
  Tests/EditMode/Northbound.EditModeTests.asmdef
  Tests/PlayMode/Northbound.PlayModeTests.asmdef
Packages/{manifest.json,packages-lock.json}
ProjectSettings/{ProjectVersion.txt,EditorBuildSettings.asset}
```

---

### Task 1: Create the Unity project and automated test entry point

**Files:**
- Create: `.gitignore`
- Create: `.gitattributes`
- Create: `Assets/Northbound/Scenes/Bootstrap.unity`
- Create: `Assets/Northbound/Scenes/Greybridge.unity`
- Create: `Assets/Northbound/Scenes/TestSandbox.unity`
- Create: `Assets/Northbound/Scripts/Core/SceneIds.cs`
- Create: `Assets/Northbound/Scripts/Core/GameBootstrap.cs`
- Create: `Assets/Northbound/Tests/EditMode/BootstrapTests.cs`
- Create: `Assets/Northbound/Northbound.Runtime.asmdef`
- Create: `Assets/Northbound/Tests/EditMode/Northbound.EditModeTests.asmdef`
- Create: `Assets/Northbound/Tests/PlayMode/Northbound.PlayModeTests.asmdef`
- Modify: `ProjectSettings/ProjectVersion.txt`
- Modify: `ProjectSettings/EditorBuildSettings.asset`

**Interfaces:**
- Produces: `SceneIds.Bootstrap`, `SceneIds.Greybridge`, and singleton `GameBootstrap.Instance` used by all later runtime services.

- [ ] **Step 1: Install and create the project**

Install Unity `6000.3.17f1` through Unity Hub with macOS Build Support. Create a Universal 2D project directly in this repository, keeping the existing `docs/` directory. Set company name to `Northbound Team`, product name to `Northbound`, default resolution to `1920×1080`, windowed mode, and color space to Linear.

- [ ] **Step 2: Add repository exclusions and LFS rules**

Use the standard Unity `.gitignore`. Add these exact LFS patterns to `.gitattributes`:

```gitattributes
*.mp4 filter=lfs diff=lfs merge=lfs -text
*.mov filter=lfs diff=lfs merge=lfs -text
*.wav filter=lfs diff=lfs merge=lfs -text
*.psd filter=lfs diff=lfs merge=lfs -text
*.tif filter=lfs diff=lfs merge=lfs -text
```

- [ ] **Step 3: Write the failing bootstrap test**

```csharp
using NUnit.Framework;
using Northbound.Core;

public sealed class BootstrapTests
{
    [Test]
    public void SceneIds_AreStable()
    {
        Assert.That(SceneIds.Bootstrap, Is.EqualTo("Bootstrap"));
        Assert.That(SceneIds.Greybridge, Is.EqualTo("Greybridge"));
    }
}
```

- [ ] **Step 4: Run the test and confirm failure**

Run:

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.3.17f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode -testResults /tmp/northbound-editmode.xml -quit
```

Expected: test compilation fails because `Northbound.Core.SceneIds` does not exist.

- [ ] **Step 5: Implement the bootstrap contract**

```csharp
namespace Northbound.Core
{
    public static class SceneIds
    {
        public const string Bootstrap = "Bootstrap";
        public const string Greybridge = "Greybridge";
    }
}
```

Create `GameBootstrap` as a duplicate-safe `DontDestroyOnLoad` component. Put it in `Bootstrap.unity`; add `Bootstrap` and then `Greybridge` to Build Settings. Create `TestSandbox.unity` outside Build Settings.

- [ ] **Step 6: Run EditMode tests and perform a clean import**

Expected: tests pass, all three scenes open without Console errors, and entering Play Mode from `Bootstrap` loads `Greybridge` once.

- [ ] **Step 7: Commit**

```bash
git add .gitignore .gitattributes Assets Packages ProjectSettings
git commit -m "build: scaffold Northbound Unity project"
```

### Task 2: Implement narrative facts, session state, and save data

**Files:**
- Create: `Assets/Northbound/Scripts/Narrative/NarrativeFact.cs`
- Create: `Assets/Northbound/Scripts/Narrative/NarrativeState.cs`
- Create: `Assets/Northbound/Scripts/Narrative/NarrativeStateStore.cs`
- Create: `Assets/Northbound/Scripts/Narrative/SaveGameService.cs`
- Create: `Assets/Northbound/Tests/EditMode/NarrativeStateTests.cs`
- Create: `Assets/Northbound/Tests/EditMode/SaveGameServiceTests.cs`
- Modify: `Assets/Northbound/Scripts/Core/GameBootstrap.cs`

**Interfaces:**
- Produces: `NarrativeStateStore.Has(string)`, `Set(string, bool)`, `Add(string, int)`, `GetInt(string)`, `Reset()` and `Changed` event.
- Produces: `SaveGameService.Save(NarrativeState)`, `LoadOrNew()`, and `Delete()`.
- Consumes: `GameBootstrap` lifecycle from Task 1.

- [ ] **Step 1: Write failing fact and serialization tests**

```csharp
[Test]
public void FactsAndCounters_RoundTrip()
{
    var state = new NarrativeState();
    state.Set("attended_maya_exhibition", true);
    state.Add("bond_maya", 2);
    string json = state.ToJson();
    var loaded = NarrativeState.FromJson(json);
    Assert.That(loaded.Has("attended_maya_exhibition"), Is.True);
    Assert.That(loaded.GetInt("bond_maya"), Is.EqualTo(2));
}
```

Also test that absent facts return `false`, absent counters return `0`, repeated `Set` calls are idempotent, and corrupt JSON returns a new state rather than throwing.

- [ ] **Step 2: Run tests and confirm failure**

Expected: compilation fails because `NarrativeState` is undefined.

- [ ] **Step 3: Implement state with serializable entries**

```csharp
[Serializable]
public sealed class IntEntry
{
    public string Id;
    public int Value;
    public IntEntry(string id, int value) { Id = id; Value = value; }
}

[Serializable]
public sealed class NarrativeState
{
    [SerializeField] private List<string> facts = new();
    [SerializeField] private List<IntEntry> counters = new();

    public bool Has(string id) => facts.Contains(id);
    public void Set(string id, bool value)
    {
        if (value && !facts.Contains(id)) facts.Add(id);
        if (!value) facts.Remove(id);
    }

    public int GetInt(string id) => counters.Find(x => x.Id == id)?.Value ?? 0;

    public void Add(string id, int amount)
    {
        var entry = counters.Find(x => x.Id == id);
        if (entry == null) counters.Add(new IntEntry(id, amount));
        else entry.Value += amount;
    }

    public string ToJson() => JsonUtility.ToJson(this);

    public static NarrativeState FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new NarrativeState();
        try
        {
            var state = JsonUtility.FromJson<NarrativeState>(json);
            if (state == null) return new NarrativeState();
            state.facts ??= new List<string>();
            state.counters ??= new List<IntEntry>();
            return state;
        }
        catch (ArgumentException)
        {
            return new NarrativeState();
        }
    }
}
```

Implement save path as `Application.persistentDataPath/northbound-save.json`; write to `*.tmp`, then atomically replace the live file. Save only at chapter boundaries, mission commitments, and ending commitment.

- [ ] **Step 4: Run all EditMode tests**

Expected: fact, counter, new-save, round-trip, deletion, and corrupt-save tests pass.

- [ ] **Step 5: Commit**

```bash
git add Assets/Northbound/Scripts/Narrative Assets/Northbound/Tests/EditMode Assets/Northbound/Scripts/Core/GameBootstrap.cs
git commit -m "feat: add narrative state and save service"
```

### Task 3: Add player movement, camera, and interaction gating

**Files:**
- Create: `Assets/Northbound/Scripts/Core/InputGate.cs`
- Create: `Assets/Northbound/Scripts/Player/PlayerMotor.cs`
- Create: `Assets/Northbound/Scripts/Player/FollowCamera.cs`
- Create: `Assets/Northbound/Scripts/Interaction/IInteractable.cs`
- Create: `Assets/Northbound/Scripts/Interaction/PlayerInteractor.cs`
- Create: `Assets/Northbound/Scripts/Interaction/InteractionPromptView.cs`
- Create: `Assets/Northbound/Prefabs/Characters/Jamie.prefab`
- Create: `Assets/Northbound/Prefabs/UI/InteractionPrompt.prefab`
- Create: `Assets/Northbound/Tests/PlayMode/PlayerInteractionTests.cs`
- Modify: `Assets/Northbound/Scenes/TestSandbox.unity`

**Interfaces:**
- Produces: `InputGate.Acquire(object owner): IDisposable` for dialogue, minigames, pause, and cinematics.
- Produces: `IInteractable.Prompt`, `CanInteract`, and `Interact(GameObject actor)`.
- Produces: `PlayerMotor.SetMoveInput(Vector2)` for deterministic tests.

- [ ] **Step 1: Write failing PlayMode tests**

Test that Jamie moves on unobstructed ground, stops at a `Collider2D`, shows the closest interaction prompt inside range, invokes that interactable once, and cannot move while an `InputGate` lease exists.

```csharp
[UnityTest]
public IEnumerator InputLease_DisablesMovementUntilDisposed()
{
    var gate = new GameObject().AddComponent<InputGate>();
    var motor = TestFactory.CreatePlayer(gate);
    using (gate.Acquire(this))
    {
        motor.SetMoveInput(Vector2.right);
        yield return new WaitForFixedUpdate();
        Assert.That(motor.transform.position.x, Is.EqualTo(0f).Within(.01f));
    }
}
```

- [ ] **Step 2: Run PlayMode tests and confirm failure**

Expected: compilation fails because the player and input contracts do not exist.

- [ ] **Step 3: Implement input and movement**

Use a `Rigidbody2D` with zero gravity, frozen rotation, and `MovePosition` in `FixedUpdate`. Read a `Move` action and an `Interact` action from the Input System. Normalize diagonal speed. Use `Physics2D.OverlapCircleNonAlloc` to select the closest enabled `IInteractable`.

```csharp
public interface IInteractable
{
    string Prompt { get; }
    bool CanInteract { get; }
    void Interact(GameObject actor);
}
```

The follow camera uses damped position tracking and fixed orthographic size; it does not rotate during play.

- [ ] **Step 4: Build the sandbox and verify controls**

Add floor, four walls, one interactable cube, Jamie, camera, and prompt UI. Verify WASD, arrow keys, interaction, collision, and prompt selection.

- [ ] **Step 5: Run tests and commit**

```bash
git add Assets/Northbound/Scripts/Core/InputGate.cs Assets/Northbound/Scripts/Player Assets/Northbound/Scripts/Interaction Assets/Northbound/Prefabs Assets/Northbound/Scenes/TestSandbox.unity Assets/Northbound/Tests/PlayMode
git commit -m "feat: add top-down movement and interaction"
```

### Task 4: Build the data-driven dialogue system

**Files:**
- Create: `Assets/Northbound/Scripts/Dialogue/DialogueLine.cs`
- Create: `Assets/Northbound/Scripts/Dialogue/DialogueChoice.cs`
- Create: `Assets/Northbound/Scripts/Dialogue/DialogueAsset.cs`
- Create: `Assets/Northbound/Scripts/Dialogue/DialogueRunner.cs`
- Create: `Assets/Northbound/Scripts/Dialogue/DialogueView.cs`
- Create: `Assets/Northbound/Prefabs/UI/DialogueView.prefab`
- Create: `Assets/Northbound/Tests/EditMode/DialogueRunnerTests.cs`
- Modify: `Assets/Northbound/Scripts/Core/GameBootstrap.cs`

**Interfaces:**
- Produces: `DialogueRunner.Start(DialogueAsset)`, `Advance()`, `Choose(int)`, `IsRunning`, and `Completed`.
- Produces: `DialogueLine` with `speakerId`, `text`, `portrait`, `reactionClip`, `requiredFact`, and `grantedFact`.
- Consumes: `NarrativeStateStore` from Task 2 and `InputGate` from Task 3.

- [ ] **Step 1: Write failing branching tests**

Create an in-memory dialogue containing a normal line, a line requiring `helped_noah`, and a choice granting `jamie_uncertain`. Assert that unmet lines are skipped, a chosen fact is granted, and completion releases input.

- [ ] **Step 2: Run tests and confirm failure**

Expected: compilation fails because `DialogueRunner` and dialogue data types are undefined.

- [ ] **Step 3: Implement the smallest dialogue state machine**

```csharp
[CreateAssetMenu(menuName = "Northbound/Dialogue")]
public sealed class DialogueAsset : ScriptableObject
{
    public string id;
    public List<DialogueLine> lines;
}

public sealed class DialogueRunner
{
    public void Start(DialogueAsset asset);
    public DialogueLine Current { get; }
    public bool Advance();
    public bool Choose(int index);
}
```

The runtime never parses story logic from prose. Facts and next-line indices are explicit serialized fields. The view renders speaker, portrait, text, up to four tone choices, continue indicator, and optional short reaction audio. It supports keyboard, mouse, and gamepad navigation.

- [ ] **Step 4: Author and test one approved exchange**

Create `Assets/Northbound/Data/Dialogue/Ch1_GarageSchedule.asset` containing:

> Elias: “Friday. Six in the morning. No speeches, no delays.”  
> Maya: “You just gave a speech.”  
> Elias: “That was a schedule.”

Verify the player stops, text wraps at 1920×1080 and 1280×720, and input resumes after completion.

- [ ] **Step 5: Run tests and commit**

```bash
git add Assets/Northbound/Scripts/Dialogue Assets/Northbound/Prefabs/UI/DialogueView.prefab Assets/Northbound/Data/Dialogue Assets/Northbound/Tests/EditMode Assets/Northbound/Scripts/Core/GameBootstrap.cs
git commit -m "feat: add data-driven dialogue system"
```

### Task 5: Implement quests and mutually exclusive mission commitment

**Files:**
- Create: `Assets/Northbound/Scripts/Quests/QuestObjective.cs`
- Create: `Assets/Northbound/Scripts/Quests/QuestAsset.cs`
- Create: `Assets/Northbound/Scripts/Quests/QuestRunner.cs`
- Create: `Assets/Northbound/Scripts/Quests/MissionPairController.cs`
- Create: `Assets/Northbound/Prefabs/UI/QuestHint.prefab`
- Create: `Assets/Northbound/Tests/EditMode/QuestRunnerTests.cs`
- Create: `Assets/Northbound/Tests/PlayMode/MissionPairPlayModeTests.cs`

**Interfaces:**
- Produces: `QuestRunner.StartQuest(QuestAsset)`, `Report(string objectiveId, int amount)`, and `CompleteQuest(string questId)`.
- Produces: `MissionPairController.TryCommit(string questId): bool` and `CommittedQuestId`.
- Consumes: `NarrativeStateStore`, `DialogueRunner`, and `SaveGameService`.

- [ ] **Step 1: Write failing quest tests**

Test sequential objective completion, duplicate report idempotency, completion facts, and mutual exclusion:

```csharp
[Test]
public void Commit_FirstMissionLocksItsPair()
{
    var pair = new MissionPairController("alternator", "first_light", state);
    Assert.That(pair.TryCommit("first_light"), Is.True);
    Assert.That(pair.TryCommit("alternator"), Is.False);
    Assert.That(state.Has("missed_alternator"), Is.True);
}
```

- [ ] **Step 2: Run tests and confirm failure**

Expected: quest types are missing.

- [ ] **Step 3: Implement quests as facts plus progress**

Each quest asset contains ID, title, optional hint, ordered objectives, completion facts, and next quest IDs. Mission pairs contain two quest IDs, one neutral commitment message—`This will take the rest of the evening.`—and explicit missed-state facts. Starting a commitment writes the save immediately.

- [ ] **Step 4: Build the Elias/Maya pair in TestSandbox**

Create two spatially separated triggers. Entering one displays the neutral commitment prompt. Confirm that backing out keeps both available, confirming locks the other, and reloading preserves the lock.

- [ ] **Step 5: Run all quest tests and commit**

```bash
git add Assets/Northbound/Scripts/Quests Assets/Northbound/Prefabs/UI/QuestHint.prefab Assets/Northbound/Tests Assets/Northbound/Data/Quests
git commit -m "feat: add quests and conflicting missions"
```

### Task 6: Build chapter flow and the changing Greybridge world

**Files:**
- Create: `Assets/Northbound/Scripts/Core/GameFlowController.cs`
- Create: `Assets/Northbound/Scripts/World/ChapterVariant.cs`
- Create: `Assets/Northbound/Scripts/World/ChapterWorldController.cs`
- Create: `Assets/Northbound/Scripts/World/WorldFactBinding.cs`
- Create: `Assets/Northbound/Data/Chapters/Prologue.asset`
- Create: `Assets/Northbound/Data/Chapters/Chapter1.asset`
- Create: `Assets/Northbound/Data/Chapters/Chapter2.asset`
- Create: `Assets/Northbound/Data/Chapters/Chapter3Day3.asset`
- Create: `Assets/Northbound/Data/Chapters/Chapter3Day2.asset`
- Create: `Assets/Northbound/Data/Chapters/Chapter4.asset`
- Create: `Assets/Northbound/Data/Chapters/Finale.asset`
- Create: `Assets/Northbound/Tests/EditMode/ChapterWorldControllerTests.cs`
- Modify: `Assets/Northbound/Scenes/Greybridge.unity`

**Interfaces:**
- Produces: `GameFlowController.EnterChapter(string chapterId)` and `CurrentChapterId`.
- Produces: `WorldFactBinding.Refresh(NarrativeState)`.
- Consumes: quest completion events and narrative facts.

- [ ] **Step 1: Write failing world-state tests**

Test that Chapter 1 activates the open diner and market, Chapter 2 adds `FINAL WEEK`, Chapter 4 activates dark storefronts, and Finale activates all four direction regions.

- [ ] **Step 2: Run tests and confirm failure**

Expected: world controller types are missing.

- [ ] **Step 3: Implement chapter variants**

`ChapterVariant` contains an ID, required facts, forbidden facts, objects to activate, objects to deactivate, spawn point ID, ambient snapshot ID, and starting quest IDs. `ChapterWorldController` applies exactly one base chapter variant plus any fact bindings.

- [ ] **Step 4: Greybox the complete map**

Build the Old Neighborhood, Vale Auto Garage, and Rooftop Overlook in a single scene using simple production-sized geometry. Include all walkways, collision, NPC anchors, camera bounds, mission trigger zones, and finale branches. Do not decorate before a full traversal proves the layout.

- [ ] **Step 5: Verify navigation and chapter transitions**

Walk from every chapter spawn to every required mission in under 45 seconds. Confirm no collider traps, hidden exits, or camera exposures outside the map. Confirm entering a chapter saves and respawns correctly.

- [ ] **Step 6: Run tests and commit**

```bash
git add Assets/Northbound/Scripts/Core/GameFlowController.cs Assets/Northbound/Scripts/World Assets/Northbound/Data/Chapters Assets/Northbound/Scenes/Greybridge.unity Assets/Northbound/Tests/EditMode
git commit -m "feat: add chapter flow and changing world"
```

### Task 7: Implement the three reusable narrative minigames

**Files:**
- Create: `Assets/Northbound/Scripts/Minigames/MinigameController.cs`
- Create: `Assets/Northbound/Scripts/Minigames/DinerShiftGame.cs`
- Create: `Assets/Northbound/Scripts/Minigames/WiringGame.cs`
- Create: `Assets/Northbound/Scripts/Minigames/TrunkPackingGame.cs`
- Create: `Assets/Northbound/Prefabs/UI/DinerShift.prefab`
- Create: `Assets/Northbound/Prefabs/UI/WiringGame.prefab`
- Create: `Assets/Northbound/Prefabs/UI/TrunkPacking.prefab`
- Create: `Assets/Northbound/Tests/PlayMode/MinigameTests.cs`

**Interfaces:**
- Produces: `MinigameController.Begin(string id)`, `Complete()`, `Cancel()`, and `Completed(string id)`.
- Consumes: `InputGate` and `QuestRunner.Report`.

- [ ] **Step 1: Write failing completion and accessibility tests**

Test that each game acquires input, reports exactly one completion, releases input on complete/cancel, and that `SettingsModel.SkipMinigames` completes it without simulating interactions.

- [ ] **Step 2: Implement the common minigame lifecycle**

```csharp
public abstract class MinigameController : MonoBehaviour
{
    public event Action<string> Completed;
    public bool IsRunning { get; private set; }
    public abstract string Id { get; }

    public void Begin()
    {
        if (IsRunning) return;
        IsRunning = true;
        gameObject.SetActive(true);
        OnBegin();
    }

    protected void Complete()
    {
        if (!IsRunning) return;
        IsRunning = false;
        Completed?.Invoke(Id);
        gameObject.SetActive(false);
    }

    public void Cancel()
    {
        if (!IsRunning) return;
        IsRunning = false;
        OnCancel();
        gameObject.SetActive(false);
    }

    protected abstract void OnBegin();
    protected virtual void OnCancel() { }
}
```

- [ ] **Step 3: Implement exact game rules**

- `DinerShiftGame`: deliver three visible orders to matching table icons; no timer and no failure state.
- `WiringGame`: rotate four wire tiles until source and recorder are connected; reset button restores the authored layout.
- `TrunkPackingGame`: a 6×4 grid with five authored objects; capacity prevents all five from fitting and stores `packed_<item_id>` facts.

- [ ] **Step 4: Playtest duration and accessibility**

Each game must take a first-time player between 20 and 90 seconds. Verify mouse and keyboard operation, large readable targets, and Skip Minigames behavior.

- [ ] **Step 5: Run tests and commit**

```bash
git add Assets/Northbound/Scripts/Minigames Assets/Northbound/Prefabs/UI Assets/Northbound/Tests/PlayMode/MinigameTests.cs
git commit -m "feat: add narrative minigames"
```

### Task 8: Add reliable cinematic playback and skip behavior

**Files:**
- Create: `Assets/Northbound/Scripts/Cinematics/CinematicAsset.cs`
- Create: `Assets/Northbound/Scripts/Cinematics/CinematicPlayer.cs`
- Create: `Assets/Northbound/Prefabs/UI/CinematicCanvas.prefab`
- Create: `Assets/Northbound/Cinematics/Opening/opening_proxy.mp4`
- Create: `Assets/Northbound/Cinematics/Highlights/{maya_proxy,noah_proxy,leo_proxy}.mp4`
- Create: `Assets/Northbound/Cinematics/Rooftop/rooftop_proxy.mp4`
- Create: `Assets/Northbound/Cinematics/Finale/finale_proxy.mp4`
- Create: `Assets/Northbound/Tests/PlayMode/CinematicPlayerTests.cs`

**Interfaces:**
- Produces: `CinematicPlayer.Play(CinematicAsset)`, `Skip()`, `IsPlaying`, and `Finished(string cinematicId)`.
- Consumes: `InputGate`, `NarrativeStateStore`, dialogue subtitle settings, and audio mixer snapshots.

- [ ] **Step 1: Write failing cinematic lifecycle tests**

Use a fake playback adapter so tests do not depend on decoding video. Assert `Prepare` precedes `Play`, skip is unavailable for two seconds, natural completion and skip grant the same fact, and input/audio/camera state restore once.

- [ ] **Step 2: Implement an adapter around `VideoPlayer`**

```csharp
public interface IVideoPlayback
{
    event Action Prepared;
    event Action Finished;
    void Prepare(VideoClip clip);
    void Play();
    void Stop();
}
```

`CinematicPlayer` fades to black, acquires an input lease, selects the configured audio snapshot, prepares the clip, plays it on a full-screen `RenderTexture`, and completes through one idempotent cleanup method.

- [ ] **Step 3: Produce deterministic proxy clips**

Create six short 1920×1080 H.264 proxy clips with clip name, duration, shot purpose, and a burnt-in timecode. These are valid integration assets, not empty files. Import audio disabled for silent proxies.

- [ ] **Step 4: Test every slot watched and skipped**

Verify Opening, all three Highlights, Rooftop, and Finale. After each, Jamie can move, normal camera renders, gameplay audio returns, and the correct completion fact exists.

- [ ] **Step 5: Commit**

```bash
git add Assets/Northbound/Scripts/Cinematics Assets/Northbound/Prefabs/UI/CinematicCanvas.prefab Assets/Northbound/Cinematics Assets/Northbound/Tests/PlayMode/CinematicPlayerTests.cs
git commit -m "feat: add skippable cinematic playback"
```

### Task 9: Implement finale direction choice and ending variants

**Files:**
- Create: `Assets/Northbound/Scripts/Endings/EndingDirection.cs`
- Create: `Assets/Northbound/Scripts/Endings/EndingContext.cs`
- Create: `Assets/Northbound/Scripts/Endings/EndingResolver.cs`
- Create: `Assets/Northbound/Scripts/Endings/EndingTrigger.cs`
- Create: `Assets/Northbound/Data/Endings/{Northbound,HomeChosen,NoMap,NotAloneMaya,NotAloneNoah,NotAloneLeo}.asset`
- Create: `Assets/Northbound/Tests/EditMode/EndingResolverTests.cs`
- Create: `Assets/Northbound/Tests/PlayMode/EndingTriggerTests.cs`
- Modify: `Assets/Northbound/Scenes/Greybridge.unity`

**Interfaces:**
- Produces: `EndingResolver.Resolve(EndingDirection, string friendId, NarrativeState): EndingContext`.
- Produces: `EndingTrigger` requiring 1.25 seconds of continued directional commitment before confirmation.
- Consumes: hidden facts and counters but accepts physical direction as the authoritative ending input.

- [ ] **Step 1: Write failing ending tests**

Cover all six ending assets. Assert high/low Promise changes Elias's line but not `EndingDirection.Northbound`; high/low Connection changes Home staging; carried object changes No Map gesture; and every friend ID resolves regardless of bond score.

```csharp
[TestCase("maya")]
[TestCase("noah")]
[TestCase("leo")]
public void FriendEnding_IsNeverLockedByBond(string friendId)
{
    var result = resolver.Resolve(EndingDirection.Friend, friendId, new NarrativeState());
    Assert.That(result.AssetId, Is.EqualTo($"not_alone_{friendId}"));
}
```

- [ ] **Step 2: Implement pure ending resolution**

Keep `EndingResolver` free of scene objects. It returns an `EndingContext` containing ending asset ID, dialogue variant ID, carried prop ID, lighting variant, and end card.

- [ ] **Step 3: Add and test physical ending zones**

Place separate, visually readable zones for car, neighborhood, unmarked road, Maya, Noah, and Leo. Do not show quest markers. A radial hold indicator may appear only after the player remains in a zone for 0.4 seconds. Leaving before 1.25 seconds cancels it.

- [ ] **Step 4: Verify no accidental or hidden selection**

Start at the finale spawn and test small movements in every direction. Confirm no ending triggers from spawn drift and no hidden state redirects the selected path.

- [ ] **Step 5: Run tests and commit**

```bash
git add Assets/Northbound/Scripts/Endings Assets/Northbound/Data/Endings Assets/Northbound/Scenes/Greybridge.unity Assets/Northbound/Tests
git commit -m "feat: add physical ending choices"
```

### Task 10: Author and integrate the complete approved narrative content

**Files:**
- Create: `Assets/Northbound/Data/Quests/` assets for all missions in the specification
- Create: `Assets/Northbound/Data/Dialogue/` assets for required, optional, missed, and farewell conversations
- Create: `Assets/Northbound/Prefabs/Characters/{Elias,Maya,Noah,Leo}.prefab`
- Create: `Assets/Northbound/Prefabs/Triggers/` mission and conversation triggers
- Create: `Assets/Northbound/Tests/PlayMode/FullPathSmokeTests.cs`
- Modify: `Assets/Northbound/Scenes/Greybridge.unity`

**Interfaces:**
- Produces: a connected quest graph from `prologue` through `finale` with no dead ends.
- Consumes: all systems from Tasks 2–9.

- [ ] **Step 1: Create a content manifest before authoring assets**

Create `Assets/Northbound/Data/content-manifest.json` listing every required chapter, quest, dialogue, trigger, cinematic slot, fact, and ending asset. The validation test loads the manifest and fails on a missing or duplicate ID.

- [ ] **Step 2: Write the failing complete-path tests**

Add four simulated paths:

1. All Elias alternatives → Northbound.
2. All friend alternatives, community interactions → Home, Chosen.
3. Mixed missions, notebook → No Map.
4. One path for each Maya/Noah/Leo friend ending.

Each test asserts chapter order, exactly one quest from each pair, Finale reachability, and expected ending asset. Tests manipulate data and facts directly rather than waiting through dialogue or video.

- [ ] **Step 3: Author Chapter 1 and Chapter 2 content**

Create Clock In, The Missing Socket, Parts of a Future, Rooftop Inventory, The Last Sign, Dead Air, and One More Table. Enter the approved lines verbatim from the narrative specification, then add only functional connective lines required to identify immediate objectives.

- [ ] **Step 4: Author Crossroads and missed-state content**

Create all six alternatives, their commitment triggers, missed states, Pair B's `TWO DAYS` transition, highlight selection, and Rooftop Fracture. A missed mission leaves a physical trace: closed exhibition door, returned radio equipment, dark garage, or packed trunk.

- [ ] **Step 5: Author Chapter 4, Finale, and endings**

Create Things We Leave, The Spare Key, Before Morning, the six final trigger paths, relationship variants, props, end cards, credits, and Return to Title action.

- [ ] **Step 6: Run content validation and smoke paths**

Expected: no missing IDs, duplicate IDs, unreachable required quests, absent dialogue references, or impossible endings. All seven simulated ending variants pass.

- [ ] **Step 7: Commit**

```bash
git add Assets/Northbound/Data Assets/Northbound/Prefabs/Characters Assets/Northbound/Prefabs/Triggers Assets/Northbound/Scenes/Greybridge.unity Assets/Northbound/Tests/PlayMode/FullPathSmokeTests.cs
git commit -m "feat: integrate complete Northbound narrative"
```

### Task 11: Add menus, accessibility, audio, and production media replacement

**Files:**
- Create: `Assets/Northbound/Scripts/UI/PauseController.cs`
- Create: `Assets/Northbound/Scripts/UI/SettingsModel.cs`
- Create: `Assets/Northbound/Scripts/UI/SubtitleView.cs`
- Create: `Assets/Northbound/Prefabs/UI/{TitleMenu,PauseMenu,SettingsMenu,Credits}.prefab`
- Create: `Assets/Northbound/Audio/NorthboundMixer.mixer`
- Create: `docs/production/asset-manifest.md`
- Modify: production assets under `Assets/Northbound/Art`, `Audio`, and `Cinematics`
- Create: `Assets/Northbound/Tests/EditMode/SettingsModelTests.cs`

**Interfaces:**
- Produces: settings for master/music/SFX/voice volume, subtitle size, subtitle background opacity, reduced motion, minigame skip, and interaction-time multiplier.
- Consumes: `InputGate`, `DialogueView`, `CinematicPlayer`, and minigame controllers.

- [ ] **Step 1: Write failing settings persistence tests**

Test default values, JSON round-trip, clamping, and corrupt settings fallback. Defaults: all volumes `0.8`, subtitle scale `1.0`, opacity `0.75`, reduced motion `false`, skip minigames `false`, time multiplier `1.0`.

- [ ] **Step 2: Implement title, pause, settings, and credits flows**

New Game deletes the narrative save only after confirmation. Continue is disabled without a save. Pause acquires input and pauses gameplay clocks but not UI. Credits always offer Return to Title.

- [ ] **Step 3: Define and enforce the production asset contract**

Document every asset's filename, dimensions, pivot, pixels-per-unit, sorting layer, character costume, animation frames, audio peak target, video codec, duration, and source owner in `asset-manifest.md`. Required minimums:

- Gameplay sprites: PNG with transparency, consistent 100 pixels per unit.
- Portraits: 1024×1024 PNG.
- Cinematics: 1920×1080, 30 fps, H.264, AAC 48 kHz, English subtitles supplied separately in data.
- Music/SFX/voice masters: 48 kHz WAV; normalize through the Unity mixer, not destructive asset gain.

- [ ] **Step 4: Replace proxy art chapter by chapter**

Replace character sprites, portraits, environment tiles, props, lighting overlays, UI, SFX, music, voice reactions, and six proxy videos. After each category, compare against the manifest and run the relevant smoke scene. Preserve asset GUIDs by replacing file contents rather than deleting referenced assets.

- [ ] **Step 5: Perform media continuity review**

Check that Jamie, Elias, Maya, Noah, and Leo retain the same hair, costume, palette, body proportions, and key props across gameplay portraits and all cinematic clips. Reject any clip that changes identity-defining features or screen direction around a transition.

- [ ] **Step 6: Run tests and commit**

```bash
git add Assets/Northbound/Scripts/UI Assets/Northbound/Prefabs/UI Assets/Northbound/Audio Assets/Northbound/Art Assets/Northbound/Cinematics docs/production Assets/Northbound/Tests/EditMode/SettingsModelTests.cs
git commit -m "feat: add final presentation and accessibility"
```

### Task 12: Validate playtime, narrative goals, and the macOS release build

**Files:**
- Create: `Assets/Northbound/Tests/PlayMode/ReleaseAcceptanceTests.cs`
- Create: `docs/qa/playtest-script.md`
- Create: `docs/qa/playtest-results.md`
- Create: `docs/qa/release-checklist.md`
- Modify: only files required by failures discovered during this task

**Interfaces:**
- Consumes: the complete game and narrative specification acceptance criteria.
- Produces: a signed-off macOS build under ignored `Builds/macOS/Northbound.app`.

- [ ] **Step 1: Add automated release acceptance tests**

Verify build scenes, missing references, duplicate content IDs, save/load from every chapter, all six ending assets, skip behavior for six videos, input restoration, and required subtitle fields. Fail the suite on any unexpected `Debug.LogError` or exception.

- [ ] **Step 2: Run all automated tests from a clean editor launch**

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.3.17f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode -testResults /tmp/northbound-editmode.xml -quit
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform PlayMode -testResults /tmp/northbound-playmode.xml -quit
```

Expected: both commands exit `0`; result XML contains zero failures and zero errors.

- [ ] **Step 3: Conduct three observed playtests**

Use one first-time player, one narrative-focused player, and one player instructed to skip optional content. Record time, chosen missions, ending, confusion points, accidental ending triggers, and the player's own statement of the theme. Do not explain the theme before the session.

- [ ] **Step 4: Apply explicit acceptance thresholds**

- Median first-play time is 45–60 minutes.
- All players understand the immediate objective in every chapter.
- At least two of three hesitate at a conflicting mission.
- At least two of three describe the theme without using a line supplied by the game.
- No player calls Elias a simple villain without also identifying a sympathetic motive.
- No ending is selected accidentally.
- The skip-optional-content player still understands the main plot.

Fix any threshold failure, rerun the affected automated suite, and repeat the relevant playtest.

- [ ] **Step 5: Build and smoke-test macOS**

Create `Builds/macOS/Northbound.app`. Launch it outside the Editor; verify New Game, Continue, save/reload, all input modes, pause, one cinematic skip, one chapter transition, one ending, credits, and Return to Title. Confirm no `Player.log` exceptions.

- [ ] **Step 6: Complete release checklist and commit**

```bash
git add Assets/Northbound/Tests/PlayMode/ReleaseAcceptanceTests.cs docs/qa
git commit -m "test: verify Northbound release candidate"
```

## Final Verification Commands

```bash
git status --short
git lfs ls-files
UNITY="/Applications/Unity/Hub/Editor/6000.3.17f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform EditMode -testResults /tmp/northbound-editmode.xml -quit
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform PlayMode -testResults /tmp/northbound-playmode.xml -quit
```

Expected final state: clean Git worktree, all large production media tracked through LFS, zero EditMode failures, zero PlayMode failures, and a manually smoke-tested macOS application.
