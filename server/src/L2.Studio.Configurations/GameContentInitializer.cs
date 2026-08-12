using L2.Studio.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace L2.Studio.Configurations;

public sealed class GameContentInitializer(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IOptions<GameContentOptions> options,
    ILogger<GameContentInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.Value.RunMigrations)
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
            if (pending.Length > 0)
            {
                await context.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Applied game content migrations {Migrations}", pending);
            }
        }

    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
