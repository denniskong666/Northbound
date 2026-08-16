using System.Collections.Generic;
using Northbound.Narrative;
using Northbound.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Northbound.Minigames
{
    public sealed class TrunkPackingGame : MinigameController
    {
        private const int RequiredItemCount = 3;
        private static readonly string[] AuthoredItemIds =
        {
            "repair_tools",
            "childhood_box",
            "maya_painting",
            "noah_recorder",
            "leo_travel_bag"
        };

        private readonly HashSet<string> packedItems = new HashSet<string>();
        private readonly Dictionary<string, Button> itemButtons = new Dictionary<string, Button>();
        private NarrativeStateStore state;

        public override string Id => "trunk_packing";
        public override int MinimumFirstRunInteractions => 4;
        public override int MaximumFirstRunInteractions => 7;
        public int PackedCount => packedItems.Count;
        public IReadOnlyList<string> ItemIds => AuthoredItemIds;

        public bool IsPacked(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && packedItems.Contains(itemId);
        }

        public bool ToggleItem(string itemId)
        {
            if (!IsRunning || System.Array.IndexOf(AuthoredItemIds, itemId) < 0)
            {
                return false;
            }

            if (packedItems.Remove(itemId))
            {
                RefreshChoiceVisuals();
                SetVisibleStatus(GameText.T(
                    $"{DisplayName(itemId)} removed. Choose {RequiredItemCount - packedItems.Count} more.",
                    $"已取消{DisplayName(itemId)}。还需选择 {RequiredItemCount - packedItems.Count} 件。"));
                return true;
            }

            if (packedItems.Count == RequiredItemCount)
            {
                SetVisibleStatus(GameText.T(
                    "Three items are already selected. Remove one before choosing another.",
                    "已经选了三件。请先取消一件，再选择其他物品。"), new Color(1f, .65f, .42f, 1f));
                return false;
            }

            packedItems.Add(itemId);
            RefreshChoiceVisuals();
            SetVisibleStatus(packedItems.Count == RequiredItemCount
                ? GameText.T(
                    "Three items selected. Confirm the load, or click an item to change it.",
                    "已选三件。确认装车，或再次点击某件物品进行更换。")
                : GameText.T(
                    $"{DisplayName(itemId)} selected. Choose {RequiredItemCount - packedItems.Count} more.",
                    $"已选择{DisplayName(itemId)}。还需选择 {RequiredItemCount - packedItems.Count} 件。"),
                new Color(.55f, 1f, .68f, 1f));
            return true;
        }

        public bool ConfirmPacking()
        {
            if (!IsRunning || packedItems.Count != RequiredItemCount)
            {
                if (IsRunning)
                {
                    SetVisibleStatus(GameText.T(
                        "Choose exactly three items before confirming.",
                        "请选满三件物品后再确认。"), new Color(1f, .65f, .42f, 1f));
                }
                return false;
            }

            if (!Complete())
            {
                return false;
            }

            foreach (var itemId in AuthoredItemIds)
            {
                state?.Set($"packed_{itemId}", packedItems.Contains(itemId));
            }
            return true;
        }

        protected override void OnConfigured(NarrativeStateStore configuredState)
        {
            state = configuredState;
        }

        protected override void OnBegin()
        {
            packedItems.Clear();
            itemButtons.Clear();

            var title = GameText.T("PACK THE TRUNK", "整理后备箱");
            var instruction = GameText.T(
                "Choose exactly three things to bring. Click again to remove. Keyboard: 1-5, Enter.",
                "选择三件带上路的物品。再次点击可取消。键盘：1-5，回车确认。");
            var panel = CreateOverlay(title, instruction);
            ArrangeOverlay(panel, title, instruction);
            SetVisibleStatus(GameText.T(
                "Choose three items for the station wagon.",
                "为旅行车选择三件物品。"));

            for (var index = 0; index < AuthoredItemIds.Length; index++)
            {
                var capturedItem = AuthoredItemIds[index];
                var position = index switch
                {
                    0 => new Vector2(-255f, 110f),
                    1 => new Vector2(255f, 110f),
                    2 => new Vector2(-255f, 10f),
                    3 => new Vector2(255f, 10f),
                    _ => new Vector2(0f, -90f)
                };
                var button = CreateButton(panel, ChoiceLabel(index, capturedItem, false), position, new Vector2(430f, 82f), () => ToggleItem(capturedItem));
                button.gameObject.name = $"Trunk Item {index + 1}";
                FitButtonLabel(button);
                itemButtons[capturedItem] = button;
            }

            var confirm = CreateButton(
                panel,
                GameText.T("Confirm 3 items (Enter)", "确认 3 件物品（回车）"),
                new Vector2(0f, -315f),
                new Vector2(430f, 72f),
                () => ConfirmPacking());
            confirm.gameObject.name = "Trunk Confirm";
            FitButtonLabel(confirm);
            RefreshChoiceVisuals();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsRunning || Keyboard.current == null)
            {
                return;
            }

            if (KeyPressed(Key.Digit1)) ToggleItem(AuthoredItemIds[0]);
            if (KeyPressed(Key.Digit2)) ToggleItem(AuthoredItemIds[1]);
            if (KeyPressed(Key.Digit3)) ToggleItem(AuthoredItemIds[2]);
            if (KeyPressed(Key.Digit4)) ToggleItem(AuthoredItemIds[3]);
            if (KeyPressed(Key.Digit5)) ToggleItem(AuthoredItemIds[4]);
            if (KeyPressed(Key.Enter)) ConfirmPacking();
        }

        private void RefreshChoiceVisuals()
        {
            for (var index = 0; index < AuthoredItemIds.Length; index++)
            {
                var itemId = AuthoredItemIds[index];
                if (!itemButtons.TryGetValue(itemId, out var button))
                {
                    continue;
                }

                var selected = packedItems.Contains(itemId);
                button.GetComponent<Image>().color = selected
                    ? new Color(.28f, .66f, .46f, 1f)
                    : new Color(.17f, .35f, .5f, 1f);
                var label = button.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    label.text = ChoiceLabel(index, itemId, selected);
                }
            }
        }

        private static string ChoiceLabel(int index, string itemId, bool selected)
        {
            var marker = selected ? "[X] " : string.Empty;
            return $"{index + 1}. {marker}{DisplayName(itemId)}";
        }

        private static string DisplayName(string itemId)
        {
            return itemId switch
            {
                "repair_tools" => GameText.T("Repair tools", "维修工具"),
                "childhood_box" => GameText.T("Childhood box", "童年纪念箱"),
                "maya_painting" => GameText.T("Maya's painting", "玛雅的画"),
                "noah_recorder" => GameText.T("Noah's recorder", "诺亚的录音机"),
                "leo_travel_bag" => GameText.T("Leo's travel bag", "利奥的旅行包"),
                _ => itemId
            };
        }

        private static void ArrangeOverlay(RectTransform panel, string title, string instruction)
        {
            SetDirectLabelRect(panel, title, new Vector2(0f, 305f), new Vector2(1000f, 68f));
            SetDirectLabelRect(panel, instruction, new Vector2(0f, 220f), new Vector2(1000f, 80f));
            var status = panel.Find("Minigame Status") as RectTransform;
            if (status != null)
            {
                status.anchoredPosition = new Vector2(0f, -210f);
                status.sizeDelta = new Vector2(1000f, 78f);
            }
        }

        private static void SetDirectLabelRect(RectTransform panel, string text, Vector2 position, Vector2 size)
        {
            foreach (Transform child in panel)
            {
                var label = child.GetComponent<Text>();
                if (label == null || label.text != text)
                {
                    continue;
                }

                var rect = label.rectTransform;
                rect.anchoredPosition = position;
                rect.sizeDelta = size;
                return;
            }
        }

        private static void FitButtonLabel(Button button)
        {
            var label = button.GetComponentInChildren<Text>(true);
            if (label == null)
            {
                return;
            }

            label.fontSize = 27;
            label.rectTransform.sizeDelta = new Vector2(
                button.GetComponent<RectTransform>().sizeDelta.x - 28f,
                button.GetComponent<RectTransform>().sizeDelta.y - 12f);
        }
    }
}
