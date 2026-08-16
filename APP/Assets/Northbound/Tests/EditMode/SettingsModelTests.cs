using System;
using System.IO;
using System.Reflection;
using Northbound.UI;
using NUnit.Framework;
using UnityEngine;

namespace Northbound.Tests
{
    public sealed class SettingsModelTests
    {
        private string directoryPath;
        private string settingsPath;

        [SetUp]
        public void SetUp()
        {
            GameText.Use(GameLanguage.English);
            directoryPath = Path.Combine(Application.temporaryCachePath, "northbound-settings-tests-" + Guid.NewGuid().ToString("N"));
            settingsPath = Path.Combine(directoryPath, "northbound-settings.json");
        }

        [TearDown]
        public void TearDown()
        {
            GameText.Use(GameLanguage.English);
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }

        [Test]
        public void Defaults_ProvideTheAccessibilityAndAudioContract()
        {
            var settings = new SettingsModel();

            Assert.That(Float(settings, "MasterVolume"), Is.EqualTo(.8f));
            Assert.That(Float(settings, "MusicVolume"), Is.EqualTo(.8f));
            Assert.That(Float(settings, "SfxVolume"), Is.EqualTo(.8f));
            Assert.That(Float(settings, "VoiceVolume"), Is.EqualTo(.8f));
            Assert.That(Float(settings, "SubtitleScale"), Is.EqualTo(1f));
            Assert.That(Float(settings, "SubtitleBackgroundOpacity"), Is.EqualTo(.75f));
            Assert.That(Bool(settings, "ReducedMotion"), Is.False);
            Assert.That(settings.SkipMinigames, Is.False);
            Assert.That(Float(settings, "InteractionTimeMultiplier"), Is.EqualTo(1f));
            Assert.That(settings.Language, Is.EqualTo(GameLanguage.English));
        }

        [Test]
        public void FirstRunDefaults_StartInSimplifiedChinese()
        {
            var settings = SettingsModel.CreateFirstRunDefaults();

            Assert.That(settings.Language, Is.EqualTo(GameLanguage.SimplifiedChinese));
            Assert.That(GameText.IsChinese, Is.True);
        }

        [Test]
        public void SaveAndLoad_RoundTripsAllPersistentSettings()
        {
            var settings = new SettingsModel();
            Set(settings, "MasterVolume", .35f);
            Set(settings, "MusicVolume", .45f);
            Set(settings, "SfxVolume", .55f);
            Set(settings, "VoiceVolume", .65f);
            Set(settings, "SubtitleScale", 1.25f);
            Set(settings, "SubtitleBackgroundOpacity", .2f);
            Set(settings, "ReducedMotion", true);
            settings.SkipMinigames = true;
            Set(settings, "InteractionTimeMultiplier", 1.4f);
            settings.Language = GameLanguage.SimplifiedChinese;

            Assert.That(InvokeStatic<bool>("Save", settingsPath, settings), Is.True);
            var loaded = InvokeStatic<SettingsModel>("Load", settingsPath);

            Assert.That(Float(loaded, "MasterVolume"), Is.EqualTo(.35f));
            Assert.That(Float(loaded, "MusicVolume"), Is.EqualTo(.45f));
            Assert.That(Float(loaded, "SfxVolume"), Is.EqualTo(.55f));
            Assert.That(Float(loaded, "VoiceVolume"), Is.EqualTo(.65f));
            Assert.That(Float(loaded, "SubtitleScale"), Is.EqualTo(1.25f));
            Assert.That(Float(loaded, "SubtitleBackgroundOpacity"), Is.EqualTo(.2f));
            Assert.That(Bool(loaded, "ReducedMotion"), Is.True);
            Assert.That(loaded.SkipMinigames, Is.True);
            Assert.That(Float(loaded, "InteractionTimeMultiplier"), Is.EqualTo(1.4f));
            Assert.That(loaded.Language, Is.EqualTo(GameLanguage.SimplifiedChinese));
        }

        [Test]
        public void Values_ClampToDocumentedSafeBounds()
        {
            var settings = new SettingsModel();
            Set(settings, "MasterVolume", -2f);
            Set(settings, "MusicVolume", 2f);
            Set(settings, "SfxVolume", -1f);
            Set(settings, "VoiceVolume", 3f);
            Set(settings, "SubtitleScale", .1f);
            Set(settings, "SubtitleBackgroundOpacity", 3f);
            Set(settings, "InteractionTimeMultiplier", 4f);

            Assert.That(Float(settings, "MasterVolume"), Is.EqualTo(0f));
            Assert.That(Float(settings, "MusicVolume"), Is.EqualTo(1f));
            Assert.That(Float(settings, "SfxVolume"), Is.EqualTo(0f));
            Assert.That(Float(settings, "VoiceVolume"), Is.EqualTo(1f));
            Assert.That(Float(settings, "SubtitleScale"), Is.EqualTo(.75f));
            Assert.That(Float(settings, "SubtitleBackgroundOpacity"), Is.EqualTo(1f));
            Assert.That(Float(settings, "InteractionTimeMultiplier"), Is.EqualTo(1.5f));
        }

        [Test]
        public void Load_CorruptFileFallsBackToDefaultsWithoutReplacingTheLastFile()
        {
            Directory.CreateDirectory(directoryPath);
            File.WriteAllText(settingsPath, "{ not-valid-json");

            var loaded = InvokeStatic<SettingsModel>("Load", settingsPath);

            Assert.That(Float(loaded, "MasterVolume"), Is.EqualTo(.8f));
            Assert.That(Float(loaded, "SubtitleScale"), Is.EqualTo(1f));
            Assert.That(File.ReadAllText(settingsPath), Is.EqualTo("{ not-valid-json"));
        }

        private static float Float(SettingsModel settings, string name) => (float)Property(name).GetValue(settings);
        private static bool Bool(SettingsModel settings, string name) => (bool)Property(name).GetValue(settings);
        private static void Set(SettingsModel settings, string name, object value) => Property(name).SetValue(settings, value);

        private static PropertyInfo Property(string name)
        {
            var property = typeof(SettingsModel).GetProperty(name);
            Assert.That(property, Is.Not.Null, $"SettingsModel requires {name}.");
            return property;
        }

        private static T InvokeStatic<T>(string name, params object[] arguments)
        {
            var method = typeof(SettingsModel).GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"SettingsModel requires static {name}.");
            return (T)method.Invoke(null, arguments);
        }
    }
}
