namespace L2.Studio.Messages;

public sealed record DiscoverNpcAppearances(Guid RunId) : IAssetImportDiscoveryCommand;
