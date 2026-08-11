namespace L2.Studio.Context.Entities;

public sealed class AssetImportRun
{
    public Guid Id { get; set; }
    public required string Kind { get; set; }
    public required string TriggerType { get; set; }
    public required string Status { get; set; }
    public string? RequestedSourceKey { get; set; }
    public string? NormalizedRequestedSourceKey { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? DiscoveryFinishedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public int DiscoveredFileCount { get; set; }
    public int CompletedFileCount { get; set; }
    public int SucceededFileCount { get; set; }
    public int WarningFileCount { get; set; }
    public int FailedFileCount { get; set; }
    public string? Error { get; set; }
    public ICollection<AssetImportWorkItem> WorkItems { get; set; } = [];
    public ICollection<AssetImportDiagnostic> Diagnostics { get; set; } = [];
}
