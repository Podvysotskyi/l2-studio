using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

[Table("asset_release_pointers")]
public sealed class AssetReleasePointer
{
    [Key, Column("game_version"), MaxLength(32)]
    public string GameVersion { get; set; } = "interlude";
    [Column("desired_release_id")]
    public Guid? DesiredReleaseId { get; set; }
    [Column("published_release_id")]
    public Guid? PublishedReleaseId { get; set; }
    [Column("status"), MaxLength(32)]
    public string Status { get; set; } = "inactive";
    [Column("error"), MaxLength(4000)]
    public string? Error { get; set; }
    [Column("requested_at")]
    public DateTimeOffset? RequestedAt { get; set; }
    [Column("published_at")]
    public DateTimeOffset? PublishedAt { get; set; }
    public AssetRelease? DesiredRelease { get; set; }
    public AssetRelease? PublishedRelease { get; set; }
}
