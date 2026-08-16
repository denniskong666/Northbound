using System;
using System.Linq;
using Northbound.Dialogue;

namespace Northbound.Content
{
    /// <summary>Content planning estimate only; it is deliberately not a substitute for observed playtests.</summary>
    public static class NarrativeContentMetrics
    {
        public static int CountSpokenLines(NarrativeContentCatalog catalog) => catalog?.dialogues?.Sum(dialogue => dialogue != null ? dialogue.lines.Count : 0) ?? 0;

        public static float EstimatePlaythroughMinutes(NarrativeContentCatalog catalog, NarrativeContentManifest manifest)
        {
            var words = catalog?.dialogues?
                .Where(dialogue => dialogue != null)
                .SelectMany(dialogue => dialogue.lines ?? new System.Collections.Generic.List<DialogueLine>())
                .Sum(line => CountWords(line?.text)) ?? 0;
            // Self-paced game text is generally skimmed faster than read-aloud dialogue; this is a planning model only.
            var readingMinutes = words / 230f;
            var interactiveMinutes = (manifest?.quests?.Length ?? 0) * 1.2f;
            const float explorationAndInspectionMinutes = 8f;
            const float cinematicMinutes = 4.5f;
            return readingMinutes + interactiveMinutes + explorationAndInspectionMinutes + cinematicMinutes;
        }

        private static int CountWords(string text) => string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
