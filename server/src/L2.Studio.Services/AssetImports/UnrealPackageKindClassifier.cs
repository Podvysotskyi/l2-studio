using System.Text.RegularExpressions;

namespace L2.Studio.Services;

public static partial class UnrealPackageKindClassifier
{
    [GeneratedRegex("^[0-9]{2}_[0-9]{2}$", RegexOptions.CultureInvariant)]
    private static partial Regex WorldLevelNamePattern();

    public static bool IsWorldLevel(string path) =>
        WorldLevelNamePattern().IsMatch(Path.GetFileNameWithoutExtension(path));

    public static bool IsScene(string path) => !IsWorldLevel(path);
}
