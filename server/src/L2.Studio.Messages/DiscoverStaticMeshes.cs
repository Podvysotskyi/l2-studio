namespace L2.Studio.Messages;

public sealed record DiscoverStaticMeshes(Guid RunId) : IAssetImportDiscoveryCommand;
