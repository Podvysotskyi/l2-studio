using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

[Table("asset_release_events")]
public sealed class AssetReleaseEvent
{
    [Key, Column("id")]
    public long Id { get; set; }
    [Column("release_id")]
    public Guid ReleaseId { get; set; }
    [Column("action"), MaxLength(64)]
    public required string Action { get; set; }
    [Column("details_json")]
    public required string DetailsJson { get; set; }
    [Column("occurred_at")]
    public DateTimeOffset OccurredAt { get; set; }
    public AssetRelease Release { get; set; } = null!;
}
