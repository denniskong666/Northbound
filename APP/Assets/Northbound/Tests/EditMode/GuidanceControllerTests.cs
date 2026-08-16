using Northbound.Content;
using Northbound.Guidance;
using Northbound.Narrative;
using Northbound.Quests;
using Northbound.UI;
using NUnit.Framework;
using UnityEngine;

namespace Northbound.Tests
{
    public sealed class GuidanceControllerTests
    {
        [Test]
        public void FirstSession_TeachesMovementBeforeInteraction()
        {
            var state = new NarrativeStateStore();

            var step = GuidanceController.Resolve(state, Manifest(), "chapter_1", null, null);

            Assert.That(step.instruction, Does.Contain("WASD"));
            Assert.That(step.destinationId, Is.Empty);
            Assert.That(step.locationName, Is.EqualTo("Greybridge"));
            Assert.That(step.nextAction, Does.StartWith("MOVE:"));
        }

        [Test]
        public void ChineseLanguage_LocalizesFirstSessionMovement()
        {
            var state = new NarrativeStateStore();
            var step = GuidanceController.Resolve(state, Manifest(), "chapter_1", null, null);
            GameText.Use(GameLanguage.SimplifiedChinese);
            try
            {
                Assert.That(GuidanceController.ResolveNavigationAction(step, "exterior"), Does.StartWith("移动："));
            }
            finally
            {
                GameText.Use(GameLanguage.English);
            }
        }

        [Test]
        public void AfterMoving_PointsAtFirstAvailableStoryRouteAndTeachesE()
        {
            var state = new NarrativeStateStore();
            state.Set("tutorial_moved", true);

            var step = GuidanceController.Resolve(state, Manifest(), "chapter_1", null, null);

            Assert.That(step.instruction, Does.Contain("E"));
            Assert.That(step.destinationId, Is.EqualTo("route_clock_in"));
            Assert.That(step.locationName, Is.EqualTo("Ruth's Diner"));
            Assert.That(step.nextAction, Does.StartWith("INTERACT:"));
            Assert.That(step.isMissionStart, Is.True);
        }

        [Test]
        public void ActiveQuest_PointsAtNextIncompletePhysicalObjective()
        {
            var state = new NarrativeStateStore();
            state.Set("tutorial_moved", true);
            state.Set("tutorial_interacted", true);

            var step = GuidanceController.Resolve(state, Manifest(), "chapter_1", "clock_in", "serve_tables", "ruths_diner");

            Assert.That(step.objective, Is.EqualTo("Serve the diner shift"));
            Assert.That(step.objectiveId, Is.EqualTo("serve_tables"));
            Assert.That(step.destinationId, Is.EqualTo("clock_in:serve_tables"));
            Assert.That(step.instruction, Does.Contain("gold outline"));
            Assert.That(step.locationName, Is.EqualTo("Ruth's Diner"));
            Assert.That(GuidanceController.ResolveNavigationAction(step, "ruths_diner"), Does.Contain("E / Enter"));
            Assert.That(GuidanceController.ResolveNavigationAction(step, "ruths_diner"), Does.Contain("serve the diner shift"));
        }

        [Test]
        public void CompletedDinerQuest_PointsAtTheNextMainStoryLocation()
        {
            var state = new NarrativeStateStore();
            state.Set("tutorial_moved", true);
            state.Set(QuestRunner.CompletionFact("clock_in"), true);

            var step = GuidanceController.Resolve(state, Manifest(), "chapter_1", null, null);

            Assert.That(step.destinationId, Is.EqualTo("missing_socket_trigger"));
            Assert.That(step.locationName, Is.EqualTo("Vale Auto Garage"));
            Assert.That(step.objective, Is.EqualTo("Start Missing Socket"));
            Assert.That(step.targetLocationId, Is.EqualTo("vale_garage"));
        }

        [Test]
        public void PairedPrerequisites_AcceptTheCompletedSiblingForLaterGuidance()
        {
            var manifest = NarrativeContentManifest.FromJson(@"{
              ""chapters"":[{""id"":""chapter_3_day_2""},{""id"":""chapter_4""}],
              ""quests"":[
                {""id"":""alternator"",""chapterId"":""chapter_3_day_3"",""pairId"":""alternator|first_light""},
                {""id"":""first_light"",""chapterId"":""chapter_3_day_3"",""pairId"":""alternator|first_light""},
                {""id"":""road_test"",""chapterId"":""chapter_3_day_2"",""pairId"":""road_test|static"",""prerequisiteQuestIds"":[""alternator"",""first_light""]},
                {""id"":""static"",""chapterId"":""chapter_3_day_2"",""pairId"":""road_test|static"",""prerequisiteQuestIds"":[""alternator"",""first_light""]},
                {""id"":""pack_trunk"",""chapterId"":""chapter_3_day_2"",""pairId"":""pack_trunk|last_night_open""},
                {""id"":""last_night_open"",""chapterId"":""chapter_3_day_2"",""pairId"":""pack_trunk|last_night_open""},
                {""id"":""things_we_leave"",""chapterId"":""chapter_4"",""prerequisiteQuestIds"":[""pack_trunk"",""last_night_open""]}
              ],
              ""triggers"":[
                {""id"":""road_test_trigger"",""routeType"":""quest"",""targetId"":""road_test"",""chapterId"":""chapter_3_day_2""},
                {""id"":""static_trigger"",""routeType"":""quest"",""targetId"":""static"",""chapterId"":""chapter_3_day_2""},
                {""id"":""things_we_leave_trigger"",""routeType"":""quest"",""targetId"":""things_we_leave"",""chapterId"":""chapter_4""}
              ]
            }");
            var state = new NarrativeStateStore();
            state.Set("tutorial_moved", true);
            state.Set(QuestRunner.CompletionFact("alternator"), true);

            var pairedStep = GuidanceController.Resolve(state, manifest, "chapter_3_day_2", null, null);

            Assert.That(pairedStep.destinationId, Is.EqualTo("road_test_trigger"));
            var noahRoomStep = GuidanceController.Resolve(state, manifest, "chapter_3_day_2", null, null, "noah_electronics");
            Assert.That(noahRoomStep.destinationId, Is.EqualTo("static_trigger"),
                "Inside Noah's room, guidance must offer Static instead of pointing back through the exit to the garage.");
            state = new NarrativeStateStore();
            state.Set("tutorial_moved", true);
            state.Set(QuestRunner.CompletionFact("pack_trunk"), true);

            var chapterFourStep = GuidanceController.Resolve(state, manifest, "chapter_4", null, null);

            Assert.That(chapterFourStep.destinationId, Is.EqualTo("things_we_leave_trigger"));
        }

        [Test]
        public void Navigation_StagesExitThenStreetEntranceThenRoomObjective()
        {
            var state = new NarrativeStateStore();
            state.Set("tutorial_moved", true);
            state.Set(QuestRunner.CompletionFact("clock_in"), true);
            var step = GuidanceController.Resolve(state, Manifest(), "chapter_1", null, null);

            Assert.That(GuidanceController.ResolveNavigationTarget(step, "ruths_diner"), Is.EqualTo("exit:ruths_diner"));
            Assert.That(GuidanceController.ResolveNavigationTarget(step, "exterior"), Is.EqualTo("entrance:vale_garage"));
            Assert.That(GuidanceController.ResolveNavigationTarget(step, "vale_garage"), Is.EqualTo("missing_socket_trigger"));
            Assert.That(GuidanceController.ResolveNavigationAction(step, "ruths_diner"), Does.StartWith("EXIT:"));
            Assert.That(GuidanceController.ResolveNavigationAction(step, "exterior"), Does.StartWith("INTERACT:"));
            Assert.That(GuidanceController.ResolveNavigationAction(step, "vale_garage"), Does.StartWith("BEGIN:"));
        }

        [Test]
        public void ChineseLanguage_LocalizesTheThreeNavigationStages()
        {
            var state = new NarrativeStateStore();
            state.Set("tutorial_moved", true);
            state.Set(QuestRunner.CompletionFact("clock_in"), true);
            var step = GuidanceController.Resolve(state, Manifest(), "chapter_1", null, null);
            GameText.Use(GameLanguage.SimplifiedChinese);
            try
            {
                Assert.That(GuidanceController.ResolveNavigationAction(step, "ruths_diner"), Does.Contain("离开"));
                Assert.That(GuidanceController.ResolveNavigationAction(step, "exterior"), Does.Contain("修理厂"));
                Assert.That(GuidanceController.ResolveNavigationAction(step, "vale_garage"), Does.StartWith("开始："));
            }
            finally
            {
                GameText.Use(GameLanguage.English);
            }
        }

        [Test]
        public void ReturningPlayer_UsesTheShortDoorInstructionAfterLearningInteraction()
        {
            var state = new NarrativeStateStore();
            state.Set("tutorial_moved", true);
            state.Set("tutorial_interacted", true);

            var step = GuidanceController.Resolve(state, Manifest(), "chapter_1", null, null);

            Assert.That(step.nextAction, Does.StartWith("ENTER:"));
            Assert.That(GuidanceController.ResolveNavigationAction(step, "exterior"), Does.StartWith("ENTER:"));
        }

        [Test]
        public void NavigationLabel_FollowsTheStagedDestinationAndLanguage()
        {
            var step = new GuidanceStep
            {
                locationName = "Maya's Studio",
                destinationId = "first_light_trigger",
                targetLocationId = "maya_studio"
            };

            Assert.That(GuidanceController.ResolveNavigationLabel(step, "ruths_diner"), Is.EqualTo("Greybridge"));
            Assert.That(GuidanceController.ResolveNavigationLabel(step, "exterior"), Is.EqualTo("Maya's Studio"));

            GameText.Use(GameLanguage.SimplifiedChinese);
            try
            {
                Assert.That(GuidanceController.ResolveNavigationLabel(step, "ruths_diner"), Is.EqualTo("格雷布里奇街区"));
                Assert.That(GuidanceController.ResolveNavigationLabel(step, "exterior"), Is.EqualTo("玛雅工作室"));
            }
            finally
            {
                GameText.Use(GameLanguage.English);
            }
        }

        [Test]
        public void OffscreenIndicator_HidesOnscreenAndClampsWithCorrectDirection()
        {
            var canvas = new Vector2(1920f, 1080f);
            var padding = new Vector2(170f, 92f);

            Assert.That(GuidanceHudView.TryResolveOffscreenIndicator(
                new Vector3(.5f, .5f, 1f), canvas, padding, out _, out _), Is.False);
            Assert.That(GuidanceHudView.TryResolveOffscreenIndicator(
                new Vector3(1.4f, .5f, 1f), canvas, padding, out var rightPosition, out var rightRotation), Is.True);
            Assert.That(rightPosition.x, Is.EqualTo(canvas.x * .5f - padding.x).Within(.01f));
            Assert.That(rightPosition.y, Is.EqualTo(0f).Within(.01f));
            Assert.That(Mathf.DeltaAngle(rightRotation, 0f), Is.EqualTo(0f).Within(.01f));

            Assert.That(GuidanceHudView.TryResolveOffscreenIndicator(
                new Vector3(.2f, 1.4f, 1f), canvas, padding, out var upperPosition, out var upperRotation), Is.True);
            Assert.That(Mathf.Abs(upperPosition.x), Is.LessThanOrEqualTo(canvas.x * .5f - padding.x));
            Assert.That(upperPosition.y, Is.EqualTo(canvas.y * .5f - padding.y).Within(.01f));
            Assert.That(upperRotation, Is.GreaterThan(90f));
        }

        [Test]
        public void FinaleBeforeReview_PointsToTheGatheringPoint()
        {
            var state = new NarrativeStateStore();
            state.Set("tutorial_moved", true);
            state.Set("cinematic_finale_complete", true);

            var step = GuidanceController.Resolve(state, Manifest(), "finale", null, null);

            Assert.That(step.locationName, Is.EqualTo("Finale Gathering"));
            Assert.That(step.destinationId, Is.EqualTo("finale_gathering"));
            Assert.That(step.targetLocationId, Is.EqualTo("exterior"));
            Assert.That(step.objective, Is.EqualTo("Meet at the wagon"));
            Assert.That(step.instruction, Does.Contain("routes your journey has left open"));
            Assert.That(step.instruction, Does.Not.Contain("four routes"));
        }

        [Test]
        public void FinaleAfterReview_RemovesGatheringMarkerAndAllowsDirectionChoice()
        {
            var state = FinaleState();

            var step = GuidanceController.Resolve(state, Manifest(), "finale", null, null);

            Assert.That(step.destinationId, Is.Empty);
            Assert.That(step.objective, Is.EqualTo("Choose your direction"));
            Assert.That(step.nextAction, Does.Contain("Southeast Northbound"));
            Assert.That(step.nextAction, Does.Contain("Southwest Home"));
            Assert.That(step.nextAction, Does.Contain("East No Map"));
            Assert.That(step.nextAction, Does.Contain("Northeast Wait"));
        }

        [Test]
        public void FinaleAfterStrongPlanHistory_ExplainsThatHomeClosedAndListsThreeRemainingRoutes()
        {
            var state = FinaleState(
                ChapterStoryMarkResolver.ChapterOnePlanFact,
                ChapterStoryMarkResolver.ChapterTwoPlanFact,
                ChapterStoryMarkResolver.ChapterThreePlanFact);

            var step = GuidanceController.Resolve(state, Manifest(), "finale", null, null);

            Assert.That(step.instruction, Does.Contain("three visible route signs"));
            Assert.That(step.instruction, Does.Contain("earlier choices have closed one direction"));
            Assert.That(step.nextAction, Does.Contain("Southeast Northbound"));
            Assert.That(step.nextAction, Does.Not.Contain("Southwest Home"));
            Assert.That(step.nextAction, Does.Contain("East No Map"));
            Assert.That(step.nextAction, Does.Contain("Northeast Wait"));
        }

        [Test]
        public void FinaleAfterStrongAgencyHistory_ExplainsThatNorthboundClosedAndListsThreeRemainingRoutes()
        {
            var state = FinaleState(
                ChapterStoryMarkResolver.ChapterOneAgencyFact,
                ChapterStoryMarkResolver.ChapterTwoAgencyFact,
                ChapterStoryMarkResolver.ChapterThreeAgencyFact);

            var step = GuidanceController.Resolve(state, Manifest(), "finale", null, null);

            Assert.That(step.instruction, Does.Contain("three visible route signs"));
            Assert.That(step.nextAction, Does.Not.Contain("Southeast Northbound"));
            Assert.That(step.nextAction, Does.Contain("Southwest Home"));
            Assert.That(step.nextAction, Does.Contain("East No Map"));
            Assert.That(step.nextAction, Does.Contain("Northeast Wait"));
        }

        [Test]
        public void ChineseFinaleAfterStrongPlanHistory_ListsOnlyTheThreeVisibleRoutes()
        {
            var state = FinaleState(
                ChapterStoryMarkResolver.ChapterOnePlanFact,
                ChapterStoryMarkResolver.ChapterTwoPlanFact,
                ChapterStoryMarkResolver.ChapterThreePlanFact);
            GameText.Use(GameLanguage.SimplifiedChinese);
            try
            {
                var step = GuidanceController.Resolve(state, Manifest(), "finale", null, null);

                Assert.That(step.instruction, Does.Contain("三块可见路线牌"));
                Assert.That(step.nextAction, Does.Contain("东南向北公路"));
                Assert.That(step.nextAction, Does.Not.Contain("西南留在故乡"));
                Assert.That(step.nextAction, Does.Contain("向东无图之路"));
                Assert.That(step.nextAction, Does.Contain("东北等到天亮"));
            }
            finally
            {
                GameText.Use(GameLanguage.English);
            }
        }

        private static NarrativeStateStore FinaleState(params string[] marks)
        {
            var state = new NarrativeStateStore();
            state.Set("tutorial_moved", true);
            state.Set("cinematic_finale_complete", true);
            state.Set("finale_routes_reviewed", true);
            foreach (var mark in marks) state.Set(mark, true);
            return state;
        }

        private static NarrativeContentManifest Manifest()
        {
            return NarrativeContentManifest.FromJson(@"{
              ""chapters"":[{""id"":""chapter_1""}],
              ""quests"":[
                {""id"":""clock_in"",""chapterId"":""chapter_1"",""triggerId"":""route_clock_in"",""completionMode"":""physical""},
                {""id"":""missing_socket"",""chapterId"":""chapter_1"",""triggerId"":""missing_socket_trigger"",""completionMode"":""physical"",""prerequisiteQuestIds"":[""clock_in""]}
              ],
              ""triggers"":[
                {""id"":""route_clock_in"",""routeType"":""quest"",""targetId"":""clock_in"",""chapterId"":""chapter_1""},
                {""id"":""missing_socket_trigger"",""routeType"":""quest"",""targetId"":""missing_socket"",""chapterId"":""chapter_1""}
              ]
            }");
        }
    }
}
