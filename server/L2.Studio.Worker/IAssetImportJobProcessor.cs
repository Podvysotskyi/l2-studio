namespace L2.Studio.Worker;

public interface IAssetImportJobProcessor
{
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken);
}
