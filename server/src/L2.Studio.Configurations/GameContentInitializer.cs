using L2.Studio.Context;
using L2.Studio.Migrations.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace L2.Studio.Configurations;

public sealed class GameContentInitializer(
    IDbContextFactory<GameContentDbContext> contextFactory,
    NpcLookupSeeder npcLookupSeeder,
    PlayerLookupSeeder playerLookupSeeder,
    PlayerClassSeeder playerClassSeeder,
    PlayerAppearanceSeeder playerAppearanceSeeder,
    NpcSeeder npcSeeder,
    SkillSeeder skillSeeder,
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

        if (options.Value.SeedNpcLookups) await npcLookupSeeder.SeedAsync(cancellationToken);
        if (options.Value.SeedPlayerLookups) await playerLookupSeeder.SeedAsync(cancellationToken);
        if (options.Value.SeedPlayerClasses) await playerClassSeeder.SeedAsync(cancellationToken);
        if (options.Value.SeedPlayerAppearances) await playerAppearanceSeeder.SeedAsync(cancellationToken);
        if (options.Value.SeedNpcs) await npcSeeder.SeedAsync(cancellationToken);
        if (options.Value.SeedSkills) await skillSeeder.SeedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
