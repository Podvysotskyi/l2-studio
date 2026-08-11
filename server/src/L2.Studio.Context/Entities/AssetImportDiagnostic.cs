namespace L2.Studio.Context.Entities;

public sealed class AssetImportDiagnostic
{
    public long Id { get; set; }
    public Guid RunId { get; set; }
    public Guid? WorkItemId { get; set; }
    public required string Severity { get; set; }
    public required string Code { get; set; }
    public required string Stage { get; set; }
    public string? SourceKey { get; set; }
    public string? ObjectName { get; set; }
    public required string Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public AssetImportRun Run { get; set; } = null!;
    public AssetImportWorkItem? WorkItem { get; set; }
}
