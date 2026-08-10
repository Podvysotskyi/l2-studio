using System.Buffers.Binary;
using System.Numerics;
using L2.Tools.PackageReader;

namespace L2.Tools.StaticMeshConverter;

public static class UnrealTerrainMeshBuilder
{
    public static UnrealStaticMesh Build(
        UnrealTexture heightmap,
        UnrealCoordinateFrame toWorld,
        Vector3 terrainLocation)
    {
        ArgumentNullException.ThrowIfNull(heightmap);
        if (heightmap.Format != UnrealTextureFormat.G16)
        {
            throw new InvalidDataException($"Terrain heightmap '{heightmap.Name}' is not G16.");
        }
        if (heightmap.Width < 2 || heightmap.Height < 2 ||
            heightmap.Data.Length != checked(heightmap.Width * heightmap.Height * 2))
        {
            throw new InvalidDataException($"Terrain heightmap '{heightmap.Name}' has invalid dimensions or data length.");
        }
        if (heightmap.Width * heightmap.Height > ushort.MaxValue + 1)
        {
            throw new InvalidDataException($"Terrain heightmap '{heightmap.Name}' exceeds the 16-bit GLB index limit.");
        }

        var positions = new Vector3[heightmap.Width * heightmap.Height];
        var normals = new Vector3[positions.Length];
        var textureCoordinates = new Vector2[positions.Length];
        for (var y = 0; y < heightmap.Height; y++)
        {
            for (var x = 0; x < heightmap.Width; x++)
            {
                var index = y * heightmap.Width + x;
                positions[index] = toWorld.TransformPoint(new Vector3(
                    x,
                    y,
                    Height(heightmap, x, y))) - terrainLocation;
                textureCoordinates[index] = new Vector2(
                    x / (float)(heightmap.Width - 1),
                    y / (float)(heightmap.Height - 1));
            }
        }

        for (var y = 0; y < heightmap.Height; y++)
        {
            for (var x = 0; x < heightmap.Width; x++)
            {
                var left = positions[y * heightmap.Width + Math.Max(0, x - 1)];
                var right = positions[y * heightmap.Width + Math.Min(heightmap.Width - 1, x + 1)];
                var down = positions[Math.Max(0, y - 1) * heightmap.Width + x];
                var up = positions[Math.Min(heightmap.Height - 1, y + 1) * heightmap.Width + x];
                normals[y * heightmap.Width + x] = Vector3.Normalize(Vector3.Cross(right - left, up - down));
            }
        }

        var indices = new ushort[(heightmap.Width - 1) * (heightmap.Height - 1) * 6];
        var cursor = 0;
        for (var y = 0; y < heightmap.Height - 1; y++)
        {
            for (var x = 0; x < heightmap.Width - 1; x++)
            {
                var topLeft = checked((ushort)(y * heightmap.Width + x));
                var topRight = checked((ushort)(topLeft + 1));
                var bottomLeft = checked((ushort)((y + 1) * heightmap.Width + x));
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
            heightmap.Name,
            positions,
            normals,
            textureCoordinates,
            indices,
            [new UnrealStaticMeshSection(0, indices.Length)]);
    }

    private static float Height(UnrealTexture heightmap, int x, int y)
    {
        var offset = (y * heightmap.Width + x) * 2;
        var sample = BinaryPrimitives.ReadUInt16LittleEndian(heightmap.Data.AsSpan(offset, 2));
        return sample;
    }
}
