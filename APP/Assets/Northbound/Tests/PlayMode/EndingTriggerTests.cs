using System.Collections;
using System.Linq;
using Northbound.Endings;
using Northbound.Core;
using Northbound.Narrative;
using Northbound.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Northbound.Tests
{
    public sealed class EndingTriggerTests
    {
        private GameObject triggerObject;
        private EndingTrigger trigger;
        private EndingContext confirmed;

        [SetUp]
        public void SetUp()
        {
            confirmed = null;
            triggerObject = new GameObject("Ending Trigger Test");
            var collider = triggerObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            trigger = triggerObject.AddComponent<EndingTrigger>();
            trigger.Configure(EndingDirection.Northbound, null, new EndingResolver(), new NarrativeStateStore());
            trigger.Confirmed += context => confirmed = context;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(triggerObject);
        }

        [Test]
        public void ContinuedCommitment_ShowsIndicatorOnlyAfterPointFourSecondsAndConfirmsAtOnePointTwoFive()
        {
            trigger.Tick(0.39f, true, Vector2.down);

            Assert.That(trigger.IsIndicatorVisible, Is.False);
            Assert.That(confirmed, Is.Null);

            trigger.Tick(0.02f, true, Vector2.down);
            Assert.That(trigger.IsIndicatorVisible, Is.True);

            trigger.Tick(0.83f, true, Vector2.down);
            Assert.That(confirmed, Is.Null);

            trigger.Tick(0.01f, true, Vector2.down);
            Assert.That(confirmed, Is.Not.Null);
            Assert.That(confirmed.Direction, Is.EqualTo(EndingDirection.Northbound));
        }

        [Test]
        public void LeavingBeforeCommitment_CancelsTheHoldAndHidesTheIndicator()
        {
            trigger.Tick(0.8f, true, Vector2.down);
            trigger.Tick(0.1f, false, Vector2.zero);

            Assert.That(trigger.HoldSeconds, Is.EqualTo(0f));
            Assert.That(trigger.IsIndicatorVisible, Is.False);
            Assert.That(confirmed, Is.Null);
        }

        [Test]
        public void StoppingDirectionalMovement_CancelsTheHold()
        {
            trigger.Tick(0.8f, true, Vector2.down);
            trigger.Tick(0.1f, true, Vector2.zero);

            Assert.That(trigger.HoldSeconds, Is.EqualTo(0f));
            Assert.That(confirmed, Is.Null);
        }

        [Test]
        public void InteractionTimeMultiplier_ScalesEndingConfirmationHold()
        {
            var settings = new Northbound.UI.SettingsModel { InteractionTimeMultiplier = 1.5f };
            var configure = typeof(EndingTrigger).GetMethods()
                .SingleOrDefault(method => method.Name == "Configure" && method.GetParameters().Length == 7);
            Assert.That(configure, Is.Not.Null, "EndingTrigger must accept the shared interaction-time provider.");
            configure.Invoke(trigger, new object[]
            {
                EndingDirection.Northbound, null, new EndingResolver(), new NarrativeStateStore(),
                Vector2.down, null, new System.Func<float>(() => settings.InteractionTimeMultiplier)
            });

            trigger.Tick(1.25f, true, Vector2.down);
            Assert.That(confirmed, Is.Null);
            trigger.Tick(.625f, true, Vector2.down);
            Assert.That(confirmed, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Greybridge_FinaleStartsAwayFromTheFourWebsiteCoreEndingZones()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;

            var spawn = GameObject.Find("Spawn Finale").transform.position;
            var triggers = Object.FindObjectsByType<EndingTrigger>(FindObjectsSortMode.None);

            Assert.That(triggers, Has.Length.EqualTo(4));
            foreach (var endingTrigger in triggers)
            {
                Assert.That(Vector2.Distance(spawn, endingTrigger.transform.position), Is.GreaterThan(4f), endingTrigger.name);
                var collider = endingTrigger.gameObject.GetComponent<BoxCollider2D>();
                Assert.That(collider.isTrigger, Is.True, endingTrigger.name);
                var commitmentLength = Mathf.Abs(endingTrigger.CommitmentDirection.x) * collider.size.x +
                    Mathf.Abs(endingTrigger.CommitmentDirection.y) * collider.size.y;
                Assert.That(commitmentLength, Is.GreaterThanOrEqualTo(7.5f),
                    $"{endingTrigger.name} must support the 1.875 second accessibility hold at normal walking speed.");
            }

            CollectionAssert.AreEquivalent(new[]
            {
                EndingDirection.Northbound,
                EndingDirection.HomeChosen,
                EndingDirection.NoMap,
                EndingDirection.PauseJourney
            }, triggers.Select(item => item.Direction));
            Assert.That(triggers.All(item => string.IsNullOrEmpty(item.FriendId)), Is.True,
                "Friend bonds may alter the epilogue, but must not replace the website's four core decisions.");
        }

        [UnityTest]
        public IEnumerator Greybridge_CarRouteConfirmsThroughNormalContinuedPlayerMovement()
        {
            if (GameBootstrap.Instance != null)
            {
                Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;

            var finaleState = new NarrativeState();
            finaleState.Set(Northbound.World.FinaleGatheringInteractor.ReviewedFact, true);
            Assert.That(Object.FindFirstObjectByType<Northbound.World.ChapterWorldController>().Apply("finale", finaleState), Is.True);

            var endingTrigger = System.Array.Find(
                Object.FindObjectsByType<EndingTrigger>(FindObjectsSortMode.None),
                item => item.Direction == EndingDirection.Northbound);
            var jamie = GameObject.Find("Jamie");
            var motor = jamie.GetComponent<Northbound.Player.PlayerMotor>();
            motor.SetMoveInput(endingTrigger.CommitmentDirection);
            jamie.transform.position = endingTrigger.transform.position - (Vector3)(endingTrigger.CommitmentDirection * 3.5f);

            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(1.3f);

            Assert.That(endingTrigger.LastContext, Is.Not.Null);
            Assert.That(endingTrigger.LastContext.Direction, Is.EqualTo(EndingDirection.Northbound));
            motor.ClearMoveInputOverride();
        }

        [UnityTest]
        public IEnumerator Greybridge_FinaleSpawnAndSmallMovementsCannotAccidentallySelectAnEnding()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;

            var spawn = (Vector2)GameObject.Find("Spawn Finale").transform.position;
            var triggers = Object.FindObjectsByType<EndingTrigger>(FindObjectsSortMode.None);
            var smallMovements = new[] { Vector2.zero, Vector2.up, Vector2.down, Vector2.left, Vector2.right };

            foreach (var movement in smallMovements)
            {
                var position = spawn + movement;
                foreach (var endingTrigger in triggers)
                {
                    Assert.That(endingTrigger.GetComponent<Collider2D>().OverlapPoint(position), Is.False,
                        $"{endingTrigger.name} overlaps finale spawn drift {movement}");
                }
            }
        }

        [UnityTest]
        public IEnumerator GreybridgeDirectScene_DisablesEndingRoutesUntilTheFinaleChapterIsActive()
        {
            if (GameBootstrap.Instance != null)
            {
                Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }

            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;

            var trigger = System.Array.Find(
                Object.FindObjectsByType<EndingTrigger>(FindObjectsSortMode.None),
                item => item.Direction == EndingDirection.Northbound);
            Assert.That(trigger.IsAvailable, Is.False);
            Assert.That(trigger.GetComponent<Collider2D>().enabled, Is.False);
            trigger.Tick(EndingTrigger.CommitmentSeconds, true, Vector2.down);
            Assert.That(trigger.LastContext, Is.Null);

            var controller = Object.FindFirstObjectByType<Northbound.World.ChapterWorldController>();
            Assert.That(controller.Apply("finale", new NarrativeState()), Is.True);
            yield return null;

            Assert.That(trigger.IsAvailable, Is.True);
            Assert.That(trigger.GetComponent<Collider2D>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator BootstrapFinaleSequence_PlaysPreChoiceCinematicThenUnlocksRouteAndShowsEndpoint()
        {
            if (GameBootstrap.Instance != null)
            {
                Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }

            SceneManager.LoadScene(SceneIds.Bootstrap, LoadSceneMode.Single);
            yield return null;
            yield return null;

            for (var frame = 0; frame < 10 && Object.FindFirstObjectByType<Northbound.World.ChapterWorldController>() == null; frame++)
            {
                yield return null;
            }

            var bootstrap = GameBootstrap.Instance;
            var state = bootstrap.NarrativeState;
            state.Set("cinematic_finale_complete", false);
            state.Set(Northbound.World.FinaleGatheringInteractor.ReviewedFact, false);
            state.Set("ending_selected", false);
            const string rememberedChoice = "farewell_maya_curious";
            state.Set(rememberedChoice, true);
            Assert.That(ChoiceConsequenceResolver.ApplyImplicit(state, rememberedChoice), Is.True);
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            yield return null;
            Assert.That(state.Has("cinematic_finale_complete"), Is.False);
            AssertNoFinaleMemoryRoute();
            Assert.That(flow.EnterChapter("finale"), Is.True);
            var endingTrigger = System.Array.Find(
                Object.FindObjectsByType<EndingTrigger>(FindObjectsSortMode.None),
                item => item.Direction == EndingDirection.Northbound);

            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True);
            Assert.That(endingTrigger.IsAvailable, Is.False);
            AssertNoFinaleMemoryRoute();
            bootstrap.Cinematics.Tick(2f);
            bootstrap.Cinematics.Skip();
            yield return null;

            Assert.That(state.Has("cinematic_finale_complete"), Is.True);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.False);
            var endingTriggers = Object.FindObjectsByType<EndingTrigger>(FindObjectsSortMode.None);
            Assert.That(endingTriggers, Has.Length.EqualTo(4));
            Assert.That(endingTriggers, Has.All.Matches<EndingTrigger>(trigger => !trigger.IsAvailable));
            Assert.That(endingTrigger.IsAvailable, Is.False,
                "The four ending directions stay locked until Jamie meets the group at the gold finale marker.");
            var layout = Object.FindFirstObjectByType<Northbound.World.GreybridgeWorldLayout>();
            Assert.That(layout, Is.Not.Null);
            var routeRegions = new[]
            {
                "Finale Car Region",
                "Finale Home Region",
                "Finale Road Region",
                "Finale Friends Region"
            };
            Assert.That(routeRegions.All(name => !layout.transform.Find(name).gameObject.activeSelf), Is.True,
                "The four route regions must be revealed by the gathering interaction, not by entering the chapter.");
            AssertNoFinaleMemoryRoute();
            var gathering = Object.FindFirstObjectByType<Northbound.World.FinaleGatheringInteractor>();
            Assert.That(gathering, Is.Not.Null);
            gathering.Interact(GameObject.Find("Jamie"));
            yield return null;
            Assert.That(state.Has(Northbound.World.FinaleGatheringInteractor.ReviewedFact), Is.True);
            Assert.That(endingTriggers, Has.All.Matches<EndingTrigger>(trigger => trigger.IsAvailable));
            Assert.That(routeRegions.All(name => layout.transform.Find(name).gameObject.activeSelf), Is.True);
            var gatheringPosition = (Vector2)gathering.transform.position;
            foreach (var trigger in endingTriggers)
            {
                var routeDirection = ((Vector2)trigger.transform.position - gatheringPosition).normalized;
                Assert.That(Vector2.Dot(trigger.CommitmentDirection, routeDirection), Is.GreaterThan(.98f),
                    $"{trigger.name} must confirm in the same direction its route sign tells the player to walk.");
                Assert.That(layout.HasClearWalkablePath(gatheringPosition, trigger.transform.position), Is.True, trigger.name);
            }
            foreach (var regionName in routeRegions)
            {
                var region = layout.transform.Find(regionName);
                Assert.That(region.GetComponentInChildren<SpriteRenderer>(true), Is.Not.Null,
                    $"{regionName} needs a visible route beacon, not only an invisible trigger region.");
                var plaque = region.GetComponentInChildren<Northbound.Guidance.DoorNamePlaque>(true);
                Assert.That(plaque, Is.Not.Null);
                Assert.That(plaque.LabelText, Is.Not.Empty);
            }
            Assert.That(flow.EnterChapter("finale"), Is.True);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.False);
            AssertNoFinaleMemoryRoute();
            var jamie = GameObject.Find("Jamie");
            var motor = jamie.GetComponent<Northbound.Player.PlayerMotor>();
            motor.SetMoveInput(endingTrigger.CommitmentDirection);
            jamie.transform.position = endingTrigger.transform.position - (Vector3)(endingTrigger.CommitmentDirection * 3.5f);

            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(1.3f);

            Assert.That(endingTrigger.LastContext, Is.Not.Null);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.False);
            Assert.That(bootstrap.InputGate.IsBlocked, Is.True);
            Assert.That(state.Has("ending_northbound"), Is.True);
            Assert.That(endingTriggers, Has.All.Matches<EndingTrigger>(trigger => !trigger.IsAvailable),
                "Selecting one ending must disable the other three immediately.");
            Assert.That(routeRegions.All(name => !layout.transform.Find(name).gameObject.activeSelf), Is.True);
            Assert.That(bootstrap.Dialogue.IsRunning, Is.True, "The selected endpoint must play its authored ending dialogue before the end card.");
            var safety = 0;
            while (bootstrap.Dialogue.IsRunning && safety++ < 20)
            {
                if (bootstrap.Dialogue.Current.choices != null && bootstrap.Dialogue.Current.choices.Count > 0)
                {
                    bootstrap.Dialogue.Choose(0);
                }
                else
                {
                    bootstrap.Dialogue.Advance();
                }
                yield return null;
            }
            Assert.That(safety, Is.LessThan(20));
            Assert.That(bootstrap.Endings.IsShowing, Is.True);
            Assert.That(bootstrap.Endings.CurrentContext, Is.SameAs(endingTrigger.LastContext));
            Assert.That(bootstrap.Endings.VisibleEndCard, Is.Not.Empty,
                "The end card must remain visible in the active English or Chinese language.");
            StringAssert.DoesNotContain("Scene:", bootstrap.Endings.VisibleStaging);
            StringAssert.DoesNotContain("Moment:", bootstrap.Endings.VisibleStaging);
            StringAssert.DoesNotContain("Keeps:", bootstrap.Endings.VisibleStaging);
            Assert.That(bootstrap.Endings.VisibleStaging, Is.Not.Empty);
            var expectedEcho = GameText.IsChinese
                ? endingTrigger.LastContext.HistoryEchoTextChinese
                : endingTrigger.LastContext.HistoryEchoText;
            Assert.That(expectedEcho, Is.Not.Empty);
            StringAssert.Contains(expectedEcho, bootstrap.Endings.VisibleStaging,
                "A choice made before the finale must be visibly recalled in the ending presentation.");
            bootstrap.Endings.ReturnToTitle();
            motor.ClearMoveInputOverride();
        }

        [UnityTest]
        public IEnumerator BootstrapFinale_AvailableRouteFeedbackMatchesVisibleRegionsForStrongAndMixedHistories()
        {
            if (GameBootstrap.Instance != null)
            {
                Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }

            SceneManager.LoadScene(SceneIds.Bootstrap, LoadSceneMode.Single);
            yield return null;
            yield return null;

            for (var frame = 0; frame < 10 && Object.FindFirstObjectByType<Northbound.World.GreybridgeWorldLayout>() == null; frame++)
            {
                yield return null;
            }

            var bootstrap = GameBootstrap.Instance;
            GameText.Use(GameLanguage.English);
            var state = bootstrap.NarrativeState;
            state.Set("cinematic_finale_complete", true);
            state.Set(Northbound.World.FinaleGatheringInteractor.ReviewedFact, false);
            state.Set("ending_selected", false);
            ResetTendencyCounters(state);
            SetChapterMarks(state,
                ChapterStoryMarkResolver.ChapterOnePlanFact,
                ChapterStoryMarkResolver.ChapterTwoPlanFact,
                ChapterStoryMarkResolver.ChapterThreePlanFact,
                ChapterStoryMarkResolver.ChapterFourAgencyFact);

            var flow = Object.FindFirstObjectByType<GameFlowController>();
            Assert.That(flow.EnterChapter("finale"), Is.True);
            yield return null;

            var gathering = Object.FindFirstObjectByType<Northbound.World.FinaleGatheringInteractor>();
            var layout = Object.FindFirstObjectByType<Northbound.World.GreybridgeWorldLayout>();
            var endingTriggers = Object.FindObjectsByType<EndingTrigger>(FindObjectsSortMode.None);
            Assert.That(gathering, Is.Not.Null);
            Assert.That(gathering.Prompt, Is.EqualTo("Review the available routes"));

            gathering.Interact(GameObject.Find("Jamie"));
            yield return null;

            Assert.That(bootstrap.Feedback.VisibleMessage, Does.StartWith("Three routes remain"));
            Assert.That(bootstrap.Feedback.VisibleMessage, Does.Contain("closed the road home"));
            Assert.That(bootstrap.Feedback.VisibleMessage, Does.Contain("southeast northbound"));
            Assert.That(bootstrap.Feedback.VisibleMessage, Does.Not.Contain("southwest toward home"));
            AssertFinaleAvailability(endingTriggers, northbound: true, home: false);
            AssertFinaleRegions(layout, northbound: true, home: false);

            state.Set(Northbound.World.FinaleGatheringInteractor.ReviewedFact, false);
            SetChapterMarks(state,
                ChapterStoryMarkResolver.ChapterOneAgencyFact,
                ChapterStoryMarkResolver.ChapterTwoAgencyFact,
                ChapterStoryMarkResolver.ChapterThreeAgencyFact,
                ChapterStoryMarkResolver.ChapterFourPlanFact);
            gathering.Interact(GameObject.Find("Jamie"));
            yield return null;

            Assert.That(bootstrap.Feedback.VisibleMessage, Does.StartWith("Three routes remain"));
            Assert.That(bootstrap.Feedback.VisibleMessage, Does.Contain("closed the northbound road"));
            Assert.That(bootstrap.Feedback.VisibleMessage, Does.Not.Contain("southeast northbound"));
            Assert.That(bootstrap.Feedback.VisibleMessage, Does.Contain("southwest toward home"));
            AssertFinaleAvailability(endingTriggers, northbound: false, home: true);
            AssertFinaleRegions(layout, northbound: false, home: true);

            state.Set(Northbound.World.FinaleGatheringInteractor.ReviewedFact, false);
            SetChapterMarks(state,
                ChapterStoryMarkResolver.ChapterOnePlanFact,
                ChapterStoryMarkResolver.ChapterTwoPlanFact,
                ChapterStoryMarkResolver.ChapterThreeAgencyFact,
                ChapterStoryMarkResolver.ChapterFourAgencyFact);
            gathering.Interact(GameObject.Find("Jamie"));
            yield return null;

            Assert.That(bootstrap.Feedback.VisibleMessage, Does.StartWith("Four routes remain"));
            Assert.That(bootstrap.Feedback.VisibleMessage, Does.Contain("southeast northbound"));
            Assert.That(bootstrap.Feedback.VisibleMessage, Does.Contain("southwest toward home"));
            AssertFinaleAvailability(endingTriggers, northbound: true, home: true);
            AssertFinaleRegions(layout, northbound: true, home: true);
        }

        private static void SetChapterMarks(NarrativeStateStore state, params string[] marks)
        {
            foreach (var mark in marks)
            {
                Assert.That(ChapterStoryMarkResolver.TrySetExclusive(state, mark), Is.True, mark);
            }
        }

        private static void ResetTendencyCounters(NarrativeStateStore state)
        {
            state.Add(ChapterStoryMarkResolver.CommitmentCounterId, -state.GetInt(ChapterStoryMarkResolver.CommitmentCounterId));
            state.Add(ChapterStoryMarkResolver.AgencyCounterId, -state.GetInt(ChapterStoryMarkResolver.AgencyCounterId));
        }

        private static void AssertFinaleAvailability(EndingTrigger[] triggers, bool northbound, bool home)
        {
            Assert.That(triggers.Single(item => item.Direction == EndingDirection.Northbound).IsAvailable, Is.EqualTo(northbound));
            Assert.That(triggers.Single(item => item.Direction == EndingDirection.HomeChosen).IsAvailable, Is.EqualTo(home));
            Assert.That(triggers.Single(item => item.Direction == EndingDirection.NoMap).IsAvailable, Is.True);
            Assert.That(triggers.Single(item => item.Direction == EndingDirection.PauseJourney).IsAvailable, Is.True);
        }

        private static void AssertFinaleRegions(Northbound.World.GreybridgeWorldLayout layout, bool northbound, bool home)
        {
            Assert.That(layout.transform.Find("Finale Car Region").gameObject.activeSelf, Is.EqualTo(northbound));
            Assert.That(layout.transform.Find("Finale Home Region").gameObject.activeSelf, Is.EqualTo(home));
            Assert.That(layout.transform.Find("Finale Road Region").gameObject.activeSelf, Is.True);
            Assert.That(layout.transform.Find("Finale Friends Region").gameObject.activeSelf, Is.True);
        }

        private static void AssertNoFinaleMemoryRoute()
        {
            Assert.That(System.Array.Exists(
                Object.FindObjectsByType<Northbound.Cinematics.CinematicRouteTrigger>(FindObjectsSortMode.None),
                candidate => candidate.CinematicId == "finale"), Is.False);
        }
    }
}
