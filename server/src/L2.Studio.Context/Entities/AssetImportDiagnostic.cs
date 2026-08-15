using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

[Table("asset_import_diagnostics")]
public sealed class AssetImportDiagnostic
{
    [Key, Column("id")]
    public long Id { get; set; }
    [Column("run_id")]
    public Guid RunId { get; set; }
    [Column("work_item_id")]
    public Guid? WorkItemId { get; set; }
    [Column("severity"), MaxLength(16)]
    public required string Severity { get; set; }
    [Column("code"), MaxLength(128)]
    public required string Code { get; set; }
    [Column("stage"), MaxLength(64)]
    public required string Stage { get; set; }
    [Column("source_key"), MaxLength(256)]
    public string? SourceKey { get; set; }
    [Column("object_name"), MaxLength(512)]
    public string? ObjectName { get; set; }
    [Column("message"), MaxLength(4000)]
    public required string Message { get; set; }
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
    public AssetImportRun Run { get; set; } = null!;
    public AssetImportWorkItem? WorkItem { get; set; }
}
