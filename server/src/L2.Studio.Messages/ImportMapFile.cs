namespace L2.Studio.Messages;

public sealed record ImportMapFile(Guid WorkItemId) : IAssetImportFileCommand;
