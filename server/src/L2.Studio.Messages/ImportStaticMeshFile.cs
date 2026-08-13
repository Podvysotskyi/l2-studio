namespace L2.Studio.Messages;

public sealed record ImportStaticMeshFile(Guid WorkItemId) : IAssetImportFileCommand;
