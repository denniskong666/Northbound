using Northbound.Narrative;
using NUnit.Framework;

namespace Northbound.Tests
{
    public sealed class ChapterStoryMarkResolverTests
    {
        [TestCase(16, 0, ChapterStoryMarkResolver.ChapterOnePlanFact)]
        [TestCase(0, 16, ChapterStoryMarkResolver.ChapterOneAgencyFact)]
        [TestCase(10, 5, ChapterStoryMarkResolver.ChapterOneBalanceFact)]
        [TestCase(3, 3, ChapterStoryMarkResolver.ChapterOneBalanceFact)]
        public void ResolveChapterOne_UsesScaledWebsiteThresholds(int commitment, int agency, string expectedFact)
        {
            var state = new NarrativeStateStore();
            state.Add(ChapterStoryMarkResolver.CommitmentCounterId, commitment);
            state.Add(ChapterStoryMarkResolver.AgencyCounterId, agency);

            var result = ChapterStoryMarkResolver.ResolveChapterOne(state);

            Assert.That(result, Is.EqualTo(expectedFact));
            Assert.That(state.Has(expectedFact), Is.True);
        }

        [Test]
        public void ResolveChapterOne_ReplacesAStaleMarkAndLeavesExactlyOne()
        {
            var state = new NarrativeStateStore();
            state.Set(ChapterStoryMarkResolver.ChapterOnePlanFact, true);
            state.Add(ChapterStoryMarkResolver.AgencyCounterId, 20);

            var result = ChapterStoryMarkResolver.ResolveChapterOne(state);

            Assert.That(result, Is.EqualTo(ChapterStoryMarkResolver.ChapterOneAgencyFact));
            Assert.That(state.Has(ChapterStoryMarkResolver.ChapterOnePlanFact), Is.False);
            Assert.That(state.Has(ChapterStoryMarkResolver.ChapterOneBalanceFact), Is.False);
            Assert.That(state.Has(ChapterStoryMarkResolver.ChapterOneAgencyFact), Is.True);
        }
    }
}
