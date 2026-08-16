using System;
using System.Collections.Generic;
using Northbound.Narrative;

namespace Northbound.Quests
{
    public sealed class QuestRunner
    {
        private readonly NarrativeStateStore state;
        private readonly Dictionary<string, QuestAsset> knownQuests = new Dictionary<string, QuestAsset>();

        public QuestRunner(NarrativeStateStore narrativeState)
        {
            state = narrativeState ?? throw new ArgumentNullException(nameof(narrativeState));
        }

        public string ActiveQuestId { get; private set; }
        public string NextObjectiveId => !string.IsNullOrWhiteSpace(ActiveQuestId) && knownQuests.TryGetValue(ActiveQuestId, out var quest)
            ? NextIncompleteObjective(quest)?.id : null;
        public event Action<string> QuestCompleted;

        public string RestoreActiveQuest(IEnumerable<QuestAsset> quests)
        {
            if (quests == null) return null;
            foreach (var quest in quests)
            {
                if (quest == null || string.IsNullOrWhiteSpace(quest.id)) continue;
                knownQuests[quest.id] = quest;
            }

            ActiveQuestId = null;
            foreach (var quest in knownQuests.Values)
            {
                if (!state.Has(StartedFact(quest.id)) || state.Has(CompletionFact(quest.id))) continue;
                ActiveQuestId = quest.id;
                break;
            }
            return ActiveQuestId;
        }

        public bool StartQuest(QuestAsset quest)
        {
            if (quest == null || string.IsNullOrWhiteSpace(quest.id) || state.Has(CompletionFact(quest.id)) ||
                (!string.IsNullOrWhiteSpace(ActiveQuestId) && ActiveQuestId != quest.id))
            {
                return false;
            }

            knownQuests[quest.id] = quest;
            ActiveQuestId = quest.id;
            state.Set(StartedFact(quest.id), true);
            return true;
        }

        public bool IsCurrentObjective(string questId, string objectiveId)
        {
            return questId == ActiveQuestId && knownQuests.TryGetValue(questId, out var quest) && NextIncompleteObjective(quest)?.id == objectiveId;
        }

        public bool Report(string objectiveId, int amount)
        {
            if (string.IsNullOrWhiteSpace(ActiveQuestId) || string.IsNullOrWhiteSpace(objectiveId) || amount <= 0 ||
                !knownQuests.TryGetValue(ActiveQuestId, out var quest))
            {
                return false;
            }

            var objective = NextIncompleteObjective(quest);
            if (objective == null || objective.id != objectiveId)
            {
                return false;
            }

            var requiredAmount = Math.Max(1, objective.requiredAmount);
            var progressFact = ObjectiveProgressFact(quest.id, objective.id);
            var currentAmount = state.GetInt(progressFact);
            var acceptedAmount = Math.Min(amount, requiredAmount - currentAmount);
            if (acceptedAmount <= 0)
            {
                return false;
            }

            state.Add(progressFact, acceptedAmount);
            if (currentAmount + acceptedAmount < requiredAmount)
            {
                return true;
            }

            state.Set(ObjectiveCompletionFact(quest.id, objective.id), true);
            if (NextIncompleteObjective(quest) == null)
            {
                CompleteQuest(quest.id);
            }

            return true;
        }

        public bool CompleteQuest(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId) || questId != ActiveQuestId || !knownQuests.TryGetValue(questId, out var quest) ||
                NextIncompleteObjective(quest) != null)
            {
                return false;
            }

            state.Set(CompletionFact(questId), true);
            if (quest.completionFacts != null)
            {
                foreach (var fact in quest.completionFacts)
                {
                    state.Set(fact, true);
                }
            }

            if (quest.nextQuestIds != null)
            {
                foreach (var nextQuestId in quest.nextQuestIds)
                {
                    state.Set(AvailableFact(nextQuestId), true);
                }
            }

            ActiveQuestId = null;
            QuestCompleted?.Invoke(questId);
            return true;
        }

        public static string CompletionFact(string questId)
        {
            return $"quest_{questId}_complete";
        }

        public static string StartedFactId(string questId) => StartedFact(questId);
        public static string ObjectiveProgressFactId(string questId, string objectiveId) => ObjectiveProgressFact(questId, objectiveId);
        public static string ObjectiveCompletionFactId(string questId, string objectiveId) => ObjectiveCompletionFact(questId, objectiveId);

        private QuestObjective NextIncompleteObjective(QuestAsset quest)
        {
            if (quest.objectives == null)
            {
                return null;
            }

            foreach (var objective in quest.objectives)
            {
                if (objective != null && !string.IsNullOrWhiteSpace(objective.id) && !state.Has(ObjectiveCompletionFact(quest.id, objective.id)))
                {
                    return objective;
                }
            }

            return null;
        }

        private static string StartedFact(string questId) => $"quest_{questId}_started";

        private static string AvailableFact(string questId) => $"quest_{questId}_available";

        private static string ObjectiveProgressFact(string questId, string objectiveId) => $"quest_{questId}_objective_{objectiveId}_progress";

        private static string ObjectiveCompletionFact(string questId, string objectiveId) => $"quest_{questId}_objective_{objectiveId}_complete";
    }
}
