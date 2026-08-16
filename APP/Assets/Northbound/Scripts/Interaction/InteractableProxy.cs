using UnityEngine;

namespace Northbound.Interaction
{
    public sealed class InteractableProxy : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "Inspect";
        [SerializeField] private bool canInteract = true;

        public string Prompt => prompt;

        public bool CanInteract => canInteract;

        public void Interact(GameObject actor)
        {
            Debug.Log($"{actor.name} interacted with {name}.", this);
        }
    }
}
