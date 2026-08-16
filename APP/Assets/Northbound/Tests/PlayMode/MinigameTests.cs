using System.Collections;
using Northbound.Core;
using Northbound.Minigames;
using Northbound.Narrative;
using Northbound.Quests;
using Northbound.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Northbound.Tests
{
    public sealed class MinigameTests
    {
        [UnityTest]
        public IEnumerator DinerShift_CompleteReportsOnceAndReleasesInput()
        {
            var context = CreateContext("diner_shift");
            var game = context.host.AddComponent<DinerShiftGame>();
            game.Configure(context.gate, context.runner, context.state, new SettingsModel(), "serve_diner");
            var completions = 0;
            game.Completed += _ => completions++;

            game.Begin();
            Assert.That(context.gate.IsBlocked, Is.True);
            Assert.That(game.DeliverOrder("coffee", "table_coffee"), Is.True);
            Assert.That(game.DeliverOrder("pie", "table_pie"), Is.True);
            Assert.That(game.DeliverOrder("soup", "table_soup"), Is.True);

            Assert.That(game.IsRunning, Is.False);
            Assert.That(context.gate.IsBlocked, Is.False);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(context.state.GetInt("quest_diner_shift_objective_serve_diner_progress"), Is.EqualTo(1));
            game.DeliverOrder("coffee", "table_coffee");
            Assert.That(completions, Is.EqualTo(1));
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator DinerShift_RequiresSelectingAnOrderBeforeMatchingItsTable()
        {
            var context = CreateContext("diner_shift");
            var game = context.host.AddComponent<DinerShiftGame>();
            game.Configure(context.gate, context.runner, context.state, new SettingsModel(), "serve_diner");
            game.Begin();

            Assert.That(game.DeliverToTable("table_coffee"), Is.False);
            Assert.That(game.SelectOrder("coffee"), Is.True);
            Assert.That(game.DeliverToTable("table_pie"), Is.False);
            Assert.That(game.DeliverToTable("table_coffee"), Is.True);
            Assert.That(game.DeliveredOrderCount, Is.EqualTo(1));
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator Completion_ReportFailureKeepsGameRunningAndDoesNotEmitCompletion()
        {
            var host = new GameObject("Unbound minigame");
            var game = host.AddComponent<DinerShiftGame>();
            game.Configure(host.AddComponent<InputGate>(), new QuestRunner(new NarrativeStateStore()), new NarrativeStateStore(), new SettingsModel(), "missing");
            var completions = 0;
            game.Completed += _ => completions++;
            game.Begin();

            game.DeliverOrder("coffee", "table_coffee");
            game.DeliverOrder("pie", "table_pie");
            game.DeliverOrder("soup", "table_soup");

            Assert.That(game.IsRunning, Is.True);
            Assert.That(completions, Is.Zero);
            game.Cancel();
            yield return Destroy(host);
        }

        [UnityTest]
        public IEnumerator DinerShift_RetriesFinalReportWithoutCancelling()
        {
            var host = new GameObject("Retry diner");
            var state = new NarrativeStateStore();
            var failedRunner = new QuestRunner(state);
            var game = host.AddComponent<DinerShiftGame>();
            game.Configure(host.AddComponent<InputGate>(), failedRunner, state, new SettingsModel(), "serve_diner");
            game.Begin();
            game.DeliverOrder("coffee", "table_coffee");
            game.DeliverOrder("pie", "table_pie");
            game.DeliverOrder("soup", "table_soup");
            var recoveryRunner = new QuestRunner(state);
            var quest = ScriptableObject.CreateInstance<QuestAsset>();
            quest.id = "diner_retry";
            quest.objectives.Add(new QuestObjective { id = "serve_diner" });
            recoveryRunner.StartQuest(quest);
            game.Configure(host.GetComponent<InputGate>(), recoveryRunner, state, new SettingsModel(), "serve_diner");

            Assert.That(game.RetryCompletion(), Is.True);
            Assert.That(game.IsRunning, Is.False);
            yield return Destroy(host);
        }

        [UnityTest]
        public IEnumerator WiringGame_CancelReleasesInputWithoutReporting()
        {
            var context = CreateContext("wiring_job");
            var game = context.host.AddComponent<WiringGame>();
            game.Configure(context.gate, context.runner, context.state, new SettingsModel(), "wire_recorder");

            game.Begin();
            Assert.That(context.gate.IsBlocked, Is.True);
            game.Cancel();

            Assert.That(game.IsRunning, Is.False);
            Assert.That(context.gate.IsBlocked, Is.False);
            Assert.That(context.state.GetInt("quest_wiring_job_objective_wire_recorder_progress"), Is.Zero);
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator WiringGame_ResetRestoresAuthoredLayoutAndFourTilesCompleteIt()
        {
            var context = CreateContext("wiring_job");
            var game = context.host.AddComponent<WiringGame>();
            game.Configure(context.gate, context.runner, context.state, new SettingsModel(), "wire_recorder");

            game.Begin();
            game.RotateTile(0);
            Assert.That(game.GetTileRotation(0), Is.Not.EqualTo(game.GetAuthoredRotation(0)));
            game.ResetLayout();
            Assert.That(game.GetTileRotation(0), Is.EqualTo(game.GetAuthoredRotation(0)));

            for (var tile = 0; tile < WiringGame.TileCount; tile++)
            {
                while (game.GetTileRotation(tile) != game.GetConnectedRotation(tile))
                {
                    game.RotateTile(tile);
                }
            }

            Assert.That(game.IsRunning, Is.False);
            Assert.That(context.state.GetInt("quest_wiring_job_objective_wire_recorder_progress"), Is.EqualTo(1));
            Assert.That(game.MinimumFirstRunInteractions, Is.EqualTo(7));
            Assert.That(game.MaximumFirstRunInteractions, Is.EqualTo(19));
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator TrunkPacking_TogglesNarrativeItemsRejectsFourthAndRequiresExactlyThree()
        {
            var context = CreateContext("road_trip");
            var game = context.host.AddComponent<TrunkPackingGame>();
            game.Configure(context.gate, context.runner, context.state, new SettingsModel(), "pack_trunk");
            game.Begin();

            Assert.That(game.ItemIds, Is.EqualTo(new[]
            {
                "repair_tools", "childhood_box", "maya_painting", "noah_recorder", "leo_travel_bag"
            }));
            Assert.That(game.MinimumFirstRunInteractions, Is.EqualTo(4));
            Assert.That(game.MaximumFirstRunInteractions, Is.EqualTo(7));
            Assert.That(game.ConfirmPacking(), Is.False);
            Assert.That(game.VisibleStatus, Does.Contain("exactly three"));

            Assert.That(game.ToggleItem("repair_tools"), Is.True);
            Assert.That(game.ToggleItem("childhood_box"), Is.True);
            Assert.That(game.ToggleItem("maya_painting"), Is.True);
            Assert.That(game.PackedCount, Is.EqualTo(3));
            Assert.That(game.ToggleItem("noah_recorder"), Is.False);
            Assert.That(game.VisibleStatus, Does.Contain("Remove one"));
            Assert.That(game.PackedCount, Is.EqualTo(3));

            Assert.That(game.ToggleItem("childhood_box"), Is.True);
            Assert.That(game.IsPacked("childhood_box"), Is.False);
            Assert.That(game.ToggleItem("noah_recorder"), Is.True);
            Assert.That(game.IsPacked("noah_recorder"), Is.True);
            Assert.That(game.PackedCount, Is.EqualTo(3));
            game.Cancel();

            Assert.That(context.gate.IsBlocked, Is.False);
            Assert.That(context.state.Has("packed_repair_tools"), Is.False);
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator TrunkPacking_ConfirmPersistsOnlyTheThreeSelectedFactsAndReportsOnce()
        {
            var context = CreateContext("road_trip");
            var game = context.host.AddComponent<TrunkPackingGame>();
            game.Configure(context.gate, context.runner, context.state, new SettingsModel(), "pack_trunk");
            var completions = 0;
            game.Completed += _ => completions++;
            game.Begin();

            Assert.That(game.ToggleItem("repair_tools"), Is.True);
            Assert.That(game.ToggleItem("maya_painting"), Is.True);
            Assert.That(game.ToggleItem("leo_travel_bag"), Is.True);
            Assert.That(game.ConfirmPacking(), Is.True);
            Assert.That(game.ConfirmPacking(), Is.False);

            Assert.That(context.state.Has("packed_repair_tools"), Is.True);
            Assert.That(context.state.Has("packed_maya_painting"), Is.True);
            Assert.That(context.state.Has("packed_leo_travel_bag"), Is.True);
            Assert.That(context.state.Has("packed_childhood_box"), Is.False);
            Assert.That(context.state.Has("packed_noah_recorder"), Is.False);
            Assert.That(context.state.Has("packed_guitar"), Is.False);
            Assert.That(context.gate.IsBlocked, Is.False);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(context.state.GetInt("quest_road_trip_objective_pack_trunk_progress"), Is.EqualTo(1));
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator TrunkPacking_UIShowsOnlyFiveNumberedChoicesAndConfirmWithoutOverlap()
        {
            var context = CreateContext("road_trip");
            var game = context.host.AddComponent<TrunkPackingGame>();
            game.Configure(context.gate, context.runner, context.state, new SettingsModel(), "pack_trunk");
            game.Begin();
            yield return null;

            var buttons = context.host.GetComponentsInChildren<Button>(true);
            Assert.That(buttons.Length, Is.EqualTo(6));
            var expectedNames = new[] { "Repair tools", "Childhood box", "Maya's painting", "Noah's recorder", "Leo's travel bag" };
            for (var index = 0; index < expectedNames.Length; index++)
            {
                var item = Find(context.host, $"Trunk Item {index + 1}");
                Assert.That(item, Is.Not.Null);
                var label = item.GetComponentInChildren<Text>(true);
                Assert.That(label.text, Does.StartWith($"{index + 1}. "));
                Assert.That(label.text, Does.Contain(expectedNames[index]));
                Assert.That(item.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(80f));
            }
            Assert.That(Find(context.host, "Trunk Confirm"), Is.Not.Null);
            Assert.That(Find(context.host, "Cell 0,0"), Is.Null);
            Assert.That(Find(context.host, "Rotate selected item (R)"), Is.Null);
            Assert.That(Find(context.host, "Place at selected grid cell"), Is.Null);

            Click(context.host, "Trunk Item 1");
            Assert.That(Find(context.host, "Trunk Item 1").GetComponentInChildren<Text>(true).text, Does.StartWith("1. [X]"));
            AssertDirectChildrenDoNotOverlap(Find(context.host, "Minigame Panel").GetComponent<RectTransform>());
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator TrunkPacking_ChineseCopyNamesItemsAndExplainsHowToReplaceTheFourthChoice()
        {
            var context = CreateContext("road_trip");
            var settings = new SettingsModel { Language = GameLanguage.SimplifiedChinese };
            var game = context.host.AddComponent<TrunkPackingGame>();
            game.Configure(context.gate, context.runner, context.state, settings, "pack_trunk");
            game.Begin();
            yield return null;

            try
            {
                var labels = context.host.GetComponentsInChildren<Text>(true);
                Assert.That(labels, Has.Some.Matches<Text>(label => label.text.Contains("选择三件")));
                Assert.That(labels, Has.Some.Matches<Text>(label => label.text.Contains("维修工具")));
                Assert.That(labels, Has.Some.Matches<Text>(label => label.text.Contains("童年纪念箱")));
                Assert.That(labels, Has.Some.Matches<Text>(label => label.text.Contains("玛雅的画")));
                Assert.That(labels, Has.Some.Matches<Text>(label => label.text.Contains("诺亚的录音机")));
                Assert.That(labels, Has.Some.Matches<Text>(label => label.text.Contains("利奥的旅行包")));

                game.ToggleItem("repair_tools");
                game.ToggleItem("childhood_box");
                game.ToggleItem("maya_painting");
                Assert.That(game.ToggleItem("noah_recorder"), Is.False);
                Assert.That(game.VisibleStatus, Does.Contain("先取消一件"));
                Assert.That(game.ToggleItem("repair_tools"), Is.True);
                Assert.That(game.VisibleStatus, Does.Contain("已取消"));
            }
            finally
            {
                GameText.Use(GameLanguage.English);
            }
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator SkipMinigames_TrunkPackingCompletesWithoutOverlayOrPackedFacts()
        {
            var context = CreateContext("road_trip");
            var settings = new SettingsModel { SkipMinigames = true };
            var game = context.host.AddComponent<TrunkPackingGame>();
            game.Configure(context.gate, context.runner, context.state, settings, "pack_trunk");
            var completions = 0;
            game.Completed += _ => completions++;

            game.Begin("trunk_packing");
            game.Begin("trunk_packing");

            Assert.That(game.IsRunning, Is.False);
            Assert.That(context.gate.IsBlocked, Is.False);
            Assert.That(game.PackedCount, Is.Zero);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(context.state.GetInt("quest_road_trip_objective_pack_trunk_progress"), Is.EqualTo(1));
            Assert.That(Find(context.host, "Minigame Panel"), Is.Null);
            foreach (var itemId in game.ItemIds)
            {
                Assert.That(context.state.Has($"packed_{itemId}"), Is.False);
            }
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator SkipMinigames_CompletesWithoutSimulatingInteractionsAndRemainsIdempotent()
        {
            var context = CreateContext("diner_shift");
            var settings = new SettingsModel { SkipMinigames = true };
            var game = context.host.AddComponent<DinerShiftGame>();
            game.Configure(context.gate, context.runner, context.state, settings, "serve_diner");
            var completions = 0;
            game.Completed += _ => completions++;

            game.Begin("diner_shift");
            game.Begin("diner_shift");

            Assert.That(game.IsRunning, Is.False);
            Assert.That(context.gate.IsBlocked, Is.False);
            Assert.That(game.DeliveredOrderCount, Is.Zero);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(context.state.GetInt("quest_diner_shift_objective_serve_diner_progress"), Is.EqualTo(1));
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator DinerShift_BeginCreatesLargeMouseTargetsAndAnEventSystem()
        {
            var context = CreateContext("diner_shift");
            var game = context.host.AddComponent<DinerShiftGame>();
            game.Configure(context.gate, context.runner, context.state, new SettingsModel(), "serve_diner");

            game.Begin();
            yield return null;

            Assert.That(Object.FindFirstObjectByType<EventSystem>(), Is.Not.Null);
            var buttons = context.host.GetComponentsInChildren<Button>();
            Assert.That(buttons.Length, Is.EqualTo(6));
            foreach (var button in buttons)
            {
                Assert.That(button.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(80f));
            }
            Assert.That(game.MinimumFirstRunInteractions, Is.EqualTo(6));
            Assert.That(game.MaximumFirstRunInteractions, Is.EqualTo(12));
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator DinerShift_CancelThenRestartDoesNotDuplicateItsMouseTargets()
        {
            var context = CreateContext("diner_shift");
            var game = context.host.AddComponent<DinerShiftGame>();
            game.Configure(context.gate, context.runner, context.state, new SettingsModel(), "serve_diner");

            game.Begin();
            game.Cancel();
            game.Begin();
            yield return null;

            Assert.That(context.host.GetComponentsInChildren<Button>().Length, Is.EqualTo(6));
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator DinerShift_MouseButtonsShowSelectionAndDeliveryFeedback()
        {
            var context = CreateContext("diner_shift");
            var game = context.host.AddComponent<DinerShiftGame>();
            game.Configure(context.gate, context.runner, context.state, new SettingsModel(), "serve_diner");
            game.Begin();
            yield return null;

            Click(context.host, "1. Take coffee");
            Assert.That(game.VisibleStatus, Does.Contain("Coffee selected"));
            Click(context.host, "Q. Coffee table");
            Assert.That(game.VisibleStatus, Does.Contain("Coffee delivered"));
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator DinerShift_WrongVisibleTableExplainsWhatToDoInsteadOfFailingSilently()
        {
            var context = CreateContext("diner_shift");
            var game = context.host.AddComponent<DinerShiftGame>();
            game.Configure(context.gate, context.runner, context.state, new SettingsModel(), "serve_diner");
            game.Begin();
            yield return null;

            Click(context.host, "2. Take pie");
            Click(context.host, "Q. Coffee table");
            Assert.That(game.VisibleStatus, Does.Contain("Pie belongs"));
            Assert.That(game.DeliveredOrderCount, Is.EqualTo(0));
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator DinerShift_SelectionDrawsMatchingServiceConnectionAndDeliveredOrderDisappears()
        {
            var context = CreateContext("diner_shift");
            var game = context.host.AddComponent<DinerShiftGame>();
            game.Configure(context.gate, context.runner, context.state, new SettingsModel(), "serve_diner");
            game.Begin();
            yield return null;

            var coffeeIcon = Find(context.host, "Order Icon coffee");
            var link = Find(context.host, "Delivery Link coffee");
            Assert.That(coffeeIcon, Is.Not.Null);
            Assert.That(link.activeSelf, Is.False);
            game.SelectOrder("coffee");
            Assert.That(link.activeSelf, Is.True);
            game.DeliverToTable("table_coffee");
            Assert.That(coffeeIcon.activeSelf, Is.False);
            Assert.That(link.activeSelf, Is.False);
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator DinerShift_ChineseModeExplainsSelectThenDeliverAndKeepsVisibleFeedback()
        {
            var context = CreateContext("diner_shift");
            var settings = new SettingsModel { Language = GameLanguage.SimplifiedChinese };
            var game = context.host.AddComponent<DinerShiftGame>();
            game.Configure(context.gate, context.runner, context.state, settings, "serve_diner");
            game.Begin();
            yield return null;
            try
            {
                Assert.That(context.host.GetComponentsInChildren<Text>(true), Has.Some.Matches<Text>(label => label.text.Contains("先选择餐点")));
                game.SelectOrder("coffee");
                Assert.That(game.VisibleStatus, Does.Contain("咖啡"));
                game.DeliverToTable("table_coffee");
                Assert.That(game.VisibleStatus, Does.Contain("已送达"));
            }
            finally
            {
                GameText.Use(GameLanguage.English);
            }
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator WiringGame_RotatesVisiblePathAndShowsConnectedColor()
        {
            var context = CreateContext("wiring_job");
            var game = context.host.AddComponent<WiringGame>();
            game.Configure(context.gate, context.runner, context.state, new SettingsModel(), "wire_recorder");
            game.Begin();
            yield return null;

            var path = Find(context.host, "Wire Path 1").GetComponent<RectTransform>();
            var before = path.localEulerAngles.z;
            game.RotateTile(0);
            Assert.That(path.localEulerAngles.z, Is.Not.EqualTo(before));
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator TrunkPacking_NumberKeyTogglesTheSameVisibleChoice()
        {
            var context = CreateContext("road_trip");
            var game = context.host.AddComponent<TrunkPackingGame>();
            game.Configure(context.gate, context.runner, context.state, new SettingsModel(), "pack_trunk");
            game.Begin();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1));
                InputSystem.Update();
                game.SendMessage("Update");
                Assert.That(game.IsPacked("repair_tools"), Is.True);
                Assert.That(Find(context.host, "Trunk Item 1").GetComponentInChildren<Text>(true).text, Does.StartWith("1. [X]"));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                game.SendMessage("Update");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1));
                InputSystem.Update();
                game.SendMessage("Update");
                Assert.That(game.IsPacked("repair_tools"), Is.False);
                Assert.That(Find(context.host, "Trunk Item 1").GetComponentInChildren<Text>(true).text, Does.StartWith("1. Repair"));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
            yield return Destroy(context.host);
        }

        [UnityTest]
        public IEnumerator DinerShift_KeyboardOneThenQUsesTheSameVisibleInteractionPath()
        {
            var context = CreateContext("diner_shift");
            var game = context.host.AddComponent<DinerShiftGame>();
            game.Configure(context.gate, context.runner, context.state, new SettingsModel(), "serve_diner");
            game.Begin();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit1));
                InputSystem.Update();
                game.SendMessage("Update");
                Assert.That(game.VisibleStatus, Does.Contain("Coffee selected"));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                game.SendMessage("Update");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q));
                InputSystem.Update();
                game.SendMessage("Update");
                Assert.That(game.VisibleStatus, Does.Contain("Coffee delivered"));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
            yield return Destroy(context.host);
        }

        private static (GameObject host, InputGate gate, NarrativeStateStore state, QuestRunner runner) CreateContext(string questId)
        {
            var host = new GameObject("Minigame Test Host");
            var state = new NarrativeStateStore();
            var runner = new QuestRunner(state);
            var quest = ScriptableObject.CreateInstance<QuestAsset>();
            quest.id = questId;
            quest.objectives.Add(new QuestObjective { id = questId == "diner_shift" ? "serve_diner" : questId == "wiring_job" ? "wire_recorder" : "pack_trunk" });
            runner.StartQuest(quest);
            return (host, host.AddComponent<InputGate>(), state, runner);
        }

        private static void AssertDirectChildrenDoNotOverlap(RectTransform panel)
        {
            for (var leftIndex = 0; leftIndex < panel.childCount; leftIndex++)
            {
                var left = panel.GetChild(leftIndex).GetComponent<RectTransform>();
                if (left == null || !left.gameObject.activeSelf)
                {
                    continue;
                }

                for (var rightIndex = leftIndex + 1; rightIndex < panel.childCount; rightIndex++)
                {
                    var right = panel.GetChild(rightIndex).GetComponent<RectTransform>();
                    if (right == null || !right.gameObject.activeSelf)
                    {
                        continue;
                    }

                    Assert.That(AnchoredRect(left).Overlaps(AnchoredRect(right)), Is.False,
                        $"{left.name} overlaps {right.name}");
                }
            }
        }

        private static Rect AnchoredRect(RectTransform rect)
        {
            return new Rect(
                rect.anchoredPosition.x + rect.rect.x,
                rect.anchoredPosition.y + rect.rect.y,
                rect.rect.width,
                rect.rect.height);
        }

        private static IEnumerator Destroy(GameObject host)
        {
            Object.Destroy(host);
            yield return null;
        }

        private static void Click(GameObject host, string buttonName)
        {
            var button = System.Array.Find(host.GetComponentsInChildren<Button>(true), value => value.name.Contains(buttonName));
            Assert.That(button, Is.Not.Null, buttonName);
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
        }

        private static GameObject Find(GameObject host, string objectName)
        {
            foreach (var item in host.GetComponentsInChildren<Transform>(true))
                if (item.name == objectName) return item.gameObject;
            return null;
        }
    }
}
