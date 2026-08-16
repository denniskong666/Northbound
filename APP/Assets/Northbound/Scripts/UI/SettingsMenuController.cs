using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Northbound.UI
{
    /// <summary>Binds the authored settings controls to the shared persistent settings model and audio mixer.</summary>
    public sealed class SettingsMenuController : MonoBehaviour
    {
        private SettingsModel settings;
        private AudioMixer mixer;
        private string settingsPath;

        public SettingsModel Settings => settings;
        public event Action Applied;

        public void Initialize(SettingsModel model, AudioMixer audioMixer, string path = null)
        {
            settings = model ?? throw new ArgumentNullException(nameof(model));
            GameText.Use(settings.Language);
            mixer = audioMixer;
            settingsPath = string.IsNullOrWhiteSpace(path) ? SettingsModel.DefaultPath : path;
            EnsureLanguageButton();
            PopulateControls();
            var apply = Button("Apply");
            apply.onClick.RemoveListener(Apply);
            apply.onClick.AddListener(Apply);
            ApplyAudio(settings, mixer);
            RefreshLocalizedLabels();
        }

        public void Apply()
        {
            if (settings == null)
            {
                return;
            }

            settings.MasterVolume = Slider("Master Volume").value;
            settings.MusicVolume = Slider("Music Volume").value;
            settings.SfxVolume = Slider("SFX Volume").value;
            settings.VoiceVolume = Slider("Voice Volume").value;
            settings.SubtitleScale = Slider("Subtitle Scale").value;
            settings.SubtitleBackgroundOpacity = Slider("Subtitle Background Opacity").value;
            settings.InteractionTimeMultiplier = Slider("Interaction Time Multiplier").value;
            settings.ReducedMotion = Toggle("Reduced Motion").isOn;
            settings.SkipMinigames = Toggle("Skip Minigames").isOn;
            ApplyAudio(settings, mixer);
            SettingsModel.Save(settingsPath, settings);
            Applied?.Invoke();
        }

        public static void ApplyAudio(SettingsModel model, AudioMixer audioMixer)
        {
            if (model == null || audioMixer == null)
            {
                return;
            }

            audioMixer.SetFloat("MasterVolume", ToDecibels(model.MasterVolume));
            audioMixer.SetFloat("MusicVolume", ToDecibels(model.MusicVolume));
            audioMixer.SetFloat("SFXVolume", ToDecibels(model.SfxVolume));
            audioMixer.SetFloat("VoiceVolume", ToDecibels(model.VoiceVolume));
        }

        private void PopulateControls()
        {
            Slider("Master Volume").value = settings.MasterVolume;
            Slider("Music Volume").value = settings.MusicVolume;
            Slider("SFX Volume").value = settings.SfxVolume;
            Slider("Voice Volume").value = settings.VoiceVolume;
            Slider("Subtitle Scale").value = settings.SubtitleScale;
            Slider("Subtitle Background Opacity").value = settings.SubtitleBackgroundOpacity;
            Slider("Interaction Time Multiplier").value = settings.InteractionTimeMultiplier;
            Toggle("Reduced Motion").isOn = settings.ReducedMotion;
            Toggle("Skip Minigames").isOn = settings.SkipMinigames;
        }

        private void EnsureLanguageButton()
        {
            var existing = GetComponentsInChildren<Button>(true).FirstOrDefault(control => control.name == "Language");
            if (existing == null)
            {
                var buttonObject = new GameObject("Language", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(transform, false);
                var rect = buttonObject.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(0f, -405f);
                rect.sizeDelta = new Vector2(520f, 58f);
                buttonObject.GetComponent<Image>().color = new Color(.12f, .2f, .25f, .96f);
                existing = buttonObject.GetComponent<Button>();
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(buttonObject.transform, false);
                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.sizeDelta = Vector2.zero;
                var label = labelObject.GetComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 25; label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.raycastTarget = false;
            }
            existing.onClick.RemoveListener(ToggleLanguage);
            existing.onClick.AddListener(ToggleLanguage);
        }

        private void ToggleLanguage()
        {
            if (settings == null) return;
            settings.Language = settings.Language == GameLanguage.English ? GameLanguage.SimplifiedChinese : GameLanguage.English;
            SettingsModel.Save(settingsPath, settings);
            RefreshLocalizedLabels();
            Applied?.Invoke();
        }

        private void RefreshLocalizedLabels()
        {
            foreach (var control in GetComponentsInChildren<Selectable>(true))
            {
                var label = control.GetComponentInChildren<Text>(true);
                if (label == null) continue;
                label.text = control.name == "Language"
                    ? GameText.T("Language: English / 中文", "语言：中文 / English")
                    : GameText.UiLabel(control.name);
                GameText.ApplyFont(label);
            }
            foreach (var label in GetComponentsInChildren<Text>(true))
            {
                if (label.gameObject.name == "Heading") label.text = GameText.T("Settings", "设置");
                GameText.ApplyFont(label);
            }
        }

        private Slider Slider(string controlName) => GetComponentsInChildren<Slider>(true).Single(control => control.name == controlName);
        private Toggle Toggle(string controlName) => GetComponentsInChildren<Toggle>(true).Single(control => control.name == controlName);
        private Button Button(string controlName) => GetComponentsInChildren<Button>(true).Single(control => control.name == controlName);

        private static float ToDecibels(float linear) => linear <= 0f ? -80f : Mathf.Log10(linear) * 20f;
    }
}
