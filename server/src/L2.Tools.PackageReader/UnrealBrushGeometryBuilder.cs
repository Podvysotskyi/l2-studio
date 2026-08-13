using System.Numerics;

namespace L2.Tools.PackageReader;

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
