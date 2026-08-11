namespace L2.Studio.Messages;

public interface IAssetImportDiscoveryCommand
{
    Guid RunId { get; }
}

public interface IAssetImportFileCommand
{
    Guid WorkItemId { get; }
}

public sealed record DiscoverSystemTextures(Guid RunId) : IAssetImportDiscoveryCommand;
public sealed record DiscoverTextures(Guid RunId) : IAssetImportDiscoveryCommand;
public sealed record DiscoverStaticMeshes(Guid RunId) : IAssetImportDiscoveryCommand;
public sealed record DiscoverSounds(Guid RunId) : IAssetImportDiscoveryCommand;
public sealed record DiscoverMusic(Guid RunId) : IAssetImportDiscoveryCommand;
public sealed record DiscoverLevels(Guid RunId) : IAssetImportDiscoveryCommand;
public sealed record DiscoverScenes(Guid RunId) : IAssetImportDiscoveryCommand;
public sealed record DiscoverLevelPreviews(Guid RunId) : IAssetImportDiscoveryCommand;

public sealed record ImportSystemTextureFile(Guid WorkItemId) : IAssetImportFileCommand;
public sealed record ImportTextureFile(Guid WorkItemId) : IAssetImportFileCommand;
public sealed record ImportStaticMeshFile(Guid WorkItemId) : IAssetImportFileCommand;
public sealed record ImportSoundFile(Guid WorkItemId) : IAssetImportFileCommand;
public sealed record ImportMusicFile(Guid WorkItemId) : IAssetImportFileCommand;
public sealed record ImportLevelFile(Guid WorkItemId) : IAssetImportFileCommand;
public sealed record ImportSceneFile(Guid WorkItemId) : IAssetImportFileCommand;
public sealed record GenerateLevelPreview(Guid WorkItemId) : IAssetImportFileCommand;

public sealed record AssetImportWorkItemCompleted(Guid RunId, Guid WorkItemId);
public sealed record FinalizeAssetImportRun(Guid RunId);
public sealed record DeleteAssetVersion(string RelativePath, bool Force);
public sealed record ReconcileAssetStorage;
