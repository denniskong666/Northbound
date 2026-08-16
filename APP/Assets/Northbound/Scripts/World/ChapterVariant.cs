using UnityEngine;
using Northbound.Narrative;

namespace Northbound.World
{
    [CreateAssetMenu(menuName = "Northbound/Chapter Variant")]
    public sealed class ChapterVariant : ScriptableObject
    {
        public string id;
        public string[] requiredFacts = new string[0];
        public string[] forbiddenFacts = new string[0];
        public GameObject[] objectsToActivate = new GameObject[0];
        public GameObject[] objectsToDeactivate = new GameObject[0];
        public string[] objectIdsToActivate = new string[0];
        public string[] objectIdsToDeactivate = new string[0];
        public string spawnPointId;
        public string ambientSnapshotId;
        public string[] startingQuestIds = new string[0];

        public bool Matches(NarrativeState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            foreach (var fact in requiredFacts ?? new string[0])
            {
                if (!string.IsNullOrWhiteSpace(fact) && !state.Has(fact))
                {
                    return false;
                }
            }

            foreach (var fact in forbiddenFacts ?? new string[0])
            {
                if (!string.IsNullOrWhiteSpace(fact) && state.Has(fact))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
