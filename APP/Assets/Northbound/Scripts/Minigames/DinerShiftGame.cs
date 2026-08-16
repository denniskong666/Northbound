using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Northbound.UI;

namespace Northbound.Minigames
{
    public sealed class DinerShiftGame : MinigameController
    {
        private static readonly string[] OrderIds = { "coffee", "pie", "soup" };
        private readonly HashSet<string> delivered = new HashSet<string>();
        private string selectedOrder;
        private RectTransform panel;
        private bool reportPending;
        private readonly Dictionary<string, GameObject> orderIcons = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, GameObject> deliveryLinks = new Dictionary<string, GameObject>();

        public override string Id => "diner_shift";
        public override int MinimumFirstRunInteractions => 6;
        public override int MaximumFirstRunInteractions => 12;
        public int DeliveredOrderCount => delivered.Count;

        public bool SelectOrder(string orderId)
        {
            if (!IsRunning || System.Array.IndexOf(OrderIds, orderId) < 0)
            {
                return false;
            }

            if (delivered.Contains(orderId))
            {
                SetVisibleStatus(GameText.T(
                    $"{Display(orderId)} is already delivered. Choose another order.",
                    $"{Display(orderId)}已经送达，请选择另一份餐点。"), new Color(1f, 0.76f, 0.45f, 1f));
                return false;
            }

            selectedOrder = orderId;
            foreach (var pair in deliveryLinks) pair.Value.SetActive(pair.Key == orderId);
            SetVisibleStatus(GameText.T(
                $"{Display(orderId)} selected — choose its matching table.",
                $"已选择{Display(orderId)}——请选择对应餐桌。"));
            return true;
        }

        public bool DeliverToTable(string tableId)
        {
            if (string.IsNullOrWhiteSpace(selectedOrder))
            {
                SetVisibleStatus(GameText.T("Choose an order before walking to a table.", "请先选择餐点，再送往餐桌。"), new Color(1f, 0.65f, 0.42f, 1f));
                return false;
            }

            if (tableId != $"table_{selectedOrder}")
            {
                SetVisibleStatus(GameText.T(
                    $"{Display(selectedOrder)} belongs at its matching table. Try again.",
                    $"{Display(selectedOrder)}应该送到对应餐桌，请重试。"), new Color(1f, 0.65f, 0.42f, 1f));
                return false;
            }

            var orderId = selectedOrder;
            selectedOrder = null;
            delivered.Add(orderId);
            if (orderIcons.TryGetValue(orderId, out var icon)) icon.SetActive(false);
            if (deliveryLinks.TryGetValue(orderId, out var link)) link.SetActive(false);
            SetVisibleStatus(GameText.T(
                $"{Display(orderId)} delivered. {delivered.Count} of {OrderIds.Length} orders complete.",
                $"{Display(orderId)}已送达。已完成 {delivered.Count}/{OrderIds.Length} 份订单。"), new Color(0.55f, 1f, 0.68f, 1f));
            if (delivered.Count == OrderIds.Length)
            {
                reportPending = !Complete();
                if (reportPending)
                {
                    SetVisibleStatus(GameText.T("Service recorded. Press Enter to retry the quest update.", "服务已记录。按回车重试任务更新。"), new Color(1f, 0.65f, 0.42f, 1f));
                    CreateRetryButton();
                }
            }

            return true;
        }

        public bool DeliverOrder(string orderId, string tableId)
        {
            return SelectOrder(orderId) && DeliverToTable(tableId);
        }

        public bool RetryCompletion()
        {
            if (!reportPending) return false;
            reportPending = !Complete();
            if (!reportPending)
            {
                SetVisibleStatus(GameText.T("Service recorded. Returning to Greybridge.", "服务已记录，正在返回格雷布里奇。"), new Color(0.55f, 1f, 0.68f, 1f));
            }
            return !reportPending;
        }

        protected override void OnBegin()
        {
            delivered.Clear();
            selectedOrder = null;
            reportPending = false;
            orderIcons.Clear();
            deliveryLinks.Clear();
            panel = CreateOverlay(
                GameText.T("DINER SHIFT", "餐馆值班"),
                GameText.T("Choose a visible order, then carry it to its matching table. Keyboard: 1–3, then Q/W/E.", "先选择餐点，再送到对应餐桌。键盘：先按 1–3，再按 Q/W/E。"));
            SetVisibleStatus(GameText.T("Choose an order: Coffee, Pie, or Soup.", "先选择餐点：咖啡、派或汤。"));
            CreateButton(panel, GameText.T("1. Take coffee", "1. 拿咖啡"), new Vector2(-250, 80), () => SelectOrder("coffee"));
            CreateButton(panel, GameText.T("2. Take pie", "2. 拿派"), new Vector2(-250, -30), () => SelectOrder("pie"));
            CreateButton(panel, GameText.T("3. Take soup", "3. 拿汤"), new Vector2(-250, -140), () => SelectOrder("soup"));
            CreateButton(panel, GameText.T("Q. Coffee table", "Q. 咖啡桌"), new Vector2(250, 80), () => DeliverToTable("table_coffee"));
            CreateButton(panel, GameText.T("W. Pie table", "W. 派餐桌"), new Vector2(250, -30), () => DeliverToTable("table_pie"));
            CreateButton(panel, GameText.T("E. Soup table", "E. 汤餐桌"), new Vector2(250, -140), () => DeliverToTable("table_soup"));
            for (var index = 0; index < OrderIds.Length; index++)
            {
                var id = OrderIds[index];
                var y = 80 - index * 110;
                orderIcons[id] = CreateServiceGraphic(panel, $"Order Icon {id}", new Vector2(-500, y), new Vector2(52, 52), OrderColor(id));
                deliveryLinks[id] = CreateServiceGraphic(panel, $"Delivery Link {id}", new Vector2(0, y), new Vector2(330, 10), OrderColor(id));
                deliveryLinks[id].SetActive(false);
                CreateServiceGraphic(panel, $"Table Marker {id}", new Vector2(500, y), new Vector2(52, 52), OrderColor(id));
            }
        }

        protected override void Update()
        {
            base.Update();
            if (!IsRunning || Keyboard.current == null)
            {
                return;
            }

            if (KeyPressed(Key.Digit1)) SelectOrder("coffee");
            if (KeyPressed(Key.Digit2)) SelectOrder("pie");
            if (KeyPressed(Key.Digit3)) SelectOrder("soup");
            if (KeyPressed(Key.Q)) DeliverToTable("table_coffee");
            if (KeyPressed(Key.W)) DeliverToTable("table_pie");
            if (KeyPressed(Key.E)) DeliverToTable("table_soup");
            if (reportPending && (KeyPressed(Key.Enter) || KeyPressed(Key.Space))) RetryCompletion();
        }

        private static string Display(string orderId)
        {
            if (GameText.IsChinese)
            {
                return orderId switch { "coffee" => "咖啡", "pie" => "派", "soup" => "汤", _ => orderId };
            }
            return char.ToUpperInvariant(orderId[0]) + orderId.Substring(1);
        }

        private void CreateRetryButton()
        {
            if (panel != null && panel.Find("Retry completion (Enter)") == null)
            {
                CreateButton(panel, "Retry completion (Enter)", new Vector2(0, -260), () => RetryCompletion());
            }
        }

        private static GameObject CreateServiceGraphic(RectTransform panel, string name, Vector2 position, Vector2 size, Color color)
        {
            var graphic = new GameObject(name, typeof(RectTransform), typeof(Image));
            graphic.transform.SetParent(panel, false);
            var rect = graphic.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = graphic.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return graphic;
        }

        private static Color OrderColor(string id) => id switch
        {
            "coffee" => new Color(.82f, .54f, .28f, 1f),
            "pie" => new Color(.93f, .43f, .52f, 1f),
            _ => new Color(.42f, .78f, .58f, 1f)
        };
    }
}
