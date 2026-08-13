using L2.Studio.Services;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class AssetArtifactPathTests
{
    [Theory]
    [InlineData("System Textures/UI.utx", "System Textures/UI/fingerprint")]
    [InlineData("Textures/World.utx", "Textures/World/fingerprint")]
    [InlineData("Music/Theme.ogg", "Music/Theme/fingerprint")]
    [InlineData("Sounds/Ambience.uax", "Sounds/Ambience/fingerprint")]
    [InlineData("Meshes/Object.usx", "Meshes/Object/fingerprint")]
    [InlineData("Maps/17_25.unr", "Maps/17_25/fingerprint")]
    [InlineData("Maps/Lobby.unr", "Maps/Lobby/fingerprint")]
    public void UsesTheSourceHierarchyWithoutAnImportKindFolder(string sourceKey, string expected)
    {
        var actual = AssetImportJobProcessor.ArtifactRelativePath(sourceKey, "fingerprint")
            .Replace('\\', '/');

        Assert.Equal(expected, actual);
    }
}
