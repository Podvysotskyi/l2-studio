using System.Numerics;

namespace L2.Tools.PackageReader;

public readonly record struct UnrealCoordinateFrame(
    Vector3 Origin,
    Vector3 XAxis,
    Vector3 YAxis,
    Vector3 ZAxis)
{
    public Vector3 TransformPoint(Vector3 value)
    {
        var relative = value - Origin;
        return new Vector3(
            Vector3.Dot(relative, XAxis),
            Vector3.Dot(relative, YAxis),
            Vector3.Dot(relative, ZAxis));
    }
}
