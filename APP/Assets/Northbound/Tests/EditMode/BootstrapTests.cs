using NUnit.Framework;
using Northbound.Core;

public sealed class BootstrapTests
{
    [Test]
    public void SceneIds_AreStable()
    {
        Assert.That(SceneIds.Bootstrap, Is.EqualTo("Bootstrap"));
        Assert.That(SceneIds.Greybridge, Is.EqualTo("Greybridge"));
    }
}
