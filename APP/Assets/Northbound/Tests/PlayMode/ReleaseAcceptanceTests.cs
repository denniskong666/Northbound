using System.Linq;
using System.Collections;
using Northbound.Content;
using Northbound.Interaction;
using Northbound.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Northbound.Tests
{
    public sealed class ReleaseAcceptanceTests
    {
        [Test]
        public void RuntimeHarness_KeepsReleaseScenesFirstAndExcludesTestSandbox()
        {
            var scenePaths = Enumerable.Range(0, SceneManager.sceneCountInBuildSettings)
                .Select(SceneUtility.GetScenePathByBuildIndex)
                .ToArray();

            Assert.That(scenePaths.Take(2), Is.EqualTo(new[]
            {
                "Assets/Northbound/Scenes/Bootstrap.unity",
                "Assets/Northbound/Scenes/Greybridge.unity"
            }));
        }

        [UnityTest]
        public IEnumerator GuidedInteriors_AllLocationsRoutesPropsAndCharactersAreRuntimeReachable()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<LocationTransitionController>();
            controller.SetTransitionDuration(0f);
            var expectedRootNames = controller.RegisteredLocationIds.Select(id => $"Location {id}").ToArray();
            var roots = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(item => expectedRootNames.Contains(item.name)).ToArray();
            Assert.That(roots, Has.Length.EqualTo(7));

            foreach (var id in controller.RegisteredLocationIds)
            {
                if (controller.CurrentLocationId != id)
                {
                    Assert.That(controller.StartTravel(id), Is.True, id);
                    for (var frame = 0; frame < 10 && controller.IsTravelling; frame++) yield return null;
                }
                Assert.That(roots.Count(root => root.gameObject.activeSelf), Is.EqualTo(1), $"Only {id} may render after travel.");
                var current = roots.Single(root => root.name == $"Location {id}");
                Assert.That(current.GetComponentInChildren<SpriteRenderer>(true), Is.Not.Null, $"{id} requires its own environment art.");
            }

            var routes = Object.FindObjectsByType<NarrativeRouteTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var objectives = Object.FindObjectsByType<NarrativeObjectiveTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(routes.Length, Is.GreaterThanOrEqualTo(35));
            Assert.That(objectives.Length, Is.GreaterThan(12));
            Assert.That(routes.All(route => HasLocationAncestor(route.transform)), Is.True);
            Assert.That(objectives.All(objective => HasLocationAncestor(objective.transform)), Is.True);

            foreach (var name in new[] { "NPC elias", "NPC maya", "NPC noah", "NPC leo" })
            {
                var character = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(item => item.name == name);
                Assert.That(character.GetComponents<MonoBehaviour>().OfType<IInteractable>().Any(), Is.True, $"{name} must be interactable.");
            }
        }

        private static bool HasLocationAncestor(Transform item)
        {
            while (item != null)
            {
                if (item.name.StartsWith("Location ")) return true;
                item = item.parent;
            }
            return false;
        }
    }
}
