using System;
using System.Collections.Generic;
using System.Linq;
using Northbound.Endings;
using Northbound.Narrative;

namespace Northbound.Content
{
    /// <summary>Deterministic content-path model used by smoke tests and build validation.</summary>
    public sealed class NarrativePathSimulator
    {
        private readonly NarrativeContentManifest manifest;
        private readonly NarrativeState state = new NarrativeState();
        private readonly Dictionary<string, string> commitmentByPair = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<string> completedPairs = new List<string>();

        public NarrativePathSimulator(NarrativeContentManifest content)
        {
            manifest = content ?? throw new ArgumentNullException(nameof(content));
            CurrentChapterId = "prologue";
        }

        public string CurrentChapterId { get; private set; }
        public IReadOnlyList<string> CompletedPairs => completedPairs;
        public NarrativeState State => state;

        public void SetFact(string fact) => state.Set(fact, true);

        public bool CompleteExclusiveMission(string questId)
        {
            var quest = manifest.FindQuest(questId);
            if (quest == null || string.IsNullOrWhiteSpace(quest.pairId)) return false;
            if (commitmentByPair.TryGetValue(quest.pairId, out _)) return false;
            commitmentByPair.Add(quest.pairId, questId);
            completedPairs.Add(quest.pairId);
            state.Set($"quest_{questId}_complete", true);
            foreach (var fact in quest.completionFacts ?? Array.Empty<string>()) state.Set(fact, true);
            RecordFriendMission(quest.id);
            foreach (var sibling in manifest.quests.Where(item => item != null && item.pairId == quest.pairId && item.id != questId)) state.Set($"missed_{sibling.id}", true);
            AdvanceForPair(quest.pairId);
            return true;
        }

        public bool EnterFinale()
        {
            if (completedPairs.Count != 3 || commitmentByPair.Count != 3) return false;
            CurrentChapterId = "chapter_4";
            CurrentChapterId = "finale";
            state.Set("current_chapter_finale", true);
            return true;
        }

        public EndingContext ResolveEnding(string endingId)
        {
            var resolver = new EndingResolver();
            switch (endingId)
            {
                case "northbound": return resolver.Resolve(EndingDirection.Northbound, null, state);
                case "home_chosen": return resolver.Resolve(EndingDirection.HomeChosen, null, state);
                case "no_map": return resolver.Resolve(EndingDirection.NoMap, null, state);
                case "pause_journey": return resolver.Resolve(EndingDirection.PauseJourney, null, state);
                case "not_alone_maya": return resolver.Resolve(EndingDirection.Friend, "maya", state);
                case "not_alone_noah": return resolver.Resolve(EndingDirection.Friend, "noah", state);
                case "not_alone_leo": return resolver.Resolve(EndingDirection.Friend, "leo", state);
                default: throw new ArgumentException("Unknown approved ending id.", nameof(endingId));
            }
        }

        private void AdvanceForPair(string pairId)
        {
            if (pairId == "alternator|first_light") CurrentChapterId = "chapter_3_day_3";
            if (pairId == "road_test|static") CurrentChapterId = "chapter_3_day_2";
            if (pairId == "pack_trunk|last_night_open") CurrentChapterId = "chapter_4";
        }

        private void RecordFriendMission(string questId)
        {
            var friend = questId == "first_light" ? "maya" : questId == "static" ? "noah" : questId == "last_night_open" ? "leo" : string.Empty;
            if (string.IsNullOrEmpty(friend)) return;
            state.Add($"bond_{friend}", 1);
            var order = state.GetInt("friend_completion_count") + 1;
            state.Add("friend_completion_count", 1);
            state.Set($"friend_{friend}_completion_order_{order}", true);
        }
    }
}
