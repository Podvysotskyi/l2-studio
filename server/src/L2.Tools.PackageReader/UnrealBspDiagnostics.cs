using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealBspDiagnostics(
    int SplitterNodeCount,
    int InvisibleSurfaceCount,
    int PortalSurfaceCount,
    int FakeBackdropSurfaceCount,
    int MalformedSurfaceCount,
    int UnresolvedMaterialReferenceCount);
