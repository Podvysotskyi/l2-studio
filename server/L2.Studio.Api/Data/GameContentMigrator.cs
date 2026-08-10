using L2.Studio.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace L2.Studio.Api.Data;

public sealed class GameContentMigrator(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IOptions<GameContentOptions> options,
    ILogger<GameContentMigrator> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.RunMigrations)
        {
            return;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        if (pending.Length == 0)
        {
            return;
        }

        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Applied game content migrations {Migrations}", pending);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
