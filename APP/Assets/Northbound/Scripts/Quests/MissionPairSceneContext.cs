using System.IO;
using Northbound.Core;
using Northbound.Dialogue;
using Northbound.Narrative;
using UnityEngine;

namespace Northbound.Quests
{
    public sealed class MissionPairSceneContext : MonoBehaviour
    {
        private const string TestSandboxSaveFileName = "northbound-testsandbox-save.json";

        [SerializeField] private DialogueView dialogueViewPrefab;

        public NarrativeStateStore NarrativeState { get; private set; }

        public SaveGameService SaveGame { get; private set; }

        public InputGate InputGate { get; private set; }

        public DialogueRunner Dialogue { get; private set; }

        private void Awake()
        {
            InputGate = FindSceneInputGate();
            if (InputGate == null)
            {
                InputGate = gameObject.AddComponent<InputGate>();
            }

            SaveGame = new SaveGameService(Path.Combine(Application.persistentDataPath, TestSandboxSaveFileName));
            NarrativeState = new NarrativeStateStore(SaveGame.LoadOrNew());
            Dialogue = new DialogueRunner(NarrativeState, InputGate);
            if (dialogueViewPrefab != null)
            {
                Instantiate(dialogueViewPrefab).Bind(Dialogue);
            }
        }

        private void OnDestroy()
        {
            Dialogue?.Stop();
        }

        private InputGate FindSceneInputGate()
        {
            foreach (var gate in FindObjectsByType<InputGate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (gate.gameObject.scene == gameObject.scene)
                {
                    return gate;
                }
            }

            return null;
        }
    }
}
