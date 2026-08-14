namespace L2.Studio.Services;

internal sealed record AnimationClipManifestEntry(
    string Name,
    int FrameCount,
    float FrameRate,
    float DurationSeconds,
    IReadOnlyList<string> Groups,
    IReadOnlyList<AnimationNotifyManifestEntry> Notifies);
