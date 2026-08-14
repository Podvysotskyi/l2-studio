using System.Globalization;
using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed partial class UnrealPackageReader
{
    private const int MaximumAnimationElements = 10_000_000;

    public UnrealAnimationPackage ReadAnimationPackage()
    {
        var header = ReadHeader();
        ReadNames(header);
        ReadImports(header);
        var exports = ReadExports(header);
        var meshes = new List<UnrealSkeletalMesh>();
        var animations = new List<UnrealMeshAnimation>();
        var vertexMeshes = 0;
        foreach (var export in exports)
        {
            var className = ResolveClassName(export.ClassIndex, exports);
            if (string.Equals(className, "SkeletalMesh", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    meshes.Add(ReadSkeletalMesh(export, exports));
                }
                catch (Exception exception) when (exception is InvalidDataException or OverflowException)
                {
                    meshes.Add(new UnrealSkeletalMesh(
                        ResolveObjectPath(export, exports), [], [], [], [], [], [], [], null,
                        Vector3.One, Vector3.Zero, new UnrealRotator(0, 0, 0), exception.Message));
                }
            }
            else if (string.Equals(className, "MeshAnimation", StringComparison.OrdinalIgnoreCase))
                animations.Add(ReadMeshAnimation(export, exports));
            else if (string.Equals(className, "VertMesh", StringComparison.OrdinalIgnoreCase))
                vertexMeshes++;
        }
        return new UnrealAnimationPackage(meshes, animations, vertexMeshes);
    }

    private UnrealSkeletalMesh ReadSkeletalMesh(ExportEntry export, IReadOnlyList<ExportEntry> exports)
    {
        var properties = ReadObjectProperties(export, exports, requireComplete: false, maximumBlocks: 1);
        var reader = new PackageCursor(data, properties.NativeOffset, properties.NativeLength);
        reader.Skip(41); // UPrimitive bounding box (25 bytes) and bounding sphere (16 bytes)
        var lodVersion = reader.ReadInt32();
        if (lodVersion is < 2 or > 6)
            throw new InvalidDataException($"Skeletal mesh '{export.ObjectName}' uses unsupported LOD version {lodVersion}.");
        _ = reader.ReadInt32(); // vertex count
        SkipArray(reader, 4, "packed mesh vertices");
        var materials = ReadArray(reader, "mesh materials", item =>
            ResolveObjectReference(item.ReadCompactIndex(), exports));
        var meshScale = reader.ReadVector3();
        var meshOrigin = reader.ReadVector3();
        var rotationOrigin = new UnrealRotator(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        SkipArray(reader, 2, "face levels");
        SkipArray(reader, 8, "mesh faces");
        SkipArray(reader, 2, "collapse wedges");
        SkipArray(reader, 10, "base wedges");
        var meshMaterialIndices = ReadArray(reader, "LOD materials", item =>
        {
            _ = item.ReadUInt32();
            return item.ReadInt32();
        });
        _ = reader.ReadSingle(); // maximum scale
        _ = reader.ReadSingle(); // hysteresis
        _ = reader.ReadSingle(); // strength
        _ = reader.ReadInt32(); // minimum vertices
        _ = reader.ReadSingle(); // morph
        _ = reader.ReadSingle(); // Z displacement
        if (lodVersion >= 3)
        {
            _ = reader.ReadInt32();
            _ = reader.ReadCompactIndex();
            reader.Skip(12 + 12 + 12 + 4 + 12);
        }
        if (lodVersion >= 4) _ = reader.ReadSingle();
        if (lodVersion >= 5)
        {
            _ = reader.ReadInt32();
            if (lodVersion >= 6) reader.Skip(1);
        }

        SkipArray(reader, 12, "secondary skeletal points");
        var bones = ReadArray(reader, "reference skeleton", item =>
        {
            var name = ReadName(item);
            _ = item.ReadUInt32();
            var orientation = ReadQuaternion(item);
            var position = item.ReadVector3();
            _ = item.ReadSingle();
            item.Skip(12);
            _ = item.ReadInt32(); // children
            var parent = item.ReadInt32();
            return new UnrealSkeletalBone(name, parent, orientation, position);
        });
        var animation = ResolveObjectReference(reader.ReadCompactIndex(), exports);
        _ = reader.ReadInt32(); // skeletal depth
        var weightIndexCount = ReadCount(reader, "weight indices");
        for (var index = 0; index < weightIndexCount; index++)
        {
            SkipArray(reader, 2, "weight-index bones");
            _ = reader.ReadInt32();
        }
        SkipArray(reader, 4, "compact bone influences");
        SkipNameArray(reader, "attachment aliases");
        SkipNameArray(reader, "attachment bones");
        SkipArray(reader, 48, "attachment coordinates");

        if (lodVersion <= 1)
            throw new InvalidDataException($"Skeletal mesh '{export.ObjectName}' uses the unsupported legacy layout.");
        var lodCount = ReadCount(reader, "skeletal LOD models");
        RawSkeletalLod? firstLod = null;
        for (var index = 0; index < lodCount; index++)
        {
            var lod = ReadSkeletalLod(reader);
            firstLod ??= lod;
        }
        _ = reader.ReadCompactIndex();
        var points = ReadLazyArray(reader, "skeletal points", item => item.ReadVector3());
        var wedges = ReadLazyArray(reader, "skeletal wedges", item =>
            new RawSkeletalWedge(item.ReadUInt16(), new Vector2(item.ReadSingle(), item.ReadSingle())));
        var triangles = ReadLazyArray(reader, "skeletal triangles", item =>
            new RawSkeletalTriangle(
                item.ReadUInt16(), item.ReadUInt16(), item.ReadUInt16(),
                item.ReadByte(), item.ReadByte(), item.ReadUInt32()));
        var influences = ReadLazyArray(reader, "skeletal influences", item =>
            new RawSkeletalInfluence(item.ReadSingle(), item.ReadUInt16(), item.ReadUInt16()));
        SkipLazyArray(reader, 2, "skeletal collapse wedges");
        SkipLazyArray(reader, 2, "skeletal triangle points");
        if (packageVersion >= 118 && licenseeVersion >= 3) _ = reader.ReadInt32();
        if (packageVersion >= 123 && licenseeVersion >= 0x12) SkipArray(reader, 4, "Lineage mesh metadata");
        if (packageVersion >= 120) _ = reader.ReadInt32();
        if (licenseeVersion >= 0x23) _ = reader.ReadInt32();

        if ((points.Count == 0 || wedges.Count == 0 || triangles.Count == 0) && firstLod is not null)
        {
            points = firstLod.Points;
            wedges = firstLod.Wedges;
            triangles = firstLod.Triangles;
            influences = firstLod.Influences;
        }
        return BuildSkeletalMesh(
            ResolveObjectPath(export, exports), points, wedges, triangles, influences,
            bones, materials, meshMaterialIndices, animation, meshScale, meshOrigin, rotationOrigin);
    }

    private UnrealSkeletalMesh BuildSkeletalMesh(
        string name,
        IReadOnlyList<Vector3> points,
        IReadOnlyList<RawSkeletalWedge> wedges,
        IReadOnlyList<RawSkeletalTriangle> triangles,
        IReadOnlyList<RawSkeletalInfluence> influences,
        IReadOnlyList<UnrealSkeletalBone> bones,
        IReadOnlyList<UnrealObjectReference?> materials,
        IReadOnlyList<int> materialIndices,
        UnrealObjectReference? animation,
        Vector3 meshScale,
        Vector3 meshOrigin,
        UnrealRotator rotationOrigin)
    {
        if (points.Count == 0 || wedges.Count == 0 || triangles.Count == 0 || bones.Count == 0)
            throw new InvalidDataException($"Skeletal mesh '{name}' has no renderable geometry or skeleton.");
        var positions = new Vector3[wedges.Count];
        var coordinates = new Vector2[wedges.Count];
        var normals = new Vector3[wedges.Count];
        for (var index = 0; index < wedges.Count; index++)
        {
            var wedge = wedges[index];
            if (wedge.PointIndex >= points.Count)
                throw new InvalidDataException($"Skeletal mesh '{name}' references point {wedge.PointIndex} outside its point array.");
            positions[index] = points[wedge.PointIndex];
            coordinates[index] = wedge.TextureCoordinate;
        }

        var groupedTriangles = triangles.GroupBy(item => item.MaterialIndex).OrderBy(item => item.Key).ToArray();
        var indices = new List<uint>(triangles.Count * 3);
        var sections = new List<UnrealSkeletalMeshSection>(groupedTriangles.Length);
        foreach (var group in groupedTriangles)
        {
            var first = indices.Count;
            foreach (var triangle in group)
            {
                var wedgeIndices = new[] { triangle.Wedge0, triangle.Wedge1, triangle.Wedge2 };
                if (wedgeIndices.Any(index => index >= wedges.Count))
                    throw new InvalidDataException($"Skeletal mesh '{name}' has a triangle outside its wedge array.");
                indices.Add(wedgeIndices[0]);
                indices.Add(wedgeIndices[2]);
                indices.Add(wedgeIndices[1]);
                var normal = Vector3.Cross(
                    positions[wedgeIndices[2]] - positions[wedgeIndices[0]],
                    positions[wedgeIndices[1]] - positions[wedgeIndices[0]]);
                if (normal.LengthSquared() > 0)
                {
                    normals[wedgeIndices[0]] += normal;
                    normals[wedgeIndices[1]] += normal;
                    normals[wedgeIndices[2]] += normal;
                }
            }
            UnrealObjectReference? material = null;
            if (group.Key < materialIndices.Count)
            {
                var textureIndex = materialIndices[group.Key];
                if (textureIndex >= 0 && textureIndex < materials.Count) material = materials[textureIndex];
            }
            sections.Add(new UnrealSkeletalMeshSection(first, indices.Count - first, material));
        }
        for (var index = 0; index < normals.Length; index++)
            normals[index] = normals[index].LengthSquared() > 0 ? Vector3.Normalize(normals[index]) : Vector3.UnitZ;

        var pointWeights = influences.GroupBy(item => item.PointIndex).ToDictionary(
            group => group.Key,
            group => group.Where(item => item.BoneIndex < bones.Count && item.Weight > 0)
                .OrderByDescending(item => item.Weight).Take(4).ToArray());
        var weights = wedges.Select(wedge => BuildWeight(pointWeights.GetValueOrDefault(wedge.PointIndex))).ToArray();
        return new UnrealSkeletalMesh(
            name, positions, normals, coordinates, indices, weights, bones, sections,
            animation, meshScale, meshOrigin, rotationOrigin);
    }

    private static UnrealSkeletalWeight BuildWeight(IReadOnlyList<RawSkeletalInfluence>? source)
    {
        if (source is null || source.Count == 0)
            return new UnrealSkeletalWeight(0, 0, 0, 0, new Vector4(1, 0, 0, 0));
        Span<ushort> bones = stackalloc ushort[4];
        Span<float> weights = stackalloc float[4];
        var total = source.Sum(item => item.Weight);
        for (var index = 0; index < source.Count; index++)
        {
            bones[index] = source[index].BoneIndex;
            weights[index] = source[index].Weight / total;
        }
        return new UnrealSkeletalWeight(
            bones[0], bones[1], bones[2], bones[3],
            new Vector4(weights[0], weights[1], weights[2], weights[3]));
    }

    private UnrealMeshAnimation ReadMeshAnimation(ExportEntry export, IReadOnlyList<ExportEntry> exports)
    {
        var properties = ReadObjectProperties(export, exports, requireComplete: false, maximumBlocks: 1);
        var reader = new PackageCursor(data, properties.NativeOffset, properties.NativeLength);
        _ = reader.ReadInt32(); // animation serialization version
        var bones = ReadArray(reader, "animation bones", item =>
            new UnrealAnimationBone(ReadName(item), ReadAnimationBoneTail(item)));
        var moves = ReadAnimationMoves(reader);
        var sequences = ReadArray(reader, "animation sequences", item => ReadAnimationSequence(item, exports));
        if (moves.Count != sequences.Count)
            throw new InvalidDataException($"Animation set '{export.ObjectName}' has {moves.Count} moves for {sequences.Count} sequences.");
        var clips = sequences.Select((sequence, index) =>
        {
            var move = moves[index];
            var timeScale = move.TrackTime > 0 ? sequence.FrameCount / move.TrackTime : 1;
            var tracks = Enumerable.Range(0, bones.Count).Select(trackIndex =>
            {
                if (trackIndex >= move.Tracks.Count)
                    return new UnrealAnimationTrack([], [], []);
                var track = move.Tracks[trackIndex];
                return new UnrealAnimationTrack(
                    track.Rotations,
                    track.Translations,
                    track.Times.Select(value => value * timeScale).ToArray());
            }).ToArray();
            return new UnrealAnimationClip(
                sequence.Name, sequence.FrameCount, sequence.FrameRate,
                sequence.Groups, tracks, sequence.Notifies);
        }).ToArray();
        return new UnrealMeshAnimation(ResolveObjectPath(export, exports), bones, clips);
    }

    private int ReadAnimationBoneTail(PackageCursor reader)
    {
        _ = reader.ReadUInt32();
        return reader.ReadInt32();
    }

    private IReadOnlyList<RawAnimationMove> ReadAnimationMoves(PackageCursor reader)
    {
        int count;
        int? finalPosition = null;
        if (packageVersion >= 123 && licenseeVersion >= 0x19)
        {
            finalPosition = reader.ReadInt32();
            count = ReadCount(reader, "Lineage animation moves");
        }
        else count = ReadCount(reader, "animation moves");
        var moves = new RawAnimationMove[count];
        for (var index = 0; index < count; index++)
        {
            int? localPosition = finalPosition is null ? null : reader.ReadInt32();
            moves[index] = ReadAnimationMove(reader);
            if (localPosition is not null && reader.Position != localPosition)
                throw new InvalidDataException("A Lineage animation move ended at an unexpected offset.");
        }
        if (finalPosition is not null && reader.Position != finalPosition)
            throw new InvalidDataException("The Lineage animation move array ended at an unexpected offset.");
        return moves;
    }

    private RawAnimationMove ReadAnimationMove(PackageCursor reader)
    {
        reader.Skip(12);
        var trackTime = reader.ReadSingle();
        _ = reader.ReadInt32();
        _ = reader.ReadUInt32();
        SkipArray(reader, 4, "animation bone indices");
        var tracks = ReadArray(reader, "animation tracks", ReadAnimationTrack);
        _ = ReadAnimationTrack(reader); // root track
        return new RawAnimationMove(trackTime, tracks);
    }

    private RawAnimationTrack ReadAnimationTrack(PackageCursor reader)
    {
        _ = reader.ReadUInt32();
        var rotations = ReadArray(reader, "rotation keys", ReadQuaternion);
        var translations = ReadArray(reader, "translation keys", item => item.ReadVector3());
        var times = ReadArray(reader, "animation key times", item => item.ReadSingle());
        return new RawAnimationTrack(rotations, translations, times);
    }

    private RawAnimationSequence ReadAnimationSequence(PackageCursor reader, IReadOnlyList<ExportEntry> exports)
    {
        if (packageVersion >= 115) _ = reader.ReadSingle();
        var name = ReadName(reader);
        var groups = ReadArray(reader, "animation groups", ReadName);
        _ = reader.ReadInt32(); // start frame; each move is already sequence-local
        var frameCount = reader.ReadInt32();
        var notifies = ReadArray(reader, "animation notifies", item => ReadAnimationNotify(item, exports));
        var rate = reader.ReadSingle();
        if (licenseeVersion >= 1)
        {
            _ = reader.ReadInt32();
            _ = reader.ReadInt32();
            if (licenseeVersion >= 2) _ = reader.ReadInt32();
            _ = reader.ReadCompactIndex();
            if (licenseeVersion >= 0x14) _ = reader.ReadInt32();
            if (licenseeVersion >= 0x19) _ = reader.ReadInt32();
            if (licenseeVersion >= 0x1A) SkipLineageSequenceMetadata(reader);
        }
        return new RawAnimationSequence(name, frameCount, rate, groups, notifies);
    }

    private UnrealAnimationNotify ReadAnimationNotify(PackageCursor reader, IReadOnlyList<ExportEntry> exports)
    {
        var time = reader.ReadSingle();
        var function = ReadName(reader);
        var objectIndex = packageVersion >= 112 ? reader.ReadCompactIndex() : 0;
        if (packageVersion >= 131)
        {
            var characters = reader.ReadInt32();
            if (characters < 0 || characters > MaximumAnimationElements) throw new InvalidDataException("Animation notify text is invalid.");
            reader.Skip(checked(characters * 2));
        }
        var reference = ResolveObjectReference(objectIndex, exports);
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (objectIndex > 0)
        {
            try
            {
                var export = exports[objectIndex - 1];
                var parsed = ReadObjectProperties(export, exports, requireComplete: false, maximumBlocks: 1);
                foreach (var property in parsed.Values)
                    values[property.Key] = FormatNotifyProperty(property.Value);
            }
            catch (Exception exception) when (exception is InvalidDataException or OverflowException)
            {
                values["parseError"] = exception.Message;
            }
        }
        return new UnrealAnimationNotify(time, function, reference?.Path, reference?.ClassName, values);
    }

    private static string FormatNotifyProperty(object? value) => value switch
    {
        null => "null",
        float number => number.ToString("R", CultureInfo.InvariantCulture),
        double number => number.ToString("R", CultureInfo.InvariantCulture),
        Vector3 vector => string.Create(CultureInfo.InvariantCulture, $"{vector.X:R},{vector.Y:R},{vector.Z:R}"),
        UnrealRotator rotation => $"{rotation.Pitch},{rotation.Yaw},{rotation.Roll}",
        UnrealObjectReference reference => reference.Path,
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };

    private void SkipLineageSequenceMetadata(PackageCursor reader)
    {
        if (licenseeVersion == 0x1A)
        {
            SkipArray(reader, 8, "Lineage sequence metadata");
            return;
        }
        _ = reader.ReadByte();
        SkipArray(reader, 8, "Lineage sequence pairs");
        var groups = ReadCount(reader, "Lineage sequence groups");
        for (var index = 0; index < groups; index++)
        {
            _ = reader.ReadInt32();
            SkipArray(reader, 8, "Lineage sequence group pairs");
        }
        _ = reader.ReadInt32();
        _ = reader.ReadInt32();
        SkipArray(reader, 8, "Lineage sequence tail pairs");
    }

    private RawSkeletalLod ReadSkeletalLod(PackageCursor reader)
    {
        SkipArray(reader, 4, "LOD skinning data");
        SkipArray(reader, 16, "LOD skin points");
        _ = reader.ReadInt32();
        SkipSectionArray(reader);
        SkipSectionArray(reader);
        SkipRawIndexBuffer(reader);
        SkipRawIndexBuffer(reader);
        reader.Skip(12);
        SkipArray(reader, 32, "LOD stream vertices");
        var influences = ReadLazyArray(reader, "LOD influences", item =>
            new RawSkeletalInfluence(item.ReadSingle(), item.ReadUInt16(), item.ReadUInt16()));
        var wedges = ReadLazyArray(reader, "LOD wedges", item =>
            new RawSkeletalWedge(item.ReadUInt16(), new Vector2(item.ReadSingle(), item.ReadSingle())));
        var triangles = ReadLazyArray(reader, "LOD faces", item =>
            new RawSkeletalTriangle(
                item.ReadUInt16(), item.ReadUInt16(), item.ReadUInt16(),
                checked((byte)item.ReadUInt16()), 0, 0));
        var points = ReadLazyArray(reader, "LOD points", item => item.ReadVector3());
        reader.Skip(8 + 16);
        if (licenseeVersion >= 0x1C)
        {
            _ = reader.ReadInt32();
            SkipArray(reader, 60, "Lineage LOD wedges");
        }
        return new RawSkeletalLod(points, wedges, triangles, influences);
    }

    private void SkipSectionArray(PackageCursor reader)
    {
        var count = ReadCount(reader, "skeletal mesh sections");
        for (var index = 0; index < count; index++)
        {
            reader.Skip(18);
            if (licenseeVersion >= 0x1C) SkipArray(reader, 4, "section bone map");
        }
    }

    private void SkipRawIndexBuffer(PackageCursor reader)
    {
        SkipArray(reader, 2, "LOD indices");
        _ = reader.ReadInt32();
    }

    private void SkipNameArray(PackageCursor reader, string description)
    {
        var count = ReadCount(reader, description);
        for (var index = 0; index < count; index++) _ = ReadName(reader);
    }

    private IReadOnlyList<T> ReadLazyArray<T>(PackageCursor reader, string description, Func<PackageCursor, T> read)
    {
        if (packageVersion > 61) _ = reader.ReadInt32();
        return ReadArray(reader, description, read);
    }

    private void SkipLazyArray(PackageCursor reader, int itemSize, string description)
    {
        if (packageVersion > 61) _ = reader.ReadInt32();
        SkipArray(reader, itemSize, description);
    }

    private static Quaternion ReadQuaternion(PackageCursor reader) => new(
        reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    private static IReadOnlyList<T> ReadArray<T>(PackageCursor reader, string description, Func<PackageCursor, T> read)
    {
        var count = ReadCount(reader, description);
        var values = new T[count];
        for (var index = 0; index < count; index++) values[index] = read(reader);
        return values;
    }

    private static int ReadCount(PackageCursor reader, string description)
    {
        var count = reader.ReadCompactIndex();
        if (count < 0 || count > MaximumAnimationElements)
            throw new InvalidDataException($"The {description} count {count} is invalid.");
        return count;
    }

    private static void SkipArray(PackageCursor reader, int itemSize, string description)
    {
        var count = ReadCount(reader, description);
        reader.Skip(checked(count * itemSize));
    }

    private sealed record RawSkeletalWedge(ushort PointIndex, Vector2 TextureCoordinate);
    private sealed record RawSkeletalTriangle(
        ushort Wedge0, ushort Wedge1, ushort Wedge2, byte MaterialIndex, byte AuxiliaryMaterialIndex, uint SmoothingGroups);
    private sealed record RawSkeletalInfluence(float Weight, ushort PointIndex, ushort BoneIndex);
    private sealed record RawSkeletalLod(
        IReadOnlyList<Vector3> Points,
        IReadOnlyList<RawSkeletalWedge> Wedges,
        IReadOnlyList<RawSkeletalTriangle> Triangles,
        IReadOnlyList<RawSkeletalInfluence> Influences);
    private sealed record RawAnimationTrack(
        IReadOnlyList<Quaternion> Rotations, IReadOnlyList<Vector3> Translations, IReadOnlyList<float> Times);
    private sealed record RawAnimationMove(float TrackTime, IReadOnlyList<RawAnimationTrack> Tracks);
    private sealed record RawAnimationSequence(
        string Name,
        int FrameCount,
        float FrameRate,
        IReadOnlyList<string> Groups,
        IReadOnlyList<UnrealAnimationNotify> Notifies);
}
