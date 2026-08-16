using System.Collections;
using Northbound.Core;
using Northbound.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Northbound.UI;
using Northbound.Content;
using Northbound.Art;
using Northbound.Narrative;

namespace Northbound.Tests
{
    public sealed class LocationTransitionTests
    {
        [UnityTest]
        public IEnumerator DoorInteraction_SwitchesToOnlyDestinationRootAndReleasesInput()
        {
            var host = new GameObject("Location Host");
            var gate = host.AddComponent<InputGate>();
            var player = new GameObject("Jamie");
            var exterior = new GameObject("Exterior");
            var garage = new GameObject("Garage");
            var exteriorSpawn = new GameObject("Exterior Spawn").transform;
            var garageSpawn = new GameObject("Garage Spawn").transform;
            garageSpawn.position = new Vector3(12, 8, 0);
            var controller = host.AddComponent<LocationTransitionController>();
            controller.Configure(player.transform, gate, null);
            controller.Register(new LocationDefinition("exterior", exterior, exteriorSpawn, new Bounds(Vector3.zero, Vector3.one * 20), "Greybridge"));
            controller.Register(new LocationDefinition("vale_garage", garage, garageSpawn, new Bounds(Vector3.zero, Vector3.one * 12), "Vale Auto Garage"));
            controller.SetInitial("exterior");
            var door = new GameObject("Garage Door").AddComponent<DoorInteractor>();
            door.Configure("[E] Enter Vale Auto Garage", "vale_garage", controller);

            door.Interact(player);
            yield return null;
            yield return null;

            Assert.That(exterior.activeSelf, Is.False);
            Assert.That(garage.activeSelf, Is.True);
            Assert.That(player.transform.position, Is.EqualTo(garageSpawn.position));
            Assert.That(gate.IsBlocked, Is.False);
            Assert.That(controller.CurrentLocationId, Is.EqualTo("vale_garage"));
            Object.Destroy(host); Object.Destroy(player); Object.Destroy(exterior); Object.Destroy(garage);
        }

        [UnityTest]
        public IEnumerator UnknownDestination_LeavesCurrentLocationActiveAndReleasesInput()
        {
            var host = new GameObject("Location Host");
            var gate = host.AddComponent<InputGate>();
            var player = new GameObject("Jamie");
            var exterior = new GameObject("Exterior");
            var controller = host.AddComponent<LocationTransitionController>();
            controller.Configure(player.transform, gate, null);
            controller.Register(new LocationDefinition("exterior", exterior, player.transform, new Bounds(Vector3.zero, Vector3.one * 20), "Greybridge"));
            controller.SetInitial("exterior");

            controller.StartTravel("missing_room");
            yield return null;

            Assert.That(exterior.activeSelf, Is.True);
            Assert.That(controller.CurrentLocationId, Is.EqualTo("exterior"));
            Assert.That(gate.IsBlocked, Is.False);
            Object.Destroy(host); Object.Destroy(player); Object.Destroy(exterior);
        }

        [UnityTest]
        public IEnumerator Greybridge_ProvidesExteriorAndSixEnterableStoryLocations()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;
            GameText.Use(GameLanguage.English);

            var controller = Object.FindFirstObjectByType<LocationTransitionController>();
            Assert.That(controller, Is.Not.Null);
            CollectionAssert.AreEquivalent(new[] { "exterior", "jamie_home", "vale_garage", "ruths_diner", "maya_studio", "noah_electronics", "rooftop_overlook" }, controller.RegisteredLocationIds);
            Assert.That(controller.CurrentLocationId, Is.EqualTo("exterior"));
            Assert.That(GameObject.Find("Location exterior"), Is.Not.Null);
            Assert.That(Object.FindObjectsByType<DoorInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(12));
            Assert.That(Object.FindObjectsByType<DoorInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Any(door => door.Prompt == GameText.Prompt("[E] Enter Vale Auto Garage")), Is.True);
        }

        [UnityTest]
        public IEnumerator Greybridge_EachInteriorHasFourSolidRoomBoundariesAndBackgroundCoverage()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            foreach (var roomId in new[] { "jamie_home", "vale_garage", "ruths_diner", "maya_studio", "noah_electronics", "rooftop_overlook" })
            {
                var room = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(item => item.name == $"Location {roomId}").gameObject;
                var boundaries = room.GetComponentsInChildren<BoxCollider2D>(true)
                    .Where(collider => !collider.isTrigger && collider.name.StartsWith("Room Boundary"))
                    .ToArray();
                Assert.That(boundaries, Has.Length.EqualTo(4), $"{roomId} must physically contain Jamie.");

                var background = room.GetComponentsInChildren<SpriteRenderer>(true)
                    .FirstOrDefault(renderer => renderer.name.StartsWith("Art "));
                Assert.That(background, Is.Not.Null, $"{roomId} needs a room background.");
                Assert.That(background.bounds.size.x, Is.GreaterThanOrEqualTo(16f), $"{roomId} background must cover a 16:9 view.");
                Assert.That(background.bounds.size.y, Is.GreaterThanOrEqualTo(9f), $"{roomId} background must cover a 16:9 view.");
            }
        }

        [UnityTest]
        public IEnumerator EnteringDiner_ClampsJamieToTheVisibleRoomArea()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<LocationTransitionController>();
            var jamie = Object.FindFirstObjectByType<Northbound.Player.PlayerMotor>();
            controller.SetTransitionDuration(0f);
            Assert.That(controller.StartTravel("ruths_diner"), Is.True);
            while (controller.IsTravelling) yield return null;

            Assert.That(jamie.CurrentMovementBounds.HasValue, Is.True,
                "The active room must configure a hard player boundary, not only decorative wall colliders.");
            jamie.SetMoveInput(new Vector2(1f, -1f));
            for (var index = 0; index < 180; index++) yield return new WaitForFixedUpdate();

            Assert.That(jamie.transform.position.x, Is.LessThanOrEqualTo(3.86f));
            Assert.That(jamie.transform.position.y, Is.GreaterThanOrEqualTo(-2.41f));
        }

        [UnityTest]
        public IEnumerator EnteringRooftop_KeepsTheWholeRoomCameraCenteredAfterLateUpdate()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<LocationTransitionController>();
            controller.SetTransitionDuration(0f);
            Assert.That(controller.StartTravel("rooftop_overlook"), Is.True);
            while (controller.IsTravelling) yield return null;
            yield return null;

            var camera = Camera.main;
            Assert.That(camera.rect, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
            Assert.That(camera.transform.position.x, Is.EqualTo(23f).Within(.05f));
            Assert.That(camera.transform.position.y, Is.EqualTo(9f).Within(.05f),
                "The exterior camera clamp must not drag an interior room into the upper-right corner.");
        }

        [UnityTest]
        public IEnumerator EnteringFinaleFromRooftop_ReactivatesExteriorBeforeRespawningJamie()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var locations = Object.FindFirstObjectByType<LocationTransitionController>();
            locations.SetTransitionDuration(0f);
            Assert.That(locations.StartTravel("rooftop_overlook"), Is.True);
            while (locations.IsTravelling) yield return null;
            Assert.That(locations.CurrentLocationId, Is.EqualTo("rooftop_overlook"));

            var savePath = Path.Combine(Application.temporaryCachePath, "northbound-finale-location-transition.json");
            var save = new SaveGameService(savePath);
            save.Delete();
            var flow = Object.FindFirstObjectByType<GameFlowController>();
            var world = Object.FindFirstObjectByType<ChapterWorldController>();
            flow.Initialize(new Northbound.Narrative.NarrativeStateStore(), save, world);

            Assert.That(flow.EnterChapter("finale"), Is.True);
            yield return new WaitForFixedUpdate();

            var spawn = GameObject.Find("Spawn Finale").transform.position;
            var jamie = GameObject.Find("Jamie");
            var exterior = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(item => item.name == "Location exterior").gameObject;
            Assert.That(locations.CurrentLocationId, Is.EqualTo("exterior"));
            Assert.That(exterior.activeInHierarchy, Is.True);
            Assert.That(GameObject.Find("Finale Gathering"), Is.Not.Null);
            Assert.That((Vector2)jamie.transform.position, Is.EqualTo((Vector2)spawn),
                "Rooftop movement bounds must not clamp the finale respawn on the next physics step.");
            Assert.That(jamie.GetComponent<Northbound.Player.PlayerMotor>().CurrentMovementBounds.Value.Contains(spawn), Is.True);
            save.Delete();
        }

        [UnityTest]
        public IEnumerator DinerExit_IsAtTheRoomEdgeRatherThanBesideLeo()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var exit = Object.FindObjectsByType<DoorInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(door => door.name == "Exit ruths_diner");
            var leo = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(item => item.name == "NPC leo");

            Assert.That(Vector2.Distance(exit.transform.position, leo.position), Is.GreaterThan(4f));
        }

        [UnityTest]
        public IEnumerator Greybridge_EntrancesAndReturnsSitOnTheirAuthoredDoorThresholds()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;
            GameText.Use(GameLanguage.English);

            var expectedEntrances = new Dictionary<string, Vector2>
            {
                ["vale_garage"] = new Vector2(-14.1f, .3f),
                ["ruths_diner"] = new Vector2(-2f, 5.2f),
                ["jamie_home"] = new Vector2(8.2f, 4.4f),
                ["maya_studio"] = new Vector2(15.2f, 1.4f),
                ["noah_electronics"] = new Vector2(21.7f, 1.4f),
                ["rooftop_overlook"] = new Vector2(6.5f, -6.2f)
            };
            var expectedReturns = new Dictionary<string, Vector2>
            {
                ["jamie_home"] = new Vector2(6.8f, 1.7f),
                ["vale_garage"] = new Vector2(-25.8f, -3.6f),
                ["ruths_diner"] = new Vector2(-3.3f, -1.1f),
                ["maya_studio"] = new Vector2(22f, 2.6f),
                ["noah_electronics"] = new Vector2(12f, -1.1f),
                ["rooftop_overlook"] = new Vector2(15f, 10.5f)
            };
            var doors = Object.FindObjectsByType<DoorInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var pair in expectedEntrances)
            {
                var door = doors.Single(candidate => candidate.DestinationId == pair.Key && LocationId(candidate.transform) == "exterior");
                Assert.That(Vector2.Distance(door.transform.position, pair.Value), Is.LessThan(.01f),
                    $"{pair.Key} entrance must be attached to its visible exterior threshold.");
                Assert.That(door.Prompt, Does.Contain("ENTER"), "English door prompts must disclose Enter as well as E.");
            }

            foreach (var pair in expectedReturns)
            {
                var door = doors.Single(candidate => candidate.name == $"Exit {pair.Key}");
                Assert.That(door.DestinationId, Is.EqualTo("exterior"));
                Assert.That(Vector2.Distance(door.transform.position, pair.Value), Is.LessThan(.01f),
                    $"{pair.Key} return must sit on the door painted into that room.");
            }
        }

        [UnityTest]
        public IEnumerator EveryInteriorConstrainsJamieToItsPaintedFloorAndKeepsTheReturnReachable()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<LocationTransitionController>();
            var motor = Object.FindFirstObjectByType<Northbound.Player.PlayerMotor>();
            controller.SetTransitionDuration(0f);
            foreach (var roomId in new[] { "jamie_home", "vale_garage", "ruths_diner", "maya_studio", "noah_electronics", "rooftop_overlook" })
            {
                Assert.That(controller.StartTravel(roomId), Is.True, roomId);
                while (controller.IsTravelling) yield return null;

                Assert.That(motor.CurrentMovementBounds.HasValue, Is.True, roomId);
                var floor = motor.CurrentMovementBounds.Value;
                Assert.That(floor.size.x, Is.LessThan(22.4f), $"{roomId} must exclude the plate's dark side margins.");
                Assert.That(floor.size.y, Is.LessThan(11.5f), $"{roomId} must exclude the plate's dark lower margin.");
                var exit = Object.FindObjectsByType<DoorInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(candidate => candidate.name == $"Exit {roomId}");
                Assert.That(floor.SqrDistance(exit.transform.position), Is.LessThanOrEqualTo(1.7f * 1.7f),
                    $"Jamie must be able to reach the visible return door in {roomId}.");
            }
        }

        [UnityTest]
        public IEnumerator PackTrunk_RouteAndObjectivesBelongToGarageWithoutADetachedVehicleOverlay()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var route = Object.FindObjectsByType<NarrativeRouteTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(trigger => trigger.RouteId == "pack_trunk_trigger");
            var objectives = Object.FindObjectsByType<NarrativeObjectiveTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(trigger => trigger.QuestId == "pack_trunk")
                .ToArray();

            Assert.That(LocationId(route.transform), Is.EqualTo("vale_garage"));
            Assert.That(objectives, Is.Not.Empty);
            Assert.That(objectives.Select(trigger => LocationId(trigger.transform)), Has.All.EqualTo("vale_garage"));
            Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Any(item => item.name == "Art Station Wagon"), Is.False,
                "The garage plate already contains the complete car; an extra cropped wagon creates detached body fragments.");
        }

        [UnityTest]
        public IEnumerator BeforeMorning_FriendVisitObjectivesBelongToEachFriendsActualRoom()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var expectedLocations = new Dictionary<string, string>
            {
                ["visit_maya"] = "maya_studio",
                ["visit_noah"] = "noah_electronics",
                ["visit_leo"] = "ruths_diner"
            };
            var objectives = Object.FindObjectsByType<NarrativeObjectiveTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(trigger => trigger.QuestId == "before_morning")
                .ToArray();

            Assert.That(objectives, Has.Length.EqualTo(expectedLocations.Count));
            foreach (var objective in objectives)
            {
                Assert.That(expectedLocations.ContainsKey(objective.ObjectiveId), Is.True, objective.ObjectiveId);
                Assert.That(LocationId(objective.transform), Is.EqualTo(expectedLocations[objective.ObjectiveId]), objective.ObjectiveId);
                Assert.That(LocationId(objective.transform), Is.Not.EqualTo("rooftop_overlook"), objective.ObjectiveId);
            }
        }

        [UnityTest]
        public IEnumerator NoahRoom_SpawnAndStaticInteractionsStayOutsideTheExitDoorInteractionShadow()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<LocationTransitionController>();
            controller.SetTransitionDuration(0f);
            Assert.That(controller.StartTravel("noah_electronics"), Is.True);
            while (controller.IsTravelling) yield return null;

            var room = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(item => item.name == "Location noah_electronics");
            var exit = room.GetComponentsInChildren<DoorInteractor>(true)
                .Single(door => door.DestinationId == "exterior");
            var exitCollider = exit.GetComponent<Collider2D>();
            var route = room.GetComponentsInChildren<NarrativeRouteTrigger>(true)
                .Single(trigger => trigger.RouteId == "static_trigger");
            var objectives = room.GetComponentsInChildren<NarrativeObjectiveTrigger>(true)
                .Where(trigger => trigger.QuestId == "static")
                .ToArray();
            var candidatePositions = new List<(string label, Vector2 position)>
            {
                ("Noah room spawn", room.Find("Spawn").position),
                ("Static route", route.transform.position)
            };
            candidatePositions.AddRange(objectives.Select(objective =>
                ($"Static objective {objective.ObjectiveId}", (Vector2)objective.transform.position)));
            Physics2D.SyncTransforms();

            Assert.That(objectives, Has.Length.EqualTo(2));
            foreach (var candidate in candidatePositions)
            {
                Assert.That(Physics2D.OverlapCircleAll(candidate.position, 1.25f).Contains(exitCollider), Is.False,
                    $"{candidate.label} must not let the exit door override its interaction.");
            }
        }

        [UnityTest]
        public IEnumerator Finale_CreatesAVisibleGatheringPointWithFourFriendsOnTheStreet()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var gathering = GameObject.Find("Finale Gathering");
            Assert.That(gathering, Is.Not.Null);
            Assert.That(LocationId(gathering.transform), Is.EqualTo("exterior"));
            Assert.That((Vector2)gathering.transform.position, Is.EqualTo(Vector2.zero),
                "The finale gathering belongs in the clear center intersection, not on the lower-right rooftop painted into the street plate.");
            var finaleSpawn = GameObject.Find("Spawn Finale").transform.position;
            Assert.That((Vector2)finaleSpawn, Is.EqualTo(new Vector2(0f, -4f)));
            Assert.That(gathering.GetComponent<FinaleGatheringInteractor>(), Is.Not.Null);
            Assert.That(gathering.GetComponent<Northbound.Guidance.ObjectiveMarker>(), Is.Not.Null);
            Assert.That(gathering.GetComponentsInChildren<TopDownCharacterVisual>(true), Has.Length.EqualTo(4));
            var wagon = gathering.transform.Find("Greybridge Friends/Finale Wagon");
            Assert.That(wagon, Is.Not.Null);
            var wagonRenderer = wagon.GetComponent<SpriteRenderer>();
            Assert.That(wagonRenderer.sprite, Is.Not.Null);
            Assert.That(wagonRenderer.sprite.rect.width / wagonRenderer.sprite.rect.height, Is.GreaterThan(1.4f));
            Assert.That(wagonRenderer.sharedMaterial, Is.Not.Null);
            Assert.That(wagonRenderer.sharedMaterial.HasProperty("_KeyColor"), Is.True,
                "The finale wagon must remove the source sheet's magenta key background.");
            var jamie = GameObject.Find("Jamie");
            var jamieRenderer = jamie.GetComponent<TopDownCharacterVisual>().CharacterRenderer;
            var jamieTopAtSpawn = jamieRenderer.bounds.max.y + finaleSpawn.y - jamie.transform.position.y;
            Assert.That(jamieTopAtSpawn, Is.LessThan(wagonRenderer.bounds.min.y),
                "The finale approach spawn must not place Jamie inside the wagon artwork.");
            var starRenderer = gathering.transform.Find("Required Objective Star").GetComponent<Renderer>();
            Assert.That(starRenderer.bounds.min.y, Is.GreaterThan(wagonRenderer.bounds.max.y),
                "The gold marker must remain visibly above the wagon instead of disappearing inside its body.");
            var cast = gathering.transform.Find("Greybridge Friends");
            Assert.That(cast.Find("Finale elias").localPosition.x, Is.LessThan(-2.5f));
            Assert.That(cast.Find("Finale leo").localPosition.x, Is.GreaterThan(2.5f));
            Assert.That(Mathf.Abs(cast.Find("Finale maya").localPosition.x), Is.GreaterThan(1f));
            Assert.That(Mathf.Abs(cast.Find("Finale noah").localPosition.x), Is.GreaterThan(1f),
                "The cast must leave the gold marker visible instead of standing over the car and marker.");
        }

        private static string LocationId(Transform target)
        {
            while (target != null && !target.name.StartsWith("Location ")) target = target.parent;
            return target == null ? string.Empty : target.name.Substring("Location ".Length);
        }
    }
}
