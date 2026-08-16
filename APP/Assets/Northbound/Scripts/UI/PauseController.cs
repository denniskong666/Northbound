using System;
using System.IO;
using Northbound.Core;
using Northbound.Narrative;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Linq;
using UnityEngine.EventSystems;

namespace Northbound.UI
{
    /// <summary>Keyboard/mouse-only pause and title state; the lease is idempotently released on every exit.</summary>
    public sealed class PauseController : MonoBehaviour
    {
        private InputGate inputGate;
        private SaveGameService saveGame;
        private IDisposable pauseLease;
        private IDisposable titleLease;
        private CanvasGroup canvasGroup;
        private GameObject pauseMenuRoot;
        private SettingsMenuController settingsMenu;
        private GameObject creditsRoot;
        private bool settingsReturnToPause;
        private bool escapeWasPressed;
        private Action newGameConfirmed;
        private Func<bool> continueConfirmed;
        private Action returnToTitleRequested;
        private Func<bool> saveAndQuitRequested;
        private AudioMixer mixer;
        private AudioMixerSnapshot normalSnapshot;
        private AudioMixerSnapshot pauseSnapshot;
        private bool hasCapturedMix;
        private float capturedMaster;
        private float capturedMusic;
        private float capturedSfx;
        private float capturedVoice;
        public bool IsPaused { get; private set; }
        public bool IsTitleVisible { get; private set; }
        public bool IsNewGameConfirmationVisible { get; private set; }

        public void Initialize(InputGate gate, SaveGameService saves)
        {
            Initialize(gate, saves, new SettingsModel(), null, null);
        }

        public void Initialize(InputGate gate, SaveGameService saves, SettingsModel settings, AudioMixer mixer, string settingsPath = null, Action onNewGameConfirmed = null, Func<bool> onContinueConfirmed = null, Action onReturnToTitleRequested = null, Func<bool> onSaveAndQuitRequested = null)
        {
            inputGate = gate ?? throw new ArgumentNullException(nameof(gate));
            saveGame = saves ?? throw new ArgumentNullException(nameof(saves));
            newGameConfirmed = onNewGameConfirmed;
            continueConfirmed = onContinueConfirmed;
            returnToTitleRequested = onReturnToTitleRequested;
            saveAndQuitRequested = onSaveAndQuitRequested;
            this.mixer = mixer;
            InputSystem.onEvent -= ProcessPauseInput;
            InputSystem.onEvent += ProcessPauseInput;
            GameText.LanguageChanged -= RefreshLocalizedLabels;
            GameText.LanguageChanged += RefreshLocalizedLabels;
            normalSnapshot = mixer != null ? mixer.FindSnapshot("Normal") : null;
            pauseSnapshot = mixer != null ? mixer.FindSnapshot("Pause") : null;
            EnsureUi();
            EnsureSaveAndQuitButtons();
            settingsMenu?.Initialize(settings ?? new SettingsModel(), mixer, settingsPath);
            WireTitleButtons();
            WirePanelButtons();
            ShowTitle();
            RefreshLocalizedLabels();
        }

        public void AttachPanels(GameObject pauseMenu, SettingsMenuController settingsController, GameObject creditsMenu)
        {
            if (settingsMenu != null)
            {
                settingsMenu.Applied -= RefreshPausedMix;
            }
            pauseMenuRoot = pauseMenu;
            settingsMenu = settingsController;
            creditsRoot = creditsMenu;
            if (settingsMenu != null)
            {
                settingsMenu.Applied += RefreshPausedMix;
            }
            NormalizeStandalonePanel(pauseMenuRoot);
            NormalizeStandalonePanel(settingsMenu != null ? settingsMenu.gameObject : null);
            NormalizeStandalonePanel(creditsRoot);
            WirePanelButtons();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (IsNewGameConfirmationVisible && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) ConfirmNewGame();
        }

        private void ProcessPauseInput(InputEventPtr eventPtr, InputDevice device)
        {
            if (device is not Keyboard keyboard ||
                (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()) ||
                !keyboard.escapeKey.ReadValueFromEvent(eventPtr, out var escapeValue)) return;

            var isPressed = escapeValue >= InputSystem.settings.defaultButtonPressPoint;
            if (isPressed && !escapeWasPressed)
            {
                HandleEscape();
            }
            escapeWasPressed = isPressed;
        }

        private void HandleEscape()
        {
            if (settingsMenu != null && settingsMenu.gameObject.activeSelf) ReturnFromSettings();
            else if (creditsRoot != null && creditsRoot.activeSelf) ShowTitle();
            else if (IsNewGameConfirmationVisible) CancelNewGame();
            else if (IsPaused) Resume();
            else if (!IsTitleVisible) Pause();
        }

        public void Pause()
        {
            if (IsPaused || IsTitleVisible || inputGate == null) return;
            pauseLease = inputGate.Acquire(this);
            ApplyPauseMix();
            Time.timeScale = 0f;
            IsPaused = true;
            SetActive(pauseMenuRoot, true);
            Select(pauseMenuRoot, "Resume");
        }

        public void Resume()
        {
            RestoreNormalMix();
            Time.timeScale = 1f;
            IsPaused = false;
            SetActive(pauseMenuRoot, false);
            ReleaseLease();
        }

        public void ShowTitle()
        {
            Resume();
            IsTitleVisible = true;
            IsNewGameConfirmationVisible = false;
            gameObject.SetActive(true);
            SetActive(pauseMenuRoot, false);
            SetActive(settingsMenu != null ? settingsMenu.gameObject : null, false);
            SetActive(creditsRoot, false);
            SetActive(ConfirmationRoot(), false);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            RefreshContinueButton();
            titleLease ??= inputGate.Acquire(this);
            Select(gameObject, "New Game");
        }

        public void HideTitle()
        {
            IsTitleVisible = false;
            IsNewGameConfirmationVisible = false;
            SetActive(ConfirmationRoot(), false);
            ReleaseTitleLease();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void RequestNewGame()
        {
            IsNewGameConfirmationVisible = true;
            SetActive(ConfirmationRoot(), true);
            Select(ConfirmationRoot(), "Confirm New Game");
        }

        public void CancelNewGame()
        {
            IsNewGameConfirmationVisible = false;
            SetActive(ConfirmationRoot(), false);
            Select(gameObject, "New Game");
        }

        public void ConfirmNewGame()
        {
            if (!IsNewGameConfirmationVisible) return;
            var confirm = FindButton(gameObject, "Confirm New Game");
            if (EventSystem.current != null && confirm != null && EventSystem.current.currentSelectedGameObject != confirm.gameObject) return;
            saveGame.Delete();
            newGameConfirmed?.Invoke();
            IsNewGameConfirmationVisible = false;
            HideTitle();
        }

        public bool Continue() => File.Exists(saveGame.SavePath);

        public void ShowSettings()
        {
            settingsReturnToPause = IsPaused;
            IsTitleVisible = false;
            SetCanvasVisible(false);
            SetActive(pauseMenuRoot, false);
            SetActive(settingsMenu != null ? settingsMenu.gameObject : null, true);
            Select(settingsMenu != null ? settingsMenu.gameObject : null, "Master Volume");
        }

        public void ShowCredits()
        {
            IsTitleVisible = false;
            SetCanvasVisible(false);
            SetActive(creditsRoot, true);
            Select(creditsRoot, "Return to Title");
        }

        public void ReturnFromSettings()
        {
            SetActive(settingsMenu != null ? settingsMenu.gameObject : null, false);
            if (settingsReturnToPause)
            {
                SetActive(pauseMenuRoot, true);
                Select(pauseMenuRoot, "Resume");
            }
            else
            {
                ShowTitle();
            }
        }

        private void OnDestroy()
        {
            InputSystem.onEvent -= ProcessPauseInput;
            GameText.LanguageChanged -= RefreshLocalizedLabels;
            if (settingsMenu != null)
            {
                settingsMenu.Applied -= RefreshPausedMix;
            }
            Resume();
            ReleaseTitleLease();
            DestroyPanel(pauseMenuRoot);
            DestroyPanel(settingsMenu != null ? settingsMenu.gameObject : null);
            DestroyPanel(creditsRoot);
        }

        private void RefreshLocalizedLabels()
        {
            RefreshLocalizedButtons(gameObject);
            RefreshLocalizedButtons(pauseMenuRoot);
            RefreshLocalizedButtons(creditsRoot);
            SetHeading(pauseMenuRoot, GameText.T("Paused", "已暂停"));
            SetHeading(creditsRoot, GameText.T("Northbound Credits", "Northbound 制作人员"));
            ApplyFonts(gameObject);
            ApplyFonts(pauseMenuRoot);
            ApplyFonts(creditsRoot);
        }

        private static void RefreshLocalizedButtons(GameObject root)
        {
            if (root == null) return;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                var label = button.GetComponentInChildren<Text>(true);
                if (label == null) continue;
                label.text = GameText.UiLabel(button.name);
                GameText.ApplyFont(label);
            }
        }

        private static void ApplyFonts(GameObject root)
        {
            if (root == null) return;
            foreach (var label in root.GetComponentsInChildren<Text>(true)) GameText.ApplyFont(label);
        }

        private static void SetHeading(GameObject root, string value)
        {
            if (root == null) return;
            var heading = root.GetComponentsInChildren<Text>(true).FirstOrDefault(label => label.gameObject.name == "Heading");
            if (heading != null) heading.text = value;
        }
        private void ReleaseLease() { pauseLease?.Dispose(); pauseLease = null; }
        private void ReleaseTitleLease() { titleLease?.Dispose(); titleLease = null; }

        private void EnsureUi()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 700;
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            if (GetComponent<CanvasScaler>() == null) gameObject.AddComponent<CanvasScaler>();
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
            if (GetComponentsInChildren<Button>(true).Length > 0) return;
            if (transform.Find("New Game") == null) CreateButton("New Game", RequestNewGame, 80f);
            if (transform.Find("Continue") == null) CreateButton("Continue", () => { if (Continue()) HideTitle(); }, 0f).interactable = Continue();
            if (transform.Find("Confirm New Game") == null) CreateButton("Confirm New Game", ConfirmNewGame, -80f);
            if (transform.Find("Resume") == null) CreateButton("Resume", Resume, -160f);
            if (transform.Find("Return to Title") == null) CreateButton("Return to Title", RequestReturnToTitle, -240f);
        }

        private void WireTitleButtons()
        {
            WireButton(gameObject, "New Game", RequestNewGame);
            WireButton(gameObject, "Continue", () =>
            {
                if (continueConfirmed != null ? continueConfirmed() : Continue()) HideTitle();
            });
            WireButton(gameObject, "Settings", ShowSettings);
            WireButton(gameObject, "Credits", ShowCredits);
            WireButton(gameObject, "Save and Quit", RequestSaveAndQuit);
            WireButton(gameObject, "Confirm New Game", ConfirmNewGame);
            WireButton(gameObject, "Cancel New Game", CancelNewGame);
            var continueButton = FindButton(gameObject, "Continue");
            if (continueButton != null) continueButton.interactable = Continue();
        }

        private void WirePanelButtons()
        {
            WireButton(pauseMenuRoot, "Resume", Resume);
            WireButton(pauseMenuRoot, "Settings", ShowSettings);
            WireButton(pauseMenuRoot, "Return to Title", RequestReturnToTitle);
            WireButton(pauseMenuRoot, "Save and Quit", RequestSaveAndQuit);
            if (settingsMenu != null) WireButton(settingsMenu.gameObject, "Back", ReturnFromSettings);
            WireButton(creditsRoot, "Return to Title", RequestReturnToTitle);
        }

        private void RequestReturnToTitle()
        {
            if (returnToTitleRequested != null) returnToTitleRequested();
            else ShowTitle();
        }

        private void RequestSaveAndQuit()
        {
            saveAndQuitRequested?.Invoke();
        }

        private void EnsureSaveAndQuitButtons()
        {
            CloneButtonBelow(gameObject, "Credits", "Save and Quit", 75f);
            CloneButtonBelow(pauseMenuRoot, "Return to Title", "Save and Quit", 80f);
        }

        private static void CloneButtonBelow(GameObject root, string templateName, string buttonName, float spacing)
        {
            if (root == null || FindButton(root, buttonName) != null) return;
            var template = FindButton(root, templateName);
            if (template == null) return;

            var clone = Instantiate(template.gameObject, template.transform.parent);
            clone.name = buttonName;
            clone.transform.SetAsLastSibling();
            var rect = clone.GetComponent<RectTransform>();
            rect.anchoredPosition = template.GetComponent<RectTransform>().anchoredPosition + Vector2.down * spacing;
            var label = clone.GetComponentInChildren<Text>(true);
            if (label != null) label.text = GameText.UiLabel(buttonName);
        }

        private static void WireButton(GameObject root, string buttonName, UnityEngine.Events.UnityAction action)
        {
            var button = FindButton(root, buttonName);
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static Button FindButton(GameObject root, string buttonName) => root == null
            ? null
            : root.GetComponentsInChildren<Button>(true).FirstOrDefault(button => button.name == buttonName);

        private GameObject ConfirmationRoot()
        {
            var confirmation = transform.Find("New Game Confirmation");
            return confirmation != null ? confirmation.gameObject : null;
        }

        private void RefreshContinueButton()
        {
            var continueButton = FindButton(gameObject, "Continue");
            if (continueButton != null) continueButton.interactable = Continue();
        }

        private void SetCanvasVisible(bool visible)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null) target.SetActive(active);
        }

        private static void DestroyPanel(GameObject panel)
        {
            if (panel == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(panel);
            else UnityEngine.Object.DestroyImmediate(panel);
        }

        private static void NormalizeStandalonePanel(GameObject panel)
        {
            if (panel == null) return;
            var rect = panel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            var canvas = panel.GetComponent<Canvas>() ?? panel.AddComponent<Canvas>();
            canvas.enabled = true;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 700;
            var scaler = panel.GetComponent<CanvasScaler>() ?? panel.AddComponent<CanvasScaler>();
            scaler.enabled = true;
            var raycaster = panel.GetComponent<GraphicRaycaster>() ?? panel.AddComponent<GraphicRaycaster>();
            raycaster.enabled = true;
        }

        private static void Select(GameObject root, string controlName)
        {
            if (root == null || EventSystem.current == null) return;
            var selectable = root.GetComponentsInChildren<Selectable>(true)
                .FirstOrDefault(control => control.name == controlName);
            EventSystem.current.SetSelectedGameObject(selectable != null ? selectable.gameObject : null);
        }

        private void ApplyPauseMix()
        {
            pauseSnapshot?.TransitionTo(0f);
            if (mixer == null || hasCapturedMix) return;
            capturedMaster = MixerValue("MasterVolume");
            capturedMusic = MixerValue("MusicVolume");
            capturedSfx = MixerValue("SFXVolume");
            capturedVoice = MixerValue("VoiceVolume");
            hasCapturedMix = true;
            ApplyPausedEffectiveMix();
        }

        private void RefreshPausedMix()
        {
            if (!IsPaused || mixer == null) return;
            capturedMaster = MixerValue("MasterVolume");
            capturedMusic = MixerValue("MusicVolume");
            capturedSfx = MixerValue("SFXVolume");
            capturedVoice = MixerValue("VoiceVolume");
            hasCapturedMix = true;
            ApplyPausedEffectiveMix();
        }

        private void ApplyPausedEffectiveMix()
        {
            mixer.SetFloat("MasterVolume", capturedMaster);
            mixer.SetFloat("MusicVolume", Mathf.Max(-80f, capturedMusic - 12f));
            mixer.SetFloat("SFXVolume", Mathf.Max(-80f, capturedSfx - 18f));
            mixer.SetFloat("VoiceVolume", capturedVoice);
        }

        private void RestoreNormalMix()
        {
            if (mixer == null || !hasCapturedMix) return;
            normalSnapshot?.TransitionTo(0f);
            mixer.SetFloat("MasterVolume", capturedMaster);
            mixer.SetFloat("MusicVolume", capturedMusic);
            mixer.SetFloat("SFXVolume", capturedSfx);
            mixer.SetFloat("VoiceVolume", capturedVoice);
            hasCapturedMix = false;
        }

        private float MixerValue(string parameter)
        {
            return mixer.GetFloat(parameter, out var value) ? value : 0f;
        }

        private Button CreateButton(string title, UnityEngine.Events.UnityAction action, float y)
        {
            var root = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(transform, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(360f, 60f);
            rect.anchoredPosition = new Vector2(0f, y);
            var button = root.GetComponent<Button>();
            button.onClick.AddListener(action);
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(root.transform, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.alignment = TextAnchor.MiddleCenter;
            label.text = title;
            return button;
        }
    }
}
