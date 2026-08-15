using L2.Studio.Services;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class AnimationSkeletonCompatibilityTests
{
    [Fact]
    public void AcceptsExactAndReorderedSkeletons()
    {
        Assert.True(AnimationSkeletonCompatibility.Evaluate(
            ["root", "spine", "hand"], ["root", "spine", "hand"]).IsCompatible);
        Assert.True(AnimationSkeletonCompatibility.Evaluate(
            ["hand", "root", "spine"], ["root", "spine", "hand"]).IsCompatible);
    }

    [Fact]
    public void AcceptsTheNinetyFivePercentBoundary()
    {
        var animation = Enumerable.Range(0, 20).Select(index => $"bone-{index}").ToArray();
        var mesh = animation.Take(19).ToArray();

        var result = AnimationSkeletonCompatibility.Evaluate(mesh, animation);

        Assert.True(result.IsCompatible);
        Assert.Equal(19, result.MatchedBoneCount);
        Assert.Equal(20, result.AnimationBoneCount);
    }

    [Fact]
    public void RejectsSkeletonsBelowTheBoundaryAndEmptyAnimations()
    {
        var animation = Enumerable.Range(0, 20).Select(index => $"bone-{index}").ToArray();

        Assert.False(AnimationSkeletonCompatibility.Evaluate(animation.Take(18), animation).IsCompatible);
        Assert.False(AnimationSkeletonCompatibility.Evaluate(["root"], []).IsCompatible);
    }

    [Fact]
    public void UsesExactRuntimeNamesAndCountsDuplicateAnimationTargets()
    {
        var result = AnimationSkeletonCompatibility.Evaluate(
            ["root", "Hand"], ["root", "Hand", "Hand"]);

        Assert.True(result.IsCompatible);
        Assert.Equal(3, result.MatchedBoneCount);
        Assert.False(AnimationSkeletonCompatibility.Evaluate(
            ["root", "hand"], ["root", "Hand"]).IsCompatible);
    }
}
