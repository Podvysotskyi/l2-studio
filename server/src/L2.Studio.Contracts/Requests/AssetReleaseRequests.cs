namespace L2.Studio.Contracts.Requests;

public sealed record CreateAssetReleaseRequest(string Name, string? Notes);

public sealed record UpdateAssetReleaseRequest(
    string Name,
    string? Notes,
    long? LoginSceneFileId,
    string? LoginCameraSequence,
    long? LoginMusicFileId,
    long? PrimaryLogoFileId,
    long? VersionLogoFileId,
    long? LoadingArtworkFileId,
    long? CharacterSelectionSceneFileId,
    string? CharacterSelectionCameraSequence);
