using System.Collections.Generic;
using UnityEngine;

namespace Northbound.Quests
{
    [CreateAssetMenu(menuName = "Northbound/Quest")]
    public sealed class QuestAsset : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea(2, 4)] public string hint;
        public List<QuestObjective> objectives = new List<QuestObjective>();
        public string[] completionFacts = new string[0];
        public string[] nextQuestIds = new string[0];
    }
}
