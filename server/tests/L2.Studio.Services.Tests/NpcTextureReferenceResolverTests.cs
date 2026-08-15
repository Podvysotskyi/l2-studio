using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class NpcTextureReferenceResolverTests
{
    [Fact]
    public void PrefersAnExactObjectPathOverLeafAliases()
    {
        var expected = Reference("LineageNpcsTex", "royal.guard_t00", "Texture");
        var resolver = new NpcTextureReferenceResolver([
            expected,
            Reference("LineageNpcsTex", "other.guard_t00", "Texture")
        ]);

        var resolved = resolver.Resolve("lineagenpcstex.royal.guard_t00", out var matchCount);

        Assert.Equal(expected, resolved);
        Assert.Equal(1, matchCount);
    }

    [Fact]
    public void ResolvesAUniquePackageLocalLeafAlias()
    {
        var expected = Reference("LineageNpcsTex", "Box.coffer_a_t00", "Texture");
        var resolver = new NpcTextureReferenceResolver([
            expected,
            Reference("LineageMonstersTex", "Other.coffer_a_t00", "Texture")
        ]);

        var resolved = resolver.Resolve("LineageNPCsTex.coffer_a_t00", out var matchCount);

        Assert.Equal(expected, resolved);
        Assert.Equal(1, matchCount);
    }

    [Fact]
    public void RejectsAnAmbiguousPackageLocalLeafAlias()
    {
        var resolver = new NpcTextureReferenceResolver([
            Reference("LineageNpcsTex", "First.guard_t00", "Texture"),
            Reference("LineageNpcsTex", "Second.guard_t00", "Shader")
        ]);

        var resolved = resolver.Resolve("LineageNpcsTex.guard_t00", out var matchCount);

        Assert.Null(resolved);
        Assert.Equal(2, matchCount);
    }

    private static TextureMaterialReference Reference(string package, string name, string className) =>
        new(package, name, className);
}
