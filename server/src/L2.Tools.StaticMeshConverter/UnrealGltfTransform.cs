using System.Numerics;

namespace L2.Tools.StaticMeshConverter;

internal static class UnrealGltfTransform
{
    private const float CentimetersToMeters = 0.01f;

    public static Vector3 Position(Vector3 value) => Direction(value) * CentimetersToMeters;

    public static Vector3 Direction(Vector3 value) => new(value.X, value.Z, value.Y);

    public static Quaternion Rotation(Quaternion value, bool conjugateRoot = false)
    {
        var converted = Quaternion.Normalize(new Quaternion(value.X, value.Z, value.Y, value.W));
        return conjugateRoot ? Quaternion.Conjugate(converted) : converted;
    }
}
