namespace L2.Studio.Messages;

public sealed record ImportMusicFile(Guid WorkItemId) : IAssetImportFileCommand;
