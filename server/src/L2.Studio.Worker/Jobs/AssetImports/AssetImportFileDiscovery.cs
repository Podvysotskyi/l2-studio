using L2.Studio.Repositories.Interfaces.Models;

namespace L2.Studio.Worker;

internal static class AssetImportFileDiscovery
{
    public static IReadOnlyList<string> Paths(string versionRoot, string kind)
    {
        var root = Path.GetFullPath(versionRoot);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"The configured source directory does not exist: {root}");
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };
        var paths = Directory.EnumerateFiles(root, "*", options)
            .Where(path => AssetImportSourcePaths.MatchesKind(kind, path))
            .OrderBy(path => Path.GetRelativePath(root, path).Replace('\\', '/'), StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var duplicate = paths.GroupBy(
            path => Path.GetRelativePath(root, path).Replace('\\', '/'),
            StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidDataException(
                $"Source path '{Path.GetRelativePath(root, duplicate.First()).Replace('\\', '/')}' is duplicated ignoring case.");
        return paths;
    }
}
