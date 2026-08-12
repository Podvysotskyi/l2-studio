namespace L2.Studio.Context.Entities;

public sealed class AssetRelease
{
    public Guid Id { get; set; }
    public string GameVersion { get; set; } = "interlude";
    public required string Name { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "draft";
    public required string SnapshotHash { get; set; }
    public string ValidationStatus { get; set; } = "not_validated";
    public string ValidationIssuesJson { get; set; } = "[]";
    public string? ValidatedSnapshotHash { get; set; }
    public DateTimeOffset? ValidationRequestedAt { get; set; }
    public DateTimeOffset? ValidatedAt { get; set; }
    public string? ManifestPath { get; set; }
    public string? ManifestHash { get; set; }
    public long? LoginSceneFileId { get; set; }
    public string? LoginCameraSequence { get; set; }
    public long? LoginMusicFileId { get; set; }
    public long? PrimaryLogoFileId { get; set; }
    public long? VersionLogoFileId { get; set; }
    public long? LoadingArtworkFileId { get; set; }
    public long? CharacterSelectionSceneFileId { get; set; }
    public string? CharacterSelectionCameraSequence { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? RetiredAt { get; set; }
    public ICollection<AssetReleaseArtifact> Artifacts { get; set; } = [];
    public ICollection<AssetReleaseEvent> Events { get; set; } = [];
}
