using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

[Table("asset_releases")]
public sealed class AssetRelease
{
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }
    [Column("game_version"), MaxLength(32)]
    public string GameVersion { get; set; } = "interlude";
    [Column("name"), MaxLength(120)]
    public required string Name { get; set; }
    [Column("notes"), MaxLength(4000)]
    public string? Notes { get; set; }
    [Column("status"), MaxLength(32)]
    public string Status { get; set; } = "draft";
    [Column("snapshot_hash"), MaxLength(64)]
    public required string SnapshotHash { get; set; }
    [Column("validation_status"), MaxLength(32)]
    public string ValidationStatus { get; set; } = "not_validated";
    [Column("validation_issues_json")]
    public string ValidationIssuesJson { get; set; } = "[]";
    [Column("validated_snapshot_hash"), MaxLength(64)]
    public string? ValidatedSnapshotHash { get; set; }
    [Column("validation_requested_at")]
    public DateTimeOffset? ValidationRequestedAt { get; set; }
    [Column("validated_at")]
    public DateTimeOffset? ValidatedAt { get; set; }
    [Column("manifest_path"), MaxLength(1024)]
    public string? ManifestPath { get; set; }
    [Column("manifest_hash"), MaxLength(64)]
    public string? ManifestHash { get; set; }
    [Column("login_scene_file_id")]
    public long? LoginSceneFileId { get; set; }
    [Column("login_camera_sequence"), MaxLength(256)]
    public string? LoginCameraSequence { get; set; }
    [Column("login_music_file_id")]
    public long? LoginMusicFileId { get; set; }
    [Column("primary_logo_file_id")]
    public long? PrimaryLogoFileId { get; set; }
    [Column("version_logo_file_id")]
    public long? VersionLogoFileId { get; set; }
    [Column("loading_artwork_file_id")]
    public long? LoadingArtworkFileId { get; set; }
    [Column("character_selection_scene_file_id")]
    public long? CharacterSelectionSceneFileId { get; set; }
    [Column("character_selection_camera_sequence"), MaxLength(256)]
    public string? CharacterSelectionCameraSequence { get; set; }
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
    [Column("published_at")]
    public DateTimeOffset? PublishedAt { get; set; }
    [Column("retired_at")]
    public DateTimeOffset? RetiredAt { get; set; }
    public ICollection<AssetReleaseArtifact> Artifacts { get; set; } = [];
    public ICollection<AssetReleaseEvent> Events { get; set; } = [];
}
