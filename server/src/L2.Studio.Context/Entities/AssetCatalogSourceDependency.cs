namespace L2.Studio.Context.Entities;

public sealed class AssetCatalogSourceDependency
{
    public long Id { get; set; }
    public Guid SourceId { get; set; }
    public required string Kind { get; set; }
    public required string DependencyKey { get; set; }
    public string? ResolvedSourceKey { get; set; }
    public string? ArtifactFingerprint { get; set; }
    public bool IsResolved { get; set; }
    public AssetCatalogSource Source { get; set; } = null!;
}
