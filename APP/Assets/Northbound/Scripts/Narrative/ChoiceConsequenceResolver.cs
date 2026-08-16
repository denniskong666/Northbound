using System;

namespace Northbound.Narrative
{
    public sealed class ChoiceEcho
    {
        public static readonly ChoiceEcho Empty = new ChoiceEcho(string.Empty, string.Empty, string.Empty);

        public ChoiceEcho(string id, string english, string chinese)
        {
            Id = id ?? string.Empty;
            English = english ?? string.Empty;
            Chinese = chinese ?? string.Empty;
        }

        public string Id { get; }
        public string English { get; }
        public string Chinese { get; }
        public bool IsEmpty => string.IsNullOrEmpty(Id);
    }

    /// <summary>Turns every authored relationship choice into persistent, later-consumed state.</summary>
    public static class ChoiceConsequenceResolver
    {
        private static readonly string[] CharacterIds = { "maya", "noah", "leo", "elias" };
        private static readonly string[] ToneIds = { "committed", "curious", "uncertain", "silent" };

        public static bool ApplyImplicit(NarrativeStateStore state, string choiceFact)
        {
            if (state == null || !TryParse(choiceFact, out var characterId, out var toneId))
            {
                return false;
            }

            var recordedFact = $"choice_effect_recorded_{choiceFact}";
            if (state.Has(recordedFact))
            {
                return true;
            }

            switch (toneId)
            {
                case "committed":
                    Apply(state, characterId, commitment: 6, rootedness: 3, agency: -1, bond: 4);
                    break;
                case "curious":
                    Apply(state, characterId, commitment: 1, rootedness: 1, agency: 5, bond: 3);
                    break;
                case "uncertain":
                    Apply(state, characterId, commitment: 0, rootedness: 2, agency: 2, bond: 1);
                    break;
                case "silent":
                    Apply(state, characterId, commitment: -2, rootedness: -2, agency: -2, bond: -3);
                    break;
                default:
                    return false;
            }

            state.Set(recordedFact, true);
            return true;
        }

        public static bool IsTrackedChoiceFact(string choiceFact)
        {
            return TryParse(choiceFact, out _, out _);
        }

        public static ChoiceEcho ResolveEcho(NarrativeState state)
        {
            if (state == null)
            {
                return ChoiceEcho.Empty;
            }

            var storyMark = ResolveLatestStoryMarkEcho(state);
            var farewell = ResolveBestEcho(state, true);
            var relationship = !farewell.IsEmpty ? farewell : ResolveBestEcho(state, false);
            if (storyMark.IsEmpty)
            {
                return relationship;
            }

            if (relationship.IsEmpty)
            {
                return storyMark;
            }

            return new ChoiceEcho(
                $"{storyMark.Id}|{relationship.Id}",
                $"{storyMark.English}\n{relationship.English}",
                $"{storyMark.Chinese}\n{relationship.Chinese}");
        }

        private static ChoiceEcho ResolveLatestStoryMarkEcho(NarrativeState state)
        {
            var orderedMarks = new[]
            {
                (ChapterStoryMarkResolver.ChapterFourPlanFact,
                    "Before dawn, Jamie chose to keep the old promise alive.",
                    "黎明前，杰米选择继续守住那份旧约定。"),
                (ChapterStoryMarkResolver.ChapterFourBalanceFact,
                    "Before dawn, Jamie refused to turn staying or leaving into the only right answer.",
                    "黎明前，杰米拒绝把留下或离开说成唯一正确的答案。"),
                (ChapterStoryMarkResolver.ChapterFourAgencyFact,
                    "Before dawn, Jamie said each person had to choose a life that actually fit them.",
                    "黎明前，杰米说，每个人都该选择真正适合自己的生活。"),
                (ChapterStoryMarkResolver.ChapterThreePlanFact,
                    "On the rooftop, Jamie sided with the promise the group had made.",
                    "在屋顶上，杰米站在了大家曾经许下的约定这一边。"),
                (ChapterStoryMarkResolver.ChapterThreeBalanceFact,
                    "On the rooftop, Jamie asked both sides to stop making the others surrender.",
                    "在屋顶上，杰米要求双方都别再逼另一边投降。"),
                (ChapterStoryMarkResolver.ChapterThreeAgencyFact,
                    "On the rooftop, Jamie defended everyone's right to change their mind.",
                    "在屋顶上，杰米捍卫了每个人改变心意的权利。"),
                (ChapterStoryMarkResolver.ChapterTwoPlanFact,
                    "When Friday was questioned, Jamie put the shared departure first.",
                    "当星期五的计划受到质疑时，杰米把共同出发放在了第一位。"),
                (ChapterStoryMarkResolver.ChapterTwoBalanceFact,
                    "When Friday was questioned, Jamie tried to leave without cutting every tie to home.",
                    "当星期五的计划受到质疑时，杰米想要离开，也不愿斩断与故乡的全部联结。"),
                (ChapterStoryMarkResolver.ChapterTwoAgencyFact,
                    "When Friday was questioned, Jamie put each friend's chosen future before the timetable.",
                    "当星期五的计划受到质疑时，杰米把每个朋友自己选择的未来放在了时间表之前。"),
                (ChapterStoryMarkResolver.ChapterOnePlanFact,
                    "From the beginning, Jamie treated the northern plan as a promise.",
                    "从一开始，杰米就把北上的计划当成一份约定。"),
                (ChapterStoryMarkResolver.ChapterOneBalanceFact,
                    "From the beginning, Jamie kept room for both the road and Greybridge.",
                    "从一开始，杰米就同时为远方和格雷布里奇保留了位置。"),
                (ChapterStoryMarkResolver.ChapterOneAgencyFact,
                    "From the beginning, Jamie questioned whether leaving was the only way to live.",
                    "从一开始，杰米就在追问，离开是否真的是唯一的生活方式。")
            };

            foreach (var mark in orderedMarks)
            {
                if (state.Has(mark.Item1))
                {
                    return new ChoiceEcho(mark.Item1, mark.Item2, mark.Item3);
                }
            }

            return ChoiceEcho.Empty;
        }

        private static ChoiceEcho ResolveBestEcho(NarrativeState state, bool farewell)
        {
            ChoiceEcho best = ChoiceEcho.Empty;
            var bestBond = int.MinValue;
            foreach (var characterId in CharacterIds)
            {
                foreach (var toneId in ToneIds)
                {
                    var fact = farewell ? $"farewell_{characterId}_{toneId}" : OptionalFact(characterId, toneId);
                    if (!state.Has(fact))
                    {
                        continue;
                    }

                    var bond = state.GetInt($"bond_{characterId}");
                    if (best.IsEmpty || bond > bestBond)
                    {
                        best = Echo(fact, characterId, toneId);
                        bestBond = bond;
                    }
                }
            }

            return best;
        }

        private static void Apply(NarrativeStateStore state, string characterId, int commitment, int rootedness, int agency, int bond)
        {
            state.Add(ChapterStoryMarkResolver.CommitmentCounterId, commitment);
            state.Add(ChapterStoryMarkResolver.RootednessCounterId, rootedness);
            state.Add(ChapterStoryMarkResolver.AgencyCounterId, agency);
            state.Add($"bond_{characterId}", bond);
        }

        private static bool TryParse(string fact, out string characterId, out string toneId)
        {
            characterId = string.Empty;
            toneId = string.Empty;
            if (string.IsNullOrWhiteSpace(fact))
            {
                return false;
            }

            foreach (var candidateCharacter in CharacterIds)
            {
                foreach (var candidateTone in ToneIds)
                {
                    if (string.Equals(fact, $"farewell_{candidateCharacter}_{candidateTone}", StringComparison.Ordinal) ||
                        string.Equals(fact, OptionalFact(candidateCharacter, candidateTone), StringComparison.Ordinal))
                    {
                        characterId = candidateCharacter;
                        toneId = candidateTone;
                        return true;
                    }
                }
            }

            return false;
        }

        private static string OptionalFact(string characterId, string toneId)
        {
            var location = characterId == "maya" ? "mural" : characterId == "noah" ? "radio" : characterId == "leo" ? "diner" : "garage";
            return $"optional_{characterId}_{location}_{toneId}";
        }

        private static ChoiceEcho Echo(string fact, string characterId, string toneId)
        {
            var name = characterId == "maya" ? "Maya" : characterId == "noah" ? "Noah" : characterId == "leo" ? "Leo" : "Elias";
            var chineseName = characterId == "maya" ? "玛雅" : characterId == "noah" ? "诺亚" : characterId == "leo" ? "利奥" : "伊莱亚斯";
            switch (toneId)
            {
                case "committed":
                    return new ChoiceEcho(fact, $"{name} remembers that Jamie promised to be there.", $"{chineseName}记得杰米答应过会留下陪伴。");
                case "curious":
                    return new ChoiceEcho(fact, $"{name} remembers that Jamie asked for the truth instead of an easy promise.", $"{chineseName}记得杰米没有轻易承诺，而是追问了真实的想法。");
                case "uncertain":
                    return new ChoiceEcho(fact, $"{name} remembers that Jamie admitted there was no answer yet.", $"{chineseName}记得杰米承认，那时还没有答案。");
                default:
                    return new ChoiceEcho(fact, $"{name} remembers the answer Jamie left in silence.", $"{chineseName}记得杰米把答案留在了沉默里。");
            }
        }
    }
}
