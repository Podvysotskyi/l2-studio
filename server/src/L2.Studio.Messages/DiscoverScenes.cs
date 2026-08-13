namespace L2.Studio.Messages;

public sealed record DiscoverScenes(Guid RunId) : IAssetImportDiscoveryCommand;
