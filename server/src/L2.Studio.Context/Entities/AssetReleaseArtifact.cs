namespace L2.Studio.Context.Entities;

public sealed class AssetReleaseArtifact
{
    public Guid ReleaseId { get; set; }
    public Guid ArtifactId { get; set; }
    public bool IsRoot { get; set; }
    public AssetRelease Release { get; set; } = null!;
    public AssetArtifact Artifact { get; set; } = null!;
}
