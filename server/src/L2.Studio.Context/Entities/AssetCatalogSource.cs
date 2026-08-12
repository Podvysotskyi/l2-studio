namespace L2.Studio.Context.Entities;

public sealed class AssetCatalogSource
{
    public Guid Id { get; set; }
    public Guid CatalogId { get; set; }
    public Guid ArtifactId { get; set; }
    public Guid PublishingWorkItemId { get; set; }
    public required string SourceKey { get; set; }
    public required string NormalizedSourceKey { get; set; }
    public required string SourceHash { get; set; }
    public string? ArtifactFingerprint { get; set; }
    public required string OutputRoot { get; set; }
    public required string MetadataJson { get; set; }
    public required string ReferencedOutputRootsJson { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public bool IsStale { get; set; }
    public DateTimeOffset? StaleAt { get; set; }
    public string StaleReasonsJson { get; set; } = "[]";
    public AssetCatalog Catalog { get; set; } = null!;
    public AssetArtifact Artifact { get; set; } = null!;
    public ICollection<AssetCatalogGroup> Groups { get; set; } = [];
    public ICollection<AssetCatalogItem> Items { get; set; } = [];
    public ICollection<AssetCatalogSourceDependency> Dependencies { get; set; } = [];
}
