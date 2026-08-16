using System.Collections;
using System.IO;
using System.Linq;
using Guid = System.Guid;
using Northbound.Core;
using Northbound.Narrative;
using Northbound.Player;
using Northbound.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Northbound.Tests
{
    public sealed class GreybridgeWorldPlayModeTests
    {
        [TearDown]
        public void RestoreSessionSaveConfiguration()
        {
            GameBootstrap.SessionSaveGameFactory = null;
        }

        [UnityTest]
        public IEnumerator Greybridge_ContainsTheThreeConnectedLocationsAndChapterStateGeometry()
        {
            if (GameBootstrap.Instance != null)
            {
                Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;

            var layout = Object.FindFirstObjectByType<GreybridgeWorldLayout>();
            var controller = Object.FindFirstObjectByType<ChapterWorldController>();
            Assert.That(layout, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(GameObject.Find("Old Neighborhood"), Is.Not.Null);
            Assert.That(GameObject.Find("Vale Auto Garage"), Is.Not.Null);
            Assert.That(GameObject.Find("Rooftop Overlook"), Is.Not.Null);
            Assert.That(Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(12));
            Assert.That(GameObject.Find("Spawn Chapter 1"), Is.Not.Null);
            Assert.That(GameObject.Find("Spawn Chapter 2"), Is.Not.Null);
            Assert.That(GameObject.Find("Spawn Chapter 4"), Is.Not.Null);
            Assert.That(GameObject.Find("Spawn Finale"), Is.Not.Null);
            Assert.That(GameObject.Find("Mission Zone Garage"), Is.Not.Null);
            Assert.That(GameObject.Find("Mission Zone Rooftop"), Is.Not.Null);

            Assert.That(controller.Apply("chapter_1", new NarrativeState()), Is.True);
            Assert.That(GameObject.Find("Open Diner").activeSelf, Is.True);
            Assert.That(GameObject.Find("Open Market").activeSelf, Is.True);
            Assert.That(controller.Apply("chapter_2", new NarrativeState()), Is.True);
            Assert.That(GameObject.Find("FINAL WEEK").activeSelf, Is.True);
            Assert.That(controller.Apply("chapter_4", new NarrativeState()), Is.True);
            Assert.That(GameObject.Find("Dark Storefronts").activeSelf, Is.True);
            var finaleState = new NarrativeState();
            finaleState.Set(FinaleGatheringInteractor.ReviewedFact, true);
            Assert.That(controller.Apply("finale", finaleState), Is.True);
            Assert.That(GameObject.Find("Finale Car Region").activeSelf, Is.True);
            Assert.That(GameObject.Find("Finale Home Region").activeSelf, Is.True);
            Assert.That(GameObject.Find("Finale Road Region").activeSelf, Is.True);
            Assert.That(GameObject.Find("Finale Friends Region").activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator Greybridge_EveryChapterSpawnHasAClearWalkableRouteToEveryMissionInUnderFortyFiveSeconds()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;

            var layout = Object.FindFirstObjectByType<GreybridgeWorldLayout>();
            var spawns = new[]
            {
                "Spawn Prologue", "Spawn Chapter 1", "Spawn Chapter 2", "Spawn Chapter 3 Day 3",
                "Spawn Chapter 3 Day 2", "Spawn Chapter 4", "Spawn Finale"
            };
            var missions = new[]
            {
                "Mission Zone Garage", "Mission Zone Diner", "Mission Zone Market", "Mission Zone Rooftop", "Mission Zone Electronics"
            };

            foreach (var spawnName in spawns)
            {
                var spawn = GameObject.Find(spawnName).transform.position;
                foreach (var missionName in missions)
                {
                    var mission = GameObject.Find(missionName).transform.position;
                    Assert.That(layout.HasClearWalkablePath(spawn, mission), Is.True, $"{spawnName} to {missionName}");
                    Assert.That(layout.GetWalkingSeconds(spawn, mission), Is.LessThan(45f), $"{spawnName} to {missionName}");
                }
            }
        }

        [UnityTest]
        public IEnumerator Greybridge_JamieUsesThePersistentBootstrapInputGate()
        {
            if (GameBootstrap.Instance != null)
            {
                Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }

            var safeTestSave = new SaveGameService(Path.Combine(Application.temporaryCachePath, $"northbound-world-{Guid.NewGuid():N}.json"));
            GameBootstrap.SessionSaveGameFactory = () => safeTestSave;
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var bootstrap = GameBootstrap.Instance;
            var title = GameObject.Find("TitleMenu(Clone)");
            title.GetComponentsInChildren<Button>(true).Single(button => button.name == "New Game").onClick.Invoke();
            title.GetComponentsInChildren<Button>(true).Single(button => button.name == "Confirm New Game").onClick.Invoke();
            for (var frame = 0; frame < 20 && !bootstrap.IsSessionActive; frame++)
            {
                yield return null;
            }

            Assert.That(bootstrap.Cinematics.IsPlaying, Is.True, "Starting a new game must hand input ownership to the opening cinematic.");
            bootstrap.Cinematics.Cancel();
            var motor = Object.FindFirstObjectByType<PlayerMotor>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(motor, Is.Not.Null);
            motor.SetMoveInput(Vector2.right);
            var start = motor.transform.position;
            var lease = bootstrap.InputGate.Acquire(this);
            try
            {
                yield return new WaitForFixedUpdate();
                Assert.That(motor.transform.position.x, Is.EqualTo(start.x).Within(0.0001f));

                lease.Dispose();
                yield return new WaitForFixedUpdate();
                Assert.That(motor.transform.position.x, Is.GreaterThan(start.x));
            }
            finally
            {
                lease.Dispose();
                motor.ClearMoveInputOverride();
            }
        }

        [UnityTest]
        public IEnumerator Greybridge_ContainsStableNpcAnchorsForEveryRelevantCharacterLocation()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;

            var anchors = Object.FindObjectsByType<GreybridgeNpcAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .ToDictionary(anchor => anchor.CharacterId, anchor => anchor.LocationId);

            Assert.That(anchors["jamie"], Is.EqualTo("old_neighborhood"));
            Assert.That(anchors["elias"], Is.EqualTo("vale_garage"));
            Assert.That(anchors["maya"], Is.EqualTo("maya_studio"));
            Assert.That(anchors["noah"], Is.EqualTo("noah_electronics"));
            Assert.That(anchors["leo"], Is.EqualTo("ruths_diner"));
        }
    }
}
