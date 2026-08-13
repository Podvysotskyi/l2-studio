namespace L2.Studio.Context.Entities;

public sealed class NpcLookupImportRun
{
    public Guid Id { get; set; }
    public required string GameVersion { get; set; }
    public required string Kind { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public int TotalCount { get; set; }
    public int InsertedCount { get; set; }
    public int ExistingCount { get; set; }
    public string? Error { get; set; }
}
