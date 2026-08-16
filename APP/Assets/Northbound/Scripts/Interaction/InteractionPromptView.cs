using UnityEngine;
using UnityEngine.UI;
using Northbound.UI;

namespace Northbound.Interaction
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private Text promptLabel;

        private CanvasGroup canvasGroup;
        private string rawPrompt = string.Empty;

        public bool IsVisible { get; private set; }

        public string CurrentPrompt { get; private set; } = string.Empty;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            GameText.LanguageChanged += RefreshLanguage;
            SetPrompt(null);
        }

        private void OnDestroy() => GameText.LanguageChanged -= RefreshLanguage;

        public void SetPrompt(string prompt)
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            rawPrompt = prompt ?? string.Empty;
            CurrentPrompt = GameText.Prompt(rawPrompt);
            IsVisible = !string.IsNullOrWhiteSpace(CurrentPrompt);
            canvasGroup.alpha = IsVisible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (promptLabel != null)
            {
                promptLabel.text = CurrentPrompt;
                GameText.ApplyFont(promptLabel);
            }
        }

        private void RefreshLanguage() => SetPrompt(rawPrompt);

        public void SetPromptLabel(Text value)
        {
            promptLabel = value;
            promptLabel.text = CurrentPrompt;
            GameText.ApplyFont(promptLabel);
        }

        private void OnGUI()
        {
            if (promptLabel == null && IsVisible)
            {
                GUI.Label(new Rect(16f, 16f, 360f, 28f), $"[E] {CurrentPrompt}");
            }
        }
    }
}
