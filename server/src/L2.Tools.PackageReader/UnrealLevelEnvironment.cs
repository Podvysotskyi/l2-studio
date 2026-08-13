using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealLevelEnvironment(
    string SourceName,
    string SourceClass,
    UnrealColor AmbientColor,
    float AmbientBrightness,
    UnrealDistanceFog? DistanceFog);
