using Northbound.Narrative;
using UnityEngine;

namespace Northbound.Interaction
{
    public enum CompletionVisualMode
    {
        Hide,
        ShowCompletedState
    }

    /// <summary>Keeps a physical objective's visible/collider state synchronized with narrative authority.</summary>
    public sealed class ObjectivePropFeedback : MonoBehaviour
    {
        [SerializeField] private string completedFact;
        [SerializeField] private CompletionVisualMode mode;
        private NarrativeStateStore state;

        public void Configure(string fact, CompletionVisualMode visualMode, NarrativeStateStore narrativeState)
        {
            Unbind();
            completedFact = fact;
            mode = visualMode;
            state = narrativeState;
            if (state != null) state.Changed += Refresh;
            Refresh();
        }

        public void PlaySuccessAndHide()
        {
            Refresh();
        }

        public void Refresh(NarrativeStateStore narrativeState)
        {
            if (!ReferenceEquals(state, narrativeState)) Configure(completedFact, mode, narrativeState);
            else Refresh();
        }

        private void OnEnable() => Refresh();

        private void OnDestroy() => Unbind();

        private void Refresh()
        {
            if (state == null || string.IsNullOrWhiteSpace(completedFact)) return;
            var complete = state.Has(completedFact);
            var objective = GetComponent<Northbound.Content.NarrativeObjectiveTrigger>();
            var available = objective == null || objective.CanInteract;
            foreach (var interactionCollider in GetComponents<Collider2D>()) interactionCollider.enabled = available && !complete;
            if (mode != CompletionVisualMode.Hide) return;
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true)) renderer.enabled = available && !complete;
        }

        private void Unbind()
        {
            if (state != null) state.Changed -= Refresh;
            state = null;
        }
    }
}
