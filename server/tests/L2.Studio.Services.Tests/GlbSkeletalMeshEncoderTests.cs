using System.Numerics;
using System.Text;
using System.Text.Json;
using L2.Tools.PackageReader;
using L2.Tools.StaticMeshConverter;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class GlbSkeletalMeshEncoderTests
{
    [Fact]
    public void EncodesSkinWeightsSkeletonAndPlayableClip()
    {
        var bone = new UnrealSkeletalBone("root", -1, Quaternion.Identity, Vector3.Zero);
        var weight = new UnrealSkeletalWeight(0, 0, 0, 0, new Vector4(1, 0, 0, 0));
        var mesh = new UnrealSkeletalMesh(
            "example",
            [Vector3.Zero, Vector3.UnitX, Vector3.UnitY],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [Vector2.Zero, Vector2.UnitX, Vector2.UnitY],
            [0, 1, 2],
            [weight, weight, weight],
            [bone],
            [new UnrealSkeletalMeshSection(0, 3, null)],
            new UnrealObjectReference("", "example_anim", "MeshAnimation"),
            Vector3.One,
            Vector3.Zero,
            new UnrealRotator(0, 0, 0));
        var track = new UnrealAnimationTrack(
            [Quaternion.Identity, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.5f)],
            [Vector3.Zero, Vector3.UnitX],
            [0, 1]);
        var animation = new UnrealMeshAnimation(
            "example_anim",
            [new UnrealAnimationBone("root", -1)],
            [new UnrealAnimationClip("idle", 2, 1, [], [track], [])]);

        var glb = GlbSkeletalMeshEncoder.Encode(mesh);
        var repeated = GlbSkeletalMeshEncoder.Encode(mesh);
        var animationGlb = GlbAnimationEncoder.Encode(animation);

        Assert.Equal(glb, repeated);
        Assert.Equal(0x46546c67u, BitConverter.ToUInt32(glb));
        var jsonLength = BitConverter.ToInt32(glb, 12);
        var json = Encoding.UTF8.GetString(glb, 20, jsonLength).TrimEnd(' ');
        using var document = JsonDocument.Parse(json);
        Assert.Equal(1, document.RootElement.GetProperty("skins").GetArrayLength());
        Assert.False(document.RootElement.TryGetProperty("animations", out _));
        Assert.DoesNotContain(":null", json);
        var attributes = document.RootElement.GetProperty("meshes")[0].GetProperty("primitives")[0].GetProperty("attributes");
        Assert.True(attributes.TryGetProperty("JOINTS_0", out _));
        Assert.True(attributes.TryGetProperty("WEIGHTS_0", out _));
        var animationJsonLength = BitConverter.ToInt32(animationGlb, 12);
        using var animationDocument = JsonDocument.Parse(
            Encoding.UTF8.GetString(animationGlb, 20, animationJsonLength).TrimEnd(' '));
        Assert.Equal("idle", animationDocument.RootElement.GetProperty("animations")[0].GetProperty("name").GetString());
    }

    [Fact]
    public void EncodesResolvedDefaultMaterialsForEachSkeletalSection()
    {
        var bone = new UnrealSkeletalBone("root", -1, Quaternion.Identity, Vector3.Zero);
        var weight = new UnrealSkeletalWeight(0, 0, 0, 0, new Vector4(1, 0, 0, 0));
        var mesh = new UnrealSkeletalMesh(
            "example",
            [Vector3.Zero, Vector3.UnitX, Vector3.UnitY, Vector3.One],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [Vector2.Zero, Vector2.UnitX, Vector2.UnitY, Vector2.One],
            [0, 1, 2, 1, 3, 2],
            [weight, weight, weight, weight],
            [bone],
            [new UnrealSkeletalMeshSection(0, 3, null), new UnrealSkeletalMeshSection(3, 3, null)],
            null,
            Vector3.One,
            Vector3.Zero,
            new UnrealRotator(0, 0, 0));
        var materials = new StaticMeshMaterialBinding?[]
        {
            new("body", "/textures/body.webp", null, null, StaticMeshBlendMode.Opaque, false, 0.5f, true, true),
            new("armor", "/textures/armor.webp", null, null, StaticMeshBlendMode.AlphaBlend, true, 0.5f, true, true)
        };

        using var document = ParseJson(GlbSkeletalMeshEncoder.Encode(mesh, sectionMaterials: materials));

        var root = document.RootElement;
        Assert.Equal(2, root.GetProperty("materials").GetArrayLength());
        Assert.Equal(0, root.GetProperty("meshes")[0].GetProperty("primitives")[0].GetProperty("material").GetInt32());
        Assert.Equal(1, root.GetProperty("meshes")[0].GetProperty("primitives")[1].GetProperty("material").GetInt32());
        Assert.Equal("/textures/body.webp", root.GetProperty("images")[0].GetProperty("uri").GetString());
        Assert.False(root.GetProperty("materials")[0].TryGetProperty("alphaMode", out _));
        Assert.Equal("BLEND", root.GetProperty("materials")[1].GetProperty("alphaMode").GetString());
        Assert.Equal("alphablend", root.GetProperty("materials")[1].GetProperty("extras").GetProperty("l2")
            .GetProperty("blendMode").GetString());
    }

    [Fact]
    public void ConvertsUnrealCoordinatesRootRotationAndFallbackTimelines()
    {
        var rootRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
        var position = new Vector3(100, 200, 300);
        var bone = new UnrealSkeletalBone("root", -1, rootRotation, position);
        var weight = new UnrealSkeletalWeight(0, 0, 0, 0, new Vector4(1, 0, 0, 0));
        var mesh = new UnrealSkeletalMesh(
            "example",
            [position, position + Vector3.UnitX, position + Vector3.UnitY],
            [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            [Vector2.Zero, Vector2.UnitX, Vector2.UnitY],
            [0, 1, 2],
            [weight, weight, weight],
            [bone],
            [new UnrealSkeletalMeshSection(0, 3, null)],
            null,
            Vector3.One,
            Vector3.Zero,
            new UnrealRotator(0, 0, 0));
        var track = new UnrealAnimationTrack(
            [rootRotation, rootRotation, rootRotation],
            [position, position, position],
            []);
        var animation = new UnrealMeshAnimation(
            "example_anim",
            [new UnrealAnimationBone("root", -1)],
            [new UnrealAnimationClip("idle", 3, 2, [], [track], [])]);

        var meshGlb = GlbSkeletalMeshEncoder.Encode(mesh);
        using var meshDocument = ParseJson(meshGlb);
        var rootNode = meshDocument.RootElement.GetProperty("nodes")[0];
        AssertFloatArray(rootNode.GetProperty("translation"), [1, 3, 2]);
        AssertFloatArray(rootNode.GetProperty("rotation"), [0, 0, -MathF.Sqrt(0.5f), MathF.Sqrt(0.5f)]);
        var primitiveAttributes = meshDocument.RootElement.GetProperty("meshes")[0]
            .GetProperty("primitives")[0].GetProperty("attributes");
        AssertFloatArray(ReadAccessor(meshGlb, meshDocument.RootElement,
            primitiveAttributes.GetProperty("POSITION").GetInt32()).Take(3).ToArray(), [1, 3, 2]);
        AssertFloatArray(ReadAccessor(meshGlb, meshDocument.RootElement,
            primitiveAttributes.GetProperty("NORMAL").GetInt32()).Take(3).ToArray(), [0, 1, 0]);
        var inverseBindAccessor = meshDocument.RootElement.GetProperty("skins")[0]
            .GetProperty("inverseBindMatrices").GetInt32();
        var inverseBind = ReadAccessor(meshGlb, meshDocument.RootElement, inverseBindAccessor);
        Assert.Equal(0f, inverseBind[3]);
        Assert.Equal(0f, inverseBind[7]);
        Assert.Equal(0f, inverseBind[11]);
        Assert.Equal(1f, inverseBind[15]);

        var animationGlb = GlbAnimationEncoder.Encode(animation);
        using var animationDocument = ParseJson(animationGlb);
        var samplers = animationDocument.RootElement.GetProperty("animations")[0].GetProperty("samplers");
        AssertFloatArray(ReadAccessor(animationGlb, animationDocument.RootElement,
            samplers[0].GetProperty("input").GetInt32()), [0, 0.5f, 1]);
        AssertFloatArray(ReadAccessor(animationGlb, animationDocument.RootElement,
            samplers[0].GetProperty("output").GetInt32()).Take(4).ToArray(),
            [0, 0, -MathF.Sqrt(0.5f), MathF.Sqrt(0.5f)]);
        AssertFloatArray(ReadAccessor(animationGlb, animationDocument.RootElement,
            samplers[1].GetProperty("output").GetInt32()).Take(3).ToArray(), [1, 3, 2]);
    }

    [Fact]
    public void EncodesAChronicleOneMeshAndItsLinkedAnimationWhenAvailable()
    {
        var root = Environment.GetEnvironmentVariable("L2_C1_ANIMATIONS_PATH");
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return;
        var path = Path.Combine(root, "LineageDecos.ukx");
        var package = new UnrealPackageReader(LineagePackageDecoder.DecodeProtocol111(File.ReadAllBytes(path)))
            .ReadAnimationPackage();
        var pair = (
            from mesh in package.SkeletalMeshes
            where mesh.Error is null && mesh.Animation is not null
            from animation in package.AnimationSets
            where LeafName(animation.Name).Equals(LeafName(mesh.Animation!.ObjectName), StringComparison.OrdinalIgnoreCase)
            where mesh.Bones.Select(bone => bone.Name).SequenceEqual(
                animation.Bones.Select(bone => bone.Name), StringComparer.OrdinalIgnoreCase)
            select (mesh, animation)).First();

        var glb = GlbAnimationEncoder.Encode(pair.animation);

        Assert.Equal(0x46546c67u, BitConverter.ToUInt32(glb));
        var jsonLength = BitConverter.ToInt32(glb, 12);
        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(glb, 20, jsonLength).TrimEnd(' '));
        Assert.Equal(pair.animation.Clips.Count, document.RootElement.GetProperty("animations").GetArrayLength());
    }

    private static string LeafName(string path)
    {
        var separator = path.LastIndexOf('.');
        return separator < 0 ? path : path[(separator + 1)..];
    }

    private static JsonDocument ParseJson(byte[] glb)
    {
        var jsonLength = BitConverter.ToInt32(glb, 12);
        return JsonDocument.Parse(Encoding.UTF8.GetString(glb, 20, jsonLength).TrimEnd(' '));
    }

    private static float[] ReadAccessor(byte[] glb, JsonElement document, int accessorIndex)
    {
        var accessor = document.GetProperty("accessors")[accessorIndex];
        var view = document.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
        var jsonLength = BitConverter.ToInt32(glb, 12);
        var binaryOffset = 20 + jsonLength + 8;
        var offset = binaryOffset + view.GetProperty("byteOffset").GetInt32();
        if (accessor.TryGetProperty("byteOffset", out var accessorOffset)) offset += accessorOffset.GetInt32();
        var components = accessor.GetProperty("type").GetString() switch
        {
            "SCALAR" => 1,
            "VEC3" => 3,
            "VEC4" => 4,
            "MAT4" => 16,
            _ => throw new InvalidDataException("Unsupported test accessor type.")
        };
        var values = new float[accessor.GetProperty("count").GetInt32() * components];
        for (var index = 0; index < values.Length; index++)
            values[index] = BitConverter.ToSingle(glb, offset + index * sizeof(float));
        return values;
    }

    private static void AssertFloatArray(JsonElement actual, IReadOnlyList<float> expected) =>
        AssertFloatArray(actual.EnumerateArray().Select(item => item.GetSingle()).ToArray(), expected);

    private static void AssertFloatArray(IReadOnlyList<float> actual, IReadOnlyList<float> expected)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var index = 0; index < expected.Count; index++)
            Assert.Equal(expected[index], actual[index], 5);
    }
}
