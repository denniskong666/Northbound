using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Northbound.Content
{
    [Serializable]
    public sealed class ContentChapter
    {
        public string id;
        public string nextId;
        public string[] requiredQuestIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ContentQuest
    {
        public string id;
        public string chapterId;
        public string pairId;
        public string dialogueId;
        public string triggerId;
        public string[] prerequisiteQuestIds = Array.Empty<string>();
        public string[] prerequisiteFacts = Array.Empty<string>();
        /// <summary>"physical" requires an authored interaction; "dialogue" is explicitly dialogue-only.</summary>
        public string completionMode;
        public string minigameId;
        public string[] completionFacts = Array.Empty<string>();
        public string[] nextQuestIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ContentDialogue
    {
        public string id;
        public string kind;
    }

    [Serializable]
    public sealed class ContentTrigger
    {
        public string id;
        public string routeType;
        public string targetId;
        public string chapterId;
        public string[] prerequisiteFacts = Array.Empty<string>();
        public string phase;
    }

    [Serializable]
    public sealed class ContentCinematic
    {
        public string id;
        public string completionFact;
        public string dialogueId;
        public string[] subtitleCues = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ContentFact
    {
        public string id;
    }

    [Serializable]
    public sealed class ContentEnding
    {
        public string id;
        public string[] dialogueIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ContentCharacter
    {
        public string id;
        public string prefabId;
    }

    [Serializable]
    public sealed class NarrativeContentManifest
    {
        public ContentChapter[] chapters = Array.Empty<ContentChapter>();
        public ContentQuest[] quests = Array.Empty<ContentQuest>();
        public ContentDialogue[] dialogues = Array.Empty<ContentDialogue>();
        public ContentTrigger[] triggers = Array.Empty<ContentTrigger>();
        public ContentCinematic[] cinematics = Array.Empty<ContentCinematic>();
        public ContentFact[] facts = Array.Empty<ContentFact>();
        public ContentEnding[] endings = Array.Empty<ContentEnding>();
        public ContentCharacter[] characters = Array.Empty<ContentCharacter>();

        public static NarrativeContentManifest FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new NarrativeContentManifest();
            }

            var result = JsonUtility.FromJson<NarrativeContentManifest>(json) ?? new NarrativeContentManifest();
            result.chapters ??= Array.Empty<ContentChapter>();
            result.quests ??= Array.Empty<ContentQuest>();
            result.dialogues ??= Array.Empty<ContentDialogue>();
            result.triggers ??= Array.Empty<ContentTrigger>();
            result.cinematics ??= Array.Empty<ContentCinematic>();
            result.facts ??= Array.Empty<ContentFact>();
            result.endings ??= Array.Empty<ContentEnding>();
            result.characters ??= Array.Empty<ContentCharacter>();
            return result;
        }

        public static NarrativeContentManifest LoadApproved()
        {
            var manifest = Resources.Load<TextAsset>("Northbound/content-manifest");
            if (manifest == null)
            {
                throw new InvalidOperationException("Northbound content manifest is not present in Resources.");
            }

            return FromJson(manifest.text);
        }

        public IReadOnlyList<string> ChapterOrder()
        {
            var result = new List<string>();
            var byId = chapters.Where(chapter => chapter != null && !string.IsNullOrWhiteSpace(chapter.id))
                .ToDictionary(chapter => chapter.id, StringComparer.Ordinal);
            var current = byId.ContainsKey("prologue") ? "prologue" : null;
            while (current != null && byId.TryGetValue(current, out var chapter) && !result.Contains(current))
            {
                result.Add(current);
                current = string.IsNullOrWhiteSpace(chapter.nextId) ? null : chapter.nextId;
            }

            return result;
        }

        public ContentQuest FindQuest(string id) => quests.FirstOrDefault(quest => quest != null && quest.id == id);
        public ContentTrigger FindTrigger(string id) => triggers.FirstOrDefault(trigger => trigger != null && trigger.id == id);
    }
}
