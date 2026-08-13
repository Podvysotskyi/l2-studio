namespace L2.Studio.Repositories.Interfaces.Models;

public sealed record AssetArtifactFilePublication(
    string RelativePath,
    string PublicPath,
    string Role,
    string MediaType,
    long SizeBytes,
    string Sha256);
