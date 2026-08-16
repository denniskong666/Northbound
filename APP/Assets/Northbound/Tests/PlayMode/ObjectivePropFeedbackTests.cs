using System.Collections;
using Northbound.Interaction;
using Northbound.Narrative;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Northbound.Tests
{
    public sealed class ObjectivePropFeedbackTests
    {
        [UnityTest]
        public IEnumerator CompletedFact_HidesPropAndDisablesInteractionCollider()
        {
            var state = new NarrativeStateStore();
            var root = CreateProp(out var collider, out var visual);
            var feedback = root.AddComponent<ObjectivePropFeedback>();
            feedback.Configure("quest_socket_objective_find_complete", CompletionVisualMode.Hide, state);

            state.Set("quest_socket_objective_find_complete", true);
            yield return null;

            Assert.That(collider.enabled, Is.False);
            Assert.That(visual.enabled, Is.False);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ExistingCompletedFact_StartsHiddenAfterReload()
        {
            var state = new NarrativeStateStore();
            state.Set("quest_socket_objective_find_complete", true);
            var root = CreateProp(out var collider, out var visual);
            var feedback = root.AddComponent<ObjectivePropFeedback>();

            feedback.Configure("quest_socket_objective_find_complete", CompletionVisualMode.Hide, state);
            yield return null;

            Assert.That(collider.enabled, Is.False);
            Assert.That(visual.enabled, Is.False);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ReplacingWithIncompleteState_RestoresPropAndCollider()
        {
            var state = new NarrativeStateStore();
            state.Set("quest_socket_objective_find_complete", true);
            var root = CreateProp(out var collider, out var visual);
            var feedback = root.AddComponent<ObjectivePropFeedback>();
            feedback.Configure("quest_socket_objective_find_complete", CompletionVisualMode.Hide, state);

            state.Replace(new NarrativeState());
            yield return null;

            Assert.That(collider.enabled, Is.True);
            Assert.That(visual.enabled, Is.True);
            Object.Destroy(root);
        }

        private static GameObject CreateProp(out Collider2D collider, out SpriteRenderer visual)
        {
            var root = new GameObject("Objective Prop");
            collider = root.AddComponent<CircleCollider2D>();
            var child = new GameObject("Quest Object Visual");
            child.transform.SetParent(root.transform, false);
            visual = child.AddComponent<SpriteRenderer>();
            return root;
        }
    }
}
