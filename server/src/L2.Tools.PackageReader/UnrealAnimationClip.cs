namespace L2.Tools.PackageReader;

public sealed record UnrealAnimationClip(
    string Name,
    int FrameCount,
    float FrameRate,
    IReadOnlyList<string> Groups,
    IReadOnlyList<UnrealAnimationTrack> Tracks,
    IReadOnlyList<UnrealAnimationNotify> Notifies)
{
    public float DurationSeconds => FrameRate > 0 ? FrameCount / FrameRate : 0;
}
