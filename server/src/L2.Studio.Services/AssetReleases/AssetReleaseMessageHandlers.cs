using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces;
using Wolverine.Attributes;

namespace L2.Studio.Services;

[WolverineHandler]
public static class AssetReleaseMessageHandlers
{
    public static Task Handle(ValidateAssetRelease message, IAssetReleaseRepository releases, CancellationToken token) =>
        releases.ValidateAsync(message.ReleaseId, token);

    public static Task Handle(ActivateAssetRelease message, IAssetReleaseRepository releases, CancellationToken token) =>
        releases.ActivateAsync(message.GameVersion, message.ReleaseId, token);
}
