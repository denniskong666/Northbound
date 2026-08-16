using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Northbound.Core;
using Northbound.Guidance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Northbound.UI;
using Northbound.World;
using Northbound.Content;
using Northbound.Quests;

namespace Northbound.Tests
{
    public sealed class GuidanceFlowTests
    {
        [UnityTest]
        public IEnumerator Greybridge_ProvidesReadableHudAndExactlyOnePrimaryMarker()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var guidance = Object.FindFirstObjectByType<GuidanceController>();
            var hud = Object.FindFirstObjectByType<GuidanceHudView>();
            Assert.That(guidance, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            var visibleText = hud.GetComponentsInChildren<Text>(false);
            Assert.That(visibleText, Has.Length.EqualTo(4));
            Assert.That(visibleText, Has.All.Matches<Text>(text => text.fontSize >= 24));
            var labels = visibleText.Select(text => text.text).ToArray();
            Assert.That(visibleText, Has.All.Matches<Text>(text => text.font != null));
            Assert.That(labels, Has.All.Not.Empty);
            Assert.That(labels.Any(label => label.StartsWith(GameText.T("GO TO:", "目的地："))), Is.True);
            Assert.That(labels.Any(label => label.StartsWith(GameText.T("NOW:", "当前任务："))), Is.True);
            Assert.That(labels.Any(label => label.StartsWith(GameText.T("MOVE:", "移动：")) || label.StartsWith(GameText.T("NEXT:", "下一步：")) || label.StartsWith(GameText.T("ENTER:", "进入："))), Is.True);
            Assert.That(hud.GetComponentsInChildren<Button>(true).Single(button => button.name == "Pause"), Is.Not.Null);

            var activePrimary = Object.FindObjectsByType<ObjectiveMarker>(FindObjectsSortMode.None)
                .Count(marker => marker.transform.Find("Required Objective Star")?.gameObject.activeSelf == true);
            Assert.That(activePrimary, Is.LessThanOrEqualTo(1));
        }

        [UnityTest]
        public IEnumerator ObjectiveHud_UsesLargeGoldFramingAndNamesTheExactInteraction()
        {
            GameText.Use(GameLanguage.English);
            var hud = GuidanceHudView.Create();
            hud.Show("Vale Auto Garage", "Pick up the missing socket", "NEXT: At the gold-outlined target, press E / Enter to pick up the missing socket.");
            yield return null;

            Assert.That(hud.ObjectivePanelSize.x, Is.GreaterThanOrEqualTo(720f));
            Assert.That(hud.ObjectivePanelSize.y, Is.GreaterThanOrEqualTo(220f));
            Assert.That(hud.HasGoldObjectivePanelBorder, Is.True);
            Assert.That(hud.CurrentObjectiveText, Does.Contain("missing socket"));
            Assert.That(hud.CurrentInstructionText, Does.Contain("E / Enter"));
            Assert.That(hud.CurrentInstructionText, Does.Contain("gold-outlined"));

            Object.Destroy(hud.gameObject);
        }

        [UnityTest]
        public IEnumerator EveryPhysicalObjective_HasALargeGoldMarkerAndVisibleObjectOutline()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var objectives = Object.FindObjectsByType<NarrativeObjectiveTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(objectives, Is.Not.Empty);
            foreach (var objective in objectives)
            {
                var marker = objective.GetComponent<ObjectiveMarker>();
                Assert.That(marker, Is.Not.Null, $"{objective.QuestId}/{objective.ObjectiveId}");
                Assert.That(marker.HasObjectiveOutline, Is.True, $"{objective.QuestId}/{objective.ObjectiveId}");
                var star = marker.transform.Find("Required Objective Star");
                Assert.That(star, Is.Not.Null, $"{objective.QuestId}/{objective.ObjectiveId}");
                Assert.That(star.localScale.x, Is.GreaterThanOrEqualTo(.55f), $"{objective.QuestId}/{objective.ObjectiveId}");

                marker.SetHighlighted(true);
                Assert.That(marker.IsHighlighted, Is.True, $"{objective.QuestId}/{objective.ObjectiveId}");
                Assert.That(marker.ObjectiveOutlineVisible, Is.True, $"{objective.QuestId}/{objective.ObjectiveId}");
                marker.SetHighlighted(false);
            }
        }

        [UnityTest]
        public IEnumerator ThingsWeLeave_RegistersAllFourCarriedObjectsAsTheSameCurrentTarget()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var guidance = Object.FindFirstObjectByType<GuidanceController>();
            var field = typeof(GuidanceController).GetField("targets", BindingFlags.Instance | BindingFlags.NonPublic);
            var targets = field?.GetValue(guidance) as IDictionary<string, List<ObjectiveMarker>>;

            Assert.That(targets, Is.Not.Null);
            Assert.That(targets.TryGetValue("things_we_leave:choose_carried_object", out var choices), Is.True);
            Assert.That(choices, Has.Count.EqualTo(4));
            Assert.That(choices, Has.All.Matches<ObjectiveMarker>(marker => marker.HasObjectiveOutline));
        }

        [UnityTest]
        public IEnumerator Greybridge_EveryExteriorDoorHasALocalizedPlaqueOnTheDoorObject()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;
            GameText.Use(GameLanguage.English);

            var entrances = Object.FindObjectsByType<DoorInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(door => door.DestinationId != "exterior" && door.transform.parent != null && door.transform.parent.name == "Location exterior")
                .OrderBy(door => door.DestinationId)
                .ToArray();
            Assert.That(entrances, Has.Length.EqualTo(6));

            foreach (var entrance in entrances)
            {
                var plaque = entrance.GetComponentInChildren<DoorNamePlaque>(true);
                Assert.That(plaque, Is.Not.Null, entrance.name);
                Assert.That(plaque.transform.parent, Is.EqualTo(entrance.transform), entrance.name);
                Assert.That(plaque.LocationId, Is.EqualTo(entrance.DestinationId), entrance.name);
                Assert.That(plaque.LabelText, Is.EqualTo(plaque.EnglishDisplayName), entrance.name);
                Assert.That(plaque.transform.localPosition.y, Is.GreaterThan(1f), entrance.name);
            }

            GameText.Use(GameLanguage.SimplifiedChinese);
            try
            {
                foreach (var entrance in entrances)
                {
                    var plaque = entrance.GetComponentInChildren<DoorNamePlaque>(true);
                    Assert.That(plaque.LabelText, Is.EqualTo(GameText.Location(plaque.EnglishDisplayName)), entrance.name);
                }
            }
            finally
            {
                GameText.Use(GameLanguage.English);
            }
        }

        [UnityTest]
        public IEnumerator Guidance_HighlightsOnlyTheCurrentExteriorDestinationPlaque()
        {
            var host = new GameObject("Guidance Plaque Test");
            var state = new Northbound.Narrative.NarrativeStateStore();
            state.Set("tutorial_moved", true);
            var director = host.AddComponent<Northbound.Content.NarrativeContentDirector>();
            var guidance = host.AddComponent<GuidanceController>();
            guidance.Configure(state, director, null, null);
            var plaques = new[]
            {
                CreatePlaque("jamie_home", "Jamie's Home"),
                CreatePlaque("vale_garage", "Vale Auto Garage"),
                CreatePlaque("ruths_diner", "Ruth's Diner"),
                CreatePlaque("maya_studio", "Maya's Studio"),
                CreatePlaque("noah_electronics", "Noah's Electronics"),
                CreatePlaque("rooftop_overlook", "Rooftop Overlook")
            };
            foreach (var plaque in plaques) guidance.RegisterDoorPlaque(plaque.LocationId, plaque);
            yield return null;

            Assert.That(guidance.CurrentDestinationId, Does.StartWith("entrance:"));
            Assert.That(plaques.Count(plaque => plaque.IsHighlighted), Is.EqualTo(1));
            Assert.That(plaques.Single(plaque => plaque.IsHighlighted).LocationId,
                Is.EqualTo(guidance.CurrentDestinationId.Substring("entrance:".Length)));
            Object.Destroy(host);

            DoorNamePlaque CreatePlaque(string id, string displayName)
            {
                var door = new GameObject($"Door {id}");
                door.transform.SetParent(host.transform, false);
                return DoorNamePlaque.Create(door.transform, id, displayName);
            }
        }

        [UnityTest]
        public IEnumerator DirectionIndicator_ShowsOnlyWhileTheDestinationIsOffscreen()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;
            var hud = Object.FindFirstObjectByType<GuidanceHudView>();
            hud.SetPresentationVisible(true);
            var camera = Camera.main;
            GameObject testCamera = null;
            if (camera == null)
            {
                testCamera = new GameObject("Guidance Test Camera", typeof(Camera));
                testCamera.tag = "MainCamera";
                camera = testCamera.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
            }
            var target = new GameObject("Direction Indicator Test Target");

            target.transform.position = camera.ViewportToWorldPoint(new Vector3(1.35f, .5f, 10f));
            hud.ShowDestination(target.transform, GameText.Location("Maya's Studio"));
            yield return null;
            Assert.That(hud.DirectionIndicatorVisible, Is.True);
            Assert.That(hud.DirectionLabel, Is.EqualTo(GameText.Location("Maya's Studio")));
            Assert.That(Mathf.DeltaAngle(hud.DirectionArrowRect.localEulerAngles.z, 0f), Is.EqualTo(0f).Within(1f));

            target.transform.position = camera.ViewportToWorldPoint(new Vector3(.5f, .5f, 10f));
            yield return null;
            Assert.That(hud.DirectionIndicatorVisible, Is.False);

            hud.ShowDestination(null, string.Empty);
            Assert.That(hud.DirectionIndicatorVisible, Is.False);
            Object.Destroy(target);
            if (testCamera != null) Object.Destroy(testCamera);
        }

        [UnityTest]
        public IEnumerator MissionCompletionCard_PersistsForAnInteriorAndRelocalizesInPlace()
        {
            GameText.Use(GameLanguage.English);
            var hud = GuidanceHudView.Create();
            hud.ShowMissionComplete("Clock In", true);
            yield return null;

            Assert.That(hud.MissionCompletionVisible, Is.True);
            Assert.That(hud.MissionCompletionTitle, Does.Contain("MISSION COMPLETE"));
            Assert.That(hud.MissionCompletionAction, Does.Contain("door"));
            Assert.That(hud.MissionCompletionAction, Does.Contain("E / Enter"));

            GameText.Use(GameLanguage.SimplifiedChinese);
            Assert.That(hud.MissionCompletionTitle, Does.Contain("任务完成"));
            Assert.That(hud.MissionCompletionAction, Does.Contain("离开房间"));
            Assert.That(hud.MissionCompletionVisible, Is.True,
                "An interior completion stays visible until Jamie reaches the door.");

            hud.ClearMissionComplete();
            Assert.That(hud.MissionCompletionVisible, Is.False);
            GameText.Use(GameLanguage.English);
            Object.Destroy(hud.gameObject);
        }

        [UnityTest]
        public IEnumerator MissionCompletionCard_SurvivesAutomaticChapterRelocationButClearsForDoorTravel()
        {
            GameText.Use(GameLanguage.English);
            var host = new GameObject("Completion Relocation Test");
            var player = new GameObject("Completion Relocation Jamie");
            var room = new GameObject("Completion Test Room");
            var exterior = new GameObject("Completion Test Exterior");
            var roomSpawn = new GameObject("Room Spawn").transform;
            var exteriorSpawn = new GameObject("Exterior Spawn").transform;
            roomSpawn.SetParent(room.transform, false);
            exteriorSpawn.SetParent(exterior.transform, false);
            var locations = host.AddComponent<LocationTransitionController>();
            locations.Configure(player.transform, host.AddComponent<InputGate>(), null);
            locations.Register(new LocationDefinition("ruths_diner", room, roomSpawn, new Bounds(Vector3.zero, Vector3.one * 12f), "Ruth's Diner"));
            locations.Register(new LocationDefinition("exterior", exterior, exteriorSpawn, new Bounds(Vector3.zero, Vector3.one * 24f), "Greybridge"));
            locations.SetInitial("ruths_diner");

            var state = new Northbound.Narrative.NarrativeStateStore();
            state.Set("tutorial_moved", true);
            var director = host.AddComponent<NarrativeContentDirector>();
            var hud = GuidanceHudView.Create();
            var guidance = host.AddComponent<GuidanceController>();
            guidance.Configure(state, director, null, hud);
            guidance.BindLocationController(locations);
            hud.ShowMissionComplete("Clock In", true);

            locations.SetInitial("exterior");
            yield return null;

            Assert.That(hud.MissionCompletionVisible, Is.True,
                "A chapter respawn uses SetInitial and must not erase a completion before it is visible.");
            Assert.That(hud.MissionCompletionAction, Does.Contain("next story objective"));

            locations.SetInitial("ruths_diner");
            Assert.That(hud.MissionCompletionAction, Does.Contain("door"));
            locations.SetTransitionDuration(0f);
            Assert.That(locations.StartTravel("exterior"), Is.True);
            yield return null;

            Assert.That(hud.MissionCompletionVisible, Is.False,
                "A real door transition acknowledges and clears the completion card.");
            Object.Destroy(host);
            Object.Destroy(player);
            Object.Destroy(room);
            Object.Destroy(exterior);
            Object.Destroy(hud.gameObject);
        }

        [UnityTest]
        public IEnumerator MissionCompletionCountdown_DoesNotExpireWhileGuidanceIsHidden()
        {
            var hud = GuidanceHudView.Create();
            hud.ShowMissionComplete("Clock In", false);
            hud.SetPresentationVisible(false);
            typeof(GuidanceHudView).GetField("completionVisibleRemaining", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(hud, -1f);

            yield return null;

            Assert.That(hud.MissionCompletionVisible, Is.True);
            hud.SetPresentationVisible(true);
            yield return null;
            Assert.That(hud.MissionCompletionVisible, Is.False);
            Object.Destroy(hud.gameObject);
        }

        [UnityTest]
        public IEnumerator EveryInteriorQuestRoute_CoversMostOfTheRoomButNeverTheExitDoor()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var director = Object.FindFirstObjectByType<NarrativeContentDirector>();
            var routes = Object.FindObjectsByType<NarrativeRouteTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(route => director.Manifest.FindTrigger(route.RouteId)?.routeType == "quest")
                .ToArray();
            Assert.That(routes, Is.Not.Empty);

            foreach (var route in routes)
            {
                var room = route.transform.parent;
                Assert.That(room, Is.Not.Null, route.RouteId);
                Assert.That(room.name, Does.StartWith("Location "), route.RouteId);
                var locationId = room.name.Substring("Location ".Length);
                Assert.That(locationId, Is.Not.EqualTo("exterior"), route.RouteId);
                var exit = room.GetComponentsInChildren<DoorInteractor>(true)
                    .Single(door => door.DestinationId == "exterior");
                var zone = route.GetComponent<RoomMissionStartZone>();
                Assert.That(zone, Is.Not.Null, route.RouteId);
                Assert.That(zone.InteractionArea, Is.GreaterThan(zone.RoomBounds.size.x * zone.RoomBounds.size.y * .7f), route.RouteId);
                Assert.That(zone.Contains(exit.transform.position), Is.False, route.RouteId);
                Assert.That(route.GetComponents<BoxCollider2D>().Where(item => item.enabled),
                    Has.All.Matches<BoxCollider2D>(item => !item.OverlapPoint(exit.transform.position)), route.RouteId);
                Assert.That(route.GetComponent<DoorInteractor>(), Is.Null,
                    "Mission areas may start content, but only real door objects may change rooms.");
            }
        }
    }
}
