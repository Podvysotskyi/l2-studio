using L2.Studio.Context;
using L2.Studio.Contracts;
using L2.Studio.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Repositories;

public sealed class GameVersionRepository(IDbContextFactory<GameContentDbContext> contextFactory)
    : IGameVersionRepository
{
    public async Task<IReadOnlyList<GameVersionSummary>> ListAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GameVersions.AsNoTracking().OrderBy(version => version.SortOrder)
            .Select(version => new GameVersionSummary(
                version.Key,
                version.DisplayName,
                version.SourceFolder,
                version.SortOrder,
                version.Key == "interlude"))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.GameVersions.AsNoTracking().AnyAsync(
            version => version.Key == key,
            cancellationToken);
    }
}
