using System.Collections.Generic;
using System.Linq;
using Northbound.Endings;
using Northbound.Narrative;
using NUnit.Framework;
using UnityEditor;

namespace Northbound.Tests
{
    public sealed class EndingResolverTests
    {
        private readonly List<EndingAsset> loadedAssets = new List<EndingAsset>();

        [TearDown]
        public void TearDown()
        {
            loadedAssets.Clear();
        }

        [Test]
        public void ResultAssets_ContainEachReachableEndingVariantExactlyOnce()
        {
            var assetIds = AssetDatabase.FindAssets("t:EndingAsset", new[] { "Assets/Northbound/Data/Endings" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<EndingAsset>(path))
                .Where(asset => asset != null)
                .Select(asset => asset.Id)
                .OrderBy(id => id)
                .ToArray();

            CollectionAssert.AreEqual(new[]
            {
                "home_chosen", "no_map", "northbound", "not_alone_leo", "not_alone_noah", "pause_journey"
            }, assetIds);
        }

        [Test]
        public void PauseJourney_IsTheFourthWebsiteCoreEndingAndCarriesChoiceHistory()
        {
            var state = new NarrativeStateStore();
            state.Set("farewell_leo_uncertain", true);
            ChoiceConsequenceResolver.ApplyImplicit(state, "farewell_leo_uncertain");
            state.Set("carried_notebook", true);

            var result = new EndingResolver().Resolve(EndingDirection.PauseJourney, null, state.State);

            Assert.That(result.AssetId, Is.EqualTo("pause_journey"));
            Assert.That(result.DialogueVariantId, Is.EqualTo("pause_journey"));
            Assert.That(result.CarriedPropId, Is.EqualTo("notebook_blank_page"));
            Assert.That(result.HistoryEchoId, Is.EqualTo("farewell_leo_uncertain"));
        }

        [Test]
        public void StrongPlanHistory_HidesHomeButKeepsTheOtherCoreDirections()
        {
            var state = StateWithMarks(
                ChapterStoryMarkResolver.ChapterOnePlanFact,
                ChapterStoryMarkResolver.ChapterTwoPlanFact,
                ChapterStoryMarkResolver.ChapterThreePlanFact,
                ChapterStoryMarkResolver.ChapterFourAgencyFact);

            Assert.That(EndingResolver.IsStronglyNorthbound(state), Is.True);
            Assert.That(EndingResolver.IsStronglyRooted(state), Is.False);
            Assert.That(EndingResolver.IsDirectionAvailable(EndingDirection.Northbound, state), Is.True);
            Assert.That(EndingResolver.IsDirectionAvailable(EndingDirection.HomeChosen, state), Is.False);
            Assert.That(EndingResolver.IsDirectionAvailable(EndingDirection.NoMap, state), Is.True);
            Assert.That(EndingResolver.IsDirectionAvailable(EndingDirection.PauseJourney, state), Is.True);
        }

        [Test]
        public void StrongAgencyHistory_HidesNorthboundButKeepsTheOtherCoreDirections()
        {
            var state = StateWithMarks(
                ChapterStoryMarkResolver.ChapterOneAgencyFact,
                ChapterStoryMarkResolver.ChapterTwoAgencyFact,
                ChapterStoryMarkResolver.ChapterThreeAgencyFact,
                ChapterStoryMarkResolver.ChapterFourPlanFact);

            Assert.That(EndingResolver.IsStronglyNorthbound(state), Is.False);
            Assert.That(EndingResolver.IsStronglyRooted(state), Is.True);
            Assert.That(EndingResolver.IsDirectionAvailable(EndingDirection.Northbound, state), Is.False);
            Assert.That(EndingResolver.IsDirectionAvailable(EndingDirection.HomeChosen, state), Is.True);
            Assert.That(EndingResolver.IsDirectionAvailable(EndingDirection.NoMap, state), Is.True);
            Assert.That(EndingResolver.IsDirectionAvailable(EndingDirection.PauseJourney, state), Is.True);
        }

        [Test]
        public void MixedPlanAndAgencyHistory_KeepsAllFourCoreDirections()
        {
            var state = StateWithMarks(
                ChapterStoryMarkResolver.ChapterOnePlanFact,
                ChapterStoryMarkResolver.ChapterTwoPlanFact,
                ChapterStoryMarkResolver.ChapterThreeAgencyFact,
                ChapterStoryMarkResolver.ChapterFourAgencyFact);

            Assert.That(EndingResolver.IsStronglyNorthbound(state), Is.False);
            Assert.That(EndingResolver.IsStronglyRooted(state), Is.False);
            Assert.That(EndingResolver.IsDirectionAvailable(EndingDirection.Northbound, state), Is.True);
            Assert.That(EndingResolver.IsDirectionAvailable(EndingDirection.HomeChosen, state), Is.True);
            Assert.That(EndingResolver.IsDirectionAvailable(EndingDirection.NoMap, state), Is.True);
            Assert.That(EndingResolver.IsDirectionAvailable(EndingDirection.PauseJourney, state), Is.True);
        }

        [TestCase(4, -2, true, false)]
        [TestCase(-2, 4, false, true)]
        [TestCase(3, -2, false, false)]
        [TestCase(4, -1, false, false)]
        [TestCase(-1, 4, false, false)]
        [TestCase(-2, 3, false, false)]
        public void TendencyThresholds_MatchTheWebsiteRules(int commitment, int agency, bool northbound, bool rooted)
        {
            var state = new NarrativeState();
            state.Add(ChapterStoryMarkResolver.CommitmentCounterId, commitment);
            state.Add(ChapterStoryMarkResolver.AgencyCounterId, agency);

            Assert.That(EndingResolver.IsStronglyNorthbound(state), Is.EqualTo(northbound));
            Assert.That(EndingResolver.IsStronglyRooted(state), Is.EqualTo(rooted));
            Assert.That(EndingResolver.IsDirectionAvailable(EndingDirection.NoMap, state), Is.True);
            Assert.That(EndingResolver.IsDirectionAvailable(EndingDirection.PauseJourney, state), Is.True);
        }

        [Test]
        public void Northbound_PromiseOnlyChangesEliasLineNotPhysicalDirection()
        {
            var highPromise = new NarrativeState();
            highPromise.Add("promise", 2);
            var lowPromise = new NarrativeState();
            var resolver = new EndingResolver();

            var high = resolver.Resolve(EndingDirection.Northbound, null, highPromise);
            var low = resolver.Resolve(EndingDirection.Northbound, null, lowPromise);

            Assert.That(high.Direction, Is.EqualTo(EndingDirection.Northbound));
            Assert.That(low.Direction, Is.EqualTo(EndingDirection.Northbound));
            Assert.That(high.AssetId, Is.EqualTo("northbound"));
            Assert.That(low.AssetId, Is.EqualTo("northbound"));
            Assert.That(high.DialogueVariantId, Is.EqualTo("elias_ready"));
            Assert.That(low.DialogueVariantId, Is.EqualTo("elias_remember"));
        }

        [Test]
        public void HomeChosen_ConnectionOnlyChangesStaging()
        {
            var connected = new NarrativeState();
            connected.Add("connection", 2);
            var resolver = new EndingResolver();

            var high = resolver.Resolve(EndingDirection.HomeChosen, null, connected);
            var low = resolver.Resolve(EndingDirection.HomeChosen, null, new NarrativeState());

            Assert.That(high.Direction, Is.EqualTo(EndingDirection.HomeChosen));
            Assert.That(low.Direction, Is.EqualTo(EndingDirection.HomeChosen));
            Assert.That(high.LightingVariantId, Is.EqualTo("home_garage_light"));
            Assert.That(low.LightingVariantId, Is.EqualTo("home_bus_stop"));
        }

        [TestCase("carried_notebook", "notebook_write_date")]
        [TestCase("carried_photo", "photo_hold_to_sunrise")]
        [TestCase("carried_house_key", "house_key_unlock_door")]
        [TestCase("carried_map", "map_fold_keep")]
        public void NoMap_EachCarriedObjectHasItsOwnFinalGesture(string carriedFact, string expectedGesture)
        {
            var state = new NarrativeState();
            state.Set(carriedFact, true);

            var result = new EndingResolver().Resolve(EndingDirection.NoMap, null, state);

            Assert.That(result.AssetId, Is.EqualTo("no_map"));
            Assert.That(result.CarriedPropId, Is.EqualTo(expectedGesture));
        }

        [TestCase("maya")]
        [TestCase("noah")]
        [TestCase("leo")]
        public void FriendEnding_IsNeverLockedByBond(string friendId)
        {
            var state = new NarrativeState();
            state.Add($"bond_{friendId}", -99);

            var result = new EndingResolver().Resolve(EndingDirection.Friend, friendId, state);

            Assert.That(result.Direction, Is.EqualTo(EndingDirection.Friend));
            Assert.That(result.AssetId, Is.EqualTo($"not_alone_{friendId}"));
            Assert.That(result.FriendId, Is.EqualTo(friendId));
        }

        [TestCase("farewell_maya_committed")]
        [TestCase("farewell_maya_curious")]
        [TestCase("farewell_maya_uncertain")]
        [TestCase("farewell_maya_silent")]
        public void AuthoredFarewellChoice_IsRecalledInLaterEndingPresentation(string choiceFact)
        {
            var state = new NarrativeStateStore();
            state.Set(choiceFact, true);
            Assert.That(ChoiceConsequenceResolver.ApplyImplicit(state, choiceFact), Is.True);

            var result = new EndingResolver().Resolve(EndingDirection.Northbound, null, state.State);

            Assert.That(result.HistoryEchoId, Is.EqualTo(choiceFact));
            Assert.That(result.HistoryEchoText, Is.Not.Empty);
            Assert.That(result.HistoryEchoTextChinese, Is.Not.Empty);
        }

        [Test]
        public void RealRelationshipChoices_ChangeLaterEndingVariants()
        {
            var committed = new NarrativeStateStore();
            committed.Set("optional_elias_garage_committed", true);
            ChoiceConsequenceResolver.ApplyImplicit(committed, "optional_elias_garage_committed");
            var silent = new NarrativeStateStore();
            silent.Set("optional_elias_garage_silent", true);
            ChoiceConsequenceResolver.ApplyImplicit(silent, "optional_elias_garage_silent");
            var resolver = new EndingResolver();

            var committedEnding = resolver.Resolve(EndingDirection.Northbound, null, committed.State);
            var silentEnding = resolver.Resolve(EndingDirection.Northbound, null, silent.State);

            Assert.That(committedEnding.DialogueVariantId, Is.EqualTo("elias_ready"));
            Assert.That(silentEnding.DialogueVariantId, Is.EqualTo("elias_remember"));
            Assert.That(committedEnding.HistoryEchoId, Is.EqualTo("optional_elias_garage_committed"));
            Assert.That(silentEnding.HistoryEchoId, Is.EqualTo("optional_elias_garage_silent"));
        }

        [Test]
        public void ChapterFourStances_CreateDistinctBilingualEndingEchoes()
        {
            var marks = new[]
            {
                ChapterStoryMarkResolver.ChapterFourPlanFact,
                ChapterStoryMarkResolver.ChapterFourBalanceFact,
                ChapterStoryMarkResolver.ChapterFourAgencyFact
            };
            var resolver = new EndingResolver();
            var results = new List<EndingContext>();

            foreach (var mark in marks)
            {
                var state = new NarrativeState();
                state.Set(mark, true);
                results.Add(resolver.Resolve(EndingDirection.PauseJourney, null, state));
            }

            Assert.That(results.Select(result => result.HistoryEchoId), Is.EqualTo(marks));
            Assert.That(results.Select(result => result.HistoryEchoText).Distinct().Count(), Is.EqualTo(3));
            Assert.That(results.Select(result => result.HistoryEchoTextChinese).Distinct().Count(), Is.EqualTo(3));
            Assert.That(results, Has.All.Matches<EndingContext>(result =>
                !string.IsNullOrWhiteSpace(result.HistoryEchoText) &&
                !string.IsNullOrWhiteSpace(result.HistoryEchoTextChinese)));
        }

        private static NarrativeState StateWithMarks(params string[] marks)
        {
            var state = new NarrativeState();
            foreach (var mark in marks) state.Set(mark, true);
            return state;
        }
    }
}
