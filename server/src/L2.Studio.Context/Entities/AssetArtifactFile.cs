namespace L2.Studio.Context.Entities;

public sealed class AssetArtifactFile
{
    public long Id { get; set; }
    public Guid ArtifactId { get; set; }
    public required string RelativePath { get; set; }
    public required string PublicPath { get; set; }
    public required string Role { get; set; }
    public required string MediaType { get; set; }
    public long SizeBytes { get; set; }
    public required string Sha256 { get; set; }
    public AssetArtifact Artifact { get; set; } = null!;
}
