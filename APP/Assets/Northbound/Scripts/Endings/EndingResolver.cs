using System;
using Northbound.Narrative;

namespace Northbound.Endings
{
    public sealed class EndingResolver
    {
        private const string NorthboundEndCard = "Some promises carry us forward. Some ask us how long we are willing to be carried.";
        private const string HomeEndCard = "Staying is not the absence of a journey when staying is a choice.";
        private const string NoMapEndCard = "Not every road begins with a destination.";
        private const string PauseEndCard = "Taking time is also a direction when the choice is finally your own.";
        private const string FriendEndCard = "A life can be chosen by direction—and by who we choose to meet there.";

        public EndingContext Resolve(EndingDirection direction, string friendId, NarrativeState state)
        {
            state = state ?? new NarrativeState();

            switch (direction)
            {
                case EndingDirection.Northbound:
                    return ResolveNorthbound(state);
                case EndingDirection.HomeChosen:
                    return ResolveHome(state);
                case EndingDirection.NoMap:
                    return ResolveNoMap(state);
                case EndingDirection.PauseJourney:
                    return ResolvePauseJourney(state);
                case EndingDirection.Friend:
                    return ResolveFriend(friendId, state);
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown ending direction.");
            }
        }

        public static bool IsDirectionAvailable(EndingDirection direction, NarrativeState state)
        {
            state ??= new NarrativeState();
            if (direction == EndingDirection.HomeChosen && IsStronglyNorthbound(state)) return false;
            if (direction == EndingDirection.Northbound && IsStronglyRooted(state)) return false;
            return true;
        }

        public static bool IsStronglyNorthbound(NarrativeState state)
        {
            state ??= new NarrativeState();
            var planMarks = CountFacts(state, ChapterStoryMarkResolver.ChapterOnePlanFact, ChapterStoryMarkResolver.ChapterTwoPlanFact,
                ChapterStoryMarkResolver.ChapterThreePlanFact, ChapterStoryMarkResolver.ChapterFourPlanFact);
            var agencyMarks = CountFacts(state, ChapterStoryMarkResolver.ChapterOneAgencyFact, ChapterStoryMarkResolver.ChapterTwoAgencyFact,
                ChapterStoryMarkResolver.ChapterThreeAgencyFact, ChapterStoryMarkResolver.ChapterFourAgencyFact);
            return (planMarks >= 3 && agencyMarks <= 1) ||
                   (state.GetInt(ChapterStoryMarkResolver.CommitmentCounterId) > 3 && state.GetInt(ChapterStoryMarkResolver.AgencyCounterId) < -1);
        }

        public static bool IsStronglyRooted(NarrativeState state)
        {
            state ??= new NarrativeState();
            var planMarks = CountFacts(state, ChapterStoryMarkResolver.ChapterOnePlanFact, ChapterStoryMarkResolver.ChapterTwoPlanFact,
                ChapterStoryMarkResolver.ChapterThreePlanFact, ChapterStoryMarkResolver.ChapterFourPlanFact);
            var agencyMarks = CountFacts(state, ChapterStoryMarkResolver.ChapterOneAgencyFact, ChapterStoryMarkResolver.ChapterTwoAgencyFact,
                ChapterStoryMarkResolver.ChapterThreeAgencyFact, ChapterStoryMarkResolver.ChapterFourAgencyFact);
            return (agencyMarks >= 3 && planMarks <= 1) ||
                   (state.GetInt(ChapterStoryMarkResolver.AgencyCounterId) > 3 && state.GetInt(ChapterStoryMarkResolver.CommitmentCounterId) < -1);
        }

        private static EndingContext ResolveNorthbound(NarrativeState state)
        {
            var highPromise = state.GetInt(ChapterStoryMarkResolver.CommitmentCounterId) > 0 ||
                HasTendency(state, "promise", "helped_elias", "completed_road_test", "packed_trunk");
            return WithHistory(state, new EndingContext(
                EndingDirection.Northbound,
                "northbound",
                highPromise ? "elias_ready" : "elias_remember",
                "second_key",
                "dawn_car",
                NorthboundEndCard));
        }

        private static EndingContext ResolveHome(NarrativeState state)
        {
            var highConnection = state.GetInt(ChapterStoryMarkResolver.RootednessCounterId) > 0 ||
                HasTendency(state, "connection", "helped_maya", "helped_noah", "helped_leo");
            return WithHistory(state, new EndingContext(
                EndingDirection.HomeChosen,
                "home_chosen",
                highConnection ? "home_garage" : "home_bus_stop",
                highConnection ? "garage_light_switch" : "bus_stop_bench",
                highConnection ? "home_garage_light" : "home_bus_stop",
                HomeEndCard));
        }

        private static EndingContext ResolveNoMap(NarrativeState state)
        {
            var carriedPropId = "map_fold_keep";
            var dialogueVariantId = "no_map_map";
            if (state.Has("carried_notebook"))
            {
                carriedPropId = "notebook_write_date";
                dialogueVariantId = "no_map_notebook";
            }
            else if (state.Has("carried_photo"))
            {
                carriedPropId = "photo_hold_to_sunrise";
                dialogueVariantId = "no_map_photo";
            }
            else if (state.Has("carried_house_key"))
            {
                carriedPropId = "house_key_unlock_door";
                dialogueVariantId = "no_map_house_key";
            }

            return WithHistory(state, new EndingContext(
                EndingDirection.NoMap,
                "no_map",
                dialogueVariantId,
                carriedPropId,
                "unmarked_road_dawn",
                NoMapEndCard));
        }

        private static EndingContext ResolveFriend(string friendId, NarrativeState state)
        {
            var normalizedFriend = (friendId ?? string.Empty).Trim().ToLowerInvariant();
            switch (normalizedFriend)
            {
                case "maya":
                    return Friend("maya", "maya_mural", "paint_brush", "mural_dawn", state);
                case "noah":
                    return Friend("noah", "noah_radio", "headphones", "radio_dawn", state);
                case "leo":
                    return Friend("leo", "leo_diner", "closed_sign", "diner_dawn", state);
                default:
                    throw new ArgumentException("Friend endings require Maya, Noah, or Leo.", nameof(friendId));
            }
        }

        private static EndingContext ResolvePauseJourney(NarrativeState state)
        {
            var keepsake = state.Has("carried_notebook") ? "notebook_blank_page" :
                state.Has("carried_photo") ? "photo_rooftop_dawn" :
                state.Has("carried_house_key") ? "house_key_in_pocket" : "folded_map_beside_arrow";
            return WithHistory(state, new EndingContext(
                EndingDirection.PauseJourney,
                "pause_journey",
                "pause_journey",
                keepsake,
                "rooftop_first_light",
                PauseEndCard));
        }

        private static EndingContext Friend(string friendId, string dialogueVariantId, string carriedPropId, string lightingVariantId, NarrativeState state)
        {
            return WithHistory(state, new EndingContext(
                EndingDirection.Friend,
                $"not_alone_{friendId}",
                dialogueVariantId,
                carriedPropId,
                lightingVariantId,
                FriendEndCard,
                friendId));
        }

        private static EndingContext WithHistory(NarrativeState state, EndingContext context)
        {
            var echo = ChoiceConsequenceResolver.ResolveEcho(state);
            return echo.IsEmpty
                ? context
                : new EndingContext(
                    context.Direction,
                    context.AssetId,
                    context.DialogueVariantId,
                    context.CarriedPropId,
                    context.LightingVariantId,
                    context.EndCard,
                    context.FriendId,
                    echo.Id,
                    echo.English,
                    echo.Chinese);
        }

        private static bool HasTendency(NarrativeState state, string counterId, params string[] supportingFacts)
        {
            if (state.GetInt(counterId) > 0)
            {
                return true;
            }

            foreach (var fact in supportingFacts)
            {
                if (state.Has(fact))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountFacts(NarrativeState state, params string[] facts)
        {
            var count = 0;
            foreach (var fact in facts) if (state.Has(fact)) count++;
            return count;
        }
    }
}
