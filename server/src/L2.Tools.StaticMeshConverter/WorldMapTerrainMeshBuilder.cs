using System.Buffers.Binary;
using System.Numerics;
using L2.Tools.PackageReader;

namespace L2.Tools.StaticMeshConverter;

public static class WorldMapTerrainMeshBuilder
{
    public const int Resolution = 101;

    public static UnrealStaticMesh Build(
        UnrealTexture heightmap,
        UnrealCoordinateFrame toWorld,
        string name)
    {
        ArgumentNullException.ThrowIfNull(heightmap);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (heightmap.Format != UnrealTextureFormat.G16)
            throw new InvalidDataException($"Terrain heightmap '{heightmap.Name}' is not G16.");
        if (heightmap.Width < 2 || heightmap.Height < 2 ||
            heightmap.Data.Length != checked(heightmap.Width * heightmap.Height * 2))
            throw new InvalidDataException($"Terrain heightmap '{heightmap.Name}' has invalid dimensions or data length.");

        var positions = new Vector3[Resolution * Resolution];
        var normals = new Vector3[positions.Length];
        var textureCoordinates = new Vector2[positions.Length];
        for (var y = 0; y < Resolution; y++)
        {
            for (var x = 0; x < Resolution; x++)
            {
                var sourceX = SampleCoordinate(x, heightmap.Width);
                var sourceY = SampleCoordinate(y, heightmap.Height);
                var index = y * Resolution + x;
                positions[index] = toWorld.TransformPoint(new Vector3(
                    sourceX,
                    sourceY,
                    Height(heightmap, sourceX, sourceY)));
                textureCoordinates[index] = new Vector2(
                    x / (float)(Resolution - 1),
                    y / (float)(Resolution - 1));
            }
        }

        for (var y = 0; y < Resolution; y++)
        {
            for (var x = 0; x < Resolution; x++)
            {
                var left = positions[y * Resolution + Math.Max(0, x - 1)];
                var right = positions[y * Resolution + Math.Min(Resolution - 1, x + 1)];
                var down = positions[Math.Max(0, y - 1) * Resolution + x];
                var up = positions[Math.Min(Resolution - 1, y + 1) * Resolution + x];
                normals[y * Resolution + x] = Vector3.Normalize(Vector3.Cross(right - left, up - down));
            }
        }

        var indices = new ushort[(Resolution - 1) * (Resolution - 1) * 6];
        var cursor = 0;
        for (var y = 0; y < Resolution - 1; y++)
        {
            for (var x = 0; x < Resolution - 1; x++)
            {
                var topLeft = checked((ushort)(y * Resolution + x));
                var topRight = checked((ushort)(topLeft + 1));
                var bottomLeft = checked((ushort)((y + 1) * Resolution + x));
                var bottomRight = checked((ushort)(bottomLeft + 1));
                indices[cursor++] = topLeft;
                indices[cursor++] = bottomLeft;
                indices[cursor++] = topRight;
                indices[cursor++] = topRight;
                indices[cursor++] = bottomLeft;
                indices[cursor++] = bottomRight;
            }
        }

        return new UnrealStaticMesh(
            name,
            positions,
            normals,
            textureCoordinates,
            indices,
            [new UnrealStaticMeshSection(0, indices.Length)]);
    }

    private static int SampleCoordinate(int sample, int sourceLength) =>
        (int)Math.Round(sample * (sourceLength - 1d) / (Resolution - 1));

    private static float Height(UnrealTexture heightmap, int x, int y)
    {
        var offset = (y * heightmap.Width + x) * 2;
        return BinaryPrimitives.ReadUInt16LittleEndian(heightmap.Data.AsSpan(offset, 2));
    }
}
