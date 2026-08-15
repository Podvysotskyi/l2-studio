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

    [Fact]
    public void ResolvesAMaterialRootWithoutAStaticMesh()
    {
        var resolver = new StaticMeshMaterialResolver(
            [Texture("diffuse", "dxt1")],
            [new TextureMaterialManifestEntry(
                "package",
                "group.shader",
                "Shader",
                null,
                new TextureMaterialReference("package", "diffuse", "Texture"),
                null,
                null,
                0,
                0,
                true,
                false,
                128,
                true,
                true)]);

        var material = resolver.Resolve(new TextureMaterialReference("package", "group.shader", "Shader"));

        Assert.Equal("group.shader", material.Name);
        Assert.Equal("/diffuse.webp", material.DiffuseUrl);
        Assert.True(material.DoubleSided);
    }

    [Fact]
    public void UsesAnimatedDiffuseLuminanceForLegacyShaderTranslucency()
    {
        var animation = new TextureAnimationManifestEntry(
            ["/flame-1.webp", "/flame-2.webp"],
            20,
            20);
        var resolver = new StaticMeshMaterialResolver(
            [Texture("flame", "dxt1", animation: animation)],
            [Material("flame-shader", "Shader", diffuse: "flame", outputBlending: 3)]);

        var material = resolver.Resolve(new TextureMaterialReference("package", "flame-shader", "Shader"));

        Assert.Equal(StaticMeshBlendMode.AlphaBlend, material.BlendMode);
        Assert.Equal("/flame.webp", material.OpacityUrl);
        Assert.Equal(StaticMeshOpacitySource.Texture, material.OpacitySource);
        Assert.Equal(StaticMeshOpacityChannel.Luminance, material.OpacityChannel);
        Assert.Equal(animation.FrameUrls, material.OpacityAnimation?.FrameUrls);
        Assert.Equal(animation.MaxFrameRate, material.OpacityAnimation?.FrameRate);
        Assert.Equal(material.DiffuseAnimation, material.OpacityAnimation);
    }

    [Fact]
    public void UsesDiffuseLuminanceForLegacyFinalBlendTranslucency()
    {
        var resolver = new StaticMeshMaterialResolver(
            [Texture("glow", "dxt1")],
            [Material("glow-final", "FinalBlend", material: "glow", frameBufferBlending: 4)]);

        var material = resolver.Resolve(new TextureMaterialReference("package", "glow-final", "FinalBlend"));

        Assert.Equal(StaticMeshBlendMode.AlphaBlend, material.BlendMode);
        Assert.Equal("/glow.webp", material.OpacityUrl);
        Assert.Equal(StaticMeshOpacitySource.Texture, material.OpacitySource);
        Assert.Equal(StaticMeshOpacityChannel.Luminance, material.OpacityChannel);
    }

    [Theory]
    [InlineData(false, false, StaticMeshOpacityChannel.Luminance)]
    [InlineData(true, false, StaticMeshOpacityChannel.Alpha)]
    [InlineData(false, true, StaticMeshOpacityChannel.Alpha)]
    public void SelectsExplicitOpacityChannelFromDecodedAlphaMetadata(
        bool alphaTexture,
        bool hasTransparency,
        StaticMeshOpacityChannel expectedChannel)
    {
        var resolver = new StaticMeshMaterialResolver(
            [
                Texture("diffuse", "dxt1"),
                Texture("opacity", "dxt5", alphaTexture: alphaTexture, hasTransparency: hasTransparency)
            ],
            [Material("shader", "Shader", diffuse: "diffuse", opacity: "opacity")]);

        var material = resolver.Resolve(new TextureMaterialReference("package", "shader", "Shader"));

        Assert.Equal(StaticMeshBlendMode.AlphaBlend, material.BlendMode);
        Assert.Equal("/opacity.webp", material.OpacityUrl);
        Assert.Equal(StaticMeshOpacitySource.Texture, material.OpacitySource);
        Assert.Equal(expectedChannel, material.OpacityChannel);
    }

    [Fact]
    public void PreservesNonTranslucentBlendMappingsWithoutSynthesizingOpacity()
    {
        var resolver = new StaticMeshMaterialResolver(
            [Texture("diffuse", "dxt1")],
            [Material("additive", "Shader", diffuse: "diffuse", outputBlending: 5)]);

        var material = resolver.Resolve(new TextureMaterialReference("package", "additive", "Shader"));

        Assert.Equal(StaticMeshBlendMode.Additive, material.BlendMode);
        Assert.Null(material.OpacityUrl);
        Assert.Equal(StaticMeshOpacitySource.None, material.OpacitySource);
    }

    [Fact]
    public void KeepsNormalShaderOpaqueWhenDiffuseAlphaIsItsSpecularityMask()
    {
        var resolver = new StaticMeshMaterialResolver(
            [Texture("ant", "dxt3", hasTransparency: true)],
            [Material("ant-shader", "Shader", diffuse: "ant", specularityMask: "ant")]);

        var material = resolver.Resolve(new TextureMaterialReference("package", "ant-shader", "Shader"));

        Assert.Equal(StaticMeshBlendMode.Opaque, material.BlendMode);
        Assert.Null(material.OpacityUrl);
        Assert.Equal(StaticMeshOpacitySource.None, material.OpacitySource);
        Assert.Equal("/ant.webp", material.SpecularityMaskUrl);
    }

    private static TextureManifestEntry Texture(
        string name,
        string format,
        bool masked = false,
        bool alphaTexture = false,
        bool hasTransparency = false,
        bool twoSided = false,
        string? detail = null,
        bool clampU = false,
        TextureAnimationManifestEntry? animation = null) => new(
            "package",
            name,
            $"/{name}.webp",
            1,
            1,
            format,
            "hash",
            "resolved",
            null,
            Animation: animation,
            Masked: masked,
            AlphaTexture: alphaTexture,
            HasTransparency: hasTransparency,
            TwoSided: twoSided,
            Detail: detail is null ? null : new TextureMaterialReference("package", detail, "Texture"),
            DetailScale: 4,
            ClampU: clampU);

    private static TextureMaterialManifestEntry Material(
        string name,
        string className,
        string? material = null,
        string? diffuse = null,
        string? opacity = null,
        byte outputBlending = 0,
        byte frameBufferBlending = 0,
        string? specularityMask = null) => new(
            "package",
            name,
            className,
            Reference(material),
            Reference(diffuse),
            Reference(opacity),
            null,
            outputBlending,
            frameBufferBlending,
            false,
            false,
            128,
            true,
            true,
            SpecularityMask: Reference(specularityMask));

    private static TextureMaterialReference? Reference(string? name) => name is null
        ? null
        : new TextureMaterialReference("package", name, "Texture");

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
