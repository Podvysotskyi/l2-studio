using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

[Table("asset_artifact_dependencies")]
public sealed class AssetArtifactDependency
{
    [Key, Column("id")]
    public long Id { get; set; }
    [Column("artifact_id")]
    public Guid ArtifactId { get; set; }
    [Column("kind"), MaxLength(64)]
    public required string Kind { get; set; }
    [Column("dependency_key"), MaxLength(512)]
    public required string DependencyKey { get; set; }
    [Column("resolved_artifact_id")]
    public Guid? ResolvedArtifactId { get; set; }
    [Column("resolved_source_key"), MaxLength(256)]
    public string? ResolvedSourceKey { get; set; }
    [Column("build_fingerprint"), MaxLength(64)]
    public string? BuildFingerprint { get; set; }
    [Column("is_resolved")]
    public bool IsResolved { get; set; }
    public AssetArtifact Artifact { get; set; } = null!;
    public AssetArtifact? ResolvedArtifact { get; set; }
}
