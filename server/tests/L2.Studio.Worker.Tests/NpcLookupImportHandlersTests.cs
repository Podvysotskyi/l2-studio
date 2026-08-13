using Xunit;

namespace L2.Studio.Worker.Tests;

public sealed class NpcLookupImportHandlersTests
{
    private static readonly NpcLookupDefinition[] Definitions =
    [
        new("FIGHTER", "Fighter"),
        new("MAGE", "Mage")
    ];

    [Fact]
    public void AddMissingPreservesExistingDisplayNames()
    {
        var existing = new Dictionary<string, string>
        {
            ["FIGHTER"] = "Custom fighter",
            ["CUSTOM"] = "Custom"
        };

        var result = NpcLookupImportHandlers.Reconcile(Definitions, existing, false);

        Assert.Equal("MAGE", Assert.Single(result.Missing).Name);
        Assert.Empty(result.Restored);
    }

    [Fact]
    public void RestoreDefaultsResetsChangedBuiltInsAndPreservesExtras()
    {
        var existing = new Dictionary<string, string>
        {
            ["FIGHTER"] = "Custom fighter",
            ["MAGE"] = "Mage",
            ["CUSTOM"] = "Custom"
        };

        var result = NpcLookupImportHandlers.Reconcile(Definitions, existing, true);

        Assert.Empty(result.Missing);
        Assert.Equal("Fighter", Assert.Single(result.Restored).Value);
        Assert.DoesNotContain("CUSTOM", result.Restored);
    }
}
