namespace L2.Studio.Context.Entities;

public sealed class AssetArtifactDependency
{
    public long Id { get; set; }
    public Guid ArtifactId { get; set; }
    public required string Kind { get; set; }
    public required string DependencyKey { get; set; }
    public Guid? ResolvedArtifactId { get; set; }
    public string? ResolvedSourceKey { get; set; }
    public string? BuildFingerprint { get; set; }
    public bool IsResolved { get; set; }
    public AssetArtifact Artifact { get; set; } = null!;
    public AssetArtifact? ResolvedArtifact { get; set; }
}
