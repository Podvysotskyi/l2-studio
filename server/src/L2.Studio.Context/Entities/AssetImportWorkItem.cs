using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

[Table("asset_import_work_items")]
public sealed class AssetImportWorkItem
{
    [Column("game_version"), MaxLength(32)]
    public string GameVersion { get; set; } = "interlude";
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }
    [Column("run_id")]
    public Guid RunId { get; set; }
    [Column("import_kind"), MaxLength(64)]
    public required string ImportKind { get; set; }
    [Column("source_key"), MaxLength(256)]
    public required string SourceKey { get; set; }
    [Column("normalized_source_key"), MaxLength(256)]
    public required string NormalizedSourceKey { get; set; }
    [Column("source_path"), MaxLength(1024)]
    public required string SourcePath { get; set; }
    [Column("source_hash"), MaxLength(64)]
    public string? SourceHash { get; set; }
    [Column("artifact_fingerprint"), MaxLength(64)]
    public string? ArtifactFingerprint { get; set; }
    [Column("status"), MaxLength(32)]
    public required string Status { get; set; }
    [Column("attempt_count")]
    public int AttemptCount { get; set; }
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
    [Column("started_at")]
    public DateTimeOffset? StartedAt { get; set; }
    [Column("finished_at")]
    public DateTimeOffset? FinishedAt { get; set; }
    [Column("total_resource_count")]
    public int TotalResourceCount { get; set; }
    [Column("processed_resource_count")]
    public int ProcessedResourceCount { get; set; }
    [Column("skipped_resource_count")]
    public int SkippedResourceCount { get; set; }
    [Column("warning_count")]
    public int WarningCount { get; set; }
    [Column("error"), MaxLength(4000)]
    public string? Error { get; set; }
    [Column("unpublished_at")]
    public DateTimeOffset? UnpublishedAt { get; set; }
    [Column("last_heartbeat_at")]
    public DateTimeOffset? LastHeartbeatAt { get; set; }
    public AssetImportRun Run { get; set; } = null!;
    public ICollection<AssetImportDiagnostic> Diagnostics { get; set; } = [];

    [NotMapped]
    public string? ConversionSourcePath { get; set; }

    // Transitional aliases used by the converter implementation while it is split into per-file handlers.
    [NotMapped]
    public string Kind { get => ImportKind; set => ImportKind = value; }
    [NotMapped]
    public int TotalCount { get => TotalResourceCount; set => TotalResourceCount = value; }
    [NotMapped]
    public int ProcessedCount { get => ProcessedResourceCount; set => ProcessedResourceCount = value; }
    [NotMapped]
    public int SkippedCount { get => SkippedResourceCount; set => SkippedResourceCount = value; }
    [NotMapped]
    public string WarningsJson { get; set; } = "[]";
}
