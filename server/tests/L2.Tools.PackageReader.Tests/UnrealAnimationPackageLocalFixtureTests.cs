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
        AssertValidSkinWeights(packages);
    }

    private static void AssertValidSkinWeights(IEnumerable<UnrealAnimationPackage> packages)
    {
        foreach (var mesh in packages.SelectMany(package => package.SkeletalMeshes).Where(mesh => mesh.Error is null))
        {
            foreach (var weight in mesh.Weights)
            {
                var values = new[] { weight.Weights.X, weight.Weights.Y, weight.Weights.Z, weight.Weights.W };
                Assert.InRange(values.Sum(), 0.99999f, 1.00001f);
                var joints = new[] { weight.Bone0, weight.Bone1, weight.Bone2, weight.Bone3 };
                var activeJoints = joints.Where((_, index) => values[index] > 0).ToArray();
                Assert.Equal(activeJoints.Length, activeJoints.Distinct().Count());
            }
        }
    }
}
