using Northbound.Narrative;
using UnityEngine;

namespace Northbound.World
{
    public sealed class WorldFactBinding : MonoBehaviour
    {
        [SerializeField] private GameObject target;
        [SerializeField] private string[] requiredFacts = new string[0];
        [SerializeField] private string[] forbiddenFacts = new string[0];

        public void Configure(GameObject bindingTarget, string[] required, string[] forbidden)
        {
            target = bindingTarget;
            requiredFacts = required ?? new string[0];
            forbiddenFacts = forbidden ?? new string[0];
        }

        public void Refresh(NarrativeState state)
        {
            if (target == null)
            {
                return;
            }

            target.SetActive(Matches(state));
        }

        private bool Matches(NarrativeState state)
        {
            if (state == null)
            {
                return false;
            }

            foreach (var fact in requiredFacts ?? new string[0])
            {
                if (!string.IsNullOrWhiteSpace(fact) && !state.Has(fact))
                {
                    return false;
                }
            }

            foreach (var fact in forbiddenFacts ?? new string[0])
            {
                if (!string.IsNullOrWhiteSpace(fact) && state.Has(fact))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
