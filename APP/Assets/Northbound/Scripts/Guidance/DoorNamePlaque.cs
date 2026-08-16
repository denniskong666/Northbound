using Northbound.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Northbound.Guidance
{
    public sealed class DoorNamePlaque : MonoBehaviour
    {
        private static readonly Color RestingSurface = new Color(.055f, .065f, .075f, .84f);
        private static readonly Color HighlightSurface = new Color(.23f, .17f, .07f, .96f);
        private static readonly Color RestingText = new Color(.86f, .88f, .89f);
        private static readonly Color HighlightText = new Color(1f, .84f, .43f);
        private string locationId;
        private string englishDisplayName;
        private Image surface;
        private Image accent;
        private Text label;
        private Canvas worldCanvas;

        public string LocationId => locationId;
        public string EnglishDisplayName => englishDisplayName;
        public string LabelText => label != null ? label.text : string.Empty;
        public bool IsHighlighted { get; private set; }

        public static DoorNamePlaque Create(Transform door, string id, string displayName)
        {
            if (door == null) return null;
            var root = new GameObject("Door Name Plaque", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(door, false);
            root.transform.localPosition = new Vector3(0f, 1.48f, -.35f);
            root.transform.localScale = Vector3.one * .008f;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(248f, 54f);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 62;
            var plaque = root.AddComponent<DoorNamePlaque>();
            plaque.worldCanvas = canvas;
            plaque.Configure(id, displayName);
            return plaque;
        }

        public void Configure(string id, string displayName)
        {
            locationId = id ?? string.Empty;
            englishDisplayName = displayName ?? string.Empty;
            if (surface == null) Build();
            RefreshLanguage();
            SetHighlighted(IsHighlighted);
        }

        public void SetHighlighted(bool highlighted)
        {
            IsHighlighted = highlighted;
            if (surface != null) surface.color = highlighted ? HighlightSurface : RestingSurface;
            if (accent != null) accent.gameObject.SetActive(highlighted);
            if (label != null)
            {
                label.color = highlighted ? HighlightText : RestingText;
                label.fontStyle = highlighted ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        public void SetPresentationVisible(bool visible)
        {
            worldCanvas ??= GetComponent<Canvas>();
            if (worldCanvas != null) worldCanvas.enabled = visible;
        }

        private void OnEnable()
        {
            GameText.LanguageChanged -= RefreshLanguage;
            GameText.LanguageChanged += RefreshLanguage;
        }

        private void OnDestroy() => GameText.LanguageChanged -= RefreshLanguage;

        private void RefreshLanguage()
        {
            if (label == null) return;
            label.text = GameText.Location(englishDisplayName);
            GameText.ApplyFont(label);
        }

        private void Build()
        {
            surface = gameObject.AddComponent<Image>();
            surface.raycastTarget = false;

            label = new GameObject("Location Name", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(transform, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(10f, 5f);
            label.rectTransform.offsetMax = new Vector2(-10f, -5f);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 25;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = 25;
            label.raycastTarget = false;

            accent = new GameObject("Current Destination Accent", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            accent.transform.SetParent(transform, false);
            var accentRect = accent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(1f, 0f);
            accentRect.pivot = new Vector2(.5f, 0f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0f, 4f);
            accent.color = HighlightText;
            accent.raycastTarget = false;
        }
    }
}
