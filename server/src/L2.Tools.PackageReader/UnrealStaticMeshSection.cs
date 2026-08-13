using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealStaticMeshSection(
    int FirstIndex,
    int IndexCount,
    UnrealObjectReference? Material = null);
