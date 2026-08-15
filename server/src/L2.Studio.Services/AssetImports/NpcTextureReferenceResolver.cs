namespace L2.Studio.Services;

internal sealed class NpcTextureReferenceResolver
{
    private readonly TextureMaterialReference[] candidates;

    public NpcTextureReferenceResolver(IEnumerable<TextureMaterialReference> candidates)
    {
        this.candidates = candidates.ToArray();
    }

    public TextureMaterialReference? Resolve(string reference, out int matchCount)
    {
        var separator = reference.IndexOf('.');
        if (separator <= 0 || separator == reference.Length - 1)
        {
            matchCount = 0;
            return null;
        }

        var packageName = reference[..separator];
        var objectName = reference[(separator + 1)..];
        var exactMatches = candidates.Where(candidate =>
                string.Equals(candidate.PackageName, packageName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(candidate.ObjectName, objectName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (exactMatches.Length > 0)
        {
            matchCount = exactMatches.Length;
            return exactMatches.Length == 1 ? exactMatches[0] : null;
        }

        var leafName = LeafName(objectName);
        var aliasMatches = candidates.Where(candidate =>
                string.Equals(candidate.PackageName, packageName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(LeafName(candidate.ObjectName), leafName, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        matchCount = aliasMatches.Length;
        return aliasMatches.Length == 1 ? aliasMatches[0] : null;
    }

    private static string LeafName(string objectName)
    {
        var separator = objectName.LastIndexOf('.');
        return separator < 0 ? objectName : objectName[(separator + 1)..];
    }
}
