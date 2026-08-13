namespace L2.Studio.Messages;

public sealed record DiscoverSounds(Guid RunId) : IAssetImportDiscoveryCommand;
