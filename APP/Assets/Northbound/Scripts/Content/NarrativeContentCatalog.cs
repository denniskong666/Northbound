using System;
using System.Collections.Generic;
using System.Linq;
using Northbound.Cinematics;
using Northbound.Dialogue;
using Northbound.Endings;
using Northbound.Quests;
using UnityEngine;

namespace Northbound.Content
{
    [CreateAssetMenu(menuName = "Northbound/Narrative Content Catalog")]
    public sealed class NarrativeContentCatalog : ScriptableObject
    {
        public QuestAsset[] quests = Array.Empty<QuestAsset>();
        public DialogueAsset[] dialogues = Array.Empty<DialogueAsset>();
        public CinematicAsset[] cinematics = Array.Empty<CinematicAsset>();
        public EndingAsset[] endings = Array.Empty<EndingAsset>();
        public string[] triggerIds = Array.Empty<string>();
        public string[] characterIds = Array.Empty<string>();
        public GameObject[] characterPrefabs = Array.Empty<GameObject>();

        public QuestAsset Quest(string id) => quests.FirstOrDefault(asset => asset != null && asset.id == id);
        public DialogueAsset Dialogue(string id) => dialogues.FirstOrDefault(asset => asset != null && asset.id == id);
        public CinematicAsset Cinematic(string id) => cinematics.FirstOrDefault(asset => asset != null && asset.id == id);
        public EndingAsset Ending(string id) => endings.FirstOrDefault(asset => asset != null && asset.Id == id);
        public bool HasTrigger(string id) => triggerIds != null && triggerIds.Contains(id);
        public bool HasCharacter(string id) => characterIds != null && characterIds.Contains(id);
        public GameObject CharacterPrefab(string id)
        {
            return characterPrefabs.FirstOrDefault(prefab => prefab != null && string.Equals(prefab.name, id, StringComparison.OrdinalIgnoreCase));
        }
    }

    public interface IContentAssetSource
    {
        bool HasQuest(string id);
        bool HasDialogue(string id);
        bool HasCinematic(string id);
        bool HasEnding(string id);
        bool HasTrigger(string id);
    }

    public sealed class EmptyContentAssetSource : IContentAssetSource
    {
        public bool HasQuest(string id) => false;
        public bool HasDialogue(string id) => false;
        public bool HasCinematic(string id) => false;
        public bool HasEnding(string id) => false;
        public bool HasTrigger(string id) => false;
    }

    public sealed class ResourceContentAssetSource : IContentAssetSource
    {
        private readonly NarrativeContentCatalog catalog;

        public ResourceContentAssetSource()
        {
            catalog = Resources.Load<NarrativeContentCatalog>("Northbound/NarrativeContentCatalog");
        }

        public bool HasQuest(string id) => catalog != null && catalog.Quest(id) != null;
        public bool HasDialogue(string id) => catalog != null && catalog.Dialogue(id) != null;
        public bool HasCinematic(string id) => catalog != null && catalog.Cinematic(id) != null;
        public bool HasEnding(string id) => catalog != null && catalog.Ending(id) != null;
        public bool HasTrigger(string id) => catalog != null && catalog.HasTrigger(id);
    }
}
