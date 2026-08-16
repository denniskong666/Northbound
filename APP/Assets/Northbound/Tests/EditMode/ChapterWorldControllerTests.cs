using System.Collections.Generic;
using System.IO;
using Northbound.Core;
using Northbound.Narrative;
using Northbound.World;
using NUnit.Framework;
using UnityEngine;

namespace Northbound.Tests
{
    public sealed class ChapterWorldControllerTests
    {
        private readonly List<Object> createdObjects = new List<Object>();
        private string saveDirectory;

        [SetUp]
        public void SetUp()
        {
            saveDirectory = Path.Combine(Path.GetTempPath(), "NorthboundChapterWorldTests", Path.GetRandomFileName());
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in createdObjects)
            {
                Object.DestroyImmediate(created);
            }

            if (Directory.Exists(saveDirectory))
            {
                Directory.Delete(saveDirectory, true);
            }
        }

        [Test]
        public void Apply_ChapterOneActivatesTheOpenDinerAndMarket()
        {
            var controller = CreateController();
            var diner = CreateObject("Open Diner", false);
            var market = CreateObject("Open Market", false);
            controller.Configure(new[]
            {
                Variant("chapter_1", activate: new[] { diner, market }, spawn: "diner", ambient: "amber", quests: new[] { "clock_in" })
            });

            Assert.That(controller.Apply("chapter_1", new NarrativeState()), Is.True);
            Assert.That(diner.activeSelf, Is.True);
            Assert.That(market.activeSelf, Is.True);
            Assert.That(controller.CurrentSpawnPointId, Is.EqualTo("diner"));
            CollectionAssert.AreEqual(new[] { "clock_in" }, controller.CurrentStartingQuestIds);
        }

        [Test]
        public void Apply_ChapterTwoActivatesFinalWeekSign()
        {
            var controller = CreateController();
            var finalWeek = CreateObject("FINAL WEEK", false);
            controller.Configure(new[] { Variant("chapter_2", activate: new[] { finalWeek }, ambient: "cool") });

            controller.Apply("chapter_2", new NarrativeState());

            Assert.That(finalWeek.activeSelf, Is.True);
            Assert.That(controller.CurrentAmbientSnapshotId, Is.EqualTo("cool"));
        }

        [Test]
        public void Apply_ChapterFourActivatesDarkStorefronts()
        {
            var controller = CreateController();
            var darkStorefronts = CreateObject("Dark Storefronts", false);
            controller.Configure(new[] { Variant("chapter_4", activate: new[] { darkStorefronts }) });

            controller.Apply("chapter_4", new NarrativeState());

            Assert.That(darkStorefronts.activeSelf, Is.True);
        }

        [Test]
        public void Apply_ChapterFourThenChapterThreeResetsObjectsOnlyControlledByThePriorVariant()
        {
            var controller = CreateController();
            var darkStorefronts = CreateObject("Dark Storefronts", false);
            controller.Configure(new[]
            {
                Variant("chapter_3_day_3"),
                Variant("chapter_4", activate: new[] { darkStorefronts })
            });

            Assert.That(controller.Apply("chapter_4", new NarrativeState()), Is.True);
            Assert.That(darkStorefronts.activeSelf, Is.True);
            Assert.That(controller.Apply("chapter_3_day_3", new NarrativeState()), Is.True);

            Assert.That(darkStorefronts.activeSelf, Is.False);
        }

        [Test]
        public void Apply_FinaleActivatesAllFourDirectionRegions()
        {
            var controller = CreateController();
            var car = CreateObject("Finale Car Region", false);
            var home = CreateObject("Finale Home Region", false);
            var road = CreateObject("Finale Road Region", false);
            var friends = CreateObject("Finale Friends Region", false);
            controller.Configure(new[]
            {
                Variant("finale", activate: new[] { car, home, road, friends }, spawn: "finale")
            });

            controller.Apply("finale", new NarrativeState());

            Assert.That(car.activeSelf, Is.True);
            Assert.That(home.activeSelf, Is.True);
            Assert.That(road.activeSelf, Is.True);
            Assert.That(friends.activeSelf, Is.True);
        }

        [Test]
        public void Apply_UsesOnlyTheRequestedBaseVariantThenRefreshesMatchingFactBindings()
        {
            var controller = CreateController();
            var chapterOneOnly = CreateObject("Chapter One Only", false);
            var chapterTwoOnly = CreateObject("Chapter Two Only", true);
            var factObject = CreateObject("Maya Exhibition Door", false);
            var binding = factObject.AddComponent<WorldFactBinding>();
            binding.Configure(factObject, new[] { "attended_maya_exhibition" }, new string[0]);
            controller.Configure(new[]
            {
                Variant("chapter_1", activate: new[] { chapterOneOnly }, deactivate: new[] { chapterTwoOnly }),
                Variant("chapter_2", activate: new[] { chapterTwoOnly }, deactivate: new[] { chapterOneOnly })
            }, new[] { binding });
            var state = new NarrativeState();
            state.Set("attended_maya_exhibition", true);

            controller.Apply("chapter_1", state);

            Assert.That(chapterOneOnly.activeSelf, Is.True);
            Assert.That(chapterTwoOnly.activeSelf, Is.False);
            Assert.That(factObject.activeSelf, Is.True);
        }

        [Test]
        public void BindNarrativeState_RefreshesFactBindingsWhenTheLiveStateChangesWithoutReapplyingAChapter()
        {
            var controller = CreateController();
            var factObject = CreateObject("Live Fact Object", false);
            var binding = factObject.AddComponent<WorldFactBinding>();
            binding.Configure(factObject, new[] { "live_fact" }, new string[0]);
            controller.Configure(new[] { Variant("chapter_1") }, new[] { binding });
            var state = new NarrativeStateStore();
            controller.BindNarrativeState(state);

            Assert.That(controller.Apply("chapter_1", state.State), Is.True);
            Assert.That(factObject.activeSelf, Is.False);
            state.Set("live_fact", true);

            Assert.That(factObject.activeSelf, Is.True);
            controller.UnbindNarrativeState();
            state.Set("live_fact", false);
            Assert.That(factObject.activeSelf, Is.True);
        }

        [Test]
        public void EnterChapter_SavesCurrentChapterAndRestoresItForANewFlowController()
        {
            var controller = CreateController();
            controller.Configure(new[] { Variant("chapter_1", spawn: "garage") });
            var save = new SaveGameService(Path.Combine(saveDirectory, "northbound-save.json"));
            var state = new NarrativeStateStore();
            var flow = CreateFlow(controller, state, save);

            Assert.That(flow.EnterChapter("chapter_1"), Is.True);
            Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_1"));
            Assert.That(save.LoadOrNew().Has("current_chapter_chapter_1"), Is.True);

            var restored = CreateFlow(controller, new NarrativeStateStore(save.LoadOrNew()), save);
            Assert.That(restored.RestoreCurrentChapter(), Is.True);
            Assert.That(restored.CurrentChapterId, Is.EqualTo("chapter_1"));
            Assert.That(controller.CurrentSpawnPointId, Is.EqualTo("garage"));
        }

        [Test]
        public void RestoreCurrentChapter_NotifiesRuntimeSystemsAfterApplyingTheSavedChapter()
        {
            var controller = CreateController();
            controller.Configure(new[] { Variant("finale", spawn: "finale") });
            var save = new SaveGameService(Path.Combine(saveDirectory, "northbound-save.json"));
            var savedState = new NarrativeState();
            savedState.Set(GameFlowController.ChapterFact("finale"), true);
            Assert.That(save.Save(savedState), Is.True);
            var flow = CreateFlow(controller, new NarrativeStateStore(save.LoadOrNew()), save);
            var enteredChapter = string.Empty;
            var notificationCount = 0;
            flow.ChapterEntered += chapterId =>
            {
                enteredChapter = chapterId;
                notificationCount++;
            };

            Assert.That(flow.RestoreCurrentChapter(), Is.True);

            Assert.That(enteredChapter, Is.EqualTo("finale"));
            Assert.That(notificationCount, Is.EqualTo(1));
            Assert.That(flow.CurrentChapterId, Is.EqualTo("finale"));
            Assert.That(controller.CurrentSpawnPointId, Is.EqualTo("finale"));
        }

        [Test]
        public void EnterChapter_ReplacesAPersistedChapterEvenWhenTheFlowWasNotRestoredFirst()
        {
            var controller = CreateController();
            controller.Configure(new[] { Variant("chapter_1"), Variant("chapter_2") });
            var save = new SaveGameService(Path.Combine(saveDirectory, "northbound-save.json"));
            var state = new NarrativeStateStore();
            state.Set(GameFlowController.ChapterFact("chapter_1"), true);
            var flow = CreateFlow(controller, state, save);

            Assert.That(flow.EnterChapter("chapter_2"), Is.True);
            Assert.That(state.Has(GameFlowController.ChapterFact("chapter_1")), Is.False);
            Assert.That(state.Has(GameFlowController.ChapterFact("chapter_2")), Is.True);
        }

        private ChapterWorldController CreateController()
        {
            var root = CreateObject("Chapter World Controller", true);
            return root.AddComponent<ChapterWorldController>();
        }

        private GameFlowController CreateFlow(ChapterWorldController controller, NarrativeStateStore state, SaveGameService save)
        {
            var root = CreateObject("Game Flow", true);
            var flow = root.AddComponent<GameFlowController>();
            flow.Initialize(state, save, controller);
            return flow;
        }

        private GameObject CreateObject(string name, bool active)
        {
            var gameObject = new GameObject(name);
            gameObject.SetActive(active);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private ChapterVariant Variant(
            string id,
            GameObject[] activate = null,
            GameObject[] deactivate = null,
            string spawn = "",
            string ambient = "",
            string[] quests = null)
        {
            var variant = ScriptableObject.CreateInstance<ChapterVariant>();
            variant.id = id;
            variant.objectsToActivate = activate ?? new GameObject[0];
            variant.objectsToDeactivate = deactivate ?? new GameObject[0];
            variant.spawnPointId = spawn;
            variant.ambientSnapshotId = ambient;
            variant.startingQuestIds = quests ?? new string[0];
            createdObjects.Add(variant);
            return variant;
        }
    }
}
