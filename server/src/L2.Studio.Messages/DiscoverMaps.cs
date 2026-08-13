namespace L2.Studio.Messages;

public sealed record DiscoverMaps(Guid RunId) : IAssetImportDiscoveryCommand;
