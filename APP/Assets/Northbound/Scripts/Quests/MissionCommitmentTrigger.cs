using Northbound.Core;
using Northbound.Interaction;
using UnityEngine;

namespace Northbound.Quests
{
    public sealed class MissionCommitmentTrigger : MonoBehaviour, IInteractable
    {
        [SerializeField] private string questId;
        [SerializeField] private string pairedQuestId;
        [SerializeField] private string prompt = "Discuss mission";

        private MissionPairController pair;

        public string QuestId => questId;

        public string Prompt => prompt;

        public bool CanInteract => GetPair() != null && GetPair().IsAvailable(questId);

        public void Interact(GameObject actor)
        {
            GetPair()?.BeginCommitment(questId);
        }

        private MissionPairController GetPair()
        {
            if (pair != null)
            {
                return pair;
            }

            var sceneContext = FindFirstObjectByType<MissionPairSceneContext>();
            if (sceneContext != null)
            {
                pair = new MissionPairController(
                    questId,
                    pairedQuestId,
                    sceneContext.NarrativeState,
                    sceneContext.SaveGame,
                    sceneContext.Dialogue);
                return pair;
            }

            if (GameBootstrap.Instance == null)
            {
                return null;
            }

            pair = new MissionPairController(
                questId,
                pairedQuestId,
                GameBootstrap.Instance.NarrativeState,
                GameBootstrap.Instance.SaveGame,
                GameBootstrap.Instance.Dialogue);
            return pair;
        }
    }
}
