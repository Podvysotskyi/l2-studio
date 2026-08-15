using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

[Table("asset_artifacts")]
public sealed class AssetArtifact
{
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }
    [Column("game_version"), MaxLength(32)]
    public string GameVersion { get; set; } = "interlude";
    [Column("kind"), MaxLength(64)]
    public required string Kind { get; set; }
    [Column("source_key"), MaxLength(256)]
    public required string SourceKey { get; set; }
    [Column("normalized_source_key"), MaxLength(256)]
    public required string NormalizedSourceKey { get; set; }
    [Column("source_hash"), MaxLength(64)]
    public required string SourceHash { get; set; }
    [Column("recipe_version"), MaxLength(128)]
    public required string RecipeVersion { get; set; }
    [Column("build_fingerprint"), MaxLength(64)]
    public required string BuildFingerprint { get; set; }
    [Column("content_hash"), MaxLength(64)]
    public required string ContentHash { get; set; }
    [Column("output_root"), MaxLength(1024)]
    public required string OutputRoot { get; set; }
    [Column("schema_version")]
    public int SchemaVersion { get; set; }
    [Column("protocol")]
    public int? Protocol { get; set; }
    [Column("file_count")]
    public int FileCount { get; set; }
    [Column("size_bytes")]
    public long SizeBytes { get; set; }
    [Column("integrity_status"), MaxLength(32)]
    public required string IntegrityStatus { get; set; }
    [Column("last_verified_at")]
    public DateTimeOffset? LastVerifiedAt { get; set; }
    [Column("publishing_work_item_id")]
    public Guid PublishingWorkItemId { get; set; }
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
    public AssetImportWorkItem PublishingWorkItem { get; set; } = null!;
    public ICollection<AssetArtifactFile> Files { get; set; } = [];
    public ICollection<AssetArtifactDependency> Dependencies { get; set; } = [];
    public ICollection<AssetCatalogSource> Publications { get; set; } = [];
    public ICollection<AssetReleaseArtifact> Releases { get; set; } = [];
}
