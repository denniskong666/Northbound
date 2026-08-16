using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Northbound.Dialogue;
using Northbound.Narrative;
using Northbound.Minigames;
using Northbound.UI;
using Northbound.Cinematics;
using Northbound.Endings;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Audio;
using System.Collections;

namespace Northbound.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameObject dialogueViewPrefab;
        [SerializeField] private DinerShiftGame dinerShiftPrefab;
        [SerializeField] private WiringGame wiringGamePrefab;
        [SerializeField] private TrunkPackingGame trunkPackingPrefab;
        [SerializeField] private GameObject cinematicCanvasPrefab;
        [SerializeField] private CinematicCatalog cinematicCatalog;
        [SerializeField] private GameObject titleMenuPrefab;
        [SerializeField] private GameObject pauseMenuPrefab;
        [SerializeField] private GameObject settingsMenuPrefab;
        [SerializeField] private GameObject creditsPrefab;
        [SerializeField] private AudioMixer audioMixer;
        private IDisposable sessionTransitionLease;
        private bool sessionRestarting;

        public static GameBootstrap Instance { get; private set; }

        /// <summary>Optional session-level save source for demos and isolated runtime sessions.</summary>
        public static Func<SaveGameService> SessionSaveGameFactory { get; set; }
        public static string SessionSettingsPath { get; set; }
        public static Action SessionQuitAction { get; set; }

        public NarrativeStateStore NarrativeState { get; private set; }

        public SaveGameService SaveGame { get; private set; }

        public InputGate InputGate { get; private set; }

        public DialogueRunner Dialogue { get; private set; }

        public DialogueView DialogueView { get; private set; }

        public MinigameService Minigames { get; private set; }

        public CinematicPlayer Cinematics { get; private set; }

        public SettingsModel Settings { get; private set; }

        public EndingPresentationController Endings { get; private set; }
        public PauseController Menus { get; private set; }
        public InteractionFeedbackService Feedback { get; private set; }

        public CinematicCatalog CinematicCatalog => cinematicCatalog;
        public bool IsSessionActive { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SaveGame = SessionSaveGameFactory?.Invoke() ?? new SaveGameService();
            NarrativeState = new NarrativeStateStore(SaveGame.LoadOrNew());
            InputGate = GetComponent<InputGate>() ?? gameObject.AddComponent<InputGate>();
            Settings = SettingsModel.Load(SessionSettingsPath);
            Dialogue = new DialogueRunner(NarrativeState, InputGate);
            Minigames = CreateMinigameService();
            DialogueView = CreateDialogueView();
            Cinematics = CreateCinematicService();
            Endings = CreateEndingPresentation();
            var sfxGroups = audioMixer != null ? audioMixer.FindMatchingGroups("Master/SFX") : Array.Empty<AudioMixerGroup>();
            Feedback = InteractionFeedbackService.Create(sfxGroups.Length > 0 ? sfxGroups[0] : null);
            Menus = CreateMenus();
            SceneManager.LoadScene(SceneIds.Greybridge, LoadSceneMode.Single);
        }

        private void OnDestroy()
        {
            sessionTransitionLease?.Dispose();
            sessionTransitionLease = null;
            Dialogue?.Stop();

            if (Minigames != null)
            {
                Destroy(Minigames.gameObject);
            }

            if (DialogueView != null)
            {
                Destroy(DialogueView.gameObject);
            }

            if (Cinematics != null)
            {
                Destroy(Cinematics.gameObject);
            }

            if (Endings != null)
            {
                Destroy(Endings.gameObject);
            }

            if (Menus != null)
            {
                Destroy(Menus.gameObject);
            }
            if (Feedback != null) Destroy(Feedback.gameObject);

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private MinigameService CreateMinigameService()
        {
            if (dinerShiftPrefab == null || wiringGamePrefab == null || trunkPackingPrefab == null)
            {
                dinerShiftPrefab ??= new GameObject("DinerShift Fallback").AddComponent<DinerShiftGame>();
                wiringGamePrefab ??= new GameObject("WiringGame Fallback").AddComponent<WiringGame>();
                trunkPackingPrefab ??= new GameObject("TrunkPacking Fallback").AddComponent<TrunkPackingGame>();
            }

            var service = new GameObject("Minigame Service").AddComponent<MinigameService>();
            DontDestroyOnLoad(service.gameObject);
            service.Initialize(InputGate, NarrativeState, Settings, dinerShiftPrefab, wiringGamePrefab, trunkPackingPrefab);
            return service;
        }

        private CinematicPlayer CreateCinematicService()
        {
            if (cinematicCanvasPrefab == null)
            {
                cinematicCanvasPrefab = new GameObject("CinematicCanvas Fallback", typeof(RectTransform));
            }

            var cinematicRoot = Instantiate(cinematicCanvasPrefab);
            var canvas = cinematicRoot.GetComponent<Canvas>() ?? cinematicRoot.AddComponent<Canvas>();
            if (cinematicRoot.GetComponent<CanvasGroup>() == null)
            {
                cinematicRoot.AddComponent<CanvasGroup>();
            }
            if (cinematicRoot.GetComponent<GraphicRaycaster>() == null)
            {
                cinematicRoot.AddComponent<GraphicRaycaster>();
            }
            if (cinematicRoot.GetComponent<CanvasScaler>() == null)
            {
                cinematicRoot.AddComponent<CanvasScaler>();
            }
            var video = cinematicRoot.GetComponent<VideoPlayer>();
            if (video == null)
            {
                video = cinematicRoot.AddComponent<VideoPlayer>();
            }
            var audio = cinematicRoot.GetComponent<AudioSource>();
            if (audio == null)
            {
                audio = cinematicRoot.AddComponent<AudioSource>();
            }
            audio.playOnAwake = false;
            var voiceGroups = audioMixer != null ? audioMixer.FindMatchingGroups("Master/Voice") : Array.Empty<AudioMixerGroup>();
            audio.outputAudioMixerGroup = voiceGroups.Length > 0 ? voiceGroups[0] : null;
            video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            video.controlledAudioTrackCount = 1;
            video.EnableAudioTrack(0, true);
            video.SetTargetAudioSource(0, audio);
            var host = cinematicRoot.GetComponent<RenderTextureHost>();
            if (host == null)
            {
                host = cinematicRoot.AddComponent<RenderTextureHost>();
            }
            var player = cinematicRoot.GetComponent<CinematicPlayer>();
            if (player == null)
            {
                player = cinematicRoot.AddComponent<CinematicPlayer>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            DontDestroyOnLoad(player.gameObject);
            player.Initialize(InputGate, NarrativeState, Settings, new VideoPlayerPlayback(video), host, SaveGame);
            return player;
        }

        private DialogueView CreateDialogueView()
        {
            if (dialogueViewPrefab == null)
            {
                Debug.LogError("GameBootstrap requires a DialogueView prefab.", this);
                return null;
            }

            var view = Instantiate(dialogueViewPrefab).GetComponent<DialogueView>();
            if (view == null)
            {
                Debug.LogError("GameBootstrap dialogue prefab requires DialogueView.", this);
                return null;
            }

            DontDestroyOnLoad(view.gameObject);
            view.Bind(Dialogue);
            return view;
        }

        private EndingPresentationController CreateEndingPresentation()
        {
            var presentation = new GameObject("Ending Presentation", typeof(RectTransform)).AddComponent<EndingPresentationController>();
            DontDestroyOnLoad(presentation.gameObject);
            presentation.Initialize(InputGate);
            presentation.ReturnedToTitle += ReturnToTitle;
            return presentation;
        }

        private PauseController CreateMenus()
        {
            var titleRoot = titleMenuPrefab != null
                ? Instantiate(titleMenuPrefab)
                : new GameObject("TitleMenu", typeof(RectTransform));
            var menu = titleRoot.GetComponent<PauseController>() ?? titleRoot.AddComponent<PauseController>();
            var pauseRoot = InstantiateOrCreate(pauseMenuPrefab, "PauseMenu");
            var settingsRoot = InstantiateOrCreate(settingsMenuPrefab, "SettingsMenu");
            var creditsRoot = InstantiateOrCreate(creditsPrefab, "Credits");
            menu.AttachPanels(pauseRoot, settingsRoot.GetComponent<SettingsMenuController>(), creditsRoot);
            DontDestroyOnLoad(titleRoot);
            DontDestroyOnLoad(pauseRoot);
            DontDestroyOnLoad(settingsRoot);
            DontDestroyOnLoad(creditsRoot);
            menu.Initialize(InputGate, SaveGame, Settings, audioMixer, SessionSettingsPath, StartNewGame, ContinueGame, ReturnToTitle, SaveAndQuitGame);
            return menu;
        }

        private static GameObject InstantiateOrCreate(GameObject prefab, string fallbackName)
        {
            return prefab != null ? Instantiate(prefab) : new GameObject(fallbackName, typeof(RectTransform));
        }

        private void ReturnToTitle()
        {
            IsSessionActive = false;
            CancelRuntimeState();
            Time.timeScale = 1f;
            Menus?.ShowTitle();
        }

        private void StartNewGame()
        {
            if (sessionRestarting) return;
            StartCoroutine(RestartSession(false));
        }

        private bool ContinueGame()
        {
            if (sessionRestarting || SaveGame == null || !System.IO.File.Exists(SaveGame.SavePath)) return false;
            StartCoroutine(RestartSession(true));
            return true;
        }

        private bool SaveAndQuitGame()
        {
            if (SaveGame == null || NarrativeState == null || !SaveGame.Save(NarrativeState.State))
            {
                Debug.LogError("Northbound could not save the current game. The application will remain open.", this);
                return false;
            }

            if (SessionQuitAction != null) SessionQuitAction();
            else Application.Quit();
            return true;
        }

        private IEnumerator RestartSession(bool restoreSave)
        {
            sessionRestarting = true;
            IsSessionActive = false;
            sessionTransitionLease?.Dispose();
            sessionTransitionLease = InputGate.Acquire(this);
            CancelRuntimeState();
            NarrativeState.Replace(restoreSave ? SaveGame.LoadOrNew() : new NarrativeState());

            var load = SceneManager.LoadSceneAsync(SceneIds.Greybridge, LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;
            yield return null;

            IsSessionActive = true;
            var flow = FindFirstObjectByType<GameFlowController>();
            var entered = flow != null && (restoreSave ? flow.RestoreOrEnterPrologue() : flow.EnterChapter("prologue"));
            if (!entered)
            {
                IsSessionActive = false;
                Menus?.ShowTitle();
            }

            sessionTransitionLease?.Dispose();
            sessionTransitionLease = null;
            sessionRestarting = false;
        }

        private void CancelRuntimeState()
        {
            Cinematics?.Cancel();
            Dialogue?.ResetSession();
            Endings?.Cancel();
            Minigames?.ResetSession();
        }

        public bool PlayCinematic(string cinematicId)
        {
            var cinematic = cinematicCatalog != null ? cinematicCatalog.Find(cinematicId) : null;
            return Cinematics != null && Cinematics.Play(cinematic);
        }
    }
}
