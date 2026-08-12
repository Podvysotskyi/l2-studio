namespace L2.Studio.Context.Entities;

public sealed class AssetArtifact
{
    public Guid Id { get; set; }
    public string GameVersion { get; set; } = "interlude";
    public required string Kind { get; set; }
    public required string SourceKey { get; set; }
    public required string NormalizedSourceKey { get; set; }
    public required string SourceHash { get; set; }
    public required string RecipeVersion { get; set; }
    public required string BuildFingerprint { get; set; }
    public required string ContentHash { get; set; }
    public required string OutputRoot { get; set; }
    public int SchemaVersion { get; set; }
    public int? Protocol { get; set; }
    public int FileCount { get; set; }
    public long SizeBytes { get; set; }
    public required string IntegrityStatus { get; set; }
    public DateTimeOffset? LastVerifiedAt { get; set; }
    public Guid PublishingWorkItemId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public AssetImportWorkItem PublishingWorkItem { get; set; } = null!;
    public ICollection<AssetArtifactFile> Files { get; set; } = [];
    public ICollection<AssetArtifactDependency> Dependencies { get; set; } = [];
    public ICollection<AssetCatalogSource> Publications { get; set; } = [];
    public ICollection<AssetReleaseArtifact> Releases { get; set; } = [];
}
