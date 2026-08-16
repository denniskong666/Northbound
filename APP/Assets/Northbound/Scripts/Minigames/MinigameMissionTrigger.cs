using Northbound.Core;
using Northbound.Interaction;
using UnityEngine;

namespace Northbound.Minigames
{
    public sealed class MinigameMissionTrigger : MonoBehaviour, IInteractable
    {
        [SerializeField] private string minigameId;
        [SerializeField] private string questId;
        [SerializeField] private string objectiveId;
        [SerializeField] private string prompt;

        public string MinigameId => minigameId;
        public string Prompt => prompt;
        public bool CanInteract => GameBootstrap.Instance != null && GameBootstrap.Instance.Minigames != null && !string.IsNullOrWhiteSpace(minigameId);

        public void Configure(string gameId, string routeQuestId, string routeObjectiveId, string routePrompt)
        {
            minigameId = gameId;
            questId = routeQuestId;
            objectiveId = routeObjectiveId;
            prompt = routePrompt;
        }

        public void Interact(GameObject actor)
        {
            GameBootstrap.Instance?.Minigames?.Begin(minigameId, questId, objectiveId);
        }
    }
}
