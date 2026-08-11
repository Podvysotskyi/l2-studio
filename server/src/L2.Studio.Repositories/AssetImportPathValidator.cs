namespace L2.Studio.Repositories;

public static class AssetImportPathValidator
{
    public static string ResolveContainedFile(string sourceRoot, string fileName, string expectedExtension)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName) ||
            fileName.Contains('/') || fileName.Contains('\\'))
            throw new ArgumentException("A single source filename is required.", nameof(fileName));
        if (!string.Equals(Path.GetExtension(fileName), expectedExtension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"The source must be a {expectedExtension} file.", nameof(fileName));

        var root = Path.GetFullPath(sourceRoot);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"The configured source directory does not exist: {root}");
        var matches = Directory.EnumerateFiles(root)
            .Where(path => string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();
        if (matches.Length == 0) throw new FileNotFoundException($"Source file '{fileName}' was not found.", fileName);
        if (matches.Length > 1) throw new InvalidDataException($"Source filename '{fileName}' is duplicated ignoring case.");

        var fullPath = Path.GetFullPath(matches[0]);
        var relative = Path.GetRelativePath(root, fullPath);
        if (Path.IsPathRooted(relative) || relative.StartsWith("..", StringComparison.Ordinal) ||
            new FileInfo(fullPath).LinkTarget is not null)
            throw new ArgumentException("The source must be a regular file contained by the configured directory.", nameof(fileName));
        return fullPath;
    }
}
