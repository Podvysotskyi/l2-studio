namespace L2.Studio.Contracts;

public sealed record AssetReleaseEntrypoints(
    long? LoginSceneFileId,
    string? LoginScenePath,
    string? LoginCameraSequence,
    long? LoginMusicFileId,
    string? LoginMusicPath,
    long? PrimaryLogoFileId,
    string? PrimaryLogoPath,
    long? VersionLogoFileId,
    string? VersionLogoPath,
    long? LoadingArtworkFileId,
    string? LoadingArtworkPath,
    long? CharacterSelectionSceneFileId,
    string? CharacterSelectionScenePath,
    string? CharacterSelectionCameraSequence);
