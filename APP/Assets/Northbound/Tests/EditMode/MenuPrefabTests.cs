using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Northbound.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Northbound.Tests
{
    public sealed class MenuPrefabTests
    {
        private const string TitlePath = "Assets/Northbound/Prefabs/UI/TitleMenu.prefab";
        private const string PausePath = "Assets/Northbound/Prefabs/UI/PauseMenu.prefab";
        private const string SettingsPath = "Assets/Northbound/Prefabs/UI/SettingsMenu.prefab";
        private const string CreditsPath = "Assets/Northbound/Prefabs/UI/Credits.prefab";
        private const string MixerPath = "Assets/Northbound/Audio/NorthboundMixer.mixer";

        [Test]
        public void MenuPrefabs_ContainTheRequiredKeyboardReachableControls()
        {
            AssertRootScale(TitlePath);
            AssertRootScale(PausePath);
            AssertRootScale(SettingsPath);
            AssertRootScale(CreditsPath);
            AssertButtons(TitlePath, "New Game", "Continue", "Settings", "Credits", "Confirm New Game", "Cancel New Game");
            AssertButtons(PausePath, "Resume", "Settings", "Return to Title");
            AssertButtons(SettingsPath, "Apply", "Back");
            AssertButtons(CreditsPath, "Return to Title");

            var settings = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPath);
            CollectionAssert.AreEquivalent(new[]
            {
                "Master Volume", "Music Volume", "SFX Volume", "Voice Volume",
                "Subtitle Scale", "Subtitle Background Opacity", "Interaction Time Multiplier"
            }, settings.GetComponentsInChildren<Slider>(true).Select(control => control.name));
            CollectionAssert.AreEquivalent(new[] { "Reduced Motion", "Skip Minigames" },
                settings.GetComponentsInChildren<Toggle>(true).Select(control => control.name));
            Assert.That(settings.GetComponentsInChildren<Selectable>(true).All(control => control.navigation.mode != Navigation.Mode.None), Is.True);
            var confirmation = AssetDatabase.LoadAssetAtPath<GameObject>(TitlePath).transform.Find("New Game Confirmation");
            Assert.That(confirmation, Is.Not.Null);
            Assert.That(confirmation.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void SettingsMenu_ApplyPersistsEveryControlAndUpdatesMixerParameters()
        {
            var path = Path.Combine(Application.temporaryCachePath, "northbound-settings-menu-" + Guid.NewGuid().ToString("N") + ".json");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPath);
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                var controller = instance.GetComponents<Component>().SingleOrDefault(component => component != null && component.GetType().Name == "SettingsMenuController");
                Assert.That(controller, Is.Not.Null, "SettingsMenu prefab requires its production controller.");
                var initialize = controller.GetType().GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance);
                Assert.That(initialize, Is.Not.Null);
                var model = new SettingsModel();
                initialize.Invoke(controller, new object[] { model, mixer, path });

                var language = Button(instance, "Language");
                Assert.That(language.GetComponentInChildren<Text>().text, Does.Contain("English"));
                language.onClick.Invoke();
                Assert.That(model.Language, Is.EqualTo(GameLanguage.SimplifiedChinese));
                Assert.That(language.GetComponentInChildren<Text>().text, Does.Contain("中文"));
                Assert.That(instance.transform.Find("Heading").GetComponent<Text>().text, Is.EqualTo("设置"));
                language.onClick.Invoke();
                Assert.That(model.Language, Is.EqualTo(GameLanguage.English));
                Assert.That(language.GetComponentInChildren<Text>().text, Is.EqualTo("Language: English / 中文"));
                Assert.That(instance.transform.Find("Heading").GetComponent<Text>().text, Is.EqualTo("Settings"));
                language.onClick.Invoke();

                Slider(instance, "Master Volume").value = .25f;
                Slider(instance, "Music Volume").value = .5f;
                Slider(instance, "SFX Volume").value = .75f;
                Slider(instance, "Voice Volume").value = 1f;
                Slider(instance, "Subtitle Scale").value = 1.25f;
                Slider(instance, "Subtitle Background Opacity").value = .35f;
                Slider(instance, "Interaction Time Multiplier").value = 1.4f;
                Toggle(instance, "Reduced Motion").isOn = true;
                Toggle(instance, "Skip Minigames").isOn = true;
                Button(instance, "Apply").onClick.Invoke();

                Assert.That(model.MasterVolume, Is.EqualTo(.25f));
                Assert.That(model.MusicVolume, Is.EqualTo(.5f));
                Assert.That(model.SfxVolume, Is.EqualTo(.75f));
                Assert.That(model.VoiceVolume, Is.EqualTo(1f));
                Assert.That(model.SubtitleScale, Is.EqualTo(1.25f));
                Assert.That(model.SubtitleBackgroundOpacity, Is.EqualTo(.35f));
                Assert.That(model.ReducedMotion, Is.True);
                Assert.That(model.SkipMinigames, Is.True);
                Assert.That(model.InteractionTimeMultiplier, Is.EqualTo(1.4f));

                var persisted = SettingsModel.Load(path);
                Assert.That(persisted.MasterVolume, Is.EqualTo(.25f));
                Assert.That(persisted.MusicVolume, Is.EqualTo(.5f));
                Assert.That(persisted.SfxVolume, Is.EqualTo(.75f));
                Assert.That(persisted.VoiceVolume, Is.EqualTo(1f));
                Assert.That(persisted.SubtitleScale, Is.EqualTo(1.25f));
                Assert.That(persisted.SubtitleBackgroundOpacity, Is.EqualTo(.35f));
                Assert.That(persisted.ReducedMotion, Is.True);
                Assert.That(persisted.SkipMinigames, Is.True);
                Assert.That(persisted.InteractionTimeMultiplier, Is.EqualTo(1.4f));
                Assert.That(persisted.Language, Is.EqualTo(GameLanguage.SimplifiedChinese));

            }
            finally
            {
                GameText.Use(GameLanguage.English);
                UnityEngine.Object.DestroyImmediate(instance);
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static void AssertButtons(string path, params string[] expected)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            CollectionAssert.AreEquivalent(expected, prefab.GetComponentsInChildren<Button>(true).Select(button => button.name), path);
        }

        private static void AssertRootScale(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one), path + " must render at authored size.");
        }

        private static Slider Slider(GameObject root, string name) => root.GetComponentsInChildren<Slider>(true).Single(control => control.name == name);
        private static Toggle Toggle(GameObject root, string name) => root.GetComponentsInChildren<Toggle>(true).Single(control => control.name == name);
        private static Button Button(GameObject root, string name) => root.GetComponentsInChildren<Button>(true).Single(control => control.name == name);
    }
}
