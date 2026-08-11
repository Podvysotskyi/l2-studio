namespace L2.Studio.Exceptions;

public sealed class AssetImportTargetNotFoundException(string levelName)
    : Exception($"The level '{levelName}' does not exist in the active level catalog.");
