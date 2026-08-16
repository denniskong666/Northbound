using System;
using System.Collections;
using System.IO;
using Northbound.Cinematics;
using Northbound.Core;
using Northbound.Narrative;
using Northbound.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Video;
using UnityEngine.Audio;
using System.Linq;

namespace Northbound.Tests
{
    public sealed class CinematicPlayerTests
    {
        private GameObject gameObject;
        private InputGate inputGate;
        private NarrativeStateStore state;
        private FakePlayback playback;
        private FakePresentation presentation;
        private CinematicPlayer player;
        private CinematicAsset asset;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Cinematic Player Test");
            inputGate = gameObject.AddComponent<InputGate>();
            state = new NarrativeStateStore();
            playback = new FakePlayback();
            presentation = new FakePresentation();
            player = gameObject.AddComponent<CinematicPlayer>();
            player.Initialize(inputGate, state, new SettingsModel(), playback, presentation);
            asset = ScriptableObject.CreateInstance<CinematicAsset>();
            asset.id = "opening";
            asset.completionFact = "cinematic_opening_complete";
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(asset);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Play_PreparesBeforePlaybackAndBlocksInput()
        {
            player.Play(asset);

            Assert.That(playback.Calls, Is.EqualTo(new[] { "Prepare" }));
            Assert.That(player.IsPlaying, Is.True);
            Assert.That(inputGate.IsBlocked, Is.True);

            playback.RaisePrepared();

            Assert.That(playback.Calls, Is.EqualTo(new[] { "Prepare", "Play" }));
            Assert.That(presentation.ShowCount, Is.EqualTo(1));
        }

        [Test]
        public void Skip_IsLockedForTwoSecondsThenCompletesWithTheSameFact()
        {
            player.Play(asset);
            playback.RaisePrepared();

            player.Skip();
            Assert.That(playback.StopCount, Is.Zero);
            Assert.That(player.IsPlaying, Is.True);

            player.Tick(1.99f);
            Assert.That(player.CanSkip, Is.False);
            player.Tick(0.01f);
            Assert.That(player.CanSkip, Is.True);
            player.Skip();

            Assert.That(state.Has(asset.completionFact), Is.True);
            Assert.That(player.IsPlaying, Is.False);
            Assert.That(playback.StopCount, Is.EqualTo(1));
        }

        [Test]
        public void NaturalCompletionAndSkip_RestoreInputAudioAndCameraExactlyOnce()
        {
            var finishedCount = 0;
            player.Finished += _ => finishedCount++;
            player.Play(asset);
            playback.RaisePrepared();
            playback.RaiseFinished();
            playback.RaiseFinished();
            player.Skip();

            Assert.That(state.Has(asset.completionFact), Is.True);
            Assert.That(inputGate.IsBlocked, Is.False);
            Assert.That(presentation.HideCount, Is.EqualTo(1));
            Assert.That(presentation.RestoreAudioCount, Is.EqualTo(1));
            Assert.That(presentation.RestoreCameraCount, Is.EqualTo(1));
            Assert.That(finishedCount, Is.EqualTo(1));
        }

        [Test]
        public void PlaybackFailure_BeforeOrAfterPrepareRestoresOnceAndRecordsTheError()
        {
            var failureCount = 0;
            player.Failed += _ => failureCount++;

            player.Play(asset);
            playback.RaiseFailed("prepare failed");

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(player.LastError, Is.EqualTo("prepare failed"));
            Assert.That(inputGate.IsBlocked, Is.False);
            Assert.That(presentation.HideCount, Is.EqualTo(1));
            Assert.That(presentation.RestoreAudioCount, Is.EqualTo(1));
            Assert.That(presentation.RestoreCameraCount, Is.EqualTo(1));

            player.Play(asset);
            playback.RaisePrepared();
            playback.RaiseFailed("decode failed");
            playback.RaiseFailed("duplicate failure");

            Assert.That(player.IsPlaying, Is.False);
            Assert.That(player.LastError, Is.EqualTo("decode failed"));
            Assert.That(inputGate.IsBlocked, Is.False);
            Assert.That(presentation.HideCount, Is.EqualTo(2));
            Assert.That(presentation.RestoreAudioCount, Is.EqualTo(2));
            Assert.That(presentation.RestoreCameraCount, Is.EqualTo(2));
            Assert.That(failureCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator RenderTextureHost_ProgressesTimedCuesAndClearsThemAfterSkip()
        {
            var hostObject = new GameObject("Timed subtitle host", typeof(RectTransform));
            var host = hostObject.AddComponent<RenderTextureHost>();
            var hostPlayer = hostObject.AddComponent<CinematicPlayer>();
            var hostPlayback = new FakePlayback();
            var timedAsset = ScriptableObject.CreateInstance<CinematicAsset>();
            timedAsset.id = "timed";
            timedAsset.subtitleCues = new[]
            {
                new CinematicSubtitleCue { startSeconds = 0f, text = "First cue." },
                new CinematicSubtitleCue { startSeconds = 3f, text = "Second cue." }
            };
            hostPlayer.Initialize(inputGate, state, new SettingsModel(), hostPlayback, host);

            Assert.That(hostPlayer.Play(timedAsset), Is.True);
            hostPlayback.RaisePrepared();
            yield return null;
            Assert.That(hostObject.GetComponentInChildren<UnityEngine.UI.Text>(true).text, Is.EqualTo("First cue."));

            hostPlayer.Tick(3.1f);
            yield return null;
            Assert.That(hostObject.GetComponentInChildren<UnityEngine.UI.Text>(true).text, Is.EqualTo("Second cue."));

            hostPlayer.Skip();
            Assert.That(hostObject.GetComponentInChildren<UnityEngine.UI.Text>(true).text, Is.Empty);
            UnityEngine.Object.DestroyImmediate(timedAsset);
            UnityEngine.Object.DestroyImmediate(hostObject);
        }

        [Test]
        public void Completion_PersistsBeforeFinishedAndSaveFailureFailsClosedWithoutBlockingInput()
        {
            var savePath = Path.Combine(Application.temporaryCachePath, "northbound-cinematic-save-" + Guid.NewGuid() + ".json");
            var saveGame = new SaveGameService(savePath);
            var finishedAfterPersistence = false;
            player.Initialize(inputGate, state, new SettingsModel(), playback, presentation, saveGame);
            player.Finished += _ => finishedAfterPersistence = saveGame.LoadOrNew().Has(asset.completionFact);

            player.Play(asset);
            playback.RaisePrepared();
            playback.RaiseFinished();

            Assert.That(finishedAfterPersistence, Is.True);
            Assert.That(saveGame.LoadOrNew().Has(asset.completionFact), Is.True);
            Assert.That(state.Has(asset.completionFact), Is.True);

            var failedObject = new GameObject("Failed cinematic save harness");
            var failedGate = failedObject.AddComponent<InputGate>();
            var failedState = new NarrativeStateStore();
            var failedPlayback = new FakePlayback();
            var failedPresentation = new FakePresentation();
            var failedPlayer = failedObject.AddComponent<CinematicPlayer>();
            failedPlayer.Initialize(failedGate, failedState, new SettingsModel(), failedPlayback, failedPresentation, new SaveGameService("/dev/null/northbound-cinematic-save.json"));
            var finishedCount = 0;
            failedPlayer.Finished += _ => finishedCount++;

            failedPlayer.Play(asset);
            failedPlayback.RaisePrepared();
            failedPlayback.RaiseFinished();

            Assert.That(failedState.Has(asset.completionFact), Is.False);
            Assert.That(finishedCount, Is.Zero);
            Assert.That(failedPlayer.LastError, Is.EqualTo("Unable to save cinematic completion."));
            Assert.That(failedGate.IsBlocked, Is.False);
            Assert.That(failedPresentation.HideCount, Is.EqualTo(1));
            Assert.That(failedPresentation.RestoreAudioCount, Is.EqualTo(1));
            Assert.That(failedPresentation.RestoreCameraCount, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(failedObject);
        }

        [UnityTest]
        public IEnumerator Bootstrap_ProvidesTheRuntimeCinematicServiceAndFullscreenCanvas()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneIds.Bootstrap, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return null;
            yield return null;
            for (var frame = 0; frame < 10 && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != SceneIds.Greybridge; frame++)
            {
                yield return null;
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneIds.Greybridge, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return null;

            var bootstrap = GameBootstrap.Instance;
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.Cinematics, Is.Not.Null);
            Assert.That(bootstrap.Minigames, Is.Not.Null);
            Assert.That(bootstrap.Cinematics.GetComponentInChildren<Canvas>(true), Is.Not.Null);
            Assert.That(bootstrap.Cinematics.GetComponentInChildren<RenderTextureHost>(true), Is.Not.Null);
            Assert.That(bootstrap.Cinematics.GetComponentsInChildren<Component>(true), Has.None.Null,
                "The cinematic runtime prefab must not contain missing-script placeholders.");
            Assert.That(bootstrap.Minigames.GetComponentsInChildren<Component>(true), Has.None.Null,
                "The minigame runtime prefabs must not contain missing-script placeholders.");

            var video = bootstrap.Cinematics.GetComponent<VideoPlayer>();
            var audio = bootstrap.Cinematics.GetComponent<AudioSource>();
            var mixerField = typeof(GameBootstrap).GetField("audioMixer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var mixer = (AudioMixer)mixerField.GetValue(bootstrap);
            Assert.That(audio, Is.Not.Null);
            Assert.That(audio.outputAudioMixerGroup, Is.SameAs(mixer.FindMatchingGroups("Master/Voice").Single()));
            Assert.That(video.audioOutputMode, Is.EqualTo(VideoAudioOutputMode.AudioSource));
            Assert.That(video.controlledAudioTrackCount, Is.EqualTo(1));
            Assert.That(video.IsAudioTrackEnabled(0), Is.True);
            Assert.That(video.GetTargetAudioSource(0), Is.SameAs(audio));
            var runtimeValues = new[]
            {
                (parameter: "MasterVolume", decibels: -1f),
                (parameter: "MusicVolume", decibels: -2f),
                (parameter: "SFXVolume", decibels: -3f),
                (parameter: "VoiceVolume", decibels: -4f)
            };
            var originalValues = runtimeValues.Select(value =>
            {
                Assert.That(mixer.GetFloat(value.parameter, out var current), Is.True, value.parameter);
                return (value.parameter, decibels: current);
            }).ToArray();
            try
            {
                foreach (var value in runtimeValues)
                {
                    Assert.That(mixer.SetFloat(value.parameter, value.decibels), Is.True, value.parameter);
                    AssertMixer(mixer, value.parameter, value.decibels);
                }
            }
            finally
            {
                foreach (var value in originalValues)
                {
                    mixer.SetFloat(value.parameter, value.decibels);
                }
            }
        }

        [UnityTest]
        public IEnumerator ActualCinematicMix_AttenuatesLiveBaseAndRestoresExactlyOnSkipAndError()
        {
            if (GameBootstrap.Instance != null)
            {
                UnityEngine.Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneIds.Bootstrap, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return null;
            yield return null;
            var bootstrap = GameBootstrap.Instance;
            var mixerField = typeof(GameBootstrap).GetField("audioMixer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var mixer = (AudioMixer)mixerField.GetValue(bootstrap);
            var opening = bootstrap.CinematicCatalog.Find("opening");
            var host = bootstrap.Cinematics.GetComponent<RenderTextureHost>();
            var actualPlayback = new FakePlayback();
            bootstrap.Cinematics.Initialize(bootstrap.InputGate, new NarrativeStateStore(), bootstrap.Settings, actualPlayback, host);

            mixer.SetFloat("MasterVolume", -2f);
            mixer.SetFloat("MusicVolume", -4f);
            mixer.SetFloat("SFXVolume", -5f);
            mixer.SetFloat("VoiceVolume", -3f);
            Assert.That(bootstrap.Cinematics.Play(opening), Is.True);
            AssertMixer(mixer, "MasterVolume", -2f);
            AssertMixer(mixer, "MusicVolume", -10f);
            AssertMixer(mixer, "SFXVolume", -17f);
            AssertMixer(mixer, "VoiceVolume", -3f);
            bootstrap.Cinematics.Tick(2f);
            bootstrap.Cinematics.Skip();
            AssertBaseMix(mixer);

            Assert.That(bootstrap.Cinematics.Play(opening), Is.True);
            actualPlayback.RaiseFailed("decode failed");
            AssertBaseMix(mixer);
        }

        [UnityTest]
        public IEnumerator Bootstrap_CinematicCatalogImportsEvery1080pProxySlot()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneIds.Bootstrap, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return null;
            yield return null;

            var catalog = GameBootstrap.Instance.CinematicCatalog;
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.All, Has.Length.EqualTo(6));
            foreach (var cinematic in catalog.All)
            {
                Assert.That(cinematic, Is.Not.Null);
                Assert.That(cinematic.clip, Is.Not.Null, cinematic.id);
                Assert.That(cinematic.clip.width, Is.EqualTo(1920), cinematic.id);
                Assert.That(cinematic.clip.height, Is.EqualTo(1080), cinematic.id);
            }
        }

        [UnityTest]
        public IEnumerator EveryCinematicSlot_GrantsItsCompletionFactWhenWatchedOrSkipped()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneIds.Bootstrap, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return null;
            yield return null;

            var catalog = GameBootstrap.Instance.CinematicCatalog;
            var watchedState = new NarrativeStateStore();
            var watchedObject = new GameObject("Watched cinematic harness");
            var watchedGate = watchedObject.AddComponent<InputGate>();
            var watchedPlayback = new FakePlayback();
            var watchedPlayer = watchedObject.AddComponent<CinematicPlayer>();
            watchedPlayer.Initialize(watchedGate, watchedState, new SettingsModel(), watchedPlayback, new FakePresentation());
            foreach (var cinematic in catalog.All)
            {
                Assert.That(watchedPlayer.Play(cinematic), Is.True, cinematic.id);
                watchedPlayback.RaisePrepared();
                watchedPlayback.RaiseFinished();
                Assert.That(watchedState.Has(cinematic.completionFact), Is.True, cinematic.id);
                Assert.That(watchedGate.IsBlocked, Is.False, cinematic.id);
            }

            var skippedState = new NarrativeStateStore();
            var skippedObject = new GameObject("Skipped cinematic harness");
            var skippedGate = skippedObject.AddComponent<InputGate>();
            var skippedPlayback = new FakePlayback();
            var skippedPlayer = skippedObject.AddComponent<CinematicPlayer>();
            skippedPlayer.Initialize(skippedGate, skippedState, new SettingsModel(), skippedPlayback, new FakePresentation());
            foreach (var cinematic in catalog.All)
            {
                Assert.That(skippedPlayer.Play(cinematic), Is.True, cinematic.id);
                skippedPlayback.RaisePrepared();
                skippedPlayer.Tick(2f);
                skippedPlayer.Skip();
                Assert.That(skippedState.Has(cinematic.completionFact), Is.True, cinematic.id);
                Assert.That(skippedGate.IsBlocked, Is.False, cinematic.id);
            }

            UnityEngine.Object.Destroy(watchedObject);
            UnityEngine.Object.Destroy(skippedObject);
        }

        [UnityTest]
        public IEnumerator Greybridge_CinematicRoutesAreAutomaticAndNotPlayerInteractable()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneIds.Bootstrap, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return null;
            yield return null;
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneIds.Greybridge, UnityEngine.SceneManagement.LoadSceneMode.Single);
            yield return null;

            var routes = UnityEngine.Object.FindObjectsByType<CinematicRouteTrigger>(FindObjectsSortMode.None);

            Assert.That(routes, Is.Empty, "Approved cinematics are chapter/quest driven and cannot be stacked as ambiguous world routes.");
        }

        private sealed class FakePlayback : IVideoPlayback
        {
            public event Action Prepared;
            public event Action Finished;
            public event Action<string> Failed;
            public readonly System.Collections.Generic.List<string> Calls = new System.Collections.Generic.List<string>();
            public int StopCount { get; private set; }

            public void Prepare(VideoClip clip) => Calls.Add("Prepare");
            public void Play() => Calls.Add("Play");
            public void Stop() => StopCount++;
            public void RaisePrepared() => Prepared?.Invoke();
            public void RaiseFinished() => Finished?.Invoke();
            public void RaiseFailed(string message) => Failed?.Invoke(message);
        }

        private sealed class FakePresentation : ICinematicPresentation
        {
            public int ShowCount { get; private set; }
            public int HideCount { get; private set; }
            public int RestoreAudioCount { get; private set; }
            public int RestoreCameraCount { get; private set; }

            public void Show(CinematicAsset asset, SettingsModel settings) => ShowCount++;
            public void SetPlaybackTime(CinematicAsset asset, float elapsedSeconds, SettingsModel settings) { }
            public void Hide() => HideCount++;
            public void RestoreGameplayAudio(CinematicAsset asset) => RestoreAudioCount++;
            public void RestoreCamera() => RestoreCameraCount++;
        }

        private static void AssertBaseMix(AudioMixer mixer)
        {
            AssertMixer(mixer, "MasterVolume", -2f);
            AssertMixer(mixer, "MusicVolume", -4f);
            AssertMixer(mixer, "SFXVolume", -5f);
            AssertMixer(mixer, "VoiceVolume", -3f);
        }

        private static void AssertMixer(AudioMixer mixer, string parameter, float expected)
        {
            Assert.That(mixer.GetFloat(parameter, out var actual), Is.True, parameter);
            Assert.That(actual, Is.EqualTo(expected).Within(.001f), parameter);
        }
    }
}
