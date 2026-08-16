using System.Collections.Generic;
using System.Linq;
using Northbound.Core;
using Northbound.Dialogue;
using Northbound.Narrative;
using Northbound.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Northbound.Tests
{
    public sealed class DialogueRunnerTests
    {
        private GameObject gateObject;
        private InputGate gate;

        [SetUp]
        public void SetUp()
        {
            gateObject = new GameObject("Input Gate");
            gate = gateObject.AddComponent<InputGate>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gateObject);
        }

        [Test]
        public void Advance_SkipsLineWhoseRequiredFactIsNotSet()
        {
            var runner = new DialogueRunner(new NarrativeStateStore(), gate);
            var dialogue = CreateDialogue(
                Line("elias", "Friday."),
                Line("noah", "You helped.", requiredFact: "helped_noah"),
                Line("maya", "Six in the morning."));

            runner.Start(dialogue);
            runner.Advance();

            Assert.That(runner.Current.speakerId, Is.EqualTo("maya"));
            Assert.That(runner.IsRunning, Is.True);
        }

        [Test]
        public void Advance_GrantsLineFactAndCompletesOnce()
        {
            var state = new NarrativeStateStore();
            var runner = new DialogueRunner(state, gate);
            var completedCount = 0;
            runner.Completed += () => completedCount++;
            var dialogue = CreateDialogue(Line("elias", "That was a schedule.", grantedFact: "heard_schedule"));

            runner.Start(dialogue);

            Assert.That(gate.IsBlocked, Is.True);
            Assert.That(runner.Advance(), Is.True);
            Assert.That(state.Has("heard_schedule"), Is.True);
            Assert.That(runner.IsRunning, Is.False);
            Assert.That(gate.IsBlocked, Is.False);
            Assert.That(completedCount, Is.EqualTo(1));
            Assert.That(runner.Advance(), Is.False);
            Assert.That(completedCount, Is.EqualTo(1));
        }

        [Test]
        public void Choose_RejectsInvalidIndexAndGrantsSelectedChoiceFact()
        {
            var state = new NarrativeStateStore();
            var runner = new DialogueRunner(state, gate);
            var dialogue = CreateDialogue(Line(
                "maya",
                "You just gave a speech.",
                choices: new List<DialogueChoice>
                {
                    new DialogueChoice { text = "Keep it uncertain.", grantedFact = "jamie_uncertain", nextLineIndex = -1 }
                }));

            runner.Start(dialogue);

            Assert.That(runner.Choose(1), Is.False);
            Assert.That(state.Has("jamie_uncertain"), Is.False);
            Assert.That(runner.IsRunning, Is.True);
            Assert.That(runner.Choose(0), Is.True);
            Assert.That(state.Has("jamie_uncertain"), Is.True);
            Assert.That(runner.IsRunning, Is.False);
            Assert.That(gate.IsBlocked, Is.False);
        }

        [Test]
        public void Choose_AppliesLineAndSelectedChoiceCounterDeltas()
        {
            var state = new NarrativeStateStore();
            var runner = new DialogueRunner(state, gate);
            var line = Line(
                "elias",
                "What do you think?",
                choices: new List<DialogueChoice>
                {
                    new DialogueChoice
                    {
                        text = "Keep the plan.",
                        counterDeltas = new List<NarrativeCounterDelta>
                        {
                            new NarrativeCounterDelta { id = ChapterStoryMarkResolver.CommitmentCounterId, amount = 10 },
                            new NarrativeCounterDelta { id = ChapterStoryMarkResolver.AgencyCounterId, amount = -5 }
                        }
                    }
                });
            line.counterDeltas.Add(new NarrativeCounterDelta { id = "line_effect", amount = 2 });
            runner.Start(CreateDialogue(line));

            Assert.That(runner.Choose(0), Is.True);

            Assert.That(state.GetInt("line_effect"), Is.EqualTo(2));
            Assert.That(state.GetInt(ChapterStoryMarkResolver.CommitmentCounterId), Is.EqualTo(10));
            Assert.That(state.GetInt(ChapterStoryMarkResolver.AgencyCounterId), Is.EqualTo(-5));
            Assert.That(runner.Choose(0), Is.False, "A completed choice must not be applied twice.");
            Assert.That(state.GetInt(ChapterStoryMarkResolver.CommitmentCounterId), Is.EqualTo(10));
        }

        [Test]
        public void Choose_TerminalResponseDoesNotFallThroughOtherChoiceResponses()
        {
            var runner = new DialogueRunner(new NarrativeStateStore(), gate);
            var choiceLine = Line("jamie", "Choose.", choices: new List<DialogueChoice>
            {
                new DialogueChoice { text = "A", nextLineIndex = 1 },
                new DialogueChoice { text = "B", nextLineIndex = 2 },
                new DialogueChoice { text = "C", nextLineIndex = 3 }
            });
            runner.Start(CreateDialogue(
                choiceLine,
                Line("leo", "Response A."),
                Line("leo", "Response B."),
                Line("leo", "Response C.")));

            Assert.That(runner.Choose(0), Is.True);
            Assert.That(runner.Current.text, Is.EqualTo("Response A."));
            Assert.That(runner.Advance(), Is.True);
            Assert.That(runner.IsRunning, Is.False,
                "A selected response with no explicit continuation must end its branch instead of playing sibling responses.");
        }

        [Test]
        public void StartAtLine_ResumesAChosenResponseWithoutReplayingTheChoice()
        {
            var runner = new DialogueRunner(new NarrativeStateStore(), gate);
            runner.Start(CreateDialogue(
                Line("jamie", "Choose.", choices: new List<DialogueChoice>
                {
                    new DialogueChoice { text = "A", nextLineIndex = 1 }
                }),
                Line("leo", "Response A.")), 1);

            Assert.That(runner.IsRunning, Is.True);
            Assert.That(runner.CurrentLineIndex, Is.EqualTo(1));
            Assert.That(runner.Current.text, Is.EqualTo("Response A."));
        }

        [Test]
        public void StoryMarkChoice_ReplacesAnyEarlierMarkFromTheSameChapter()
        {
            var state = new NarrativeStateStore();
            state.Set(ChapterStoryMarkResolver.ChapterThreePlanFact, true);
            var runner = new DialogueRunner(state, gate);
            runner.Start(CreateDialogue(
                Line("jamie", "Choose.", choices: new List<DialogueChoice>
                {
                    new DialogueChoice
                    {
                        text = "Agency",
                        grantedFact = ChapterStoryMarkResolver.ChapterThreeAgencyFact,
                        nextLineIndex = 1
                    }
                }),
                Line("maya", "Your choice.")));

            Assert.That(runner.Choose(0), Is.True);
            Assert.That(state.Has(ChapterStoryMarkResolver.ChapterThreePlanFact), Is.False);
            Assert.That(state.Has(ChapterStoryMarkResolver.ChapterThreeBalanceFact), Is.False);
            Assert.That(state.Has(ChapterStoryMarkResolver.ChapterThreeAgencyFact), Is.True);
        }

        [Test]
        public void Start_ChoosesMatchingConditionalCallbackBeforeFollowingItsExit()
        {
            var state = new NarrativeStateStore();
            state.Set(ChapterStoryMarkResolver.ChapterOneBalanceFact, true);
            var runner = new DialogueRunner(state, gate);
            var plan = Line("maya", "Plan callback.", requiredFact: ChapterStoryMarkResolver.ChapterOnePlanFact);
            var balance = Line("maya", "Balance callback.", requiredFact: ChapterStoryMarkResolver.ChapterOneBalanceFact);
            var agency = Line("maya", "Agency callback.", requiredFact: ChapterStoryMarkResolver.ChapterOneAgencyFact);
            plan.nextLineIndex = 3;
            balance.nextLineIndex = 3;
            agency.nextLineIndex = 3;
            var common = Line("jamie", "Common scene.");

            runner.Start(CreateDialogue(plan, balance, agency, common));

            Assert.That(runner.Current.text, Is.EqualTo("Balance callback."));
            Assert.That(runner.Advance(), Is.True);
            Assert.That(runner.Current.text, Is.EqualTo("Common scene."));
        }

        [Test]
        public void ChapterOneSliceAssets_AreBilingualAndExposeThreeResponsiveChoices()
        {
            var clockIn = AssetDatabase.LoadAssetAtPath<DialogueAsset>("Assets/Northbound/Data/Dialogue/ClockInDialogue.asset");
            var rooftop = AssetDatabase.LoadAssetAtPath<DialogueAsset>("Assets/Northbound/Data/Dialogue/RooftopInventoryDialogue.asset");
            var lastSign = AssetDatabase.LoadAssetAtPath<DialogueAsset>("Assets/Northbound/Data/Dialogue/LastSignDialogue.asset");

            foreach (var dialogue in new[] { clockIn, rooftop, lastSign })
            {
                Assert.That(dialogue, Is.Not.Null);
                Assert.That(dialogue.lines, Is.Not.Empty);
                Assert.That(dialogue.lines.All(line => !string.IsNullOrWhiteSpace(line.text)), Is.True);
                Assert.That(dialogue.lines.All(line => !string.IsNullOrWhiteSpace(line.textChinese)), Is.True);
                foreach (var choice in dialogue.lines.SelectMany(line => line.choices ?? new List<DialogueChoice>()))
                {
                    Assert.That(choice.textChinese, Is.Not.Null.And.Not.Empty);
                }
            }

            Assert.That(clockIn.lines.Single(line => line.choices.Count > 0).choices, Has.Count.EqualTo(3));
            Assert.That(rooftop.lines.Single(line => line.choices.Count > 0).choices, Has.Count.EqualTo(3));
            Assert.That(lastSign.lines.Take(3).Select(line => line.requiredFact), Is.EqualTo(new[]
            {
                ChapterStoryMarkResolver.ChapterOnePlanFact,
                ChapterStoryMarkResolver.ChapterOneBalanceFact,
                ChapterStoryMarkResolver.ChapterOneAgencyFact
            }));
        }

        [Test]
        public void LastSign_AgencyEchoAffirmsStayingAndImmediatelyNamesTheLocalInvitation()
        {
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueAsset>("Assets/Northbound/Data/Dialogue/LastSignDialogue.asset");
            Assert.That(dialogue, Is.Not.Null);
            var state = new NarrativeStateStore();
            state.Set(ChapterStoryMarkResolver.ChapterOneAgencyFact, true);
            var runner = new DialogueRunner(state, gate);

            runner.Start(dialogue);

            Assert.That(runner.Current.requiredFact, Is.EqualTo(ChapterStoryMarkResolver.ChapterOneAgencyFact));
            Assert.That(runner.Current.text, Does.Contain("staying").IgnoreCase);
            Assert.That(runner.Current.text, Does.Not.Contain("leav").IgnoreCase);
            Assert.That(runner.Current.text, Does.Not.Contain("escape").IgnoreCase);
            Assert.That(runner.Current.textChinese, Does.Contain("留下"));
            Assert.That(runner.Current.textChinese, Does.Not.Contain("离开"));

            Assert.That(runner.Advance(), Is.True);
            Assert.That(runner.CurrentLineIndex, Is.EqualTo(3),
                "The stay echo must lead directly into the invitation context, not an unexplained pronoun.");
            Assert.That(runner.Current.text, Does.Contain("Greybridge Arts Center"));
            Assert.That(runner.Current.text, Does.Contain("local exhibition").IgnoreCase);
            Assert.That(runner.Current.text, Does.Contain("invitation").IgnoreCase);
            Assert.That(runner.Current.textChinese, Does.Contain("格雷布里奇艺术中心"));
            Assert.That(runner.Current.textChinese, Does.Contain("本地展览"));
        }

        [Test]
        public void FirstLight_ContinuesAtTheNamedGreybridgeArtsCenter()
        {
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueAsset>("Assets/Northbound/Data/Dialogue/FirstLightDialogue.asset");
            Assert.That(dialogue, Is.Not.Null);
            var venueLineIndex = dialogue.lines.FindIndex(line => line.text.Contains("Greybridge Arts Center"));

            Assert.That(venueLineIndex, Is.GreaterThanOrEqualTo(0),
                "First Light must identify the local venue established by Last Sign.");
            Assert.That(dialogue.lines[venueLineIndex].text, Does.Contain("tonight's exhibition").IgnoreCase);

            GameText.Use(GameLanguage.SimplifiedChinese);
            try
            {
                var chinese = DialogueChineseCatalog.Resolve(
                    dialogue.id,
                    venueLineIndex,
                    dialogue.lines[venueLineIndex].text,
                    dialogue.lines[venueLineIndex].textChinese);
                Assert.That(chinese, Does.Contain("格雷布里奇艺术中心"));
                Assert.That(chinese, Does.Contain("今晚的展览"));
            }
            finally
            {
                GameText.Use(GameLanguage.English);
            }
        }

        [TestCase(
            "Assets/Northbound/Data/Dialogue/ChapterTwoRooftop.asset",
            "story_mark_ch2_",
            null)]
        [TestCase(
            "Assets/Northbound/Data/Dialogue/RooftopDecision.asset",
            "story_mark_ch3_",
            "story_mark_ch2_")]
        [TestCase(
            "Assets/Northbound/Data/Dialogue/BeforeMorningDialogue.asset",
            "story_mark_ch4_",
            "story_mark_ch3_")]
        public void LaterChapterDecisionAssets_HaveDistinctBilingualBranchesWithPersistentConsequences(
            string path,
            string choiceFactPrefix,
            string callbackFactPrefix)
        {
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueAsset>(path);

            Assert.That(dialogue, Is.Not.Null, path);
            var choiceLine = dialogue.lines.Single(line => line.choices != null && line.choices.Count > 0);
            Assert.That(choiceLine.choices, Has.Count.EqualTo(3), dialogue.id);
            Assert.That(choiceLine.choices.Select(choice => choice.text).Distinct().Count(), Is.EqualTo(3), $"{dialogue.id} English stances");
            Assert.That(choiceLine.choices.Select(choice => choice.textChinese).Distinct().Count(), Is.EqualTo(3), $"{dialogue.id} Chinese stances");
            Assert.That(choiceLine.choices.Select(choice => choice.nextLineIndex).Distinct().Count(), Is.EqualTo(3), $"{dialogue.id} immediate responses");

            for (var index = 0; index < choiceLine.choices.Count; index++)
            {
                var choice = choiceLine.choices[index];
                Assert.That(choice.text, Is.Not.Null.And.Not.Empty, $"{dialogue.id} choice {index} English");
                Assert.That(choice.textChinese, Is.Not.Null.And.Not.Empty, $"{dialogue.id} choice {index} Chinese");
                Assert.That(choice.grantedFact, Is.EqualTo($"{choiceFactPrefix}{(char)('a' + index)}"));
                Assert.That(choice.counterDeltas, Has.Some.Matches<NarrativeCounterDelta>(
                    delta => delta != null && !string.IsNullOrEmpty(delta.id) && delta.id.StartsWith("tendency_") && delta.amount != 0),
                    $"{dialogue.id} choice {index} tendency consequence");
                Assert.That(choice.counterDeltas, Has.Some.Matches<NarrativeCounterDelta>(
                    delta => delta != null && !string.IsNullOrEmpty(delta.id) && delta.id.StartsWith("bond_") && delta.amount != 0),
                    $"{dialogue.id} choice {index} relationship consequence");
                Assert.That(choice.nextLineIndex, Is.InRange(0, dialogue.lines.Count - 1));
                var response = dialogue.lines[choice.nextLineIndex];
                Assert.That(response.text, Is.Not.Null.And.Not.Empty, $"{dialogue.id} response {index} English");
                Assert.That(response.textChinese, Is.Not.Null.And.Not.Empty, $"{dialogue.id} response {index} Chinese");
            }

            if (!string.IsNullOrEmpty(callbackFactPrefix))
            {
                Assert.That(
                    dialogue.lines.Take(3).Select(line => line.requiredFact),
                    Is.EqualTo(new[] { $"{callbackFactPrefix}a", $"{callbackFactPrefix}b", $"{callbackFactPrefix}c" }));
            }
        }

        [Test]
        public void EveryAuthoredChoice_IsBilingualBranchesAndPersistsAConsumedConsequence()
        {
            var paths = AssetDatabase.FindAssets("t:DialogueAsset", new[] { "Assets/Northbound/Data/Dialogue" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
            var choiceCount = 0;

            foreach (var path in paths)
            {
                var dialogue = AssetDatabase.LoadAssetAtPath<DialogueAsset>(path);
                var choiceLine = dialogue?.lines?.FirstOrDefault(line => line?.choices != null && line.choices.Count > 0);
                if (choiceLine == null)
                {
                    continue;
                }

                for (var index = 0; index < choiceLine.choices.Count; index++)
                {
                    var choice = choiceLine.choices[index];
                    choiceCount++;
                    Assert.That(choice.text, Is.Not.Null.And.Not.Empty, $"{dialogue.id} choice {index} English");
                    Assert.That(choice.textChinese, Is.Not.Null.And.Not.Empty, $"{dialogue.id} choice {index} Chinese");
                    Assert.That(choice.grantedFact, Is.Not.Null.And.Not.Empty, $"{dialogue.id} choice {index} fact");
                    Assert.That(choice.nextLineIndex, Is.InRange(0, dialogue.lines.Count - 1), $"{dialogue.id} choice {index} branch");
                    Assert.That(
                        (choice.counterDeltas != null && choice.counterDeltas.Any(delta => delta != null && delta.amount != 0)) ||
                        ChoiceConsequenceResolver.IsTrackedChoiceFact(choice.grantedFact),
                        Is.True,
                        $"{dialogue.id} choice {index} must change a future-facing counter");

                    var state = new NarrativeStateStore();
                    var choiceRunner = new DialogueRunner(state);
                    choiceRunner.Start(dialogue);
                    while (choiceRunner.IsRunning && (choiceRunner.Current.choices == null || choiceRunner.Current.choices.Count == 0))
                    {
                        Assert.That(choiceRunner.Advance(), Is.True, dialogue.id);
                    }

                    Assert.That(choiceRunner.Choose(index), Is.True, $"{dialogue.id} choice {index} must trigger");
                    Assert.That(state.Has(choice.grantedFact), Is.True, $"{dialogue.id} choice {index} fact persisted");
                    Assert.That(choiceRunner.Current, Is.SameAs(dialogue.lines[choice.nextLineIndex]), $"{dialogue.id} choice {index} branch target");
                    if (ChoiceConsequenceResolver.IsTrackedChoiceFact(choice.grantedFact))
                    {
                        Assert.That(state.Has($"choice_effect_recorded_{choice.grantedFact}"), Is.True, $"{dialogue.id} choice {index} consumed");
                    }
                }
            }

            Assert.That(choiceCount, Is.EqualTo(43), "All twelve authored branching conversations must stay covered.");
        }

        [TestCase("optional_maya_mural_committed", 6, 3, -1, 4)]
        [TestCase("optional_maya_mural_curious", 1, 1, 5, 3)]
        [TestCase("optional_maya_mural_uncertain", 0, 2, 2, 1)]
        [TestCase("optional_maya_mural_silent", -2, -2, -2, -3)]
        public void RelationshipChoice_ToneChangesTendenciesAndBondExactlyOnce(
            string fact, int commitment, int rootedness, int agency, int bond)
        {
            var state = new NarrativeStateStore();

            Assert.That(ChoiceConsequenceResolver.ApplyImplicit(state, fact), Is.True);
            Assert.That(ChoiceConsequenceResolver.ApplyImplicit(state, fact), Is.True);

            Assert.That(state.GetInt(ChapterStoryMarkResolver.CommitmentCounterId), Is.EqualTo(commitment));
            Assert.That(state.GetInt(ChapterStoryMarkResolver.RootednessCounterId), Is.EqualTo(rootedness));
            Assert.That(state.GetInt(ChapterStoryMarkResolver.AgencyCounterId), Is.EqualTo(agency));
            Assert.That(state.GetInt("bond_maya"), Is.EqualTo(bond));
        }

        [Test]
        public void StartWhileRunning_ReleasesPreviousInputLeaseBeforeNewDialogueCompletes()
        {
            var runner = new DialogueRunner(new NarrativeStateStore(), gate);
            var first = CreateDialogue(Line("elias", "First."));
            var replacement = CreateDialogue(Line("maya", "Replacement."));

            runner.Start(first);
            runner.Start(replacement);

            Assert.That(gate.IsBlocked, Is.True);
            Assert.That(runner.Current.text, Is.EqualTo("Replacement."));
            Assert.That(runner.Advance(), Is.True);
            Assert.That(gate.IsBlocked, Is.False);
            Assert.That(runner.IsRunning, Is.False);
        }

        [Test]
        public void Advance_NotifiesBoundViewWhenItMovesToAnotherLine()
        {
            var runner = new DialogueRunner(new NarrativeStateStore(), gate);
            var changeCount = 0;
            runner.Changed += () => changeCount++;
            var dialogue = CreateDialogue(Line("elias", "First."), Line("maya", "Second."));

            runner.Start(dialogue);
            changeCount = 0;
            runner.Advance();

            Assert.That(changeCount, Is.EqualTo(1));
            Assert.That(runner.Current.text, Is.EqualTo("Second."));
        }

        [Test]
        public void Start_RejectsAuthoredDialogueWithMoreThanFourChoices()
        {
            var runner = new DialogueRunner(new NarrativeStateStore(), gate);
            var choices = new List<DialogueChoice>();
            for (var index = 0; index < 5; index++)
            {
                choices.Add(new DialogueChoice { text = $"Choice {index + 1}", nextLineIndex = -1 });
            }

            runner.Start(CreateDialogue(Line("maya", "Choose.", choices: choices)));

            Assert.That(runner.IsRunning, Is.False);
            Assert.That(gate.IsBlocked, Is.False);
            Assert.That(runner.LastValidationError, Does.Contain("four"));
        }

        [Test]
        public void GarageSchedule_ContainsTheApprovedThreeLineExchange()
        {
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueAsset>("Assets/Northbound/Data/Dialogue/Ch1_GarageSchedule.asset");

            Assert.That(dialogue, Is.Not.Null);
            Assert.That(dialogue.lines, Has.Count.EqualTo(3));
            Assert.That(dialogue.lines[0].speakerId, Is.EqualTo("Elias"));
            Assert.That(dialogue.lines[0].text, Is.EqualTo("Friday. Six in the morning. No speeches, no delays."));
            Assert.That(dialogue.lines[1].speakerId, Is.EqualTo("Maya"));
            Assert.That(dialogue.lines[1].text, Is.EqualTo("You just gave a speech."));
            Assert.That(dialogue.lines[2].speakerId, Is.EqualTo("Elias"));
            Assert.That(dialogue.lines[2].text, Is.EqualTo("That was a schedule."));
        }

        [Test]
        public void BootstrapScene_ReferencesCanvasHostedDialogueView()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Northbound/Scenes/Bootstrap.unity", OpenSceneMode.Additive);
            try
            {
                var bootstrap = FindBootstrap(scene);
                var serializedBootstrap = new SerializedObject(bootstrap);
                var dialoguePrefab = serializedBootstrap.FindProperty("dialogueViewPrefab");

                Assert.That(dialoguePrefab, Is.Not.Null);
                Assert.That(dialoguePrefab.objectReferenceValue, Is.Not.Null);
                var prefab = dialoguePrefab.objectReferenceValue as GameObject;
                Assert.That(prefab.GetComponent<Canvas>(), Is.Not.Null);
                Assert.That(prefab.GetComponent<GraphicRaycaster>(), Is.Not.Null);
                Assert.That(prefab.GetComponent<CanvasScaler>(), Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [TestCase(1280, 720, 900f)]
        [TestCase(1920, 1080, 1300f)]
        public void DialogueView_UsesActualCanvasLayoutWithoutPortraitOverlap(int width, int height, float minimumTextWidth)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Northbound/Prefabs/UI/DialogueView.prefab");
            var view = Object.Instantiate(prefab);
            var texture = new Texture2D(2, 2);
            var portraitSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(.5f, .5f));
            try
            {
                var canvas = view.GetComponent<Canvas>();
                var scaler = view.GetComponent<CanvasScaler>();
                var root = view.GetComponent<RectTransform>();
                var portrait = view.transform.Find("Panel/Portrait").GetComponent<RectTransform>();
                var dialogueText = view.transform.Find("Panel/Dialogue Text").GetComponent<RectTransform>();
                var label = dialogueText.GetComponent<Text>();

                Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
                Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));
                canvas.renderMode = RenderMode.WorldSpace;
                scaler.referenceResolution = new Vector2(width, height);
                root.sizeDelta = new Vector2(width, height);

                var line = Line(
                    "elias",
                    "Friday. Six in the morning. No speeches, no delays. Friday. Six in the morning. No speeches, no delays. Friday. Six in the morning. No speeches, no delays. Friday. Six in the morning. No speeches, no delays.",
                    choices: Enumerable.Range(1, DialogueRunner.MaximumChoices)
                        .Select(index => new DialogueChoice { text = $"Choice {index}" })
                        .ToList());
                line.portrait = portraitSprite;
                var runner = new DialogueRunner(new NarrativeStateStore(), gate);
                view.GetComponent<DialogueView>().Bind(runner);
                runner.Start(CreateDialogue(line));
                view.GetComponent<DialogueView>().RevealCurrentLine();
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
                Canvas.ForceUpdateCanvases();

                Assert.That(dialogueText.rect.width, Is.GreaterThan(minimumTextWidth));
                Assert.That(label.cachedTextGenerator.lineCount, Is.GreaterThan(1));
                var portraitCorners = new Vector3[4];
                var dialogueCorners = new Vector3[4];
                portrait.GetWorldCorners(portraitCorners);
                dialogueText.GetWorldCorners(dialogueCorners);
                Assert.That(dialogueCorners[0].x, Is.GreaterThanOrEqualTo(portraitCorners[2].x),
                    "Visible dialogue copy must start to the right of the portrait.");

                for (var index = 1; index <= DialogueRunner.MaximumChoices; index++)
                {
                    var choice = view.transform.Find($"Panel/Choice {index}").GetComponent<RectTransform>();
                    var choiceCorners = new Vector3[4];
                    choice.GetWorldCorners(choiceCorners);
                    Assert.That(choiceCorners[0].x, Is.GreaterThanOrEqualTo(portraitCorners[2].x),
                        $"Visible choice {index} must start to the right of the portrait.");
                }
            }
            finally
            {
                Object.DestroyImmediate(view);
                Object.DestroyImmediate(portraitSprite);
                Object.DestroyImmediate(texture);
            }
        }

        private static GameBootstrap FindBootstrap(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var bootstrap = root.GetComponent<GameBootstrap>();
                if (bootstrap != null)
                {
                    return bootstrap;
                }
            }

            return null;
        }

        private static DialogueAsset CreateDialogue(params DialogueLine[] lines)
        {
            var asset = ScriptableObject.CreateInstance<DialogueAsset>();
            asset.id = "test";
            asset.lines = new List<DialogueLine>(lines);
            return asset;
        }

        private static DialogueLine Line(
            string speakerId,
            string text,
            string requiredFact = null,
            string grantedFact = null,
            List<DialogueChoice> choices = null)
        {
            return new DialogueLine
            {
                speakerId = speakerId,
                text = text,
                requiredFact = requiredFact,
                grantedFact = grantedFact,
                nextLineIndex = -1,
                choices = choices
            };
        }
    }
}
