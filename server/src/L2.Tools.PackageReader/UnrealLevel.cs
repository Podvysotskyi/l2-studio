using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealLevel(
    IReadOnlyList<UnrealLevelActor> Actors,
    IReadOnlyList<UnrealTerrainInfo> Terrains,
    IReadOnlyList<UnrealLevelLight> Lights,
    IReadOnlyList<UnrealWaterVolume> WaterVolumes,
    IReadOnlyDictionary<string, int> UnrepresentedObjectClasses,
    UnrealLevelEnvironment? Environment = null,
    string? EnvironmentWarning = null,
    IReadOnlyList<UnrealBspModel>? BspModelData = null,
    IReadOnlyList<UnrealSkyZoneInfo>? SkyZoneData = null)
{
    public IReadOnlyList<UnrealBspModel> BspModels { get; } = BspModelData ?? [];
    public IReadOnlyList<UnrealSkyZoneInfo> SkyZones { get; } = SkyZoneData ?? [];
}
