namespace L2.Studio.Messages;

public sealed record GenerateMapPreview(Guid WorkItemId) : IAssetImportFileCommand;
