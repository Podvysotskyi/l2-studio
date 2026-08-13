namespace L2.Studio.Messages;

public sealed record DiscoverMusic(Guid RunId) : IAssetImportDiscoveryCommand;
