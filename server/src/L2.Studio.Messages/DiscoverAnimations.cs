namespace L2.Studio.Messages;

public sealed record DiscoverAnimations(Guid RunId) : IAssetImportDiscoveryCommand;
