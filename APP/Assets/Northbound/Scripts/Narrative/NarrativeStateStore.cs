using System;

namespace Northbound.Narrative
{
    public sealed class NarrativeStateStore
    {
        private NarrativeState state;

        public NarrativeState State => state;

        public event Action Changed;

        public NarrativeStateStore(NarrativeState initialState = null)
        {
            state = initialState ?? new NarrativeState();
        }

        public bool Has(string id)
        {
            return state.Has(id);
        }

        public void Set(string id, bool value)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (state.Has(id) == value)
            {
                return;
            }

            state.Set(id, value);
            Changed?.Invoke();
        }

        public void Add(string id, int amount)
        {
            if (amount == 0 || string.IsNullOrEmpty(id))
            {
                return;
            }

            state.Add(id, amount);
            Changed?.Invoke();
        }

        public int GetInt(string id)
        {
            return state.GetInt(id);
        }

        public void Reset()
        {
            state = new NarrativeState();
            Changed?.Invoke();
        }

        public void Replace(NarrativeState replacement)
        {
            state = replacement ?? new NarrativeState();
            Changed?.Invoke();
        }
    }
}
