using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

[Table("asset_artifact_files")]
public sealed class AssetArtifactFile
{
    [Key, Column("id")]
    public long Id { get; set; }
    [Column("artifact_id")]
    public Guid ArtifactId { get; set; }
    [Column("relative_path"), MaxLength(1024)]
    public required string RelativePath { get; set; }
    [Column("public_path"), MaxLength(2048)]
    public required string PublicPath { get; set; }
    [Column("role"), MaxLength(64)]
    public required string Role { get; set; }
    [Column("media_type"), MaxLength(128)]
    public required string MediaType { get; set; }
    [Column("size_bytes")]
    public long SizeBytes { get; set; }
    [Column("sha256"), MaxLength(64)]
    public required string Sha256 { get; set; }
    public AssetArtifact Artifact { get; set; } = null!;
}
