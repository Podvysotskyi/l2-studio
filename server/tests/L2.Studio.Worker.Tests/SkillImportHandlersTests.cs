using Xunit;

namespace L2.Studio.Worker.Tests;

public sealed class SkillImportHandlersTests
{
    private static readonly SkillTargetTypeDefinition[] Definitions =
    [
        new("AREA", "Area"),
        new("SELF", "Self")
    ];

    [Fact]
    public void AddMissingPreservesExistingDisplayNames()
    {
        var existing = new Dictionary<string, string>
        {
            ["AREA"] = "Custom area",
            ["CUSTOM"] = "Custom"
        };

        var result = SkillImportHandlers.Reconcile(
            Definitions, existing, value => value.Name, value => value.DisplayName, false);

        Assert.Equal("SELF", Assert.Single(result.Missing).Name);
        Assert.Empty(result.Restored);
    }

    [Fact]
    public void RestoreDefaultsResetsChangedBuiltInsAndPreservesExtras()
    {
        var existing = new Dictionary<string, string>
        {
            ["AREA"] = "Custom area",
            ["SELF"] = "Self",
            ["CUSTOM"] = "Custom"
        };

        var result = SkillImportHandlers.Reconcile(
            Definitions, existing, value => value.Name, value => value.DisplayName, true);

        Assert.Empty(result.Missing);
        Assert.Equal("Area", Assert.Single(result.Restored).Value);
        Assert.DoesNotContain("CUSTOM", result.Restored);
    }
}
