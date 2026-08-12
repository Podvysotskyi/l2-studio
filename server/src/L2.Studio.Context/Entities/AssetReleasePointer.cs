namespace L2.Studio.Context.Entities;

public sealed class AssetReleasePointer
{
    public string GameVersion { get; set; } = "interlude";
    public Guid? DesiredReleaseId { get; set; }
    public Guid? PublishedReleaseId { get; set; }
    public string Status { get; set; } = "inactive";
    public string? Error { get; set; }
    public DateTimeOffset? RequestedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public AssetRelease? DesiredRelease { get; set; }
    public AssetRelease? PublishedRelease { get; set; }
}
