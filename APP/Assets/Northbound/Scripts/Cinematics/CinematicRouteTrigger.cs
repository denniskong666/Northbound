using Northbound.Core;
using Northbound.Interaction;
using UnityEngine;

namespace Northbound.Cinematics
{
    public sealed class CinematicRouteTrigger : MonoBehaviour, IInteractable
    {
        [SerializeField] private string cinematicId;
        [SerializeField] private string prompt = "Watch memory";

        public string CinematicId => cinematicId;
        public string Prompt => prompt;
        public bool CanInteract => GameBootstrap.Instance != null && GameBootstrap.Instance.Cinematics != null &&
                                   GameBootstrap.Instance.CinematicCatalog != null &&
                                   GameBootstrap.Instance.CinematicCatalog.Find(cinematicId) != null;

        public void Configure(string id, string interactionPrompt)
        {
            cinematicId = id;
            prompt = interactionPrompt;
        }

        public void Interact(GameObject actor)
        {
            GameBootstrap.Instance?.PlayCinematic(cinematicId);
        }
    }
}
