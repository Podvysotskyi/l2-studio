namespace L2.Studio.Context.Entities;

public sealed class AssetReleaseEvent
{
    public long Id { get; set; }
    public Guid ReleaseId { get; set; }
    public required string Action { get; set; }
    public required string DetailsJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public AssetRelease Release { get; set; } = null!;
}
