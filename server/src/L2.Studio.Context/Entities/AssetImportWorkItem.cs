namespace L2.Studio.Context.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public sealed class AssetImportWorkItem
{
    public string GameVersion { get; set; } = "interlude";
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public required string ImportKind { get; set; }
    public required string SourceKey { get; set; }
    public required string NormalizedSourceKey { get; set; }
    public required string SourcePath { get; set; }
    public string? SourceHash { get; set; }
    public required string Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public int TotalResourceCount { get; set; }
    public int ProcessedResourceCount { get; set; }
    public int SkippedResourceCount { get; set; }
    public int WarningCount { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset? UnpublishedAt { get; set; }
    public AssetImportRun Run { get; set; } = null!;
    public ICollection<AssetImportDiagnostic> Diagnostics { get; set; } = [];

    [NotMapped]
    public string? ConversionSourcePath { get; set; }

    // Transitional aliases used by the converter implementation while it is split into per-file handlers.
    public string Kind { get => ImportKind; set => ImportKind = value; }
    public int TotalCount { get => TotalResourceCount; set => TotalResourceCount = value; }
    public int ProcessedCount { get => ProcessedResourceCount; set => ProcessedResourceCount = value; }
    public int SkippedCount { get => SkippedResourceCount; set => SkippedResourceCount = value; }
    public string WarningsJson { get; set; } = "[]";
}
