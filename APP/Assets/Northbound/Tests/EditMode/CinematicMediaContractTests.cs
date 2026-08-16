using NUnit.Framework;
using UnityEditor;
using UnityEngine.Video;

namespace Northbound.Tests
{
    public sealed class CinematicMediaContractTests
    {
        [TestCase("Assets/Northbound/Cinematics/Opening/opening_proxy.mp4", "7e64c50de94fc4c53aa5f5ba90f8dc26", 40d, 50d)]
        [TestCase("Assets/Northbound/Cinematics/Highlights/maya_proxy.mp4", "c63014df10e2f43eba1509661ac8e5e8", 45d, 60d)]
        [TestCase("Assets/Northbound/Cinematics/Highlights/noah_proxy.mp4", "6bdabb3697aca47a7bd0b5ee44a40605", 45d, 60d)]
        [TestCase("Assets/Northbound/Cinematics/Highlights/leo_proxy.mp4", "b20da990f02724b25ae683e441434150", 45d, 60d)]
        [TestCase("Assets/Northbound/Cinematics/Rooftop/rooftop_proxy.mp4", "ac844021f365c40549538217d8cf5f31", 60d, 75d)]
        [TestCase("Assets/Northbound/Cinematics/Finale/finale_proxy.mp4", "dcbb2c89781e44b0a86222495371be48", 30d, 45d)]
        public void StableReplacementSlot_PreservesGuidAndTechnicalImportContract(string path, string guid, double minimumSeconds, double maximumSeconds)
        {
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid), path);
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(path);
            Assert.That(clip, Is.Not.Null, path);
            Assert.That(clip.width, Is.EqualTo(1920), path);
            Assert.That(clip.height, Is.EqualTo(1080), path);
            Assert.That(clip.frameRate, Is.EqualTo(30d).Within(.01d), path);
            Assert.That(clip.length, Is.InRange(minimumSeconds, maximumSeconds), path);
            var importer = AssetImporter.GetAtPath(path) as VideoClipImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.importAudio, Is.True, path + " must retain its supplied AAC track.");
        }
    }
}
