using NUnit.Framework;
using UnityEditor;
using UnityEngine.Audio;
using Northbound.Cinematics;
using UnityEngine;
using System.IO;
using System.Linq;
using System;
using System.Reflection;

namespace Northbound.Tests
{
    public sealed class AudioMixerTests
    {
        [Test]
        public void NorthboundMixer_ExposesRequiredGroupsAndSnapshots()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Northbound/Audio/NorthboundMixer.mixer");
            Assert.That(mixer, Is.Not.Null);
            Assert.That(mixer.FindMatchingGroups("Master/Music"), Is.Not.Empty);
            Assert.That(mixer.FindMatchingGroups("Master/SFX"), Is.Not.Empty);
            Assert.That(mixer.FindMatchingGroups("Master/Voice"), Is.Not.Empty);
            Assert.That(mixer.FindSnapshot("Normal"), Is.Not.Null);
            Assert.That(mixer.FindSnapshot("Cinematic"), Is.Not.Null);
            Assert.That(mixer.FindSnapshot("Pause"), Is.Not.Null);
        }

        [Test]
        public void EveryCinematicAsset_UsesTheAuthoredCinematicAndGameplaySnapshots()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Northbound/Audio/NorthboundMixer.mixer");
            var cinematicSnapshot = mixer.FindSnapshot("Cinematic");
            var gameplaySnapshot = mixer.FindSnapshot("Normal");
            var catalog = AssetDatabase.LoadAssetAtPath<CinematicCatalog>("Assets/Northbound/Data/Cinematics/CinematicCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.All, Has.Length.EqualTo(6));
            foreach (var cinematic in catalog.All)
            {
                Assert.That(cinematic.cinematicAudioSnapshot, Is.SameAs(cinematicSnapshot), cinematic.id);
                Assert.That(cinematic.gameplayAudioSnapshot, Is.SameAs(gameplaySnapshot), cinematic.id);
            }
        }

        [Test]
        public void DialogueReactionAudio_RoutesThroughVoiceGroup()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Northbound/Audio/NorthboundMixer.mixer");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Northbound/Prefabs/UI/DialogueView.prefab");
            var voice = mixer.FindMatchingGroups("Master/Voice")[0];

            Assert.That(prefab.GetComponent<AudioSource>().outputAudioMixerGroup, Is.SameAs(voice));
        }

        [Test]
        public void NormalCinematicAndPauseSnapshots_SerializeDistinctMixValues()
        {
            var yaml = File.ReadAllText("Assets/Northbound/Audio/NorthboundMixer.mixer");
            Assert.That(yaml, Does.Not.Contain("m_FloatValues: {}"), "Snapshots must not be inert placeholders.");
            Assert.That(yaml, Does.Contain("11111111111111111111111111111111: -6"), "Cinematic must attenuate Music.");
            Assert.That(yaml, Does.Contain("33333333333333333333333333333333: -18"), "Pause must attenuate SFX.");
            Assert.That(yaml, Does.Contain("55555555555555555555555555555555: 0"), "Voice stays intelligible.");
        }

        [Test]
        public void EveryMixerGroup_OwnsOneAttenuationUnitBoundToItsExposedVolume()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Northbound/Audio/NorthboundMixer.mixer");

            foreach (var contract in new[]
            {
                (path: "Master", group: "Master", parameter: "MasterVolume"),
                (path: "Master/Music", group: "Music", parameter: "MusicVolume"),
                (path: "Master/SFX", group: "SFX", parameter: "SFXVolume"),
                (path: "Master/Voice", group: "Voice", parameter: "VoiceVolume")
            })
            {
                var groups = mixer.FindMatchingGroups(contract.path).Where(candidate => candidate.name == contract.group).ToArray();
                Assert.That(groups, Has.Length.EqualTo(1), contract.path);
                var groupData = new SerializedObject(groups.Single());
                var effects = groupData.FindProperty("m_Effects");
                Assert.That(effects, Is.Not.Null, contract.path);
                Assert.That(effects.arraySize, Is.EqualTo(1), contract.path + " must own exactly one attenuation effect.");
                var effect = effects.GetArrayElementAtIndex(0).objectReferenceValue;
                Assert.That(effect, Is.Not.Null, contract.path);
                Assert.That(new SerializedObject(effect).FindProperty("m_EffectName").stringValue,
                    Is.EqualTo("Attenuation"), contract.path);
                Assert.That((bool)groups.Single().GetType()
                    .GetMethod("HasAttenuation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Invoke(groups.Single(), null), Is.True, contract.path + " must recognize its attenuation unit.");

                var volumeGuid = groups.Single().GetType()
                    .GetMethod("GetGUIDForVolume", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Invoke(groups.Single(), null).ToString();
                Assert.That(volumeGuid, Is.Not.Empty, contract.path);
                Assert.That(ExposedGuid(mixer, contract.parameter), Is.EqualTo(volumeGuid),
                    contract.parameter + " must address that group's attenuation volume.");
            }
        }

        private static string ExposedGuid(AudioMixer mixer, string parameter)
        {
            var mixerType = mixer.GetType();
            var exposed = (Array)mixerType.GetProperty("exposedParameters", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(mixer);
            foreach (var item in exposed)
            {
                var itemType = item.GetType();
                if ((string)itemType.GetField("name").GetValue(item) == parameter)
                {
                    return itemType.GetField("guid").GetValue(item).ToString();
                }
            }
            Assert.Fail("Missing exposed mixer parameter " + parameter);
            return string.Empty;
        }
    }
}
