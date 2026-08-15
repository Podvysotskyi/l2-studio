using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;
using L2.Tools.PackageReader;
using L2.Tools.StaticMeshConverter;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class GlbStaticMeshEncoderTests
{
    [Fact]
    public void PreservesRootRelativePublishedTextureUrls()
    {
        var mesh = new UnrealStaticMesh(
            "controltower_hat",
            [new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0)],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [Vector2.Zero, Vector2.UnitX, Vector2.UnitY],
            [0, 1, 2],
            [new UnrealStaticMeshSection(0, 3)]);
        var textureUrl = "/versions/c1/Textures/Aden_Castle_Etc_T/hash/Aden_Castle_Etc_T/aden_castle_etc06.webp?gpu=none";
        var material = new StaticMeshMaterialBinding(
            "Aden_Castle_Etc06",
            textureUrl,
            null,
            null,
            StaticMeshBlendMode.Opaque,
            false,
            0.5f,
            true,
            true);

        var glb = GlbStaticMeshEncoder.Encode(mesh, [material]);

        var jsonLength = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(12, 4));
        using var document = JsonDocument.Parse(glb.AsSpan(20, (int)jsonLength).ToArray());
        Assert.Equal(textureUrl, document.RootElement.GetProperty("images")[0].GetProperty("uri").GetString());
        var l2 = document.RootElement.GetProperty("materials")[0]
            .GetProperty("extras").GetProperty("l2");
        Assert.Equal(textureUrl, l2.GetProperty("diffuseUrl").GetString());
        Assert.Equal(JsonValueKind.Null, l2.GetProperty("emissiveUrl").ValueKind);
    }

    [Fact]
    public void PreservesAuthoredTextureClampModes()
    {
        var mesh = new UnrealStaticMesh(
            "clamped",
            [Vector3.Zero, Vector3.UnitX, Vector3.UnitY],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [Vector2.Zero, Vector2.UnitX, Vector2.UnitY],
            [0, 1, 2],
            [new UnrealStaticMeshSection(0, 3)]);
        var material = new StaticMeshMaterialBinding(
            "clamped",
            "/clamped.webp",
            null,
            null,
            StaticMeshBlendMode.Opaque,
            false,
            0.5f,
            true,
            true,
            ClampU: true);

        var glb = GlbStaticMeshEncoder.Encode(mesh, [material]);

        var jsonLength = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(12, 4));
        using var document = JsonDocument.Parse(glb.AsSpan(20, (int)jsonLength).ToArray());
        var texture = document.RootElement.GetProperty("textures")[0];
        var sampler = document.RootElement.GetProperty("samplers")[texture.GetProperty("sampler").GetInt32()];
        Assert.Equal(33071, sampler.GetProperty("wrapS").GetInt32());
        Assert.Equal(10497, sampler.GetProperty("wrapT").GetInt32());
        var l2 = document.RootElement.GetProperty("materials")[0]
            .GetProperty("extras").GetProperty("l2");
        Assert.True(l2.GetProperty("clampU").GetBoolean());
        Assert.False(l2.GetProperty("clampV").GetBoolean());
    }

    [Fact]
    public void PublishesLuminanceOpacityAnimationForLegacyTranslucency()
    {
        var mesh = new UnrealStaticMesh(
            "flame",
            [Vector3.Zero, Vector3.UnitX, Vector3.UnitY],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [Vector2.Zero, Vector2.UnitX, Vector2.UnitY],
            [0, 1, 2],
            [new UnrealStaticMeshSection(0, 3)]);
        var animation = new StaticMeshTextureAnimation(
            ["/flame-1.webp", "/flame-2.webp"],
            20);
        var material = new StaticMeshMaterialBinding(
            "flame",
            "/flame.webp",
            "/flame.webp",
            null,
            StaticMeshBlendMode.AlphaBlend,
            true,
            0.5f,
            false,
            true,
            StaticMeshOpacitySource.Texture,
            StaticMeshOpacityChannel.Luminance,
            DiffuseAnimation: animation,
            OpacityAnimation: animation);

        var glb = GlbStaticMeshEncoder.Encode(mesh, [material]);

        var jsonLength = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(12, 4));
        using var document = JsonDocument.Parse(glb.AsSpan(20, (int)jsonLength).ToArray());
        var published = document.RootElement.GetProperty("materials")[0];
        var l2 = published.GetProperty("extras").GetProperty("l2");
        Assert.Equal("BLEND", published.GetProperty("alphaMode").GetString());
        Assert.Equal("/flame.webp", l2.GetProperty("opacityUrl").GetString());
        Assert.Equal("texture", l2.GetProperty("opacitySource").GetString());
        Assert.Equal("luminance", l2.GetProperty("opacityChannel").GetString());
        Assert.Equal(20, l2.GetProperty("opacityAnimation").GetProperty("frameRate").GetSingle());
        Assert.Equal(
            ["/flame-1.webp", "/flame-2.webp"],
            l2.GetProperty("opacityAnimation").GetProperty("frameUrls")
                .EnumerateArray().Select(value => value.GetString()!).ToArray());
    }
}
