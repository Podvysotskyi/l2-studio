namespace L2.Studio.Contracts;

public sealed record AssetArtifactFileSummary(
    string RelativePath,
    string PublicPath,
    string Role,
    string MediaType,
    long SizeBytes,
    string Sha256);
