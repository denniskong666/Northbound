using System;
using Northbound.Core;
using Northbound.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Northbound.Interaction
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        private const int MaxCandidates = 16;

        [SerializeField, Min(0f)] private float interactionRange = 1.25f;
        [SerializeField] private InputGate inputGate;
        [SerializeField] private InteractionPromptView promptView;

        private Collider2D[] overlapResults = new Collider2D[MaxCandidates];
        private InputAction interactAction;

        public IInteractable CurrentInteractable { get; private set; }

        private void Awake()
        {
            interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            interactAction.AddBinding("<Keyboard>/enter");
            interactAction.AddBinding("<Keyboard>/space");
        }

        private void OnEnable()
        {
            interactAction?.Enable();
        }

        private void OnDisable()
        {
            interactAction?.Disable();
        }

        private void OnDestroy()
        {
            interactAction?.Dispose();
        }

        private void Update()
        {
            RefreshTarget();

            if (interactAction.WasPressedThisFrame())
            {
                TryInteract();
            }
        }

        public void SetInputGate(InputGate gate)
        {
            inputGate = gate;
        }

        public void SetPromptView(InteractionPromptView value)
        {
            promptView = value;
            UpdatePrompt();
        }

        public void SetInteractionRange(float value)
        {
            interactionRange = Mathf.Max(0f, value);
        }

        public void RefreshTarget()
        {
            CurrentInteractable = FindClosestInteractable();
            UpdatePrompt();
        }

        public void TryInteract()
        {
            if (inputGate != null && inputGate.IsBlocked)
            {
                return;
            }

            RefreshTarget();
            var target = CurrentInteractable;
            if (target == null) return;
            target.Interact(gameObject);
            GameBootstrap.Instance?.NarrativeState?.Set("tutorial_interacted", true);
        }

        private IInteractable FindClosestInteractable()
        {
            var hitCount = GetOverlapCount();
            var closestDistanceSquared = float.PositiveInfinity;
            IInteractable closest = null;
            var closestIsDoor = false;

            for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                var collider = overlapResults[hitIndex];
                foreach (var component in collider.GetComponents<MonoBehaviour>())
                {
                    if (!(component is IInteractable interactable) || !component.isActiveAndEnabled || !interactable.CanInteract)
                    {
                        continue;
                    }

                    var distanceSquared = ((Vector2)component.transform.position - (Vector2)transform.position).sqrMagnitude;
                    var candidateIsDoor = component is DoorInteractor;
                    if ((!candidateIsDoor && closestIsDoor) ||
                        (candidateIsDoor == closestIsDoor && distanceSquared >= closestDistanceSquared))
                    {
                        continue;
                    }

                    closestDistanceSquared = distanceSquared;
                    closest = interactable;
                    closestIsDoor = candidateIsDoor;
                }
            }

            return closest;
        }

        private int GetOverlapCount()
        {
            while (true)
            {
                var hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, interactionRange, overlapResults);
                if (hitCount < overlapResults.Length)
                {
                    return hitCount;
                }

                Array.Resize(ref overlapResults, overlapResults.Length * 2);
            }
        }

        private void UpdatePrompt()
        {
            if (promptView != null)
            {
                promptView.SetPrompt(CurrentInteractable?.Prompt);
            }
        }
    }
}
