using System.Collections.Generic;
using Northbound.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Northbound.UI;

namespace Northbound.Dialogue
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DialogueView : MonoBehaviour, IPointerClickHandler
    {
        private const float DefaultSecondsPerCharacter = .028f;
        private static readonly Color PanelColor = new Color(.059f, .051f, .071f, .94f);
        private static readonly Color GoldColor = new Color(.82f, .67f, .38f, 1f);
        private static readonly Color MutedGoldColor = new Color(.76f, .72f, .62f, 1f);

        [SerializeField] private Text speakerLabel;
        [SerializeField] private Text dialogueLabel;
        [SerializeField] private Image portraitImage;
        [SerializeField] private GameObject continueIndicator;
        [SerializeField] private Button[] choiceButtons = new Button[DialogueRunner.MaximumChoices];
        [SerializeField] private Text[] choiceLabels = new Text[DialogueRunner.MaximumChoices];
        [SerializeField] private AudioSource reactionAudio;
        [SerializeField] private Image subtitleBackground;
        [SerializeField, Min(.001f)] private float secondsPerCharacter = DefaultSecondsPerCharacter;
        [SerializeField] private Color selectedChoiceColor = new Color(.82f, .67f, .38f, 1f);
        [SerializeField] private Color unselectedChoiceColor = new Color(.76f, .72f, .62f, 1f);

        private CanvasGroup canvasGroup;
        private DialogueRunner runner;
        private DialogueLine displayedLine;
        private string fullLineText = string.Empty;
        private float characterElapsed;
        private float fadeElapsed;
        private float portraitElapsed;
        private int visibleCharacterCount;
        private int selectedChoiceIndex;
        private bool wasVisible;
        private RectTransform panelRect;
        private readonly HashSet<Key> pressedKeys = new HashSet<Key>();

        public bool IsTyping => visibleCharacterCount < fullLineText.Length;
        public int SelectedChoiceIndex => selectedChoiceIndex;
        public bool IsShowingNarration { get; private set; }

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            GameText.LanguageChanged += Render;
            ApplyWebDialogueLayout();
            EnsureSubtitleBackground();
            EnsureEventSystem();
            for (var index = 0; index < choiceButtons.Length; index++)
            {
                var choiceIndex = index;
                if (choiceButtons[index] != null)
                {
                    choiceButtons[index].onClick.AddListener(() => Choose(choiceIndex));
                    AddHoverHandler(choiceButtons[index], choiceIndex);
                }
            }

            Render();
        }

        private void Start()
        {
            if (runner == null && GameBootstrap.Instance != null)
            {
                Bind(GameBootstrap.Instance.Dialogue);
            }
        }

        private void OnEnable()
        {
            InputSystem.onEvent -= ProcessKeyboardInput;
            InputSystem.onEvent += ProcessKeyboardInput;
        }

        private void OnDisable()
        {
            InputSystem.onEvent -= ProcessKeyboardInput;
            pressedKeys.Clear();
        }

        private void OnDestroy()
        {
            InputSystem.onEvent -= ProcessKeyboardInput;
            GameText.LanguageChanged -= Render;
            if (runner != null)
            {
                runner.Changed -= Render;
            }
        }

        private void Update()
        {
            if (runner != null && runner.IsRunning)
            {
                TickPresentation(Time.unscaledDeltaTime);
            }
        }

        private void ProcessKeyboardInput(InputEventPtr eventPtr, InputDevice device)
        {
            if (device is not Keyboard keyboard ||
                (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()))
            {
                return;
            }

            ProcessKey(eventPtr, keyboard, Key.Enter);
            ProcessKey(eventPtr, keyboard, Key.Space);
            ProcessKey(eventPtr, keyboard, Key.UpArrow);
            ProcessKey(eventPtr, keyboard, Key.W);
            ProcessKey(eventPtr, keyboard, Key.DownArrow);
            ProcessKey(eventPtr, keyboard, Key.S);
            ProcessKey(eventPtr, keyboard, Key.Digit1);
            ProcessKey(eventPtr, keyboard, Key.Digit2);
            ProcessKey(eventPtr, keyboard, Key.Digit3);
            ProcessKey(eventPtr, keyboard, Key.Digit4);
        }

        private void ProcessKey(InputEventPtr eventPtr, Keyboard keyboard, Key key)
        {
            var control = keyboard[key];
            if (!control.ReadValueFromEvent(eventPtr, out var value))
            {
                return;
            }

            var isPressed = value >= InputSystem.settings.defaultButtonPressPoint;
            if (!isPressed)
            {
                pressedKeys.Remove(key);
                return;
            }

            if (pressedKeys.Add(key))
            {
                HandleKeyPress(key);
            }
        }

        private void HandleKeyPress(Key key)
        {
            if (runner == null || !runner.IsRunning)
            {
                return;
            }

            var confirm = key is Key.Enter or Key.Space;
            if (IsTyping)
            {
                if (confirm)
                {
                    RevealCurrentLine();
                }

                return;
            }

            if (HasChoices())
            {
                if (key is Key.UpArrow or Key.W)
                {
                    MoveChoice(-1);
                }
                else if (key is Key.DownArrow or Key.S)
                {
                    MoveChoice(1);
                }
                else if (key == Key.Digit1)
                {
                    Choose(0);
                }
                else if (key == Key.Digit2)
                {
                    Choose(1);
                }
                else if (key == Key.Digit3)
                {
                    Choose(2);
                }
                else if (key == Key.Digit4)
                {
                    Choose(3);
                }
                else if (confirm)
                {
                    Choose(selectedChoiceIndex);
                }

                return;
            }

            if (confirm)
            {
                runner.Advance();
            }
        }

        public void Bind(DialogueRunner value)
        {
            if (runner != null)
            {
                runner.Changed -= Render;
            }

            runner = value;
            if (runner != null)
            {
                runner.Changed += Render;
            }

            Render();
        }

        public void StartDialogue(DialogueAsset asset)
        {
            runner?.Start(asset);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (runner == null || !runner.IsRunning || HasVisibleChoices())
            {
                return;
            }

            if (IsTyping)
            {
                RevealCurrentLine();
            }
            else
            {
                runner.Advance();
            }
        }

        public void RevealCurrentLine()
        {
            if (dialogueLabel == null || string.IsNullOrEmpty(fullLineText))
            {
                return;
            }

            visibleCharacterCount = fullLineText.Length;
            dialogueLabel.text = fullLineText;
            RefreshPromptsAndChoices();
        }

        private void Choose(int index)
        {
            if (runner == null || IsTyping || !HasChoices())
            {
                return;
            }

            runner.Choose(index);
        }

        private void Render()
        {
            var line = runner != null && runner.IsRunning ? runner.Current : null;
            var isVisible = line != null;
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;

            if (!isVisible)
            {
                wasVisible = false;
                IsShowingNarration = false;
                displayedLine = null;
                fullLineText = string.Empty;
                visibleCharacterCount = 0;
                return;
            }

            if (!wasVisible)
            {
                fadeElapsed = 0f;
                canvasGroup.alpha = ReducedMotion ? 1f : 0f;
            }
            wasVisible = true;

            IsShowingNarration = DialoguePresentationCatalog.IsNarration(
                runner.ActiveDialogueId,
                runner.CurrentLineIndex,
                line);
            if (speakerLabel != null)
            {
                speakerLabel.gameObject.SetActive(!IsShowingNarration);
                speakerLabel.text = GameText.IsChinese
                    ? GameText.CharacterName(line.speakerId ?? string.Empty)
                    : (line.speakerId ?? string.Empty).ToUpperInvariant();
                GameText.ApplyFont(speakerLabel);
            }

            var localizedLine = DialogueChineseCatalog.Resolve(
                runner.ActiveDialogueId,
                runner.CurrentLineIndex,
                line.text,
                line.textChinese);
            var lineChanged = !ReferenceEquals(displayedLine, line);
            if (lineChanged || !string.Equals(fullLineText, localizedLine, System.StringComparison.Ordinal))
            {
                displayedLine = line;
                fullLineText = localizedLine;
                visibleCharacterCount = 0;
                characterElapsed = 0f;
                portraitElapsed = 0f;
                selectedChoiceIndex = 0;
                if (dialogueLabel != null)
                {
                    dialogueLabel.text = string.Empty;
                }
            }

            if (dialogueLabel != null)
            {
                SubtitleView.Apply(dialogueLabel, subtitleBackground, GameBootstrap.Instance != null ? GameBootstrap.Instance.Settings : null);
                if (IsShowingNarration && subtitleBackground != null)
                {
                    subtitleBackground.enabled = false;
                }
            }
            if (portraitImage != null)
            {
                portraitImage.sprite = line.portrait;
                portraitImage.enabled = !IsShowingNarration && line.portrait != null;
            }
            ApplyPresentationLayout(IsShowingNarration, HasChoices(), line.portrait != null);

            RefreshPromptsAndChoices();
            if (lineChanged && line.reactionClip != null && reactionAudio != null)
            {
                reactionAudio.PlayOneShot(line.reactionClip);
            }
        }

        private bool HasChoices()
        {
            return runner != null && runner.Current != null && runner.Current.choices != null && runner.Current.choices.Count > 0;
        }

        private bool HasVisibleChoices() => HasChoices() && !IsTyping;

        private bool ReducedMotion => GameBootstrap.Instance != null && GameBootstrap.Instance.Settings != null &&
            GameBootstrap.Instance.Settings.ReducedMotion;

        private void TickPresentation(float deltaTime)
        {
            if (canvasGroup != null && canvasGroup.alpha < 1f)
            {
                fadeElapsed += deltaTime;
                canvasGroup.alpha = ReducedMotion ? 1f : Mathf.Clamp01(fadeElapsed / .2f);
            }

            if (IsTyping && dialogueLabel != null)
            {
                characterElapsed += deltaTime;
                var nextCount = Mathf.Min(fullLineText.Length,
                    Mathf.FloorToInt(characterElapsed / Mathf.Max(.001f, secondsPerCharacter)));
                if (nextCount != visibleCharacterCount)
                {
                    visibleCharacterCount = nextCount;
                    dialogueLabel.text = fullLineText.Substring(0, visibleCharacterCount);
                    if (!IsTyping)
                    {
                        RefreshPromptsAndChoices();
                    }
                }
            }

            if (portraitImage != null && portraitImage.enabled)
            {
                portraitElapsed += deltaTime;
                if (ReducedMotion)
                {
                    portraitImage.rectTransform.localScale = Vector3.one;
                }
                else if (portraitElapsed < .22f)
                {
                    var progress = 1f - Mathf.Pow(1f - Mathf.Clamp01(portraitElapsed / .22f), 3f);
                    portraitImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(.85f, 1f, progress);
                }
                else
                {
                    var breath = (Mathf.Sin((portraitElapsed - .22f) * Mathf.PI * 2f / 1.5f) + 1f) * .5f;
                    portraitImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.035f, breath);
                }
            }
        }

        private void RefreshPromptsAndChoices()
        {
            var showChoices = HasVisibleChoices();
            if (continueIndicator != null)
            {
                continueIndicator.SetActive(!IsTyping && !showChoices);
                var continueText = continueIndicator.GetComponent<Text>() ?? continueIndicator.GetComponentInChildren<Text>(true);
                if (continueText != null)
                {
                    continueText.text = GameText.T("ENTER / SPACE  CONTINUE", "回车 / 空格  继续");
                    continueText.color = MutedGoldColor;
                    GameText.ApplyFont(continueText);
                }
            }

            var line = runner?.Current;
            var count = line?.choices?.Count ?? 0;
            if (count > 0)
            {
                selectedChoiceIndex = Mathf.Clamp(selectedChoiceIndex, 0, count - 1);
            }

            for (var index = 0; index < choiceButtons.Length; index++)
            {
                var active = showChoices && index < count && line.choices[index] != null;
                if (choiceButtons[index] != null)
                {
                    choiceButtons[index].gameObject.SetActive(active);
                }

                if (!active || index >= choiceLabels.Length || choiceLabels[index] == null)
                {
                    continue;
                }

                var choice = line.choices[index];
                var selected = index == selectedChoiceIndex;
                choiceLabels[index].text = $"{(selected ? "\u25B6" : " ")}  {Localized(choice.text, choice.textChinese)}";
                choiceLabels[index].color = selected ? selectedChoiceColor : unselectedChoiceColor;
                choiceLabels[index].fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
                GameText.ApplyFont(choiceLabels[index]);
            }
        }

        private void MoveChoice(int delta)
        {
            var count = runner?.Current?.choices?.Count ?? 0;
            if (count == 0)
            {
                return;
            }

            selectedChoiceIndex = (selectedChoiceIndex + delta + count) % count;
            RefreshPromptsAndChoices();
        }

        private void SelectChoice(int index)
        {
            if (!HasVisibleChoices() || index < 0 || index >= (runner?.Current?.choices?.Count ?? 0))
            {
                return;
            }

            selectedChoiceIndex = index;
            RefreshPromptsAndChoices();
        }

        private static string Localized(string english, string chinese)
        {
            if (GameText.IsChinese && !string.IsNullOrWhiteSpace(chinese))
            {
                return chinese;
            }

            return english ?? string.Empty;
        }

        private void ApplyWebDialogueLayout()
        {
            // Give the root a deterministic reference-sized rect. This keeps the
            // web layout stable in both ScreenSpaceOverlay and test/world-space
            // canvases; CanvasScaler still maps it to the actual display.
            var root = GetComponent<RectTransform>();
            if (root != null)
            {
                root.anchorMin = root.anchorMax = Vector2.zero;
                root.pivot = Vector2.zero;
                var scaler = GetComponent<CanvasScaler>();
                if (root.sizeDelta.sqrMagnitude < 1f && scaler != null)
                {
                    root.sizeDelta = scaler.referenceResolution;
                }
            }

            panelRect = transform.Find("Panel") as RectTransform;
            if (panelRect == null)
            {
                return;
            }

            ConfigurePanel(false, false);
            var panelImage = panelRect.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = PanelColor;
                panelImage.type = Image.Type.Sliced;
                var outline = panelRect.GetComponent<Outline>() ?? panelRect.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(GoldColor.r, GoldColor.g, GoldColor.b, .72f);
                outline.effectDistance = new Vector2(2f, -2f);
                outline.useGraphicAlpha = true;
            }

            if (portraitImage != null)
            {
                var portrait = portraitImage.rectTransform;
                portrait.anchorMin = portrait.anchorMax = new Vector2(0f, 1f);
                portrait.pivot = new Vector2(0f, 1f);
                portrait.anchoredPosition = new Vector2(40f, -72f);
                portrait.sizeDelta = new Vector2(144f, 240f);
            }

            if (speakerLabel != null)
            {
                speakerLabel.fontSize = 28;
                speakerLabel.fontStyle = FontStyle.Bold;
                speakerLabel.color = GoldColor;
                speakerLabel.alignment = TextAnchor.UpperLeft;
            }

            if (dialogueLabel != null)
            {
                dialogueLabel.color = new Color(.94f, .91f, .84f, 1f);
                dialogueLabel.alignment = TextAnchor.UpperLeft;
                dialogueLabel.lineSpacing = 1.08f;
                dialogueLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                dialogueLabel.verticalOverflow = VerticalWrapMode.Truncate;
            }

            for (var index = 0; index < choiceButtons.Length; index++)
            {
                var button = choiceButtons[index];
                if (button == null)
                {
                    continue;
                }

                button.transition = Selectable.Transition.None;
                var buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = Color.clear;
                }
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(0f, -150f - index * 52f);
                rect.sizeDelta = new Vector2(0f, 48f);

                if (index < choiceLabels.Length && choiceLabels[index] != null)
                {
                    var label = choiceLabels[index];
                    label.fontSize = 28;
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 18;
                    label.resizeTextMaxSize = 28;
                    label.alignment = TextAnchor.MiddleLeft;
                    label.horizontalOverflow = HorizontalWrapMode.Wrap;
                    label.verticalOverflow = VerticalWrapMode.Truncate;
                    var labelRect = label.rectTransform;
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.anchoredPosition = Vector2.zero;
                    labelRect.sizeDelta = new Vector2(-8f, 0f);
                }
            }

            if (continueIndicator != null)
            {
                var rect = continueIndicator.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(1f, 0f);
                    rect.anchoredPosition = new Vector2(-32f, 24f);
                    rect.sizeDelta = new Vector2(360f, 32f);
                }
            }

            // The view starts hidden and may not have a bound line yet. Keep its
            // dormant geometry portrait-safe so editor/world-space layout passes
            // and the first rendered frame cannot flash overlapping controls.
            ConfigureContentBounds(true, false);
        }

        private void ApplyPresentationLayout(bool narration, bool hasChoices, bool hasPortrait)
        {
            ConfigurePanel(narration, hasChoices);
            if (narration)
            {
                ConfigureNarrationBounds(hasChoices);
                return;
            }

            if (dialogueLabel != null)
            {
                dialogueLabel.alignment = TextAnchor.UpperLeft;
            }
            ConfigureContentBounds(hasPortrait, hasChoices);
        }

        private void ConfigurePanel(bool narration, bool hasChoices)
        {
            if (panelRect == null)
            {
                panelRect = transform.Find("Panel") as RectTransform;
            }
            if (panelRect == null)
            {
                return;
            }

            var compactNarration = narration && !hasChoices;
            panelRect.anchorMin = new Vector2(compactNarration ? .16f : .041667f, 0f);
            panelRect.anchorMax = new Vector2(compactNarration ? .84f : .958333f, 0f);
            panelRect.pivot = new Vector2(.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, compactNarration ? 62f : 40f);
            panelRect.sizeDelta = new Vector2(0f, compactNarration ? 176f : 400f);
        }

        private void ConfigureNarrationBounds(bool hasChoices)
        {
            const float left = 48f;
            const float right = 48f;
            if (dialogueLabel != null)
            {
                var rect = dialogueLabel.rectTransform;
                StretchHorizontally(rect, left, right);
                rect.anchorMin = new Vector2(rect.anchorMin.x, 1f);
                rect.anchorMax = new Vector2(rect.anchorMax.x, 1f);
                rect.pivot = new Vector2(.5f, 1f);
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, hasChoices ? -42f : -28f);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, hasChoices ? 72f : 104f);
                dialogueLabel.alignment = hasChoices ? TextAnchor.UpperLeft : TextAnchor.MiddleCenter;
            }

            for (var index = 0; index < choiceButtons.Length; index++)
            {
                var rect = choiceButtons[index] != null ? choiceButtons[index].GetComponent<RectTransform>() : null;
                if (rect == null)
                {
                    continue;
                }
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(left, -138f - index * 52f);
                rect.sizeDelta = new Vector2(-(left + right), 48f);
            }
        }

        private void ConfigureContentBounds(bool hasPortrait, bool hasChoices)
        {
            var contentLeft = hasPortrait ? 216f : 40f;
            const float right = 40f;
            StretchHorizontally(speakerLabel?.rectTransform, contentLeft, right);
            StretchHorizontally(dialogueLabel?.rectTransform, contentLeft, right);
            if (speakerLabel != null)
            {
                var rect = speakerLabel.rectTransform;
                rect.anchorMin = new Vector2(rect.anchorMin.x, 1f);
                rect.anchorMax = new Vector2(rect.anchorMax.x, 1f);
                rect.pivot = new Vector2(.5f, 1f);
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -28f);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 38f);
            }
            if (dialogueLabel != null)
            {
                var rect = dialogueLabel.rectTransform;
                rect.anchorMin = new Vector2(rect.anchorMin.x, 1f);
                rect.anchorMax = new Vector2(rect.anchorMax.x, 1f);
                rect.pivot = new Vector2(.5f, 1f);
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -76f);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, hasChoices ? 68f : 230f);
                if (subtitleBackground != null)
                {
                    var backgroundRect = subtitleBackground.rectTransform;
                    backgroundRect.anchorMin = rect.anchorMin;
                    backgroundRect.anchorMax = rect.anchorMax;
                    backgroundRect.pivot = rect.pivot;
                    backgroundRect.anchoredPosition = rect.anchoredPosition;
                    backgroundRect.sizeDelta = rect.sizeDelta;
                }
            }

            for (var index = 0; index < choiceButtons.Length; index++)
            {
                var rect = choiceButtons[index] != null ? choiceButtons[index].GetComponent<RectTransform>() : null;
                if (rect == null)
                {
                    continue;
                }
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(contentLeft, -150f - index * 52f);
                rect.sizeDelta = new Vector2(-contentLeft - right, 48f);
                rect.anchorMax = new Vector2(1f, 1f);
            }
        }

        private static void StretchHorizontally(RectTransform rect, float left, float right)
        {
            if (rect == null)
            {
                return;
            }
            rect.anchorMin = new Vector2(0f, rect.anchorMin.y);
            rect.anchorMax = new Vector2(1f, rect.anchorMax.y);
            rect.anchoredPosition = new Vector2((left - right) * .5f, rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(-(left + right), rect.sizeDelta.y);
        }

        private void AddHoverHandler(Button button, int index)
        {
            var trigger = button.GetComponent<EventTrigger>() ?? button.gameObject.AddComponent<EventTrigger>();
            trigger.triggers ??= new System.Collections.Generic.List<EventTrigger.Entry>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener(_ => SelectChoice(index));
            trigger.triggers.Add(entry);
        }

        private void EnsureSubtitleBackground()
        {
            if (subtitleBackground != null || dialogueLabel == null)
            {
                return;
            }

            var labelRect = dialogueLabel.rectTransform;
            var backgroundObject = new GameObject("Subtitle Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(labelRect.parent, false);
            var backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = labelRect.anchorMin;
            backgroundRect.anchorMax = labelRect.anchorMax;
            backgroundRect.anchoredPosition = labelRect.anchoredPosition;
            backgroundRect.sizeDelta = labelRect.sizeDelta;
            backgroundRect.pivot = labelRect.pivot;
            backgroundObject.transform.SetSiblingIndex(labelRect.GetSiblingIndex());
            subtitleBackground = backgroundObject.GetComponent<Image>();
            subtitleBackground.color = new Color(PanelColor.r, PanelColor.g, PanelColor.b, .75f);
            subtitleBackground.raycastTarget = false;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("Dialogue Event System");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
            DontDestroyOnLoad(eventSystem);
        }
    }
}
