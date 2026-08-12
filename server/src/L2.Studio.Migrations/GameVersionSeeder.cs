using L2.Studio.Context;
using L2.Studio.Context.Entities;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Migrations;

public sealed class GameVersionSeeder
{
    private static readonly SeedVersion[] Versions =
    [
        new("c1", "Chronicle 1", "C1", 10),
        new("c4", "Chronicle 4", "C4", 20),
        new("interlude", "Interlude", "Interlude", 30)
    ];

    public async Task SeedAsync(GameContentDbContext context, CancellationToken cancellationToken)
    {
        var keys = Versions.Select(version => version.Key).ToArray();
        var existing = await context.GameVersions
            .Where(version => keys.Contains(version.Key))
            .ToDictionaryAsync(version => version.Key, cancellationToken);

        foreach (var version in Versions)
        {
            if (existing.TryGetValue(version.Key, out var entity))
            {
                entity.DisplayName = version.DisplayName;
                entity.SourceFolder = version.SourceFolder;
                entity.SortOrder = version.SortOrder;
                continue;
            }

            context.GameVersions.Add(new GameVersion
            {
                Key = version.Key,
                DisplayName = version.DisplayName,
                SourceFolder = version.SourceFolder,
                SortOrder = version.SortOrder
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private sealed record SeedVersion(string Key, string DisplayName, string SourceFolder, int SortOrder);
}
