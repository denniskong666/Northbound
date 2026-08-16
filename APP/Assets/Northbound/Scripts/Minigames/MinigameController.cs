using System;
using System.Collections.Generic;
using Northbound.Core;
using Northbound.Narrative;
using Northbound.Quests;
using Northbound.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Northbound.Minigames
{
    public abstract class MinigameController : MonoBehaviour
    {
        private IDisposable inputLease;
        private InputGate inputGate;
        private QuestRunner questRunner;
        private SettingsModel settings;
        private string objectiveId;
        private bool hasCompleted;
        private Canvas overlayCanvas;
        private Text statusLabel;
        private readonly HashSet<Key> heldKeys = new HashSet<Key>();

        public event Action<string> Completed;

        public bool IsRunning { get; private set; }

        /// <summary>Short, player-visible confirmation of the most recent minigame action.</summary>
        public string VisibleStatus { get; private set; } = string.Empty;

        public abstract string Id { get; }

        // These are interaction-count estimates, not claims about observed player time.
        public abstract int MinimumFirstRunInteractions { get; }

        public abstract int MaximumFirstRunInteractions { get; }

        public void Configure(InputGate gate, QuestRunner runner, NarrativeStateStore state, SettingsModel settingsModel, string reportObjectiveId)
        {
            inputGate = gate;
            questRunner = runner;
            settings = settingsModel;
            objectiveId = reportObjectiveId;
            hasCompleted = false;
            OnConfigured(state);
        }

        public void Begin()
        {
            if (IsRunning || hasCompleted)
            {
                return;
            }

            IsRunning = true;
            heldKeys.Clear();
            gameObject.SetActive(true);
            inputLease = inputGate?.Acquire(this);
            if (settings != null && settings.SkipMinigames)
            {
                Complete();
                return;
            }

            OnBegin();
        }

        public void Begin(string id)
        {
            if (id == Id)
            {
                Begin();
            }
        }

        public void Cancel()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            OnCancel();
            ReleaseInput();
            gameObject.SetActive(false);
        }

        protected bool Complete()
        {
            if (!IsRunning)
            {
                return false;
            }

            if (questRunner == null || !questRunner.Report(objectiveId, 1))
            {
                return false;
            }

            IsRunning = false;
            hasCompleted = true;
            Completed?.Invoke(Id);
            ReleaseInput();
            gameObject.SetActive(false);
            return true;
        }

        protected virtual void Update()
        {
            if (IsRunning && KeyPressed(Key.Escape))
            {
                Cancel();
            }
        }

        /// <summary>Reliable rising-edge input for keyboard-only minigames.</summary>
        protected bool KeyPressed(Key key)
        {
            var keyboard = Keyboard.current;
            var down = keyboard != null && keyboard[key] != null && keyboard[key].isPressed;
            if (!down)
            {
                heldKeys.Remove(key);
                return false;
            }

            return heldKeys.Add(key);
        }

        protected virtual void OnConfigured(NarrativeStateStore state) { }

        protected abstract void OnBegin();

        protected virtual void OnCancel() { }

        private void OnDisable()
        {
            if (IsRunning)
            {
                Cancel();
            }
        }

        private void ReleaseInput()
        {
            inputLease?.Dispose();
            inputLease = null;
        }

        protected RectTransform CreateOverlay(string title, string instruction)
        {
            var canvas = overlayCanvas != null ? overlayCanvas : GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = GetComponentInChildren<Canvas>(true);
            }

            if (canvas == null)
            {
                var canvasObject = new GameObject("Minigame Canvas", typeof(RectTransform));
                canvasObject.transform.SetParent(transform, false);
                canvas = canvasObject.AddComponent<Canvas>();
            }

            overlayCanvas = canvas;

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            if (canvas.GetComponent<CanvasScaler>() == null)
            {
                var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            EnsureEventSystem();
            ClearOverlay(canvas.transform);
            var panel = new GameObject("Minigame Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1180, 760);
            panel.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.09f, 0.96f);
            CreateLabel(panel.transform, title, new Vector2(0, 270), 56);
            CreateLabel(panel.transform, instruction, new Vector2(0, 190), 32);
            statusLabel = CreateLabel(panel.transform, string.Empty, new Vector2(0, -245), 34);
            statusLabel.name = "Minigame Status";
            statusLabel.color = new Color(1f, 0.87f, 0.46f, 1f);
            return rect;
        }

        protected Button CreateButton(RectTransform panel, string text, Vector2 position, UnityAction action)
        {
            return CreateButton(panel, text, position, new Vector2(430, 82), action);
        }

        protected Button CreateButton(RectTransform panel, string text, Vector2 position, Vector2 size, UnityAction action)
        {
            var buttonObject = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panel, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            buttonObject.GetComponent<Image>().color = new Color(0.17f, 0.35f, 0.5f, 1f);
            buttonObject.GetComponent<Button>().onClick.AddListener(action);
            CreateLabel(buttonObject.transform, text, Vector2.zero, 31);
            return buttonObject.GetComponent<Button>();
        }

        protected void SetVisibleStatus(string message, Color? color = null)
        {
            VisibleStatus = message ?? string.Empty;
            if (statusLabel != null)
            {
                statusLabel.text = VisibleStatus;
                statusLabel.color = color ?? new Color(1f, 0.87f, 0.46f, 1f);
            }
        }

        private static void ClearOverlay(Transform canvasTransform)
        {
            for (var index = canvasTransform.childCount - 1; index >= 0; index--)
            {
                var child = canvasTransform.GetChild(index).gameObject;
                if (child.name == "Minigame Panel")
                {
                    child.SetActive(false);
                    Destroy(child);
                }
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var systemObject = new GameObject("Minigame Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(systemObject);
        }

        protected static Text CreateLabel(Transform parent, string text, Vector2 position, int size)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1000, 90);
            rect.anchoredPosition = position;
            var label = labelObject.GetComponent<Text>();
            label.raycastTarget = false;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = text;
            GameText.ApplyFont(label);
            return label;
        }
    }
}
