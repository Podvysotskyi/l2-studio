using L2.Studio.Context;
using L2.Studio.Migrations;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Configurations;

public sealed class GameContentInitializer(
    IDbContextFactory<GameContentDbContext> contextFactory,
    GameVersionSeeder gameVersionSeeder,
    ILogger<GameContentInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        if (pending.Length > 0)
        {
            await context.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Applied game content migrations {Migrations}", pending);
        }

        await gameVersionSeeder.SeedAsync(context, cancellationToken);
        logger.LogInformation("Seeded game versions");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
