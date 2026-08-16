using System;
using System.IO;
using NUnit.Framework;
using Northbound.Narrative;
using UnityEngine;

public sealed class SaveGameServiceTests
{
    private string directoryPath;
    private SaveGameService service;

    [SetUp]
    public void SetUp()
    {
        directoryPath = Path.Combine(Application.temporaryCachePath, "northbound-save-tests-" + Guid.NewGuid().ToString("N"));
        service = new SaveGameService(Path.Combine(directoryPath, "northbound-save.json"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
        }
    }

    [Test]
    public void DefaultSavePath_UsesPersistentDataPath()
    {
        var defaultService = new SaveGameService();

        Assert.That(defaultService.SavePath, Is.EqualTo(Path.Combine(Application.persistentDataPath, "northbound-save.json")));
    }

    [Test]
    public void LoadOrNew_WhenNoSaveExists_ReturnsEmptyState()
    {
        var loaded = service.LoadOrNew();

        Assert.That(loaded.Has("attended_maya_exhibition"), Is.False);
        Assert.That(loaded.GetInt("bond_maya"), Is.EqualTo(0));
    }

    [Test]
    public void SaveThenLoad_RoundTripsFactsAndCounters()
    {
        var state = new NarrativeState();
        state.Set("attended_maya_exhibition", true);
        state.Add("bond_maya", 2);

        service.Save(state);
        var loaded = service.LoadOrNew();

        Assert.That(loaded.Has("attended_maya_exhibition"), Is.True);
        Assert.That(loaded.GetInt("bond_maya"), Is.EqualTo(2));
    }

    [Test]
    public void Save_ReplacesTheLiveFileAndLeavesNoTemporaryFile()
    {
        var first = new NarrativeState();
        first.Set("helped_noah", true);
        service.Save(first);

        var replacement = new NarrativeState();
        replacement.Set("attended_maya_exhibition", true);
        service.Save(replacement);
        var loaded = service.LoadOrNew();

        Assert.That(loaded.Has("helped_noah"), Is.False);
        Assert.That(loaded.Has("attended_maya_exhibition"), Is.True);
        Assert.That(File.Exists(service.SavePath + ".tmp"), Is.False);
    }

    [Test]
    public void Save_WhenTheSaveDirectoryCannotBeCreated_ReturnsFalseAndCleansTemporaryFile()
    {
        Directory.CreateDirectory(directoryPath);
        var blockedPath = Path.Combine(directoryPath, "blocked");
        File.WriteAllText(blockedPath, "not-a-directory");
        var blockedService = new SaveGameService(Path.Combine(blockedPath, "northbound-save.json"));

        var state = new NarrativeState();
        state.Set("helped_noah", true);

        Assert.That(blockedService.Save(state), Is.False);
        Assert.That(File.Exists(blockedService.SavePath + ".tmp"), Is.False);
    }

    [TestCase("{ not-json")]
    [TestCase("")]
    public void LoadOrNew_CorruptOrEmptyFileReturnsEmptyState(string contents)
    {
        Directory.CreateDirectory(directoryPath);
        File.WriteAllText(service.SavePath, contents);

        Assert.DoesNotThrow(() => service.LoadOrNew());
        var loaded = service.LoadOrNew();

        Assert.That(loaded.Has("attended_maya_exhibition"), Is.False);
        Assert.That(loaded.GetInt("bond_maya"), Is.EqualTo(0));
    }

    [Test]
    public void Delete_RemovesSaveAndTemporaryFilesSafely()
    {
        Directory.CreateDirectory(directoryPath);
        File.WriteAllText(service.SavePath, "old-save");
        File.WriteAllText(service.SavePath + ".tmp", "in-progress-save");

        Assert.DoesNotThrow(() => service.Delete());
        Assert.DoesNotThrow(() => service.Delete());

        Assert.That(File.Exists(service.SavePath), Is.False);
        Assert.That(File.Exists(service.SavePath + ".tmp"), Is.False);
    }
}
