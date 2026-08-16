using Northbound.Player;
using Northbound.Core;
using Northbound.Minigames;
using Northbound.Interaction;
using Northbound.Cinematics;
using Northbound.Endings;
using Northbound.Narrative;
using Northbound.Content;
using Northbound.Art;
using Northbound.Quests;
using Northbound.Guidance;
using Northbound.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Northbound.World
{
    [System.Serializable]
    public struct NpcAnchorDefinition
    {
        public string characterId;
        public string locationId;
        public Vector2 position;
    }

    [RequireComponent(typeof(ChapterWorldController))]
    public sealed class GreybridgeWorldLayout : MonoBehaviour
    {
        private const float WalkingSpeed = 4f;
        private static readonly Rect WalkableBounds = new Rect(-28f, -11f, 56f, 22f);
        private static readonly string[] FinaleRouteRegionNames =
        {
            "Finale Car Region",
            "Finale Home Region",
            "Finale Road Region",
            "Finale Friends Region"
        };
        [SerializeField] private ChapterVariant[] chapterVariants = new ChapterVariant[0];
        [SerializeField] private NpcAnchorDefinition[] npcAnchors = new NpcAnchorDefinition[0];
        private readonly List<EndingTrigger> endingTriggers = new List<EndingTrigger>();
        private ChapterWorldController chapterWorld;
        private NarrativeStateStore sceneNarrativeState;
        private GuidanceController guidance;
        private NarrativeContentDirector contentDirector;
        private GreybridgeArtBuilder artBuilder;

        public float GetWalkingSeconds(Vector2 start, Vector2 destination)
        {
            return Vector2.Distance(start, destination) / WalkingSpeed;
        }

        public bool HasClearWalkablePath(Vector2 start, Vector2 destination)
        {
            if (!WalkableBounds.Contains(start) || !WalkableBounds.Contains(destination))
            {
                return false;
            }

            foreach (var hit in Physics2D.LinecastAll(start, destination))
            {
                if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject.name != "Jamie")
                {
                    return false;
                }
            }

            return true;
        }

        private void Awake()
        {
            chapterWorld = GetComponent<ChapterWorldController>();
            contentDirector = GetComponent<NarrativeContentDirector>() ?? gameObject.AddComponent<NarrativeContentDirector>();
            contentDirector.EnsureInitialized();
            sceneNarrativeState = GameBootstrap.Instance != null ? GameBootstrap.Instance.NarrativeState : new NarrativeStateStore();
            chapterWorld.BindNarrativeState(sceneNarrativeState);
            chapterWorld.ChapterApplied += RefreshEndingRouteAvailability;
            sceneNarrativeState.Changed += RefreshEndingRouteAvailability;
            BuildMap();
            artBuilder = GetComponent<GreybridgeArtBuilder>() ?? gameObject.AddComponent<GreybridgeArtBuilder>();
            artBuilder.Build(transform);
            RegisterMarkers(chapterWorld);
            chapterWorld.Configure(chapterVariants, CreateFactBindings());
            CreateGuidance();
            CreateFinaleGathering();
            CreateNpcAnchors();
            CreateContentRoutes();
            CreateObjectiveInteractions();
            var player = CreatePlayerAndCamera();
            CreateLocations(player);
        }

        private void OnDestroy()
        {
            if (chapterWorld != null)
            {
                chapterWorld.ChapterApplied -= RefreshEndingRouteAvailability;
            }
            if (sceneNarrativeState != null)
            {
                sceneNarrativeState.Changed -= RefreshEndingRouteAvailability;
            }
        }

        private void BuildMap()
        {
            CreateArea("Old Neighborhood", new Vector2(-2f, 0f), new Vector2(26f, 17f), new Color(0.18f, 0.24f, 0.29f));
            CreateArea("Vale Auto Garage", new Vector2(-22f, -4f), new Vector2(13f, 11f), new Color(0.15f, 0.19f, 0.22f));
            CreateArea("Rooftop Overlook", new Vector2(23f, 9f), new Vector2(12f, 8f), new Color(0.12f, 0.16f, 0.23f));
            CreateArea("Alley Walkway", new Vector2(12f, 5f), new Vector2(18f, 3.5f), new Color(0.25f, 0.28f, 0.3f));
            CreateArea("Garage Walkway", new Vector2(-12f, -3f), new Vector2(8f, 3.5f), new Color(0.25f, 0.28f, 0.3f));

            CreateMarker("Open Diner", new Vector2(-7f, 3f), new Color(0.87f, 0.54f, 0.24f), true);
            CreateMarker("Open Market", new Vector2(4f, 4f), new Color(0.52f, 0.68f, 0.37f), true);
            CreateMarker("FINAL WEEK", new Vector2(4f, 6f), new Color(0.8f, 0.78f, 0.6f), false);
            CreateMarker("Dark Storefronts", new Vector2(0f, -5f), new Color(0.05f, 0.06f, 0.09f), false);
            CreateMarker("North Poster Torn", new Vector2(7f, -4f), new Color(0.42f, 0.37f, 0.31f), false);
            CreateMarker("Garage Countdown", new Vector2(-22f, 2f), new Color(0.54f, 0.68f, 0.78f), true);

            CreateSpawn("Spawn Prologue", new Vector2(22f, 8f));
            CreateSpawn("Spawn Chapter 1", new Vector2(-6f, 0f));
            CreateSpawn("Spawn Chapter 2", new Vector2(3f, 0f));
            CreateSpawn("Spawn Chapter 3 Day 3", new Vector2(-17f, -3f));
            CreateSpawn("Spawn Chapter 3 Day 2", new Vector2(-1f, -1f));
            CreateSpawn("Spawn Chapter 4", new Vector2(0f, -3f));
            // The center intersection is the clear road area on the authored street plate.
            CreateSpawn("Spawn Finale", new Vector2(0f, -4f));

            CreateMissionZone("Mission Zone Garage", new Vector2(-20f, -4f), new Vector2(3f, 3f));
            CreateMissionZone("Mission Zone Diner", new Vector2(-7f, 3f), new Vector2(3f, 3f));
            CreateMissionZone("Mission Zone Market", new Vector2(4f, 4f), new Vector2(3f, 3f));
            CreateMissionZone("Mission Zone Rooftop", new Vector2(23f, 9f), new Vector2(3f, 3f));
            CreateMissionZone("Mission Zone Electronics", new Vector2(9f, -2f), new Vector2(3f, 3f));

            CreateFinaleRouteMarker("Finale Car Region", new Vector2(10f, -8f), new Color(0.26f, 0.46f, 0.73f), "Southeast - Northbound Road");
            CreateFinaleRouteMarker("Finale Home Region", new Vector2(-4f, -9f), new Color(0.72f, 0.48f, 0.25f), "Southwest - Home in Greybridge");
            CreateFinaleRouteMarker("Finale Road Region", new Vector2(26f, -7f), new Color(0.47f, 0.47f, 0.51f), "East - Road Without a Map");
            CreateFinaleRouteMarker("Finale Friends Region", new Vector2(16f, 7f), new Color(0.65f, 0.4f, 0.6f), "Northeast - Wait Until Morning");
            CreateMarker("Finale Maya Region", new Vector2(14f, 8f), new Color(0.65f, 0.4f, 0.6f), false);
            CreateMarker("Finale Noah Region", new Vector2(24f, 3f), new Color(0.42f, 0.56f, 0.66f), false);
            CreateMarker("Finale Leo Region", new Vector2(-10f, 3f), new Color(0.78f, 0.48f, 0.24f), false);
            CreateEndingZone("Finale Car Choice", new Vector2(10f, -8f), new Vector2(8f, 10f), EndingDirection.Northbound, string.Empty, new Vector2(10f, -8f));
            CreateEndingZone("Finale Home Choice", new Vector2(-4f, -9f), new Vector2(12f, 8f), EndingDirection.HomeChosen, string.Empty, new Vector2(-4f, -9f));
            CreateEndingZone("Finale Road Choice", new Vector2(25f, -7f), new Vector2(8f, 10f), EndingDirection.NoMap, string.Empty, new Vector2(25f, -7f));
            CreateEndingZone("Finale Pause Choice", new Vector2(16f, 7f), new Vector2(6f, 10f), EndingDirection.PauseJourney, string.Empty, new Vector2(16f, 7f));

            CreateBoundary("North Boundary", new Vector2(0f, 12f), new Vector2(58f, 1f));
            CreateBoundary("South Boundary", new Vector2(0f, -13f), new Vector2(58f, 1f));
            CreateBoundary("West Boundary", new Vector2(-30f, 0f), new Vector2(1f, 26f));
            CreateBoundary("East Boundary", new Vector2(30f, 0f), new Vector2(1f, 26f));
        }

        private GameObject CreateArea(string objectName, Vector2 position, Vector2 size, Color color)
        {
            var area = CreateBox(objectName, position, size, color, false);
            area.AddComponent<BoxCollider2D>().isTrigger = true;
            return area;
        }

        private void CreateMarker(string objectName, Vector2 position, Color color, bool active)
        {
            var marker = CreateBox(objectName, position, new Vector2(3f, 2f), color, false);
            marker.SetActive(active);
        }

        private void CreateFinaleRouteMarker(string objectName, Vector2 position, Color color, string label)
        {
            var marker = new GameObject(objectName);
            marker.transform.SetParent(transform);
            marker.transform.position = new Vector3(position.x, position.y, -1f);

            var beacon = new GameObject("Route Beacon");
            beacon.transform.SetParent(marker.transform, false);
            beacon.transform.localScale = new Vector3(2.4f, .28f, 1f);
            var renderer = beacon.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSolidSprite();
            renderer.color = color;
            renderer.sortingOrder = 58;

            var plaque = DoorNamePlaque.Create(marker.transform, objectName, label);
            plaque.transform.localPosition = new Vector3(0f, 1.05f, -.35f);
            plaque.SetHighlighted(true);
            marker.SetActive(false);
        }

        private static Sprite CreateSolidSprite()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "Finale Route Marker Texture",
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(.5f, .5f), 2f);
            sprite.name = "Finale Route Marker Sprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private void CreateSpawn(string objectName, Vector2 position)
        {
            var spawn = new GameObject(objectName);
            spawn.transform.SetParent(transform);
            spawn.transform.position = position;
        }

        private void CreateMissionZone(string objectName, Vector2 position, Vector2 size)
        {
            var zone = new GameObject(objectName);
            zone.transform.SetParent(transform);
            zone.transform.position = position;
            var collider = zone.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = true;
        }

        private void CreateMinigameMissionZone(string objectName, Vector2 position, string minigameId, string questId, string objectiveId, string prompt)
        {
            var zone = new GameObject(objectName);
            zone.transform.SetParent(transform);
            zone.transform.position = position;
            var collider = zone.AddComponent<CircleCollider2D>();
            collider.radius = 1.3f;
            collider.isTrigger = true;
            zone.AddComponent<MinigameMissionTrigger>().Configure(minigameId, questId, objectiveId, prompt);
        }

        private void CreateEndingZone(string objectName, Vector2 position, Vector2 size, EndingDirection direction, string friendId, Vector2 commitmentDirection)
        {
            var zone = new GameObject(objectName);
            zone.transform.SetParent(transform);
            zone.transform.position = position;
            var collider = zone.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = true;
            var endingTrigger = zone.AddComponent<EndingTrigger>();
            endingTrigger.Configure(
                direction,
                friendId,
                new EndingResolver(),
                sceneNarrativeState,
                commitmentDirection,
                () => AreFinaleChoicesAvailable() && EndingResolver.IsDirectionAvailable(direction, sceneNarrativeState.State),
                () => GameBootstrap.Instance != null ? GameBootstrap.Instance.Settings.InteractionTimeMultiplier : 1f);
            endingTriggers.Add(endingTrigger);
        }

        private bool AreFinaleChoicesAvailable()
        {
            if (chapterWorld == null || chapterWorld.CurrentChapterId != "finale")
            {
                return false;
            }

            return GameBootstrap.Instance == null ||
                (sceneNarrativeState.Has("cinematic_finale_complete") &&
                 sceneNarrativeState.Has(FinaleGatheringInteractor.ReviewedFact) &&
                 !sceneNarrativeState.Has("ending_selected"));
        }

        private void RefreshEndingRouteAvailability(ChapterVariant _) => RefreshEndingRouteAvailability();

        private void RefreshEndingRouteAvailability()
        {
            foreach (var endingTrigger in endingTriggers)
            {
                endingTrigger?.RefreshAvailability();
            }

            var routesVisible = AreFinaleChoicesAvailable();
            foreach (var regionName in FinaleRouteRegionNames)
            {
                var region = transform.Find(regionName);
                if (region != null)
                {
                    region.gameObject.SetActive(routesVisible &&
                        EndingResolver.IsDirectionAvailable(FinaleDirectionFor(regionName), sceneNarrativeState.State));
                }
            }
        }

        private static EndingDirection FinaleDirectionFor(string regionName) => regionName switch
        {
            "Finale Car Region" => EndingDirection.Northbound,
            "Finale Home Region" => EndingDirection.HomeChosen,
            "Finale Road Region" => EndingDirection.NoMap,
            _ => EndingDirection.PauseJourney
        };

        private void CreateCinematicRoute(string objectName, Vector2 position, string cinematicId, string prompt)
        {
            var zone = new GameObject(objectName);
            zone.transform.SetParent(transform);
            zone.transform.position = position;
            var collider = zone.AddComponent<CircleCollider2D>();
            collider.radius = 1.3f;
            collider.isTrigger = true;
            zone.AddComponent<CinematicRouteTrigger>().Configure(cinematicId, prompt);
        }

        private void CreateBoundary(string objectName, Vector2 position, Vector2 size)
        {
            var boundary = new GameObject(objectName);
            boundary.transform.SetParent(transform);
            boundary.transform.position = position;
            var collider = boundary.AddComponent<BoxCollider2D>();
            collider.size = size;
        }

        private void RegisterMarkers(ChapterWorldController controller)
        {
            foreach (var markerName in new[]
            {
                "Open Diner", "Open Market", "FINAL WEEK", "Dark Storefronts", "North Poster Torn", "Garage Countdown",
                "Finale Car Region", "Finale Home Region", "Finale Road Region", "Finale Friends Region",
                "Finale Maya Region", "Finale Noah Region", "Finale Leo Region"
            })
            {
                var marker = transform.Find(markerName);
                if (marker != null)
                {
                    controller.RegisterWorldObject(markerName, marker.gameObject);
                }
            }
        }

        private WorldFactBinding[] CreateFactBindings()
        {
            var exhibitionClosed = CreateMarkerForFact("Maya Exhibition Closed", new Vector2(11f, 5f), new Color(0.22f, 0.14f, 0.18f));
            var radioReturned = CreateMarkerForFact("Noah Equipment Returned", new Vector2(10f, -2f), new Color(0.2f, 0.18f, 0.15f));
            var garageDark = CreateMarkerForFact("Garage Dark", new Vector2(-22f, -5f), new Color(0.04f, 0.05f, 0.07f));
            var packedTrunk = CreateMarkerForFact("Packed Trunk Trace", new Vector2(-18f, -5f), new Color(0.34f, 0.27f, 0.18f));
            var roadTest = CreateMarkerForFact("Road Test Trace", new Vector2(-16f, -3f), new Color(0.22f, 0.19f, 0.14f));
            var lastNight = CreateMarkerForFact("Last Night Open Trace", new Vector2(-8f, 1f), new Color(0.11f, 0.09f, 0.08f));
            return new[]
            {
                CreateFactBinding(exhibitionClosed, "missed_first_light"),
                CreateFactBinding(radioReturned, "missed_static"),
                CreateFactBinding(garageDark, "missed_alternator"),
                CreateFactBinding(packedTrunk, "missed_pack_trunk"),
                CreateFactBinding(roadTest, "missed_road_test"),
                CreateFactBinding(lastNight, "missed_last_night_open")
            };
        }

        private void CreateGuidance()
        {
            var hud = GuidanceHudView.Create();
            guidance = gameObject.AddComponent<GuidanceController>();
            guidance.Configure(sceneNarrativeState, contentDirector, GetComponent<GameFlowController>(), hud);
        }

        private void CreateFinaleGathering()
        {
            var gathering = new GameObject("Finale Gathering");
            gathering.transform.SetParent(transform);
            gathering.transform.position = new Vector3(0f, 0f, -1f);
            var collider = gathering.AddComponent<CircleCollider2D>();
            collider.radius = 1.8f;
            collider.isTrigger = true;

            var castRoot = new GameObject("Greybridge Friends");
            castRoot.transform.SetParent(gathering.transform, false);
            var artCatalog = Resources.Load<NorthboundArtCatalog>("Northbound/NorthboundArtCatalog");
            var wagon = new GameObject("Finale Wagon");
            wagon.transform.SetParent(castRoot.transform, false);
            wagon.transform.localPosition = new Vector3(0f, -1.35f, .25f);
            var wagonRenderer = wagon.AddComponent<SpriteRenderer>();
            wagonRenderer.sprite = artCatalog != null ? artCatalog.StationWagon() : null;
            if (artCatalog != null)
            {
                GreybridgeArtBuilder.ApplyKeyMaterial(wagonRenderer, artCatalog.PropKeyColor("station_wagon"));
            }
            wagonRenderer.sortingOrder = 8;
            if (wagonRenderer.sprite != null)
            {
                var size = wagonRenderer.sprite.bounds.size;
                wagon.transform.localScale = new Vector3(4.5f / size.x, 2.6f / size.y, 1f);
            }

            var catalog = Resources.Load<NarrativeContentCatalog>("Northbound/NarrativeContentCatalog");
            var cast = new[]
            {
                (id: "elias", offset: new Vector2(-3f, -.8f)),
                (id: "maya", offset: new Vector2(-1.4f, 1.1f)),
                (id: "noah", offset: new Vector2(1.4f, 1.1f)),
                (id: "leo", offset: new Vector2(3f, -.8f))
            };
            foreach (var member in cast)
            {
                var prefab = catalog != null ? catalog.CharacterPrefab(member.id) : null;
                var actor = prefab != null ? Instantiate(prefab) : new GameObject($"Finale {member.id}");
                actor.name = $"Finale {member.id}";
                actor.transform.SetParent(castRoot.transform, false);
                actor.transform.localPosition = new Vector3(member.offset.x, member.offset.y, -.5f);
                EnsureVisibleCharacter(actor, member.id);
            }

            gathering.AddComponent<FinaleGatheringInteractor>().Configure(
                contentDirector,
                GetComponent<GameFlowController>(),
                sceneNarrativeState,
                castRoot);
            guidance?.RegisterTarget("finale_gathering", gathering.transform, MarkerKind.Required);
        }

        private void CreateNpcAnchors()
        {
            foreach (var definition in npcAnchors)
            {
                if (definition.characterId == "jamie") continue;
                if (string.IsNullOrWhiteSpace(definition.characterId) || string.IsNullOrWhiteSpace(definition.locationId))
                {
                    continue;
                }

                var catalog = Resources.Load<NarrativeContentCatalog>("Northbound/NarrativeContentCatalog");
                var prefab = catalog != null ? catalog.CharacterPrefab(definition.characterId) : null;
                var anchorObject = prefab != null ? Instantiate(prefab) : new GameObject($"NPC Anchor {definition.characterId}");
                anchorObject.name = $"NPC {definition.characterId}";
                anchorObject.transform.SetParent(transform);
                anchorObject.transform.position = definition.position;
                EnsureVisibleCharacter(anchorObject, definition.characterId);
                anchorObject.AddComponent<GreybridgeNpcAnchor>().Configure(definition.characterId, definition.locationId);
                if (definition.characterId != "jamie" && anchorObject.GetComponent<NarrativeCharacterInteractor>() == null)
                {
                    var collider = anchorObject.AddComponent<CircleCollider2D>();
                    collider.radius = 0.75f;
                    collider.isTrigger = true;
                    anchorObject.AddComponent<NarrativeCharacterInteractor>().Configure(
                        definition.characterId,
                        $"optional_{definition.characterId}_{(definition.characterId == "elias" ? "garage" : definition.characterId == "maya" ? "mural" : definition.characterId == "noah" ? "radio" : "diner")}_trigger",
                        contentDirector);
                }
            }
        }

        private void CreateContentRoutes()
        {
            if (contentDirector == null || contentDirector.Manifest == null)
            {
                return;
            }

            foreach (var route in contentDirector.Manifest.triggers)
            {
                if (route == null || string.IsNullOrWhiteSpace(route.id))
                {
                    continue;
                }

                var triggerObject = new GameObject($"Narrative Route {route.id}");
                triggerObject.transform.SetParent(transform);
                triggerObject.transform.position = RoutePosition(route.targetId, route.routeType);
                var collider = triggerObject.AddComponent<CircleCollider2D>();
                collider.radius = 0.75f;
                collider.isTrigger = true;
                triggerObject.AddComponent<NarrativeRouteTrigger>().Configure(route.id, RoutePrompt(route.routeType), contentDirector);
                guidance?.RegisterTarget(route.id, triggerObject.transform, MarkerKind.Required);
            }
        }

        private void CreateObjectiveInteractions()
        {
            if (contentDirector?.Manifest == null) return;
            var catalog = Resources.Load<NarrativeContentCatalog>("Northbound/NarrativeContentCatalog");
            foreach (var quest in contentDirector.Manifest.quests)
            {
                var asset = catalog?.Quest(quest.id);
                if (quest == null || asset?.objectives == null || quest.completionMode != "physical") continue;
                for (var index = 0; index < asset.objectives.Count; index++)
                {
                    var objective = asset.objectives[index];
                    if (quest.id == "things_we_leave")
                    {
                        CreateCarriedObjectChoice(quest.id, objective.id, "Photograph", "carried_photo", new Vector2(-3.2f, -1.2f));
                        CreateCarriedObjectChoice(quest.id, objective.id, "Notebook", "carried_notebook", new Vector2(-1.8f, -1.2f));
                        CreateCarriedObjectChoice(quest.id, objective.id, "House Key", "carried_house_key", new Vector2(-3.2f, -2.5f));
                        CreateCarriedObjectChoice(quest.id, objective.id, "Old Map", "carried_old_map", new Vector2(-1.8f, -2.5f));
                        continue;
                    }
                    var trigger = new GameObject($"Objective {quest.id} {objective.id}");
                    trigger.transform.SetParent(transform);
                    trigger.transform.position = ObjectivePosition(quest.id, objective.id, index);
                    var collider = trigger.AddComponent<CircleCollider2D>();
                    collider.isTrigger = true;
                    collider.radius = 0.5f;
                    trigger.AddComponent<NarrativeObjectiveTrigger>().Configure(
                        quest.id,
                        objective.id,
                        ObjectivePrompt(objective.id),
                        contentDirector,
                        index == 0 ? quest.minigameId : string.Empty,
                        dialogueRoute: FarewellRouteFor(objective.id));
                    artBuilder?.AttachQuestProp(trigger.transform, ObjectivePropIndex(objective.id));
                    trigger.AddComponent<ObjectivePropFeedback>().Configure(
                        QuestRunner.ObjectiveCompletionFactId(quest.id, objective.id), CompletionVisualMode.Hide, sceneNarrativeState);
                    guidance?.RegisterTarget($"{quest.id}:{objective.id}", trigger.transform, MarkerKind.Required);
                }
            }
        }

        private void CreateCarriedObjectChoice(string questId, string objectiveId, string label, string fact, Vector2 position)
        {
            var trigger = new GameObject($"Carry {label}");
            trigger.transform.SetParent(transform);
            trigger.transform.position = position;
            var collider = trigger.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.45f;
            trigger.AddComponent<NarrativeObjectiveTrigger>().Configure(questId, objectiveId, $"Carry {label}", contentDirector, "", fact);
            artBuilder?.AttachQuestProp(trigger.transform, CarryPropIndex(fact));
            trigger.AddComponent<ObjectivePropFeedback>().Configure(
                QuestRunner.ObjectiveCompletionFactId(questId, objectiveId), CompletionVisualMode.Hide, sceneNarrativeState);
            guidance?.RegisterTarget($"{questId}:{objectiveId}", trigger.transform, MarkerKind.Required);
        }

        private static int CarryPropIndex(string fact) => fact switch
        {
            "carried_photo" => 12,
            "carried_notebook" => 13,
            "carried_house_key" => 14,
            "carried_old_map" => 15,
            _ => 13
        };

        private static string FarewellRouteFor(string objectiveId) => objectiveId switch
        {
            "visit_maya" => "farewell_maya_trigger",
            "visit_noah" => "farewell_noah_trigger",
            "visit_leo" => "farewell_leo_trigger",
            _ => string.Empty
        };

        private static int ObjectivePropIndex(string objectiveId) => objectiveId switch
        {
            "find_socket" => 0,
            "fit_battery" or "test_charge" => 1,
            "collect_belt" or "connect_belt" => 2,
            "collect_fuses" => 3,
            "collect_toolbox" or "return_garage" => 4,
            "hang_painting" => 5,
            "set_lights" or "open_exhibition" => 6,
            "lift_alternator" => 7,
            "wire_recorder" or "carry_recorder" or "record_goodbye" => 8,
            "deliver_radio_case" => 9,
            "serve_tables" or "close_diner" => 10,
            "drive_service_road" or "push_wagon" or "pack_trunk" => 11,
            "count_inventory" => 4,
            "remove_sign" => 5,
            "find_key" => 14,
            _ => -1
        };

        private static Vector2 ObjectivePosition(string questId, string objectiveId, int index)
        {
            if (questId == "before_morning")
            {
                return objectiveId switch
                {
                    "visit_maya" => new Vector2(18.5f, 2.5f),
                    "visit_noah" => new Vector2(10f, -3.2f),
                    "visit_leo" => new Vector2(-7.4f, .5f),
                    _ => RoutePosition(questId, "quest")
                };
            }

            var offset = questId switch
            {
                "clock_in" => new Vector2(-1f, 0.8f), "missing_socket" => new Vector2(0.8f, -0.8f), "parts_future" => new Vector2(0.6f, 0.9f),
                "rooftop_inventory" => new Vector2(-0.9f, -0.5f), "last_sign" => new Vector2(0.8f, 0.2f), "dead_air" => new Vector2(-0.8f, 0.5f),
                "one_more_table" => new Vector2(0.7f, -0.6f), "alternator" => new Vector2(0.9f, 0.7f), "first_light" => new Vector2(-0.8f, -0.6f),
                "road_test" => new Vector2(1.1f, 0.4f), "static" => new Vector2(-0.8f, -0.1f), "pack_trunk" => new Vector2(0.5f, 1.0f),
                "last_night_open" => new Vector2(-0.8f, -0.8f), "things_we_leave" => new Vector2(0.8f, 0.8f), "spare_key" => new Vector2(-0.8f, 0.7f), _ => new Vector2(0.7f, -0.7f)
            };
            return RoutePosition(questId, "quest") + offset + new Vector2(index * 0.65f, index % 2 == 0 ? 0f : 0.55f);
        }

        private static string ObjectivePrompt(string id) => GameText.ObjectivePrompt(id);

        private static Vector2 RoutePosition(string targetId, string routeType)
        {
            switch (targetId)
            {
                case "clock_in": return new Vector2(-7f, 3f); case "one_more_table": return new Vector2(-5f, 3f); case "last_night_open": return new Vector2(-3f, 3f); case "leo": return new Vector2(-1f, 3f);
                case "missing_socket": return new Vector2(-20f, -4f); case "alternator": return new Vector2(-18f, -4f); case "road_test": return new Vector2(-16f, -4f); case "pack_trunk": return new Vector2(-14f, -4f); case "spare_key": return new Vector2(-12f, -4f); case "elias": return new Vector2(-10f, -4f);
                case "parts_future": return new Vector2(11f, 5f); case "last_sign": return new Vector2(13f, 5f); case "first_light": return new Vector2(15f, 5f); case "maya": return new Vector2(17f, 5f);
                case "dead_air": return new Vector2(7.5f, -3.3f); case "static": return new Vector2(9f, -3.3f); case "noah": return new Vector2(13.8f, -3.4f);
                case "rooftop_inventory": return new Vector2(23f, 9f); case "rooftop": return new Vector2(21f, 9f); case "before_morning": return new Vector2(19f, 9f);
                case "things_we_leave": return new Vector2(-2f, 0f);
                case "opening": return new Vector2(20f, 8f);
                case "finale": return new Vector2(0f, -4f);
                case "optional_elias_garage": return new Vector2(-22f, -7f); case "optional_maya_mural": return new Vector2(8f, 7f); case "optional_noah_radio": return new Vector2(7f, -4f); case "optional_leo_diner": return new Vector2(-9f, 5f);
                case "farewell_elias": return new Vector2(-24f, -2f); case "farewell_maya": return new Vector2(19f, 7f); case "farewell_noah": return new Vector2(15f, -4f); case "farewell_leo": return new Vector2(-11f, 1f);
                case "missed_alternator": return new Vector2(-23f, -6f); case "missed_first_light": return new Vector2(10f, 7f); case "missed_road_test": return new Vector2(-15f, -6f); case "missed_static": return new Vector2(10f, -4f); case "missed_pack_trunk": return new Vector2(-13f, -6f); case "missed_last_night_open": return new Vector2(-7f, 1f);
                case "npc_ruth": return new Vector2(-9f, 1f); case "npc_market": return new Vector2(4f, 4f); case "npc_rooftop": return new Vector2(22f, 7f); case "return_to_title": return new Vector2(0f, -10f);
                default: return routeType == "dialogue" ? new Vector2(2f, 2f) : new Vector2(0f, 0f);
            }
        }

        private static string RoutePrompt(string routeType) => routeType == "quest" ? "Begin mission" : routeType == "cinematic" ? "Watch memory" : "Talk";

        private GameObject CreateMarkerForFact(string objectName, Vector2 position, Color color)
        {
            var marker = CreateBox(objectName, position, new Vector2(2f, 1.4f), color, false);
            marker.SetActive(false);
            return marker;
        }

        private WorldFactBinding CreateFactBinding(GameObject target, string requiredFact)
        {
            var binding = gameObject.AddComponent<WorldFactBinding>();
            binding.Configure(target, new[] { requiredFact }, new string[0]);
            return binding;
        }

        private GameObject CreatePlayerAndCamera()
        {
            var catalog = Resources.Load<NarrativeContentCatalog>("Northbound/NarrativeContentCatalog");
            var prefab = catalog != null ? catalog.CharacterPrefab("jamie") : null;
            var player = prefab != null ? Instantiate(prefab) : CreateBox("Jamie", new Vector2(-6f, 0f), new Vector2(0.7f, 0.7f), new Color(0.93f, 0.77f, 0.55f), false);
            player.name = "Jamie";
            player.transform.SetParent(transform);
            player.transform.position = new Vector3(-6f, 0f, -2f);
            EnsureVisibleCharacter(player, "jamie");
            // Jamie is a real character prefab, but remains the stable world anchor
            // used by location-aware systems and saves.
            var jamieAnchor = player.GetComponent<GreybridgeNpcAnchor>() ?? player.AddComponent<GreybridgeNpcAnchor>();
            jamieAnchor.Configure("jamie", "old_neighborhood");
            var body = player.GetComponent<Rigidbody2D>() ?? player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            var collider = player.GetComponent<CircleCollider2D>() ?? player.AddComponent<CircleCollider2D>();
            collider.radius = 0.35f;
            var motor = player.GetComponent<PlayerMotor>() ?? player.AddComponent<PlayerMotor>();
            var interactor = player.GetComponent<PlayerInteractor>() ?? player.AddComponent<PlayerInteractor>();
            interactor.SetPromptView(EnsureReadableInteractionPrompt());
            if (GameBootstrap.Instance != null)
            {
                motor.SetInputGate(GameBootstrap.Instance.InputGate);
                interactor.SetInputGate(GameBootstrap.Instance.InputGate);
            }

            var camera = Camera.main;
            if (camera != null)
            {
                var follow = camera.gameObject.GetComponent<FollowCamera>() ?? camera.gameObject.AddComponent<FollowCamera>();
                follow.SetTarget(player.transform);
                follow.SetReducedMotionProvider(() => GameBootstrap.Instance != null && GameBootstrap.Instance.Settings.ReducedMotion);
                var bounds = camera.gameObject.GetComponent<GreybridgeCameraBounds>() ?? camera.gameObject.AddComponent<GreybridgeCameraBounds>();
                bounds.Configure(WalkableBounds);
            }
            return player;
        }

        private void CreateLocations(GameObject player)
        {
            var controller = gameObject.AddComponent<LocationTransitionController>();
            var gate = GameBootstrap.Instance != null ? GameBootstrap.Instance.InputGate : gameObject.AddComponent<InputGate>();
            var follow = Camera.main != null ? Camera.main.GetComponent<FollowCamera>() : null;
            controller.Configure(player.transform, gate, follow, LocationFadeView.Create());

            var locations = new Dictionary<string, GameObject>();
            var walkableBoundsByLocation = new Dictionary<string, Bounds>();
            RegisterLocation("exterior", "Greybridge", new Vector2(-6f, 0f), new Bounds(Vector3.zero, new Vector3(52, 24, 1)), new Bounds(WalkableBounds.center, WalkableBounds.size), 5f);
            RegisterLocation("jamie_home", "Jamie's Home", new Vector2(-2f, 0f), RoomBounds(-2, 0), RoomWalkableBounds(-2, 0), 6.75f);
            RegisterLocation("vale_garage", "Vale Auto Garage", new Vector2(-20f, -4f), RoomBounds(-20, -4), RoomWalkableBounds(-20, -4), 6.75f);
            RegisterLocation("ruths_diner", "Ruth's Diner", new Vector2(-7f, 3f), RoomBounds(-7, 3), RoomWalkableBounds(-7, 3), 6.75f);
            RegisterLocation("maya_studio", "Maya's Studio", new Vector2(13f, 5f), RoomBounds(13, 5), RoomWalkableBounds(13, 5), 6.75f);
            RegisterLocation("noah_electronics", "Noah's Electronics", new Vector2(7.5f, -4f), RoomBounds(10, -2), RoomWalkableBounds(10, -2), 6.75f);
            RegisterLocation("rooftop_overlook", "Rooftop Overlook", new Vector2(23f, 9f), RoomBounds(23, 9), RoomWalkableBounds(23, 9), 6.75f);

            Move("Art Street", "exterior");
            Move("Art Jamie Home", "jamie_home");
            Move("Art Garage", "vale_garage");
            Move("Art Diner", "ruths_diner");
            Move("Art Gallery", "maya_studio");
            Move("Art Electronics", "noah_electronics");
            Move("Art Rooftop", "rooftop_overlook");
            Move("Finale Gathering", "exterior");

            foreach (var item in GetComponentsInChildren<NarrativeRouteTrigger>(true)) MoveByStory(item.gameObject);
            foreach (var item in GetComponentsInChildren<NarrativeObjectiveTrigger>(true)) MoveByStory(item.gameObject);
            foreach (var item in GetComponentsInChildren<GreybridgeNpcAnchor>(true))
            {
                if (item.gameObject == player) continue;
                Move(item.gameObject.name, item.CharacterId switch { "elias" => "vale_garage", "maya" => "maya_studio", "noah" => "noah_electronics", "leo" => "ruths_diner", _ => "exterior" });
            }

            // The rooftop conversation is a shared scene, so its participants need
            // real, visible bodies even though their stable homes remain elsewhere.
            CreateRooftopStoryCast(locations["rooftop_overlook"].transform);

            // Entrances sit on the authored facade/threshold pixels, not on the
            // nearest route marker or an arbitrary point beside the spawn.
            CreateEntrance("Garage Entrance", new Vector2(-14.1f, 0.3f), "[E] Enter Vale Auto Garage", "vale_garage", "Vale Auto Garage");
            CreateEntrance("Diner Entrance", new Vector2(-2f, 5.2f), "[E] Enter Ruth's Diner", "ruths_diner", "Ruth's Diner");
            CreateEntrance("Home Entrance", new Vector2(8.2f, 4.4f), "[E] Enter Jamie's Home", "jamie_home", "Jamie's Home");
            CreateEntrance("Studio Entrance", new Vector2(15.2f, 1.4f), "[E] Enter Maya's Studio", "maya_studio", "Maya's Studio");
            CreateEntrance("Electronics Entrance", new Vector2(21.7f, 1.4f), "[E] Enter Noah's Electronics", "noah_electronics", "Noah's Electronics");
            CreateEntrance("Rooftop Entrance", new Vector2(6.5f, -6.2f), "[E] Climb to Rooftop Overlook", "rooftop_overlook", "Rooftop Overlook");

            foreach (var id in new[] { "jamie_home", "vale_garage", "ruths_diner", "maya_studio", "noah_electronics", "rooftop_overlook" })
            {
                var exit = CreateDoor($"Exit {id}", locations[id].transform, RoomDoorPosition(id), "[E] Return to Greybridge", "exterior");
                guidance?.RegisterTarget($"exit:{id}", exit.transform, MarkerKind.Required);
            }

            ConfigureMissionStartZones();

            controller.SetInitial("exterior");
            guidance?.BindLocationController(controller);
            return;

            void CreateEntrance(string name, Vector2 position, string prompt, string destination, string displayName)
            {
                var door = CreateDoor(name, locations["exterior"].transform, position, prompt, destination);
                var plaque = DoorNamePlaque.Create(door.transform, destination, displayName);
                guidance?.RegisterDoorPlaque(destination, plaque);
                guidance?.RegisterTarget($"entrance:{destination}", door.transform, MarkerKind.Required);
            }

            void RegisterLocation(string id, string displayName, Vector2 spawnPosition, Bounds bounds, Bounds walkable, float cameraSize)
            {
                var root = new GameObject($"Location {id}");
                root.transform.SetParent(transform, false);
                var spawn = new GameObject("Spawn").transform;
                spawn.SetParent(root.transform, false);
                spawn.position = new Vector3(spawnPosition.x, spawnPosition.y, -2f);
                locations[id] = root;
                walkableBoundsByLocation[id] = walkable;
                controller.Register(new LocationDefinition(id, root, spawn, bounds, walkable, cameraSize, displayName));
            }

            void ConfigureMissionStartZones()
            {
                foreach (var route in GetComponentsInChildren<NarrativeRouteTrigger>(true))
                {
                    var definition = contentDirector?.Manifest?.FindTrigger(route.RouteId);
                    var roomRoot = route.transform.parent;
                    if (definition == null || definition.routeType != "quest" || roomRoot == null ||
                        !roomRoot.name.StartsWith("Location ", System.StringComparison.Ordinal)) continue;

                    var locationId = roomRoot.name.Substring("Location ".Length);
                    if (locationId == "exterior" || !walkableBoundsByLocation.TryGetValue(locationId, out var walkable)) continue;
                    var zone = route.GetComponent<RoomMissionStartZone>() ?? route.gameObject.AddComponent<RoomMissionStartZone>();
                    zone.Configure(walkable, RoomDoorPosition(locationId));
                }
            }

            void Move(string objectName, string locationId)
            {
                var target = transform.Find(objectName);
                if (target != null && locations.TryGetValue(locationId, out var root)) target.SetParent(root.transform, true);
            }

            void MoveByStory(GameObject storyObject)
            {
                var objective = storyObject.GetComponent<NarrativeObjectiveTrigger>();
                var storyId = objective != null && objective.QuestId == "before_morning" && !string.IsNullOrWhiteSpace(objective.ObjectiveId)
                    ? objective.ObjectiveId
                    : objective != null && !string.IsNullOrWhiteSpace(objective.QuestId)
                        ? objective.QuestId
                    : storyObject.name;
                var lower = storyId.ToLowerInvariant();
                var location = lower.Contains("clock_in") || lower.Contains("one_more_table") || lower.Contains("last_night_open") || lower.Contains("leo") ? "ruths_diner" :
                    lower.Contains("missing_socket") || lower.Contains("alternator") || lower.Contains("road_test") || lower.Contains("pack_trunk") || lower.Contains("spare_key") || lower.Contains("elias") ? "vale_garage" :
                    lower.Contains("parts_future") || lower.Contains("last_sign") || lower.Contains("first_light") || lower.Contains("maya") ? "maya_studio" :
                    lower.Contains("dead_air") || lower.Contains("static") || lower.Contains("noah") ? "noah_electronics" :
                    lower.Contains("rooftop") || lower.Contains("before_morning") ? "rooftop_overlook" :
                    lower.Contains("things_we_leave") ? "jamie_home" : "exterior";
                storyObject.transform.SetParent(locations[location].transform, true);
            }

            GameObject CreateDoor(string name, Transform parent, Vector2 position, string prompt, string destination)
            {
                var door = new GameObject(name);
                door.transform.SetParent(parent, true);
                door.transform.position = new Vector3(position.x, position.y, -1f);
                var collider = door.AddComponent<BoxCollider2D>();
                collider.isTrigger = true; collider.size = new Vector2(1.2f, 1.8f);
                door.AddComponent<DoorInteractor>().Configure(prompt, destination, controller);
                return door;
            }
        }

        private static Bounds RoomBounds(float x, float y) => new Bounds(new Vector3(x, y, 0), new Vector3(24f, 13.5f, 1));

        private static Bounds RoomWalkableBounds(float x, float y)
        {
            // These are deliberately tighter than the plate rectangle. The authored
            // images have dark perspective corners; movement must stop on the visible
            // floor instead of letting Jamie walk into an unpainted margin.
            if (Mathf.Approximately(x, -2f) && Mathf.Approximately(y, 0f))
            {
                return new Bounds(new Vector3(-2f, -2.25f, 0f), new Vector3(18f, 6.5f, 1f));
            }
            if (Mathf.Approximately(x, -20f) && Mathf.Approximately(y, -4f))
            {
                return new Bounds(new Vector3(-20f, -5.3f, 0f), new Vector3(19.5f, 6.8f, 1f));
            }
            if (Mathf.Approximately(x, -7f) && Mathf.Approximately(y, 3f))
            {
                return new Bounds(new Vector3(-6.5f, 1.7f, 0f), new Vector3(15f, 7f, 1f));
            }
            if (Mathf.Approximately(x, 13f) && Mathf.Approximately(y, 5f))
            {
                return new Bounds(new Vector3(13f, 5f, 0f), new Vector3(18f, 7.5f, 1f));
            }
            if (Mathf.Approximately(x, 10f) && Mathf.Approximately(y, -2f))
            {
                return new Bounds(new Vector3(10.25f, -4f, 0f), new Vector3(20.5f, 7f, 1f));
            }
            if (Mathf.Approximately(x, 23f) && Mathf.Approximately(y, 9f))
            {
                return new Bounds(new Vector3(23.5f, 8.6f, 0f), new Vector3(18f, 7.2f, 1f));
            }
            return new Bounds(new Vector3(x, y, 0), new Vector3(18f, 7f, 1f));
        }

        private static Vector2 RoomDoorPosition(string locationId) => locationId switch
        {
            "jamie_home" => new Vector2(6.8f, 1.7f),
            "vale_garage" => new Vector2(-25.8f, -3.6f),
            "ruths_diner" => new Vector2(-3.3f, -1.1f),
            "maya_studio" => new Vector2(22f, 2.6f),
            "noah_electronics" => new Vector2(12f, -1.1f),
            "rooftop_overlook" => new Vector2(15f, 10.5f),
            _ => Vector2.zero
        };

        private static void CreateRooftopStoryCast(Transform rooftopRoot)
        {
            var cast = new[]
            {
                (id: "elias", position: new Vector2(20f, 9.5f)),
                (id: "leo", position: new Vector2(23f, 9f)),
                (id: "maya", position: new Vector2(26f, 9.5f))
            };

            foreach (var member in cast)
            {
                var actor = new GameObject($"Rooftop {member.id}");
                actor.transform.SetParent(rooftopRoot, true);
                actor.transform.position = new Vector3(member.position.x, member.position.y, -2f);
                EnsureVisibleCharacter(actor, member.id);
            }
        }

        private InteractionPromptView EnsureReadableInteractionPrompt()
        {
            var existing = FindFirstObjectByType<InteractionPromptView>();
            if (existing != null)
            {
                return existing;
            }

            var canvasObject = new GameObject("Greybridge Interaction Prompt", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var backing = new GameObject("Prompt Backing", typeof(RectTransform), typeof(Image));
            backing.transform.SetParent(canvasObject.transform, false);
            var backingRect = backing.GetComponent<RectTransform>();
            backingRect.anchorMin = backingRect.anchorMax = new Vector2(.5f, .12f);
            backingRect.sizeDelta = new Vector2(760f, 94f);
            backing.GetComponent<Image>().color = new Color(.025f, .04f, .07f, .88f);

            var labelObject = new GameObject("Prompt Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(backing.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 36;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, .92f, .63f, 1f);

            var prompt = canvasObject.AddComponent<InteractionPromptView>();
            prompt.SetPromptLabel(label);
            return prompt;
        }

        private GameObject CreateBox(string objectName, Vector2 position, Vector2 size, Color color, bool collider)
        {
            var box = new GameObject(objectName);
            box.name = objectName;
            box.transform.SetParent(transform);
            box.transform.position = new Vector3(position.x, position.y, 0f);
            box.transform.localScale = new Vector3(size.x, size.y, 1f);

            if (collider)
            {
                box.AddComponent<BoxCollider2D>().size = Vector2.one;
            }

            return box;
        }

        private static void EnsureVisibleCharacter(GameObject character, string characterId)
        {
            var catalog = Resources.Load<NorthboundArtCatalog>("Northbound/NorthboundArtCatalog");
            if (catalog == null) return;
            var visual = character.GetComponent<TopDownCharacterVisual>();
            if (visual == null)
            {
                visual = character.AddComponent<TopDownCharacterVisual>();
            }
            visual.Configure(characterId, catalog);
        }
    }
}
