using System;
using System.IO;
using UnityEngine;

namespace Northbound.UI
{
    /// <summary>Player-owned presentation and accessibility settings. Values are clamped at the model boundary.</summary>
    [Serializable]
    public sealed class SettingsModel
    {
        public const string SettingsFileName = "northbound-settings.json";
        public const float MinimumSubtitleScale = .75f;
        public const float MaximumSubtitleScale = 1.5f;
        public const float MinimumInteractionTimeMultiplier = .5f;
        public const float MaximumInteractionTimeMultiplier = 1.5f;

        [SerializeField] private float masterVolume = .8f;
        [SerializeField] private float musicVolume = .8f;
        [SerializeField] private float sfxVolume = .8f;
        [SerializeField] private float voiceVolume = .8f;
        [SerializeField] private float subtitleScale = 1f;
        [SerializeField] private float subtitleBackgroundOpacity = .75f;
        [SerializeField] private bool reducedMotion;
        [SerializeField] private bool skipMinigames;
        [SerializeField] private bool showSubtitles = true;
        [SerializeField] private float interactionTimeMultiplier = 1f;
        [SerializeField] private GameLanguage language = GameLanguage.English;

        public float MasterVolume { get => masterVolume; set => masterVolume = Mathf.Clamp01(value); }
        public float MusicVolume { get => musicVolume; set => musicVolume = Mathf.Clamp01(value); }
        public float SfxVolume { get => sfxVolume; set => sfxVolume = Mathf.Clamp01(value); }
        public float VoiceVolume { get => voiceVolume; set => voiceVolume = Mathf.Clamp01(value); }
        public float SubtitleScale { get => subtitleScale; set => subtitleScale = Mathf.Clamp(value, MinimumSubtitleScale, MaximumSubtitleScale); }
        public float SubtitleBackgroundOpacity { get => subtitleBackgroundOpacity; set => subtitleBackgroundOpacity = Mathf.Clamp01(value); }
        public bool ReducedMotion { get => reducedMotion; set => reducedMotion = value; }
        public bool SkipMinigames { get => skipMinigames; set => skipMinigames = value; }
        public bool ShowSubtitles { get => showSubtitles; set => showSubtitles = value; }
        public float InteractionTimeMultiplier { get => interactionTimeMultiplier; set => interactionTimeMultiplier = Mathf.Clamp(value, MinimumInteractionTimeMultiplier, MaximumInteractionTimeMultiplier); }
        public GameLanguage Language { get => language; set { language = value; GameText.Use(value); } }

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, SettingsFileName);

        public static SettingsModel CreateFirstRunDefaults()
        {
            var settings = new SettingsModel();
            settings.Language = GameLanguage.SimplifiedChinese;
            return settings;
        }

        public static SettingsModel Load(string path = null)
        {
            var settings = new SettingsModel();
            var isFirstRunPath = string.IsNullOrWhiteSpace(path);
            var resolvedPath = isFirstRunPath ? DefaultPath : path;
            try
            {
                if (!File.Exists(resolvedPath))
                {
                    return isFirstRunPath ? CreateFirstRunDefaults() : settings;
                }

                var persisted = JsonUtility.FromJson<PersistedSettings>(File.ReadAllText(resolvedPath));
                if (persisted == null)
                {
                    return settings;
                }

                settings.Apply(persisted);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (ArgumentException) { }
            return settings;
        }

        public static bool Save(string path, SettingsModel settings)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var temporaryPath = path + ".tmp";
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(temporaryPath, JsonUtility.ToJson((settings ?? new SettingsModel()).ToPersisted(), true));
                if (File.Exists(path))
                {
                    File.Replace(temporaryPath, path, null);
                }
                else
                {
                    File.Move(temporaryPath, path);
                }
                return true;
            }
            catch (IOException)
            {
                DeleteTemporary(temporaryPath);
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                DeleteTemporary(temporaryPath);
                return false;
            }
        }

        public bool Save() => Save(DefaultPath, this);

        private void Apply(PersistedSettings persisted)
        {
            MasterVolume = persisted.masterVolume;
            MusicVolume = persisted.musicVolume;
            SfxVolume = persisted.sfxVolume;
            VoiceVolume = persisted.voiceVolume;
            SubtitleScale = persisted.subtitleScale;
            SubtitleBackgroundOpacity = persisted.subtitleBackgroundOpacity;
            ReducedMotion = persisted.reducedMotion;
            SkipMinigames = persisted.skipMinigames;
            ShowSubtitles = persisted.showSubtitles;
            InteractionTimeMultiplier = persisted.interactionTimeMultiplier;
            Language = persisted.language;
        }

        private PersistedSettings ToPersisted() => new PersistedSettings
        {
            masterVolume = MasterVolume,
            musicVolume = MusicVolume,
            sfxVolume = SfxVolume,
            voiceVolume = VoiceVolume,
            subtitleScale = SubtitleScale,
            subtitleBackgroundOpacity = SubtitleBackgroundOpacity,
            reducedMotion = ReducedMotion,
            skipMinigames = SkipMinigames,
            showSubtitles = ShowSubtitles,
            interactionTimeMultiplier = InteractionTimeMultiplier,
            language = Language
        };

        private static void DeleteTemporary(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        [Serializable]
        private sealed class PersistedSettings
        {
            public float masterVolume = .8f;
            public float musicVolume = .8f;
            public float sfxVolume = .8f;
            public float voiceVolume = .8f;
            public float subtitleScale = 1f;
            public float subtitleBackgroundOpacity = .75f;
            public bool reducedMotion;
            public bool skipMinigames;
            public bool showSubtitles = true;
            public float interactionTimeMultiplier = 1f;
            public GameLanguage language = GameLanguage.English;
        }
    }
}
