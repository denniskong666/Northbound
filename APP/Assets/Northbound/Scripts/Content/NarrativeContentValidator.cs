using System;
using System.Collections.Generic;
using System.Linq;
using Northbound.Dialogue;
using Northbound.Endings;
using Northbound.Quests;

namespace Northbound.Content
{
    public static class NarrativeContentValidator
    {
        public static IReadOnlyList<string> Validate(NarrativeContentManifest manifest, IContentAssetSource assets)
        {
            var errors = new List<string>();
            manifest ??= new NarrativeContentManifest();
            assets ??= new EmptyContentAssetSource();
            ValidateUnique(manifest.chapters, chapter => chapter?.id, "chapter", errors);
            ValidateUnique(manifest.quests, quest => quest?.id, "quest", errors);
            ValidateUnique(manifest.dialogues, dialogue => dialogue?.id, "dialogue", errors);
            ValidateUnique(manifest.triggers, trigger => trigger?.id, "trigger", errors);
            ValidateUnique(manifest.cinematics, cinematic => cinematic?.id, "cinematic", errors);
            ValidateUnique(manifest.facts, fact => fact?.id, "fact", errors);
            ValidateUnique(manifest.endings, ending => ending?.id, "ending", errors);
            ValidateUnique(manifest.characters, character => character?.id, "character", errors);

            var dialogueIds = Ids(manifest.dialogues, dialogue => dialogue?.id);
            var triggerIds = Ids(manifest.triggers, trigger => trigger?.id);
            var questIds = Ids(manifest.quests, quest => quest?.id);
            var cinematicIds = Ids(manifest.cinematics, cinematic => cinematic?.id);
            var chapterIds = Ids(manifest.chapters, chapter => chapter?.id);
            var factIds = Ids(manifest.facts, fact => fact?.id);
            foreach (var quest in manifest.quests.Where(quest => quest != null))
            {
                if (string.IsNullOrWhiteSpace(quest.chapterId) || string.IsNullOrWhiteSpace(quest.completionMode) ||
                    (quest.completionMode != "physical" && quest.completionMode != "dialogue"))
                    errors.Add($"quest '{quest.id}' has missing activation metadata");
                Require(chapterIds.Contains(quest.chapterId), $"quest '{quest.id}' references missing chapter '{quest.chapterId}'", errors);
                foreach (var prerequisite in quest.prerequisiteQuestIds ?? Array.Empty<string>())
                    Require(questIds.Contains(prerequisite), $"quest '{quest.id}' references missing prerequisite quest '{prerequisite}'", errors);
                foreach (var fact in quest.prerequisiteFacts ?? Array.Empty<string>())
                    Require(factIds.Contains(fact), $"quest '{quest.id}' references missing prerequisite fact '{fact}'", errors);
                foreach (var fact in quest.completionFacts ?? Array.Empty<string>())
                    Require(factIds.Contains(fact), $"quest '{quest.id}' references missing completion fact '{fact}'", errors);
                Require(assets.HasQuest(quest.id), $"missing quest asset '{quest.id}'", errors);
                Require(dialogueIds.Contains(quest.dialogueId), $"quest '{quest.id}' references missing dialogue '{quest.dialogueId}'", errors);
                Require(assets.HasDialogue(quest.dialogueId), $"missing dialogue asset '{quest.dialogueId}'", errors);
                Require(triggerIds.Contains(quest.triggerId), $"quest '{quest.id}' references missing trigger '{quest.triggerId}'", errors);
                Require(assets.HasTrigger(quest.triggerId), $"missing trigger '{quest.triggerId}'", errors);
                foreach (var next in quest.nextQuestIds ?? Array.Empty<string>())
                {
                    Require(questIds.Contains(next), $"quest '{quest.id}' references missing next quest '{next}'", errors);
                }
            }

            foreach (var trigger in manifest.triggers.Where(trigger => trigger != null))
            {
                Require(chapterIds.Contains(trigger.chapterId), $"trigger '{trigger.id}' references missing chapter '{trigger.chapterId}'", errors);
                foreach (var fact in trigger.prerequisiteFacts ?? Array.Empty<string>())
                    Require(factIds.Contains(fact), $"trigger '{trigger.id}' references missing prerequisite fact '{fact}'", errors);
                if (trigger.routeType == "quest") Require(questIds.Contains(trigger.targetId), $"trigger '{trigger.id}' targets missing quest '{trigger.targetId}'", errors);
                if (trigger.routeType == "dialogue") Require(dialogueIds.Contains(trigger.targetId), $"trigger '{trigger.id}' targets missing dialogue '{trigger.targetId}'", errors);
                if (trigger.routeType == "cinematic") Require(cinematicIds.Contains(trigger.targetId), $"trigger '{trigger.id}' targets missing cinematic '{trigger.targetId}'", errors);
            }

            foreach (var cinematic in manifest.cinematics.Where(cinematic => cinematic != null))
            {
                Require(assets.HasCinematic(cinematic.id), $"missing cinematic asset '{cinematic.id}'", errors);
                Require(dialogueIds.Contains(cinematic.dialogueId), $"cinematic '{cinematic.id}' references missing dialogue '{cinematic.dialogueId}'", errors);
                Require(factIds.Contains(cinematic.completionFact), $"cinematic '{cinematic.id}' references missing cinematic completion fact '{cinematic.completionFact}'", errors);
                Require((cinematic.subtitleCues ?? Array.Empty<string>()).Length > 0, $"cinematic '{cinematic.id}' has no subtitle cues", errors);
                var previous = -1d;
                foreach (var cue in cinematic.subtitleCues ?? Array.Empty<string>())
                {
                    var stamp = cue?.Split(' ').FirstOrDefault();
                    if (!TimeSpan.TryParse("00:" + stamp, out var parsed) || parsed.TotalSeconds <= previous)
                    {
                        errors.Add($"cinematic '{cinematic.id}' subtitle cues are not ordered");
                        break;
                    }
                    previous = parsed.TotalSeconds;
                }
            }
            foreach (var ending in manifest.endings.Where(ending => ending != null))
            {
                Require(assets.HasEnding(ending.id), $"missing ending asset '{ending.id}'", errors);
                foreach (var dialogueId in ending.dialogueIds ?? Array.Empty<string>())
                    Require(dialogueIds.Contains(dialogueId), $"ending '{ending.id}' references missing dialogue '{dialogueId}'", errors);
                if ((ending.dialogueIds ?? Array.Empty<string>()).Length == 0)
                    errors.Add($"ending '{ending.id}' references missing dialogue");
            }
            ValidateChapterGraph(manifest, errors);
            ValidateQuestReachability(manifest, errors);
            if (manifest.characters.Length != 5) errors.Add("Northbound requires the five primary characters, including Jamie.");
            return errors;
        }

        public static IReadOnlyList<string> ValidateRuntimeAssets(NarrativeContentManifest manifest, NarrativeContentCatalog catalog)
        {
            var errors = new List<string>();
            var facts = Ids(manifest.facts, fact => fact?.id);
            foreach (var cinematic in manifest.cinematics ?? Array.Empty<ContentCinematic>())
            {
                var asset = catalog?.Cinematic(cinematic.id); var dialogue = catalog?.Dialogue(cinematic.dialogueId);
                if (asset == null || dialogue == null || !asset.subtitleCues.Select(c => c.text).SequenceEqual(dialogue.lines.Select(line => line.text))) errors.Add($"cinematic '{cinematic?.id}' cue parity failed");
            }
            foreach (var variant in EndingDialogueMap.SupportedVariantIds)
                if (catalog?.Dialogue(EndingDialogueMap.DialogueId(variant)) == null) errors.Add($"ending variant '{variant}' has no dialogue mapping");
            foreach (var dialogue in catalog?.dialogues ?? Array.Empty<DialogueAsset>()) foreach (var choice in dialogue.lines.SelectMany(line => line.choices ?? new List<DialogueChoice>()))
                if (!string.IsNullOrWhiteSpace(choice.grantedFact) && !facts.Contains(choice.grantedFact) && !IsChoiceFact(dialogue.id, choice.grantedFact)) errors.Add($"choice fact '{choice.grantedFact}' is not authoritative");
            foreach (var quest in catalog?.quests ?? Array.Empty<QuestAsset>()) foreach (var objective in quest.objectives ?? new List<QuestObjective>())
            {
                foreach (var fact in new[] { QuestRunner.StartedFactId(quest.id), QuestRunner.ObjectiveProgressFactId(quest.id, objective.id), QuestRunner.ObjectiveCompletionFactId(quest.id, objective.id), QuestRunner.CompletionFact(quest.id) })
                    if (!IsQuestRuntimeFact(quest.id, objective.id, fact)) errors.Add($"quest runtime fact '{fact}' is not authoritative");
            }
            return errors;
        }

        private static bool IsChoiceFact(string dialogueId, string fact) => fact.StartsWith(dialogueId + "_", StringComparison.Ordinal) && (fact.EndsWith("_committed", StringComparison.Ordinal) || fact.EndsWith("_curious", StringComparison.Ordinal) || fact.EndsWith("_uncertain", StringComparison.Ordinal) || fact.EndsWith("_silent", StringComparison.Ordinal));
        private static bool IsQuestRuntimeFact(string questId, string objectiveId, string fact) => fact == QuestRunner.StartedFactId(questId) || fact == QuestRunner.ObjectiveProgressFactId(questId, objectiveId) || fact == QuestRunner.ObjectiveCompletionFactId(questId, objectiveId) || fact == QuestRunner.CompletionFact(questId);
        public static bool IsAuthorizedQuestRuntimeFact(string questId, string objectiveId, string fact) => IsQuestRuntimeFact(questId, objectiveId, fact);

        private static void ValidateQuestReachability(NarrativeContentManifest manifest, ICollection<string> errors)
        {
            var byId = manifest.quests.Where(quest => quest != null && !string.IsNullOrWhiteSpace(quest.id))
                .GroupBy(quest => quest.id, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            foreach (var quest in byId.Values)
            {
                foreach (var prerequisite in quest.prerequisiteQuestIds ?? Array.Empty<string>())
                {
                    if (prerequisite == quest.id || (byId.TryGetValue(prerequisite, out var previous) &&
                        (previous.prerequisiteQuestIds ?? Array.Empty<string>()).Contains(quest.id)))
                        errors.Add($"quest graph has a cycle involving '{quest.id}'");
                }
            }
        }

        private static void ValidateChapterGraph(NarrativeContentManifest manifest, ICollection<string> errors)
        {
            var order = manifest.ChapterOrder();
            if (order.Count != manifest.chapters.Length) errors.Add("Chapter graph is disconnected or cyclic.");
            if (!order.Contains("finale")) errors.Add("Finale is unreachable from prologue.");
        }

        private static HashSet<string> Ids<T>(IEnumerable<T> source, Func<T, string> id) => new HashSet<string>((source ?? Array.Empty<T>()).Select(id).Where(value => !string.IsNullOrWhiteSpace(value)), StringComparer.Ordinal);
        private static void Require(bool condition, string message, ICollection<string> errors) { if (!condition) errors.Add(message); }
        private static void ValidateUnique<T>(IEnumerable<T> source, Func<T, string> id, string label, ICollection<string> errors)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in source ?? Array.Empty<T>())
            {
                var value = id(item);
                if (string.IsNullOrWhiteSpace(value)) errors.Add($"{label} has a missing id");
                else if (!seen.Add(value)) errors.Add($"Duplicate {label} id '{value}'");
            }
        }
    }
}
