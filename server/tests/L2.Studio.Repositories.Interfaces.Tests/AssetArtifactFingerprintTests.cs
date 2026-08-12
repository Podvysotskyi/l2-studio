using L2.Studio.Repositories.Interfaces.Models;
using Xunit;

namespace L2.Studio.Repositories.Interfaces.Tests;

public sealed class AssetArtifactFingerprintTests
{
    [Theory]
    [InlineData("maps", "maps:14:111")]
    [InlineData("scenes", "scenes:13:111")]
    public void UsesCurrentManifestRecipeVersions(string kind, string expected)
    {
        Assert.Equal(expected, AssetArtifactFingerprint.RecipeVersion(kind));
    }

    [Fact]
    public void IsStableAcrossDependencyOrder()
    {
        var left = AssetArtifactFingerprint.Compute("maps", "source", [
            ("textures", "a.texture", "first"),
            ("staticmeshes", "b.mesh", "second")
        ]);
        var right = AssetArtifactFingerprint.Compute("maps", "source", [
            ("staticmeshes", "b.mesh", "second"),
            ("textures", "a.texture", "first")
        ]);

        Assert.Equal(left, right);
    }

    [Fact]
    public void ChangesForSourceDependencyOrPipelineChanges()
    {
        var baseline = AssetArtifactFingerprint.Compute("maps", "source", [
            ("textures", "a.texture", "first")
        ]);

        Assert.NotEqual(baseline, AssetArtifactFingerprint.Compute("maps", "changed", [
            ("textures", "a.texture", "first")
        ]));
        Assert.NotEqual(baseline, AssetArtifactFingerprint.Compute("maps", "source", [
            ("textures", "a.texture", "changed")
        ]));
        Assert.NotEqual(baseline, AssetArtifactFingerprint.Compute("scenes", "source", [
            ("textures", "a.texture", "first")
        ]));
    }
}
