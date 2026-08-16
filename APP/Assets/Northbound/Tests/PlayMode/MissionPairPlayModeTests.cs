using System.Collections;
using Northbound.Narrative;
using Northbound.Quests;
using Northbound.Core;
using Northbound.Dialogue;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Northbound.Tests
{
    public sealed class MissionPairPlayModeTests
    {
        [UnityTest]
        public IEnumerator TestSandbox_ContainsSpatiallySeparatedEliasAndMayaMissionTriggers()
        {
            SceneManager.LoadScene("TestSandbox", LoadSceneMode.Single);
            yield return null;

            var elias = GameObject.Find("Elias Mission");
            var maya = GameObject.Find("Maya Mission");
            var context = Object.FindFirstObjectByType<MissionPairSceneContext>();
            var dialogueView = Object.FindFirstObjectByType<DialogueView>();

            Assert.That(elias, Is.Not.Null);
            Assert.That(maya, Is.Not.Null);
            Assert.That(context, Is.Not.Null);
            Assert.That(context.SaveGame, Is.Not.Null);
            Assert.That(context.SaveGame.SavePath, Does.EndWith("northbound-testsandbox-save.json"));
            Assert.That(dialogueView, Is.Not.Null);
            var gate = context.InputGate;
            Assert.That(Vector2.Distance(elias.transform.position, maya.transform.position), Is.GreaterThan(2f));
            var eliasTrigger = elias.GetComponent<MissionCommitmentTrigger>();
            var mayaTrigger = maya.GetComponent<MissionCommitmentTrigger>();
            Assert.That(eliasTrigger.QuestId, Is.EqualTo("alternator"));
            Assert.That(mayaTrigger.QuestId, Is.EqualTo("first_light"));
            Assert.That(eliasTrigger.CanInteract, Is.True);

            eliasTrigger.Interact(null);

            Assert.That(context.Dialogue.IsRunning, Is.True);
            Assert.That(context.Dialogue.Current.text, Is.EqualTo("This will take the rest of the evening."));
            Assert.That(gate.IsBlocked, Is.True);
            context.Dialogue.Choose(1);
            Assert.That(gate.IsBlocked, Is.False);
            Assert.That(mayaTrigger.CanInteract, Is.True);
        }

        [UnityTest]
        public IEnumerator TestSandbox_ConfirmedCommitmentPersistsAcrossReload()
        {
            SceneManager.LoadScene("TestSandbox", LoadSceneMode.Single);
            yield return null;

            var firstContext = Object.FindFirstObjectByType<MissionPairSceneContext>();
            firstContext.SaveGame.Delete();
            var elias = GameObject.Find("Elias Mission").GetComponent<MissionCommitmentTrigger>();
            elias.Interact(null);
            firstContext.Dialogue.Choose(0);

            Assert.That(firstContext.SaveGame.LoadOrNew().Has("mission_pair_alternator_first_light_committed_alternator"), Is.True);

            SceneManager.LoadScene("TestSandbox", LoadSceneMode.Single);
            yield return null;

            var reloadedContext = Object.FindFirstObjectByType<MissionPairSceneContext>();
            var maya = GameObject.Find("Maya Mission").GetComponent<MissionCommitmentTrigger>();
            try
            {
                Assert.That(reloadedContext.NarrativeState.Has("mission_pair_alternator_first_light_committed_alternator"), Is.True);
                Assert.That(maya.CanInteract, Is.False);
            }
            finally
            {
                reloadedContext.SaveGame.Delete();
            }
        }

        [UnityTest]
        public IEnumerator TestSandbox_UsesItsLocalInputGateWhenBootstrapPersists()
        {
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;
            yield return null;
            SceneManager.LoadScene("TestSandbox", LoadSceneMode.Single);
            yield return null;

            var context = Object.FindFirstObjectByType<MissionPairSceneContext>();
            var elias = GameObject.Find("Elias Mission").GetComponent<MissionCommitmentTrigger>();

            Assert.That(context.InputGate.gameObject.scene, Is.EqualTo(SceneManager.GetActiveScene()));
            elias.Interact(null);
            Assert.That(context.Dialogue.IsRunning, Is.True);
            Assert.That(context.InputGate.IsBlocked, Is.True);
            context.Dialogue.Choose(1);
        }

        [UnityTest]
        public IEnumerator BeginCommitment_CancelLeavesBothMissionsAvailable()
        {
            var state = new NarrativeStateStore();
            var pair = new MissionPairController("alternator", "first_light", state);

            pair.BeginCommitment("first_light");
            pair.CancelCommitment();
            yield return null;

            Assert.That(pair.CommittedQuestId, Is.Null);
            Assert.That(pair.IsAvailable("alternator"), Is.True);
            Assert.That(pair.IsAvailable("first_light"), Is.True);
        }

        [UnityTest]
        public IEnumerator BeginCommitment_UsesTheNeutralEveningMessageAndCommitsOnlyOnConfirm()
        {
            var state = new NarrativeStateStore();
            var pair = new MissionPairController("alternator", "first_light", state);

            Assert.That(pair.BeginCommitment("first_light"), Is.True);
            Assert.That(pair.PendingMessage, Is.EqualTo("This will take the rest of the evening."));
            Assert.That(pair.ConfirmCommitment(), Is.True);
            yield return null;

            Assert.That(pair.CommittedQuestId, Is.EqualTo("first_light"));
            Assert.That(pair.IsAvailable("alternator"), Is.False);
        }
    }
}
