namespace L2.Studio.Services.Interfaces;

public interface IAssetImportWorkItemProcessor
{
    Task ProcessAsync(Guid workItemId, CancellationToken cancellationToken);
}
