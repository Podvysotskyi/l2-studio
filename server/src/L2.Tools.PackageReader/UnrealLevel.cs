using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealObjectReference(
    string PackageName,
    string ObjectName,
    string ClassName)
{
    public string Path => string.IsNullOrEmpty(PackageName)
        ? ObjectName
        : $"{PackageName}.{ObjectName}";
}

public readonly record struct UnrealRotator(int Pitch, int Yaw, int Roll);

public readonly record struct UnrealRange(float Min, float Max);

public readonly record struct UnrealVectorRange(Vector3 Min, Vector3 Max);
public readonly record struct UnrealParticleColorScale(float RelativeTime, UnrealColor Color);
public readonly record struct UnrealParticleSizeScale(float RelativeTime, float RelativeSize);
public readonly record struct UnrealParticleBeamEndPoint(
    string ActorTag,
    UnrealVectorRange Offset,
    float Weight);

public readonly record struct UnrealCoordinateFrame(
    Vector3 Origin,
    Vector3 XAxis,
    Vector3 YAxis,
    Vector3 ZAxis)
{
    public Vector3 TransformPoint(Vector3 value)
    {
        var relative = value - Origin;
        return new Vector3(
            Vector3.Dot(relative, XAxis),
            Vector3.Dot(relative, YAxis),
            Vector3.Dot(relative, ZAxis));
    }
}

public sealed record UnrealLevelActor(
    string Name,
    string ClassName,
    Vector3 Location,
    UnrealRotator Rotation,
    Vector3 PrePivot,
    float DrawScale,
    Vector3 DrawScale3D,
    UnrealObjectReference? StaticMesh,
    UnrealObjectReference? StaticMeshInstance = null,
    IReadOnlyList<UnrealColor>? VertexLighting = null,
    string? VertexLightingError = null);

public sealed record UnrealTerrainInfo(
    string Name,
    Vector3 Location,
    UnrealRotator Rotation,
    Vector3 TerrainScale,
    UnrealCoordinateFrame ToWorld,
    UnrealCoordinateFrame ToHeightMap,
    UnrealObjectReference? TerrainMap,
    IReadOnlyList<UnrealTerrainLayer> Layers,
    bool CoordinateFramesDerived = false);

public readonly record struct UnrealTerrainUvTransformRow(
    float X,
    float Y,
    float Z,
    float Offset);

public readonly record struct UnrealTerrainUvTransform(
    UnrealTerrainUvTransformRow U,
    UnrealTerrainUvTransformRow V);

public sealed record UnrealTerrainLayer(
    int Index,
    UnrealObjectReference? Texture,
    UnrealObjectReference? AlphaMap,
    float UScale,
    float VScale,
    float UPan,
    float VPan,
    byte TextureMapAxis,
    float TextureRotation,
    UnrealRotator LayerRotation);

public sealed record UnrealLevelLight(
    string Name,
    string ClassName,
    Vector3 Location,
    UnrealRotator Rotation,
    float Brightness,
    byte Hue,
    byte Saturation,
    float Radius,
    IReadOnlyDictionary<string, string>? Properties = null);

public sealed record UnrealDistanceFog(
    UnrealColor Color,
    float Start,
    float End);

public sealed record UnrealLevelEnvironment(
    string SourceName,
    string SourceClass,
    UnrealColor AmbientColor,
    float AmbientBrightness,
    UnrealDistanceFog? DistanceFog);

public sealed record UnrealSkyZoneLensFlare(
    int Index,
    UnrealObjectReference Texture,
    float Offset,
    float Scale);

public sealed record UnrealSkyZoneInfo(
    int Order,
    string Name,
    Vector3 Location,
    float DrawScale,
    float TexUPanSpeed,
    float TexVPanSpeed,
    IReadOnlyList<UnrealSkyZoneLensFlare> LensFlares);

public sealed record UnrealSkyBackdrop(
    string Name,
    UnrealStaticMesh? Mesh,
    string? Error);

public sealed record UnrealBrushGeometry(
    IReadOnlyList<Vector3> Positions,
    IReadOnlyList<Vector3> Normals,
    IReadOnlyList<ushort> Indices)
{
    public int TriangleCount => Indices.Count / 3;
}

public sealed record UnrealBrushFace(
    IReadOnlyList<int> PointIndices,
    Vector3 Normal);

public static class UnrealBrushGeometryBuilder
{
    public static UnrealBrushGeometry Build(
        string name,
        IReadOnlyList<Vector3> points,
        IReadOnlyList<UnrealBrushFace> faces)
    {
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var indices = new List<ushort>();
        var edges = new Dictionary<(int A, int B), int>();
        foreach (var face in faces)
        {
            if (face.PointIndices.Count < 3)
                throw new InvalidDataException($"Brush model '{name}' contains an invalid or degenerate face.");
            if (positions.Count + face.PointIndices.Count > ushort.MaxValue)
                throw new InvalidDataException($"Brush model '{name}' exceeds the GLB 16-bit vertex limit.");
            var start = positions.Count;
            var normal = face.Normal.LengthSquared() > 0 ? Vector3.Normalize(face.Normal) : Vector3.Zero;
            if (!Finite(normal) || normal == Vector3.Zero)
                throw new InvalidDataException($"Brush model '{name}' contains an invalid face normal.");
            foreach (var pointIndex in face.PointIndices)
            {
                if (pointIndex < 0 || pointIndex >= points.Count)
                    throw new InvalidDataException($"Brush model '{name}' contains an invalid point index {pointIndex}.");
                if (!Finite(points[pointIndex]))
                    throw new InvalidDataException($"Brush model '{name}' contains a non-finite coordinate.");
                positions.Add(points[pointIndex]);
                normals.Add(normal);
            }
            for (var index = 0; index < face.PointIndices.Count; index++)
            {
                var a = face.PointIndices[index];
                var b = face.PointIndices[(index + 1) % face.PointIndices.Count];
                if (a == b) throw new InvalidDataException($"Brush model '{name}' contains a zero-length edge.");
                var edge = a < b ? (a, b) : (b, a);
                edges[edge] = edges.GetValueOrDefault(edge) + 1;
            }
            for (var index = 0; index < face.PointIndices.Count - 2; index++)
            {
                var a = positions[start];
                var b = positions[start + index + 2];
                var c = positions[start + index + 1];
                if (Vector3.Cross(b - a, c - a).LengthSquared() <= 1e-8f)
                    throw new InvalidDataException($"Brush model '{name}' contains a degenerate triangle.");
                indices.Add((ushort)start);
                indices.Add((ushort)(start + index + 2));
                indices.Add((ushort)(start + index + 1));
            }
        }
        if (indices.Count == 0) throw new InvalidDataException($"Brush model '{name}' has no boundary geometry.");
        if (edges.Any(edge => edge.Value != 2))
            throw new InvalidDataException($"Brush model '{name}' boundary is not closed.");
        return new UnrealBrushGeometry(positions, normals, indices);
    }

    private static bool Finite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}

public sealed record UnrealWaterVolume(
    string Name,
    string ClassName,
    Vector3 Location,
    UnrealRotator Rotation,
    Vector3 PrePivot,
    float DrawScale,
    Vector3 DrawScale3D,
    UnrealObjectReference? Brush,
    UnrealBrushGeometry? Geometry,
    string? Error);

public sealed record UnrealLevel(
    IReadOnlyList<UnrealLevelActor> Actors,
    IReadOnlyList<UnrealTerrainInfo> Terrains,
    IReadOnlyList<UnrealLevelLight> Lights,
    IReadOnlyList<UnrealWaterVolume> WaterVolumes,
    IReadOnlyDictionary<string, int> UnrepresentedObjectClasses,
    UnrealLevelEnvironment? Environment = null,
    string? EnvironmentWarning = null,
    IReadOnlyList<UnrealBspModel>? BspModelData = null,
    IReadOnlyList<UnrealSkyZoneInfo>? SkyZoneData = null)
{
    public IReadOnlyList<UnrealBspModel> BspModels { get; } = BspModelData ?? [];
    public IReadOnlyList<UnrealSkyZoneInfo> SkyZones { get; } = SkyZoneData ?? [];
}

public sealed record UnrealSceneObject(
    int Order,
    string Name,
    string ClassName,
    Vector3 Location,
    UnrealRotator Rotation,
    float Duration,
    UnrealObjectReference? Target,
    IReadOnlyDictionary<string, string> Properties,
    string? Owner = null);

public sealed record UnrealScene(
    UnrealLevel Level,
    IReadOnlyList<UnrealSkyZoneInfo> SkyZones,
    IReadOnlyList<UnrealSkyBackdrop> SkyBackdrops,
    IReadOnlyList<UnrealSceneObject> Cameras,
    IReadOnlyList<UnrealSceneObject> InterpolationPoints,
    IReadOnlyList<UnrealSceneObject> SceneManagers,
    IReadOnlyList<UnrealSceneObject> Actions,
    IReadOnlyList<UnrealSceneObject> AmbientSounds,
    IReadOnlyList<UnrealSceneObject> Effects);
