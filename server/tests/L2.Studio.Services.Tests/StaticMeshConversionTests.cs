using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;
using L2.Tools.PackageReader;
using L2.Tools.StaticMeshConverter;
using L2.Studio.Services;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class StaticMeshConversionTests
{
    [Fact]
    public void Glb_conversion_changes_handedness_and_keeps_front_faces_aligned_with_normals()
    {
        var mesh = new UnrealStaticMesh(
            "triangle",
            [Vector3.Zero, Vector3.UnitY, Vector3.UnitX],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [],
            [0, 1, 2],
            [new UnrealStaticMeshSection(0, 3)]);

        var glb = GlbStaticMeshEncoder.Encode(mesh);
        var binaryChunk = ReadBinaryChunk(glb);
        var position0 = ReadVector3(binaryChunk, 0);
        var position1 = ReadVector3(binaryChunk, 12);
        var position2 = ReadVector3(binaryChunk, 24);
        var normal0 = ReadVector3(binaryChunk, 36);
        var faceNormal = Vector3.Cross(position1 - position0, position2 - position0);

        Assert.Equal(Vector3.UnitZ, position1);
        Assert.Equal(Vector3.UnitY, normal0);
        Assert.True(Vector3.Dot(faceNormal, normal0) > 0);
    }

    [Fact]
    public void Glb_conversion_assigns_classic_materials_to_mesh_sections()
    {
        var mesh = new UnrealStaticMesh(
            "textured-triangle",
            [Vector3.Zero, Vector3.UnitY, Vector3.UnitX],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [Vector2.Zero, Vector2.UnitY, Vector2.UnitX],
            [0, 1, 2],
            [new UnrealStaticMeshSection(0, 3)]);
        var binding = new StaticMeshMaterialBinding(
            "foliage",
            "/textures/field/leaf.webp?v=123",
            "/textures/field/leaf-mask.webp?v=456",
            "/textures/field/leaf-glow.webp?v=789",
            StaticMeshBlendMode.Masked,
            true,
            0.25f,
            true,
            true,
            StaticMeshOpacitySource.Texture,
            StaticMeshOpacityChannel.Alpha,
            PanRate: 0.2f,
            PanRateV: -0.1f,
            DiffuseAnimation: new StaticMeshTextureAnimation(
                ["/textures/field/leaf-0.webp", "/textures/field/leaf-1.webp"],
                12),
            WindMode: StaticMeshWindMode.Foliage);

        var glb = GlbStaticMeshEncoder.Encode(mesh, [binding]);
        using var json = ReadJsonChunk(glb);
        var root = json.RootElement;
        var material = root.GetProperty("materials")[0];

        Assert.Equal(0, root.GetProperty("meshes")[0].GetProperty("primitives")[0].GetProperty("material").GetInt32());
        Assert.Equal("MASK", material.GetProperty("alphaMode").GetString());
        Assert.Equal(0.25f, material.GetProperty("alphaCutoff").GetSingle());
        Assert.True(material.GetProperty("doubleSided").GetBoolean());
        Assert.Equal("masked", material.GetProperty("extras").GetProperty("l2").GetProperty("blendMode").GetString());
        Assert.Equal("texture", material.GetProperty("extras").GetProperty("l2").GetProperty("opacitySource").GetString());
        Assert.Equal("alpha", material.GetProperty("extras").GetProperty("l2").GetProperty("opacityChannel").GetString());
        Assert.False(material.GetProperty("extras").GetProperty("l2").GetProperty("unlit").GetBoolean());
        Assert.Equal("foliage", material.GetProperty("extras").GetProperty("l2").GetProperty("windMode").GetString());
        Assert.Equal(0.2f, material.GetProperty("extras").GetProperty("l2").GetProperty("panRate").GetSingle());
        Assert.Equal(-0.1f, material.GetProperty("extras").GetProperty("l2").GetProperty("panRateV").GetSingle());
        Assert.Equal(12, material.GetProperty("extras").GetProperty("l2")
            .GetProperty("diffuseAnimation").GetProperty("frameRate").GetSingle());
        Assert.Equal(2, root.GetProperty("images").GetArrayLength());
        Assert.Equal("textures/field/leaf.webp?v=123", root.GetProperty("images")[0].GetProperty("uri").GetString());
    }

    [Fact]
    public void Glb_conversion_preserves_both_native_vertex_color_streams()
    {
        var mesh = new UnrealStaticMesh(
            "lit-triangle",
            [Vector3.Zero, Vector3.UnitY, Vector3.UnitX],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [],
            [0, 1, 2],
            [new UnrealStaticMeshSection(0, 3)],
            [new UnrealColor(1, 2, 3, 4), new UnrealColor(5, 6, 7, 8), new UnrealColor(9, 10, 11, 12)],
            [new UnrealColor(13, 14, 15, 16), new UnrealColor(17, 18, 19, 20), new UnrealColor(21, 22, 23, 24)]);

        using var json = ReadJsonChunk(GlbStaticMeshEncoder.Encode(mesh));
        var attributes = json.RootElement.GetProperty("meshes")[0]
            .GetProperty("primitives")[0]
            .GetProperty("attributes");

        Assert.True(attributes.TryGetProperty("COLOR_0", out _));
        Assert.True(attributes.TryGetProperty("COLOR_1", out _));
    }

    [Fact]
    public void Material_resolver_flattens_shader_and_final_blend_graphs()
    {
        var texture = new TextureManifestEntry(
            "world",
            "leaf",
            "/textures/world/leaf.webp?v=123",
            64,
            64,
            "dxt1",
            "hash",
            "resolved",
            null);
        var textureReference = new TextureMaterialReference("world", "leaf", "Texture");
        var shaderReference = new TextureMaterialReference("world", "leaf-shader", "Shader");
        var pannerReference = new TextureMaterialReference("world", "leaf-panner", "Panner");
        var shader = new TextureMaterialManifestEntry(
            "world",
            "leaf-shader",
            "Shader",
            null,
            textureReference,
            textureReference,
            null,
            1,
            0,
            true,
            true,
            64,
            true,
            true,
            Detail: textureReference,
            DetailScale: 4);
        var finalBlend = new TextureMaterialManifestEntry(
            "world",
            "leaf-final",
            "FinalBlend",
            pannerReference,
            null,
            null,
            null,
            0,
            6,
            false,
            false,
            128,
            false,
            true);
        var panner = new TextureMaterialManifestEntry(
            "world",
            "leaf-panner",
            "Panner",
            shaderReference,
            null,
            null,
            null,
            0,
            0,
            false,
            false,
            128,
            true,
            true,
            PanRate: 0.25f);
        var manifest = new TextureManifest(
            2,
            "textures",
            "textures",
            "source",
            121,
            [],
            [texture],
            [shader, panner, finalBlend]);
        var mesh = new UnrealStaticMesh(
            "plant",
            [Vector3.Zero, Vector3.UnitY, Vector3.UnitX],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [],
            [0, 1, 2],
            [new UnrealStaticMeshSection(
                0,
                3,
                new UnrealObjectReference("world", "leaf-final", "FinalBlend"))]);

        var result = new StaticMeshMaterialResolver([manifest]).Resolve(mesh, "mesh-package");
        var binding = Assert.Single(result.SectionMaterials);

        Assert.Equal("resolved", result.Status);
        Assert.Equal(1, result.ResolvedMaterialCount);
        Assert.NotNull(binding);
        Assert.Equal(StaticMeshBlendMode.Additive, binding.BlendMode);
        Assert.Equal(texture.Url, binding.DiffuseUrl);
        Assert.Equal(texture.Url, binding.OpacityUrl);
        Assert.Equal(0.25f, binding.PanRate);
        Assert.Equal(texture.Url, binding.DetailUrl);
        Assert.Equal(4, binding.DetailScale);
        Assert.False(binding.DepthWrite);
    }

    [Fact]
    public void Material_resolver_preserves_color_modifiers_and_texture_oscillation()
    {
        var texture = new TextureManifestEntry(
            "world", "water", "/textures/world/water.webp?v=123", 64, 64,
            "dxt1", "hash", "resolved", null);
        var textureReference = new TextureMaterialReference("world", "water", "Texture");
        var oscillatorReference = new TextureMaterialReference("world", "water-osc", "TexOscillator");
        var oscillator = new TextureMaterialManifestEntry(
            "world", "water-osc", "TexOscillator", textureReference, null, null, null,
            0, 0, false, false, 128, true, true,
            UOscillationRate: 0.1f,
            VOscillationRate: 0.2f,
            UOscillationAmplitude: 0.05f,
            VOscillationAmplitude: 0.06f);
        var modifier = new TextureMaterialManifestEntry(
            "world", "water-color", "ColorModifier", oscillatorReference, null, null, null,
            0, 0, false, false, 128, true, true,
            ModifierColor: new TextureMaterialColor(0, 150, 206, 255));
        var resolver = new StaticMeshMaterialResolver(
            [texture],
            [oscillator, modifier]);
        var mesh = new UnrealStaticMesh(
            "water",
            [Vector3.Zero, Vector3.UnitY, Vector3.UnitX],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [],
            [0, 1, 2],
            [new UnrealStaticMeshSection(
                0,
                3,
                new UnrealObjectReference("world", "water-color", "ColorModifier"))]);

        var result = resolver.Resolve(mesh, "world");
        var binding = Assert.Single(result.SectionMaterials);

        Assert.Equal("resolved", result.Status);
        Assert.NotNull(binding);
        Assert.Equal(texture.Url, binding.DiffuseUrl);
        Assert.Equal(new StaticMeshMaterialTint(0, 150 / 255f, 206 / 255f, 1), binding.Tint);
        Assert.Equal(0.1f, binding.UvOscillation?.URate);
        Assert.Equal(0.06f, binding.UvOscillation?.VAmplitude);
    }

    [Fact]
    public void Material_resolver_preserves_combiner_fade_and_shader_channels()
    {
        var primary = new TextureManifestEntry(
            "lobby", "water", "/textures/lobby/water.webp?v=1", 64, 64,
            "dxt5", "hash", "resolved", null);
        var mask = new TextureManifestEntry(
            "lobby", "mask", "/textures/lobby/mask.webp?v=1", 64, 64,
            "dxt1", "hash", "resolved", null);
        var specular = new TextureManifestEntry(
            "lobby", "specular", "/textures/lobby/specular.webp?v=1", 64, 64,
            "dxt1", "hash", "resolved", null);
        var fade = new TextureMaterialManifestEntry(
            "lobby", "fade", "FadeColor", null, null, null, null,
            0, 0, false, false, 128, true, true,
            FadeColor1: new TextureMaterialColor(0, 64, 128, 255),
            FadeColor2: new TextureMaterialColor(0, 192, 255, 128),
            ColorFadeType: 1,
            FadePeriod: 2,
            FadePhase: 0.25f);
        var combiner = new TextureMaterialManifestEntry(
            "lobby", "combined-water", "Combiner",
            new TextureMaterialReference("lobby", "water", "Texture"),
            null, null, null, 0, 0, false, false, 128, true, true,
            Material2: new TextureMaterialReference("lobby", "fade", "FadeColor"),
            Mask: new TextureMaterialReference("lobby", "mask", "Texture"),
            CombineOperation: 6,
            AlphaOperation: 1,
            Specular: new TextureMaterialReference("lobby", "specular", "Texture"),
            PerformLightingOnSpecularPass: true,
            InvertMask: true,
            Modulate2X: true);
        var resolver = new StaticMeshMaterialResolver(
            [primary, mask, specular],
            [fade, combiner]);
        var mesh = new UnrealStaticMesh(
            "water",
            [Vector3.Zero, Vector3.UnitY, Vector3.UnitX],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [],
            [0, 1, 2],
            [new UnrealStaticMeshSection(
                0,
                3,
                new UnrealObjectReference("lobby", "combined-water", "Combiner"))]);

        var result = resolver.Resolve(mesh, "lobby");
        var binding = Assert.Single(result.SectionMaterials);

        Assert.Equal("resolved", result.Status);
        Assert.NotNull(binding);
        Assert.Equal(primary.Url, binding.DiffuseUrl);
        Assert.Equal(specular.Url, binding.SpecularUrl);
        Assert.True(binding.PerformLightingOnSpecularPass);
        var composite = Assert.IsType<StaticMeshMaterialComposite>(binding.Composite);
        Assert.Equal(mask.Url, composite.MaskUrl);
        Assert.Equal((byte)6, composite.ColorOperation);
        Assert.Equal((byte)1, composite.AlphaOperation);
        Assert.True(composite.InvertMask);
        Assert.Equal(2, composite.ModulateScale);
        Assert.Equal(2, composite.SecondaryFade?.Period);
        Assert.Equal(0.25f, composite.SecondaryFade?.Phase);
    }

    [Fact]
    public void Material_resolver_follows_nested_opacity_materials()
    {
        var texture = new TextureManifestEntry(
            "sky", "cloud", "/textures/sky/cloud.webp?v=123", 64, 64,
            "dxt5", "hash", "resolved", null);
        var textureReference = new TextureMaterialReference("sky", "cloud", "Texture");
        var pannerReference = new TextureMaterialReference("sky", "cloud-pan", "TexPanner");
        var panner = new TextureMaterialManifestEntry(
            "sky", "cloud-pan", "TexPanner", textureReference, null, null, null,
            0, 0, false, false, 128, true, true,
            PanRate: 0.002f);
        var shader = new TextureMaterialManifestEntry(
            "sky", "cloud-shader", "Shader", null, pannerReference, pannerReference, null,
            0, 0, false, false, 128, true, true);
        var modifier = new TextureMaterialManifestEntry(
            "sky", "cloud-final", "ColorModifier",
            new TextureMaterialReference("sky", "cloud-shader", "Shader"),
            null, null, null,
            0, 0, false, false, 128, true, true,
            ModifierColor: new TextureMaterialColor(255, 192, 151, 255));
        var resolver = new StaticMeshMaterialResolver([texture], [panner, shader, modifier]);
        var mesh = new UnrealStaticMesh(
            "cloud",
            [Vector3.Zero, Vector3.UnitY, Vector3.UnitX],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [],
            [0, 1, 2],
            [new UnrealStaticMeshSection(
                0,
                3,
                new UnrealObjectReference("sky", "cloud-final", "ColorModifier"))]);

        var result = resolver.Resolve(mesh, "sky");
        var binding = Assert.Single(result.SectionMaterials);

        Assert.Equal("resolved", result.Status);
        Assert.NotNull(binding);
        Assert.Equal(texture.Url, binding.DiffuseUrl);
        Assert.Equal(texture.Url, binding.OpacityUrl);
        Assert.Equal(StaticMeshBlendMode.AlphaBlend, binding.BlendMode);
        Assert.Equal(StaticMeshOpacitySource.Texture, binding.OpacitySource);
        Assert.Equal(0.002f, binding.PanRate);
        Assert.Equal(new StaticMeshMaterialTint(1, 192 / 255f, 151 / 255f, 1), binding.Tint);
    }

    [Theory]
    [InlineData("Dwarf_Grass.Dwarf_grass002", "grass", StaticMeshBlendMode.Masked, StaticMeshWindMode.Grass)]
    [InlineData("Landmark.Tree01", "Tree.Leaf", StaticMeshBlendMode.Masked, StaticMeshWindMode.Foliage)]
    [InlineData("Landmark.Tree01", "Tree.Bark", StaticMeshBlendMode.Opaque, StaticMeshWindMode.None)]
    [InlineData("Castle.Wall", "Castle.Stone", StaticMeshBlendMode.Opaque, StaticMeshWindMode.None)]
    public void Wind_classification_is_conservative(
        string meshName,
        string materialName,
        StaticMeshBlendMode blendMode,
        StaticMeshWindMode expected)
    {
        var material = new StaticMeshMaterialBinding(
            materialName, "/texture.webp", null, null, blendMode, false, 0.5f, true, true);

        Assert.Equal(expected, StaticMeshMaterialResolver.WindMode(meshName, material));
    }

    [Fact]
    public void Bsp_polygon_flags_override_resolved_and_neutral_material_semantics()
    {
        var neutral = AssetImportJobProcessor.ApplyBspFlags(
            null,
            UnrealPolyFlags.Masked | UnrealPolyFlags.TwoSided);
        Assert.NotNull(neutral);
        Assert.Equal(StaticMeshBlendMode.Masked, neutral.BlendMode);
        Assert.True(neutral.DoubleSided);

        var texture = new StaticMeshMaterialBinding(
            "stone",
            "/stone.webp",
            null,
            null,
            StaticMeshBlendMode.Opaque,
            false,
            0.5f,
            true,
            true);
        var unlit = AssetImportJobProcessor.ApplyBspFlags(
            texture,
            UnrealPolyFlags.Translucent | UnrealPolyFlags.Unlit);
        Assert.NotNull(unlit);
        Assert.Equal(StaticMeshBlendMode.AlphaBlend, unlit.BlendMode);
        Assert.True(unlit.Unlit);
        Assert.Null(unlit.EmissiveUrl);

        var authoredGlow = AssetImportJobProcessor.ApplyBspFlags(
            texture with { EmissiveUrl = "/glow.webp" },
            UnrealPolyFlags.Unlit);
        Assert.NotNull(authoredGlow);
        Assert.True(authoredGlow.Unlit);
        Assert.Equal("/glow.webp", authoredGlow.EmissiveUrl);

        using var json = ReadJsonChunk(GlbStaticMeshEncoder.Encode(
            new UnrealStaticMesh(
                "unlit-triangle",
                [Vector3.Zero, Vector3.UnitY, Vector3.UnitX],
                [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
                [],
                [0, 1, 2],
                [new UnrealStaticMeshSection(0, 3)]),
            [unlit]));
        var material = json.RootElement.GetProperty("materials")[0];
        Assert.True(material.GetProperty("extras").GetProperty("l2")
            .GetProperty("unlit").GetBoolean());
        Assert.False(material.TryGetProperty("emissiveTexture", out _));
    }

    [Fact]
    public void Material_loader_collects_only_reachable_texture_dependencies()
    {
        var baseTexture = new TextureMaterialReference("world", "base", "Texture");
        var opacityTexture = new TextureMaterialReference("world", "opacity", "Texture");
        var detailTexture = new TextureMaterialReference("world", "detail", "Texture");
        var glowTexture = new TextureMaterialReference("effects", "glow", "Texture");
        var shaderReference = new TextureMaterialReference("world", "shader", "Shader");
        var glowShaderReference = new TextureMaterialReference("world", "glow-shader", "Shader");
        var opacityPannerReference = new TextureMaterialReference("world", "opacity-panner", "TexPanner");
        var materials = new[]
        {
            new TextureMaterialManifestEntry(
                "world", "root", "Combiner", shaderReference, null, null, null,
                0, 0, false, false, 128, true, true,
                Material2: glowShaderReference),
            new TextureMaterialManifestEntry(
                "world", "shader", "Shader", null, baseTexture, opacityPannerReference, null,
                0, 0, false, false, 128, true, true,
                Detail: detailTexture),
            new TextureMaterialManifestEntry(
                "world", "opacity-panner", "TexPanner", opacityTexture, null, null, null,
                0, 0, false, false, 128, true, true),
            new TextureMaterialManifestEntry(
                "world", "glow-shader", "Shader", null, glowTexture, null, null,
                0, 0, false, false, 128, true, true),
            new TextureMaterialManifestEntry(
                "unused", "unreachable", "Shader", null,
                new TextureMaterialReference("unused", "texture", "Texture"), null, null,
                0, 0, false, false, 128, true, true)
        };

        var required = StaticMeshMaterialCatalogLoader.RequiredTextures(
            [new TextureMaterialReference("world", "root", "Combiner")],
            materials);

        Assert.Equal(
            ["effects.glow", "world.base", "world.detail", "world.opacity"],
            required.Select(reference => $"{reference.PackageName}.{reference.ObjectName}")
                .OrderBy(value => value));
    }

    private static ReadOnlySpan<byte> ReadBinaryChunk(byte[] glb)
    {
        var jsonLength = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(12, 4)));
        var binaryHeaderOffset = 20 + jsonLength;
        var binaryLength = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(
            glb.AsSpan(binaryHeaderOffset, 4)));
        return glb.AsSpan(binaryHeaderOffset + 8, binaryLength);
    }

    private static JsonDocument ReadJsonChunk(byte[] glb)
    {
        var jsonLength = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(12, 4)));
        return JsonDocument.Parse(glb.AsMemory(20, jsonLength));
    }

    private static Vector3 ReadVector3(ReadOnlySpan<byte> source, int offset) => new(
        ReadSingle(source, offset),
        ReadSingle(source, offset + 4),
        ReadSingle(source, offset + 8));

    private static float ReadSingle(ReadOnlySpan<byte> source, int offset) =>
        BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(source[offset..]));
}
