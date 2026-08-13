using System.Numerics;
using L2.Tools.PackageReader;
using L2.Tools.StaticMeshConverter;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class StaticMeshMaterialResolverTests
{
    [Fact]
    public void AppliesDirectTextureMaterialPropertiesWithoutInferringAlphaFromFormat()
    {
        var textures = new[]
        {
            Texture("masked", "dxt3", masked: true, twoSided: true, detail: "detail", clampU: true),
            Texture("opaque-alpha-format", "dxt5"),
            Texture("decoded-alpha", "dxt3", hasTransparency: true),
            Texture("detail", "dxt1")
        };
        var resolver = new StaticMeshMaterialResolver(textures, []);

        var masked = resolver.Resolve(Mesh("masked"), "package").SectionMaterials.Single()!;
        var opaque = resolver.Resolve(Mesh("opaque-alpha-format"), "package").SectionMaterials.Single()!;
        var decodedAlpha = resolver.Resolve(Mesh("decoded-alpha"), "package").SectionMaterials.Single()!;

        Assert.Equal(StaticMeshBlendMode.Masked, masked.BlendMode);
        Assert.True(masked.DoubleSided);
        Assert.True(masked.ClampU);
        Assert.False(masked.ClampV);
        Assert.Equal("/detail.webp", masked.DetailUrl);
        Assert.Equal(4, masked.DetailScale);
        Assert.Equal(StaticMeshBlendMode.Opaque, opaque.BlendMode);
        Assert.Equal(StaticMeshBlendMode.AlphaBlend, decodedAlpha.BlendMode);
    }

    [Fact]
    public void AppliesAuthoredAlphaBlendToDirectTexture()
    {
        var resolver = new StaticMeshMaterialResolver(
            [Texture("alpha", "dxt3", alphaTexture: true)],
            []);

        var material = resolver.Resolve(Mesh("alpha"), "package").SectionMaterials.Single()!;

        Assert.Equal(StaticMeshBlendMode.AlphaBlend, material.BlendMode);
    }

    private static TextureManifestEntry Texture(
        string name,
        string format,
        bool masked = false,
        bool alphaTexture = false,
        bool hasTransparency = false,
        bool twoSided = false,
        string? detail = null,
        bool clampU = false) => new(
            "package",
            name,
            $"/{name}.webp",
            1,
            1,
            format,
            "hash",
            "resolved",
            null,
            Masked: masked,
            AlphaTexture: alphaTexture,
            HasTransparency: hasTransparency,
            TwoSided: twoSided,
            Detail: detail is null ? null : new TextureMaterialReference("package", detail, "Texture"),
            DetailScale: 4,
            ClampU: clampU);

    private static UnrealStaticMesh Mesh(string material) => new(
        "mesh",
        [Vector3.Zero, Vector3.UnitX, Vector3.UnitY],
        [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
        [Vector2.Zero, Vector2.UnitX, Vector2.UnitY],
        [0, 1, 2],
        [new UnrealStaticMeshSection(
            0,
            3,
            new UnrealObjectReference("package", material, "Texture"))]);
}
