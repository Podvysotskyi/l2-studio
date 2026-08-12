namespace L2.Studio.Repositories;

public static class AssetImportPathValidator
{
    public static string ResolveContainedFile(string sourceRoot, string fileName, string expectedExtension)
    {
        if (string.IsNullOrWhiteSpace(fileName) || Path.IsPathRooted(fileName))
            throw new ArgumentException("A relative source key is required.", nameof(fileName));
        if (!string.Equals(Path.GetExtension(fileName), expectedExtension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"The source must be a {expectedExtension} file.", nameof(fileName));

        var root = Path.GetFullPath(sourceRoot);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"The configured source directory does not exist: {root}");
        var normalized = fileName.Replace('\\', '/').TrimStart('/');
        if (normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(segment => segment is "." or ".."))
            throw new ArgumentException("The source key must not escape the configured directory.", nameof(fileName));
        var requested = Path.GetFullPath(Path.Combine(root, normalized));
        var relative = Path.GetRelativePath(root, requested);
        if (Path.IsPathRooted(relative) || relative.StartsWith("..", StringComparison.Ordinal))
            throw new ArgumentException("The source key must not escape the configured directory.", nameof(fileName));
        var matches = File.Exists(requested)
            ? [requested]
            : Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Where(path => string.Equals(
                    Path.GetRelativePath(root, path).Replace('\\', '/'),
                    normalized,
                    StringComparison.OrdinalIgnoreCase))
                .Take(2).ToArray();
        if (matches.Length == 0) throw new FileNotFoundException($"Source file '{fileName}' was not found.", fileName);
        if (matches.Length > 1) throw new InvalidDataException($"Source filename '{fileName}' is duplicated ignoring case.");

        var fullPath = Path.GetFullPath(matches[0]);
        relative = Path.GetRelativePath(root, fullPath);
        if (Path.IsPathRooted(relative) || relative.StartsWith("..", StringComparison.Ordinal) ||
            HasSymbolicLink(root, relative))
            throw new ArgumentException("The source must be a regular file contained by the configured directory.", nameof(fileName));
        return fullPath;
    }

    public static bool HasSymbolicLink(string root, string relativePath)
    {
        var current = Path.GetFullPath(root);
        foreach (var segment in relativePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current))
            {
                if (new FileInfo(current).LinkTarget is not null) return true;
            }
            else if (Directory.Exists(current) && new DirectoryInfo(current).LinkTarget is not null)
            {
                return true;
            }
        }
        return false;
    }
}
