namespace L2.Studio.Contracts;

public sealed record AssetArtifactPage(
    IReadOnlyList<AssetArtifactSummary> Items,
    long Total,
    int Page,
    int PageSize);
