using L2.Studio.Repositories.Interfaces.Models;

namespace L2.Studio.Repositories.Interfaces;

public interface IAssetCatalogStore
{
    Task PublishAsync(AssetCatalogPublication publication, CancellationToken cancellationToken);
}
