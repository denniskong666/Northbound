using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Northbound.Art;
using Northbound.Content;
using Northbound.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Northbound.Tests
{
    public sealed class GreybridgeArtPlayModeTests
    {
        [UnityTest]
        public IEnumerator Greybridge_InstantiatesTexturedLocationRootsAndFiveRealCharacterVisuals()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            foreach (var rootName in new[] { "Art Street", "Art Garage", "Art Diner", "Art Rooftop", "Art Gallery", "Art Electronics" })
            {
                var root = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(item => item.name == rootName)?.gameObject;
                Assert.That(root, Is.Not.Null, $"Greybridge must instantiate {rootName}.");
                Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.GreaterThan(0), $"{rootName} must use sprites.");
                var plate = root.GetComponent<SpriteRenderer>();
                Assert.That(plate, Is.Not.Null, rootName);
                Assert.That(Vector2.Distance(plate.bounds.center, root.transform.position), Is.LessThan(.01f),
                    $"{rootName} must use a centered environment pivot so doors and walkable bounds align with painted pixels.");
            }

            foreach (var characterId in new[] { "Jamie", "NPC elias", "NPC maya", "NPC noah", "NPC leo" })
            {
                var character = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(item => item.name == characterId)?.gameObject;
                Assert.That(character, Is.Not.Null, characterId);
                Assert.That(character.GetComponentInChildren<TopDownCharacterVisual>(true), Is.Not.Null, $"{characterId} must have a directional visual.");
                if (characterId != "Jamie")
                {
                    Assert.That(character.GetComponent<Rigidbody2D>(), Is.Null, $"{characterId} is a stationary interaction character and must not fall because of a visual component.");
                }
            }

            var allStoryVisuals = Object.FindObjectsByType<TopDownCharacterVisual>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(allStoryVisuals.Length, Is.GreaterThanOrEqualTo(12),
                "Stable NPCs, the rooftop cast and all four finale friends must use the same visual system.");
            Assert.That(allStoryVisuals.Select(visual => visual.EstimatedVisibleHeight),
                Has.All.EqualTo(TopDownCharacterVisual.StandardVisibleHeight).Within(.01f));

            Assert.That(Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Any(renderer => renderer.gameObject.name == "Visible Character Proxy"), Is.False, "No character may fall back to the old primitive proxy.");

            var objectiveTriggers = Object.FindObjectsByType<NarrativeObjectiveTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(objectiveTriggers.Length, Is.GreaterThan(12), "Every physical objective must exist in the playable map.");
            Assert.That(objectiveTriggers.All(trigger => trigger.GetComponentInChildren<SpriteRenderer>(true) != null), Is.True,
                "Every physical objective and carried-object choice must show a readable world prop, not an invisible collider.");
            Assert.That(objectiveTriggers.All(trigger => trigger.GetComponent<ObjectivePropFeedback>() != null), Is.True,
                "Every physical objective must persist its completed visual state instead of leaving collected props behind.");
            Assert.That(objectiveTriggers
                .Select(trigger => trigger.GetComponentInChildren<SpriteRenderer>(true))
                .Where(renderer => renderer != null && renderer.sprite != null)
                .All(renderer => !renderer.enabled), Is.True,
                "Future quest props must stay hidden instead of scattering photos, keys, maps and toolboxes across Greybridge.");
            var returnTable = objectiveTriggers.Single(trigger =>
                trigger.QuestId == "one_more_table" && trigger.ObjectiveId == "return_table");
            Assert.That(returnTable.GetComponentInChildren<SpriteRenderer>(true).sprite, Is.Null,
                "The painted diner table needs a gold interaction point, not an unrelated floating toolbox.");
            foreach (var visiblePickup in new[] { "find_socket", "wire_recorder", "find_key" })
            {
                var trigger = objectiveTriggers.Single(item => item.ObjectiveId == visiblePickup);
                Assert.That(trigger.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sprite != null), Is.True,
                    $"{visiblePickup} must show the actual pickup inside its gold outline, not an empty renderer.");
            }

            foreach (var duplicateName in new[] { "Art Socket", "Art Battery", "Art Toolbox", "Art Painting", "Art Recorder", "Art Notebook", "Art Map" })
            {
                Assert.That(GameObject.Find(duplicateName), Is.Null, $"{duplicateName} duplicates a real objective and would remain after pickup.");
            }

            var prompt = Object.FindFirstObjectByType<InteractionPromptView>();
            Assert.That(prompt, Is.Not.Null, "Greybridge must provide a readable interaction prompt for nearby characters and props.");
            var promptText = prompt.GetComponentInChildren<UnityEngine.UI.Text>(true);
            Assert.That(promptText.fontSize, Is.GreaterThanOrEqualTo(32));
        }

        [UnityTest]
        public IEnumerator RooftopInventory_ShowsEliasLeoAndMayaAsCharactersInsteadOfQuestProps()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var rooftop = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(item => item.name == "Location rooftop_overlook");
            var catalog = Resources.Load<NorthboundArtCatalog>("Northbound/NorthboundArtCatalog");
            var questPropTexture = catalog.QuestProp(4).texture;

            foreach (var id in new[] { "elias", "leo", "maya" })
            {
                var actor = rooftop.GetComponentsInChildren<Transform>(true)
                    .Single(item => item.name == $"Rooftop {id}");
                var visual = actor.GetComponent<TopDownCharacterVisual>();
                Assert.That(visual, Is.Not.Null, $"The rooftop scene needs a real {id} character visual.");
                Assert.That(visual.CurrentSprite, Is.Not.Null, id);
                Assert.That(visual.CurrentSprite.texture, Is.SameAs(catalog.Character(id, Facing.South, false).texture), id);
                Assert.That(visual.CurrentSprite.texture, Is.Not.SameAs(questPropTexture),
                    $"{id} must never be represented by the inventory book/toolbox sheet.");
                Assert.That(visual.EstimatedVisibleHeight,
                    Is.EqualTo(TopDownCharacterVisual.StandardVisibleHeight).Within(.01f), id);
            }

            var expectedFloorPositions = new Dictionary<string, Vector2>
            {
                ["elias"] = new Vector2(20f, 9.5f),
                ["leo"] = new Vector2(23f, 9f),
                ["maya"] = new Vector2(26f, 9.5f)
            };
            foreach (var pair in expectedFloorPositions)
            {
                var actor = rooftop.GetComponentsInChildren<Transform>(true)
                    .Single(item => item.name == $"Rooftop {pair.Key}");
                Assert.That((Vector2)actor.position, Is.EqualTo(pair.Value),
                    $"Rooftop {pair.Key} must stand on the open roof surface.");
            }
        }

        [UnityTest]
        public IEnumerator RoomNpcs_UseCanonicalLocationsAndClearReachableFloorAnchors()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var expected = new Dictionary<string, (string locationId, Vector2 position)>
            {
                ["elias"] = ("vale_garage", new Vector2(-14.5f, -6.2f)),
                ["maya"] = ("maya_studio", new Vector2(18.5f, 2.5f)),
                ["noah"] = ("noah_electronics", new Vector2(10f, -3.2f)),
                ["leo"] = ("ruths_diner", new Vector2(-7.4f, .5f))
            };
            var garageCarBody = Rect.MinMaxRect(-25f, -6.7f, -16.8f, -1.4f);

            foreach (var pair in expected)
            {
                var actor = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .SingleOrDefault(item => item.name == $"NPC {pair.Key}")?.gameObject;
                Assert.That(actor, Is.Not.Null, pair.Key);
                var anchor = actor.GetComponent<Northbound.World.GreybridgeNpcAnchor>();
                Assert.That(anchor.LocationId, Is.EqualTo(pair.Value.locationId), pair.Key);
                Assert.That(actor.transform.parent.name, Is.EqualTo($"Location {pair.Value.locationId}"), pair.Key);
                Assert.That((Vector2)actor.transform.position, Is.EqualTo(pair.Value.position),
                    $"{pair.Key} must stand on the authored room's open floor.");
                Assert.That(actor.GetComponent<CircleCollider2D>(), Is.Not.Null, pair.Key);

                var interactionPoint = pair.Value.position + Vector2.down;
                actor.transform.parent.gameObject.SetActive(true);
                Physics2D.SyncTransforms();
                Assert.That(Physics2D.OverlapCircleAll(interactionPoint, 1.25f)
                    .Any(collider => collider.gameObject == actor), Is.True,
                    $"{pair.Key} must be reachable from a clear one-step interaction position.");
                Assert.That(actor.GetComponent<TopDownCharacterVisual>().EstimatedVisibleHeight,
                    Is.EqualTo(TopDownCharacterVisual.StandardVisibleHeight).Within(.01f), pair.Key);
            }

            Assert.That(garageCarBody.Contains(expected["elias"].position), Is.False,
                "Elias must stand beside the garage car, never on its painted roof or hood.");
        }
    }
}
