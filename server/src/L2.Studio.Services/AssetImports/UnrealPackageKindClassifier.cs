using L2.Studio.Repositories.Interfaces.Models;

namespace L2.Studio.Services;

public static class UnrealPackageKindClassifier
{
    public static bool IsWorldMap(string path) =>
        AssetImportSourcePaths.MatchesKind(AssetImportJobValues.Maps, path);

    public static bool IsScene(string path) => !IsWorldMap(path);
}
