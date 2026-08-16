using System;
using Northbound.Core;
using Northbound.Narrative;
using Northbound.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

namespace Northbound.Cinematics
{
    public sealed class CinematicPlayer : MonoBehaviour
    {
        private const float SkipLockoutSeconds = 2f;

        private InputGate inputGate;
        private NarrativeStateStore state;
        private SettingsModel settings;
        private SaveGameService saveGame;
        private IVideoPlayback playback;
        private ICinematicPresentation presentation;
        private IDisposable inputLease;
        private CinematicAsset activeAsset;
        private float elapsed;
        private bool completed;

        public bool IsPlaying { get; private set; }
        public bool CanSkip => IsPlaying && elapsed >= SkipLockoutSeconds;
        public string LastError { get; private set; }
        public event Action<string> Finished;
        public event Action<string> Failed;

        private void Awake()
        {
            var host = GetComponent<RenderTextureHost>();
            var videoPlayer = GetComponent<VideoPlayer>();
            if (host != null && videoPlayer != null)
            {
                ConfigurePlayback(new VideoPlayerPlayback(videoPlayer), host);
                if (host.SkipButton != null)
                {
                    host.SkipButton.onClick.AddListener(Skip);
                }
            }
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
            if (!CanSkip || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Skip();
            }
        }

        private void OnDestroy()
        {
            Complete(false);
            if (playback != null)
            {
                playback.Prepared -= OnPrepared;
                playback.Finished -= OnPlaybackFinished;
                playback.Failed -= OnPlaybackFailed;
            }
        }

        public void Initialize(InputGate gate, NarrativeStateStore narrativeState, SettingsModel settingsModel, IVideoPlayback playbackAdapter = null, ICinematicPresentation presentationAdapter = null, SaveGameService saveGameService = null)
        {
            inputGate = gate ?? throw new ArgumentNullException(nameof(gate));
            state = narrativeState ?? throw new ArgumentNullException(nameof(narrativeState));
            settings = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));
            saveGame = saveGameService;
            if (playbackAdapter != null && presentationAdapter != null)
            {
                ConfigurePlayback(playbackAdapter, presentationAdapter);
            }
        }

        public bool Play(CinematicAsset asset)
        {
            if (asset == null || IsPlaying || inputGate == null || state == null || playback == null || presentation == null)
            {
                return false;
            }

            activeAsset = asset;
            elapsed = 0f;
            completed = false;
            LastError = null;
            IsPlaying = true;
            inputLease = inputGate.Acquire(this);
            presentation.Show(asset, settings);
            playback.Prepare(asset.clip);
            return true;
        }

        public void Tick(float deltaSeconds)
        {
            if (!IsPlaying)
            {
                return;
            }

            var wasLocked = !CanSkip;
            elapsed += Mathf.Max(0f, deltaSeconds);
            presentation?.SetPlaybackTime(activeAsset, elapsed, settings);
            if (wasLocked && CanSkip && presentation is RenderTextureHost host)
            {
                host.SetSkipAvailable(true);
            }
        }

        public void Skip()
        {
            if (!CanSkip)
            {
                return;
            }

            playback.Stop();
            Complete(true);
        }

        public void Cancel()
        {
            if (!IsPlaying)
            {
                return;
            }

            playback.Stop();
            Complete(false);
        }

        private void ConfigurePlayback(IVideoPlayback playbackAdapter, ICinematicPresentation presentationAdapter)
        {
            if (playback != null)
            {
                playback.Prepared -= OnPrepared;
                playback.Finished -= OnPlaybackFinished;
                playback.Failed -= OnPlaybackFailed;
            }

            playback = playbackAdapter;
            presentation = presentationAdapter;
            playback.Prepared += OnPrepared;
            playback.Finished += OnPlaybackFinished;
            playback.Failed += OnPlaybackFailed;
        }

        private void OnPrepared()
        {
            if (IsPlaying && !completed)
            {
                playback.Play();
            }
        }

        private void OnPlaybackFinished()
        {
            Complete(true);
        }

        private void OnPlaybackFailed(string error)
        {
            if (!IsPlaying || completed)
            {
                return;
            }

            LastError = string.IsNullOrWhiteSpace(error) ? "Cinematic playback failed." : error;
            Complete(false);
            Failed?.Invoke(LastError);
        }

        private void Complete(bool grantCompletionFact)
        {
            if (completed)
            {
                return;
            }

            var completedAsset = activeAsset;
            if (grantCompletionFact && !TryPersistCompletion(completedAsset))
            {
                LastError = "Unable to save cinematic completion.";
                Finish(false);
                Failed?.Invoke(LastError);
                return;
            }

            Finish(grantCompletionFact);
        }

        private bool TryPersistCompletion(CinematicAsset completedAsset)
        {
            if (completedAsset == null || string.IsNullOrWhiteSpace(completedAsset.completionFact))
            {
                return true;
            }

            if (saveGame != null)
            {
                var prospective = NarrativeState.FromJson(state.State.ToJson());
                prospective.Set(completedAsset.completionFact, true);
                if (!saveGame.Save(prospective))
                {
                    return false;
                }
            }

            state.Set(completedAsset.completionFact, true);
            return true;
        }

        private void Finish(bool grantCompletionFact)
        {
            completed = true;
            var completedAsset = activeAsset;
            if (IsPlaying)
            {
                presentation?.Hide();
                presentation?.RestoreGameplayAudio(completedAsset);
                presentation?.RestoreCamera();
            }

            inputLease?.Dispose();
            inputLease = null;
            var cinematicId = completedAsset != null ? completedAsset.id : string.Empty;
            var shouldNotify = IsPlaying && grantCompletionFact;
            IsPlaying = false;
            activeAsset = null;
            if (shouldNotify)
            {
                Finished?.Invoke(cinematicId);
            }
        }
    }
}
