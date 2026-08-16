using System;
using System.Collections.Generic;
using Northbound.Core;
using Northbound.Narrative;
using Northbound.Quests;
using Northbound.UI;
using UnityEngine;

namespace Northbound.Minigames
{
    public sealed class MinigameService : MonoBehaviour
    {
        private readonly Dictionary<string, MinigameController> games = new Dictionary<string, MinigameController>();
        private InputGate inputGate;
        private NarrativeStateStore state;
        private SettingsModel settings;

        public QuestRunner Quests { get; private set; }

        public void Initialize(InputGate gate, NarrativeStateStore narrativeState, SettingsModel settingsModel, DinerShiftGame dinerShiftPrefab, WiringGame wiringGamePrefab, TrunkPackingGame trunkPackingPrefab)
        {
            inputGate = gate ?? throw new ArgumentNullException(nameof(gate));
            state = narrativeState ?? throw new ArgumentNullException(nameof(narrativeState));
            settings = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));
            Quests = new QuestRunner(state);
            Register(dinerShiftPrefab);
            Register(wiringGamePrefab);
            Register(trunkPackingPrefab);
        }

        public MinigameController GetGame(string id) => !string.IsNullOrWhiteSpace(id) && games.TryGetValue(id, out var game) ? game : null;

        public bool Begin(string id, string questId, string objectiveId)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(objectiveId) ||
                !games.TryGetValue(id, out var game) || game.IsRunning || Quests == null)
            {
                return false;
            }

            var quest = ScriptableObject.CreateInstance<QuestAsset>();
            quest.id = questId;
            quest.objectives.Add(new QuestObjective { id = objectiveId, requiredAmount = 1 });
            if (!Quests.StartQuest(quest))
            {
                Destroy(quest);
                return false;
            }

            game.Configure(inputGate, Quests, state, settings, objectiveId);
            game.Begin();
            return game.IsRunning || state.Has(QuestRunner.CompletionFact(questId));
        }

        /// <summary>Starts a minigame only for the narrative quest already accepted by the player.</summary>
        public bool BeginActive(string id, string questId, string objectiveId)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(objectiveId) ||
                !games.TryGetValue(id, out var game) || game.IsRunning || Quests == null || Quests.ActiveQuestId != questId)
            {
                return false;
            }

            game.Configure(inputGate, Quests, state, settings, objectiveId);
            game.Begin();
            return game.IsRunning || state.Has(QuestRunner.CompletionFact(questId));
        }

        public void ResetSession()
        {
            foreach (var game in games.Values)
            {
                game?.Cancel();
            }
            Quests = new QuestRunner(state);
        }

        private void Register(MinigameController prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("MinigameService requires all three minigame prefabs.", this);
                return;
            }

            var instance = Instantiate(prefab, transform);
            instance.name = prefab.name;
            instance.gameObject.SetActive(false);
            DontDestroyOnLoad(instance.gameObject);
            games[instance.Id] = instance;
        }
    }
}
