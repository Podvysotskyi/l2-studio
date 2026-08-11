namespace L2.Studio.Context.Entities;

public sealed class AssetCatalog
{
    public Guid Id { get; set; }
    public required string Kind { get; set; }
    public required string SourceFolder { get; set; }
    public required string SourceHash { get; set; }
    public int SchemaVersion { get; set; }
    public int? Protocol { get; set; }
    public required string MetadataJson { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public ICollection<AssetCatalogSource> Sources { get; set; } = [];
    public ICollection<AssetCatalogGroup> Groups { get; set; } = [];
    public ICollection<AssetCatalogItem> Items { get; set; } = [];
}
