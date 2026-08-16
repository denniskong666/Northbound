using System;

namespace Northbound.Quests
{
    [Serializable]
    public sealed class QuestObjective
    {
        public string id;
        public string description;
        public int requiredAmount = 1;
    }
}
