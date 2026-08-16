using System;
using System.Collections.Generic;
using Northbound.Narrative;
using UnityEngine;

namespace Northbound.World
{
    public sealed class ChapterWorldController : MonoBehaviour
    {
        [SerializeField] private ChapterVariant[] chapterVariants = new ChapterVariant[0];
        [SerializeField] private WorldFactBinding[] factBindings = new WorldFactBinding[0];
        private readonly Dictionary<string, GameObject> namedWorldObjects = new Dictionary<string, GameObject>();
        private NarrativeStateStore boundNarrativeState;

        public string CurrentChapterId { get; private set; }
        public string CurrentSpawnPointId { get; private set; }
        public string CurrentAmbientSnapshotId { get; private set; }
        public IReadOnlyList<string> CurrentStartingQuestIds { get; private set; } = Array.Empty<string>();
        public IReadOnlyList<string> ChapterIds => Array.ConvertAll(chapterVariants ?? Array.Empty<ChapterVariant>(), variant => variant == null ? string.Empty : variant.id);

        public event Action<ChapterVariant> ChapterApplied;

        public void Configure(ChapterVariant[] variants, WorldFactBinding[] bindings = null)
        {
            chapterVariants = variants ?? Array.Empty<ChapterVariant>();
            factBindings = bindings ?? Array.Empty<WorldFactBinding>();
            RefreshFactBindings();
        }

        public void BindNarrativeState(NarrativeStateStore state)
        {
            if (boundNarrativeState == state)
            {
                return;
            }

            UnbindNarrativeState();
            boundNarrativeState = state;
            if (boundNarrativeState != null)
            {
                boundNarrativeState.Changed += RefreshFactBindings;
                RefreshFactBindings();
            }
        }

        public void UnbindNarrativeState()
        {
            if (boundNarrativeState != null)
            {
                boundNarrativeState.Changed -= RefreshFactBindings;
                boundNarrativeState = null;
            }
        }

        private void OnDestroy()
        {
            UnbindNarrativeState();
        }

        public void RegisterWorldObject(string objectId, GameObject worldObject)
        {
            if (!string.IsNullOrWhiteSpace(objectId) && worldObject != null)
            {
                namedWorldObjects[objectId] = worldObject;
            }
        }

        public bool Apply(string chapterId, NarrativeState state)
        {
            var selected = FindVariant(chapterId, state);
            if (selected == null)
            {
                return false;
            }

            ResetVariantControlledObjects();
            SetActive(selected.objectsToActivate, true);
            SetActive(selected.objectsToDeactivate, false);
            SetActiveByName(selected.objectIdsToActivate, true);
            SetActiveByName(selected.objectIdsToDeactivate, false);
            CurrentChapterId = selected.id;
            CurrentSpawnPointId = selected.spawnPointId ?? string.Empty;
            CurrentAmbientSnapshotId = selected.ambientSnapshotId ?? string.Empty;
            CurrentStartingQuestIds = selected.startingQuestIds ?? Array.Empty<string>();

            RefreshFactBindings(state);

            ChapterApplied?.Invoke(selected);
            return true;
        }

        public bool CanApply(string chapterId, NarrativeState state)
        {
            return FindVariant(chapterId, state) != null;
        }

        private ChapterVariant FindVariant(string chapterId, NarrativeState state)
        {
            if (string.IsNullOrWhiteSpace(chapterId))
            {
                return null;
            }

            foreach (var variant in chapterVariants ?? Array.Empty<ChapterVariant>())
            {
                if (variant != null && variant.id == chapterId && variant.Matches(state))
                {
                    return variant;
                }
            }

            return null;
        }

        private static void SetActive(GameObject[] objects, bool active)
        {
            foreach (var worldObject in objects ?? Array.Empty<GameObject>())
            {
                if (worldObject != null)
                {
                    worldObject.SetActive(active);
                }
            }
        }

        private void ResetVariantControlledObjects()
        {
            foreach (var variant in chapterVariants ?? Array.Empty<ChapterVariant>())
            {
                if (variant == null)
                {
                    continue;
                }

                SetActive(variant.objectsToActivate, false);
                SetActive(variant.objectsToDeactivate, false);
                SetActiveByName(variant.objectIdsToActivate, false);
                SetActiveByName(variant.objectIdsToDeactivate, false);
            }
        }

        private void RefreshFactBindings()
        {
            RefreshFactBindings(boundNarrativeState?.State);
        }

        private void RefreshFactBindings(NarrativeState state)
        {
            foreach (var binding in factBindings ?? Array.Empty<WorldFactBinding>())
            {
                if (binding != null)
                {
                    binding.Refresh(state);
                }
            }
        }

        private void SetActiveByName(string[] objectIds, bool active)
        {
            foreach (var objectId in objectIds ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(objectId))
                {
                    continue;
                }

                if (!namedWorldObjects.TryGetValue(objectId, out var worldObject))
                {
                    worldObject = GameObject.Find(objectId);
                }
                if (worldObject != null)
                {
                    worldObject.SetActive(active);
                }
            }
        }
    }
}
