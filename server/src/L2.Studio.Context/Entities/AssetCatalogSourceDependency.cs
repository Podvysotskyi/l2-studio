using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

[Table("asset_catalog_source_dependencies")]
public sealed class AssetCatalogSourceDependency
{
    [Key, Column("id")]
    public long Id { get; set; }
    [Column("source_id")]
    public Guid SourceId { get; set; }
    [Column("kind"), MaxLength(64)]
    public required string Kind { get; set; }
    [Column("dependency_key"), MaxLength(512)]
    public required string DependencyKey { get; set; }
    [Column("resolved_source_key"), MaxLength(256)]
    public string? ResolvedSourceKey { get; set; }
    [Column("artifact_fingerprint"), MaxLength(64)]
    public string? ArtifactFingerprint { get; set; }
    [Column("is_resolved")]
    public bool IsResolved { get; set; }
    public AssetCatalogSource Source { get; set; } = null!;
}
