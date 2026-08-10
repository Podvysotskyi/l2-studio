using L2.Tools.PackageReader;
using L2.Tools.StaticMeshConverter;
using L2.Studio.Worker;
using System.Numerics;
using Xunit;

namespace L2.Foundation.Tests;

public sealed class LevelImportTests
{
    [Fact]
    public void Published_level_and_scene_schema_versions_are_current()
    {
        Assert.Equal(12, AssetImportJobProcessor.LevelSchemaVersion);
        Assert.Equal(11, AssetImportJobProcessor.SceneSchemaVersion);
    }

    [Fact]
    public void Gludin_primary_water_BSP_is_linked_to_native_water_volumes()
    {
        var source = Path.Combine(
            FindRepositoryRoot(),
            "sources",
            "Interlude",
            "maps",
            "16_25.unr");
        if (!File.Exists(source)) return;

        var decoded = LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(source));
        var level = new UnrealPackageReader(decoded).ReadLevel();
        var water = Assert.Single(
            Assert.Single(level.BspModels).Chunks,
            chunk => chunk.Role == UnrealBspMeshRole.WaterSurface);

        Assert.Equal("Model589-bsp-0007", water.Name);
        Assert.Equal(9, water.SurfaceCount);
        Assert.Equal(22, water.Mesh.Indices.Count / 3);
        Assert.Equal(["WaterVolume1", "WaterVolume3"], water.WaterVolumeNames);
    }

    [Fact]
    public void Coordinate_sky_zone_BSP_is_classified_from_native_zones()
    {
        var source = Path.Combine(
            FindRepositoryRoot(),
            "sources",
            "Interlude",
            "maps",
            "16_25.unr");
        if (!File.Exists(source)) return;

        var decoded = LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(source));
        var level = new UnrealPackageReader(decoded).ReadLevel();
        var skyZone = Assert.Single(level.SkyZones, zone => zone.Name == "SkyZoneInfo0");
        var bsp = Assert.Single(level.BspModels);
        var skyChunks = bsp.Chunks
            .Where(chunk => chunk.Role == UnrealBspMeshRole.SkyZone)
            .ToArray();

        Assert.Equal(
            "Model589-bsp-0000:SkyZoneInfo0,Model589-bsp-0001:SkyZoneInfo0," +
            "Model589-bsp-0002:SkyZoneInfo0,Model589-bsp-0003:SkyZoneInfo0",
            string.Join(',', skyChunks.Select(chunk => $"{chunk.Name}:{chunk.SkyZoneName}")));
        Assert.All(skyChunks, chunk => Assert.Equal(skyZone.Name, chunk.SkyZoneName));
        Assert.Equal(53, skyChunks.Sum(chunk => chunk.SurfaceCount));
        Assert.Equal(76, skyChunks.Sum(chunk => chunk.Mesh.Indices.Count / 3));
    }

    [Fact]
    public void BSP_point_traversal_returns_the_native_leaf_zone()
    {
        var model = new UnrealModelData(
            "Model",
            [],
            [],
            [new UnrealModelNode(0, 0, 0, Vector3.UnitX, 5, -1, -1, 3, 7)],
            [],
            []);

        Assert.Equal(3, UnrealBspMeshBuilder.FindZone(model, new Vector3(4, 0, 0)));
        Assert.Equal(7, UnrealBspMeshBuilder.FindZone(model, new Vector3(6, 0, 0)));
    }

    [Fact]
    public void Primary_water_surface_links_all_overlapping_transformed_volumes()
    {
        var waterMaterial = new UnrealObjectReference(
            "FX_E_T",
            "WaterSurfaceShaderSet.WaterShader01",
            "Shader");
        var model = new UnrealModelData(
            "Model",
            [Vector3.UnitX * 256, Vector3.UnitY * 256],
            [new Vector3(0, 0, 100), new Vector3(10, 0, 100), new Vector3(10, 10, 100), new Vector3(0, 10, 100)],
            [new UnrealModelNode(0, 0, 4, Vector3.UnitZ)],
            [Surface(waterMaterial, UnrealPolyFlags.None)],
            [0, 1, 2, 3]);
        var boundary = new UnrealBrushGeometry(
            [new Vector3(0, 0, 5), new Vector3(10, 10, 5), new Vector3(10, 0, 5)],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [0, 1, 2]);
        UnrealWaterVolume Volume(string name, Vector3 location) => new(
            name,
            "WaterVolume",
            location,
            default,
            new Vector3(0, 0, 5),
            1,
            Vector3.One,
            null,
            boundary,
            null);

        var result = UnrealBspMeshBuilder.Build(
            model,
            UnrealModelSurfaceSelection.World,
            [],
            [Volume("WaterVolume2", new Vector3(0, 0, 100)), Volume("WaterVolume1", new Vector3(0, 0, 100)), Volume("Elsewhere", new Vector3(1000, 0, 100))]);
        var chunk = Assert.Single(result.Chunks);

        Assert.Equal(UnrealBspMeshRole.WaterSurface, chunk.Role);
        Assert.Equal(["WaterVolume1", "WaterVolume2"], chunk.WaterVolumeNames);
    }

    [Theory]
    [InlineData("15_20.unr", "Model5-bsp-0005", UnrealPolyFlags.None)]
    [InlineData("16_25.unr", "Model589-bsp-0007", UnrealPolyFlags.Translucent)]
    [InlineData("20_15.unr", "Model23-bsp-0005", UnrealPolyFlags.TwoSided)]
    public void Primary_water_material_role_does_not_depend_on_polygon_flags(
        string fileName,
        string chunkName,
        UnrealPolyFlags flags)
    {
        var source = Path.Combine(FindRepositoryRoot(), "sources", "Interlude", "maps", fileName);
        if (!File.Exists(source)) return;

        var decoded = LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(source));
        var chunk = Assert.Single(new UnrealPackageReader(decoded).ReadLevel().BspModels)
            .Chunks.Single(item => item.Name == chunkName);

        Assert.Equal(UnrealBspMeshRole.WaterSurface, chunk.Role);
        Assert.Equal(flags, chunk.RenderFlags);
    }

    [Fact]
    public void Talking_Island_level_can_be_read_when_local_source_is_available()
    {
        var source = Path.Combine(
            FindRepositoryRoot(),
            "sources",
            "Interlude",
            "maps",
            "17_25.unr");
        if (!File.Exists(source))
        {
            return;
        }

        var decoded = LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(source));
        var level = new UnrealPackageReader(decoded).ReadLevel();

        var bsp = Assert.Single(level.BspModels);
        Assert.Equal("Model315", bsp.Name);
        Assert.Null(bsp.Error);
        Assert.Equal(27, bsp.Chunks.Count);
        Assert.Equal(1019, bsp.Chunks.Sum(chunk => chunk.SurfaceCount));
        Assert.Equal(4704, bsp.Chunks.Sum(chunk => chunk.Mesh.Positions.Count));
        Assert.Equal(2634, bsp.Chunks.Sum(chunk => chunk.Mesh.Indices.Count / 3));
        var worldBase = Assert.Single(
            bsp.Chunks,
            chunk => chunk.Role == UnrealBspMeshRole.WorldBase);
        Assert.Equal("Model315-bsp-0004", worldBase.Name);
        Assert.Equal(36, worldBase.SurfaceCount);
        var skyChunks = bsp.Chunks
            .Where(chunk => chunk.Role == UnrealBspMeshRole.SkyZone)
            .ToArray();
        Assert.Equal(
            [
                "Model315-bsp-0023",
                "Model315-bsp-0024",
                "Model315-bsp-0025",
                "Model315-bsp-0026"
            ],
            skyChunks.Select(chunk => chunk.Name));
        Assert.All(skyChunks, chunk => Assert.Equal("SkyZoneInfo0", chunk.SkyZoneName));
        var waterSurface = Assert.Single(
            bsp.Chunks,
            chunk => chunk.Role == UnrealBspMeshRole.WaterSurface);
        Assert.Equal("Model315-bsp-0005", waterSurface.Name);
        Assert.Equal(["WaterVolume0"], waterSurface.WaterVolumeNames);
        Assert.All(
            bsp.Chunks.Where(chunk =>
                chunk != worldBase && chunk != waterSurface && !skyChunks.Contains(chunk)),
            chunk => Assert.Equal(UnrealBspMeshRole.Geometry, chunk.Role));
        Assert.Equal(65, bsp.Diagnostics.InvisibleSurfaceCount);
        Assert.Equal(5, bsp.Diagnostics.FakeBackdropSurfaceCount);
        Assert.Equal(0, bsp.Diagnostics.MalformedSurfaceCount);
        Assert.Equal(0, bsp.Diagnostics.UnresolvedMaterialReferenceCount);
        Assert.All(bsp.Chunks, chunk =>
        {
            Assert.InRange(chunk.Mesh.Positions.Count, 1, ushort.MaxValue);
            Assert.All(chunk.Mesh.Positions, position => AssertFinite(position));
            Assert.All(chunk.Mesh.Normals, normal => AssertFinite(normal));
            Assert.All(chunk.Mesh.TextureCoordinates, coordinate =>
            {
                Assert.True(float.IsFinite(coordinate.X));
                Assert.True(float.IsFinite(coordinate.Y));
            });
        });

        Assert.NotEmpty(level.Terrains);
        Assert.Contains(level.Terrains, terrain =>
            terrain.TerrainMap is { } map &&
            string.Equals(map.PackageName, "T_17_25", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(map.ObjectName, "Height.17_25", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(983, level.Actors.Count);
        Assert.Equal(55, level.Lights.Count);
        var water = Assert.Single(level.WaterVolumes);
        Assert.Equal("WaterVolume0", water.Name);
        Assert.Equal("Model269", water.Brush?.ObjectName);
        AssertVector(new Vector3(-98304, 262144, -3776), water.Location);
        Assert.NotNull(water.Geometry);
        Assert.Equal(24, water.Geometry!.Positions.Count);
        Assert.Equal(12, water.Geometry.TriangleCount);
        Assert.Null(water.Error);
        Assert.Equal(983, level.Actors.Count(actor => actor.Location != Vector3.Zero));
        Assert.Equal(923, level.Actors.Count(actor => actor.Rotation != default));
        Assert.Equal(258, level.Actors.Count(actor => actor.DrawScale != 1));
        Assert.Equal(19, level.Actors.Count(actor => actor.DrawScale3D != Vector3.One));
        Assert.DoesNotContain(level.Actors, actor => actor.Name is
            "StaticMeshActor574" or
            "StaticMeshActor586" or
            "StaticMeshActor627");
        var actor = Assert.Single(level.Actors, actor => actor.Name == "StaticMeshActor555");
        AssertVector(new Vector3(-71169.15625f, 257961.453125f, -2860), actor.Location);
        Assert.Equal(new UnrealRotator(0, -7336, 0), actor.Rotation);

        var terrainInfo = Assert.Single(level.Terrains);
        AssertVector(new Vector3(-81920, 245760, 160.6512756f), terrainInfo.Location);
        AssertVector(new Vector3(768, -1792, 32225.859375f), terrainInfo.ToWorld.Origin);
        AssertVector(new Vector3(128, 0, 0), terrainInfo.ToWorld.XAxis);
        AssertVector(new Vector3(0, 128, 0), terrainInfo.ToWorld.YAxis);
        AssertVector(new Vector3(0, 0, 0.296875f), terrainInfo.ToWorld.ZAxis);
        Assert.All(
            new[]
            {
                terrainInfo.ToHeightMap.Origin.X,
                terrainInfo.ToHeightMap.Origin.Y,
                terrainInfo.ToHeightMap.Origin.Z,
                terrainInfo.ToHeightMap.XAxis.X,
                terrainInfo.ToHeightMap.YAxis.Y,
                terrainInfo.ToHeightMap.ZAxis.Z
            },
            value => Assert.True(float.IsFinite(value)));
        Assert.Equal(10, terrainInfo.Layers.Count);
        var paintedLayers = terrainInfo.Layers
            .Where(layer => layer.Texture is not null && layer.AlphaMap is not null)
            .ToArray();
        Assert.Equal(10, paintedLayers.Length);
        Assert.Equal(
            [
                "T_texture.Texture.Base",
                "T_sland.SL_G",
                "T_sland.SL_S3",
                "T_sland.SL_WR",
                "T_sland.SL_S6",
                "T_sland.SL_G3",
                "T_sland.SL_G2",
                "T_sland.SL_S1",
                "T_sland.SL_R1",
                "T_sland.SL_C"
            ],
            paintedLayers.Select(layer => layer.Texture?.Path));
        Assert.Equal(
            [
                "T_texture.Texture.layer0",
                "T_17_25.Height.17_25_G1",
                "T_17_25.Height.17_25_S3",
                "T_17_25.Height.17_25_WR",
                "T_17_25.Height.17_25_S",
                "T_17_25.Height.17_25_G",
                "T_17_25.Height.17_25_G2",
                "T_17_25.Height.17_25_S2",
                "T_17_25.Height.17_25_R",
                "T_17_25.Height.17_25_C"
            ],
            paintedLayers.Select(layer => layer.AlphaMap?.Path));
        Assert.All(paintedLayers, layer => Assert.Equal(0, layer.TextureMapAxis));

        Assert.Equal(55, level.Lights.Count(light => light.Location != Vector3.Zero));
        var light = Assert.Single(level.Lights, light => light.Name == "Light8");
        Assert.Equal(130, light.Brightness);
        Assert.Equal(50, light.Radius);
        Assert.All(level.Actors.Where(actor => actor.StaticMesh is not null), actor =>
        {
            Assert.False(string.IsNullOrWhiteSpace(actor.StaticMesh!.PackageName));
            Assert.False(string.IsNullOrWhiteSpace(actor.StaticMesh.ObjectName));
        });
    }

    [Fact]
    public void Gludin_trailing_texture_only_terrain_slot_is_ignored_when_local_source_is_available()
    {
        var source = Path.Combine(
            FindRepositoryRoot(),
            "sources",
            "Interlude",
            "maps",
            "16_25.unr");
        if (!File.Exists(source))
        {
            return;
        }

        var decoded = LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(source));
        var terrain = Assert.Single(new UnrealPackageReader(decoded).ReadLevel().Terrains);
        var selection = TerrainLayerSelector.SelectCompletePrefix(terrain.Layers);

        Assert.Equal(11, terrain.Layers.Count);
        Assert.Equal(10, terrain.Layers.Count(layer =>
            layer.Texture is not null && layer.AlphaMap is not null));
        var trailing = terrain.Layers[^1];
        Assert.Equal(10, trailing.Index);
        Assert.Equal("T_sland.WR_02", trailing.Texture?.Path);
        Assert.Null(trailing.AlphaMap);
        Assert.Null(selection.Error);
        Assert.Equal(10, selection.Layers.Count);
        Assert.Equal([10], selection.IgnoredTrailingLayerIndices);
    }

    [Fact]
    public void Terrain_incomplete_layer_before_a_complete_layer_is_rejected()
    {
        var texture = new UnrealObjectReference("T", "Diffuse", "Texture");
        var alpha = new UnrealObjectReference("T", "Alpha", "Texture");
        var layers = new[]
        {
            TerrainLayer(0, texture, alpha),
            TerrainLayer(1, texture, null),
            TerrainLayer(2, texture, alpha)
        };

        var selection = TerrainLayerSelector.SelectCompletePrefix(layers);

        Assert.Empty(selection.Layers);
        Assert.Contains("before a later complete layer: 1", selection.Error);
    }

    [Fact]
    public void Rune_terrain_retains_yz_projection_when_local_source_is_available()
    {
        var source = Path.Combine(
            FindRepositoryRoot(),
            "sources",
            "Interlude",
            "maps",
            "20_17.unr");
        if (!File.Exists(source))
        {
            return;
        }

        var decoded = LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(source));
        var terrain = Assert.Single(new UnrealPackageReader(decoded).ReadLevel().Terrains);
        var layers = terrain.Layers
            .Where(layer => layer.Texture is not null && layer.AlphaMap is not null)
            .ToArray();
        Assert.Equal(12, layers.Length);
        var layer = Assert.Single(layers, layer => layer.Index == 11);
        Assert.Equal(2, layer.TextureMapAxis);

        var transform = UnrealTerrainUvTransformBuilder.Build(
            terrain.ToWorld,
            terrain.ToHeightMap,
            terrain.Location,
            layer);
        Assert.All(
            new[]
            {
                transform.U.X, transform.U.Y, transform.U.Z, transform.U.Offset,
                transform.V.X, transform.V.Y, transform.V.Z, transform.V.Offset
            },
            value => Assert.True(float.IsFinite(value)));
    }

    [Fact]
    public void G16_terrain_is_converted_to_scaled_geometry()
    {
        var heightmap = new UnrealTexture(
            "height",
            UnrealTextureFormat.G16,
            2,
            2,
            [0x00, 0x80, 0x80, 0x80, 0x00, 0x81, 0x80, 0x81]);

        var toWorld = new UnrealCoordinateFrame(
            new Vector3(0, 0, 32768),
            new Vector3(2, 0, 0),
            new Vector3(0, 3, 0),
            new Vector3(0, 0, 0.5f));
        var terrain = UnrealTerrainMeshBuilder.Build(heightmap, toWorld, Vector3.Zero);

        Assert.Equal(4, terrain.Positions.Count);
        Assert.Equal(new Vector3(2, 0, 64), terrain.Positions[1]);
        Assert.Equal(new Vector3(0, 3, 128), terrain.Positions[2]);
        Assert.Equal(6, terrain.Indices.Count);
        Assert.All(terrain.Normals, normal => Assert.True(float.IsFinite(normal.X)));
    }

    [Fact]
    public void Bsp_builder_preserves_winding_uvs_flags_and_isolates_invalid_surfaces()
    {
        var material = new UnrealObjectReference("T", "Stone", "Texture");
        var model = new UnrealModelData(
            "World",
            [new Vector3(256, 0, 0), new Vector3(0, 256, 0)],
            [Vector3.Zero, Vector3.UnitX, Vector3.One, Vector3.UnitY],
            [
                new UnrealModelNode(0, 0, 4, Vector3.UnitZ),
                new UnrealModelNode(4, 1, 4, Vector3.UnitZ),
                new UnrealModelNode(8, 2, 4, Vector3.UnitZ),
                new UnrealModelNode(12, 3, 4, Vector3.UnitZ),
                new UnrealModelNode(16, 4, 4, Vector3.UnitZ),
                new UnrealModelNode(99, 0, 4, Vector3.UnitZ),
                new UnrealModelNode(0, 99, 4, Vector3.UnitZ),
                new UnrealModelNode(0, 0, 0, Vector3.UnitZ)
            ],
            [
                Surface(material, UnrealPolyFlags.TwoSided) with
                {
                    Material = null,
                    RawMaterialReference = -51,
                    MaterialReferenceInvalid = true
                },
                Surface(material, UnrealPolyFlags.Invisible),
                Surface(material, UnrealPolyFlags.Portal),
                Surface(material, UnrealPolyFlags.FakeBackdrop),
                Surface(material, UnrealPolyFlags.None, textureU: 99)
            ],
            Enumerable.Repeat(new[] { 0, 1, 2, 3 }, 5).SelectMany(value => value).ToArray());

        var result = UnrealBspMeshBuilder.Build(model, UnrealModelSurfaceSelection.World);

        var chunk = Assert.Single(result.Chunks);
        Assert.Equal(UnrealPolyFlags.TwoSided, chunk.RenderFlags);
        Assert.Equal(UnrealBspMeshRole.Geometry, chunk.Role);
        Assert.Equal([0, 2, 1, 0, 3, 2], chunk.Mesh.Indices);
        Assert.Equal(
            [Vector2.Zero, Vector2.UnitX, Vector2.One, Vector2.UnitY],
            chunk.Mesh.TextureCoordinates);
        Assert.Equal(1, result.Diagnostics.SplitterNodeCount);
        Assert.Equal(1, result.Diagnostics.InvisibleSurfaceCount);
        Assert.Equal(1, result.Diagnostics.PortalSurfaceCount);
        Assert.Equal(1, result.Diagnostics.FakeBackdropSurfaceCount);
        Assert.Equal(3, result.Diagnostics.MalformedSurfaceCount);
        Assert.Equal(1, result.Diagnostics.UnresolvedMaterialReferenceCount);

        var backdrop = UnrealBspMeshBuilder.Build(
            model,
            UnrealModelSurfaceSelection.FakeBackdrop);
        Assert.Single(backdrop.Chunks);
        var nonFinite = UnrealBspMeshBuilder.Build(
            model with
            {
                Points =
                [new Vector3(float.NaN, 0, 0), Vector3.UnitX, Vector3.One, Vector3.UnitY]
            },
            UnrealModelSurfaceSelection.World);
        Assert.Empty(nonFinite.Chunks);
        Assert.True(nonFinite.Diagnostics.MalformedSurfaceCount > 0);
    }

    [Fact]
    public void Bsp_builder_classifies_the_native_map_floor_by_geometry()
    {
        var points = new[]
        {
            new Vector3(-327680, -262144, -16384),
            new Vector3(327680, -262144, -16384),
            new Vector3(327680, 262144, -16384),
            new Vector3(-327680, 262144, -16384)
        };
        var model = new UnrealModelData(
            "World",
            [new Vector3(256, 0, 0), new Vector3(0, 256, 0)],
            points,
            [new UnrealModelNode(0, 0, 4, Vector3.UnitZ)],
            [Surface(null, UnrealPolyFlags.None)],
            [0, 1, 2, 3]);

        var worldBase = Assert.Single(
            UnrealBspMeshBuilder.Build(model, UnrealModelSurfaceSelection.World).Chunks);
        Assert.Equal(UnrealBspMeshRole.WorldBase, worldBase.Role);

        var localFloor = Assert.Single(
            UnrealBspMeshBuilder.Build(
                model with { Points = points.Select(point => point / 2).ToArray() },
                UnrealModelSurfaceSelection.World).Chunks);
        Assert.Equal(UnrealBspMeshRole.Geometry, localFloor.Role);
    }

    [Fact]
    public void Bsp_builder_splits_material_groups_at_deterministic_ushort_boundaries()
    {
        const int polygonCount = 16_384;
        var vertices = Enumerable.Repeat(new[] { 0, 1, 2, 3 }, polygonCount)
            .SelectMany(value => value)
            .ToArray();
        var nodes = Enumerable.Range(0, polygonCount)
            .Select(index => new UnrealModelNode(index * 4, 0, 4, Vector3.UnitZ))
            .ToArray();
        var model = new UnrealModelData(
            "LargeWorld",
            [new Vector3(256, 0, 0), new Vector3(0, 256, 0)],
            [Vector3.Zero, Vector3.UnitX, Vector3.One, Vector3.UnitY],
            nodes,
            [Surface(null, UnrealPolyFlags.None)],
            vertices);

        var first = UnrealBspMeshBuilder.Build(model, UnrealModelSurfaceSelection.World);
        var second = UnrealBspMeshBuilder.Build(model, UnrealModelSurfaceSelection.World);

        Assert.Equal([65_532, 4], first.Chunks.Select(chunk => chunk.Mesh.Positions.Count));
        Assert.Equal([16_383, 1], first.Chunks.Select(chunk => chunk.SurfaceCount));
        Assert.Equal(
            first.Chunks.Select(chunk => (chunk.Name, chunk.Mesh.Positions.Count, chunk.Mesh.Indices.Count)),
            second.Chunks.Select(chunk => (chunk.Name, chunk.Mesh.Positions.Count, chunk.Mesh.Indices.Count)));
    }

    [Theory]
    [InlineData(0, 1, 0, 0, 0, 0, 1)]
    [InlineData(1, 0, 1, 0, 1, 0, 0)]
    [InlineData(2, 0, 1, 0, 0, 0, 1)]
    public void Terrain_projection_axes_are_expressed_in_local_gltf_coordinates(
        byte axis,
        float ux,
        float uy,
        float uz,
        float vx,
        float vy,
        float vz)
    {
        var frame = new UnrealCoordinateFrame(
            Vector3.Zero,
            Vector3.UnitX,
            Vector3.UnitY,
            Vector3.UnitZ);
        var layer = TerrainLayer(axis: axis);

        var transform = UnrealTerrainUvTransformBuilder.Build(
            frame,
            frame,
            Vector3.Zero,
            layer);

        AssertRow(new UnrealTerrainUvTransformRow(ux, uy, uz, 0), transform.U);
        AssertRow(new UnrealTerrainUvTransformRow(vx, vy, vz, 0), transform.V);
    }

    [Fact]
    public void Terrain_projection_applies_scale_pan_texture_rotation_and_full_layer_rotation()
    {
        var frame = new UnrealCoordinateFrame(
            Vector3.Zero,
            Vector3.UnitX,
            Vector3.UnitY,
            Vector3.UnitZ);
        var scaled = UnrealTerrainUvTransformBuilder.Build(
            frame,
            frame,
            Vector3.Zero,
            TerrainLayer(uScale: 2, vScale: 4, uPan: 3, vPan: 5));
        AssertRow(new UnrealTerrainUvTransformRow(0.5f, 0, 0, -3), scaled.U);
        AssertRow(new UnrealTerrainUvTransformRow(0, 0, 0.25f, -5), scaled.V);

        var rotated = UnrealTerrainUvTransformBuilder.Build(
            frame,
            frame,
            new Vector3(10, 20, 30),
            TerrainLayer(
                axis: 2,
                textureRotation: 16384,
                layerRotation: new UnrealRotator(4096, 8192, 2048)));
        Assert.All(
            new[]
            {
                rotated.U.X, rotated.U.Y, rotated.U.Z, rotated.U.Offset,
                rotated.V.X, rotated.V.Y, rotated.V.Z, rotated.V.Offset
            },
            value => Assert.True(float.IsFinite(value)));
        Assert.NotEqual(new UnrealTerrainUvTransformRow(0, 1, 0, 0), rotated.U);
    }

    [Fact]
    public void Terrain_projection_rejects_unknown_axes_and_invalid_scales()
    {
        var frame = new UnrealCoordinateFrame(
            Vector3.Zero,
            Vector3.UnitX,
            Vector3.UnitY,
            Vector3.UnitZ);

        Assert.Throws<InvalidDataException>(() => UnrealTerrainUvTransformBuilder.Build(
            frame,
            frame,
            Vector3.Zero,
            TerrainLayer(axis: 3)));
        Assert.Throws<InvalidDataException>(() => UnrealTerrainUvTransformBuilder.Build(
            frame,
            frame,
            Vector3.Zero,
            TerrainLayer(uScale: 0)));
    }

    [Fact]
    public void Closed_brush_faces_are_validated_and_triangulated_with_ue2_winding()
    {
        Vector3[] points =
        [
            new(0, 0, 0),
            new(1, 0, 0),
            new(0, 1, 0),
            new(0, 0, 1)
        ];
        UnrealBrushFace[] faces =
        [
            new([0, 1, 2], -Vector3.UnitZ),
            new([0, 3, 1], -Vector3.UnitY),
            new([1, 3, 2], Vector3.Normalize(Vector3.One)),
            new([2, 3, 0], -Vector3.UnitX)
        ];

        var geometry = UnrealBrushGeometryBuilder.Build("Tetrahedron", points, faces);

        Assert.Equal(12, geometry.Positions.Count);
        Assert.Equal(4, geometry.TriangleCount);
        Assert.Equal<ushort>([0, 2, 1], geometry.Indices.Take(3));
        Assert.All(geometry.Normals, normal => Assert.InRange(normal.Length(), 0.999f, 1.001f));
    }

    [Fact]
    public void Invalid_or_open_brush_faces_are_rejected_independently()
    {
        Vector3[] points = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY];

        Assert.Throws<InvalidDataException>(() => UnrealBrushGeometryBuilder.Build(
            "InvalidIndex",
            points,
            [new UnrealBrushFace([0, 1, 3], Vector3.UnitZ)]));
        Assert.Throws<InvalidDataException>(() => UnrealBrushGeometryBuilder.Build(
            "Degenerate",
            points,
            [new UnrealBrushFace([0, 1, 1], Vector3.UnitZ)]));
        Assert.Throws<InvalidDataException>(() => UnrealBrushGeometryBuilder.Build(
            "Open",
            points,
            [new UnrealBrushFace([0, 1, 2], Vector3.UnitZ)]));
    }

    [Theory]
    [InlineData("17_25.unr", true)]
    [InlineData("Lobby.unr", false)]
    [InlineData("entry.UNR", false)]
    [InlineData("ship_position.unr", false)]
    [InlineData("skylevel.unr", false)]
    public void Unreal_packages_are_classified_by_coordinate_name(string fileName, bool worldLevel)
    {
        Assert.Equal(worldLevel, UnrealPackageKindClassifier.IsWorldLevel(fileName));
        Assert.Equal(!worldLevel, UnrealPackageKindClassifier.IsScene(fileName));
    }

    [Fact]
    public void Coordinate_level_BSP_compatibility_audit_is_non_publishing()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("L2_RUN_MAP_AUDIT"),
                "1",
                StringComparison.Ordinal))
            return;
        var maps = Path.Combine(FindRepositoryRoot(), "sources", "Interlude", "maps");
        if (!Directory.Exists(maps)) return;
        var sources = Directory.EnumerateFiles(maps, "*.unr")
            .Where(UnrealPackageKindClassifier.IsWorldLevel)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.Equal(153, sources.Length);
        var diagnostics = new List<string>();
        var malformedSurfaceCount = 0;
        var unresolvedMaterialReferenceCount = 0;
        var worldBaseCount = 0;
        var skyZoneMapCount = 0;
        var skyZoneChunkCount = 0;
        var waterSurfaceMapCount = 0;
        var waterSurfaceChunkCount = 0;
        foreach (var source in sources)
        {
            try
            {
                var decoded = LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(source));
                var level = new UnrealPackageReader(decoded).ReadLevel();
                var models = level.BspModels;
                malformedSurfaceCount += models.Sum(model => model.Diagnostics.MalformedSurfaceCount);
                unresolvedMaterialReferenceCount += models.Sum(
                    model => model.Diagnostics.UnresolvedMaterialReferenceCount);
                var worldBases = models
                    .SelectMany(model => model.Chunks)
                    .Where(chunk => chunk.Role == UnrealBspMeshRole.WorldBase)
                    .ToArray();
                Assert.InRange(worldBases.Length, 0, 1);
                worldBaseCount += worldBases.Length;
                var skyChunks = models
                    .SelectMany(model => model.Chunks)
                    .Where(chunk => chunk.Role == UnrealBspMeshRole.SkyZone)
                    .ToArray();
                if (skyChunks.Length > 0) skyZoneMapCount++;
                skyZoneChunkCount += skyChunks.Length;
                Assert.All(skyChunks, chunk =>
                {
                    Assert.NotNull(chunk.SkyZoneName);
                    Assert.Contains(level.SkyZones, zone => zone.Name == chunk.SkyZoneName);
                });
                var waterSurfaces = models
                    .SelectMany(model => model.Chunks)
                    .Where(chunk => chunk.Role == UnrealBspMeshRole.WaterSurface)
                    .ToArray();
                if (waterSurfaces.Length > 0) waterSurfaceMapCount++;
                waterSurfaceChunkCount += waterSurfaces.Length;
                Assert.All(
                    waterSurfaces.SelectMany(chunk => chunk.WaterVolumeNames ?? []),
                    name => Assert.Contains(level.WaterVolumes, volume => volume.Name == name));
                foreach (var model in models.Where(model => model.Error is not null))
                {
                    diagnostics.Add(
                        $"{Path.GetFileName(source)}/{model.Name}: {model.Error}");
                }
                Assert.All(
                    models.SelectMany(model => model.Chunks),
                    chunk => Assert.InRange(chunk.Mesh.Positions.Count, 1, ushort.MaxValue));
            }
            catch (Exception exception) when (exception is InvalidDataException or OverflowException)
            {
                diagnostics.Add($"{Path.GetFileName(source)}: {exception.Message}");
            }
        }
        Console.WriteLine(
            diagnostics.Count == 0
                ? $"BSP compatibility audit: {sources.Length} coordinate maps, no structural decoder failures; " +
                  $"{malformedSurfaceCount} malformed surfaces skipped independently and " +
                  $"{unresolvedMaterialReferenceCount} material references retained with neutral fallback; " +
                  $"{skyZoneChunkCount} sky-zone chunks across {skyZoneMapCount} maps and " +
                  $"{waterSurfaceChunkCount} water-surface chunks across {waterSurfaceMapCount} maps."
                : $"BSP compatibility audit: {sources.Length} coordinate maps, {diagnostics.Count} diagnostics.\n" +
                  string.Join("\n", diagnostics));
        Assert.Empty(diagnostics);
        Assert.Equal(70, malformedSurfaceCount);
        Assert.Equal(13, unresolvedMaterialReferenceCount);
        Assert.Equal(144, worldBaseCount);
        Assert.Equal(148, skyZoneMapCount);
        Assert.Equal(591, skyZoneChunkCount);
        Assert.Equal(122, waterSurfaceMapCount);
        Assert.Equal(132, waterSurfaceChunkCount);
    }

    [Theory]
    [InlineData("24_10.unr")]
    [InlineData("25_10.unr")]
    public void Previously_overflowing_coordinate_levels_can_be_read_when_local_source_is_available(string fileName)
    {
        var source = Path.Combine(FindRepositoryRoot(), "sources", "Interlude", "maps", fileName);
        if (!File.Exists(source)) return;

        var decoded = LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(source));
        var level = new UnrealPackageReader(decoded).ReadLevel();

        Assert.NotEmpty(level.Terrains);
        Assert.NotEmpty(level.BspModels);
    }

    [Theory]
    [InlineData("23_10.unr")]
    [InlineData("24_10.unr")]
    [InlineData("25_10.unr")]
    [InlineData("26_11.unr")]
    [InlineData("26_12.unr")]
    public void Coordinate_levels_with_stock_UE2_model_surfaces_can_be_read_when_local_source_is_available(
        string fileName)
    {
        var source = Path.Combine(FindRepositoryRoot(), "sources", "Interlude", "maps", fileName);
        if (!File.Exists(source)) return;

        var decoded = LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(source));
        var model = Assert.Single(new UnrealPackageReader(decoded).ReadLevel().BspModels);

        Assert.Null(model.Error);
        Assert.NotEmpty(model.Chunks);
    }

    [Fact]
    public void Lobby_scene_can_be_read_when_local_source_is_available()
    {
        var source = Path.Combine(
            FindRepositoryRoot(),
            "sources",
            "Interlude",
            "maps",
            "Lobby.unr");
        if (!File.Exists(source))
        {
            return;
        }

        var decoded = LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(source));
        var scene = new UnrealPackageReader(decoded).ReadScene();

        var terrain = Assert.Single(scene.Level.Terrains);
        Assert.Equal(22, terrain.Layers.Count(layer => layer.Texture is not null && layer.AlphaMap is not null));
        Assert.Equal(1352, scene.Level.Actors.Count);
        Assert.NotEmpty(scene.Cameras);
        Assert.NotEmpty(scene.InterpolationPoints);
        Assert.Contains(scene.InterpolationPoints, point => point.Name.EndsWith("InterpolationPoint5"));
        Assert.NotEmpty(scene.SceneManagers);
        Assert.NotEmpty(scene.Actions);
        Assert.Contains(scene.SceneManagers, manager => manager.Properties.ContainsKey("Actions"));
        Assert.Contains(scene.Actions, action => action.Target is not null);
        Assert.NotEmpty(scene.AmbientSounds);
        Assert.NotEmpty(scene.Effects);
        Assert.Equal(75, scene.Effects.Count);
        var bsp = Assert.Single(scene.Level.BspModels);
        Assert.Null(bsp.Error);
        Assert.Contains(
            bsp.Chunks,
            chunk => chunk.Role is UnrealBspMeshRole.Geometry or UnrealBspMeshRole.WorldBase);
        Assert.All(bsp.Chunks, chunk =>
        {
            Assert.NotEmpty(chunk.Mesh.Positions);
            Assert.NotEmpty(chunk.Mesh.TextureCoordinates);
        });
        Assert.True(
            scene.SkyBackdrops.Count > 0,
            $"Expected fake-backdrop BSP; parsed {scene.SkyBackdrops.Count} models. " +
            string.Join(" | ", scene.SkyBackdrops.Select(item => $"{item.Name}: {item.Error}")));
        Assert.All(scene.SkyBackdrops, backdrop => Assert.NotNull(backdrop.Mesh));
        Assert.Single(scene.SkyBackdrops);
        Assert.Single(scene.SkyBackdrops[0].Mesh!.Sections);
        Assert.NotEmpty(scene.SkyBackdrops[0].Mesh!.TextureCoordinates);
        Assert.All(
            scene.Effects.Where(effect => effect.ClassName is "SpriteEmitter" or "BeamEmitter"),
            effect => Assert.NotNull(effect.Owner));
        var declaredOwners = scene.Effects
            .Where(effect => effect.ClassName == "Emitter" && effect.Properties.ContainsKey("Emitters"))
            .SelectMany(owner => owner.Properties["Emitters"]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(child => (Child: child, Owner: owner.Name)))
            .ToDictionary(item => item.Child, item => item.Owner, StringComparer.OrdinalIgnoreCase);
        Assert.All(
            scene.Effects.Where(effect => effect.ClassName is "SpriteEmitter" or "BeamEmitter"),
            effect => Assert.Equal(declaredOwners[effect.Name], effect.Owner));
        Assert.Equal("Emitter58", scene.Effects.Single(effect => effect.Name == "BeamEmitter0").Owner);
        Assert.Equal("Emitter22", scene.Effects.Single(effect => effect.Name == "BeamEmitter4").Owner);
        Assert.Equal("Emitter6", scene.Effects.Single(effect => effect.Name == "SpriteEmitter10").Owner);
        var movingSmoke = scene.Effects.Single(effect => effect.Name == "SpriteEmitter47");
        Assert.Equal("20,20", movingSmoke.Properties["LifetimeRange"]);
        Assert.Equal("-1.333,-1.333,5;1.333,1.333,50", movingSmoke.Properties["StartVelocityRange"]);
        var beamEffects = scene.Effects.Where(effect => effect.ClassName == "BeamEmitter").ToArray();
        Assert.Equal(6, beamEffects.Length);
        Assert.All(beamEffects, effect =>
        {
            Assert.Equal("2", effect.Properties.GetValueOrDefault("DetermineEndPointBy"));
            Assert.False(
                string.IsNullOrWhiteSpace(effect.Properties.GetValueOrDefault("BeamEndPoints")),
                $"{effect.Name} lost BeamEndPoints.");
            var particle = Assert.IsType<ParticleEmitterManifestEntry>(
                AssetImportJobProcessor.ParticleSettings(effect, effect.Properties));
            Assert.Equal("beam", particle.Kind);
            Assert.Null(particle.Sprite);
            Assert.NotNull(particle.Beam);
            Assert.Equal("offset", particle.Beam.EndPointMode);
            Assert.NotEmpty(particle.Beam.EndPoints);
            Assert.All(particle.Beam.EndPoints, endpoint =>
                Assert.True(endpoint.Offset.Max.X != 0 || endpoint.Offset.Max.Y != 0 || endpoint.Offset.Max.Z != 0));
            Assert.Equal("translucent", particle.DrawStyle);
            Assert.Empty(particle.Diagnostics);
        });
        var spriteEffects = scene.Effects.Where(effect => effect.ClassName == "SpriteEmitter").ToArray();
        Assert.Equal(33, spriteEffects.Length);
        Assert.All(spriteEffects, effect =>
        {
            var particle = Assert.IsType<ParticleEmitterManifestEntry>(
                AssetImportJobProcessor.ParticleSettings(effect, effect.Properties));
            Assert.Equal("sprite", particle.Kind);
            Assert.NotNull(particle.Sprite);
            Assert.Null(particle.Beam);
            Assert.Contains(particle.DrawStyle, new[] { "alpha-blend", "translucent", "darken", "brighten" });
            Assert.Contains(particle.Sprite.DirectionMode, new[] { "none", "up", "normal" });
            Assert.Contains(particle.Sprite.StartLocationShape, new[] { "box", "sphere" });
            Assert.Contains(particle.Sprite.RotationSource, new[] { "none", "normal" });
            Assert.Empty(particle.Diagnostics);
        });
        Assert.Contains(spriteEffects, effect =>
            effect.Properties.TryGetValue("StartVelocityRange", out var velocity) &&
            velocity != "0,0,0;0,0,0");
        var colorScaledEffects = scene.Effects
            .Where(effect => effect.Properties.GetValueOrDefault("UseColorScale") == "True").ToArray();
        Assert.NotEmpty(colorScaledEffects);
        Assert.All(
            colorScaledEffects,
            effect => Assert.False(
                string.IsNullOrWhiteSpace(effect.Properties.GetValueOrDefault("ColorScale")),
                $"{effect.Name} lost ColorScale keys."));
        var sizeScaledEffects = scene.Effects
            .Where(effect => effect.Properties.GetValueOrDefault("UseSizeScale") == "True").ToArray();
        Assert.NotEmpty(sizeScaledEffects);
        Assert.All(
            sizeScaledEffects,
            effect => Assert.False(
                string.IsNullOrWhiteSpace(effect.Properties.GetValueOrDefault("SizeScale")),
                $"{effect.Name} lost SizeScale keys."));
        Assert.Contains(scene.Effects, effect => effect.ClassName == "Projector");
        Assert.Contains(scene.Effects, effect => effect.ClassName == "NSun");
        Assert.Contains(scene.Effects, effect => effect.ClassName == "NMoon");
        Assert.NotNull(scene.Level.Environment);
        Assert.NotNull(scene.Level.Environment!.DistanceFog);
        Assert.Null(scene.Level.EnvironmentWarning);
        Assert.Contains(scene.Level.Actors, actor => actor.VertexLighting is { Count: > 0 });
    }

    [Theory]
    [InlineData("entry.unr")]
    [InlineData("ship_position.unr")]
    [InlineData("skylevel.unr")]
    public void Support_scene_can_be_read_when_local_source_is_available(string fileName)
    {
        var source = Path.Combine(
            FindRepositoryRoot(),
            "sources",
            "Interlude",
            "maps",
            fileName);
        if (!File.Exists(source))
        {
            return;
        }

        var decoded = LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(source));
        _ = new UnrealPackageReader(decoded).ReadScene();
    }

    [Fact]
    public void Sky_level_retains_authored_sky_zone_when_local_source_is_available()
    {
        var source = Path.Combine(
            FindRepositoryRoot(),
            "sources",
            "Interlude",
            "maps",
            "skylevel.unr");
        if (!File.Exists(source))
        {
            return;
        }

        var decoded = LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(source));
        var scene = new UnrealPackageReader(decoded).ReadScene();

        var skyZone = Assert.Single(scene.SkyZones);
        Assert.Equal("SkyZoneInfo0", skyZone.Name);
        AssertVector(new Vector3(324.20276f, 264435.16f, 24535.02f), skyZone.Location);
        Assert.Equal(0.2f, skyZone.DrawScale);
        Assert.Equal(0.2f, skyZone.TexUPanSpeed);
        Assert.Equal(0.2f, skyZone.TexVPanSpeed);
        Assert.Equal(9, skyZone.LensFlares.Count);
        Assert.Equal(Enumerable.Range(0, 9), skyZone.LensFlares.Select(flare => flare.Index));
        Assert.Equal(
            ["Flare02", "Flare02", "Flare02", "Flare02", "Flare01", "Flare03", "Flare04", "Flare06", "Flare06"],
            skyZone.LensFlares.Select(flare => flare.Texture.ObjectName.Split('.')[^1]));
        Assert.Equal(
            [-0.12f, -0.1f, 0.1f, 0.12f, 0.17f, 0.2f, 0.4f, 0.5f, 0.6f],
            skyZone.LensFlares.Select(flare => flare.Offset));
        Assert.Equal(
            [0.5f, 0.38f, 0.3f, 0.4f, 1, 1, 1, 2, 3],
            skyZone.LensFlares.Select(flare => flare.Scale));
        Assert.DoesNotContain("SkyZoneInfo", scene.Level.UnrepresentedObjectClasses.Keys);
    }

    private static void AssertVector(Vector3 expected, Vector3 actual)
    {
        Assert.InRange(Math.Abs(expected.X - actual.X), 0, 0.001f);
        Assert.InRange(Math.Abs(expected.Y - actual.Y), 0, 0.001f);
        Assert.InRange(Math.Abs(expected.Z - actual.Z), 0, 0.001f);
    }

    private static void AssertFinite(Vector3 value)
    {
        Assert.True(float.IsFinite(value.X));
        Assert.True(float.IsFinite(value.Y));
        Assert.True(float.IsFinite(value.Z));
    }

    private static UnrealModelSurface Surface(
        UnrealObjectReference? material,
        UnrealPolyFlags flags,
        int textureU = 0) => new(material, 0, false, flags, 0, 0, textureU, 1);

    private static void AssertRow(
        UnrealTerrainUvTransformRow expected,
        UnrealTerrainUvTransformRow actual)
    {
        Assert.InRange(Math.Abs(expected.X - actual.X), 0, 0.0001f);
        Assert.InRange(Math.Abs(expected.Y - actual.Y), 0, 0.0001f);
        Assert.InRange(Math.Abs(expected.Z - actual.Z), 0, 0.0001f);
        Assert.InRange(Math.Abs(expected.Offset - actual.Offset), 0, 0.0001f);
    }

    private static UnrealTerrainLayer TerrainLayer(
        byte axis = 0,
        float uScale = 1,
        float vScale = 1,
        float uPan = 0,
        float vPan = 0,
        float textureRotation = 0,
        UnrealRotator layerRotation = default) => new(
            0,
            null,
            null,
            uScale,
            vScale,
            uPan,
            vPan,
            axis,
            textureRotation,
            layerRotation);

    private static UnrealTerrainLayer TerrainLayer(
        int index,
        UnrealObjectReference? texture,
        UnrealObjectReference? alpha) => new(
            index,
            texture,
            alpha,
            1,
            1,
            0,
            0,
            0,
            0,
            default);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "L2Web.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
