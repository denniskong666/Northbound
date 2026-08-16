using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Northbound.Cinematics;
using Northbound.Content;
using Northbound.Core;
using Northbound.Narrative;
using Northbound.Quests;
using Northbound.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Video;

namespace Northbound.Tests
{
    public sealed class CinematicRouteContractTests
    {
        private static readonly MethodInfo CompleteQuest = typeof(NarrativeContentDirector).GetMethod(
            "OnQuestCompleted",
            BindingFlags.Instance | BindingFlags.NonPublic);

        [UnityTearDown]
        public IEnumerator TearDownRuntime()
        {
            GameBootstrap.SessionSaveGameFactory = null;
            if (GameBootstrap.Instance == null) yield break;
            UnityEngine.Object.Destroy(GameBootstrap.Instance.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RuntimeCatalog_LinksExactlyTheSixProductionVideos()
        {
            yield return LoadRuntime();

            var expectedClips = new Dictionary<string, string>
            {
                ["opening"] = "opening_proxy",
                ["maya"] = "maya_proxy",
                ["noah"] = "noah_proxy",
                ["leo"] = "leo_proxy",
                ["rooftop"] = "rooftop_proxy",
                ["finale"] = "finale_proxy"
            };
            var assets = GameBootstrap.Instance.CinematicCatalog.All;

            Assert.That(assets, Has.Length.EqualTo(expectedClips.Count));
            Assert.That(assets.Select(asset => asset.id), Is.EquivalentTo(expectedClips.Keys));
            Assert.That(assets.Select(asset => asset.id).Distinct().Count(), Is.EqualTo(expectedClips.Count));
            foreach (var asset in assets)
            {
                Assert.That(asset.clip, Is.Not.Null, asset.id);
                Assert.That(asset.clip.name, Is.EqualTo(expectedClips[asset.id]), asset.id);
                Assert.That(asset.clip.width, Is.EqualTo(1920), asset.id);
                Assert.That(asset.clip.height, Is.EqualTo(1080), asset.id);
                Assert.That(asset.clip.length, Is.GreaterThan(30d), asset.id);
            }

            var manifest = UnityEngine.Object.FindFirstObjectByType<NarrativeContentDirector>().Manifest;
            foreach (var cinematic in manifest.cinematics)
            {
                Assert.That(manifest.triggers.Any(trigger => trigger.routeType == "dialogue" && trigger.targetId == cinematic.dialogueId), Is.False,
                    $"{cinematic.dialogueId} is subtitle copy for {cinematic.id}, not a second dialogue-box playback.");
            }
        }

        [UnityTest]
        public IEnumerator CharacterMissionDialogue_StopsBeforeItsCinematicPayoff()
        {
            yield return LoadRuntime();
            var catalog = Resources.Load<NarrativeContentCatalog>("Northbound/NarrativeContentCatalog");

            var mayaSetup = string.Join(" ", catalog.Dialogue("first_light_dialogue").lines.Select(line => line.text));
            StringAssert.DoesNotContain("hands Jamie the studio key", mayaSetup);
            StringAssert.Contains("still-locked door", mayaSetup);

            var noahSetup = string.Join(" ", catalog.Dialogue("static_dialogue").lines.Select(line => line.text));
            StringAssert.DoesNotContain("keeps walking toward the radio station", noahSetup);
            StringAssert.Contains("closes in an hour", noahSetup);

            var leoSetup = string.Join(" ", catalog.Dialogue("last_night_open_dialogue").lines.Select(line => line.text));
            StringAssert.DoesNotContain("turns the sign to CLOSED", leoSetup);
            StringAssert.Contains("last customers are still eating", leoSetup);
        }

        [UnityTest]
        public IEnumerator PrologueAndFinale_AutomaticallyPlayTheirBoundaryVideosOnce()
        {
            yield return LoadRuntime();
            var bootstrap = GameBootstrap.Instance;
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var finished = new List<string>();
            bootstrap.Cinematics.Finished += finished.Add;

            Assert.That(flow.EnterChapter("prologue"), Is.True);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True);
            FinishCurrentCinematic(bootstrap);
            yield return null;

            Assert.That(finished, Is.EqualTo(new[] { "opening" }));
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_1"),
                "Finishing the opening must return control in Chapter 1.");
            Assert.That(flow.EnterChapter("prologue"), Is.True);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.False,
                "The completed opening must not replay when prologue is restored.");

            Assert.That(flow.EnterChapter("finale"), Is.True);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True);
            FinishCurrentCinematic(bootstrap);
            yield return null;

            Assert.That(finished, Is.EqualTo(new[] { "opening", "finale" }));
            Assert.That(flow.EnterChapter("finale"), Is.True);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.False,
                "The completed finale introduction must not replay before the ending choice.");
        }

        [UnityTest]
        public IEnumerator SupportedFriendMainPath_PlaysAllSixProductionVideosAtTheirStoryNodes()
        {
            yield return LoadRuntime();
            Assert.That(CompleteQuest, Is.Not.Null, "The runtime quest-completion route must remain available.");
            var bootstrap = GameBootstrap.Instance;
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var director = UnityEngine.Object.FindFirstObjectByType<NarrativeContentDirector>();
            var finished = new List<string>();
            bootstrap.Cinematics.Finished += finished.Add;

            Assert.That(flow.EnterChapter("prologue"), Is.True);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True, "A fresh path must open with the childhood promise.");
            FinishCurrentCinematic(bootstrap);
            yield return null;
            Assert.That(finished, Is.EqualTo(new[] { "opening" }));
            Assert.That(flow.EnterChapter("chapter_3_day_3"), Is.True);

            CompleteQuest.Invoke(director, new object[] { "first_light" });
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True, "First Light must immediately show Maya's supported future.");
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("maya")), Is.True);
            FinishCurrentCinematic(bootstrap);
            yield return null;
            Assert.That(finished, Is.EqualTo(new[] { "opening", "maya" }));
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_3_day_2"));

            CompleteQuest.Invoke(director, new object[] { "static" });
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True, "Static must immediately show Noah choosing the radio work.");
            FinishCurrentCinematic(bootstrap);
            yield return null;
            Assert.That(finished, Is.EqualTo(new[] { "opening", "maya", "noah" }));

            CompleteQuest.Invoke(director, new object[] { "last_night_open" });
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True, "Last Night Open must immediately show Leo closing the diner.");
            FinishCurrentCinematic(bootstrap);
            yield return null;
            Assert.That(finished, Is.EqualTo(new[] { "opening", "maya", "noah", "leo" }));
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True, "Leo's scene must hand directly to the rooftop fracture.");

            FinishCurrentCinematic(bootstrap);
            yield return null;
            Assert.That(finished, Is.EqualTo(new[] { "opening", "maya", "noah", "leo", "rooftop" }));
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_3_day_2"),
                "The rooftop video must hand control to Jamie's decision before Chapter 4 begins.");
            yield return CompleteRooftopDecision(bootstrap, 1, ChapterStoryMarkResolver.ChapterThreeBalanceFact);
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_4"));
            Assert.That(new[] { "maya", "noah", "leo", "rooftop" }
                .Any(id => bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact(id))), Is.False,
                "Every watched route must clear its resumable pending fact.");

            Assert.That(flow.EnterChapter("finale"), Is.True);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True, "The dawn gathering must begin with Are You Coming?.");
            FinishCurrentCinematic(bootstrap);
            yield return null;
            Assert.That(finished, Is.EqualTo(new[] { "opening", "maya", "noah", "leo", "rooftop", "finale" }));
        }

        [UnityTest]
        public IEnumerator EliasCrossroadsPath_PreservesMissedFriendVideosAndStillReachesRooftop()
        {
            yield return LoadRuntime();
            var bootstrap = GameBootstrap.Instance;
            var director = UnityEngine.Object.FindFirstObjectByType<NarrativeContentDirector>();
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var finished = new List<string>();
            bootstrap.Cinematics.Finished += finished.Add;
            Assert.That(flow.EnterChapter("chapter_3_day_3"), Is.True);

            CompleteQuest.Invoke(director, new object[] { "alternator" });
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.False);
            CompleteQuest.Invoke(director, new object[] { "road_test" });
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.False);
            CompleteQuest.Invoke(director, new object[] { "pack_trunk" });
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True);
            FinishCurrentCinematic(bootstrap);
            yield return null;

            Assert.That(finished, Is.EqualTo(new[] { "rooftop" }),
                "Choosing the conflicting Elias missions must not fabricate unearned friend scenes.");
            yield return CompleteRooftopDecision(bootstrap, 2, ChapterStoryMarkResolver.ChapterThreeAgencyFact);
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_4"));
        }

        [UnityTest]
        public IEnumerator PackTrunk_PublicQuestFlowSettlesThenPlaysRooftopBeforeChapterFour()
        {
            yield return LoadRuntime();
            var bootstrap = GameBootstrap.Instance;
            var director = UnityEngine.Object.FindFirstObjectByType<NarrativeContentDirector>();
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var finished = new List<string>();
            bootstrap.Cinematics.Finished += finished.Add;

            bootstrap.NarrativeState.Set(QuestRunner.CompletionFact("road_test"), true);
            Assert.That(flow.EnterChapter("chapter_3_day_2"), Is.True);
            Assert.That(director.CanActivate("pack_trunk_trigger"), Is.True);
            Assert.That(director.Activate("pack_trunk_trigger"), Is.True);
            yield return CompleteDialogueChain(bootstrap);
            Assert.That(director.ActiveQuestId, Is.EqualTo("pack_trunk"));

            while (!string.IsNullOrWhiteSpace(director.NextObjectiveId))
            {
                Assert.That(director.CompleteActiveQuestObjective(director.NextObjectiveId, 99), Is.True);
                yield return null;
            }

            Assert.That(bootstrap.NarrativeState.Has(QuestRunner.CompletionFact("pack_trunk")), Is.True);
            Assert.That(bootstrap.NarrativeState.Has("packed_trunk"), Is.True,
                "The quest reward must be settled before its completion event starts the video.");
            Assert.That(bootstrap.NarrativeState.Has("quest_things_we_leave_available"), Is.True,
                "The next quest must be available before the rooftop transition starts.");
            Assert.That(director.ActiveQuestId, Is.Null);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True,
                "The Elias trunk route must still play the fixed rooftop main-story video.");

            FinishCurrentCinematic(bootstrap);
            yield return null;

            Assert.That(finished, Is.EqualTo(new[] { "rooftop" }));
            yield return CompleteRooftopDecision(bootstrap, 0, ChapterStoryMarkResolver.ChapterThreePlanFact);
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_4"));
        }

        [UnityTest]
        public IEnumerator ChapterFourRestore_RecoversARelevantMissingRooftopVideoWithoutFriendHighlights()
        {
            yield return LoadRuntime();
            var bootstrap = GameBootstrap.Instance;
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var finished = new List<string>();
            bootstrap.Cinematics.Finished += finished.Add;
            bootstrap.NarrativeState.Set(QuestRunner.CompletionFact("pack_trunk"), true);

            Assert.That(flow.EnterChapter("chapter_4"), Is.True);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True,
                "A pre-fix save that reached Chapter 4 after packing the trunk must recover the missing rooftop video.");
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("rooftop")), Is.True);
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("maya")), Is.False);
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("noah")), Is.False);
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("leo")), Is.False);

            FinishCurrentCinematic(bootstrap);
            yield return null;

            Assert.That(finished, Is.EqualTo(new[] { "rooftop" }));
            yield return CompleteRooftopDecision(bootstrap, 1, ChapterStoryMarkResolver.ChapterThreeBalanceFact);
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_4"));
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("rooftop")), Is.False);
        }

        [UnityTest]
        public IEnumerator ChapterFourRestore_DiscardsStaleFriendVideosAndNeverRegressesTheStory()
        {
            yield return LoadRuntime();
            var bootstrap = GameBootstrap.Instance;
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var finished = new List<string>();
            bootstrap.Cinematics.Finished += finished.Add;
            bootstrap.NarrativeState.Set(QuestRunner.CompletionFact("first_light"), true);
            bootstrap.NarrativeState.Set(QuestRunner.CompletionFact("static"), true);
            bootstrap.NarrativeState.Set(QuestRunner.CompletionFact("pack_trunk"), true);
            bootstrap.NarrativeState.Set(NarrativeContentDirector.CinematicRoutePendingFact("maya"), true);
            bootstrap.NarrativeState.Set(NarrativeContentDirector.CinematicRoutePendingFact("noah"), true);

            Assert.That(flow.EnterChapter("chapter_4"), Is.True);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True,
                "Chapter 4 may recover the required rooftop fracture, but not old character branches.");
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("maya")), Is.False);
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("noah")), Is.False);

            FinishCurrentCinematic(bootstrap);
            yield return null;

            Assert.That(finished, Is.EqualTo(new[] { "rooftop" }));
            yield return CompleteRooftopDecision(bootstrap, 0, ChapterStoryMarkResolver.ChapterThreePlanFact);
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_4"),
                "Recovering a missed video must never send a later save back to Chapter 3.");
        }

        [UnityTest]
        public IEnumerator RooftopDecisionRestore_AfterChoiceResumesItsResponseBeforeCompleting()
        {
            yield return LoadRuntime();
            var bootstrap = GameBootstrap.Instance;
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            bootstrap.NarrativeState.Set(QuestRunner.CompletionFact("pack_trunk"), true);
            bootstrap.NarrativeState.Set("cinematic_rooftop_complete", true);
            bootstrap.NarrativeState.Set(ChapterStoryMarkResolver.ChapterThreeBalanceFact, true);

            Assert.That(flow.EnterChapter("chapter_3_day_2"), Is.True);
            yield return null;

            Assert.That(bootstrap.Dialogue.ActiveDialogueId, Is.EqualTo("rooftop_decision"));
            Assert.That(bootstrap.Dialogue.CurrentLineIndex, Is.EqualTo(8),
                "A save made after choosing Balance must resume Maya's response instead of skipping it or replaying the choice.");
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.RooftopDecisionCompletionFact), Is.False);

            while (bootstrap.Dialogue.IsRunning)
            {
                Assert.That(bootstrap.Dialogue.Advance(), Is.True);
                yield return null;
            }

            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.RooftopDecisionCompletionFact), Is.True);
            Assert.That(bootstrap.SaveGame.LoadOrNew().Has(NarrativeContentDirector.RooftopDecisionCompletionFact), Is.True);
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_4"));
        }

        [UnityTest]
        public IEnumerator RooftopDecisionRestore_AfterWholeSceneDoesNotReplayIt()
        {
            yield return LoadRuntime();
            var bootstrap = GameBootstrap.Instance;
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            bootstrap.NarrativeState.Set(QuestRunner.CompletionFact("pack_trunk"), true);
            bootstrap.NarrativeState.Set("cinematic_rooftop_complete", true);
            bootstrap.NarrativeState.Set(ChapterStoryMarkResolver.ChapterThreeAgencyFact, true);
            bootstrap.NarrativeState.Set(NarrativeContentDirector.RooftopDecisionCompletionFact, true);

            Assert.That(flow.EnterChapter("chapter_4"), Is.True);
            yield return null;

            Assert.That(bootstrap.Dialogue.IsRunning, Is.False,
                "A fully completed rooftop decision must not replay when Chapter 4 is restored.");
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_4"));
        }

        [UnityTest]
        public IEnumerator BeforeMorningRestore_SelectedChoiceResumesResponseWithoutApplyingConsequencesTwice()
        {
            yield return LoadRuntime();
            var bootstrap = GameBootstrap.Instance;
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var director = UnityEngine.Object.FindFirstObjectByType<NarrativeContentDirector>();
            bootstrap.NarrativeState.Set(QuestRunner.CompletionFact("spare_key"), true);
            bootstrap.NarrativeState.Set(ChapterStoryMarkResolver.ChapterFourBalanceFact, true);
            bootstrap.NarrativeState.Add(ChapterStoryMarkResolver.CommitmentCounterId, 3);
            bootstrap.NarrativeState.Add(ChapterStoryMarkResolver.RootednessCounterId, 5);
            bootstrap.NarrativeState.Add(ChapterStoryMarkResolver.AgencyCounterId, 5);

            Assert.That(flow.EnterChapter("chapter_4"), Is.True);
            Assert.That(director.CanActivate("before_morning_trigger"), Is.True);
            Assert.That(director.Activate("before_morning_trigger"), Is.True);

            Assert.That(bootstrap.Dialogue.ActiveDialogueId, Is.EqualTo("before_morning_dialogue"));
            Assert.That(bootstrap.Dialogue.CurrentLineIndex, Is.EqualTo(6),
                "A saved Balance choice must resume its response rather than replaying the choice node.");
            yield return CompleteDialogueChain(bootstrap);

            Assert.That(bootstrap.NarrativeState.GetInt(ChapterStoryMarkResolver.CommitmentCounterId), Is.EqualTo(3));
            Assert.That(bootstrap.NarrativeState.GetInt(ChapterStoryMarkResolver.RootednessCounterId), Is.EqualTo(5));
            Assert.That(bootstrap.NarrativeState.GetInt(ChapterStoryMarkResolver.AgencyCounterId), Is.EqualTo(5));
            CompleteEveryObjective(director);
            yield return null;
            Assert.That(flow.CurrentChapterId, Is.EqualTo("finale"));
        }

        [UnityTest]
        public IEnumerator VisitNoah_PlayerInteractionCompletesObjectiveOnlyAfterFarewellDialogue()
        {
            yield return LoadRuntime();
            var bootstrap = GameBootstrap.Instance;
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var director = UnityEngine.Object.FindFirstObjectByType<NarrativeContentDirector>();
            bootstrap.Menus.HideTitle();
            bootstrap.NarrativeState.Set(QuestRunner.CompletionFact("spare_key"), true);

            Assert.That(flow.EnterChapter("chapter_4"), Is.True);
            Assert.That(director.Activate("before_morning_trigger"), Is.True);
            yield return CompleteDialogueChain(bootstrap);
            Assert.That(director.ActiveQuestId, Is.EqualTo("before_morning"));
            Assert.That(director.CompleteActiveQuestObjective("visit_maya"), Is.True);
            Assert.That(director.NextObjectiveId, Is.EqualTo("visit_noah"));

            var visitNoah = UnityEngine.Object.FindObjectsByType<NarrativeObjectiveTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(trigger => trigger.QuestId == "before_morning" && trigger.ObjectiveId == "visit_noah");
            var locations = UnityEngine.Object.FindFirstObjectByType<Northbound.World.LocationTransitionController>();
            locations.SetTransitionDuration(0f);
            Assert.That(locations.StartTravel("noah_electronics"), Is.True);
            while (locations.IsTravelling) yield return null;

            var jamie = GameObject.Find("Jamie");
            var interactor = jamie.GetComponent<Northbound.Interaction.PlayerInteractor>();
            jamie.transform.position = visitNoah.transform.position;
            Physics2D.SyncTransforms();
            interactor.RefreshTarget();
            Assert.That(interactor.CurrentInteractable, Is.SameAs(visitNoah));

            interactor.TryInteract();
            yield return null;

            var completionFact = QuestRunner.ObjectiveCompletionFactId("before_morning", "visit_noah");
            Assert.That(bootstrap.Dialogue.ActiveDialogueId, Is.EqualTo("farewell_noah"));
            Assert.That(bootstrap.Dialogue.IsRunning, Is.True);
            Assert.That(bootstrap.NarrativeState.Has(completionFact), Is.False,
                "Starting Noah's farewell must not count the visit before the conversation finishes.");

            yield return CompleteDialogueChain(bootstrap);

            Assert.That(bootstrap.NarrativeState.Has(completionFact), Is.True);
            Assert.That(director.NextObjectiveId, Is.EqualTo("visit_leo"));
        }

        [UnityTest]
        public IEnumerator FirstLight_PublicQuestFlowPlaysOnlyMayaThenAdvances()
        {
            yield return LoadRuntime();
            var bootstrap = GameBootstrap.Instance;
            var director = UnityEngine.Object.FindFirstObjectByType<NarrativeContentDirector>();
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var finished = new List<string>();
            bootstrap.Cinematics.Finished += finished.Add;

            bootstrap.NarrativeState.Set(QuestRunner.CompletionFact("one_more_table"), true);
            Assert.That(flow.EnterChapter("chapter_3_day_3"), Is.True);
            Assert.That(director.CanActivate("first_light_trigger"), Is.True);
            Assert.That(director.Activate("first_light_trigger"), Is.True);
            yield return CompleteDialogueChain(bootstrap);
            Assert.That(director.ActiveQuestId, Is.EqualTo("first_light"));

            while (!string.IsNullOrWhiteSpace(director.NextObjectiveId))
            {
                Assert.That(director.CompleteActiveQuestObjective(director.NextObjectiveId, 99), Is.True);
                yield return null;
            }

            Assert.That(bootstrap.NarrativeState.Has("attended_maya_exhibition"), Is.True);
            Assert.That(director.ActiveQuestId, Is.Null);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True);
            FinishCurrentCinematic(bootstrap);
            yield return null;

            Assert.That(finished, Is.EqualTo(new[] { "maya" }));
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_3_day_2"));
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("maya")), Is.False);
        }

        [UnityTest]
        public IEnumerator Static_PublicQuestFlowPlaysOnlyNoahAndStaysInDayTwo()
        {
            yield return LoadRuntime();
            var bootstrap = GameBootstrap.Instance;
            var director = UnityEngine.Object.FindFirstObjectByType<NarrativeContentDirector>();
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var finished = new List<string>();
            bootstrap.Cinematics.Finished += finished.Add;

            bootstrap.NarrativeState.Set(QuestRunner.CompletionFact("first_light"), true);
            Assert.That(flow.EnterChapter("chapter_3_day_2"), Is.True);
            yield return CompletePublicQuest(director, bootstrap, "static_trigger", "static");

            Assert.That(bootstrap.NarrativeState.Has("helped_noah"), Is.True);
            Assert.That(director.ActiveQuestId, Is.Null);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True);
            FinishCurrentCinematic(bootstrap);
            yield return null;

            Assert.That(finished, Is.EqualTo(new[] { "noah" }));
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_3_day_2"));
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("noah")), Is.False);
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("leo")), Is.False);
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("rooftop")), Is.False);
        }

        [UnityTest]
        public IEnumerator StaticVideo_PlayRejectedOnceRetriesWithoutAdvancingOrLooping()
        {
            yield return LoadRuntime();
            var bootstrap = GameBootstrap.Instance;
            var director = UnityEngine.Object.FindFirstObjectByType<NarrativeContentDirector>();
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var playback = new ControllablePlayback();
            bootstrap.Cinematics.Initialize(
                bootstrap.InputGate,
                bootstrap.NarrativeState,
                bootstrap.Settings,
                playback,
                new SilentPresentation(),
                bootstrap.SaveGame);

            bootstrap.NarrativeState.Set(QuestRunner.CompletionFact("first_light"), true);
            Assert.That(flow.EnterChapter("chapter_3_day_2"), Is.True);
            Assert.That(director.CanActivate("static_trigger"), Is.True);
            Assert.That(director.Activate("static_trigger"), Is.True);
            yield return CompleteDialogueChain(bootstrap);
            Assert.That(director.ActiveQuestId, Is.EqualTo("static"));

            Assert.That(bootstrap.PlayCinematic("opening"), Is.True);
            Assert.That(playback.PrepareCount, Is.EqualTo(1));
            LogAssert.Expect(LogType.Warning, "Cinematic 'noah' failed (the player was not ready); retrying once without advancing the story.");
            CompleteEveryObjective(director);

            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("noah")), Is.True);
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_3_day_2"));
            bootstrap.Cinematics.Cancel();
            yield return WaitForPrepareCount(playback, 2);

            Assert.That(playback.PrepareCount, Is.EqualTo(2), "The rejected Noah start must retry once on the next frame.");
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True);
            FinishCurrentCinematic(bootstrap);
            yield return null;

            Assert.That(bootstrap.NarrativeState.Has("cinematic_noah_complete"), Is.True);
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("noah")), Is.False);
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_3_day_2"));
            Assert.That(playback.PrepareCount, Is.EqualTo(2), "Completing the retry must not start a playback loop.");
        }

        [UnityTest]
        public IEnumerator StaticVideo_TwoPlaybackFailuresStayPendingUntilChapterResumeThenRecover()
        {
            yield return LoadRuntime();
            var bootstrap = GameBootstrap.Instance;
            var director = UnityEngine.Object.FindFirstObjectByType<NarrativeContentDirector>();
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var playback = new ControllablePlayback();
            bootstrap.Cinematics.Initialize(
                bootstrap.InputGate,
                bootstrap.NarrativeState,
                bootstrap.Settings,
                playback,
                new SilentPresentation(),
                bootstrap.SaveGame);

            bootstrap.NarrativeState.Set(QuestRunner.CompletionFact("first_light"), true);
            Assert.That(flow.EnterChapter("chapter_3_day_2"), Is.True);
            yield return CompletePublicQuest(director, bootstrap, "static_trigger", "static");
            Assert.That(playback.PrepareCount, Is.EqualTo(1));

            LogAssert.Expect(LogType.Warning, "Cinematic 'noah' failed (decode failed); retrying once without advancing the story.");
            playback.RaiseFailed("decode failed");
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.False);
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_3_day_2"));
            yield return WaitForPrepareCount(playback, 2);

            Assert.That(playback.PrepareCount, Is.EqualTo(2));
            playback.RaiseFailed("decode failed again");
            yield return null;
            yield return null;

            Assert.That(playback.PrepareCount, Is.EqualTo(2), "A persistent decoder failure must not create an automatic retry loop.");
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("noah")), Is.True);
            Assert.That(bootstrap.NarrativeState.Has("cinematic_noah_complete"), Is.False);
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_3_day_2"));
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("maya")), Is.False,
                "Retry recovery must not invent a different friend's branch video.");

            Assert.That(flow.EnterChapter("chapter_3_day_2"), Is.True);
            Assert.That(playback.PrepareCount, Is.EqualTo(3), "Re-entering the saved chapter must provide a fresh recovery attempt.");
            FinishCurrentCinematic(bootstrap);
            yield return null;

            Assert.That(bootstrap.NarrativeState.Has("cinematic_noah_complete"), Is.True);
            Assert.That(bootstrap.NarrativeState.Has(NarrativeContentDirector.CinematicRoutePendingFact("noah")), Is.False);
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_3_day_2"));
        }

        [UnityTest]
        public IEnumerator LastNightOpen_PublicQuestFlowPlaysLeoThenRequiredRooftop()
        {
            yield return LoadRuntime();
            var bootstrap = GameBootstrap.Instance;
            var director = UnityEngine.Object.FindFirstObjectByType<NarrativeContentDirector>();
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var finished = new List<string>();
            bootstrap.Cinematics.Finished += finished.Add;

            bootstrap.NarrativeState.Set(QuestRunner.CompletionFact("static"), true);
            bootstrap.NarrativeState.Set("cinematic_noah_complete", true);
            Assert.That(flow.EnterChapter("chapter_3_day_2"), Is.True);
            Assert.That(director.CanActivate("last_night_open_trigger"), Is.True);
            Assert.That(director.Activate("last_night_open_trigger"), Is.True);
            yield return CompleteDialogueChain(bootstrap);
            Assert.That(director.ActiveQuestId, Is.EqualTo("last_night_open"));

            while (!string.IsNullOrWhiteSpace(director.NextObjectiveId))
            {
                Assert.That(director.CompleteActiveQuestObjective(director.NextObjectiveId, 99), Is.True);
                yield return null;
            }

            Assert.That(bootstrap.NarrativeState.Has("helped_leo"), Is.True);
            Assert.That(director.ActiveQuestId, Is.Null);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True);
            FinishCurrentCinematic(bootstrap);
            yield return null;
            Assert.That(finished, Is.EqualTo(new[] { "leo" }));
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True,
                "Leo's branch video must hand directly to the fixed rooftop main-story video.");

            FinishCurrentCinematic(bootstrap);
            yield return null;
            Assert.That(finished, Is.EqualTo(new[] { "leo", "rooftop" }));
            yield return CompleteRooftopDecision(bootstrap, 1, ChapterStoryMarkResolver.ChapterThreeBalanceFact);
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_4"));
        }

        [UnityTest]
        public IEnumerator PendingCharacterVideo_ResumesAfterContinueAndCompletesItsChapterTransition()
        {
            yield return LoadRuntime();
            yield return null;
            var bootstrap = GameBootstrap.Instance;
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var pendingFact = NarrativeContentDirector.CinematicRoutePendingFact("maya");
            bootstrap.NarrativeState.Set(pendingFact, true);

            Assert.That(flow.EnterChapter("chapter_3_day_3"), Is.True);
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True,
                "Continuing after closing the game during Maya's video must resume that video rather than strand the quest path.");
            FinishCurrentCinematic(bootstrap);
            yield return null;

            Assert.That(bootstrap.NarrativeState.Has(pendingFact), Is.False);
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_3_day_2"));
        }

        [UnityTest]
        public IEnumerator OptionalLeoDialogue_CompletesOnceThenOneMoreTableAdvancesTheMainStory()
        {
            yield return LoadRuntime();
            var bootstrap = GameBootstrap.Instance;
            var flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
            var director = UnityEngine.Object.FindFirstObjectByType<NarrativeContentDirector>();
            Assert.That(flow.EnterChapter("chapter_2"), Is.True);
            const string routeId = "optional_leo_diner_trigger";

            Assert.That(director.CanActivate(routeId), Is.True);
            Assert.That(director.Activate(routeId), Is.True);
            var safety = 0;
            while (bootstrap.Dialogue.IsRunning && safety++ < 12)
            {
                if (bootstrap.Dialogue.Current.choices.Count > 0) bootstrap.Dialogue.Choose(0);
                else bootstrap.Dialogue.Advance();
                yield return null;
            }

            Assert.That(safety, Is.LessThan(12), "The selected Leo response must not fall through the other three responses.");
            var completionFact = NarrativeContentDirector.DialogueRouteCompletionFact(routeId);
            Assert.That(bootstrap.NarrativeState.Has(completionFact), Is.True);
            Assert.That(bootstrap.SaveGame.LoadOrNew().Has(completionFact), Is.True,
                "A completed optional conversation must survive returning to the diner.");
            Assert.That(director.CanActivate(routeId), Is.False,
                "The completed optional conversation must stop competing with One More Table for the E key.");

            bootstrap.NarrativeState.Set("quest_dead_air_complete", true);
            Assert.That(director.CanActivate("one_more_table_trigger"), Is.True);
            Assert.That(director.Activate("one_more_table_trigger"), Is.True);
            while (bootstrap.Dialogue.IsRunning)
            {
                bootstrap.Dialogue.Advance();
                yield return null;
            }
            Assert.That(director.ActiveQuestId, Is.EqualTo("one_more_table"));
            Assert.That(director.CompleteActiveQuestObjective("return_table"), Is.True);
            yield return null;
            Assert.That(bootstrap.Dialogue.ActiveDialogueId, Is.EqualTo("chapter_two_rooftop"),
                "Returning the table must begin the chapter-closing rooftop conversation.");
            yield return CompleteDialogueChain(bootstrap);
            Assert.That(bootstrap.NarrativeState.Has(ChapterStoryMarkResolver.ChapterTwoPlanFact), Is.True,
                "The chapter-closing choice must be recorded before Chapter 3 begins.");
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_3_day_3"),
                "One More Table must advance the main story after its rooftop scene.");
        }

        private static IEnumerator LoadRuntime()
        {
            if (GameBootstrap.Instance != null)
            {
                UnityEngine.Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }

            var savePath = Path.Combine(Application.temporaryCachePath, $"northbound-cinematic-route-{Guid.NewGuid():N}.json");
            GameBootstrap.SessionSaveGameFactory = () => new SaveGameService(savePath);
            SceneManager.LoadScene(SceneIds.Bootstrap, LoadSceneMode.Single);
            for (var frame = 0; frame < 30; frame++)
            {
                if (GameBootstrap.Instance != null &&
                    UnityEngine.Object.FindFirstObjectByType<GameFlowController>() != null &&
                    UnityEngine.Object.FindFirstObjectByType<NarrativeContentDirector>() != null)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Bootstrap did not finish loading the Greybridge cinematic route harness.");
        }

        private static void FinishCurrentCinematic(GameBootstrap bootstrap)
        {
            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True);
            bootstrap.Cinematics.Tick(2f);
            Assert.That(bootstrap.Cinematics.CanSkip, Is.True);
            bootstrap.Cinematics.Skip();
        }

        private static IEnumerator CompleteDialogueChain(GameBootstrap bootstrap)
        {
            var safety = 0;
            while (bootstrap.Dialogue.IsRunning && safety++ < 40)
            {
                if (bootstrap.Dialogue.Current.choices.Count > 0) bootstrap.Dialogue.Choose(0);
                else bootstrap.Dialogue.Advance();
                yield return null;
            }

            Assert.That(safety, Is.LessThan(40), "The commitment and quest conversations must complete without looping.");
        }

        private static IEnumerator CompleteRooftopDecision(GameBootstrap bootstrap, int choiceIndex, string expectedFact)
        {
            Assert.That(bootstrap.Dialogue.ActiveDialogueId, Is.EqualTo("rooftop_decision"),
                "The fixed rooftop video must lead to an authored player choice, not directly into Chapter 4.");
            var safety = 0;
            var chose = false;
            while (bootstrap.Dialogue.IsRunning && safety++ < 30)
            {
                if (bootstrap.Dialogue.Current.choices != null && bootstrap.Dialogue.Current.choices.Count > 0)
                {
                    Assert.That(chose, Is.False, "The rooftop decision must contain one decisive choice node.");
                    Assert.That(bootstrap.Dialogue.Choose(choiceIndex), Is.True);
                    chose = true;
                }
                else
                {
                    Assert.That(bootstrap.Dialogue.Advance(), Is.True);
                }
                yield return null;
            }

            Assert.That(safety, Is.LessThan(30), "The rooftop decision must complete without looping.");
            Assert.That(chose, Is.True);
            Assert.That(bootstrap.NarrativeState.Has(expectedFact), Is.True);
            Assert.That(new[]
            {
                ChapterStoryMarkResolver.ChapterThreePlanFact,
                ChapterStoryMarkResolver.ChapterThreeBalanceFact,
                ChapterStoryMarkResolver.ChapterThreeAgencyFact
            }.Count(bootstrap.NarrativeState.Has), Is.EqualTo(1),
                "Exactly one chapter-three stance must survive into later chapters and the ending.");
        }

        private static IEnumerator CompletePublicQuest(
            NarrativeContentDirector director,
            GameBootstrap bootstrap,
            string triggerId,
            string questId)
        {
            Assert.That(director.CanActivate(triggerId), Is.True);
            Assert.That(director.Activate(triggerId), Is.True);
            yield return CompleteDialogueChain(bootstrap);
            Assert.That(director.ActiveQuestId, Is.EqualTo(questId));
            CompleteEveryObjective(director);
            yield return null;
        }

        private static void CompleteEveryObjective(NarrativeContentDirector director)
        {
            var safety = 0;
            while (!string.IsNullOrWhiteSpace(director.NextObjectiveId) && safety++ < 20)
            {
                Assert.That(director.CompleteActiveQuestObjective(director.NextObjectiveId, 99), Is.True);
            }

            Assert.That(safety, Is.LessThan(20), "The public quest objectives must complete without looping.");
        }

        private static IEnumerator WaitForPrepareCount(ControllablePlayback playback, int expected)
        {
            for (var frame = 0; frame < 4 && playback.PrepareCount < expected; frame++) yield return null;
            Assert.That(playback.PrepareCount, Is.EqualTo(expected));
        }

        private sealed class ControllablePlayback : IVideoPlayback
        {
            public event Action Prepared;
            public event Action Finished;
            public event Action<string> Failed;
            public int PrepareCount { get; private set; }

            public void Prepare(VideoClip clip) => PrepareCount++;
            public void Play() { }
            public void Stop() { }
            public void RaisePrepared() => Prepared?.Invoke();
            public void RaiseFinished() => Finished?.Invoke();
            public void RaiseFailed(string error) => Failed?.Invoke(error);
        }

        private sealed class SilentPresentation : ICinematicPresentation
        {
            public void Show(CinematicAsset asset, SettingsModel settings) { }
            public void SetPlaybackTime(CinematicAsset asset, float elapsedSeconds, SettingsModel settings) { }
            public void Hide() { }
            public void RestoreGameplayAudio(CinematicAsset asset) { }
            public void RestoreCamera() { }
        }

    }
}
