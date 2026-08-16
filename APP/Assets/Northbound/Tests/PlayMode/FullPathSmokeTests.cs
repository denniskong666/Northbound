using System.Collections;
using System.Collections.Generic;
using System.IO;
using Guid = System.Guid;
using System.Linq;
using Northbound.Content;
using Northbound.Core;
using Northbound.Interaction;
using Northbound.Narrative;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Northbound.Dialogue;

namespace Northbound.Tests
{
    public sealed class FullPathSmokeTests
    {
        private static readonly string[] RequiredPairs =
        {
            "alternator|first_light",
            "road_test|static",
            "pack_trunk|last_night_open"
        };

        [TearDown]
        public void RestoreSessionSaveConfiguration()
        {
            GameBootstrap.SessionSaveGameFactory = null;
        }

        [Test]
        public void ContentManifest_RejectsDuplicateAndMissingReferencedAssets()
        {
            var manifest = NarrativeContentManifest.FromJson(@"{
                ""chapters"":[{""id"":""prologue"",""nextId"":""chapter_1""}],
                ""quests"":[{""id"":""clock_in"",""dialogueId"":""missing_dialogue"",""triggerId"":""clock_in_trigger""}, {""id"":""clock_in""}],
                ""dialogues"":[], ""triggers"":[], ""cinematics"":[], ""facts"":[], ""endings"":[]
            }");

            var errors = NarrativeContentValidator.Validate(manifest, new EmptyContentAssetSource());

            Assert.That(errors.Any(error => error.Contains("Duplicate quest id 'clock_in'")), Is.True);
            Assert.That(errors.Any(error => error.Contains("missing dialogue 'missing_dialogue'")), Is.True);
            Assert.That(errors.Any(error => error.Contains("missing trigger 'clock_in_trigger'")), Is.True);
        }

        [Test]
        public void ContentManifest_RejectsQuestWithoutChapterPrerequisiteAndCompletionMode()
        {
            var manifest = NarrativeContentManifest.FromJson(@"{
                ""chapters"":[{""id"":""prologue"",""nextId"":""finale""},{""id"":""finale""}],
                ""quests"":[{""id"":""clock_in"",""dialogueId"":""clock_in_dialogue"",""triggerId"":""clock_in_trigger""}],
                ""dialogues"":[{""id"":""clock_in_dialogue""}],
                ""triggers"":[{""id"":""clock_in_trigger"",""routeType"":""quest"",""targetId"":""clock_in""}],
                ""cinematics"":[], ""facts"":[], ""endings"":[], ""characters"":[]
            }");

            var errors = NarrativeContentValidator.Validate(manifest, new EmptyContentAssetSource());

            Assert.That(errors.Any(error => error.Contains("missing activation metadata")), Is.True);
        }

        [Test]
        public void ContentManifest_RejectsUnknownCompletionChoiceEndingAndCueReferences()
        {
            var manifest = NarrativeContentManifest.FromJson("{\"chapters\":[{\"id\":\"prologue\",\"nextId\":\"finale\"},{\"id\":\"finale\"}]," +
                "\"quests\":[{\"id\":\"q\",\"chapterId\":\"prologue\",\"dialogueId\":\"d\",\"triggerId\":\"t\",\"completionMode\":\"physical\",\"completionFacts\":[\"missing_completion_fact\"]}]," +
                "\"dialogues\":[{\"id\":\"d\"}],\"triggers\":[{\"id\":\"t\",\"routeType\":\"quest\",\"targetId\":\"q\",\"chapterId\":\"prologue\"}]," +
                "\"cinematics\":[{\"id\":\"opening\",\"dialogueId\":\"d\",\"completionFact\":\"missing_cinematic_fact\",\"subtitleCues\":[\"00:02 first\",\"00:01 out of order\"]}]," +
                "\"facts\":[],\"endings\":[{\"id\":\"no_map_photo\"}],\"characters\":[]}");

            var errors = NarrativeContentValidator.Validate(manifest, new EmptyContentAssetSource());

            Assert.That(errors.Any(error => error.Contains("missing completion fact 'missing_completion_fact'")), Is.True);
            Assert.That(errors.Any(error => error.Contains("missing cinematic completion fact 'missing_cinematic_fact'")), Is.True);
            Assert.That(errors.Any(error => error.Contains("subtitle cues are not ordered")), Is.True);
            Assert.That(errors.Any(error => error.Contains("ending 'no_map_photo' references missing dialogue")), Is.True);
        }

        [Test]
        public void RuntimeValidator_RejectsTypoChoiceFactsAndUsesSingleEndingVariantAuthority()
        {
            var manifest = NarrativeContentManifest.FromJson("{\"facts\":[],\"cinematics\":[],\"endings\":[],\"chapters\":[],\"quests\":[],\"dialogues\":[],\"triggers\":[],\"characters\":[]}");
            var dialogue = ScriptableObject.CreateInstance<Northbound.Dialogue.DialogueAsset>();
            dialogue.id = "scene";
            dialogue.lines.Add(new Northbound.Dialogue.DialogueLine { choices = new System.Collections.Generic.List<Northbound.Dialogue.DialogueChoice> { new Northbound.Dialogue.DialogueChoice { grantedFact = "typo_untrusted_fact" } } });
            var catalog = ScriptableObject.CreateInstance<NarrativeContentCatalog>();
            catalog.dialogues = new[] { dialogue };
            var errors = NarrativeContentValidator.ValidateRuntimeAssets(manifest, catalog);
            Assert.That(errors.Any(error => error.Contains("typo_untrusted_fact")), Is.True);
            foreach (var variant in Northbound.Endings.EndingDialogueMap.SupportedVariantIds)
                Assert.That(Northbound.Endings.EndingDialogueMap.DialogueId(variant), Is.Not.Empty);
            Object.DestroyImmediate(catalog); Object.DestroyImmediate(dialogue);
        }

        [Test]
        public void RuntimeValidator_RejectsEveryMalformedQuestFactKind()
        {
            var quest = "clock_in"; var objective = "serve_tables";
            Assert.That(NarrativeContentValidator.IsAuthorizedQuestRuntimeFact(quest, objective, Northbound.Quests.QuestRunner.StartedFactId(quest)), Is.True);
            Assert.That(NarrativeContentValidator.IsAuthorizedQuestRuntimeFact(quest, objective, Northbound.Quests.QuestRunner.ObjectiveProgressFactId(quest, objective)), Is.True);
            Assert.That(NarrativeContentValidator.IsAuthorizedQuestRuntimeFact(quest, objective, Northbound.Quests.QuestRunner.ObjectiveCompletionFactId(quest, objective)), Is.True);
            Assert.That(NarrativeContentValidator.IsAuthorizedQuestRuntimeFact(quest, objective, Northbound.Quests.QuestRunner.CompletionFact(quest)), Is.True);
            foreach (var typo in new[] { "quest_clockin_started", "objective_clock_in_serve_table_progress", "objective_clock_in_serve_table_complete", "quest_clockin_complete" })
                Assert.That(NarrativeContentValidator.IsAuthorizedQuestRuntimeFact(quest, objective, typo), Is.False, typo);
        }

        [Test]
        public void ApprovedContentManifest_HasNoMissingOrDuplicateReferences()
        {
            var manifest = NarrativeContentManifest.LoadApproved();
            var errors = NarrativeContentValidator.Validate(manifest, new ResourceContentAssetSource());

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(manifest.quests.Length, Is.EqualTo(16));
            Assert.That(manifest.triggers.Length, Is.EqualTo(35));
            Assert.That(manifest.cinematics.Length, Is.EqualTo(6));
            Assert.That(manifest.endings.Length, Is.EqualTo(4));
            Assert.That(manifest.characters.Length, Is.EqualTo(5));
            Assert.That(manifest.ChapterOrder(), Is.EqualTo(new[]
            {
                "prologue", "chapter_1", "chapter_2", "chapter_3_day_3", "chapter_3_day_2", "chapter_4", "finale"
            }));
            Assert.That(manifest.characters.Select(character => character.id), Is.EquivalentTo(new[] { "jamie", "elias", "maya", "noah", "leo" }));
            var characterCatalog = Resources.Load<NarrativeContentCatalog>("Northbound/NarrativeContentCatalog");
            Assert.That(NarrativeContentValidator.ValidateRuntimeAssets(manifest, characterCatalog), Is.Empty);
            Assert.That(new[] { "jamie", "elias", "maya", "noah", "leo" }.All(characterCatalog.HasCharacter), Is.True);
            var opening = characterCatalog.Cinematic("opening");
            var openingDialogue = characterCatalog.Dialogue("prologue_opening");
            Assert.That(opening.subtitleCues, Has.Length.EqualTo(10));
            Assert.That(opening.subtitleCues.Select(cue => cue.text), Is.EqualTo(openingDialogue.lines.Select(line => line.text)));
            StringAssert.Contains("Do they have better fries?", string.Join("\n", opening.subtitleCues.Select(cue => cue.text)));
            StringAssert.Contains("Statistically, probably.", string.Join("\n", opening.subtitleCues.Select(cue => cue.text)));
            foreach (var noMapDialogue in new[] { "ending_no_map_photo", "ending_no_map_notebook", "ending_no_map_house_key", "ending_no_map_map" })
            {
                Assert.That(characterCatalog.Dialogue(noMapDialogue), Is.Not.Null, noMapDialogue);
            }
            foreach (var cinematic in characterCatalog.cinematics)
            {
                var cinematicManifest = manifest.cinematics.Single(item => item.id == cinematic.id);
                var dialogue = characterCatalog.Dialogue(cinematicManifest.dialogueId);
                Assert.That(cinematic.subtitleCues.Select(cue => cue.text), Is.EqualTo(dialogue.lines.Select(line => line.text)), cinematic.id);
                Assert.That(cinematic.subtitleCues.Select(cue => cue.startSeconds), Is.Ordered, cinematic.id);
            }
        }

        [Test]
        public void ApprovedEnglishContent_HasSubstantiveSceneCoverageAndResponsiveJamieTones()
        {
            var manifest = NarrativeContentManifest.LoadApproved();
            var catalog = Resources.Load<NarrativeContentCatalog>("Northbound/NarrativeContentCatalog");
            Assert.That(catalog, Is.Not.Null);

            var totalLines = catalog.dialogues.Sum(dialogue => dialogue.lines.Count);
            Assert.That(totalLines, Is.GreaterThanOrEqualTo(300), "A short content sample cannot represent the approved full narrative.");
            var estimatedMinutes = NarrativeContentMetrics.EstimatePlaythroughMinutes(catalog, manifest);
            Assert.That(estimatedMinutes, Is.InRange(45f, 60f), "This is an authored-content estimate, not a substitute for observed human playtests.");
            foreach (var dialogue in manifest.dialogues)
            {
                var asset = catalog.Dialogue(dialogue.id);
                Assert.That(asset, Is.Not.Null, dialogue.id);
                var requiredMinimum = dialogue.kind is "required" or "cinematic" ? 8 : dialogue.kind is "optional" or "missed" or "farewell" ? 4 : 4;
                var maximum = dialogue.kind is "required" or "cinematic" ? 14 : 8;
                Assert.That(asset.lines.Count, Is.InRange(requiredMinimum, maximum), dialogue.id);
            }

            foreach (var choiceLine in catalog.dialogues.SelectMany(dialogue => dialogue.lines).Where(line => line.choices != null && line.choices.Count == 4))
            {
                Assert.That(choiceLine.choices.All(choice => choice.nextLineIndex >= 0), Is.True, "Each Jamie tone must produce a distinct response.");
                Assert.That(choiceLine.choices.All(choice => !string.IsNullOrWhiteSpace(choice.grantedFact)), Is.True, "Each Jamie tone must leave a named state trace.");
            }

            var spoken = catalog.dialogues.SelectMany(dialogue => dialogue.lines).Select(line => line.text?.Trim().ToLowerInvariant()).Where(text => !string.IsNullOrWhiteSpace(text)).ToArray();
            Assert.That(spoken.Any(text => text.Contains("nobody says the easy thing") || text.Contains("this corner of greybridge")), Is.False);
            Assert.That(spoken.GroupBy(text => text).All(group => group.Count() <= 2), Is.True, "Unrelated scenes must not be filled with repeated generic dialogue.");
        }

        [Test]
        public void ApprovedChineseContent_CoversEveryStoryLineAndChoice()
        {
            var catalog = Resources.Load<NarrativeContentCatalog>("Northbound/NarrativeContentCatalog");
            Assert.That(catalog, Is.Not.Null);

            foreach (var dialogue in catalog.dialogues)
            {
                for (var index = 0; index < dialogue.lines.Count; index++)
                {
                    var line = dialogue.lines[index];
                    Assert.That(DialogueChineseCatalog.HasTranslation(dialogue.id, index, line.textChinese), Is.True,
                        $"{dialogue.id} line {index} must not fall back to English in Chinese mode: {line.text}");
                    foreach (var choice in line.choices ?? new System.Collections.Generic.List<DialogueChoice>())
                    {
                        Assert.That(choice.textChinese, Is.Not.Null.And.Not.Empty,
                            $"{dialogue.id} line {index} choice '{choice.text}' must have Chinese copy.");
                    }
                }
            }
        }

        [Test]
        public void ThingsWeLeave_OnlyPhysicalChoiceCanGrantOneCarriedFact()
        {
            var catalog = Resources.Load<NarrativeContentCatalog>("Northbound/NarrativeContentCatalog");
            var quest = catalog.Quest("things_we_leave");
            Assert.That(quest.completionFacts, Has.None.Matches<string>(fact => fact.StartsWith("carried_")));
        }

        [TestCase(new[] { "alternator", "road_test", "pack_trunk" }, "northbound")]
        [TestCase(new[] { "first_light", "static", "last_night_open" }, "northbound")]
        [TestCase(new[] { "first_light", "static", "last_night_open" }, "home_chosen")]
        [TestCase(new[] { "alternator", "static", "pack_trunk" }, "no_map")]
        [TestCase(new[] { "first_light", "road_test", "pack_trunk" }, "pause_journey")]
        public void SimulatedFullPath_UsesOneMissionPerPair_ReachesFinale_AndResolvesExpectedEnding(string[] pairMissions, string endingId)
        {
            var manifest = NarrativeContentManifest.LoadApproved();
            var simulator = new NarrativePathSimulator(manifest);
            foreach (var mission in pairMissions)
            {
                Assert.That(simulator.CompleteExclusiveMission(mission), Is.True, mission);
            }

            if (endingId == "home_chosen")
            {
                simulator.SetFact("helped_maya");
                simulator.SetFact("helped_noah");
                simulator.SetFact("helped_leo");
            }
            else if (endingId == "no_map")
            {
                simulator.SetFact("carried_notebook");
            }

            Assert.That(simulator.EnterFinale(), Is.True);
            Assert.That(simulator.CurrentChapterId, Is.EqualTo("finale"));
            Assert.That(simulator.CompletedPairs, Is.EqualTo(RequiredPairs));
            Assert.That(simulator.ResolveEnding(endingId).AssetId, Is.EqualTo(endingId));
        }

        [Test]
        public void CharacterHighlight_TieUsesEarliestCompletedFriendMission()
        {
            var simulator = new NarrativePathSimulator(NarrativeContentManifest.LoadApproved());
            simulator.CompleteExclusiveMission("static");
            simulator.CompleteExclusiveMission("first_light");

            Assert.That(CharacterHighlightSelector.SelectId(simulator.State), Is.EqualTo("noah"));
        }

        [UnityTest]
        public IEnumerator Greybridge_RuntimeCreatesEveryManifestMissionAndConversationTrigger()
        {
            SceneManager.LoadScene("Greybridge", LoadSceneMode.Single);
            yield return null;

            var manifest = NarrativeContentManifest.LoadApproved();
            var director = Object.FindFirstObjectByType<NarrativeContentDirector>();
            var triggers = Object.FindObjectsByType<NarrativeRouteTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.That(director, Is.Not.Null);
            Assert.That(GameObject.Find("Jamie"), Is.Not.Null, "Jamie is the fifth primary character and must be present in the runtime scene.");
            Assert.That(triggers.Select(trigger => trigger.RouteId), Is.SupersetOf(manifest.triggers.Select(trigger => trigger.id)));
            Assert.That(triggers.All(trigger => trigger.HasResolvedContent), Is.True);

            var routePositions = triggers.GroupBy(trigger => (Vector2)trigger.transform.position).ToArray();
            Assert.That(routePositions.All(group => group.Count() == 1), Is.True,
                "Distinct narrative routes must not share a collider position and leave PlayerInteractor selection ambiguous.");

            foreach (var characterId in new[] { "Jamie", "NPC elias", "NPC maya", "NPC noah", "NPC leo" })
            {
                var character = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(item => item.name == characterId)?.gameObject;
                Assert.That(character, Is.Not.Null, characterId);
                Assert.That(character.GetComponentInChildren<Renderer>(true), Is.Not.Null, $"{characterId} needs a visible renderer proxy.");
            }
        }

        [UnityTest]
        public IEnumerator Bootstrap_RequiredQuestPathAdvancesToFinaleAndShowsEveryMissedMissionTrace()
        {
            if (GameBootstrap.Instance != null)
            {
                Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }
            var safeTestSave = new Northbound.Narrative.SaveGameService(Path.Combine(Application.temporaryCachePath, $"northbound-task10-{Guid.NewGuid():N}.json"));
            GameBootstrap.SessionSaveGameFactory = () => safeTestSave;
            SceneManager.LoadScene("Bootstrap", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var title = GameObject.Find("TitleMenu(Clone)");
            title.GetComponentsInChildren<Button>(true).Single(button => button.name == "New Game").onClick.Invoke();
            title.GetComponentsInChildren<Button>(true).Single(button => button.name == "Confirm New Game").onClick.Invoke();
            for (var frame = 0; frame < 20 && !GameBootstrap.Instance.IsSessionActive; frame++)
            {
                yield return null;
            }

            var director = Object.FindFirstObjectByType<NarrativeContentDirector>();
            Assert.That(director, Is.Not.Null);
            var finishedCinematics = new List<string>();
            GameBootstrap.Instance.Cinematics.Finished += finishedCinematics.Add;
            GameBootstrap.Instance.Settings.SkipMinigames = true;
            var flow = Object.FindFirstObjectByType<Northbound.Core.GameFlowController>();
            Assert.That(flow.CurrentChapterId, Is.EqualTo("prologue"), "A fresh game must begin at the Opening Promise before Chapter 1.");
            Assert.That(GameBootstrap.Instance.Cinematics.IsPlaying, Is.True, "The Opening Promise must begin automatically on a fresh game.");
            GameBootstrap.Instance.NarrativeState.Set("cinematic_opening_complete", true);
            GameBootstrap.Instance.Cinematics.Cancel();
            Assert.That(flow.EnterChapter("chapter_1"), Is.True);
            foreach (var routeId in new[]
            {
                "clock_in_trigger", "missing_socket_trigger", "parts_future_trigger", "rooftop_inventory_trigger",
                "last_sign_trigger", "dead_air_trigger", "one_more_table_trigger",
                "alternator_trigger", "road_test_trigger", "pack_trunk_trigger",
                "things_we_leave_trigger", "spare_key_trigger", "before_morning_trigger"
            })
            {
                yield return InteractWithRoute(routeId, ExpectedInitialDialogue(routeId));
                yield return CompleteOpenDialogue();
                yield return CompletePhysicalObjectives(routeId.Replace("_trigger", string.Empty));
                if (routeId == "one_more_table_trigger")
                {
                    Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_2"));
                    Assert.That(GameBootstrap.Instance.Dialogue.IsRunning, Is.True, "One More Table must lead into the required Chapter 2 rooftop conversation.");
                    StringAssert.Contains("If we keep moving the date", GameBootstrap.Instance.Dialogue.Current.text);
                    yield return CompleteOpenDialogue();
                    Assert.That(flow.CurrentChapterId, Is.EqualTo("chapter_3_day_3"));
                }
            }

            Assert.That(GameBootstrap.Instance.NarrativeState.Has("quest_before_morning_complete"), Is.True);
            Assert.That(Object.FindFirstObjectByType<Northbound.Core.GameFlowController>().CurrentChapterId, Is.EqualTo("finale"));
            Assert.That(GameObject.Find("Maya Exhibition Closed").activeSelf, Is.True);
            Assert.That(GameObject.Find("Noah Equipment Returned").activeSelf, Is.True);
            Assert.That(GameObject.Find("Last Night Open Trace").activeSelf, Is.True);
            Assert.That(finishedCinematics, Is.EqualTo(new[] { "rooftop", "finale" }),
                "The real Elias route must keep friend highlight videos exclusive to their chosen missions.");
            GameBootstrap.Instance.NarrativeState.Set("missed_alternator", true);
            Assert.That(GameObject.Find("Garage Dark"), Is.Not.Null, "The dark garage is the physical trace for missing the alternator mission.");
            GameBootstrap.SessionSaveGameFactory = null;
        }

        private static IEnumerator CompleteOpenDialogue()
        {
            var dialogue = GameBootstrap.Instance.Dialogue;
            var safety = 0;
            while (dialogue.IsRunning && safety++ < 40)
            {
                if (dialogue.Current.choices != null && dialogue.Current.choices.Count > 0) dialogue.Choose(0);
                else dialogue.Advance();
                yield return null;
            }

            Assert.That(safety, Is.LessThan(40), "Dialogue must complete without a loop.");
        }

        private static IEnumerator InteractWithRoute(string routeId, string dialogueId)
        {
            var route = Object.FindObjectsByType<NarrativeRouteTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(trigger => trigger.RouteId == routeId);
            yield return TravelToContainingLocation(route.transform);
            var player = GameObject.Find("Jamie");
            var interactor = player.GetComponent<PlayerInteractor>();
            player.transform.position = route.transform.position;
            Physics2D.SyncTransforms();
            interactor.TryInteract();
            yield return null;
            Assert.That(GameBootstrap.Instance.Dialogue.IsRunning, Is.True, $"Player interaction must activate {routeId} rather than a direct director call.");
            var director = Object.FindFirstObjectByType<NarrativeContentDirector>();
            Assert.That(director.LastActivatedRouteId, Is.EqualTo(routeId));
            Assert.That(GameBootstrap.Instance.Dialogue.ActiveDialogueId, Is.EqualTo(dialogueId));
            var questId = routeId.Replace("_trigger", "");
            if (!new[] { "alternator", "road_test", "pack_trunk" }.Contains(questId))
                Assert.That(director.ActiveQuestId, Is.EqualTo(questId));
        }

        private static string ExpectedInitialDialogue(string routeId) => routeId switch
        {
            "alternator_trigger" => "mission_pair_alternator_first_light_confirmation",
            "road_test_trigger" => "mission_pair_road_test_static_confirmation",
            "pack_trunk_trigger" => "mission_pair_last_night_open_pack_trunk_confirmation",
            _ => routeId.Replace("_trigger", "") + "_dialogue"
        };

        private static IEnumerator CompletePhysicalObjectives(string questId)
        {
            var player = GameObject.Find("Jamie");
            var interactor = player.GetComponent<PlayerInteractor>();
            var safety = 0;
            while (!GameBootstrap.Instance.NarrativeState.Has($"quest_{questId}_complete") && safety++ < 12)
            {
                var objective = Object.FindObjectsByType<NarrativeObjectiveTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(trigger => trigger.QuestId == questId && trigger.CanInteract);
                var director = Object.FindFirstObjectByType<NarrativeContentDirector>();
                Assert.That(objective, Is.Not.Null, $"{questId} must expose its next physical objective rather than completing when dialogue closes. Active={director?.ActiveQuestId ?? "none"}");
                yield return TravelToContainingLocation(objective.transform);
                player.transform.position = objective.transform.position;
                Physics2D.SyncTransforms();
                interactor.TryInteract();
                yield return null;
                if (questId == "before_morning" && GameBootstrap.Instance.Dialogue.IsRunning)
                {
                    yield return CompleteOpenDialogue();
                }
            }
            Assert.That(GameBootstrap.Instance.NarrativeState.Has($"quest_{questId}_complete"), Is.True, questId);
            var cinematicSafety = 0;
            while (GameBootstrap.Instance.Cinematics.IsPlaying && cinematicSafety++ < 6)
            {
                GameBootstrap.Instance.Cinematics.Tick(2.1f);
                GameBootstrap.Instance.Cinematics.Skip();
                yield return null;
            }
            Assert.That(cinematicSafety, Is.LessThan(6), "A routed video chain must finish without looping.");
            if (GameBootstrap.Instance.Dialogue.ActiveDialogueId == "rooftop_decision")
            {
                yield return CompleteOpenDialogue();
                Assert.That(GameBootstrap.Instance.NarrativeState.Has(ChapterStoryMarkResolver.ChapterThreePlanFact), Is.True,
                    "The full player path must make and persist a rooftop stance before Chapter 4.");
            }
        }

        private static IEnumerator TravelToContainingLocation(Transform target)
        {
            var root = target;
            while (root != null && !root.name.StartsWith("Location ")) root = root.parent;
            if (root == null) yield break;
            var id = root.name.Substring("Location ".Length);
            var controller = Object.FindFirstObjectByType<Northbound.World.LocationTransitionController>();
            if (controller == null || controller.CurrentLocationId == id) yield break;
            controller.SetTransitionDuration(0f);
            Assert.That(controller.StartTravel(id), Is.True, $"The route's authored location {id} must be enterable.");
            for (var frame = 0; frame < 60 && (controller.CurrentLocationId != id || controller.IsTravelling); frame++) yield return null;
            Assert.That(controller.CurrentLocationId, Is.EqualTo(id));
            Assert.That(controller.IsTravelling, Is.False);
        }
    }
}
