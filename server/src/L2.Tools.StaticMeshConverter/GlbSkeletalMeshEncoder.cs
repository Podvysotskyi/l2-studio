using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using L2.Tools.PackageReader;

namespace L2.Tools.StaticMeshConverter;

public static class GlbSkeletalMeshEncoder
{
    private const uint GlbMagic = 0x46546c67;
    private const uint JsonChunkType = 0x4e4f534a;
    private const uint BinaryChunkType = 0x004e4942;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static byte[] Encode(
        UnrealSkeletalMesh mesh,
        UnrealMeshAnimation? animation = null,
        IReadOnlyDictionary<string, UnrealAnimationClip>? clips = null,
        IReadOnlyList<StaticMeshMaterialBinding?>? sectionMaterials = null)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (mesh.Positions.Count == 0 || mesh.Indices.Count == 0 || mesh.Bones.Count == 0)
            throw new InvalidDataException($"Skeletal mesh '{mesh.Name}' is incomplete.");
        var compatibleAnimation = animation is not null && SkeletonMatches(mesh, animation);
        var includedClips = compatibleAnimation
            ? animation!.Clips.Where(clip => clips is null || clips.ContainsKey(clip.Name)).ToArray()
            : [];
        using var binary = new MemoryStream();
        var views = new List<object>();
        var accessors = new List<object>();

        int AddAccessor<T>(
            IEnumerable<T> values,
            Action<Stream, T> write,
            int elementSize,
            int componentType,
            string type,
            int? target = null,
            bool normalized = false,
            float[]? minimum = null,
            float[]? maximum = null)
        {
            Pad(binary, 4, 0);
            var offset = checked((int)binary.Position);
            var materialized = values.ToArray();
            foreach (var value in materialized) write(binary, value);
            var view = views.Count;
            views.Add(target is null
                ? new { buffer = 0, byteOffset = offset, byteLength = materialized.Length * elementSize }
                : (object)new { buffer = 0, byteOffset = offset, byteLength = materialized.Length * elementSize, target });
            var accessor = accessors.Count;
            accessors.Add(new
            {
                bufferView = view,
                componentType,
                normalized,
                count = materialized.Length,
                type,
                min = minimum,
                max = maximum
            });
            return accessor;
        }

        var convertedPositions = mesh.Positions.Select(UnrealGltfTransform.Position).ToArray();
        var positions = AddAccessor(
            convertedPositions, WriteVector3, 12, 5126, "VEC3", 34962,
            minimum: [convertedPositions.Min(v => v.X), convertedPositions.Min(v => v.Y), convertedPositions.Min(v => v.Z)],
            maximum: [convertedPositions.Max(v => v.X), convertedPositions.Max(v => v.Y), convertedPositions.Max(v => v.Z)]);
        var normals = AddAccessor(mesh.Normals.Select(UnrealGltfTransform.Direction), WriteVector3, 12, 5126, "VEC3", 34962);
        var texCoords = AddAccessor(mesh.TextureCoordinates, WriteVector2, 8, 5126, "VEC2", 34962);
        var joints = AddAccessor(mesh.Weights, WriteJoints, 8, 5123, "VEC4", 34962);
        var weights = AddAccessor(mesh.Weights.Select(item => item.Weights), WriteVector4, 16, 5126, "VEC4", 34962);
        var indexBufferView = views.Count;
        var indices = AddAccessor(mesh.Indices, WriteUInt32, 4, 5125, "SCALAR", 34963);

        var localTransforms = mesh.Bones.Select((bone, index) => BoneTransform(bone, index == 0)).ToArray();
        var globalTransforms = new Matrix4x4[localTransforms.Length];
        for (var index = 0; index < localTransforms.Length; index++)
        {
            var parent = mesh.Bones[index].ParentIndex;
            globalTransforms[index] = parent >= 0 && parent < index
                ? localTransforms[index] * globalTransforms[parent]
                : localTransforms[index];
        }
        var inverses = globalTransforms.Select(InverseBindMatrix).ToArray();
        var inverseBinds = AddAccessor(inverses, WriteMatrix, 64, 5126, "MAT4");

        var nodes = new List<object>();
        for (var index = 0; index < mesh.Bones.Count; index++)
        {
            var bone = mesh.Bones[index];
            var children = Enumerable.Range(0, mesh.Bones.Count)
                .Where(child => child != index && mesh.Bones[child].ParentIndex == index).ToArray();
            var position = UnrealGltfTransform.Position(bone.Position);
            var rotation = UnrealGltfTransform.Rotation(bone.Orientation, index == 0);
            nodes.Add(new
            {
                name = bone.Name,
                translation = new[] { position.X, position.Y, position.Z },
                rotation = new[] { rotation.X, rotation.Y, rotation.Z, rotation.W },
                children = children.Length == 0 ? null : children
            });
        }
        var meshNode = nodes.Count;
        nodes.Add(new { name = mesh.Name, mesh = 0, skin = 0 });
        var roots = Enumerable.Range(0, mesh.Bones.Count)
            .Where(index => mesh.Bones[index].ParentIndex < 0 || mesh.Bones[index].ParentIndex == index).ToArray();

        var materialEncoder = new GltfMaterialEncoder();
        var attributes = new { POSITION = positions, NORMAL = normals, TEXCOORD_0 = texCoords, JOINTS_0 = joints, WEIGHTS_0 = weights };
        var primitives = new List<object>();
        if (mesh.Sections.Count == 0)
        {
            primitives.Add(new Dictionary<string, object> { ["attributes"] = attributes, ["indices"] = indices });
        }
        else
        {
            foreach (var (section, sectionIndex) in mesh.Sections.Select((value, index) => (value, index)))
            {
                var accessor = accessors.Count;
                accessors.Add(new
                {
                    bufferView = indexBufferView,
                    byteOffset = section.FirstIndex * 4,
                    componentType = 5125,
                    normalized = false,
                    count = section.IndexCount,
                    type = "SCALAR",
                    min = (float[]?)null,
                    max = (float[]?)null
                });
                var primitive = new Dictionary<string, object>
                {
                    ["attributes"] = attributes,
                    ["indices"] = accessor
                };
                if (sectionMaterials is not null && sectionIndex < sectionMaterials.Count &&
                    sectionMaterials[sectionIndex] is { } binding)
                {
                    primitive["material"] = materialEncoder.Add(binding);
                }
                primitives.Add(primitive);
            }
        }

        var gltfAnimations = new List<object>();
        foreach (var clip in includedClips)
        {
            var samplers = new List<object>();
            var channels = new List<object>();
            for (var boneIndex = 0; boneIndex < mesh.Bones.Count && boneIndex < clip.Tracks.Count; boneIndex++)
            {
                var track = clip.Tracks[boneIndex];
                AddAnimationChannel(track.Rotations, track.Times, clip, boneIndex, "rotation",
                    value => ConvertAnimationQuaternion(value, boneIndex == 0), WriteVector4, 16, "VEC4");
                AddAnimationChannel(track.Translations, track.Times, clip, boneIndex, "translation",
                    UnrealGltfTransform.Position, WriteVector3, 12, "VEC3");
            }
            gltfAnimations.Add(new { name = clip.Name, samplers, channels });

            void AddAnimationChannel<TInput, TOutput>(
                IReadOnlyList<TInput> keys,
                IReadOnlyList<float> keyTimes,
                UnrealAnimationClip sourceClip,
                int boneIndex,
                string path,
                Func<TInput, TOutput> convert,
                Action<Stream, TOutput> write,
                int elementSize,
                string type)
            {
                if (keys.Count == 0) return;
                var times = Timeline(keys.Count, keyTimes, sourceClip.FrameRate);
                var input = AddAccessor(times, WriteSingle, 4, 5126, "SCALAR",
                    minimum: [times[0]], maximum: [times[^1]]);
                var output = AddAccessor(keys.Select(convert), write, elementSize, 5126, type);
                var sampler = samplers.Count;
                samplers.Add(new { input, output, interpolation = "LINEAR" });
                channels.Add(new { sampler, target = new { node = boneIndex, path } });
            }
        }

        var document = new Dictionary<string, object?>
        {
            ["asset"] = new { version = "2.0", generator = "L2 Studio skeletal converter" },
            ["scene"] = 0,
            ["scenes"] = new[] { new { nodes = roots.Append(meshNode).ToArray() } },
            ["nodes"] = nodes,
            ["meshes"] = new[] { new { name = mesh.Name, primitives } },
            ["skins"] = new[] { new { inverseBindMatrices = inverseBinds, joints = Enumerable.Range(0, mesh.Bones.Count).ToArray(), skeleton = roots.FirstOrDefault() } },
            ["accessors"] = accessors,
            ["bufferViews"] = views,
            ["buffers"] = new[] { new { byteLength = (int)binary.Length } }
        };
        if (gltfAnimations.Count > 0) document["animations"] = gltfAnimations;
        if (materialEncoder.Materials.Count > 0)
        {
            document["materials"] = materialEncoder.Materials;
            document["samplers"] = materialEncoder.Samplers;
            document["images"] = materialEncoder.Images;
            document["textures"] = materialEncoder.Textures;
        }
        return BuildGlb(JsonSerializer.SerializeToUtf8Bytes(document, JsonOptions), binary.ToArray());
    }

    private static bool SkeletonMatches(UnrealSkeletalMesh mesh, UnrealMeshAnimation animation) =>
        mesh.Bones.Count == animation.Bones.Count && mesh.Bones.Select(item => item.Name)
            .SequenceEqual(animation.Bones.Select(item => item.Name), StringComparer.OrdinalIgnoreCase);

    private static float[] Timeline(int count, IReadOnlyList<float> times, float rate)
    {
        if (times.Count == count) return times.Select(value => rate > 0 ? value / rate : value).ToArray();
        return Enumerable.Range(0, count).Select(index => rate > 0 ? index / rate : index).ToArray();
    }

    private static Matrix4x4 BoneTransform(UnrealSkeletalBone bone, bool conjugateRoot) =>
        Matrix4x4.CreateFromQuaternion(UnrealGltfTransform.Rotation(bone.Orientation, conjugateRoot)) *
        Matrix4x4.CreateTranslation(UnrealGltfTransform.Position(bone.Position));

    private static Matrix4x4 InverseBindMatrix(Matrix4x4 value)
    {
        var inverse = Matrix4x4.Invert(value, out var result) ? result : Matrix4x4.Identity;
        inverse.M14 = 0;
        inverse.M24 = 0;
        inverse.M34 = 0;
        inverse.M44 = 1;
        return inverse;
    }

    private static Vector4 ConvertAnimationQuaternion(Quaternion value, bool conjugateRoot)
    {
        var converted = UnrealGltfTransform.Rotation(value, conjugateRoot);
        return new Vector4(converted.X, converted.Y, converted.Z, converted.W);
    }

    private static void WriteVector2(Stream stream, Vector2 value) { WriteSingle(stream, value.X); WriteSingle(stream, value.Y); }
    private static void WriteVector3(Stream stream, Vector3 value) { WriteSingle(stream, value.X); WriteSingle(stream, value.Y); WriteSingle(stream, value.Z); }
    private static void WriteVector4(Stream stream, Vector4 value) { WriteSingle(stream, value.X); WriteSingle(stream, value.Y); WriteSingle(stream, value.Z); WriteSingle(stream, value.W); }
    private static void WriteJoints(Stream stream, UnrealSkeletalWeight value)
    { WriteUInt16(stream, value.Bone0); WriteUInt16(stream, value.Bone1); WriteUInt16(stream, value.Bone2); WriteUInt16(stream, value.Bone3); }
    private static void WriteMatrix(Stream stream, Matrix4x4 value)
    {
        WriteSingle(stream, value.M11); WriteSingle(stream, value.M12); WriteSingle(stream, value.M13); WriteSingle(stream, value.M14);
        WriteSingle(stream, value.M21); WriteSingle(stream, value.M22); WriteSingle(stream, value.M23); WriteSingle(stream, value.M24);
        WriteSingle(stream, value.M31); WriteSingle(stream, value.M32); WriteSingle(stream, value.M33); WriteSingle(stream, value.M34);
        WriteSingle(stream, value.M41); WriteSingle(stream, value.M42); WriteSingle(stream, value.M43); WriteSingle(stream, value.M44);
    }
    private static void WriteSingle(Stream stream, float value) => WriteUInt32(stream, BitConverter.SingleToUInt32Bits(value));
    private static void WriteUInt16(Stream stream, ushort value)
    { Span<byte> bytes = stackalloc byte[2]; BinaryPrimitives.WriteUInt16LittleEndian(bytes, value); stream.Write(bytes); }
    private static void WriteUInt32(Stream stream, uint value)
    { Span<byte> bytes = stackalloc byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(bytes, value); stream.Write(bytes); }
    private static void Pad(Stream stream, int boundary, byte value)
    { while (stream.Position % boundary != 0) stream.WriteByte(value); }

    private static byte[] BuildGlb(byte[] json, byte[] binary)
    {
        using var output = new MemoryStream();
        var jsonLength = (json.Length + 3) & ~3;
        var binaryLength = (binary.Length + 3) & ~3;
        WriteUInt32(output, GlbMagic); WriteUInt32(output, 2); WriteUInt32(output, checked((uint)(12 + 8 + jsonLength + 8 + binaryLength)));
        WriteUInt32(output, (uint)jsonLength); WriteUInt32(output, JsonChunkType); output.Write(json); Pad(output, 4, 0x20);
        WriteUInt32(output, (uint)binaryLength); WriteUInt32(output, BinaryChunkType); output.Write(binary); Pad(output, 4, 0);
        return output.ToArray();
    }
}
