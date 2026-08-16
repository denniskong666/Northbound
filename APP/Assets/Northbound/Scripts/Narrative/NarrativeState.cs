using System;
using System.Collections.Generic;
using UnityEngine;

namespace Northbound.Narrative
{
    [Serializable]
    public sealed class IntEntry
    {
        public string Id;
        public int Value;

        public IntEntry(string id, int value)
        {
            Id = id;
            Value = value;
        }
    }

    [Serializable]
    public sealed class NarrativeState
    {
        [SerializeField] private List<string> facts = new List<string>();
        [SerializeField] private List<IntEntry> counters = new List<IntEntry>();

        public bool Has(string id)
        {
            return !string.IsNullOrEmpty(id) && facts.Contains(id);
        }

        public void Set(string id, bool value)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (value && !facts.Contains(id))
            {
                facts.Add(id);
            }

            if (!value)
            {
                facts.RemoveAll(fact => fact == id);
            }
        }

        public int GetInt(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return 0;
            }

            var entry = counters.Find(counter => counter != null && counter.Id == id);
            return entry != null ? entry.Value : 0;
        }

        public void Add(string id, int amount)
        {
            if (string.IsNullOrEmpty(id) || amount == 0)
            {
                return;
            }

            var entry = counters.Find(counter => counter != null && counter.Id == id);
            if (entry == null)
            {
                counters.Add(new IntEntry(id, amount));
                return;
            }

            entry.Value += amount;
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static NarrativeState FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new NarrativeState();
            }

            try
            {
                var state = JsonUtility.FromJson<NarrativeState>(json);
                if (state == null)
                {
                    return new NarrativeState();
                }

                state.facts = state.facts ?? new List<string>();
                state.counters = state.counters ?? new List<IntEntry>();
                return state;
            }
            catch (ArgumentException)
            {
                return new NarrativeState();
            }
        }
    }
}
