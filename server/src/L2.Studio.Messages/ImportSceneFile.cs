namespace L2.Studio.Messages;

public sealed record ImportSceneFile(Guid WorkItemId) : IAssetImportFileCommand;
