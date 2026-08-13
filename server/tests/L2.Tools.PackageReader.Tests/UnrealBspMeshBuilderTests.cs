using System.Numerics;
using L2.Tools.PackageReader;
using Xunit;

namespace L2.Tools.PackageReader.Tests;

public sealed class UnrealBspMeshBuilderTests
{
    [Fact]
    public void ClassifiesTheLegacyWaterSurfaceMaterial()
    {
        var model = Plane("legacy", 0, new UnrealObjectReference(
            "imazEffects",
            "water.WaterSurface",
            "Texture"));

        var result = UnrealBspMeshBuilder.Build(model, UnrealModelSurfaceSelection.World);

        Assert.Equal(UnrealBspMeshRole.WaterSurface, Assert.Single(result.Chunks).Role);
    }

    [Fact]
    public void ClassifiesAnUnknownMaterialWhenItOverlapsTheWaterVolumeTop()
    {
        var model = Plane("swamp", 0, new UnrealObjectReference("T_oren", "Swamp_Shader", "Shader"));

        var result = UnrealBspMeshBuilder.Build(
            model,
            UnrealModelSurfaceSelection.World,
            waterVolumes: [WaterVolume()]);

        var chunk = Assert.Single(result.Chunks);
        Assert.Equal(UnrealBspMeshRole.WaterSurface, chunk.Role);
        Assert.Equal(["WaterVolume1"], chunk.WaterVolumeNames);
    }

    [Fact]
    public void LeavesAnUnknownNonOverlappingSurfaceAsGeometry()
    {
        var model = Plane("dry", 100, new UnrealObjectReference("Map", "Stone", "Texture"));

        var result = UnrealBspMeshBuilder.Build(
            model,
            UnrealModelSurfaceSelection.World,
            waterVolumes: [WaterVolume()]);

        var chunk = Assert.Single(result.Chunks);
        Assert.Equal(UnrealBspMeshRole.Geometry, chunk.Role);
        Assert.Empty(chunk.WaterVolumeNames ?? []);
    }

    [Fact]
    public void RetainsTheExactWorldBaseRoleForWaterLikeMaterials()
    {
        var model = Plane(
            "world-base",
            -16384,
            new UnrealObjectReference("T_oren", "water.WaterSurface", "Texture"),
            327680,
            262144);

        var result = UnrealBspMeshBuilder.Build(model, UnrealModelSurfaceSelection.World);

        Assert.Equal(UnrealBspMeshRole.WorldBase, Assert.Single(result.Chunks).Role);
    }

    [Fact]
    public void RetainsSkyZonePriorityOverWaterClassification()
    {
        var source = Plane("sky-water", 0, new UnrealObjectReference(
            "FX_E_T",
            "WaterSurfaceShaderSet.WaterShader01",
            "Shader"));
        var model = source with
        {
            Nodes = [source.Nodes[0] with { FrontZone = 1, BackZone = 1 }]
        };
        var skyZone = new UnrealSkyZoneInfo(0, "SkyZoneInfo0", Vector3.UnitX, 1, 1, 1, []);

        var result = UnrealBspMeshBuilder.Build(
            model,
            UnrealModelSurfaceSelection.World,
            [skyZone],
            [WaterVolume()]);

        Assert.Equal(UnrealBspMeshRole.SkyZone, Assert.Single(result.Chunks).Role);
    }

    private static UnrealModelData Plane(
        string name,
        float height,
        UnrealObjectReference material,
        float halfWidth = 1,
        float halfHeight = 1)
    {
        var points = new[]
        {
            new Vector3(-halfWidth, -halfHeight, height),
            new Vector3(halfWidth, -halfHeight, height),
            new Vector3(halfWidth, halfHeight, height),
            new Vector3(-halfWidth, halfHeight, height)
        };
        return new UnrealModelData(
            name,
            [Vector3.UnitX, Vector3.UnitY],
            points,
            [new UnrealModelNode(0, 0, 4, Vector3.UnitZ, FrontZone: 0, BackZone: 0)],
            [new UnrealModelSurface(material, -1, false, UnrealPolyFlags.None, 0, 0, 0, 1)],
            [0, 1, 2, 3]);
    }

    private static UnrealWaterVolume WaterVolume() => new(
        "WaterVolume1",
        "WaterVolume",
        Vector3.Zero,
        default,
        Vector3.Zero,
        1,
        Vector3.One,
        null,
        new UnrealBrushGeometry(
            [
                new(-2, -2, -1), new(2, -2, -1), new(2, 2, -1), new(-2, 2, -1),
                new(-2, -2, 0), new(2, -2, 0), new(2, 2, 0), new(-2, 2, 0)
            ],
            [],
            [
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6,
                3, 0, 4, 3, 4, 7
            ]),
        null);
}
