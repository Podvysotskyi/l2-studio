namespace L2.Studio.Services;

internal sealed record TextureAnimationManifestEntry(
    IReadOnlyList<string> FrameUrls,
    float MinFrameRate,
    float MaxFrameRate);
