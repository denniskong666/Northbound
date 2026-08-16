using Northbound.Interaction;
using UnityEngine;

namespace Northbound.Content
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class NarrativeCharacterInteractor : MonoBehaviour, IInteractable
    {
        [SerializeField] private string characterId;
        [SerializeField] private string conversationRouteId;
        private NarrativeContentDirector director;

        public string CharacterId => characterId;
        public string Prompt => $"Talk to {characterId}";
        public bool CanInteract => director != null && director.CanActivate(conversationRouteId);

        public void Configure(string id, string routeId, NarrativeContentDirector contentDirector = null)
        {
            characterId = id;
            conversationRouteId = routeId;
            director = contentDirector;
        }

        private void Awake()
        {
            if (director == null) director = FindFirstObjectByType<NarrativeContentDirector>();
        }

        public void Interact(GameObject actor)
        {
            director?.Activate(conversationRouteId);
        }
    }
}
