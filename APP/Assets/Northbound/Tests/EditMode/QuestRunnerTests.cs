using System.Collections.Generic;
using System.IO;
using Northbound.Narrative;
using Northbound.Quests;
using NUnit.Framework;
using UnityEngine;

namespace Northbound.Tests
{
    public sealed class QuestRunnerTests
    {
        private readonly List<Object> createdAssets = new List<Object>();
        private string directoryPath;

        [SetUp]
        public void SetUp()
        {
            directoryPath = Path.Combine(Path.GetTempPath(), "NorthboundQuestRunnerTests", Path.GetRandomFileName());
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in createdAssets)
            {
                Object.DestroyImmediate(asset);
            }

            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }

        [Test]
        public void Report_OnlyCompletesObjectivesInTheirAuthoredOrder()
        {
            var state = new NarrativeStateStore();
            var runner = new QuestRunner(state);
            runner.StartQuest(CreateQuest("garage", Objective("find_wrench", 2), Objective("repair_generator", 1)));

            Assert.That(runner.Report("repair_generator", 1), Is.False);
            Assert.That(state.Has("quest_garage_objective_repair_generator_complete"), Is.False);
            Assert.That(runner.Report("find_wrench", 1), Is.True);
            Assert.That(state.Has("quest_garage_objective_find_wrench_complete"), Is.False);
            Assert.That(runner.Report("find_wrench", 1), Is.True);
            Assert.That(state.Has("quest_garage_objective_find_wrench_complete"), Is.True);
            Assert.That(runner.Report("repair_generator", 1), Is.True);
            Assert.That(state.Has("quest_garage_objective_repair_generator_complete"), Is.True);
        }

        [Test]
        public void Report_AfterObjectiveCompletionIsIdempotent()
        {
            var state = new NarrativeStateStore();
            var runner = new QuestRunner(state);
            runner.StartQuest(CreateQuest("garage", Objective("find_wrench", 1)));

            runner.Report("find_wrench", 1);

            Assert.That(runner.Report("find_wrench", 1), Is.False);
            Assert.That(state.GetInt("quest_garage_objective_find_wrench_progress"), Is.EqualTo(1));
        }

        [Test]
        public void CompleteQuest_WritesCompletionFactsAfterAllObjectivesAreMet()
        {
            var state = new NarrativeStateStore();
            var runner = new QuestRunner(state);
            runner.StartQuest(CreateQuest("garage", Objective("find_wrench", 1), completionFacts: new[] { "power_restored", "elias_trusts_jamie" }));

            Assert.That(runner.CompleteQuest("garage"), Is.False);
            runner.Report("find_wrench", 1);

            Assert.That(state.Has("quest_garage_complete"), Is.True);
            Assert.That(state.Has("power_restored"), Is.True);
            Assert.That(state.Has("elias_trusts_jamie"), Is.True);
            Assert.That(runner.ActiveQuestId, Is.Null);
        }

        [Test]
        public void CompleteQuest_NotifiesOnlyAfterRewardsAndNextQuestAreSettled()
        {
            var state = new NarrativeStateStore();
            var runner = new QuestRunner(state);
            var quest = CreateQuest("garage", Objective("repair", 1), completionFacts: new[] { "car_ready" });
            quest.nextQuestIds = new[] { "road_test" };
            var notified = false;
            runner.QuestCompleted += questId =>
            {
                notified = true;
                Assert.That(questId, Is.EqualTo("garage"));
                Assert.That(runner.ActiveQuestId, Is.Null);
                Assert.That(state.Has("quest_garage_complete"), Is.True);
                Assert.That(state.Has("car_ready"), Is.True);
                Assert.That(state.Has("quest_road_test_available"), Is.True);
            };

            Assert.That(runner.StartQuest(quest), Is.True);
            Assert.That(runner.Report("repair", 1), Is.True);

            Assert.That(notified, Is.True);
        }

        [Test]
        public void RestoreActiveQuest_ResumesStartedIncompleteQuestAtItsNextObjectiveAndIgnoresCompletedQuest()
        {
            var state = new NarrativeStateStore();
            var completed = CreateQuest("completed", Objective("old_objective", 1));
            var inProgress = CreateQuest("garage", Objective("find_wrench", 1), Objective("repair_generator", 1));
            state.Set(QuestRunner.StartedFactId(completed.id), true);
            state.Set(QuestRunner.CompletionFact(completed.id), true);
            state.Set(QuestRunner.StartedFactId(inProgress.id), true);
            state.Set(QuestRunner.ObjectiveCompletionFactId(inProgress.id, "find_wrench"), true);
            var runner = new QuestRunner(state);

            Assert.That(runner.RestoreActiveQuest(new[] { completed, inProgress }), Is.EqualTo("garage"));
            Assert.That(runner.ActiveQuestId, Is.EqualTo("garage"));
            Assert.That(runner.NextObjectiveId, Is.EqualTo("repair_generator"));
            Assert.That(runner.IsCurrentObjective("garage", "repair_generator"), Is.True);
            Assert.That(runner.IsCurrentObjective("completed", "old_objective"), Is.False);
        }

        [Test]
        public void Commit_FirstMissionLocksItsPair()
        {
            var state = new NarrativeStateStore();
            var pair = new MissionPairController("alternator", "first_light", state);

            Assert.That(pair.TryCommit("first_light"), Is.True);
            Assert.That(pair.TryCommit("alternator"), Is.False);
            Assert.That(state.Has("missed_alternator"), Is.True);
        }

        [Test]
        public void Commit_TriggersConfiguredInOppositeOrderShareTheSameLock()
        {
            var state = new NarrativeStateStore();
            var eliasPair = new MissionPairController("alternator", "first_light", state);
            var mayaPair = new MissionPairController("first_light", "alternator", state);

            Assert.That(eliasPair.TryCommit("alternator"), Is.True);
            Assert.That(mayaPair.CommittedQuestId, Is.EqualTo("alternator"));
            Assert.That(mayaPair.TryCommit("first_light"), Is.False);
        }

        [Test]
        public void CommittedMission_RemainsAvailableToResumeWhileItsSiblingStaysUnavailable()
        {
            var state = new NarrativeStateStore();
            var pair = new MissionPairController("alternator", "first_light", state);

            Assert.That(pair.TryCommit("first_light"), Is.True);
            Assert.That(pair.IsAvailable("first_light"), Is.True);
            Assert.That(pair.IsAvailable("alternator"), Is.False);
            Assert.That(pair.BeginCommitment("first_light"), Is.True);
            Assert.That(pair.ConfirmCommitment(), Is.True);
            Assert.That(pair.CommittedQuestId, Is.EqualTo("first_light"));
            Assert.That(state.Has("missed_alternator"), Is.True);
        }

        [Test]
        public void TryCommit_SavesTheCommittedPairBeforeReturningSuccess()
        {
            var service = new SaveGameService(Path.Combine(directoryPath, "northbound-save.json"));
            var state = new NarrativeStateStore();
            var pair = new MissionPairController("alternator", "first_light", state, service);

            Assert.That(pair.TryCommit("first_light"), Is.True);

            var reloaded = new NarrativeStateStore(service.LoadOrNew());
            var restoredPair = new MissionPairController("alternator", "first_light", reloaded, service);
            Assert.That(restoredPair.CommittedQuestId, Is.EqualTo("first_light"));
            Assert.That(restoredPair.TryCommit("alternator"), Is.False);
            Assert.That(reloaded.Has("missed_alternator"), Is.True);
        }

        [Test]
        public void TryCommit_WhenSaveFailsDoesNotClaimOrApplyCommitment()
        {
            Directory.CreateDirectory(directoryPath);
            var blockedDirectory = Path.Combine(directoryPath, "blocked");
            File.WriteAllText(blockedDirectory, "not-a-directory");
            var state = new NarrativeStateStore();
            var pair = new MissionPairController(
                "alternator",
                "first_light",
                state,
                new SaveGameService(Path.Combine(blockedDirectory, "northbound-save.json")));

            Assert.That(pair.TryCommit("first_light"), Is.False);
            Assert.That(pair.CommittedQuestId, Is.Null);
            Assert.That(state.Has("mission_pair_alternator_first_light_committed_first_light"), Is.False);
            Assert.That(state.Has("missed_alternator"), Is.False);
        }

        [Test]
        public void ConfirmCommitment_WhenSaveFailsKeepsTheMissionsAvailableForAnotherAttempt()
        {
            Directory.CreateDirectory(directoryPath);
            var blockedDirectory = Path.Combine(directoryPath, "blocked");
            File.WriteAllText(blockedDirectory, "not-a-directory");
            var pair = new MissionPairController(
                "alternator",
                "first_light",
                new NarrativeStateStore(),
                new SaveGameService(Path.Combine(blockedDirectory, "northbound-save.json")));

            pair.BeginCommitment("first_light");

            Assert.That(pair.ConfirmCommitment(), Is.False);
            Assert.That(pair.CommittedQuestId, Is.Null);
            Assert.That(pair.BeginCommitment("first_light"), Is.True);
        }

        private QuestAsset CreateQuest(string id, QuestObjective objective, QuestObjective secondObjective = null, string[] completionFacts = null)
        {
            var quest = ScriptableObject.CreateInstance<QuestAsset>();
            quest.id = id;
            quest.title = id;
            quest.objectives = new List<QuestObjective> { objective };
            if (secondObjective != null)
            {
                quest.objectives.Add(secondObjective);
            }

            quest.completionFacts = completionFacts ?? new string[0];
            createdAssets.Add(quest);
            return quest;
        }

        private static QuestObjective Objective(string id, int requiredAmount)
        {
            return new QuestObjective { id = id, requiredAmount = requiredAmount };
        }
    }
}
