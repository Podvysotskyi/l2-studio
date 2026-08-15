using Xunit;

namespace L2.Studio.Worker.Tests;

public sealed class ItemLookupImportHandlersTests
{
    private static readonly ItemLookupDefinition[] Definitions =
    [
        new("EQUIP", "Equip"),
        new("SOULSHOT", "Soulshot")
    ];

    [Fact]
    public void AddMissingPreservesExistingDisplayNames()
    {
        var existing = new Dictionary<string, string>
        {
            ["EQUIP"] = "Custom equip",
            ["CUSTOM"] = "Custom"
        };

        var result = ItemLookupImportHandlers.Reconcile(Definitions, existing, false);

        Assert.Equal("SOULSHOT", Assert.Single(result.Missing).Name);
        Assert.Empty(result.Restored);
    }

    [Fact]
    public void RestoreDefaultsResetsChangedBuiltInsAndPreservesExtras()
    {
        var existing = new Dictionary<string, string>
        {
            ["EQUIP"] = "Custom equip",
            ["SOULSHOT"] = "Soulshot",
            ["CUSTOM"] = "Custom"
        };

        var result = ItemLookupImportHandlers.Reconcile(Definitions, existing, true);

        Assert.Empty(result.Missing);
        Assert.Equal("Equip", Assert.Single(result.Restored).Value);
        Assert.DoesNotContain("CUSTOM", result.Restored);
    }
}
