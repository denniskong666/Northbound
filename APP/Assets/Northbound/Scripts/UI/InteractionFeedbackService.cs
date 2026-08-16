using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Northbound.UI
{
    public enum FeedbackKind { Guidance, Success, Error }

    public sealed class InteractionFeedbackService : MonoBehaviour
    {
        private readonly Dictionary<FeedbackKind, AudioClip> clips = new Dictionary<FeedbackKind, AudioClip>();
        private CanvasGroup group;
        private Text label;
        private AudioSource audioSource;
        private float visibleUntil;

        public string VisibleMessage { get; private set; } = string.Empty;
        public FeedbackKind LastKind { get; private set; }

        public static InteractionFeedbackService Create(AudioMixerGroup output)
        {
            var root = new GameObject("Interaction Feedback", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(AudioSource));
            DontDestroyOnLoad(root);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 140;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            var service = root.AddComponent<InteractionFeedbackService>();
            service.Build(output);
            return service;
        }

        public void Show(string message, FeedbackKind kind)
        {
            VisibleMessage = message ?? string.Empty;
            LastKind = kind;
            label.text = VisibleMessage;
            GameText.ApplyFont(label);
            label.color = kind == FeedbackKind.Success ? new Color(.58f, 1f, .7f) : kind == FeedbackKind.Error ? new Color(1f, .58f, .42f) : new Color(1f, .84f, .4f);
            group.alpha = 1f;
            visibleUntil = Time.unscaledTime + 2.4f;
            audioSource.clip = clips[kind];
            audioSource.Play();
        }

        private void Build(AudioMixerGroup output)
        {
            group = GetComponent<CanvasGroup>(); group.alpha = 0f; group.blocksRaycasts = false;
            audioSource = GetComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.outputAudioMixerGroup = output;
            clips[FeedbackKind.Guidance] = Tone("Guidance Tick", 440f, 554f, .13f);
            clips[FeedbackKind.Success] = Tone("Success Rise", 523f, 784f, .22f);
            clips[FeedbackKind.Error] = Tone("Error Tick", 210f, 165f, .14f);
            var panel = new GameObject("Toast Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .21f); rect.sizeDelta = new Vector2(820, 84);
            panel.GetComponent<Image>().color = new Color(.025f, .035f, .05f, .94f); panel.GetComponent<Image>().raycastTarget = false;
            var textObject = new GameObject("Toast Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            var textRect = textObject.GetComponent<RectTransform>(); textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = new Vector2(28, 8); textRect.offsetMax = new Vector2(-28, -8);
            label = textObject.GetComponent<Text>(); label.font = Resources.Load<Font>("Northbound/LegacyRuntime"); label.fontSize = 36; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
        }

        private void Update()
        {
            if (group != null && Time.unscaledTime > visibleUntil) group.alpha = Mathf.MoveTowards(group.alpha, 0f, Time.unscaledDeltaTime * 3f);
        }

        private static AudioClip Tone(string name, float startFrequency, float endFrequency, float duration)
        {
            const int rate = 44100;
            var sampleCount = Mathf.CeilToInt(rate * duration);
            var samples = new float[sampleCount];
            var phase = 0f;
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleCount;
                phase += Mathf.Lerp(startFrequency, endFrequency, t) * Mathf.PI * 2f / rate;
                var envelope = Mathf.Sin(Mathf.PI * t);
                samples[i] = Mathf.Sin(phase) * envelope * .18f;
            }
            var clip = AudioClip.Create(name, sampleCount, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
