using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using L2.Tools.PackageReader;

namespace L2.Tools.StaticMeshConverter;

public static class GlbAnimationEncoder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static byte[] Encode(UnrealMeshAnimation animation)
    {
        ArgumentNullException.ThrowIfNull(animation);
        if (animation.Bones.Count == 0 || animation.Clips.Count == 0)
            throw new InvalidDataException($"Animation set '{animation.Name}' is incomplete.");
        using var binary = new MemoryStream();
        var views = new List<object>();
        var accessors = new List<object>();

        int AddAccessor<T>(
            IEnumerable<T> values,
            Action<Stream, T> write,
            int elementSize,
            string type,
            float[]? minimum = null,
            float[]? maximum = null)
        {
            Pad(binary, 4, 0);
            var offset = checked((int)binary.Position);
            var materialized = values.ToArray();
            foreach (var value in materialized) write(binary, value);
            var view = views.Count;
            views.Add(new { buffer = 0, byteOffset = offset, byteLength = materialized.Length * elementSize });
            var accessor = accessors.Count;
            accessors.Add(new
            {
                bufferView = view,
                componentType = 5126,
                count = materialized.Length,
                type,
                min = minimum,
                max = maximum
            });
            return accessor;
        }

        var nodes = animation.Bones.Select((bone, index) => new
        {
            name = bone.Name,
            children = animation.Bones.Select((child, childIndex) => (child, childIndex))
                .Where(item => item.childIndex != index && item.child.ParentIndex == index)
                .Select(item => item.childIndex).ToArray() is { Length: > 0 } children ? children : null
        }).ToArray();
        var roots = animation.Bones.Select((bone, index) => (bone, index))
            .Where(item => item.bone.ParentIndex < 0 || item.bone.ParentIndex == item.index)
            .Select(item => item.index).ToArray();
        var animations = animation.Clips.Select(clip =>
        {
            var samplers = new List<object>();
            var channels = new List<object>();
            for (var boneIndex = 0; boneIndex < animation.Bones.Count && boneIndex < clip.Tracks.Count; boneIndex++)
            {
                var track = clip.Tracks[boneIndex];
                AddChannel(track.Rotations, value => ConvertQuaternion(value, boneIndex == 0),
                    WriteVector4, 16, "VEC4", "rotation");
                AddChannel(track.Translations, UnrealGltfTransform.Position,
                    WriteVector3, 12, "VEC3", "translation");

                void AddChannel<TInput, TOutput>(
                    IReadOnlyList<TInput> keys,
                    Func<TInput, TOutput> convert,
                    Action<Stream, TOutput> write,
                    int elementSize,
                    string type,
                    string path)
                {
                    if (keys.Count == 0) return;
                    var times = Timeline(keys.Count, track.Times, clip.FrameRate);
                    var input = AddAccessor(times, WriteSingle, 4, "SCALAR", [times[0]], [times[^1]]);
                    var output = AddAccessor(keys.Select(convert), write, elementSize, type);
                    var sampler = samplers.Count;
                    samplers.Add(new { input, output, interpolation = "LINEAR" });
                    channels.Add(new { sampler, target = new { node = boneIndex, path } });
                }
            }
            return new { name = clip.Name, samplers, channels };
        }).ToArray();
        var document = new
        {
            asset = new { version = "2.0", generator = "L2 Studio animation converter" },
            scene = 0,
            scenes = new[] { new { nodes = roots } },
            nodes,
            animations,
            accessors,
            bufferViews = views,
            buffers = new[] { new { byteLength = (int)binary.Length } }
        };
        return BuildGlb(JsonSerializer.SerializeToUtf8Bytes(document, JsonOptions), binary.ToArray());
    }

    private static float[] Timeline(int count, IReadOnlyList<float> times, float rate)
    {
        if (times.Count == count) return times.Select(value => rate > 0 ? value / rate : value).ToArray();
        return Enumerable.Range(0, count).Select(index => rate > 0 ? index / rate : index).ToArray();
    }

    private static Vector4 ConvertQuaternion(Quaternion value, bool conjugateRoot)
    {
        var converted = UnrealGltfTransform.Rotation(value, conjugateRoot);
        return new Vector4(converted.X, converted.Y, converted.Z, converted.W);
    }

    private static void WriteVector3(Stream stream, Vector3 value)
    { WriteSingle(stream, value.X); WriteSingle(stream, value.Y); WriteSingle(stream, value.Z); }
    private static void WriteVector4(Stream stream, Vector4 value)
    { WriteSingle(stream, value.X); WriteSingle(stream, value.Y); WriteSingle(stream, value.Z); WriteSingle(stream, value.W); }
    private static void WriteSingle(Stream stream, float value) => WriteUInt32(stream, BitConverter.SingleToUInt32Bits(value));
    private static void WriteUInt32(Stream stream, uint value)
    { Span<byte> bytes = stackalloc byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(bytes, value); stream.Write(bytes); }
    private static void Pad(Stream stream, int boundary, byte value)
    { while (stream.Position % boundary != 0) stream.WriteByte(value); }

    private static byte[] BuildGlb(byte[] json, byte[] binary)
    {
        using var output = new MemoryStream();
        var jsonLength = (json.Length + 3) & ~3;
        var binaryLength = (binary.Length + 3) & ~3;
        WriteUInt32(output, 0x46546c67); WriteUInt32(output, 2); WriteUInt32(output, checked((uint)(12 + 8 + jsonLength + 8 + binaryLength)));
        WriteUInt32(output, (uint)jsonLength); WriteUInt32(output, 0x4e4f534a); output.Write(json); Pad(output, 4, 0x20);
        WriteUInt32(output, (uint)binaryLength); WriteUInt32(output, 0x004e4942); output.Write(binary); Pad(output, 4, 0);
        return output.ToArray();
    }
}
