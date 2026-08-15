using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

public sealed class AssetImportRun : ImportJob
{
    [Column("trigger_type"), MaxLength(32)]
    public required string TriggerType { get; set; }
    [Column("requested_source_key"), MaxLength(256)]
    public string? RequestedSourceKey { get; set; }
    [Column("normalized_requested_source_key"), MaxLength(256)]
    public string? NormalizedRequestedSourceKey { get; set; }
    [Column("force")]
    public bool Force { get; set; }
    [Column("discovery_finished_at")]
    public DateTimeOffset? DiscoveryFinishedAt { get; set; }
    [Column("discovered_file_count")]
    public int DiscoveredFileCount { get; set; }
    [Column("completed_file_count")]
    public int CompletedFileCount { get; set; }
    [Column("succeeded_file_count")]
    public int SucceededFileCount { get; set; }
    [Column("warning_file_count")]
    public int WarningFileCount { get; set; }
    [Column("failed_file_count")]
    public int FailedFileCount { get; set; }
    [Column("reused_file_count")]
    public int ReusedFileCount { get; set; }
    public ICollection<AssetImportWorkItem> WorkItems { get; set; } = [];
    public ICollection<AssetImportDiagnostic> Diagnostics { get; set; } = [];
}
