using L2.Studio.Repositories.Interfaces.Models;
using Xunit;

namespace L2.Studio.Repositories.Interfaces.Tests;

public sealed class AssetArtifactFingerprintTests
{
    [Theory]
    [InlineData("textures", "textures:10:121")]
    [InlineData("staticmeshes", "staticmeshes:12:112")]
    [InlineData("sounds", "sounds:3:111")]
    [InlineData("music", "music:5")]
    [InlineData("maps", "maps:19:111")]
    [InlineData("scenes", "scenes:18:111")]
    [InlineData("mappreviews", "mappreviews:4:8")]
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
