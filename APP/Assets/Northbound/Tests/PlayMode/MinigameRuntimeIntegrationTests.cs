using System.Collections;
using Northbound.Core;
using Northbound.Content;
using Northbound.Minigames;
using Northbound.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Northbound.Tests
{
    public sealed class MinigameRuntimeIntegrationTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (GameBootstrap.Instance != null)
            {
                Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (GameBootstrap.Instance != null)
            {
                Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Bootstrap_CreatesConfiguredMinigameServiceAndGreybridgeRoutes()
        {
            SceneManager.LoadScene(SceneIds.Bootstrap, LoadSceneMode.Single);
            yield return null;
            yield return null;
            if (SceneManager.GetActiveScene().name != SceneIds.Greybridge)
            {
                SceneManager.LoadScene(SceneIds.Greybridge, LoadSceneMode.Single);
                yield return null;
            }
            yield return WaitForGreybridge();

            var bootstrap = GameBootstrap.Instance;
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.Minigames, Is.Not.Null);
            Assert.That(bootstrap.Minigames.GetGame("diner_shift"), Is.TypeOf<DinerShiftGame>());
            Assert.That(bootstrap.Minigames.GetGame("wiring_game"), Is.TypeOf<WiringGame>());
            Assert.That(bootstrap.Minigames.GetGame("trunk_packing"), Is.TypeOf<TrunkPackingGame>());

            Assert.That(Object.FindFirstObjectByType<MinigameMissionTrigger>(), Is.Null,
                "Legacy minigame route triggers must not bypass dialogue acceptance and quest objective gating.");
            var dinerObjective = System.Array.Find(
                Object.FindObjectsByType<NarrativeObjectiveTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                trigger => trigger.QuestId == "clock_in");
            Assert.That(dinerObjective, Is.Not.Null);
            Assert.That(dinerObjective.CanInteract, Is.False,
                "A physical minigame objective remains unavailable until its authored quest dialogue has been accepted.");
        }

        private static IEnumerator WaitForGreybridge()
        {
            for (var frame = 0; frame < 10 && SceneManager.GetActiveScene().name != SceneIds.Greybridge; frame++)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneIds.Greybridge));
        }

        [UnityTest]
        public IEnumerator Service_RejectsMissingQuestBindingWithoutFalselyCompleting()
        {
            SceneManager.LoadScene(SceneIds.Bootstrap, LoadSceneMode.Single);
            yield return null;
            yield return null;

            var service = GameBootstrap.Instance.Minigames;
            var game = service.GetGame("diner_shift");
            var completions = 0;
            game.Completed += _ => completions++;

            Assert.That(service.Begin("diner_shift", string.Empty, string.Empty), Is.False);
            Assert.That(game.IsRunning, Is.False);
            Assert.That(completions, Is.Zero);
        }
    }
}
