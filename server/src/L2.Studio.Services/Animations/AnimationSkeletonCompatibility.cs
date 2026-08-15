namespace L2.Studio.Services;

internal static class AnimationSkeletonCompatibility
{
    internal const int MinimumMatchPercentage = 95;

    internal static (bool IsCompatible, int MatchedBoneCount, int AnimationBoneCount) Evaluate(
        IEnumerable<string> meshBoneNames,
        IEnumerable<string> animationBoneNames)
    {
        var meshNames = meshBoneNames.ToHashSet(StringComparer.Ordinal);
        var animationNames = animationBoneNames.ToArray();
        var matched = animationNames.Count(meshNames.Contains);
        var compatible = animationNames.Length > 0 &&
            (long)matched * 100 >= (long)animationNames.Length * MinimumMatchPercentage;
        return (compatible, matched, animationNames.Length);
    }
}
