using L2.Studio.Repositories.Interfaces.Models;
using Xunit;

namespace L2.Studio.Repositories.Interfaces.Tests;

public sealed class AssetImportSourcePathsAnimationTests
{
    [Fact]
    public void MapsAnimationsToUkxAndLimitsImportsToChronicleOne()
    {
        Assert.Equal(".ukx", AssetImportSourcePaths.ExpectedExtension(AssetImportJobValues.Animations));
        Assert.True(AssetImportSourcePaths.MatchesKind(AssetImportJobValues.Animations, "Animations/Warrior.UKX"));
        AssetImportSourcePaths.RequireSupportedVersion(AssetImportJobValues.Animations, "c1");
        var exception = Assert.Throws<ArgumentException>(() =>
            AssetImportSourcePaths.RequireSupportedVersion(AssetImportJobValues.Animations, "interlude"));
        Assert.Contains("Chronicle 1", exception.Message, StringComparison.Ordinal);
    }
}
