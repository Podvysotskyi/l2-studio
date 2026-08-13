using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealScene(
    UnrealLevel Level,
    IReadOnlyList<UnrealSkyZoneInfo> SkyZones,
    IReadOnlyList<UnrealSkyBackdrop> SkyBackdrops,
    IReadOnlyList<UnrealSceneObject> Cameras,
    IReadOnlyList<UnrealSceneObject> InterpolationPoints,
    IReadOnlyList<UnrealSceneObject> SceneManagers,
    IReadOnlyList<UnrealSceneObject> Actions,
    IReadOnlyList<UnrealSceneObject> AmbientSounds,
    IReadOnlyList<UnrealSceneObject> Effects);
