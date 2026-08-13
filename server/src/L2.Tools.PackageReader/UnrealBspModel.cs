using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealBspModel(
    string Name,
    IReadOnlyList<UnrealBspMeshChunk> Chunks,
    UnrealBspDiagnostics Diagnostics,
    string? Error);
