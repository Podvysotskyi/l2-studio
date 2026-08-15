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

    [Fact]
    public async Task PromoteCreatesARequiredDestinationParentDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"l2-studio-{Guid.NewGuid():N}");
        var stagingPath = Path.Combine(root, "work", "job");
        var finalPath = Path.Combine(root, "public", "versions", "c1", "System", "npcgrp", "fingerprint");
        Directory.CreateDirectory(stagingPath);
        await File.WriteAllTextAsync(Path.Combine(stagingPath, "manifest.json"), "{}");

        try
        {
            AssetImportJobProcessor.Promote(stagingPath, finalPath);

            Assert.False(Directory.Exists(stagingPath));
            Assert.Equal("{}", await File.ReadAllTextAsync(Path.Combine(finalPath, "manifest.json")));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
