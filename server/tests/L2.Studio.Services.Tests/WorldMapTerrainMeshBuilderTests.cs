using System.Buffers.Binary;
using System.Numerics;
using L2.Tools.PackageReader;
using L2.Tools.StaticMeshConverter;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class WorldMapTerrainMeshBuilderTests
{
    [Fact]
    public void BuildsAOneHundredByOneHundredCellWorldSpaceTerrainMesh()
    {
        var data = new byte[8];
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(0, 2), 10);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(2, 2), 20);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(4, 2), 30);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(6, 2), 40);
        var texture = new UnrealTexture("height", UnrealTextureFormat.G16, 2, 2, data);
        var frame = new UnrealCoordinateFrame(Vector3.Zero, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);

        var mesh = WorldMapTerrainMeshBuilder.Build(texture, frame, "17_25/TerrainInfo0");

        Assert.Equal(101, WorldMapTerrainMeshBuilder.Resolution);
        Assert.Equal(10201, mesh.Positions.Count);
        Assert.Equal(60000, mesh.Indices.Count);
        Assert.Equal(new Vector3(0, 0, 10), mesh.Positions[0]);
        Assert.Equal(new Vector3(1, 1, 40), mesh.Positions[^1]);
    }
}
