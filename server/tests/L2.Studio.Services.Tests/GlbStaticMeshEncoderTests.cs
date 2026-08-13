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
    }
}
