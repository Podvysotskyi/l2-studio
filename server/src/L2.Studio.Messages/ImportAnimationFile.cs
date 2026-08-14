namespace L2.Studio.Messages;

public sealed record ImportAnimationFile(Guid WorkItemId) : IAssetImportFileCommand;
