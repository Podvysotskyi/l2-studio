using L2.Studio.Repositories.Interfaces.Models;
using Xunit;

namespace L2.Studio.Repositories.Interfaces.Tests;

public sealed class AssetImportSourcePathsNpcAppearanceTests
{
    [Theory]
    [InlineData("system/npcgrp.txt", true)]
    [InlineData("SYSTEM/NPCGRP.TXT", true)]
    [InlineData("system/npcname-e.txt", false)]
    [InlineData("data/npcgrp.txt", false)]
    public void MatchesOnlySystemNpcGrpIgnoringCase(string path, bool expected) =>
        Assert.Equal(expected, AssetImportSourcePaths.MatchesKind(AssetImportJobValues.NpcAppearances, path));

    [Fact]
    public void LimitsNpcAppearancesToChronicleOne()
    {
        Assert.Equal(".txt", AssetImportSourcePaths.ExpectedExtension(AssetImportJobValues.NpcAppearances));
        AssetImportSourcePaths.RequireSupportedVersion(AssetImportJobValues.NpcAppearances, "c1");
        Assert.Throws<ArgumentException>(() =>
            AssetImportSourcePaths.RequireSupportedVersion(AssetImportJobValues.NpcAppearances, "interlude"));
    }
}
