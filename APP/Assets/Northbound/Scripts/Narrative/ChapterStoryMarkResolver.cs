using System;

namespace Northbound.Narrative
{
    public static class ChapterStoryMarkResolver
    {
        public const string CommitmentCounterId = "tendency_commitment";
        public const string RootednessCounterId = "tendency_rootedness";
        public const string AgencyCounterId = "tendency_agency";
        public const string ChapterOnePlanFact = "story_mark_ch1_a";
        public const string ChapterOneBalanceFact = "story_mark_ch1_b";
        public const string ChapterOneAgencyFact = "story_mark_ch1_c";
        public const string ChapterTwoPlanFact = "story_mark_ch2_a";
        public const string ChapterTwoBalanceFact = "story_mark_ch2_b";
        public const string ChapterTwoAgencyFact = "story_mark_ch2_c";
        public const string ChapterThreePlanFact = "story_mark_ch3_a";
        public const string ChapterThreeBalanceFact = "story_mark_ch3_b";
        public const string ChapterThreeAgencyFact = "story_mark_ch3_c";
        public const string ChapterFourPlanFact = "story_mark_ch4_a";
        public const string ChapterFourBalanceFact = "story_mark_ch4_b";
        public const string ChapterFourAgencyFact = "story_mark_ch4_c";
        public const int ChapterOneDecisionMargin = 5;

        private static readonly string[] ChapterOneFacts =
        {
            ChapterOnePlanFact,
            ChapterOneBalanceFact,
            ChapterOneAgencyFact
        };

        private static readonly string[][] ChapterFacts =
        {
            ChapterOneFacts,
            new[] { ChapterTwoPlanFact, ChapterTwoBalanceFact, ChapterTwoAgencyFact },
            new[] { ChapterThreePlanFact, ChapterThreeBalanceFact, ChapterThreeAgencyFact },
            new[] { ChapterFourPlanFact, ChapterFourBalanceFact, ChapterFourAgencyFact }
        };

        public static string ResolveChapterOne(NarrativeStateStore state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var commitment = state.GetInt(CommitmentCounterId);
            var agency = state.GetInt(AgencyCounterId);
            var selectedFact = commitment > agency + ChapterOneDecisionMargin
                ? ChapterOnePlanFact
                : agency > commitment + ChapterOneDecisionMargin
                    ? ChapterOneAgencyFact
                    : ChapterOneBalanceFact;

            foreach (var fact in ChapterOneFacts)
            {
                state.Set(fact, fact == selectedFact);
            }

            return selectedFact;
        }

        public static bool TrySetExclusive(NarrativeStateStore state, string selectedFact)
        {
            if (state == null || string.IsNullOrWhiteSpace(selectedFact)) return false;

            foreach (var chapter in ChapterFacts)
            {
                if (Array.IndexOf(chapter, selectedFact) < 0) continue;
                foreach (var fact in chapter) state.Set(fact, fact == selectedFact);
                return true;
            }

            return false;
        }
    }
}
