using System;
using Northbound.Narrative;

namespace Northbound.Content
{
    public static class CharacterHighlightSelector
    {
        private static readonly string[] Friends = { "maya", "noah", "leo" };

        public static string SelectId(NarrativeState state)
        {
            state ??= new NarrativeState();
            string selected = null;
            var bestBond = int.MinValue;
            var bestOrder = int.MaxValue;
            foreach (var friend in Friends)
            {
                if (!state.Has($"helped_{friend}")) continue;
                var bond = state.GetInt($"bond_{friend}");
                var order = CompletionOrder(state, friend);
                if (selected == null || bond > bestBond || bond == bestBond && order < bestOrder)
                {
                    selected = friend;
                    bestBond = bond;
                    bestOrder = order;
                }
            }

            return selected;
        }

        private static int CompletionOrder(NarrativeState state, string friend)
        {
            for (var index = 1; index <= Friends.Length; index++)
            {
                if (state.Has($"friend_{friend}_completion_order_{index}")) return index;
            }

            return int.MaxValue;
        }
    }
}
