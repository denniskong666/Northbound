using System;
using System.Collections.Generic;
using System.Linq;
using Northbound.Content;
using Northbound.Core;
using Northbound.Narrative;
using Northbound.Quests;
using Northbound.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using Northbound.World;
using Northbound.UI;
using Northbound.Endings;

namespace Northbound.Guidance
{
    public sealed class GuidanceController : MonoBehaviour
    {
        private readonly Dictionary<string, List<ObjectiveMarker>> targets = new Dictionary<string, List<ObjectiveMarker>>();
        private readonly Dictionary<string, DoorNamePlaque> doorPlaques = new Dictionary<string, DoorNamePlaque>();
        private NarrativeStateStore state;
        private NarrativeContentDirector director;
        private GameFlowController flow;
        private GuidanceHudView view;
        private LocationTransitionController locationController;
        private string currentLocationId = "exterior";
        private bool hasPresentationVisibility;
        private bool presentationVisible;
        private string observedActiveQuestId;
        private string lastCompletionNoticeQuestId;
        private Transform player;

        public GuidanceStep CurrentStep { get; private set; }
        public string CurrentDestinationId => ResolveNavigationTarget(CurrentStep, currentLocationId);

        public void Configure(NarrativeStateStore narrativeState, NarrativeContentDirector content, GameFlowController gameFlow, GuidanceHudView hud)
        {
            if (state != null) state.Changed -= Refresh;
            state = narrativeState;
            director = content;
            flow = gameFlow;
            view = hud;
            if (state != null) state.Changed += Refresh;
            if (flow != null) flow.ChapterEntered += OnChapterEntered;
            GameText.LanguageChanged -= Refresh;
            GameText.LanguageChanged += Refresh;
            Refresh();
        }

        public void BindLocationController(LocationTransitionController controller)
        {
            if (locationController != null) locationController.LocationChanged -= OnLocationChanged;
            locationController = controller;
            if (locationController != null)
            {
                locationController.LocationChanged += OnLocationChanged;
                if (!string.IsNullOrWhiteSpace(locationController.CurrentLocationId))
                    currentLocationId = locationController.CurrentLocationId;
            }
            Refresh();
        }

        public void RegisterTarget(string targetId, Transform target, MarkerKind kind)
        {
            if (string.IsNullOrWhiteSpace(targetId) || target == null) return;
            var marker = target.GetComponent<ObjectiveMarker>() ?? target.gameObject.AddComponent<ObjectiveMarker>();
            marker.Configure(kind);
            if (!targets.TryGetValue(targetId, out var matchingTargets))
            {
                matchingTargets = new List<ObjectiveMarker>();
                targets[targetId] = matchingTargets;
            }
            if (!matchingTargets.Contains(marker)) matchingTargets.Add(marker);
            RefreshMarkers();
        }

        public void RegisterDoorPlaque(string locationId, DoorNamePlaque plaque)
        {
            if (string.IsNullOrWhiteSpace(locationId) || plaque == null) return;
            doorPlaques[locationId] = plaque;
            RefreshMarkers();
        }

        public void Refresh()
        {
            if (state == null || director == null) return;
            ObserveMissionProgress();
            CurrentStep = Resolve(state, director.Manifest, flow?.CurrentChapterId, director.ActiveQuestId, director.NextObjectiveId, currentLocationId);
            var objective = string.IsNullOrWhiteSpace(CurrentStep.objectiveId)
                ? GameText.Objective(CurrentStep.objective)
                : GameText.ObjectiveAction(CurrentStep.objectiveId);
            view?.Show(GameText.Location(CurrentStep.locationName), objective, ResolveNavigationAction(CurrentStep, currentLocationId));
            RefreshMarkers();
        }

        public static GuidanceStep Resolve(NarrativeStateStore state, NarrativeContentManifest manifest, string chapterId, string activeQuestId,
            string activeObjectiveId, string currentLocationId = "exterior")
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!state.Has("tutorial_moved"))
                return Step(chapterId, "Greybridge", "Find your footing", "Move with WASD or the arrow keys.", "MOVE: Use WASD or arrow keys.", string.Empty, "exterior");

            if (chapterId == "finale")
            {
                if (state.Has("ending_selected"))
                    return Step(chapterId, "Greybridge", "Journey complete", "The road you chose is now part of the story.", string.Empty, string.Empty, "exterior");
                if (!state.Has("cinematic_finale_complete"))
                    return Step(chapterId, "Greybridge", "Wait for the final memory", "The last memory is still unfolding.", "WAIT: Watch the final memory.", string.Empty, "exterior");
                if (!state.Has(FinaleGatheringInteractor.ReviewedFact))
                    return Step(chapterId, "Finale Gathering", "Meet at the wagon", "Find the gathered friends, then press E to review the routes your journey has left open.", "NEXT: Press E at the gathering point.", "finale_gathering", "exterior");
                var finaleRoutes = FinaleRouteSummary(state);
                return Step(chapterId, "Greybridge", "Choose your direction", finaleRoutes.instruction, finaleRoutes.action, string.Empty, "exterior");
            }

            if (!string.IsNullOrWhiteSpace(activeQuestId))
            {
                var title = Humanize(activeQuestId);
                var destination = string.IsNullOrWhiteSpace(activeObjectiveId) ? string.Empty : $"{activeQuestId}:{activeObjectiveId}";
                var objective = string.IsNullOrWhiteSpace(activeObjectiveId) ? title : GameText.ObjectivePrompt(activeObjectiveId);
                var locationTarget = activeObjectiveId is "visit_maya" or "visit_noah" or "visit_leo"
                    ? activeObjectiveId
                    : activeQuestId;
                return Step(chapterId, LocationFor(locationTarget), objective, "Follow the gold outline, then press E / Enter to interact.",
                    "NEXT: Press E at the gold star.", destination, LocationIdFor(locationTarget), objectiveId: activeObjectiveId);
            }

            var availableRoutes = manifest?.triggers?.Where(item => IsAvailableQuestRoute(state, manifest, item, chapterId)).ToArray()
                ?? Array.Empty<ContentTrigger>();
            var route = availableRoutes.FirstOrDefault(item => LocationIdFor(item.targetId) == currentLocationId)
                ?? availableRoutes.FirstOrDefault();
            var target = route?.targetId;
            var firstInteraction = !state.Has("tutorial_interacted");
            return Step(
                chapterId,
                LocationFor(target),
                string.IsNullOrWhiteSpace(target) ? "Meet the people of Greybridge" : $"Start {Humanize(target)}",
                "Follow the gold guide and press E / Enter to begin.",
                firstInteraction ? "INTERACT: Press E / Enter at the marked door." : "ENTER: Press E / Enter at the marked door.",
                route?.id ?? string.Empty,
                LocationIdFor(target),
                route != null && route.routeType == "quest");
        }

        public static string ResolveNavigationTarget(GuidanceStep step, string currentLocationId)
        {
            if (string.IsNullOrWhiteSpace(step.destinationId)) return string.Empty;
            var current = string.IsNullOrWhiteSpace(currentLocationId) ? "exterior" : currentLocationId;
            if (current == step.targetLocationId) return step.destinationId;
            return current == "exterior" ? $"entrance:{step.targetLocationId}" : $"exit:{current}";
        }

        public static string ResolveNavigationAction(GuidanceStep step, string currentLocationId)
        {
            if (string.IsNullOrWhiteSpace(step.destinationId)) return GameText.NavigationAction(step.nextAction);
            var current = string.IsNullOrWhiteSpace(currentLocationId) ? "exterior" : currentLocationId;
            if (current == step.targetLocationId && step.isMissionStart) return GameText.T(
                "BEGIN: Press E / Enter anywhere inside the room. The door remains the exit.",
                "开始：在房间内按 E / 回车即可开始；门口只用于离开。");
            if (current == step.targetLocationId) return string.IsNullOrWhiteSpace(step.objectiveId)
                ? GameText.T(
                    "NEXT: Press E / Enter at the gold marker.",
                    "下一步：到金色标记处按 E / 回车。")
                : GameText.ObjectiveInstruction(step.objectiveId);
            if (current == "exterior" && step.nextAction.StartsWith("INTERACT:", StringComparison.Ordinal)) return GameText.T(
                $"INTERACT: Follow the gold arrow to {step.locationName}; press E / Enter at its door.",
                $"交互：跟随金色箭头前往{GameText.Location(step.locationName)}，到门口按 E / 回车。");
            if (current == "exterior") return GameText.T(
                $"ENTER: Go to {step.locationName}; press E / Enter at its door.",
                $"进入：前往{GameText.Location(step.locationName)}，到门口按 E / 回车。");
            return GameText.T(
                "EXIT: Go to the marked door and press E / Enter.",
                "离开：前往金色门口标记，按 E / 回车返回街道。");
        }

        public static string ResolveNavigationLabel(GuidanceStep step, string currentLocationId)
        {
            var targetId = ResolveNavigationTarget(step, currentLocationId);
            if (string.IsNullOrWhiteSpace(targetId)) return string.Empty;
            return targetId.StartsWith("exit:", StringComparison.Ordinal)
                ? GameText.Location("Greybridge")
                : GameText.Location(step.locationName);
        }

        private static bool IsAvailableQuestRoute(NarrativeStateStore state, NarrativeContentManifest manifest, ContentTrigger route, string chapterId)
        {
            if (route == null || route.routeType != "quest" ||
                (!string.IsNullOrWhiteSpace(chapterId) && route.chapterId != chapterId)) return false;
            var quest = manifest.FindQuest(route.targetId);
            if (quest == null || state.Has(QuestRunner.CompletionFact(quest.id))) return false;
            if (!(quest.prerequisiteFacts ?? Array.Empty<string>()).All(state.Has) ||
                !(route.prerequisiteFacts ?? Array.Empty<string>()).All(state.Has)) return false;
            return (quest.prerequisiteQuestIds ?? Array.Empty<string>())
                .All(id => IsQuestPrerequisiteSatisfied(state, manifest, id));
        }

        private static bool IsQuestPrerequisiteSatisfied(
            NarrativeStateStore state,
            NarrativeContentManifest manifest,
            string prerequisiteQuestId)
        {
            if (state.Has(QuestRunner.CompletionFact(prerequisiteQuestId))) return true;
            var prerequisite = manifest.FindQuest(prerequisiteQuestId);
            if (prerequisite == null || string.IsNullOrWhiteSpace(prerequisite.pairId)) return false;
            return (manifest.quests ?? Array.Empty<ContentQuest>())
                .Where(item => item != null && item.pairId == prerequisite.pairId)
                .Any(item => state.Has(QuestRunner.CompletionFact(item.id)));
        }

        private void Update()
        {
            RefreshPresentationVisibility();
            ObserveMissionProgress();
            ClearCompletionNoticeAtDoor();
            if (state == null || state.Has("tutorial_moved") || Keyboard.current == null) return;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed || Keyboard.current.sKey.isPressed ||
                Keyboard.current.dKey.isPressed || Keyboard.current.upArrowKey.isPressed || Keyboard.current.downArrowKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                state.Set("tutorial_moved", true);
        }

        private void RefreshMarkers()
        {
            presentationVisible = ShouldPresentGuidance();
            hasPresentationVisibility = true;
            var destinationId = CurrentDestinationId;
            foreach (var pair in targets)
                foreach (var marker in pair.Value)
                    marker.SetHighlighted(presentationVisible && pair.Key == destinationId);
            foreach (var pair in doorPlaques)
            {
                pair.Value.SetPresentationVisible(presentationVisible);
                pair.Value.SetHighlighted(destinationId == $"entrance:{pair.Key}");
            }
            view?.SetPresentationVisible(presentationVisible);
            view?.ShowDestination(
                presentationVisible && targets.TryGetValue(destinationId, out var matchingTargets) && matchingTargets.Count > 0
                    ? matchingTargets[0].transform
                    : null,
                ResolveNavigationLabel(CurrentStep, currentLocationId),
                destinationId == "finale_gathering");
        }

        private void RefreshPresentationVisibility()
        {
            var visible = ShouldPresentGuidance();
            if (!hasPresentationVisibility || visible != presentationVisible) RefreshMarkers();
        }

        private static bool ShouldPresentGuidance()
        {
            var bootstrap = GameBootstrap.Instance;
            return bootstrap == null ||
                (bootstrap.IsSessionActive && bootstrap.InputGate != null && !bootstrap.InputGate.IsBlocked);
        }

        private void OnDestroy()
        {
            if (state != null) state.Changed -= Refresh;
            if (flow != null) flow.ChapterEntered -= OnChapterEntered;
            if (locationController != null) locationController.LocationChanged -= OnLocationChanged;
            GameText.LanguageChanged -= Refresh;
        }

        private void OnChapterEntered(string _) => Refresh();
        private void OnLocationChanged(string locationId)
        {
            currentLocationId = locationId;
            if (view?.MissionCompletionVisible == true)
            {
                if (locationController?.IsTravelling == true) view.ClearMissionComplete();
                else view.UpdateMissionCompletionContext(locationId != "exterior");
            }
            Refresh();
        }

        private void ObserveMissionProgress()
        {
            var activeQuestId = director?.ActiveQuestId;
            if (!string.IsNullOrWhiteSpace(activeQuestId))
            {
                observedActiveQuestId = activeQuestId;
                return;
            }

            if (string.IsNullOrWhiteSpace(observedActiveQuestId) ||
                observedActiveQuestId == lastCompletionNoticeQuestId ||
                state == null || !state.Has(QuestRunner.CompletionFact(observedActiveQuestId))) return;

            lastCompletionNoticeQuestId = observedActiveQuestId;
            var completedQuestId = observedActiveQuestId;
            observedActiveQuestId = null;
            view?.ShowMissionComplete(Humanize(completedQuestId), currentLocationId != "exterior");
        }

        private void ClearCompletionNoticeAtDoor()
        {
            if (view?.MissionCompletionVisible != true || currentLocationId == "exterior" ||
                !targets.TryGetValue($"exit:{currentLocationId}", out var exits) || exits.Count == 0) return;
            player ??= FindFirstObjectByType<PlayerMotor>()?.transform;
            if (player != null && Vector2.Distance(player.position, exits[0].transform.position) <= 2.1f)
                view.ClearMissionComplete();
        }

        private static GuidanceStep Step(string chapter, string location, string objective, string instruction, string action, string destination,
            string targetLocation, bool missionStart = false, string objectiveId = "") => new GuidanceStep
        {
            chapter = Humanize(chapter), locationName = location, objective = objective, instruction = instruction, nextAction = action, destinationId = destination,
            targetLocationId = targetLocation, isMissionStart = missionStart, objectiveId = objectiveId ?? string.Empty
        };

        private static string LocationFor(string targetId)
        {
            return LocationIdFor(targetId) switch
            {
                "ruths_diner" => "Ruth's Diner",
                "vale_garage" => "Vale Auto Garage",
                "maya_studio" => "Maya's Studio",
                "noah_electronics" => "Noah's Electronics",
                "rooftop_overlook" => "Rooftop Overlook",
                "jamie_home" => "Jamie's Home",
                "finale_gathering" => "Finale Gathering",
                _ => "Greybridge"
            };
        }

        private static string LocationIdFor(string targetId)
        {
            var id = (targetId ?? string.Empty).Split(':')[0].Replace("route_", string.Empty);
            return id switch
            {
                "clock_in" or "one_more_table" or "last_night_open" or "leo" or "visit_leo" => "ruths_diner",
                "missing_socket" or "alternator" or "road_test" or "pack_trunk" or "spare_key" or "elias" => "vale_garage",
                "parts_future" or "last_sign" or "first_light" or "maya" or "visit_maya" => "maya_studio",
                "dead_air" or "static" or "noah" or "visit_noah" => "noah_electronics",
                "rooftop_inventory" or "rooftop" or "before_morning" => "rooftop_overlook",
                "things_we_leave" => "jamie_home",
                "finale_gathering" => "exterior",
                _ => "exterior"
            };
        }

        private static (string instruction, string action) FinaleRouteSummary(NarrativeStateStore state)
        {
            var northbound = EndingResolver.IsDirectionAvailable(EndingDirection.Northbound, state.State);
            var home = EndingResolver.IsDirectionAvailable(EndingDirection.HomeChosen, state.State);
            var english = new List<string>();
            var chinese = new List<string>();
            if (northbound) { english.Add("Southeast Northbound"); chinese.Add("东南向北公路"); }
            if (home) { english.Add("Southwest Home"); chinese.Add("西南留在故乡"); }
            english.Add("East No Map"); chinese.Add("向东无图之路");
            english.Add("Northeast Wait"); chinese.Add("东北等到天亮");
            var changed = !northbound || !home;
            return (
                GameText.T(
                    changed
                        ? "Your earlier choices have closed one direction. Follow one of the three visible route signs and keep moving toward it to confirm."
                        : "Follow one of the four visible route signs and keep moving toward it to confirm.",
                    changed
                        ? "此前的选择已经关闭了一个方向。走向三块可见路线牌之一，并继续前进以确认。"
                        : "走向四块可见路线牌之一，并继续前进以确认。"),
                GameText.T($"ROUTES: {string.Join(" | ", english)}.", $"路线：{string.Join(" | ", chinese)}。"));
        }

        private static string Humanize(string id) => string.IsNullOrWhiteSpace(id) ? "Greybridge" :
            System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('_', ' '));
    }
}
