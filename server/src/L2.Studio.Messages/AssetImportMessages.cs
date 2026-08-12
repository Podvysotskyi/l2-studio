namespace L2.Studio.Messages;

public interface IAssetImportDiscoveryCommand
{
    Guid RunId { get; }
}

public interface IAssetImportFileCommand
{
    Guid WorkItemId { get; }
}

public sealed record DiscoverTextures(Guid RunId) : IAssetImportDiscoveryCommand;
public sealed record DiscoverStaticMeshes(Guid RunId) : IAssetImportDiscoveryCommand;
public sealed record DiscoverSounds(Guid RunId) : IAssetImportDiscoveryCommand;
public sealed record DiscoverMusic(Guid RunId) : IAssetImportDiscoveryCommand;
public sealed record DiscoverMaps(Guid RunId) : IAssetImportDiscoveryCommand;
public sealed record DiscoverScenes(Guid RunId) : IAssetImportDiscoveryCommand;
public sealed record DiscoverMapPreviews(Guid RunId) : IAssetImportDiscoveryCommand;

public sealed record ImportTextureFile(Guid WorkItemId) : IAssetImportFileCommand;
public sealed record ImportStaticMeshFile(Guid WorkItemId) : IAssetImportFileCommand;
public sealed record ImportSoundFile(Guid WorkItemId) : IAssetImportFileCommand;
public sealed record ImportMusicFile(Guid WorkItemId) : IAssetImportFileCommand;
public sealed record ImportMapFile(Guid WorkItemId) : IAssetImportFileCommand;
public sealed record ImportSceneFile(Guid WorkItemId) : IAssetImportFileCommand;
public sealed record GenerateMapPreview(Guid WorkItemId) : IAssetImportFileCommand;

public sealed record AssetImportWorkItemCompleted(Guid RunId, Guid WorkItemId);
public sealed record FinalizeAssetImportRun(Guid RunId);
public sealed record DeleteAssetVersion(string RelativePath, bool Force);
public sealed record ReconcileAssetStorage;
public sealed record ValidateAssetRelease(Guid ReleaseId);
public sealed record ActivateAssetRelease(string GameVersion, Guid ReleaseId);
