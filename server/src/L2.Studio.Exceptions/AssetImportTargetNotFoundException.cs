namespace L2.Studio.Exceptions;

public sealed class AssetImportTargetNotFoundException(string sourceKey)
    : Exception($"The source file '{sourceKey}' does not exist for this import.");
