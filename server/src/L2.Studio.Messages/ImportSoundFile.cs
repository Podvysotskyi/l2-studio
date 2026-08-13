namespace L2.Studio.Messages;

public sealed record ImportSoundFile(Guid WorkItemId) : IAssetImportFileCommand;
