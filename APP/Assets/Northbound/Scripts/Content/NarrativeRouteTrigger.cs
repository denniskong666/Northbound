using Northbound.Interaction;
using UnityEngine;

namespace Northbound.Content
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class NarrativeRouteTrigger : MonoBehaviour, IInteractable
    {
        [SerializeField] private string routeId;
        [SerializeField] private string prompt;
        private NarrativeContentDirector director;

        public string RouteId => routeId;
        public string Prompt => prompt;
        public bool HasResolvedContent => director != null && director.HasRoute(routeId);
        public bool CanInteract => HasResolvedContent && director.CanActivate(routeId);

        public void Configure(string id, string interactionPrompt, NarrativeContentDirector contentDirector)
        {
            routeId = id;
            prompt = interactionPrompt;
            director = contentDirector;
        }

        public void Interact(GameObject actor)
        {
            director?.Activate(routeId);
        }
    }
}
