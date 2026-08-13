using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealModelNode(
    int VertexPool,
    int Surface,
    int VertexCount,
    Vector3 Normal,
    float PlaneW = 0,
    int Back = -1,
    int Front = -1,
    byte BackZone = 0,
    byte FrontZone = 0);
