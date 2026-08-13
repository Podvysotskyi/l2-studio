namespace L2.Studio.Messages;

public sealed record ImportTextureFile(Guid WorkItemId) : IAssetImportFileCommand;
