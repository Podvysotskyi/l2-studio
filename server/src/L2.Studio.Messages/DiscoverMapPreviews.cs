namespace L2.Studio.Messages;

public sealed record DiscoverMapPreviews(Guid RunId) : IAssetImportDiscoveryCommand;
