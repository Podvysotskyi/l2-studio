using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("asset_release_artifacts")]
[PrimaryKey(nameof(ReleaseId), nameof(ArtifactId))]
public sealed class AssetReleaseArtifact
{
    [Column("release_id")]
    public Guid ReleaseId { get; set; }
    [Column("artifact_id")]
    public Guid ArtifactId { get; set; }
    [Column("is_root")]
    public bool IsRoot { get; set; }
    public AssetRelease Release { get; set; } = null!;
    public AssetArtifact Artifact { get; set; } = null!;
}
