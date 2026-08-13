namespace L2.Studio.Messages;

public sealed record DiscoverTextures(Guid RunId) : IAssetImportDiscoveryCommand;
