namespace L2.Tools.StaticMeshConverter;

public sealed record StaticMeshTextureAnimation(
    IReadOnlyList<string> FrameUrls,
    float FrameRate);
