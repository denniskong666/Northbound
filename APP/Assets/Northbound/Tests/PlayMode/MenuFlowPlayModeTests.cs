using System.Collections;
using Northbound.Core;
using Northbound.Narrative;
using Northbound.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using Northbound.Cinematics;
using Northbound.Endings;
using Northbound.Dialogue;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using Northbound.World;

namespace Northbound.Tests
{
    public sealed class MenuFlowPlayModeTests
    {
        [TearDown]
        public void ResetQuitOverride()
        {
            GameBootstrap.SessionQuitAction = null;
            GameText.Use(GameLanguage.English);
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Bootstrap_InstantiatesTitleMenuWithDisabledContinueAndButtonPaths()
        {
            var path = Path.Combine(Application.temporaryCachePath, "northbound-menu-flow-save.json");
            new SaveGameService(path).Delete();
            GameBootstrap.SessionSaveGameFactory = () => new SaveGameService(path);
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneIds.Bootstrap);
            yield return null;
            var menu = Object.FindFirstObjectByType<PauseController>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.IsTitleVisible, Is.True);
            var continueButton = System.Array.Find(menu.GetComponentsInChildren<Button>(true), button => button.name == "Continue");
            var newGameButton = System.Array.Find(menu.GetComponentsInChildren<Button>(true), button => button.name == "New Game");
            Assert.That(continueButton.interactable, Is.False);
            newGameButton.onClick.Invoke();
            Assert.That(menu.IsNewGameConfirmationVisible, Is.True);
            GameBootstrap.SessionSaveGameFactory = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SaveAndQuit_TitleAndPausePersistTheCompleteStateBeforeRequestingExit()
        {
            var path = Path.Combine(Application.temporaryCachePath, "northbound-save-and-quit-success.json");
            var save = new SaveGameService(path);
            save.Delete();
            yield return LoadFreshBootstrapWithPath(path);

            var bootstrap = GameBootstrap.Instance;
            var menu = bootstrap.Menus;
            GameText.Use(GameLanguage.English);
            bootstrap.NarrativeState.Set("save_and_quit_fact", true);
            bootstrap.NarrativeState.Add("save_and_quit_counter", 3);
            var quitRequests = 0;
            var expectedCounter = 3;
            GameBootstrap.SessionQuitAction = () =>
            {
                var persisted = save.LoadOrNew();
                Assert.That(persisted.Has("save_and_quit_fact"), Is.True,
                    "The complete narrative state must reach disk before quitting.");
                Assert.That(persisted.GetInt("save_and_quit_counter"), Is.EqualTo(expectedCounter));
                quitRequests++;
            };

            var titleButton = menu.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Save and Quit");
            Assert.That(titleButton.GetComponentInChildren<Text>().text, Is.EqualTo("Save and Quit"));
            titleButton.onClick.Invoke();
            Assert.That(quitRequests, Is.EqualTo(1));

            menu.HideTitle();
            menu.Pause();
            var pauseRoot = SceneObject("PauseMenu(Clone)");
            Assert.That(pauseRoot, Is.Not.Null);
            var pauseButton = pauseRoot.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Save and Quit");
            Assert.That(pauseButton.gameObject.activeInHierarchy, Is.True);

            bootstrap.NarrativeState.Add("save_and_quit_counter", 2);
            expectedCounter = 5;
            GameText.Use(GameLanguage.SimplifiedChinese);
            Assert.That(titleButton.GetComponentInChildren<Text>().text, Is.EqualTo("保存并退出游戏"));
            Assert.That(pauseButton.GetComponentInChildren<Text>().text, Is.EqualTo("保存并退出游戏"));
            pauseButton.onClick.Invoke();
            Assert.That(quitRequests, Is.EqualTo(2));
            Assert.That(save.LoadOrNew().GetInt("save_and_quit_counter"), Is.EqualTo(5));

            menu.Resume();
            save.Delete();
        }

        [UnityTest]
        public IEnumerator SaveAndQuit_WhenSavingFailsKeepsTitleAndPauseOpenWithoutRequestingExit()
        {
            var path = "/dev/null/northbound-save-and-quit-failure.json";
            yield return LoadFreshBootstrapWithPath(path);

            var menu = GameBootstrap.Instance.Menus;
            var quitRequested = false;
            GameBootstrap.SessionQuitAction = () => quitRequested = true;
            LogAssert.Expect(LogType.Error,
                "Northbound could not save the current game. The application will remain open.");
            menu.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Save and Quit").onClick.Invoke();
            Assert.That(quitRequested, Is.False);
            Assert.That(menu.IsTitleVisible, Is.True);

            menu.HideTitle();
            menu.Pause();
            LogAssert.Expect(LogType.Error,
                "Northbound could not save the current game. The application will remain open.");
            SceneObject("PauseMenu(Clone)").GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "Save and Quit").onClick.Invoke();
            Assert.That(quitRequested, Is.False);
            Assert.That(menu.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            menu.Resume();
        }

        [UnityTest]
        public IEnumerator Bootstrap_TitleOwnsInputAndPreventsWorldOrOpeningProgress()
        {
            var path = Path.Combine(Application.temporaryCachePath, "northbound-title-gate-save.json");
            new SaveGameService(path).Delete();
            yield return LoadFreshBootstrapWithPath(path);
            yield return null;

            var bootstrap = GameBootstrap.Instance;
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            Assert.That(bootstrap.Menus.IsTitleVisible, Is.True);
            Assert.That(bootstrap.InputGate.IsBlocked, Is.True, "Title owns the global input lease.");
            Assert.That(flow.CurrentChapterId, Is.Null.Or.Empty);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.False);
            Assert.That(File.Exists(path), Is.False, "No chapter/cinematic may save behind title.");
        }

        [UnityTest]
        public IEnumerator NewGame_ButtonRequiresConfirmationBeforeDeletingTemporaryNarrativeSave()
        {
            var path = Path.Combine(Application.temporaryCachePath, "northbound-menu-new-game-save.json");
            var save = new SaveGameService(path);
            var state = new NarrativeState();
            state.Set("cinematic_opening_complete", true);
            Assert.That(save.Save(state), Is.True);
            if (GameBootstrap.Instance != null)
            {
                Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }
            GameBootstrap.SessionSaveGameFactory = () => new SaveGameService(path);
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneIds.Bootstrap);
            yield return null;
            var menu = Object.FindFirstObjectByType<PauseController>();
            var buttons = menu.GetComponentsInChildren<Button>(true);
            System.Array.Find(buttons, button => button.name == "New Game").onClick.Invoke();
            Assert.That(File.Exists(path), Is.True);
            System.Array.Find(buttons, button => button.name == "Confirm New Game").onClick.Invoke();
            Assert.That(File.Exists(path), Is.False);
            Assert.That(GameBootstrap.Instance.NarrativeState.Has("cinematic_opening_complete"), Is.False,
                "New Game must reset the already-loaded in-memory narrative as well as deleting the file.");
            GameBootstrap.SessionSaveGameFactory = null;
        }

        [UnityTest]
        public IEnumerator NewGame_FromLateRuntimeRebuildsFreshPrologueAndStartsOpening()
        {
            var path = Path.Combine(Application.temporaryCachePath, "northbound-new-game-late-save.json");
            var save = new SaveGameService(path);
            var late = new NarrativeState();
            late.Set(GameFlowController.ChapterFact("finale"), true);
            late.Set("cinematic_finale_complete", true);
            late.Set("mission_pair_alternator_first_light_committed_alternator", true);
            Assert.That(save.Save(late), Is.True);
            yield return LoadFreshBootstrapWithPath(path);
            var oldWorld = Object.FindFirstObjectByType<GreybridgeWorldLayout>();

            var menu = GameBootstrap.Instance.Menus;
            menu.GetComponentsInChildren<Button>(true).Single(button => button.name == "New Game").onClick.Invoke();
            menu.GetComponentsInChildren<Button>(true).Single(button => button.name == "Confirm New Game").onClick.Invoke();
            yield return WaitForChapter("prologue");

            var bootstrap = GameBootstrap.Instance;
            Assert.That(Object.FindFirstObjectByType<GreybridgeWorldLayout>(), Is.Not.SameAs(oldWorld));
            Assert.That(bootstrap.NarrativeState.Has(GameFlowController.ChapterFact("finale")), Is.False);
            Assert.That(bootstrap.NarrativeState.Has("cinematic_finale_complete"), Is.False);
            Assert.That(bootstrap.NarrativeState.Has("mission_pair_alternator_first_light_committed_alternator"), Is.False);
            Assert.That(bootstrap.Minigames.Quests.ActiveQuestId, Is.Null);
            Assert.That(bootstrap.Dialogue.IsRunning, Is.False);
            Assert.That(bootstrap.Endings.IsShowing, Is.False);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True);
            Assert.That(bootstrap.Menus.IsTitleVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator Continue_ButtonEnablesForTemporaryNarrativeSaveAndLeavesTitle()
        {
            var path = Path.Combine(Application.temporaryCachePath, "northbound-menu-continue-save.json");
            var save = new SaveGameService(path);
            var state = new NarrativeState();
            state.Set("cinematic_opening_complete", true);
            Assert.That(save.Save(state), Is.True);
            if (GameBootstrap.Instance != null) { Object.Destroy(GameBootstrap.Instance.gameObject); yield return null; }
            GameBootstrap.SessionSaveGameFactory = () => new SaveGameService(path);
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneIds.Bootstrap);
            yield return null;
            var menu = Object.FindFirstObjectByType<PauseController>();
            var button = System.Array.Find(menu.GetComponentsInChildren<Button>(true), candidate => candidate.name == "Continue");
            Assert.That(button.interactable, Is.True);
            button.onClick.Invoke();
            Assert.That(menu.IsTitleVisible, Is.False);
            Assert.That(GameBootstrap.Instance.NarrativeState.Has("cinematic_opening_complete"), Is.True);
            GameBootstrap.SessionSaveGameFactory = null;
        }

        [UnityTest]
        public IEnumerator EscapeTap_AfterContinuePausesThroughTheLiveBootstrapController()
        {
            var path = Path.Combine(Application.temporaryCachePath, "northbound-escape-tap-save.json");
            var save = new SaveGameService(path);
            var state = new NarrativeState();
            state.Set("cinematic_opening_complete", true);
            Assert.That(save.Save(state), Is.True);
            yield return LoadFreshBootstrapWithPath(path);

            var bootstrap = GameBootstrap.Instance;
            var menu = bootstrap.Menus;
            menu.GetComponentsInChildren<Button>(true).Single(button => button.name == "Continue").onClick.Invoke();
            yield return null;
            Assert.That(menu.IsTitleVisible, Is.False);

            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                InputSystem.Update();

                Assert.That(menu.IsPaused, Is.True,
                    "The live bootstrap controller must receive Escape through its enabled InputAction.");
                Assert.That(Time.timeScale, Is.EqualTo(0f));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                menu.Resume();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                Assert.That(menu.IsPaused, Is.True, "Releasing Escape must not immediately resume the game.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                if (menu != null) menu.Resume();
                GameBootstrap.SessionSaveGameFactory = null;
                save.Delete();
            }
        }

        [UnityTest]
        public IEnumerator Continue_RefreshesAfterLaunchAndReloadsSavedChapterWorldAndFacts()
        {
            var path = Path.Combine(Application.temporaryCachePath, "northbound-continue-refresh-save.json");
            var save = new SaveGameService(path);
            save.Delete();
            yield return LoadFreshBootstrapWithPath(path);
            var menu = GameBootstrap.Instance.Menus;
            var continueButton = menu.GetComponentsInChildren<Button>(true).Single(button => button.name == "Continue");
            Assert.That(continueButton.interactable, Is.False);

            var state = new NarrativeState();
            state.Set(GameFlowController.ChapterFact("chapter_2"), true);
            state.Set("continue_roundtrip_fact", true);
            Assert.That(save.Save(state), Is.True);
            menu.ShowTitle();
            Assert.That(continueButton.interactable, Is.True, "ShowTitle must refresh disk availability.");
            continueButton.onClick.Invoke();
            yield return WaitForChapter("chapter_2");

            Assert.That(menu.IsTitleVisible, Is.False);
            Assert.That(GameBootstrap.Instance.NarrativeState.Has("continue_roundtrip_fact"), Is.True);
            Assert.That(Object.FindFirstObjectByType<ChapterWorldController>().CurrentChapterId, Is.EqualTo("chapter_2"));
            Assert.That(GameBootstrap.Instance.Cinematics.IsPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator Continue_FinaleSaveGuidesAClearRoadSpawnToTheGatheringInteraction()
        {
            var path = Path.Combine(Application.temporaryCachePath, "northbound-finale-gathering-continue-save.json");
            var save = new SaveGameService(path);
            save.Delete();
            var state = new NarrativeState();
            state.Set(GameFlowController.ChapterFact("finale"), true);
            state.Set("tutorial_moved", true);
            state.Set("cinematic_finale_complete", true);
            Assert.That(save.Save(state), Is.True);
            yield return LoadFreshBootstrapWithPath(path);

            var menu = GameBootstrap.Instance.Menus;
            menu.GetComponentsInChildren<Button>(true).Single(button => button.name == "Continue").onClick.Invoke();
            yield return WaitForChapter("finale");
            yield return null;

            var gathering = GameObject.Find("Finale Gathering");
            Assert.That(gathering, Is.Not.Null);
            var cast = gathering.transform.Find("Greybridge Friends");
            Assert.That(cast, Is.Not.Null);
            Assert.That(cast.gameObject.activeInHierarchy, Is.True,
                "Restoring the finale must reveal the gathering that was configured before the chapter was restored.");
            Assert.That(cast.GetComponentsInChildren<Northbound.Art.TopDownCharacterVisual>(false), Has.Length.EqualTo(4));
            var wagon = cast.Find("Finale Wagon");
            Assert.That(wagon, Is.Not.Null);
            Assert.That(wagon.gameObject.activeInHierarchy, Is.True);
            Assert.That(wagon.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
            var star = gathering.transform.Find("Required Objective Star");
            Assert.That(star, Is.Not.Null);
            Assert.That(star.gameObject.activeInHierarchy, Is.True,
                "The restored finale objective must refresh the required gold marker.");
            var guidance = Object.FindFirstObjectByType<Northbound.Guidance.GuidanceController>();
            var guidanceHud = Object.FindFirstObjectByType<Northbound.Guidance.GuidanceHudView>();
            Assert.That(guidance.CurrentDestinationId, Is.EqualTo("finale_gathering"));
            Assert.That(guidanceHud.PresentationVisible, Is.True);
            yield return null;
            yield return null;
            Assert.That(guidanceHud.DirectionIndicatorVisible, Is.True,
                "The finale spawn needs an edge pointer while the gathering marker is just beyond the camera.");
            Assert.That(guidanceHud.DirectionLabel, Is.EqualTo(GameText.Location("Finale Gathering")));

            var jamie = GameObject.Find("Jamie");
            Assert.That(jamie, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<LocationTransitionController>().CurrentLocationId, Is.EqualTo("exterior"));
            Assert.That(Vector2.Distance(jamie.transform.position, gathering.transform.position), Is.EqualTo(4f).Within(.01f));
            Assert.That(Camera.main.WorldToViewportPoint(star.GetComponent<Renderer>().bounds.center).y, Is.GreaterThan(1f));
            var wagonBounds = wagon.GetComponent<SpriteRenderer>().bounds;
            var jamieBounds = jamie.GetComponent<Northbound.Art.TopDownCharacterVisual>().CharacterRenderer.bounds;
            Assert.That(jamieBounds.max.y, Is.LessThan(wagonBounds.min.y),
                "The restored finale must start Jamie on clear road below the wagon.");

            var finale = gathering.GetComponent<FinaleGatheringInteractor>();
            var playerInteractor = jamie.GetComponent<Northbound.Interaction.PlayerInteractor>();
            Assert.That(finale.CanInteract, Is.True);
            Physics2D.SyncTransforms();
            playerInteractor.RefreshTarget();
            Assert.That(playerInteractor.CurrentInteractable, Is.Not.SameAs(finale));
            jamie.transform.position = gathering.transform.position + Vector3.down * 2.8f;
            Physics2D.SyncTransforms();
            playerInteractor.RefreshTarget();
            Assert.That(playerInteractor.CurrentInteractable, Is.SameAs(finale));
            playerInteractor.TryInteract();
            Assert.That(GameBootstrap.Instance.NarrativeState.Has(FinaleGatheringInteractor.ReviewedFact), Is.True);
            var layout = Object.FindFirstObjectByType<GreybridgeWorldLayout>();
            Assert.That(new[] { "Finale Car Region", "Finale Home Region", "Finale Road Region", "Finale Friends Region" }
                .All(name => layout.transform.Find(name).gameObject.activeSelf), Is.True);
            save.Delete();
        }

        [UnityTest]
        public IEnumerator NewGameConfirmation_ConfirmAndCancelAreStatefulFocusedAndKeyboardReachable()
        {
            var path = Path.Combine(Application.temporaryCachePath, "northbound-confirmation-panel-save.json");
            var save = new SaveGameService(path);
            Assert.That(save.Save(new NarrativeState()), Is.True);
            yield return LoadFreshBootstrapWithPath(path);
            var menu = GameBootstrap.Instance.Menus;
            var panel = menu.transform.Find("New Game Confirmation");
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.gameObject.activeSelf, Is.False);

            menu.GetComponentsInChildren<Button>(true).Single(button => button.name == "New Game").onClick.Invoke();
            Assert.That(panel.gameObject.activeSelf, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Confirm New Game"));
            panel.GetComponentsInChildren<Button>(true).Single(button => button.name == "Cancel New Game").onClick.Invoke();
            Assert.That(panel.gameObject.activeSelf, Is.False);
            Assert.That(File.Exists(path), Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("New Game"));

            menu.GetComponentsInChildren<Button>(true).Single(button => button.name == "New Game").onClick.Invoke();
            var cancel = panel.GetComponentsInChildren<Button>(true).Single(button => button.name == "Cancel New Game");
            EventSystem.current.SetSelectedGameObject(cancel.gameObject);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                InputSystem.Update();
                typeof(PauseController).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(menu, null);
                menu.ConfirmNewGame(); // Direct trigger of the production keyboard-confirm callback; selection must still govern it.
                ExecuteEvents.Execute<ISubmitHandler>(cancel.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
                Assert.That(File.Exists(path), Is.True, "Submitting the focused Cancel control must never confirm or delete.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }

            menu.GetComponentsInChildren<Button>(true).Single(button => button.name == "New Game").onClick.Invoke();
            var selected = EventSystem.current.currentSelectedGameObject;
            ExecuteEvents.Execute<ISubmitHandler>(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            yield return WaitForChapter("prologue");
            Assert.That(menu.IsTitleVisible, Is.False);
            Assert.That(GameBootstrap.Instance.Cinematics.IsPlaying, Is.True);
        }

        [Test]
        public void PauseAndResume_LeaseInputAndRestoreTimeScaleExactlyOnce()
        {
            var root = new GameObject("Pause test");
            var gate = root.AddComponent<InputGate>();
            var menu = root.AddComponent<PauseController>();
            menu.Initialize(gate, new SaveGameService(Path.Combine(Application.temporaryCachePath, "northbound-pause-save.json")));
            menu.HideTitle();
            menu.Pause();
            Assert.That(menu.IsPaused, Is.True);
            Assert.That(gate.IsBlocked, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            menu.Resume();
            menu.Resume();
            Assert.That(menu.IsPaused, Is.False);
            Assert.That(gate.IsBlocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void EscapeKey_UsesTheRawInputEventToPauseAndResume()
        {
            var root = new GameObject("Keyboard menu test");
            var gate = root.AddComponent<InputGate>();
            var menu = root.AddComponent<PauseController>();
            menu.Initialize(gate, new SaveGameService(Path.Combine(Application.temporaryCachePath, "northbound-keyboard-menu-save.json")));
            menu.HideTitle();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                InputSystem.Update();
                Assert.That(keyboard.escapeKey.isPressed, Is.True);
                Assert.That(menu.IsPaused, Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(0f));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                InputSystem.Update();
                Assert.That(menu.IsPaused, Is.False);
                Assert.That(gate.IsBlocked, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1f));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EscapeKey_FastTapWithinOneInputUpdateStillPauses()
        {
            var root = new GameObject("Fast escape tap menu test");
            var gate = root.AddComponent<InputGate>();
            var menu = root.AddComponent<PauseController>();
            menu.Initialize(gate, new SaveGameService(Path.Combine(Application.temporaryCachePath, "northbound-fast-escape-tap-save.json")));
            menu.HideTitle();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();

                Assert.That(menu.IsPaused, Is.True, "A native Escape tap must not disappear when press and release arrive in the same input update.");
                Assert.That(Time.timeScale, Is.EqualTo(0f));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                if (menu != null) menu.Resume();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EscapeKey_StillOpensPauseMenuWhileAnotherSystemOwnsInput()
        {
            var root = new GameObject("Blocked keyboard menu test");
            var gate = root.AddComponent<InputGate>();
            var menu = root.AddComponent<PauseController>();
            menu.Initialize(gate, new SaveGameService(Path.Combine(Application.temporaryCachePath, "northbound-blocked-keyboard-menu-save.json")));
            menu.HideTitle();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var externalLease = gate.Acquire(this);
            try
            {
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                InputSystem.Update();

                Assert.That(menu.IsPaused, Is.True, "Escape must always provide a visible way out of gameplay.");
                Assert.That(Time.timeScale, Is.EqualTo(0f));
                Assert.That(gate.IsBlocked, Is.True, "Pausing must preserve the external system's lease.");
            }
            finally
            {
                externalLease.Dispose();
                InputSystem.RemoveDevice(keyboard);
                Object.DestroyImmediate(root);
            }
        }

        [UnityTest]
        public IEnumerator HudPauseButton_ProvidesAMouseFallbackToTheSamePauseMenu()
        {
            yield return LoadFreshBootstrap("northbound-hud-pause-save.json");
            var bootstrap = GameBootstrap.Instance;
            bootstrap.Cinematics.Cancel();
            bootstrap.Menus.HideTitle();
            var button = Object.FindFirstObjectByType<Northbound.Guidance.GuidanceHudView>()
                .GetComponentsInChildren<Button>(true).Single(candidate => candidate.name == "Pause");

            button.onClick.Invoke();

            Assert.That(bootstrap.Menus.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            bootstrap.Menus.Resume();
        }

        [UnityTest]
        public IEnumerator GameplayGuidance_HidesOutsidePlayerControlAndReturnsAfterResume()
        {
            yield return LoadFreshBootstrap("northbound-guidance-visibility-save.json");
            var bootstrap = GameBootstrap.Instance;
            var menu = bootstrap.Menus;
            var hud = Object.FindFirstObjectByType<Northbound.Guidance.GuidanceHudView>();
            yield return null;
            Assert.That(hud.PresentationVisible, Is.False, "The title must not show live objective guidance behind its menu.");

            menu.GetComponentsInChildren<Button>(true).Single(button => button.name == "New Game").onClick.Invoke();
            menu.GetComponentsInChildren<Button>(true).Single(button => button.name == "Confirm New Game").onClick.Invoke();
            yield return WaitForChapter("prologue");
            bootstrap.Cinematics.Cancel();
            yield return null;
            hud = Object.FindFirstObjectByType<Northbound.Guidance.GuidanceHudView>();
            Assert.That(hud.PresentationVisible, Is.True);

            menu.Pause();
            yield return null;
            Assert.That(hud.PresentationVisible, Is.False, "Pause must own the screen without HUD or arrow clutter.");
            menu.Resume();
            yield return null;
            Assert.That(hud.PresentationVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator Bootstrap_InstantiatesAllAuthoredMenuPrefabsAndCreditsReturnsToTitle()
        {
            yield return LoadFreshBootstrap("northbound-menu-prefabs-save.json");
            var menu = GameBootstrap.Instance.Menus;
            Assert.That(SceneObject("TitleMenu(Clone)"), Is.Not.Null);
            Assert.That(SceneObject("PauseMenu(Clone)"), Is.Not.Null);
            Assert.That(SceneObject("SettingsMenu(Clone)"), Is.Not.Null);
            var credits = SceneObject("Credits(Clone)");
            Assert.That(credits, Is.Not.Null);

            var showCredits = typeof(PauseController).GetMethod("ShowCredits");
            Assert.That(showCredits, Is.Not.Null);
            showCredits.Invoke(menu, null);
            Assert.That(menu.IsTitleVisible, Is.False);
            credits.GetComponentsInChildren<Button>(true).Single(button => button.name == "Return to Title").onClick.Invoke();
            Assert.That(menu.IsTitleVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator AuthoredPauseSettingsAndCredits_StayVisibleThroughRealButtonPaths()
        {
            yield return LoadFreshBootstrap("northbound-visible-menu-prefabs-save.json");
            var menu = GameBootstrap.Instance.Menus;
            var pauseRoot = SceneObject("PauseMenu(Clone)");
            var settingsRoot = SceneObject("SettingsMenu(Clone)");
            var creditsRoot = SceneObject("Credits(Clone)");

            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("New Game"));

            GameBootstrap.Instance.Cinematics.Cancel();
            menu.HideTitle();
            menu.Pause();
            AssertVisibleButton(pauseRoot, "Resume");
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Resume"));
            pauseRoot.GetComponentsInChildren<Button>(true).Single(button => button.name == "Settings").onClick.Invoke();
            AssertVisibleButton(settingsRoot, "Apply");
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Master Volume"));
            settingsRoot.GetComponentsInChildren<Button>(true).Single(button => button.name == "Back").onClick.Invoke();
            pauseRoot.GetComponentsInChildren<Button>(true).Single(button => button.name == "Return to Title").onClick.Invoke();
            menu.GetComponentsInChildren<Button>(true).Single(button => button.name == "Credits").onClick.Invoke();
            AssertVisibleButton(creditsRoot, "Return to Title");
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Return to Title"));
        }

        [UnityTest]
        public IEnumerator PausePanel_UsesAnIndependentPersistentCanvasAndTheScreenCenter()
        {
            yield return LoadFreshBootstrap("northbound-pause-layout-save.json");

            var pauseRoot = SceneObject("PauseMenu(Clone)");
            var rect = pauseRoot.GetComponent<RectTransform>();
            var nestedCanvas = pauseRoot.GetComponent<Canvas>();

            Assert.That(pauseRoot.transform.parent, Is.Null);
            Assert.That(nestedCanvas, Is.Not.Null);
            Assert.That(nestedCanvas.enabled, Is.True);
            Assert.That(nestedCanvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(nestedCanvas.sortingOrder, Is.EqualTo(700));
            Assert.That(pauseRoot.GetComponent<GraphicRaycaster>().enabled, Is.True);
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(rect.sizeDelta, Is.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator EndingReturnButton_CancelsCinematicAndRestoresTitleInputAndClock()
        {
            yield return LoadFreshBootstrap("northbound-menu-ending-save.json");
            var bootstrap = GameBootstrap.Instance;
            bootstrap.Menus.HideTitle();
            if (!bootstrap.Cinematics.IsPlaying)
            {
                Assert.That(bootstrap.Cinematics.Play(bootstrap.CinematicCatalog.Find("opening")), Is.True);
            }
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True);
            bootstrap.Endings.Show(new EndingContext(
                EndingDirection.Northbound, "northbound", "ending_northbound", "road_map", "sunrise", "Northbound"));
            Time.timeScale = 0f;

            var returnButton = bootstrap.Endings.GetComponentsInChildren<Button>(true).Single(button => button.name == "Return to Title");
            returnButton.onClick.Invoke();
            yield return null;

            Assert.That(bootstrap.Menus.IsTitleVisible, Is.True);
            Assert.That(bootstrap.Endings.IsShowing, Is.False);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.False);
            Assert.That(bootstrap.InputGate.IsBlocked, Is.True, "The returned title deliberately owns the only remaining input lease.");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator EndingPresentation_UsesDistinctScenesAndShowsWhatThePlayerCarriedForward()
        {
            yield return LoadFreshBootstrap("northbound-ending-scenes-save.json");
            var presentation = GameBootstrap.Instance.Endings;
            var contexts = new[]
            {
                new EndingContext(EndingDirection.Northbound, "northbound", "ready", "second_key", "dawn_car", "Northbound"),
                new EndingContext(EndingDirection.HomeChosen, "home", "garage", "garage_light_switch", "home_garage_light", "Home"),
                new EndingContext(EndingDirection.NoMap, "no_map", "notebook", "notebook_write_date", "unmarked_road_dawn", "No Map"),
                new EndingContext(EndingDirection.PauseJourney, "pause", "pause", "notebook_blank_page", "rooftop_first_light", "Pause")
            };
            var sceneColors = new Color[contexts.Length];

            for (var index = 0; index < contexts.Length; index++)
            {
                presentation.Show(contexts[index]);
                yield return null;
                sceneColors[index] = presentation.VisibleBackgroundColor;
                Assert.That(presentation.VisibleCarriedDetail, Is.Not.Empty);
                StringAssert.DoesNotContain(contexts[index].CarriedPropId, presentation.VisibleCarriedDetail,
                    "Players should see authored story feedback rather than an internal content identifier.");
                presentation.Cancel();
            }

            Assert.That(sceneColors.Distinct().Count(), Is.EqualTo(contexts.Length),
                "Each core finale route should have a visually distinct scene palette.");
        }

        [UnityTest]
        public IEnumerator PauseSnapshot_AttenuatesMusicAndSfxThenTitleRestoresUserMix()
        {
            yield return LoadFreshBootstrap("northbound-pause-mix-save.json");
            var bootstrap = GameBootstrap.Instance;
            bootstrap.Cinematics.Cancel();
            var mixerField = typeof(GameBootstrap).GetField("audioMixer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var mixer = (AudioMixer)mixerField.GetValue(bootstrap);
            mixer.SetFloat("MasterVolume", -2f);
            mixer.SetFloat("MusicVolume", -4f);
            mixer.SetFloat("SFXVolume", -5f);
            mixer.SetFloat("VoiceVolume", -3f);

            bootstrap.Menus.HideTitle();
            bootstrap.Menus.Pause();
            yield return null;
            AssertMixer(mixer, "MasterVolume", -2f);
            AssertMixer(mixer, "MusicVolume", -16f);
            AssertMixer(mixer, "SFXVolume", -23f);
            AssertMixer(mixer, "VoiceVolume", -3f);

            bootstrap.Menus.ShowTitle();
            yield return null;
            AssertMixer(mixer, "MasterVolume", -2f);
            AssertMixer(mixer, "MusicVolume", -4f);
            AssertMixer(mixer, "SFXVolume", -5f);
            AssertMixer(mixer, "VoiceVolume", -3f);
        }

        [UnityTest]
        public IEnumerator PauseReturnToTitle_EndsSessionAndClearsPendingDialogueCallbacks()
        {
            var path = Path.Combine(Application.temporaryCachePath, "northbound-pause-return-session.json");
            yield return LoadFreshBootstrapWithPath(path);
            var bootstrap = GameBootstrap.Instance;
            var menu = bootstrap.Menus;
            menu.GetComponentsInChildren<Button>(true).Single(button => button.name == "New Game").onClick.Invoke();
            menu.GetComponentsInChildren<Button>(true).Single(button => button.name == "Confirm New Game").onClick.Invoke();
            yield return WaitForChapter("prologue");
            bootstrap.Cinematics.Cancel();

            var staleCompletionCount = 0;
            bootstrap.Dialogue.Completed += () => staleCompletionCount++;
            menu.Pause();
            var pauseRoot = SceneObject("PauseMenu(Clone)");
            pauseRoot.GetComponentsInChildren<Button>(true).Single(button => button.name == "Return to Title").onClick.Invoke();
            Assert.That(menu.IsTitleVisible, Is.True);
            Assert.That(bootstrap.IsSessionActive, Is.False, "The real pause button must cross the Bootstrap session boundary.");
            Assert.That(bootstrap.InputGate.IsBlocked, Is.True);

            menu.GetComponentsInChildren<Button>(true).Single(button => button.name == "New Game").onClick.Invoke();
            menu.GetComponentsInChildren<Button>(true).Single(button => button.name == "Confirm New Game").onClick.Invoke();
            yield return WaitForChapter("prologue");
            bootstrap.Cinematics.Cancel();
            var dialogue = ScriptableObject.CreateInstance<DialogueAsset>();
            dialogue.id = "fresh-session-dialogue";
            dialogue.lines.Add(new DialogueLine { speakerId = "JAMIE", text = "Fresh session." });
            bootstrap.Dialogue.Start(dialogue);
            bootstrap.Dialogue.Advance();
            Assert.That(staleCompletionCount, Is.Zero, "Session-specific dialogue callbacks must not survive Return to Title.");
            Object.Destroy(dialogue);
        }

        [UnityTest]
        public IEnumerator PauseSettingsApply_KeepsNewBaseVolumesAttenuatedAndRestoresThemOnResume()
        {
            var settingsPath = Path.Combine(Application.temporaryCachePath, "northbound-pause-settings.json");
            GameBootstrap.SessionSettingsPath = settingsPath;
            try
            {
                yield return LoadFreshBootstrap("northbound-pause-settings-save.json");
                var bootstrap = GameBootstrap.Instance;
                var menu = bootstrap.Menus;
                menu.HideTitle();
                menu.Pause();
                var pauseRoot = SceneObject("PauseMenu(Clone)");
                var settingsRoot = SceneObject("SettingsMenu(Clone)");
                pauseRoot.GetComponentsInChildren<Button>(true).Single(button => button.name == "Settings").onClick.Invoke();
                SetSlider(settingsRoot, "Master Volume", .5f);
                SetSlider(settingsRoot, "Music Volume", .5f);
                SetSlider(settingsRoot, "SFX Volume", .25f);
                SetSlider(settingsRoot, "Voice Volume", .5f);
                settingsRoot.GetComponentsInChildren<Button>(true).Single(button => button.name == "Apply").onClick.Invoke();

                var mixerField = typeof(GameBootstrap).GetField("audioMixer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var mixer = (AudioMixer)mixerField.GetValue(bootstrap);
                AssertMixer(mixer, "MasterVolume", -6.0206f);
                AssertMixer(mixer, "MusicVolume", -18.0206f);
                AssertMixer(mixer, "SFXVolume", -30.0412f);
                AssertMixer(mixer, "VoiceVolume", -6.0206f);

                settingsRoot.GetComponentsInChildren<Button>(true).Single(button => button.name == "Back").onClick.Invoke();
                pauseRoot.GetComponentsInChildren<Button>(true).Single(button => button.name == "Resume").onClick.Invoke();
                AssertMixer(mixer, "MasterVolume", -6.0206f);
                AssertMixer(mixer, "MusicVolume", -6.0206f);
                AssertMixer(mixer, "SFXVolume", -12.0412f);
                AssertMixer(mixer, "VoiceVolume", -6.0206f);
            }
            finally
            {
                GameBootstrap.SessionSettingsPath = null;
                if (File.Exists(settingsPath)) File.Delete(settingsPath);
            }
        }

        [UnityTest]
        public IEnumerator SettingsControls_PersistAndApplyAllRuntimeValues()
        {
            var path = Path.Combine(Application.temporaryCachePath, "northbound-runtime-settings.json");
            if (File.Exists(path)) File.Delete(path);
            GameBootstrap.SessionSettingsPath = path;
            try
            {
                yield return LoadFreshBootstrap("northbound-menu-settings-save.json");
                var bootstrap = GameBootstrap.Instance;
                var settingsRoot = SceneObject("SettingsMenu(Clone)");
                typeof(PauseController).GetMethod("ShowSettings").Invoke(bootstrap.Menus, null);
                SetSlider(settingsRoot, "Master Volume", .25f);
                SetSlider(settingsRoot, "Music Volume", .5f);
                SetSlider(settingsRoot, "SFX Volume", .75f);
                SetSlider(settingsRoot, "Voice Volume", 1f);
                SetSlider(settingsRoot, "Subtitle Scale", 1.25f);
                SetSlider(settingsRoot, "Subtitle Background Opacity", .35f);
                SetSlider(settingsRoot, "Interaction Time Multiplier", 1.4f);
                settingsRoot.GetComponentsInChildren<Toggle>(true).Single(control => control.name == "Reduced Motion").isOn = true;
                settingsRoot.GetComponentsInChildren<Toggle>(true).Single(control => control.name == "Skip Minigames").isOn = true;
                settingsRoot.GetComponentsInChildren<Button>(true).Single(control => control.name == "Apply").onClick.Invoke();

                Assert.That(bootstrap.Settings.MasterVolume, Is.EqualTo(.25f));
                Assert.That(bootstrap.Settings.MusicVolume, Is.EqualTo(.5f));
                Assert.That(bootstrap.Settings.SfxVolume, Is.EqualTo(.75f));
                Assert.That(bootstrap.Settings.VoiceVolume, Is.EqualTo(1f));
                Assert.That(bootstrap.Settings.SubtitleScale, Is.EqualTo(1.25f));
                Assert.That(bootstrap.Settings.SubtitleBackgroundOpacity, Is.EqualTo(.35f));
                Assert.That(bootstrap.Settings.ReducedMotion, Is.True);
                Assert.That(bootstrap.Settings.SkipMinigames, Is.True);
                Assert.That(bootstrap.Settings.InteractionTimeMultiplier, Is.EqualTo(1.4f));
                var persisted = SettingsModel.Load(path);
                Assert.That(persisted.SubtitleScale, Is.EqualTo(1.25f));
                Assert.That(persisted.SubtitleBackgroundOpacity, Is.EqualTo(.35f));
                Assert.That(persisted.ReducedMotion, Is.True);
                Assert.That(persisted.SkipMinigames, Is.True);
                Assert.That(persisted.InteractionTimeMultiplier, Is.EqualTo(1.4f));

                var mixerField = typeof(GameBootstrap).GetField("audioMixer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var mixer = (AudioMixer)mixerField.GetValue(bootstrap);
                AssertMixer(mixer, "MasterVolume", -12.0412f);
                AssertMixer(mixer, "MusicVolume", -6.0206f);
                AssertMixer(mixer, "SFXVolume", -2.4988f);
                AssertMixer(mixer, "VoiceVolume", 0f);
            }
            finally
            {
                GameBootstrap.SessionSettingsPath = null;
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [UnityTest]
        public IEnumerator SubtitleSettings_ApplyToActualDialogueAndCinematicBackgrounds()
        {
            yield return LoadFreshBootstrap("northbound-subtitle-settings-save.json");
            var bootstrap = GameBootstrap.Instance;
            bootstrap.Settings.SubtitleScale = 1.25f;
            bootstrap.Settings.SubtitleBackgroundOpacity = .35f;

            var dialogueAsset = ScriptableObject.CreateInstance<DialogueAsset>();
            dialogueAsset.id = "subtitle-settings";
            dialogueAsset.lines.Add(new DialogueLine { speakerId = "NOAH", text = "Readable dialogue." });
            var dialogueView = Object.FindFirstObjectByType<DialogueView>();
            dialogueView.StartDialogue(dialogueAsset);
            yield return null;
            dialogueView.RevealCurrentLine();
            var dialogueText = dialogueView.GetComponentsInChildren<Text>(true).Single(label => label.text == "Readable dialogue.");
            var dialogueBackground = dialogueView.GetComponentsInChildren<Image>(true).Single(image => image.name == "Subtitle Background");
            Assert.That(dialogueText.fontSize, Is.EqualTo(40));
            Assert.That(dialogueBackground.color.a, Is.EqualTo(.35f).Within(.001f));

            var cinematicObject = new GameObject("Settings cinematic host", typeof(RectTransform));
            var cinematicHost = cinematicObject.AddComponent<RenderTextureHost>();
            var cinematicAsset = ScriptableObject.CreateInstance<CinematicAsset>();
            cinematicAsset.subtitleCues = new[] { new CinematicSubtitleCue { startSeconds = 0f, text = "Readable cinematic." } };
            cinematicHost.SetPlaybackTime(cinematicAsset, 0f, bootstrap.Settings);
            var cinematicText = cinematicObject.GetComponentsInChildren<Text>(true).Single(label => label.text == "Readable cinematic.");
            var cinematicBackground = cinematicObject.GetComponentsInChildren<Image>(true).Single(image => image.name == "Subtitle Background");
            Assert.That(cinematicText.fontSize, Is.EqualTo(40));
            Assert.That(cinematicBackground.color.a, Is.EqualTo(.35f).Within(.001f));
            bootstrap.Settings.ShowSubtitles = false;
            cinematicHost.SetPlaybackTime(cinematicAsset, 0f, bootstrap.Settings);
            Assert.That(cinematicText.text, Is.Empty);
            Assert.That(cinematicBackground.enabled, Is.False, "Disabling subtitles must not leave an empty black panel.");

            bootstrap.Dialogue.Stop();
            Object.DestroyImmediate(dialogueAsset);
            Object.DestroyImmediate(cinematicAsset);
            Object.DestroyImmediate(cinematicObject);
        }

        private static IEnumerator LoadFreshBootstrap(string saveFile)
        {
            if (GameBootstrap.Instance != null)
            {
                Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }

            var path = Path.Combine(Application.temporaryCachePath, saveFile);
            new SaveGameService(path).Delete();
            GameBootstrap.SessionSaveGameFactory = () => new SaveGameService(path);
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneIds.Bootstrap);
            yield return null;
            yield return null;
            GameBootstrap.SessionSaveGameFactory = null;
        }

        private static IEnumerator LoadFreshBootstrapWithPath(string path)
        {
            if (GameBootstrap.Instance != null)
            {
                Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }
            GameBootstrap.SessionSaveGameFactory = () => new SaveGameService(path);
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneIds.Bootstrap);
            yield return null;
            yield return null;
            GameBootstrap.SessionSaveGameFactory = null;
        }

        private static IEnumerator WaitForChapter(string expected)
        {
            for (var frame = 0; frame < 20; frame++)
            {
                var flow = Object.FindFirstObjectByType<GameFlowController>();
                if (flow != null && flow.CurrentChapterId == expected) yield break;
                yield return null;
            }
            Assert.Fail("Timed out waiting for chapter " + expected);
        }

        private static GameObject SceneObject(string name) => Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(candidate => candidate.scene.IsValid() && candidate.name == name);

        private static void SetSlider(GameObject root, string name, float value)
        {
            root.GetComponentsInChildren<Slider>(true).Single(control => control.name == name).value = value;
        }

        private static void AssertMixer(AudioMixer mixer, string parameter, float expected)
        {
            Assert.That(mixer, Is.Not.Null);
            Assert.That(mixer.GetFloat(parameter, out var actual), Is.True, parameter + " must be exposed.");
            Assert.That(actual, Is.EqualTo(expected).Within(.001f), parameter);
        }

        private static void AssertVisibleButton(GameObject root, string name)
        {
            Assert.That(root.activeInHierarchy, Is.True, root.name);
            var button = root.GetComponentsInChildren<Button>(true).Single(control => control.name == name);
            Assert.That(button.image.canvasRenderer.GetInheritedAlpha(), Is.GreaterThan(.9f), name);
        }

    }
}
