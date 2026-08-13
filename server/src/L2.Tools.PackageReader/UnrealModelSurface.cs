using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealModelSurface(
    UnrealObjectReference? Material,
    int RawMaterialReference,
    bool MaterialReferenceInvalid,
    UnrealPolyFlags Flags,
    int BasePoint,
    int NormalVector,
    int TextureU,
    int TextureV);
