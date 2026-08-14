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
        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(glb, 20, jsonLength).TrimEnd(' '));
        Assert.Equal(1, document.RootElement.GetProperty("skins").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("animations").ValueKind);
        var attributes = document.RootElement.GetProperty("meshes")[0].GetProperty("primitives")[0].GetProperty("attributes");
        Assert.True(attributes.TryGetProperty("JOINTS_0", out _));
        Assert.True(attributes.TryGetProperty("WEIGHTS_0", out _));
        var animationJsonLength = BitConverter.ToInt32(animationGlb, 12);
        using var animationDocument = JsonDocument.Parse(
            Encoding.UTF8.GetString(animationGlb, 20, animationJsonLength).TrimEnd(' '));
        Assert.Equal("idle", animationDocument.RootElement.GetProperty("animations")[0].GetProperty("name").GetString());
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
}
