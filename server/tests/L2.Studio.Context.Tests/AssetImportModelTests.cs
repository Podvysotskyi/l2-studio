using L2.Studio.Context;
using L2.Studio.Context.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace L2.Studio.Context.Tests;

public sealed class AssetImportModelTests
{
    [Fact]
    public void EnforcesActiveRunAndRunSourceUniqueness()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseNpgsql("Host=localhost;Database=model;Username=model;Password=model")
            .Options;
        using var context = new GameContentDbContext(options);
        var runIndexes = context.Model.FindEntityType(typeof(AssetImportRun))!.GetIndexes();
        Assert.Contains(runIndexes, index => index.IsUnique &&
            index.GetFilter()!.Contains("full_scan", StringComparison.Ordinal));
        var itemIndexes = context.Model.FindEntityType(typeof(AssetImportWorkItem))!.GetIndexes();
        Assert.Contains(itemIndexes, index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(AssetImportWorkItem.RunId), nameof(AssetImportWorkItem.NormalizedSourceKey)]));
    }
}
