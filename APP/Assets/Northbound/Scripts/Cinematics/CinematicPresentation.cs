using Northbound.UI;
using Northbound.Dialogue;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Linq;
using System;

namespace Northbound.Cinematics
{
    [RequireComponent(typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster))]
    [RequireComponent(typeof(CanvasScaler), typeof(VideoPlayer))]
    public sealed class RenderTextureHost : MonoBehaviour, ICinematicPresentation
    {
        [SerializeField] private RawImage videoImage;
        [SerializeField] private Text subtitleLabel;
        [SerializeField] private Button skipButton;
        [SerializeField] private Text skipLabel;
        [SerializeField] private Image subtitleBackground;

        private CanvasGroup canvasGroup;
        private RenderTexture renderTexture;
        private Camera gameplayCamera;
        private bool gameplayCameraWasEnabled;
        private AudioMixer activeMixer;
        private bool hasCapturedMix;
        private float capturedMaster;
        private float capturedMusic;
        private float capturedSfx;
        private float capturedVoice;

        public Button SkipButton => skipButton;

        private void Awake()
        {
            // The authored canvas is deliberately lightweight.  Keep it safe when a
            // project is opened with a platform/module combination that strips a UI
            // dependency from the serialized prefab: runtime creation must still
            // produce one complete cinematic surface rather than a null service.
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            var canvas = GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            var player = GetComponent<VideoPlayer>() ?? gameObject.AddComponent<VideoPlayer>();
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = renderTexture;
            EnsureVisuals();
            Hide();
        }

        private void OnDestroy()
        {
            RestoreCapturedMix();
            if (renderTexture == null)
            {
                return;
            }

            var player = GetComponent<VideoPlayer>();
            if (player != null && player.targetTexture == renderTexture)
            {
                player.targetTexture = null;
            }

            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }

        private void EnsureVisuals()
        {
            if (videoImage == null)
            {
                var videoObject = new GameObject("Video", typeof(RectTransform), typeof(RawImage));
                videoObject.transform.SetParent(transform, false);
                videoImage = videoObject.GetComponent<RawImage>();
                Stretch(videoObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                videoImage.texture = renderTexture;
                videoImage.color = Color.white;
            }

            if (subtitleBackground == null)
            {
                var backgroundObject = new GameObject("Subtitle Background", typeof(RectTransform), typeof(Image));
                backgroundObject.transform.SetParent(transform, false);
                subtitleBackground = backgroundObject.GetComponent<Image>();
                Stretch(backgroundObject.GetComponent<RectTransform>(), new Vector2(.1f, .08f), new Vector2(.9f, .25f), Vector2.zero, Vector2.zero);
                subtitleBackground.color = new Color(0f, 0f, 0f, .75f);
                subtitleBackground.raycastTarget = false;
                if (subtitleLabel != null)
                {
                    backgroundObject.transform.SetSiblingIndex(subtitleLabel.transform.GetSiblingIndex());
                }
            }

            if (subtitleLabel == null)
            {
                var subtitleObject = new GameObject("Subtitle", typeof(RectTransform), typeof(Text));
                subtitleObject.transform.SetParent(transform, false);
                subtitleLabel = subtitleObject.GetComponent<Text>();
                Stretch(subtitleObject.GetComponent<RectTransform>(), new Vector2(.1f, .08f), new Vector2(.9f, .25f), Vector2.zero, Vector2.zero);
                subtitleLabel.alignment = TextAnchor.MiddleCenter;
                subtitleLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                subtitleLabel.fontSize = 32;
                subtitleLabel.color = Color.white;
                subtitleLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            if (skipButton == null)
            {
                var buttonObject = new GameObject("Skip Button", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(transform, false);
                skipButton = buttonObject.GetComponent<Button>();
                Stretch(buttonObject.GetComponent<RectTransform>(), new Vector2(.78f, .88f), new Vector2(.96f, .96f), Vector2.zero, Vector2.zero);
                buttonObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, .7f);
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(buttonObject.transform, false);
                skipLabel = labelObject.GetComponent<Text>();
                Stretch(labelObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                skipLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                skipLabel.alignment = TextAnchor.MiddleCenter;
                skipLabel.color = Color.white;
            }
        }

        public void Show(CinematicAsset asset, SettingsModel settings)
        {
            gameplayCamera = Camera.main;
            if (gameplayCamera != null)
            {
                gameplayCameraWasEnabled = gameplayCamera.enabled;
                gameplayCamera.enabled = false;
            }

            if (asset != null && asset.cinematicAudioSnapshot != null)
            {
                asset.cinematicAudioSnapshot.TransitionTo(0f);
                CaptureAndApplyCinematicMix(asset.cinematicAudioSnapshot.audioMixer);
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            SetPlaybackTime(asset, 0f, settings);
            SetSkipAvailable(false);
        }

        public void SetPlaybackTime(CinematicAsset asset, float elapsedSeconds, SettingsModel settings)
        {
            if (subtitleLabel == null) return;
            if (settings == null || !settings.ShowSubtitles || asset == null)
            {
                subtitleLabel.text = string.Empty;
                if (subtitleBackground != null) subtitleBackground.enabled = false;
                return;
            }

            var cue = asset.subtitleCues?
                .Where(candidate => candidate != null && candidate.startSeconds <= elapsedSeconds)
                .OrderBy(candidate => candidate.startSeconds)
                .LastOrDefault();
            var cueIndex = cue == null ? -1 : Array.IndexOf(asset.subtitleCues, cue);
            subtitleLabel.text = cue != null
                ? DialogueChineseCatalog.Resolve(DialogueId(asset.id), cueIndex, cue.text, string.Empty)
                : string.Empty;
            if (subtitleBackground != null) subtitleBackground.enabled = !string.IsNullOrEmpty(subtitleLabel.text);
            SubtitleView.Apply(subtitleLabel, subtitleBackground, settings);
        }

        public void SetSkipAvailable(bool value)
        {
            if (skipButton != null)
            {
                skipButton.interactable = value;
            }

            if (skipLabel != null)
            {
                skipLabel.text = value
                    ? GameText.T("Skip (Space / Esc)", "跳过（空格 / Esc）")
                    : GameText.T("Skip available in 2 seconds", "2 秒后可跳过");
            }
        }

        private static string DialogueId(string cinematicId) => cinematicId switch
        {
            "opening" => "prologue_opening",
            "maya" => "highlight_maya",
            "noah" => "highlight_noah",
            "leo" => "highlight_leo",
            "rooftop" => "rooftop_fracture",
            "finale" => "finale_are_you_coming",
            _ => string.Empty
        };

        public void Hide()
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            if (subtitleLabel != null) subtitleLabel.text = string.Empty;
            if (subtitleBackground != null) subtitleBackground.enabled = false;
        }

        public void RestoreGameplayAudio(CinematicAsset asset)
        {
            if (asset != null && asset.gameplayAudioSnapshot != null)
            {
                asset.gameplayAudioSnapshot.TransitionTo(0f);
            }
            RestoreCapturedMix();
        }

        public void RestoreCamera()
        {
            if (gameplayCamera != null)
            {
                gameplayCamera.enabled = gameplayCameraWasEnabled;
                gameplayCamera = null;
            }
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void CaptureAndApplyCinematicMix(AudioMixer mixer)
        {
            if (mixer == null || hasCapturedMix) return;
            activeMixer = mixer;
            capturedMaster = MixerValue("MasterVolume");
            capturedMusic = MixerValue("MusicVolume");
            capturedSfx = MixerValue("SFXVolume");
            capturedVoice = MixerValue("VoiceVolume");
            hasCapturedMix = true;
            activeMixer.SetFloat("MasterVolume", capturedMaster);
            activeMixer.SetFloat("MusicVolume", Mathf.Max(-80f, capturedMusic - 6f));
            activeMixer.SetFloat("SFXVolume", Mathf.Max(-80f, capturedSfx - 12f));
            activeMixer.SetFloat("VoiceVolume", capturedVoice);
        }

        private void RestoreCapturedMix()
        {
            if (activeMixer == null || !hasCapturedMix) return;
            activeMixer.SetFloat("MasterVolume", capturedMaster);
            activeMixer.SetFloat("MusicVolume", capturedMusic);
            activeMixer.SetFloat("SFXVolume", capturedSfx);
            activeMixer.SetFloat("VoiceVolume", capturedVoice);
            activeMixer = null;
            hasCapturedMix = false;
        }

        private float MixerValue(string parameter)
        {
            return activeMixer.GetFloat(parameter, out var value) ? value : 0f;
        }
    }
}
