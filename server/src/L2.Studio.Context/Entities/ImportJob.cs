using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

[Table("import_jobs")]
public abstract class ImportJob
{
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }
    [Column("game_version"), MaxLength(32)]
    public string GameVersion { get; set; } = "interlude";
    [Column("category"), MaxLength(16)]
    public string Category { get; set; } = null!;
    [Column("kind"), MaxLength(64)]
    public required string Kind { get; set; }
    [Column("status"), MaxLength(32)]
    public required string Status { get; set; }
    [Column("requested_at")]
    public DateTimeOffset RequestedAt { get; set; }
    [Column("started_at")]
    public DateTimeOffset? StartedAt { get; set; }
    [Column("finished_at")]
    public DateTimeOffset? FinishedAt { get; set; }
    [Column("last_heartbeat_at")]
    public DateTimeOffset? LastHeartbeatAt { get; set; }
    [Column("error"), MaxLength(4000)]
    public string? Error { get; set; }
}
