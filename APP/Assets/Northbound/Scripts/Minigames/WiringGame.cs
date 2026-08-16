using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Northbound.Minigames
{
    public sealed class WiringGame : MinigameController
    {
        public const int TileCount = 4;
        private static readonly int[] AuthoredLayout = { 1, 2, 3, 1 };
        // Each value faces the next segment in the authored source → recorder route.
        private static readonly int[] ConnectedLayout = { 2, 1, 3, 0 };
        private readonly int[] rotations = new int[TileCount];
        private Text feedbackLabel;
        private Text[] tileLabels = new Text[TileCount];
        private readonly RectTransform[] pathVisuals = new RectTransform[TileCount];

        public override string Id => "wiring_game";
        public override int MinimumFirstRunInteractions => 7;
        public override int MaximumFirstRunInteractions => 19;
        public string ConnectionFeedback => IsConnected() ? "SOURCE CONNECTED TO RECORDER" : "SOURCE → TILE 1 → TILE 2 → TILE 3 → RECORDER";

        public void RotateTile(int tileIndex)
        {
            if (!IsRunning || tileIndex < 0 || tileIndex >= TileCount)
            {
                return;
            }

            rotations[tileIndex] = (rotations[tileIndex] + 1) % 4;
            RefreshVisualFeedback();
            if (IsConnected())
            {
                Complete();
            }
        }

        public int GetTileRotation(int tileIndex) => tileIndex >= 0 && tileIndex < TileCount ? rotations[tileIndex] : -1;

        public int GetAuthoredRotation(int tileIndex) => tileIndex >= 0 && tileIndex < TileCount ? AuthoredLayout[tileIndex] : -1;

        public int GetConnectedRotation(int tileIndex) => tileIndex >= 0 && tileIndex < TileCount ? ConnectedLayout[tileIndex] : -1;

        public void ResetLayout()
        {
            for (var i = 0; i < TileCount; i++)
            {
                rotations[i] = AuthoredLayout[i];
            }
            RefreshVisualFeedback();
        }

        protected override void OnBegin()
        {
            ResetLayout();
            var panel = CreateOverlay("RECORDER WIRING", "Rotate each visible path tile to carry the source signal to the recorder. Keyboard: 1–4. R resets.");
            for (var tile = 0; tile < TileCount; tile++)
            {
                var capturedTile = tile;
                CreateButton(panel, $"Rotate wire {tile + 1}", new Vector2(tile % 2 == 0 ? -240 : 240, tile < 2 ? 50 : -70), () => RotateTile(capturedTile));
                tileLabels[tile] = CreateLabel(panel, $"Tile {tile + 1}: {rotations[tile]}", new Vector2(tile % 2 == 0 ? -240 : 240, tile < 2 ? 115 : -5), 20);
                pathVisuals[tile] = CreatePath(panel, tile);
            }
            CreateButton(panel, "Reset authored layout (R)", new Vector2(0, -190), ResetLayout);
            feedbackLabel = CreateLabel(panel, ConnectionFeedback, new Vector2(0, -285), 24);
            RefreshVisualFeedback();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsRunning || Keyboard.current == null)
            {
                return;
            }

            if (KeyPressed(Key.Digit1)) RotateTile(0);
            if (KeyPressed(Key.Digit2)) RotateTile(1);
            if (KeyPressed(Key.Digit3)) RotateTile(2);
            if (KeyPressed(Key.Digit4)) RotateTile(3);
            if (KeyPressed(Key.R)) ResetLayout();
        }

        private bool IsConnected()
        {
            for (var i = 0; i < TileCount; i++)
            {
                if (rotations[i] != ConnectedLayout[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void RefreshVisualFeedback()
        {
            for (var tile = 0; tile < TileCount; tile++)
            {
                if (tileLabels[tile] != null) tileLabels[tile].text = $"Tile {tile + 1}: {rotations[tile]}";
                if (pathVisuals[tile] != null)
                {
                    pathVisuals[tile].localEulerAngles = new Vector3(0, 0, rotations[tile] * -90f);
                    pathVisuals[tile].GetComponent<Image>().color = rotations[tile] == ConnectedLayout[tile]
                        ? new Color(.35f, 1f, .63f, 1f) : new Color(1f, .71f, .2f, 1f);
                }
            }
            if (feedbackLabel != null) feedbackLabel.text = ConnectionFeedback;
            SetVisibleStatus(IsConnected() ? "Signal connected — recorder is ready." : "Rotate the visible wire tiles until the source reaches the recorder.");
        }

        private static RectTransform CreatePath(RectTransform panel, int tile)
        {
            var path = new GameObject($"Wire Path {tile + 1}", typeof(RectTransform), typeof(Image));
            path.transform.SetParent(panel, false);
            var rect = path.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(tile % 2 == 0 ? -240 : 240, tile < 2 ? 50 : -70);
            rect.sizeDelta = new Vector2(12, 62);
            var image = path.GetComponent<Image>();
            image.color = new Color(1f, .71f, .2f, 1f);
            image.raycastTarget = false;
            return rect;
        }
    }
}
