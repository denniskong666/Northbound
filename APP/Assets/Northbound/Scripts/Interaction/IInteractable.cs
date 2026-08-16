using UnityEngine;

namespace Northbound.Interaction
{
    public interface IInteractable
    {
        string Prompt { get; }

        bool CanInteract { get; }

        void Interact(GameObject actor);
    }
}
