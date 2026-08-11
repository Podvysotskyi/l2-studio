using System.Numerics;

namespace L2.Tools.PackageReader;

public static class UnrealTerrainUvTransformBuilder
{
    private const float RotationUnit = MathF.PI * 2 / 65536;

    public static UnrealTerrainUvTransform Build(
        UnrealCoordinateFrame toWorld,
        UnrealCoordinateFrame toHeightMap,
        Vector3 terrainLocation,
        UnrealTerrainLayer layer)
    {
        if (layer.TextureMapAxis > 2)
            throw new InvalidDataException($"Terrain layer {layer.Index} has unknown texture-map axis {layer.TextureMapAxis}.");
        if (!float.IsFinite(layer.UScale) || !float.IsFinite(layer.VScale) ||
            !float.IsFinite(layer.UPan) || !float.IsFinite(layer.VPan) ||
            MathF.Abs(layer.UScale) < 0.000001f || MathF.Abs(layer.VScale) < 0.000001f ||
            !Finite(terrainLocation) || !Finite(toWorld) || !Finite(toHeightMap))
        {
            throw new InvalidDataException($"Terrain layer {layer.Index} has an invalid affine texture transform.");
        }

        var textureCoordinates = DivideRotation(Frame.Identity, new UnrealRotator(0, Angle(layer.TextureRotation), 0));
        textureCoordinates = Multiply(textureCoordinates, Transpose(toHeightMap));
        textureCoordinates = textureCoordinates with
        {
            XAxis = textureCoordinates.XAxis / layer.UScale,
            YAxis = textureCoordinates.YAxis / layer.VScale
        };
        var pan = TransformVector(
            new Vector3(layer.UPan * layer.UScale, layer.VPan * layer.VScale, 0),
            toWorld);
        textureCoordinates = textureCoordinates with { Origin = textureCoordinates.Origin + pan };
        textureCoordinates = Project(textureCoordinates, layer.TextureMapAxis);
        textureCoordinates = Multiply(textureCoordinates, DivideRotation(Frame.Identity, layer.LayerRotation));

        Vector2 Evaluate(Vector3 gltfLocalPosition)
        {
            var unrealWorldPosition = new Vector3(
                gltfLocalPosition.X,
                gltfLocalPosition.Z,
                gltfLocalPosition.Y) + terrainLocation;
            var projected = TransformPoint(unrealWorldPosition, textureCoordinates);
            return new Vector2(projected.X, projected.Y);
        }

        var origin = Evaluate(Vector3.Zero);
        var x = Evaluate(Vector3.UnitX) - origin;
        var y = Evaluate(Vector3.UnitY) - origin;
        var z = Evaluate(Vector3.UnitZ) - origin;
        var transform = new UnrealTerrainUvTransform(
            new UnrealTerrainUvTransformRow(x.X, y.X, z.X, origin.X),
            new UnrealTerrainUvTransformRow(x.Y, y.Y, z.Y, origin.Y));
        if (!Finite(transform))
            throw new InvalidDataException($"Terrain layer {layer.Index} produced a non-finite affine texture transform.");
        return transform;
    }

    private static Frame Project(Frame frame, byte axis) => axis switch
    {
        0 => frame,
        1 => frame with
        {
            Origin = new Vector3(frame.Origin.Y, frame.Origin.Z, frame.Origin.X),
            XAxis = new Vector3(frame.XAxis.Z, frame.XAxis.Y, frame.XAxis.X),
            YAxis = new Vector3(frame.YAxis.Y, frame.YAxis.X, frame.YAxis.Z),
            ZAxis = new Vector3(frame.ZAxis.X, frame.ZAxis.Z, frame.ZAxis.Y)
        },
        2 => frame with
        {
            Origin = new Vector3(frame.Origin.Z, frame.Origin.Y, frame.Origin.X),
            XAxis = new Vector3(frame.XAxis.Z, frame.XAxis.Y, frame.XAxis.X),
            ZAxis = new Vector3(frame.ZAxis.Z, frame.ZAxis.Y, frame.ZAxis.X)
        },
        _ => throw new InvalidDataException($"Unknown terrain texture-map axis {axis}.")
    };

    private static Frame Transpose(UnrealCoordinateFrame frame) => new(
        -TransformVector(frame.Origin, frame),
        new Vector3(frame.XAxis.X, frame.YAxis.X, frame.ZAxis.X),
        new Vector3(frame.XAxis.Y, frame.YAxis.Y, frame.ZAxis.Y),
        new Vector3(frame.XAxis.Z, frame.YAxis.Z, frame.ZAxis.Z));

    private static Frame DivideRotation(Frame frame, UnrealRotator rotation)
    {
        var roll = RotationFrame(
            Vector3.UnitX,
            new Vector3(0, Cos(rotation.Roll), Sin(rotation.Roll)),
            new Vector3(0, -Sin(rotation.Roll), Cos(rotation.Roll)));
        var pitch = RotationFrame(
            new Vector3(Cos(rotation.Pitch), 0, -Sin(rotation.Pitch)),
            Vector3.UnitY,
            new Vector3(Sin(rotation.Pitch), 0, Cos(rotation.Pitch)));
        var yaw = RotationFrame(
            new Vector3(Cos(rotation.Yaw), -Sin(rotation.Yaw), 0),
            new Vector3(Sin(rotation.Yaw), Cos(rotation.Yaw), 0),
            Vector3.UnitZ);
        return Multiply(Multiply(Multiply(frame, roll), pitch), yaw);
    }

    private static Frame RotationFrame(Vector3 x, Vector3 y, Vector3 z) =>
        new(Vector3.Zero, x, y, z);

    private static Frame Multiply(Frame left, Frame right) => new(
        TransformPoint(left.Origin, right),
        TransformVector(left.XAxis, right),
        TransformVector(left.YAxis, right),
        TransformVector(left.ZAxis, right));

    private static Vector3 TransformPoint(Vector3 value, Frame frame)
    {
        var relative = value - frame.Origin;
        return new Vector3(
            Vector3.Dot(relative, frame.XAxis),
            Vector3.Dot(relative, frame.YAxis),
            Vector3.Dot(relative, frame.ZAxis));
    }

    private static Vector3 TransformVector(Vector3 value, Frame frame) => new(
        Vector3.Dot(value, frame.XAxis),
        Vector3.Dot(value, frame.YAxis),
        Vector3.Dot(value, frame.ZAxis));

    private static Vector3 TransformVector(Vector3 value, UnrealCoordinateFrame frame) => new(
        Vector3.Dot(value, frame.XAxis),
        Vector3.Dot(value, frame.YAxis),
        Vector3.Dot(value, frame.ZAxis));

    private static int Angle(float value) => checked((int)value);
    private static float Sin(int value) => MathF.Sin(value * RotationUnit);
    private static float Cos(int value) => MathF.Cos(value * RotationUnit);
    private static bool Finite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    private static bool Finite(UnrealCoordinateFrame value) =>
        Finite(value.Origin) && Finite(value.XAxis) && Finite(value.YAxis) && Finite(value.ZAxis);
    private static bool Finite(UnrealTerrainUvTransform value) =>
        Finite(value.U) && Finite(value.V);
    private static bool Finite(UnrealTerrainUvTransformRow value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) &&
        float.IsFinite(value.Z) && float.IsFinite(value.Offset);

    private readonly record struct Frame(
        Vector3 Origin,
        Vector3 XAxis,
        Vector3 YAxis,
        Vector3 ZAxis)
    {
        public static Frame Identity => new(
            Vector3.Zero,
            Vector3.UnitX,
            Vector3.UnitY,
            Vector3.UnitZ);
    }
}
