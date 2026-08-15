using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

[Table("asset_catalog_sources")]
public sealed class AssetCatalogSource
{
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }
    [Column("catalog_id")]
    public Guid CatalogId { get; set; }
    [Column("artifact_id")]
    public Guid ArtifactId { get; set; }
    [Column("publishing_work_item_id")]
    public Guid PublishingWorkItemId { get; set; }
    [Column("source_key"), MaxLength(256)]
    public required string SourceKey { get; set; }
    [Column("normalized_source_key"), MaxLength(256)]
    public required string NormalizedSourceKey { get; set; }
    [Column("source_hash"), MaxLength(64)]
    public required string SourceHash { get; set; }
    [Column("artifact_fingerprint"), MaxLength(64)]
    public string? ArtifactFingerprint { get; set; }
    [Column("output_root"), MaxLength(1024)]
    public required string OutputRoot { get; set; }
    [Column("metadata_json")]
    public required string MetadataJson { get; set; }
    [Column("referenced_output_roots_json")]
    public required string ReferencedOutputRootsJson { get; set; }
    [Column("published_at")]
    public DateTimeOffset PublishedAt { get; set; }
    [Column("is_stale")]
    public bool IsStale { get; set; }
    [Column("stale_at")]
    public DateTimeOffset? StaleAt { get; set; }
    [Column("stale_reasons_json")]
    public string StaleReasonsJson { get; set; } = "[]";
    public AssetCatalog Catalog { get; set; } = null!;
    public AssetArtifact Artifact { get; set; } = null!;
    public ICollection<AssetCatalogGroup> Groups { get; set; } = [];
    public ICollection<AssetCatalogItem> Items { get; set; } = [];
    public ICollection<AssetCatalogSourceDependency> Dependencies { get; set; } = [];
}
