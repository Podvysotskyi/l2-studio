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
