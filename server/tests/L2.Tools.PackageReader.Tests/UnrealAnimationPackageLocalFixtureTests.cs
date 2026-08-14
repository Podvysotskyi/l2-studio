using L2.Tools.PackageReader;
using Xunit;

namespace L2.Tools.PackageReader.Tests;

public sealed class UnrealAnimationPackageLocalFixtureTests
{
    [Fact]
    public void ReadsTheChronicleOneAnimationCorpusWhenAvailable()
    {
        var root = Environment.GetEnvironmentVariable("L2_C1_ANIMATIONS_PATH");
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return;
        var packages = Directory.EnumerateFiles(root, "*.ukx")
            .OrderBy(Path.GetFileName)
            .Select(path => new UnrealPackageReader(LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(path)))
                .ReadAnimationPackage())
            .ToArray();
        var totals = packages
            .Aggregate(
                (Meshes: 0, Animations: 0, VertexMeshes: 0),
                (total, package) => (
                    total.Meshes + package.SkeletalMeshes.Count,
                    total.Animations + package.AnimationSets.Count,
                    total.VertexMeshes + package.UnsupportedVertexMeshCount));
        Assert.Equal(1380, totals.Meshes);
        Assert.Equal(279, totals.Animations);
        Assert.Equal(2, totals.VertexMeshes);
        Assert.True(packages.Sum(package => package.SkeletalMeshes.Count(mesh => mesh.Error is null)) > 1_000);
        Assert.All(packages.SelectMany(package => package.AnimationSets), animation => Assert.NotEmpty(animation.Clips));
        Assert.True(packages.Sum(package => package.AnimationSets.Sum(animation => animation.Clips.Count)) > 1_000);
    }
}
