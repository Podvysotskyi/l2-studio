using System.Numerics;

namespace L2.Tools.PackageReader;

public static class UnrealBspMeshBuilder
{
    private const int MaximumVertices = ushort.MaxValue;
    private const UnrealPolyFlags RenderFlagMask =
        UnrealPolyFlags.Masked |
        UnrealPolyFlags.Translucent |
        UnrealPolyFlags.Modulated |
        UnrealPolyFlags.TwoSided |
        UnrealPolyFlags.Unlit;

    public static UnrealBspModel Build(
        UnrealModelData model,
        UnrealModelSurfaceSelection selection,
        IReadOnlyList<UnrealSkyZoneInfo>? skyZones = null,
        IReadOnlyList<UnrealWaterVolume>? waterVolumes = null)
    {
        var splitterNodes = 0;
        var invisibleSurfaces = new HashSet<int>();
        var portalSurfaces = new HashSet<int>();
        var backdropSurfaces = new HashSet<int>();
        var malformedSurfaces = 0;
        var unresolvedMaterialReferences = new HashSet<int>();
        var groups = new Dictionary<GroupKey, List<ChunkBuilder>>();
        var groupOrder = new List<GroupKey>();
        var skyZoneNames = selection == UnrealModelSurfaceSelection.World
            ? ResolveSkyZoneNames(model, skyZones ?? [])
            : [];
        var waterBoundaries = selection == UnrealModelSurfaceSelection.World
            ? WaterBoundaries(waterVolumes ?? [])
            : [];

        foreach (var node in model.Nodes)
        {
            if (node.VertexCount == 0)
            {
                splitterNodes++;
                continue;
            }
            if (node.Surface < 0 || node.Surface >= model.Surfaces.Count)
            {
                malformedSurfaces++;
                continue;
            }

            var surface = model.Surfaces[node.Surface];
            if (surface.MaterialReferenceInvalid)
                unresolvedMaterialReferences.Add(node.Surface);
            if ((surface.Flags & UnrealPolyFlags.Invisible) != 0)
            {
                invisibleSurfaces.Add(node.Surface);
                continue;
            }
            if ((surface.Flags & UnrealPolyFlags.Portal) != 0)
            {
                portalSurfaces.Add(node.Surface);
                continue;
            }

            var fakeBackdrop = (surface.Flags & UnrealPolyFlags.FakeBackdrop) != 0;
            if (selection == UnrealModelSurfaceSelection.World && fakeBackdrop)
            {
                backdropSurfaces.Add(node.Surface);
                continue;
            }
            if (selection == UnrealModelSurfaceSelection.FakeBackdrop && !fakeBackdrop)
                continue;

            if (node.VertexCount < 3 ||
                node.VertexPool < 0 ||
                node.VertexPool > model.Vertices.Count - node.VertexCount ||
                surface.BasePoint < 0 || surface.BasePoint >= model.Points.Count ||
                surface.TextureU < 0 || surface.TextureU >= model.Vectors.Count ||
                surface.TextureV < 0 || surface.TextureV >= model.Vectors.Count ||
                !Finite(node.Normal) || node.Normal.LengthSquared() <= 1e-8f)
            {
                malformedSurfaces++;
                continue;
            }

            var pointIndices = model.Vertices
                .Skip(node.VertexPool)
                .Take(node.VertexCount)
                .ToArray();
            if (pointIndices.Any(index => index < 0 || index >= model.Points.Count))
            {
                malformedSurfaces++;
                continue;
            }
            if (!Finite(model.Points[surface.BasePoint]) ||
                !Finite(model.Vectors[surface.TextureU]) ||
                !Finite(model.Vectors[surface.TextureV]) ||
                pointIndices.Any(index => !Finite(model.Points[index])))
            {
                malformedSurfaces++;
                continue;
            }

            var matchingWaterVolumes = MatchingWaterVolumes(
                model.Points,
                pointIndices,
                waterBoundaries);
            var skyZoneName = SkyZoneName(node, skyZoneNames);
            var role = skyZoneName is not null
                ? UnrealBspMeshRole.SkyZone
                : IsPrimaryWaterMaterial(surface.Material) || matchingWaterVolumes.Length > 0
                    ? UnrealBspMeshRole.WaterSurface
                    : UnrealBspMeshRole.Geometry;
            var key = new GroupKey(
                surface.Material?.PackageName ?? string.Empty,
                surface.Material?.ObjectName ?? string.Empty,
                surface.Flags & RenderFlagMask,
                role,
                skyZoneName);
            if (!groups.TryGetValue(key, out var chunks))
            {
                chunks = [];
                groups.Add(key, chunks);
                groupOrder.Add(key);
            }
            var chunk = chunks.LastOrDefault();
            if (chunk is null || chunk.VertexCount + pointIndices.Length > MaximumVertices)
            {
                chunk = new ChunkBuilder(surface.Material);
                chunks.Add(chunk);
            }
            if (!chunk.Add(
                    model.Points,
                    model.Vectors,
                    pointIndices,
                    node.Normal,
                    surface,
                    role == UnrealBspMeshRole.WaterSurface
                        ? matchingWaterVolumes
                        : []))
                malformedSurfaces++;
        }

        var result = new List<UnrealBspMeshChunk>();
        foreach (var key in groupOrder)
        {
            foreach (var chunk in groups[key])
            {
                if (chunk.TriangleCount == 0) continue;
                var chunkName = $"{model.Name}-bsp-{result.Count:D4}";
                var mesh = chunk.Build(chunkName);
                result.Add(new UnrealBspMeshChunk(
                    chunkName,
                    mesh,
                    chunk.SurfaceCount,
                    key.Flags,
                    key.Role == UnrealBspMeshRole.SkyZone
                        ? UnrealBspMeshRole.SkyZone
                        : IsWorldBase(mesh)
                            ? UnrealBspMeshRole.WorldBase
                        : key.Role == UnrealBspMeshRole.WaterSurface
                            ? UnrealBspMeshRole.WaterSurface
                            : UnrealBspMeshRole.Geometry,
                    key.SkyZoneName,
                    chunk.WaterVolumeNames));
            }
        }

        return new UnrealBspModel(
            model.Name,
            result,
            new UnrealBspDiagnostics(
                splitterNodes,
                invisibleSurfaces.Count,
                portalSurfaces.Count,
                backdropSurfaces.Count,
                malformedSurfaces,
                unresolvedMaterialReferences.Count),
            null);
    }

    private static bool Finite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    private static bool IsPrimaryWaterMaterial(UnrealObjectReference? material) =>
        material is not null &&
        (string.Equals(
             material.ObjectName,
             "WaterSurfaceShaderSet.WaterShader01",
             StringComparison.OrdinalIgnoreCase) ||
         string.Equals(
             material.ObjectName,
             "WaterSurfaceShaderSet.WaterFinal",
             StringComparison.OrdinalIgnoreCase) ||
         string.Equals(
             material.ObjectName,
             "water.WaterSurface",
             StringComparison.OrdinalIgnoreCase));

    private static WaterBoundary[] WaterBoundaries(IReadOnlyList<UnrealWaterVolume> volumes)
    {
        var result = new List<WaterBoundary>();
        foreach (var volume in volumes)
        {
            if (volume.Geometry is null) continue;
            var points = volume.Geometry.Positions
                .Select(point => TransformVolumePoint(point, volume))
                .ToArray();
            var candidates = new List<(WaterBoundary Boundary, float Height)>();
            for (var index = 0; index < volume.Geometry.Indices.Count; index += 3)
            {
                var a = points[volume.Geometry.Indices[index]];
                var b = points[volume.Geometry.Indices[index + 1]];
                var c = points[volume.Geometry.Indices[index + 2]];
                var cross = Vector3.Cross(b - a, c - a);
                if (!Finite(a) || !Finite(b) || !Finite(c) || cross.LengthSquared() <= 1e-8f)
                    continue;
                var normal = Vector3.Normalize(cross);
                if (MathF.Abs(normal.Z) < 0.99f) continue;
                candidates.Add((
                    new WaterBoundary(volume.Name, a, b, c, normal),
                    (a.Z + b.Z + c.Z) / 3));
            }
            if (candidates.Count == 0) continue;
            var maximumHeight = candidates.Max(candidate => candidate.Height);
            result.AddRange(candidates
                .Where(candidate => maximumHeight - candidate.Height <= 32)
                .Select(candidate => candidate.Boundary));
        }
        return result.ToArray();
    }

    private static Vector3 TransformVolumePoint(Vector3 point, UnrealWaterVolume volume)
    {
        const float rotationUnit = MathF.PI * 2 / 65536;
        var pitch = volume.Rotation.Pitch * rotationUnit;
        var yaw = volume.Rotation.Yaw * rotationUnit;
        var roll = volume.Rotation.Roll * rotationUnit;
        var sr = MathF.Sin(roll);
        var sp = MathF.Sin(pitch);
        var sy = MathF.Sin(yaw);
        var cr = MathF.Cos(roll);
        var cp = MathF.Cos(pitch);
        var cy = MathF.Cos(yaw);
        var xAxis = new Vector3(cp * cy, cp * sy, sp);
        var yAxis = new Vector3(sr * sp * cy - cr * sy, sr * sp * sy + cr * cy, -sr * cp);
        var zAxis = new Vector3(-(cr * sp * cy + sr * sy), cy * sr - cr * sp * sy, cr * cp);
        var relative = (point - volume.PrePivot) * volume.DrawScale * volume.DrawScale3D;
        return volume.Location + xAxis * relative.X + yAxis * relative.Y + zAxis * relative.Z;
    }

    private static string[] MatchingWaterVolumes(
        IReadOnlyList<Vector3> points,
        IReadOnlyList<int> pointIndices,
        IReadOnlyList<WaterBoundary> boundaries)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        var a = points[pointIndices[0]];
        for (var index = 0; index < pointIndices.Count - 2; index++)
        {
            var b = points[pointIndices[index + 2]];
            var c = points[pointIndices[index + 1]];
            if (Vector3.Cross(b - a, c - a).LengthSquared() <= 1e-8f) continue;
            foreach (var boundary in boundaries)
            {
                if (CoplanarTrianglesOverlap(a, b, c, boundary))
                    result.Add(boundary.Name);
            }
        }
        return result.Order(StringComparer.Ordinal).ToArray();
    }

    private static bool CoplanarTrianglesOverlap(
        Vector3 a,
        Vector3 b,
        Vector3 c,
        WaterBoundary boundary)
    {
        const float maximumPlaneDistance = 32;
        const float minimumNormalAlignment = 0.99f;
        var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
        if (MathF.Abs(Vector3.Dot(normal, boundary.Normal)) < minimumNormalAlignment)
            return false;
        if (new[] { a, b, c }.Any(point =>
                MathF.Abs(Vector3.Dot(point - boundary.A, boundary.Normal)) > maximumPlaneDistance) ||
            new[] { boundary.A, boundary.B, boundary.C }.Any(point =>
                MathF.Abs(Vector3.Dot(point - a, normal)) > maximumPlaneDistance))
            return false;

        var axis = DominantAxis(normal);
        var first = new[] { Project(a, axis), Project(b, axis), Project(c, axis) };
        var second = new[]
        {
            Project(boundary.A, axis),
            Project(boundary.B, axis),
            Project(boundary.C, axis)
        };
        return TrianglesOverlap(first, second);
    }

    private static int DominantAxis(Vector3 normal)
    {
        var absolute = Vector3.Abs(normal);
        return absolute.X >= absolute.Y && absolute.X >= absolute.Z
            ? 0
            : absolute.Y >= absolute.Z ? 1 : 2;
    }

    private static Vector2 Project(Vector3 point, int omittedAxis) => omittedAxis switch
    {
        0 => new Vector2(point.Y, point.Z),
        1 => new Vector2(point.X, point.Z),
        _ => new Vector2(point.X, point.Y)
    };

    private static bool TrianglesOverlap(IReadOnlyList<Vector2> first, IReadOnlyList<Vector2> second)
    {
        foreach (var triangle in new[] { first, second })
        {
            for (var index = 0; index < 3; index++)
            {
                var edge = triangle[(index + 1) % 3] - triangle[index];
                var axis = new Vector2(-edge.Y, edge.X);
                if (axis.LengthSquared() <= 1e-8f) continue;
                var firstProjection = first.Select(point => Vector2.Dot(point, axis)).ToArray();
                var secondProjection = second.Select(point => Vector2.Dot(point, axis)).ToArray();
                var overlap = MathF.Min(firstProjection.Max(), secondProjection.Max()) -
                    MathF.Max(firstProjection.Min(), secondProjection.Min());
                if (overlap <= 0.01f)
                    return false;
            }
        }
        return true;
    }

    private static Dictionary<byte, string> ResolveSkyZoneNames(
        UnrealModelData model,
        IReadOnlyList<UnrealSkyZoneInfo> skyZones)
    {
        var result = new Dictionary<byte, string>();
        foreach (var skyZone in skyZones.OrderBy(zone => zone.Order))
        {
            var zone = FindZone(model, skyZone.Location);
            if (zone != 0) result[zone] = skyZone.Name;
        }
        return result;
    }

    internal static byte FindZone(UnrealModelData model, Vector3 point)
    {
        if (model.Nodes.Count == 0 || !Finite(point)) return 0;
        var nodeIndex = 0;
        var visited = new HashSet<int>();
        while (nodeIndex >= 0 && nodeIndex < model.Nodes.Count && visited.Add(nodeIndex))
        {
            var node = model.Nodes[nodeIndex];
            var front = Vector3.Dot(point, node.Normal) - node.PlaneW >= 0;
            var child = front ? node.Front : node.Back;
            if (child < 0) return front ? node.FrontZone : node.BackZone;
            nodeIndex = child;
        }
        return 0;
    }

    private static string? SkyZoneName(
        UnrealModelNode node,
        IReadOnlyDictionary<byte, string> skyZoneNames)
    {
        if (node.BackZone != 0 && skyZoneNames.TryGetValue(node.BackZone, out var back))
            return back;
        return node.FrontZone != 0 && skyZoneNames.TryGetValue(node.FrontZone, out var front)
            ? front
            : null;
    }

    private static bool IsWorldBase(UnrealStaticMesh mesh)
    {
        const float epsilon = 0.01f;
        if (mesh.Positions.Count == 0 ||
            mesh.Normals.Any(normal =>
                MathF.Abs(normal.X) > epsilon ||
                MathF.Abs(normal.Y) > epsilon ||
                normal.Z < 1 - epsilon) ||
            mesh.Positions.Any(position => MathF.Abs(position.Z + 16384) > epsilon))
            return false;

        var minimumX = mesh.Positions.Min(position => position.X);
        var maximumX = mesh.Positions.Max(position => position.X);
        var minimumY = mesh.Positions.Min(position => position.Y);
        var maximumY = mesh.Positions.Max(position => position.Y);
        return maximumX - minimumX >= 655360 - epsilon &&
            maximumY - minimumY >= 524288 - epsilon;
    }

    private sealed class ChunkBuilder(UnrealObjectReference? material)
    {
        private readonly List<Vector3> positions = [];
        private readonly List<Vector3> normals = [];
        private readonly List<Vector2> textureCoordinates = [];
        private readonly List<ushort> indices = [];
        private readonly HashSet<string> waterVolumeNames = new(StringComparer.Ordinal);

        public int VertexCount => positions.Count;
        public int TriangleCount => indices.Count / 3;
        public int SurfaceCount { get; private set; }
        public string[] WaterVolumeNames => waterVolumeNames.Order(StringComparer.Ordinal).ToArray();

        public bool Add(
            IReadOnlyList<Vector3> points,
            IReadOnlyList<Vector3> vectors,
            IReadOnlyList<int> pointIndices,
            Vector3 normal,
            UnrealModelSurface surface,
            IReadOnlyList<string> matchingWaterVolumes)
        {
            var start = positions.Count;
            var basePoint = points[surface.BasePoint];
            var textureU = vectors[surface.TextureU];
            var textureV = vectors[surface.TextureV];
            var addedTriangles = 0;
            foreach (var pointIndex in pointIndices)
            {
                var point = points[pointIndex];
                positions.Add(point);
                normals.Add(Vector3.Normalize(normal));
                var relative = point - basePoint;
                textureCoordinates.Add(new Vector2(
                    Vector3.Dot(relative, textureU) / 256f,
                    Vector3.Dot(relative, textureV) / 256f));
            }
            for (var index = 0; index < pointIndices.Count - 2; index++)
            {
                var a = positions[start];
                var b = positions[start + index + 2];
                var c = positions[start + index + 1];
                if (Vector3.Cross(b - a, c - a).LengthSquared() <= 1e-8f) continue;
                indices.Add((ushort)start);
                indices.Add((ushort)(start + index + 2));
                indices.Add((ushort)(start + index + 1));
                addedTriangles++;
            }
            if (addedTriangles == 0)
            {
                positions.RemoveRange(start, pointIndices.Count);
                normals.RemoveRange(start, pointIndices.Count);
                textureCoordinates.RemoveRange(start, pointIndices.Count);
                return false;
            }
            SurfaceCount++;
            foreach (var name in matchingWaterVolumes) waterVolumeNames.Add(name);
            return true;
        }

        public UnrealStaticMesh Build(string name) => new(
            name,
            positions,
            normals,
            textureCoordinates,
            indices,
            [new UnrealStaticMeshSection(0, indices.Count, material)],
            null,
            null);
    }

    private readonly record struct GroupKey(
        string PackageName,
        string ObjectName,
        UnrealPolyFlags Flags,
        UnrealBspMeshRole Role,
        string? SkyZoneName);

    private readonly record struct WaterBoundary(
        string Name,
        Vector3 A,
        Vector3 B,
        Vector3 C,
        Vector3 Normal);
}
