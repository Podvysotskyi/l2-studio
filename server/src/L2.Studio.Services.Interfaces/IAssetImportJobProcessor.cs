namespace L2.Studio.Services.Interfaces;

public interface IAssetImportJobProcessor
{
    Task<bool> ProcessNextAsync(CancellationToken cancellationToken);
}
