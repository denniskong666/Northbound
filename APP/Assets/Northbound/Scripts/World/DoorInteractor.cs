using Northbound.Interaction;
using UnityEngine;
using Northbound.UI;

namespace Northbound.World
{
    public sealed class DoorInteractor : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "[E] Enter";
        [SerializeField] private string destinationId;
        private LocationTransitionController controller;

        public string Prompt => GameText.Prompt(prompt);
        public string DestinationId => destinationId;
        public bool CanInteract => controller != null && controller.CanTravel(destinationId);

        public void Configure(string interactionPrompt, string destination, LocationTransitionController transitionController)
        {
            prompt = interactionPrompt; destinationId = destination; controller = transitionController;
            if (GetComponent<Collider2D>() == null)
            {
                var doorCollider = gameObject.AddComponent<BoxCollider2D>();
                doorCollider.isTrigger = true;
            }
        }

        public void Interact(GameObject actor) => controller?.StartTravel(destinationId);
    }
}
