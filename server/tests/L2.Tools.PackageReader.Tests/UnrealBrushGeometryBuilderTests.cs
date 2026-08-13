using System.Numerics;
using L2.Tools.PackageReader;
using Xunit;

namespace L2.Tools.PackageReader.Tests;

public sealed class UnrealBrushGeometryBuilderTests
{
    [Fact]
    public void TreatsDuplicatePointRecordsAsSharedTopology()
    {
        var points = new List<Vector3>();
        var faces = new List<UnrealBrushFace>();
        AddFace(points, faces, Vector3.UnitZ, [
            new(-1, -1, -1), new(1, -1, -1), new(1, 1, -1), new(-1, 1, -1)]);
        AddFace(points, faces, Vector3.UnitZ, [
            new(-1, -1, 1), new(-1, 1, 1), new(1, 1, 1), new(1, -1, 1)]);
        AddFace(points, faces, Vector3.UnitY, [
            new(-1, -1, -1), new(-1, -1, 1), new(1, -1, 1), new(1, -1, -1)]);
        AddFace(points, faces, Vector3.UnitY, [
            new(-1, 1, -1), new(1, 1, -1), new(1, 1, 1), new(-1, 1, 1)]);
        AddFace(points, faces, Vector3.UnitX, [
            new(-1, -1, -1), new(-1, 1, -1), new(-1, 1, 1), new(-1, -1, 1)]);
        AddFace(points, faces, Vector3.UnitX, [
            new(1, -1, -1), new(1, -1, 1), new(1, 1, 1), new(1, 1, -1)]);

        var geometry = UnrealBrushGeometryBuilder.Build("duplicate-cube", points, faces);

        Assert.Equal(24, geometry.Positions.Count);
        Assert.Equal(12, geometry.TriangleCount);
    }

    [Fact]
    public void RejectsAGenuinelyOpenBoundary()
    {
        var exception = Assert.Throws<InvalidDataException>(() =>
            UnrealBrushGeometryBuilder.Build(
                "open",
                [new(-1, -1, 0), new(1, -1, 0), new(1, 1, 0), new(-1, 1, 0)],
                [new UnrealBrushFace([0, 1, 2, 3], Vector3.UnitZ)]));

        Assert.Contains("boundary is not closed", exception.Message);
    }

    private static void AddFace(
        List<Vector3> points,
        List<UnrealBrushFace> faces,
        Vector3 normal,
        IReadOnlyList<Vector3> facePoints)
    {
        var start = points.Count;
        points.AddRange(facePoints);
        faces.Add(new UnrealBrushFace(
            Enumerable.Range(start, facePoints.Count).ToArray(),
            normal));
    }
}
